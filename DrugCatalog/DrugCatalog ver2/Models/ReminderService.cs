using DrugCatalog_ver2.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace DrugCatalog_ver2.Models
{
    public interface IReminderService : IDisposable
    {
        void AddReminder(MedicationReminder reminder);
        void UpdateReminder(MedicationReminder reminder);
        void DeleteReminder(int reminderId);
        List<MedicationReminder> GetReminders();
        List<MedicationReminder> GetDueReminders();
        void CheckAndShowReminders();
        void ShowReminderNotification(MedicationReminder reminder);
    }

    public class ReminderService : IReminderService
    {
        // Путь к файлу теперь в папке Data
        private readonly string _remindersFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "reminders.xml");

        private List<MedicationReminder> _reminders;
        private readonly Timer _reminderTimer;
        private readonly NotifyIcon _notifyIcon;
        private readonly IXmlDataService _dataService;

        // Храним последнее показанное напоминание для обработки клика
        private MedicationReminder _lastShownReminder;
        private bool _disposed = false;

        public ReminderService(IXmlDataService dataService)
        {
            _dataService = dataService;

            // Создаем папку Data, если её нет (на всякий случай, хотя XmlDataService это тоже делает)
            var directory = Path.GetDirectoryName(_remindersFilePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            _reminders = LoadReminders();

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = "Drug Catalog Reminders"
            };

            // Подписка на клик по уведомлению
            _notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;

            // Таймер проверяет напоминания каждую минуту
            _reminderTimer = new Timer { Interval = 60000 };
            _reminderTimer.Tick += (s, e) => CheckAndShowReminders();
            _reminderTimer.Start();
        }

        // --- Обработка клика по уведомлению ---
        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (_lastShownReminder == null || _dataService == null) return;

            // Локализованный вопрос: "Вы приняли Парацетамол (1 таб)?"
            string message = string.Format(Locale.Get("MsgConfirmTake"), _lastShownReminder.DrugName, _lastShownReminder.Dosage);

            var result = MessageBox.Show(
                message,
                Locale.Get("TitleConfirmTake"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                DeductDrugStock(_lastShownReminder);
            }
        }

        // --- Логика списания со склада ---
        private void DeductDrugStock(MedicationReminder reminder)
        {
            try
            {
                var allDrugs = _dataService.LoadDrugs();
                // Ищем препарат по ID, если не нашли - по имени
                var drug = allDrugs.FirstOrDefault(d => d.Id == reminder.DrugId);
                if (drug == null)
                {
                    drug = allDrugs.FirstOrDefault(d => d.Name.Equals(reminder.DrugName, StringComparison.OrdinalIgnoreCase));
                }

                if (drug != null)
                {
                    int amountToDeduct = ParseDosageAmount(reminder.Dosage);

                    if (amountToDeduct > 0)
                    {
                        if (drug.Quantity >= amountToDeduct)
                        {
                            drug.Quantity -= amountToDeduct;
                            _dataService.SaveDrugs(allDrugs);

                            // Локализованное сообщение об успехе
                            string msg = string.Format(Locale.Get("MsgDeducted"), amountToDeduct, drug.Quantity);
                            _notifyIcon.ShowBalloonTip(3000, Locale.Get("TitleSuccess"), msg, ToolTipIcon.Info);
                        }
                        else
                        {
                            // Локализованное предупреждение
                            string msg = string.Format(Locale.Get("MsgLowStock"), drug.Name, drug.Quantity, amountToDeduct);
                            MessageBox.Show(msg, Locale.Get("TitleWarning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Locale.Get("MsgError")}: {ex.Message}");
            }
        }

        // Парсинг числа из строки дозировки (например "2 таблетки" -> 2)
        private int ParseDosageAmount(string dosageString)
        {
            if (string.IsNullOrWhiteSpace(dosageString)) return 0;

            try
            {
                var parts = dosageString.Trim().Split(' ');
                if (parts.Length > 0)
                {
                    string numberPart = parts[0].Replace(',', '.');
                    if (decimal.TryParse(numberPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                    {
                        return (int)Math.Ceiling(result);
                    }
                }
            }
            catch { }
            return 0;
        }

        // --- CRUD операции ---

        public void AddReminder(MedicationReminder reminder)
        {
            reminder.Id = _reminders.Count > 0 ? _reminders.Max(r => r.Id) + 1 : 1;
            _reminders.Add(reminder);
            SaveReminders();
        }

        public void UpdateReminder(MedicationReminder reminder)
        {
            var existing = _reminders.FirstOrDefault(r => r.Id == reminder.Id);
            if (existing != null)
            {
                _reminders.Remove(existing);
                _reminders.Add(reminder);
                SaveReminders();
            }
        }

        public void DeleteReminder(int reminderId)
        {
            _reminders.RemoveAll(r => r.Id == reminderId);
            SaveReminders();
        }

        public List<MedicationReminder> GetReminders()
        {
            return _reminders.Where(r => r.IsActive).ToList();
        }

        public List<MedicationReminder> GetDueReminders()
        {
            var now = DateTime.Now;
            return _reminders.Where(r =>
                r.IsActive &&
                r.ShouldShowToday() &&
                r.ReminderTime.TimeOfDay <= now.TimeOfDay &&
                r.ReminderTime.TimeOfDay > now.AddMinutes(-1).TimeOfDay
            ).ToList();
        }

        public void CheckAndShowReminders()
        {
            var dueReminders = GetDueReminders();
            foreach (var reminder in dueReminders)
            {
                ShowReminderNotification(reminder);
            }
        }

        public void ShowReminderNotification(MedicationReminder reminder)
        {
            _lastShownReminder = reminder;

            // Локализация заголовка и текста
            _notifyIcon.BalloonTipTitle = Locale.Get("NotifTitle");

            // Текст: Название \n Дозировка: ... \n Нажмите...
            _notifyIcon.BalloonTipText = $"{reminder.DrugName}\n{Locale.Get("NotifDosage")}: {reminder.Dosage}\n\n{Locale.Get("NotifClick")}";

            if (!string.IsNullOrEmpty(reminder.Notes))
            {
                _notifyIcon.BalloonTipText += $"\n{Locale.Get("ColNotes")}: {reminder.Notes}";
            }

            _notifyIcon.ShowBalloonTip(10000);
            System.Media.SystemSounds.Exclamation.Play();
        }

        // --- Загрузка и сохранение ---

        private List<MedicationReminder> LoadReminders()
        {
            try
            {
                if (!File.Exists(_remindersFilePath))
                    return new List<MedicationReminder>();

                var serializer = new XmlSerializer(typeof(List<MedicationReminder>));
                using (var stream = new FileStream(_remindersFilePath, FileMode.Open))
                {
                    return (List<MedicationReminder>)serializer.Deserialize(stream) ?? new List<MedicationReminder>();
                }
            }
            catch (Exception)
            {
                return new List<MedicationReminder>();
            }
        }

        private void SaveReminders()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<MedicationReminder>));
                using (var stream = new FileStream(_remindersFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, _reminders);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Locale.Get("MsgError")}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reminderTimer?.Stop();
                    _reminderTimer?.Dispose();
                    _notifyIcon?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}