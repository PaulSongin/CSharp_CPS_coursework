using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class AddEditScheduleForm : Form
    {
        private readonly IMedicationScheduleService _scheduleService;
        private readonly IXmlDataService _dataService;
        private readonly User _currentUser;
        private readonly List<Drug> _drugs;
        private MedicationSchedule _schedule;
        private bool _isEditMode;

        private ComboBox comboBoxDrug, comboBoxFrequency, comboBoxDosageUnit;
        private DateTimePicker datePickerStart, datePickerEnd;
        private DateTimePicker timePicker;
        private NumericUpDown numericDosage;
        private CheckedListBox checkedListBoxDays;
        private TextBox textBoxNotes;
        private CheckBox checkBoxActive;
        private Button buttonSave, buttonCancel;

        public AddEditScheduleForm(IMedicationScheduleService scheduleService, IXmlDataService dataService,
                                 User currentUser, List<Drug> drugs, MedicationSchedule schedule = null)
        {
            _scheduleService = scheduleService;
            _dataService = dataService;
            _currentUser = currentUser;
            _drugs = drugs;
            _isEditMode = schedule != null;
            _schedule = schedule ?? new MedicationSchedule { UserId = currentUser.Id };

            InitializeComponent();
            if (_isEditMode) FillForm();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = _isEditMode ? "Редактирование расписания" : "Добавление расписания";
            this.Size = new Size(500, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            int y = 20;

            AddComboBoxControl("Препарат*:", ref y, out comboBoxDrug, 300);
            comboBoxDrug.Items.AddRange(_drugs.Select(d => d.Name).ToArray());

            AddDosageControls(ref y);

            AddComboBoxControl("Частота приема*:", ref y, out comboBoxFrequency, 200);
            comboBoxFrequency.Items.AddRange(new[] { "Ежедневно", "Еженедельно", "Ежемесячно", "По дням недели", "Однократно" });
            comboBoxFrequency.SelectedIndex = 0;
            comboBoxFrequency.SelectedIndexChanged += (s, e) => UpdateFrequencyControls();

            AddDaySelectionControls(ref y);

            AddDateControls(ref y);

            AddTimeControls(ref y);

            AddNotesControls(ref y);

            AddActiveControl(ref y);

            AddButtons(ref y);
        }

        private void AddComboBoxControl(string labelText, ref int y, out ComboBox comboBox, int width)
        {
            var label = new Label { Text = labelText, Location = new Point(20, y), Size = new Size(150, 20) };
            comboBox = new ComboBox { Location = new Point(180, y), Size = new Size(width, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            y += 35;

            this.Controls.Add(label);
            this.Controls.Add(comboBox);
        }

        private void AddDosageControls(ref int y)
        {
            var labelDosage = new Label { Text = "Дозировка*:", Location = new Point(20, y), Size = new Size(150, 20) };

            numericDosage = new NumericUpDown
            {
                Location = new Point(180, y),
                Size = new Size(80, 25),
                Minimum = 0.1m,
                Maximum = 1000,
                DecimalPlaces = 1,
                Value = 1
            };

            comboBoxDosageUnit = new ComboBox
            {
                Location = new Point(270, y),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxDosageUnit.Items.AddRange(new[] { "мг", "г", "мл", "таб", "капс" });
            comboBoxDosageUnit.SelectedIndex = 0;

            y += 35;

            this.Controls.Add(labelDosage);
            this.Controls.Add(numericDosage);
            this.Controls.Add(comboBoxDosageUnit);
        }

        private void AddDaySelectionControls(ref int y)
        {
            var labelDays = new Label { Text = "Дни недели:", Location = new Point(20, y), Size = new Size(150, 20) };

            checkedListBoxDays = new CheckedListBox
            {
                Location = new Point(180, y),
                Size = new Size(200, 80),
                CheckOnClick = true
            };
            checkedListBoxDays.Items.AddRange(new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье" });

            y += 90;

            this.Controls.Add(labelDays);
            this.Controls.Add(checkedListBoxDays);
        }

        private void AddDateControls(ref int y)
        {
            var labelStart = new Label { Text = "Начало*:", Location = new Point(20, y), Size = new Size(150, 20) };
            datePickerStart = new DateTimePicker
            {
                Location = new Point(180, y),
                Size = new Size(120, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            var labelEnd = new Label { Text = "Окончание*:", Location = new Point(320, y), Size = new Size(80, 20) };
            datePickerEnd = new DateTimePicker
            {
                Location = new Point(400, y),
                Size = new Size(120, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(30)
            };

            y += 35;

            this.Controls.Add(labelStart);
            this.Controls.Add(datePickerStart);
            this.Controls.Add(labelEnd);
            this.Controls.Add(datePickerEnd);
        }

        private void AddTimeControls(ref int y)
        {
            var labelTime = new Label { Text = "Время приема*:", Location = new Point(20, y), Size = new Size(150, 20) };
            timePicker = new DateTimePicker
            {
                Location = new Point(180, y),
                Size = new Size(80, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(8)
            };

            y += 35;

            this.Controls.Add(labelTime);
            this.Controls.Add(timePicker);
        }

        private void AddNotesControls(ref int y)
        {
            var labelNotes = new Label { Text = "Примечания:", Location = new Point(20, y), Size = new Size(150, 20) };
            textBoxNotes = new TextBox
            {
                Location = new Point(180, y),
                Size = new Size(280, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            y += 70;

            this.Controls.Add(labelNotes);
            this.Controls.Add(textBoxNotes);
        }

        private void AddActiveControl(ref int y)
        {
            checkBoxActive = new CheckBox
            {
                Text = "Активное расписание",
                Location = new Point(20, y),
                Size = new Size(200, 20),
                Checked = true
            };

            y += 30;

            this.Controls.Add(checkBoxActive);
        }

        private void AddButtons(ref int y)
        {
            buttonSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(150, y),
                Size = new Size(100, 35),
                BackColor = Color.LightGreen
            };
            buttonSave.Click += (s, e) => SaveSchedule();

            buttonCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(260, y),
                Size = new Size(100, 35),
                BackColor = Color.LightCoral
            };
            buttonCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.Add(buttonSave);
            this.Controls.Add(buttonCancel);
        }

        private void UpdateFrequencyControls()
        {
            var showDays = comboBoxFrequency.SelectedIndex == 3; // "По дням недели"
            checkedListBoxDays.Visible = showDays;
            checkedListBoxDays.Enabled = showDays;

            if (showDays)
            {
                for (int i = 0; i < checkedListBoxDays.Items.Count; i++)
                {
                    checkedListBoxDays.SetItemChecked(i, true);
                }
            }
        }

        private void FillForm()
        {
            var drug = _drugs.FirstOrDefault(d => d.Id == _schedule.DrugId);
            if (drug != null)
            {
                comboBoxDrug.Text = drug.Name;
            }

            numericDosage.Value = _schedule.Dosage;
            comboBoxDosageUnit.Text = _schedule.DosageUnit;
            comboBoxFrequency.SelectedIndex = (int)_schedule.Frequency - 1;
            datePickerStart.Value = _schedule.StartDate;
            datePickerEnd.Value = _schedule.EndDate;
            timePicker.Value = DateTime.Today.Add(_schedule.Time);
            textBoxNotes.Text = _schedule.Notes;
            checkBoxActive.Checked = _schedule.IsActive;

            if (!string.IsNullOrEmpty(_schedule.DaysOfWeek))
            {
                var days = _schedule.DaysOfWeek.Split(',');
                for (int i = 0; i < checkedListBoxDays.Items.Count; i++)
                {
                    checkedListBoxDays.SetItemChecked(i, days.Contains((i + 1).ToString()));
                }
            }

            UpdateFrequencyControls();
        }

        private void SaveSchedule()
        {
            if (!ValidateForm()) return;

            try
            {
                var selectedDrug = _drugs.FirstOrDefault(d => d.Name == comboBoxDrug.Text);
                if (selectedDrug == null)
                {
                    MessageBox.Show("Выберите препарат из списка");
                    return;
                }

                _schedule.DrugId = selectedDrug.Id;
                _schedule.DrugName = selectedDrug.Name;
                _schedule.Dosage = numericDosage.Value;
                _schedule.DosageUnit = comboBoxDosageUnit.Text;
                _schedule.Frequency = (ScheduleFrequency)(comboBoxFrequency.SelectedIndex + 1);
                _schedule.StartDate = datePickerStart.Value;
                _schedule.EndDate = datePickerEnd.Value;
                _schedule.Time = timePicker.Value.TimeOfDay;
                _schedule.Notes = textBoxNotes.Text;
                _schedule.IsActive = checkBoxActive.Checked;

                if (comboBoxFrequency.SelectedIndex == 3) // По дням недели
                {
                    var selectedDays = new List<string>();
                    for (int i = 0; i < checkedListBoxDays.Items.Count; i++)
                    {
                        if (checkedListBoxDays.GetItemChecked(i))
                        {
                            selectedDays.Add((i + 1).ToString());
                        }
                    }
                    _schedule.DaysOfWeek = string.Join(",", selectedDays);
                }
                else
                {
                    _schedule.DaysOfWeek = "1,2,3,4,5,6,7";
                }

                if (_isEditMode)
                {
                    _scheduleService.UpdateSchedule(_schedule);
                }
                else
                {
                    _scheduleService.AddSchedule(_schedule);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrEmpty(comboBoxDrug.Text))
            {
                MessageBox.Show("Выберите препарат");
                return false;
            }

            if (datePickerStart.Value > datePickerEnd.Value)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания");
                return false;
            }

            if (comboBoxFrequency.SelectedIndex == 3) // По дням недели
            {
                var hasSelectedDays = false;
                for (int i = 0; i < checkedListBoxDays.Items.Count; i++)
                {
                    if (checkedListBoxDays.GetItemChecked(i))
                    {
                        hasSelectedDays = true;
                        break;
                    }
                }

                if (!hasSelectedDays)
                {
                    MessageBox.Show("Выберите хотя бы один день недели");
                    return false;
                }
            }

            return true;
        }
    }
}