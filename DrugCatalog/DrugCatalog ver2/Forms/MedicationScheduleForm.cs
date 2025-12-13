using DrugCatalog_ver2.Services;
using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class MedicationScheduleForm : Form
    {
        private readonly IMedicationScheduleService _scheduleService;
        private readonly IXmlDataService _dataService;
        private readonly User _currentUser;
        private List<Drug> _drugs;
        private List<MedicationSchedule> _schedules;
        private DataGridView dataGridViewSchedules;
        private Button buttonAdd, buttonEdit, buttonDelete, buttonMarkTaken, buttonClose;
        private ComboBox comboBoxFilter;

        public MedicationScheduleForm(IMedicationScheduleService scheduleService, IXmlDataService dataService, User currentUser)
        {
            _scheduleService = scheduleService;
            _dataService = dataService;
            _currentUser = currentUser;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = $"График приема лекарств - {_currentUser.FullName}";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft Sans Serif", 9f);

            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.LightSteelBlue,
                Padding = new Padding(10)
            };

            var labelFilter = new Label
            {
                Text = "Фильтр:",
                Location = new Point(10, 15),
                Size = new Size(50, 20)
            };

            comboBoxFilter = new ComboBox
            {
                Location = new Point(70, 12),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxFilter.Items.AddRange(new[] { "Все расписания", "Активные", "На сегодня", "На неделю" });
            comboBoxFilter.SelectedIndex = 0;
            comboBoxFilter.SelectedIndexChanged += (s, e) => RefreshSchedules();

            filterPanel.Controls.AddRange(new Control[] { labelFilter, comboBoxFilter });

            dataGridViewSchedules = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            buttonAdd = new Button
            {
                Text = "➕ Добавить",
                Size = new Size(120, 35),
                Location = new Point(10, 7),
                BackColor = Color.LightGreen
            };
            buttonAdd.Click += (s, e) => AddSchedule();

            buttonEdit = new Button
            {
                Text = "✏️ Редактировать",
                Size = new Size(140, 35),
                Location = new Point(140, 7),
                BackColor = Color.LightBlue
            };
            buttonEdit.Click += (s, e) => EditSchedule();

            buttonDelete = new Button
            {
                Text = "❌ Удалить",
                Size = new Size(120, 35),
                Location = new Point(290, 7),
                BackColor = Color.LightCoral
            };
            buttonDelete.Click += (s, e) => DeleteSchedule();

            buttonMarkTaken = new Button
            {
                Text = "✅ Принято",
                Size = new Size(120, 35),
                Location = new Point(420, 7),
                BackColor = Color.LightYellow
            };
            buttonMarkTaken.Click += (s, e) => MarkAsTaken();

            buttonClose = new Button
            {
                Text = "Закрыть",
                Size = new Size(100, 35),
                Location = new Point(550, 7),
                BackColor = Color.LightGray
            };
            buttonClose.Click += (s, e) => this.Close();

            buttonPanel.Controls.AddRange(new Control[] {
                buttonAdd, buttonEdit, buttonDelete, buttonMarkTaken, buttonClose
            });

            mainPanel.Controls.Add(dataGridViewSchedules);
            mainPanel.Controls.Add(filterPanel);
            mainPanel.Controls.Add(buttonPanel);

            this.Controls.Add(mainPanel);

            dataGridViewSchedules.DoubleClick += (s, e) => EditSchedule();
        }

        private void LoadData()
        {
            _drugs = _dataService.LoadDrugs();
            RefreshSchedules();
        }

        private void RefreshSchedules()
        {
            switch (comboBoxFilter.SelectedIndex)
            {
                case 0: _schedules = _scheduleService.GetUserSchedules(_currentUser.Id); break;
                case 1: _schedules = _scheduleService.GetActiveSchedules(_currentUser.Id); break;
                case 2: _schedules = _scheduleService.GetTodaysSchedules(_currentUser.Id); break;
                case 3: _schedules = _scheduleService.GetUpcomingSchedules(_currentUser.Id, 7); break;
                default: _schedules = _scheduleService.GetUserSchedules(_currentUser.Id); break;
            }

            dataGridViewSchedules.Columns.Clear();

            dataGridViewSchedules.Columns.Add("DrugName", "Препарат");
            dataGridViewSchedules.Columns.Add("DosageInfo", "Дозировка");
            dataGridViewSchedules.Columns.Add("ScheduleInfo", "Расписание");
            dataGridViewSchedules.Columns.Add("Period", "Период");
            dataGridViewSchedules.Columns.Add("Status", "Статус");
            dataGridViewSchedules.Columns.Add("LastTaken", "Последний прием");

            var idColumn = new DataGridViewTextBoxColumn { Name = "Id", Visible = false };
            dataGridViewSchedules.Columns.Add(idColumn);

            dataGridViewSchedules.Rows.Clear();

            foreach (var schedule in _schedules)
            {
                var drug = _drugs.FirstOrDefault(d => d.Id == schedule.DrugId);
                var drugName = drug?.Name ?? schedule.DrugName;

                var status = schedule.IsActive ? "Активно" : "Неактивно";
                if (schedule.LastTaken.HasValue)
                {
                    status += " (принято)";
                }

                dataGridViewSchedules.Rows.Add(
                    drugName,
                    $"{schedule.Dosage} {schedule.DosageUnit}",
                    GetScheduleDescription(schedule),
                    $"{schedule.StartDate:dd.MM.yyyy} - {schedule.EndDate:dd.MM.yyyy}",
                    status,
                    schedule.LastTaken?.ToString("dd.MM.yyyy HH:mm") ?? "Никогда",
                    schedule.Id
                );
            }

            UpdateRowColors();
        }

        private string GetScheduleDescription(MedicationSchedule schedule)
        {
            var time = schedule.Time.ToString(@"hh\:mm");

            switch (schedule.Frequency)
            {
                case ScheduleFrequency.Daily:
                    return $"Ежедневно в {time}";
                case ScheduleFrequency.Weekly:
                    return $"Еженедельно в {time}";
                case ScheduleFrequency.Monthly:
                    return $"Ежемесячно в {time}";
                case ScheduleFrequency.SpecificDays:
                    var days = GetDayNames(schedule.DaysOfWeek);
                    return $"{days} в {time}";
                case ScheduleFrequency.Once:
                    return $"Однократно в {time}";
                default:
                    return $"В {time}";
            }
        }

        private string GetDayNames(string daysOfWeek)
        {
            var dayNames = new Dictionary<string, string>
            {
                {"1", "Пн"}, {"2", "Вт"}, {"3", "Ср"}, {"4", "Чт"}, {"5", "Пт"}, {"6", "Сб"}, {"7", "Вс"}
            };

            var days = daysOfWeek.Split(',').Select(d => dayNames.ContainsKey(d) ? dayNames[d] : d);
            return string.Join(",", days);
        }

        private void UpdateRowColors()
        {
            foreach (DataGridViewRow row in dataGridViewSchedules.Rows)
            {
                var scheduleId = (int)row.Cells["Id"].Value;
                var schedule = _schedules.FirstOrDefault(s => s.Id == scheduleId);

                if (schedule != null)
                {
                    if (!schedule.IsActive)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightGray;
                    }
                    else if (schedule.LastTaken.HasValue && schedule.LastTaken.Value.Date == DateTime.Today)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightGreen;
                    }
                    else if (schedule.Time < DateTime.Now.TimeOfDay && schedule.StartDate <= DateTime.Today)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.White;
                    }
                }
            }
        }

        private void AddSchedule()
        {
            var form = new AddEditScheduleForm(_scheduleService, _dataService, _currentUser, _drugs);
            if (form.ShowDialog() == DialogResult.OK)
            {
                RefreshSchedules();
            }
        }

        private void EditSchedule()
        {
            if (dataGridViewSchedules.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите расписание для редактирования");
                return;
            }

            var scheduleId = (int)dataGridViewSchedules.SelectedRows[0].Cells["Id"].Value;
            var schedule = _scheduleService.GetSchedule(scheduleId);

            if (schedule != null)
            {
                var form = new AddEditScheduleForm(_scheduleService, _dataService, _currentUser, _drugs, schedule);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    RefreshSchedules();
                }
            }
        }

        private void DeleteSchedule()
        {
            if (dataGridViewSchedules.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите расписание для удаления");
                return;
            }

            var scheduleId = (int)dataGridViewSchedules.SelectedRows[0].Cells["Id"].Value;
            var schedule = _scheduleService.GetSchedule(scheduleId);

            if (schedule != null)
            {
                var result = MessageBox.Show(
                    $"Удалить расписание для '{schedule.DrugName}'?",
                    "Подтверждение удаления",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    _scheduleService.DeleteSchedule(scheduleId);
                    RefreshSchedules();
                    MessageBox.Show("Расписание удалено");
                }
            }
        }

        private void MarkAsTaken()
        {
            if (dataGridViewSchedules.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите расписание для отметки");
                return;
            }

            var scheduleId = (int)dataGridViewSchedules.SelectedRows[0].Cells["Id"].Value;
            _scheduleService.MarkAsTaken(scheduleId, DateTime.Now);
            RefreshSchedules();
            MessageBox.Show("Прием лекарства отмечен");
        }
    }
}