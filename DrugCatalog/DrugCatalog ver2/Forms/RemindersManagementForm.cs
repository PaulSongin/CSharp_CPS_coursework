using DrugCatalog_ver2.Services;
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

            this.Text = Locale.Get("TitleRemindersMgmt"); 
            this.Size = new Size(700, 400);
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
                Size = new Size(560, 300),
                BackgroundColor = Color.White, 
                RowHeadersVisible = false
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };

            buttonAdd = new Button { Text = Locale.Get("Add"), Location = new Point(10, 10), Size = new Size(100, 30) };
            buttonEdit = new Button { Text = Locale.Get("Edit"), Location = new Point(120, 10), Size = new Size(120, 30) };
            buttonDelete = new Button { Text = Locale.Get("Delete"), Location = new Point(250, 10), Size = new Size(100, 30) };
            buttonClose = new Button { Text = Locale.Get("Close"), Location = new Point(360, 10), Size = new Size(100, 30) };

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
            dataGridViewReminders.Columns.Add("DrugName", Locale.Get("ColDrug"));
            dataGridViewReminders.Columns.Add("Dosage", Locale.Get("ColDosage"));
            dataGridViewReminders.Columns.Add("Time", Locale.Get("ColTime"));
            dataGridViewReminders.Columns.Add("Days", Locale.Get("ColDays"));
            dataGridViewReminders.Columns.Add("Notes", Locale.Get("ColNotes"));

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
            string[] dayKeys = { "DayMon", "DayTue", "DayWed", "DayThu", "DayFri", "DaySat", "DaySun" };

            var activeDays = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                if (days[i])
                {
                    activeDays.Add(Locale.Get(dayKeys[i]));
                }
            }
            return string.Join(" ", activeDays);
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
                MessageBox.Show(Locale.Get("MsgSelEdit"));
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
                MessageBox.Show(Locale.Get("MsgSelDel")); 
                return;
            }

            var reminderId = (int)dataGridViewReminders.SelectedRows[0].Tag;

            if (MessageBox.Show(Locale.Get("MsgConfirmDelete"), Locale.Get("Delete"),
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _reminderService.DeleteReminder(reminderId);
                LoadReminders();
            }
        }
    }
}