using DrugCatalog_ver2.Models; // Добавь, если нет
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace DrugCatalog_ver2.Services
{
    public interface IReminderService : IDisposable
    {
        void AddReminder(MedicationReminder reminder);
        void UpdateReminder(MedicationReminder reminder);
        void DeleteReminder(int reminderId);
        List<MedicationReminder> GetReminders(); // Теперь возвращает только для текущего юзера
        List<MedicationReminder> GetDueReminders();
        void CheckAndShowReminders();
        void ShowReminderNotification(MedicationReminder reminder);
    }

    public class ReminderService : IReminderService
    {
        private readonly string _remindersFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "reminders.xml");
        private List<MedicationReminder> _allReminders; // Все напоминания всех пользователей
        private readonly Timer _reminderTimer;
        private readonly NotifyIcon _notifyIcon;
        private readonly IXmlDataService _dataService;
        private readonly int _currentUserId; // ID текущего пользователя

        private MedicationReminder _lastShownReminder;
        private bool _disposed = false;

        // Конструктор теперь принимает ID пользователя
        public ReminderService(IXmlDataService dataService, int currentUserId)
        {
            _dataService = dataService;
            _currentUserId = currentUserId; // Запоминаем, чей это сервис

            var directory = Path.GetDirectoryName(_remindersFilePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            _allReminders = LoadReminders();

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = "Drug Catalog Reminders"
            };

            _notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;

            _reminderTimer = new Timer { Interval = 60000 };
            _reminderTimer.Tick += (s, e) => CheckAndShowReminders();
            _reminderTimer.Start();
        }

        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (_lastShownReminder == null || _dataService == null) return;

            string message = string.Format(Locale.Get("MsgConfirmTake"), _lastShownReminder.DrugName, _lastShownReminder.Dosage);

            var result = MessageBox.Show(message, Locale.Get("TitleConfirmTake"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                DeductDrugStock(_lastShownReminder);
            }
        }

        private void DeductDrugStock(MedicationReminder reminder)
        {
            try
            {
                var allDrugs = _dataService.LoadDrugs();
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
                            string msg = string.Format(Locale.Get("MsgDeducted"), amountToDeduct, drug.Quantity);
                            _notifyIcon.ShowBalloonTip(3000, Locale.Get("TitleSuccess"), msg, ToolTipIcon.Info);
                        }
                        else
                        {
                            string msg = string.Format(Locale.Get("MsgLowStock"), drug.Name, drug.Quantity, amountToDeduct);
                            MessageBox.Show(msg, Locale.Get("TitleWarning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"{Locale.Get("MsgError")}: {ex.Message}"); }
        }

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
                        return (int)Math.Ceiling(result);
                }
            }
            catch { }
            return 0;
        }

        public void AddReminder(MedicationReminder reminder)
        {
            reminder.Id = _allReminders.Count > 0 ? _allReminders.Max(r => r.Id) + 1 : 1;
            reminder.UserId = _currentUserId; // Привязываем к текущему пользователю
            _allReminders.Add(reminder);
            SaveReminders();
        }

        public void UpdateReminder(MedicationReminder reminder)
        {
            var existing = _allReminders.FirstOrDefault(r => r.Id == reminder.Id && r.UserId == _currentUserId);
            if (existing != null)
            {
                // Обновляем поля, но сохраняем ID и UserId
                existing.DrugId = reminder.DrugId;
                existing.DrugName = reminder.DrugName;
                existing.Dosage = reminder.Dosage;
                existing.ReminderTime = reminder.ReminderTime;
                existing.DaysOfWeek = reminder.DaysOfWeek;
                existing.IsActive = reminder.IsActive;
                existing.Notes = reminder.Notes;
                SaveReminders();
            }
        }

        public void DeleteReminder(int reminderId)
        {
            _allReminders.RemoveAll(r => r.Id == reminderId && r.UserId == _currentUserId);
            SaveReminders();
        }

        // Возвращаем только напоминания ТЕКУЩЕГО пользователя
        public List<MedicationReminder> GetReminders()
        {
            return _allReminders.Where(r => r.UserId == _currentUserId && r.IsActive).ToList();
        }

        // Проверяем только напоминания ТЕКУЩЕГО пользователя
        public List<MedicationReminder> GetDueReminders()
        {
            var now = DateTime.Now;
            return _allReminders.Where(r =>
                r.UserId == _currentUserId && // Фильтр по пользователю
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
            _notifyIcon.BalloonTipTitle = Locale.Get("NotifTitle");
            _notifyIcon.BalloonTipText = $"{reminder.DrugName}\n{Locale.Get("NotifDosage")}: {reminder.Dosage}\n\n{Locale.Get("NotifClick")}";
            if (!string.IsNullOrEmpty(reminder.Notes))
                _notifyIcon.BalloonTipText += $"\n{Locale.Get("ColNotes")}: {reminder.Notes}";
            _notifyIcon.ShowBalloonTip(10000);
            System.Media.SystemSounds.Exclamation.Play();
        }

        private List<MedicationReminder> LoadReminders()
        {
            try
            {
                if (!File.Exists(_remindersFilePath)) return new List<MedicationReminder>();
                var serializer = new XmlSerializer(typeof(List<MedicationReminder>));
                using (var stream = new FileStream(_remindersFilePath, FileMode.Open))
                {
                    return (List<MedicationReminder>)serializer.Deserialize(stream) ?? new List<MedicationReminder>();
                }
            }
            catch { return new List<MedicationReminder>(); }
        }

        private void SaveReminders()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<MedicationReminder>));
                using (var stream = new FileStream(_remindersFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, _allReminders);
                }
            }
            catch (Exception ex) { MessageBox.Show($"{Locale.Get("MsgError")}: {ex.Message}"); }
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