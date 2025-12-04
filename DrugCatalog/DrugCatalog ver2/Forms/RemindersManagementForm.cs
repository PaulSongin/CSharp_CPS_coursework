using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class RemindersManagementForm : Form
    {
        private readonly IReminderService _reminderService;
        private readonly List<Drug> _drugs;
        private DataGridView dataGridViewReminders;
        private Button buttonAdd, buttonEdit, buttonDelete, buttonClose;

        public RemindersManagementForm(IReminderService reminderService, List<Drug> drugs)
        {
            _reminderService = reminderService;
            _drugs = drugs;
            InitializeComponent();
            LoadReminders();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Управление напоминаниями";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            CreateControls();
            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            dataGridViewReminders = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(10, 10),
                Size = new Size(560, 300)
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            buttonAdd = new Button { Text = "Добавить", Location = new Point(10, 10), Size = new Size(80, 30) };
            buttonEdit = new Button { Text = "Редактировать", Location = new Point(100, 10), Size = new Size(100, 30) };
            buttonDelete = new Button { Text = "Удалить", Location = new Point(210, 10), Size = new Size(80, 30) };
            buttonClose = new Button { Text = "Закрыть", Location = new Point(300, 10), Size = new Size(80, 30) };

            buttonAdd.Click += (s, e) => AddReminder();
            buttonEdit.Click += (s, e) => EditReminder();
            buttonDelete.Click += (s, e) => DeleteReminder();
            buttonClose.Click += (s, e) => this.Close();

            buttonPanel.Controls.AddRange(new Control[] { buttonAdd, buttonEdit, buttonDelete, buttonClose });

            this.Controls.Add(dataGridViewReminders);
            this.Controls.Add(buttonPanel);
        }

        private void LoadReminders()
        {
            var reminders = _reminderService.GetReminders();

            dataGridViewReminders.Columns.Clear();
            dataGridViewReminders.Columns.Add("DrugName", "Лекарство");
            dataGridViewReminders.Columns.Add("Dosage", "Дозировка");
            dataGridViewReminders.Columns.Add("Time", "Время");
            dataGridViewReminders.Columns.Add("Days", "Дни недели");
            dataGridViewReminders.Columns.Add("Notes", "Примечания");

            dataGridViewReminders.Rows.Clear();
            foreach (var reminder in reminders)
            {
                string days = GetDaysString(reminder.DaysOfWeek);
                dataGridViewReminders.Rows.Add(
                    reminder.DrugName,
                    reminder.Dosage,
                    reminder.ReminderTime.ToString("HH:mm"),
                    days,
                    reminder.Notes
                );
                dataGridViewReminders.Rows[dataGridViewReminders.Rows.Count - 1].Tag = reminder.Id;
            }
        }

        private string GetDaysString(bool[] days)
        {
            string[] dayNames = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            return string.Join(" ", days.Select((d, i) => d ? dayNames[i] : "").Where(s => !string.IsNullOrEmpty(s)));
        }

        private void AddReminder()
        {
            var form = new AddEditReminderForm(_reminderService, _drugs);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadReminders();
            }
        }

        private void EditReminder()
        {
            if (dataGridViewReminders.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите напоминание для редактирования");
                return;
            }

            var reminderId = (int)dataGridViewReminders.SelectedRows[0].Tag;
            var reminder = _reminderService.GetReminders().FirstOrDefault(r => r.Id == reminderId);

            if (reminder != null)
            {
                var form = new AddEditReminderForm(_reminderService, _drugs, reminder);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadReminders();
                }
            }
        }

        private void DeleteReminder()
        {
            if (dataGridViewReminders.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите напоминание для удаления");
                return;
            }

            var reminderId = (int)dataGridViewReminders.SelectedRows[0].Tag;

            if (MessageBox.Show("Удалить это напоминание?", "Подтверждение",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _reminderService.DeleteReminder(reminderId);
                LoadReminders();
            }
        }
    }
}