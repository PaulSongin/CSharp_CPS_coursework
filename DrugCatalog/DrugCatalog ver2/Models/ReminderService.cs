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
        private readonly string _remindersFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "reminders.xml");
        private List<MedicationReminder> _reminders;
        private readonly Timer _reminderTimer;
        private readonly NotifyIcon _notifyIcon;
        private readonly IXmlDataService _dataService;
        private MedicationReminder _lastShownReminder;
        private bool _disposed = false;

        public ReminderService(IXmlDataService dataService)
        {
            _dataService = dataService;
            _reminders = LoadReminders();

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = "Напоминания о приеме лекарств"
            };

            _notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;

            _reminderTimer = new Timer { Interval = 60000 };
            _reminderTimer.Tick += (s, e) => CheckAndShowReminders();
            _reminderTimer.Start();
        }

        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (_lastShownReminder == null || _dataService == null) return;

            var result = MessageBox.Show(
                $"Вы приняли {_lastShownReminder.DrugName} ({_lastShownReminder.Dosage})?\n\nНажмите 'Да', чтобы списать лекарство со склада.",
                "Подтверждение приема",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

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

                            _notifyIcon.ShowBalloonTip(3000, "Успешно",
                                $"Списано {amountToDeduct} ед. Остаток: {drug.Quantity}", ToolTipIcon.Info);
                        }
                        else
                        {
                            MessageBox.Show($"Внимание! Лекарство '{drug.Name}' заканчивается.\nОстаток: {drug.Quantity}, а нужно принять: {amountToDeduct}.",
                                "Недостаточно на складе", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при списании лекарства: {ex.Message}");
            }
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
                    {
                        return (int)Math.Ceiling(result);
                    }
                }
            }
            catch { }
            return 0;
        }

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

            _notifyIcon.BalloonTipTitle = "💊 Пора принять лекарство";
            _notifyIcon.BalloonTipText = $"{reminder.DrugName}\nДозировка: {reminder.Dosage}\n\nНажмите сюда, чтобы подтвердить прием.";

            if (!string.IsNullOrEmpty(reminder.Notes))
            {
                _notifyIcon.BalloonTipText += $"\nПримечание: {reminder.Notes}";
            }

            _notifyIcon.ShowBalloonTip(10000);
            System.Media.SystemSounds.Exclamation.Play();
        }

        private List<MedicationReminder> LoadReminders()
        {
            try
            {
                var directory = Path.GetDirectoryName(_remindersFilePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

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
                var directory = Path.GetDirectoryName(_remindersFilePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                var serializer = new XmlSerializer(typeof(List<MedicationReminder>));
                using (var stream = new FileStream(_remindersFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, _reminders);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения напоминаний: {ex.Message}");
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