using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class AddEditReminderForm : Form
    {
        private readonly IReminderService _reminderService;
        private readonly List<Drug> _drugs;
        private MedicationReminder _reminder;
        private bool _isEditMode;

        private ComboBox comboBoxDrug;
        private DateTimePicker timePicker;
        private NumericUpDown numericDosage;
        private ComboBox comboBoxUnit;
        private TextBox textBoxNotes;
        private CheckBox[] dayCheckboxes;
        private Button buttonSave, buttonCancel;

        public AddEditReminderForm(IReminderService reminderService, List<Drug> drugs, MedicationReminder reminder = null)
        {
            _reminderService = reminderService;
            _drugs = drugs;
            _isEditMode = reminder != null;
            _reminder = reminder ?? new MedicationReminder();

            InitializeComponent();
            if (_isEditMode) FillForm();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = _isEditMode ? "Редактирование напоминания" : "Новое напоминание";
            this.Size = new Size(400, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            CreateControls();
            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            int y = 10;
            int labelWidth = 120;
            int controlWidth = 200;

            // Выбор лекарства
            AddLabel("Лекарство:", 10, y, labelWidth);
            comboBoxDrug = new ComboBox
            {
                Location = new Point(130, y),
                Width = controlWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxDrug.Items.AddRange(_drugs.Select(d => d.Name).ToArray());
            this.Controls.Add(comboBoxDrug);
            y += 35;

            // Время приема
            AddLabel("Время приема:", 10, y, labelWidth);
            timePicker = new DateTimePicker
            {
                Location = new Point(130, y),
                Width = controlWidth,
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Now.Date.AddHours(9) // По умолчанию 9:00
            };
            this.Controls.Add(timePicker);
            y += 35;

            // Дозировка
            AddLabel("Дозировка:", 10, y, labelWidth);
            var dosagePanel = new Panel { Location = new Point(130, y), Size = new Size(200, 25) };

            numericDosage = new NumericUpDown
            {
                Location = new Point(0, 0),
                Width = 80,
                Minimum = 0.1m,
                Maximum = 1000,
                DecimalPlaces = 1,
                Value = 1
            };

            comboBoxUnit = new ComboBox
            {
                Location = new Point(85, 0),
                Width = 115,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            comboBoxUnit.Items.AddRange(new[] { "мг", "г", "мл", "таблетка", "капсула", "капли" });
            comboBoxUnit.Text = "таблетка";

            dosagePanel.Controls.Add(numericDosage);
            dosagePanel.Controls.Add(comboBoxUnit);
            this.Controls.Add(dosagePanel);
            y += 35;

            // Дни недели
            AddLabel("Дни приема:", 10, y, labelWidth);
            y += 20;

            string[] days = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            dayCheckboxes = new CheckBox[7];

            for (int i = 0; i < 7; i++)
            {
                dayCheckboxes[i] = new CheckBox
                {
                    Text = days[i],
                    Location = new Point(130 + (i * 45), y),
                    Width = 40,
                    Checked = true
                };
                this.Controls.Add(dayCheckboxes[i]);
            }
            y += 35;

            // Примечания
            AddLabel("Примечания:", 10, y, labelWidth);
            textBoxNotes = new TextBox
            {
                Location = new Point(130, y),
                Width = controlWidth,
                Height = 60,
                Multiline = true
            };
            this.Controls.Add(textBoxNotes);
            y += 70;

            // Кнопки
            var buttonPanel = new Panel { Location = new Point(10, y), Size = new Size(360, 40) };

            buttonSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(100, 5),
                Size = new Size(80, 30),
                BackColor = Color.LightGreen
            };
            buttonSave.Click += ButtonSave_Click;

            buttonCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(190, 5),
                Size = new Size(80, 30),
                BackColor = Color.LightCoral
            };
            buttonCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            buttonPanel.Controls.Add(buttonSave);
            buttonPanel.Controls.Add(buttonCancel);
            this.Controls.Add(buttonPanel);
        }

        private void AddLabel(string text, int x, int y, int width)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Width = width
            };
            this.Controls.Add(label);
        }

        private void FillForm()
        {
            if (_isEditMode)
            {
                comboBoxDrug.Text = _reminder.DrugName;
                timePicker.Value = _reminder.ReminderTime;

                // Парсим дозировку "1 таблетка" -> numeric=1, unit="таблетка"
                if (!string.IsNullOrEmpty(_reminder.Dosage))
                {
                    var parts = _reminder.Dosage.Split(' ');
                    if (parts.Length >= 2 && decimal.TryParse(parts[0], out decimal dosage))
                    {
                        numericDosage.Value = dosage;
                        comboBoxUnit.Text = string.Join(" ", parts.Skip(1));
                    }
                }

                textBoxNotes.Text = _reminder.Notes;

                for (int i = 0; i < 7; i++)
                {
                    dayCheckboxes[i].Checked = _reminder.DaysOfWeek[i];
                }
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(comboBoxDrug.Text))
            {
                MessageBox.Show("Выберите лекарство");
                return;
            }

            _reminder.DrugName = comboBoxDrug.Text;
            _reminder.ReminderTime = timePicker.Value;
            _reminder.Dosage = $"{numericDosage.Value} {comboBoxUnit.Text}";
            _reminder.Notes = textBoxNotes.Text;

            for (int i = 0; i < 7; i++)
            {
                _reminder.DaysOfWeek[i] = dayCheckboxes[i].Checked;
            }

            if (_isEditMode)
                _reminderService.UpdateReminder(_reminder);
            else
                _reminderService.AddReminder(_reminder);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}