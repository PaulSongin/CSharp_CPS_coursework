using DrugCatalog_ver2.Models;
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
        List<MedicationReminder> GetReminders();
        List<MedicationReminder> GetDueReminders();
        void CheckAndShowReminders();

        // Для напоминаний о лекарствах (с вопросом "Приняли?")
        void ShowReminderNotification(MedicationReminder reminder);

        // Для информационных сообщений (без действий)
        void ShowInfoNotification(string title, string text);
    }

    public class ReminderService : IReminderService
    {
        private readonly string _remindersFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "reminders.xml");
        private List<MedicationReminder> _allReminders;

        private readonly Timer _reminderTimer;
        private readonly NotifyIcon _notifyIcon;
        private readonly IXmlDataService _dataService;
        private readonly int _currentUserId;

        // Храним объект последнего показанного лекарства для обработки клика
        private MedicationReminder _lastShownReminder;

        // Храним последнюю обработанную минуту, чтобы не спамить уведомлениями
        private int _lastCheckedMinute = -1;

        private bool _disposed = false;

        public ReminderService(IXmlDataService dataService, int currentUserId, Action onOpen = null, Action onExit = null)
        {
            _dataService = dataService;
            _currentUserId = currentUserId;

            // Создаем папку Data, если нет
            var directory = Path.GetDirectoryName(_remindersFilePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            _allReminders = LoadReminders();

            // --- Настройка меню трея ---
            var contextMenu = new ContextMenuStrip();
            if (onOpen != null)
                contextMenu.Items.Add(Locale.Get("MenuOpen"), null, (s, e) => onOpen());

            contextMenu.Items.Add("-");

            if (onExit != null)
                contextMenu.Items.Add(Locale.Get("MenuExit"), null, (s, e) => onExit());

            // --- Настройка иконки ---
            _notifyIcon = new NotifyIcon
            {
                // Используем системную иконку - это самый надежный вариант
                Icon = SystemIcons.Application,
                Visible = true,
                Text = Locale.Get("AppTitle"),
                ContextMenuStrip = contextMenu
            };

            // Попытка загрузить кастомную иконку, если есть (необязательно)
            try { if (File.Exists("medicine_icon-icons.com_66070.ico")) _notifyIcon.Icon = Properties.Resources.medicine_icon_icons_com_66070; } catch { }

            // Двойной клик открывает приложение
            if (onOpen != null)
                _notifyIcon.DoubleClick += (s, e) => onOpen();

            // Клик по "пузырю" уведомления
            _notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;

            // --- Настройка таймера ---
            // Проверяем чаще (каждые 2 сек), чтобы не пропустить начало минуты
            _reminderTimer = new Timer { Interval = 2000 };
            _reminderTimer.Tick += (s, e) => CheckAndShowReminders();
            _reminderTimer.Start();
        }

        // --- Обработка клика по уведомлению ---
        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            // Если _lastShownReminder == null, значит это было инфо-сообщение
            if (_lastShownReminder == null || _dataService == null) return;

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

        // --- Метод проверки (вызывается таймером) ---
        public void CheckAndShowReminders()
        {
            var now = DateTime.Now;

            // Если мы уже проверяли в эту минуту - выходим
            if (_lastCheckedMinute == now.Minute) return;

            // Запоминаем текущую минуту
            _lastCheckedMinute = now.Minute;

            var dueReminders = GetDueReminders();
            foreach (var reminder in dueReminders)
            {
                ShowReminderNotification(reminder);
            }
        }

        // --- Показ уведомления о лекарстве ---
        public void ShowReminderNotification(MedicationReminder reminder)
        {
            _lastShownReminder = reminder; // Запоминаем для обработки клика

            _notifyIcon.BalloonTipTitle = Locale.Get("NotifTitle");
            _notifyIcon.BalloonTipText = $"{reminder.DrugName}\n{Locale.Get("NotifDosage")}: {reminder.Dosage}\n\n{Locale.Get("NotifClick")}";

            if (!string.IsNullOrEmpty(reminder.Notes))
            {
                _notifyIcon.BalloonTipText += $"\n{Locale.Get("ColNotes")}: {reminder.Notes}";
            }

            _notifyIcon.ShowBalloonTip(10000); // 10 секунд
            System.Media.SystemSounds.Exclamation.Play();
        }

        // --- Показ информационного уведомления ---
        public void ShowInfoNotification(string title, string text)
        {
            _lastShownReminder = null; // Сбрасываем, чтобы клик не вызывал списание

            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = text;
            _notifyIcon.ShowBalloonTip(3000);
        }

        // --- Списание со склада ---
        private void DeductDrugStock(MedicationReminder reminder)
        {
            try
            {
                var allDrugs = _dataService.LoadDrugs();
                var drug = allDrugs.FirstOrDefault(d => d.Id == reminder.DrugId);

                if (drug == null)
                    drug = allDrugs.FirstOrDefault(d => d.Name.Equals(reminder.DrugName, StringComparison.OrdinalIgnoreCase));

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
                            // Показываем инфо-сообщение об успехе
                            ShowInfoNotification(Locale.Get("TitleSuccess"), msg);
                        }
                        else
                        {
                            string msg = string.Format(Locale.Get("MsgLowStock"), drug.Name, drug.Quantity, amountToDeduct);
                            MessageBox.Show(msg, Locale.Get("TitleWarning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                _lastShownReminder = null; // Сбрасываем после обработки
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Locale.Get("MsgError")}: {ex.Message}");
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
                        return (int)Math.Ceiling(result);
                }
            }
            catch { }
            return 0;
        }

        // --- CRUD (с UserId) ---
        public void AddReminder(MedicationReminder reminder)
        {
            reminder.Id = _allReminders.Count > 0 ? _allReminders.Max(r => r.Id) + 1 : 1;
            reminder.UserId = _currentUserId;
            _allReminders.Add(reminder);
            SaveReminders();
        }

        public void UpdateReminder(MedicationReminder reminder)
        {
            var existing = _allReminders.FirstOrDefault(r => r.Id == reminder.Id && r.UserId == _currentUserId);
            if (existing != null)
            {
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

        public List<MedicationReminder> GetReminders()
        {
            return _allReminders.Where(r => r.UserId == _currentUserId && r.IsActive).ToList();
        }

        public List<MedicationReminder> GetDueReminders()
        {
            var now = DateTime.Now;
            return _allReminders.Where(r =>
                r.UserId == _currentUserId &&
                r.IsActive &&
                r.ShouldShowToday() &&
                r.ReminderTime.Hour == now.Hour &&   // Точное совпадение часа
                r.ReminderTime.Minute == now.Minute  // Точное совпадение минуты
            ).ToList();
        }

        // --- Загрузка/Сохранение ---
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

                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Visible = false; 
                        _notifyIcon.Dispose();
                    }
                }
                _disposed = true;
            }
        }
    }
}