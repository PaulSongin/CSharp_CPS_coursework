using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DrugCatalog_ver.Forms
{
    public partial class AddEditDrugForm : Form
    {
        private readonly IXmlDataService _dataService;
        private Drug _drug;
        private bool _isEditMode;

        private TextBox textBoxName, textBoxSubstance, textBoxManufacturer, textBoxForm;
        private TextBox textBoxDosage, textBoxUnit, textBoxPrescription, textBoxPrice, textBoxQuantity;
        private DateTimePicker datePickerExpiry;
        private TextBox textBoxIndications, textBoxContraindications;
        private Button buttonSave, buttonCancel;

        public AddEditDrugForm(IXmlDataService dataService, Drug drug = null)
        {
            _dataService = dataService;
            _isEditMode = drug != null;
            _drug = drug ?? new Drug();

            InitializeComponent();
            if (_isEditMode) FillForm();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = _isEditMode ? "Редактирование препарата" : "Добавление препарата";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            CreateControls();

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CreateControls()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            int y = 10;
            int labelWidth = 150;
            int textBoxWidth = 300;

            // Название
            AddLabeledControl("Название*:", ref y, labelWidth, out textBoxName, textBoxWidth);

            // Действующее вещество
            AddLabeledControl("Действующее вещество*:", ref y, labelWidth, out textBoxSubstance, textBoxWidth);

            // Производитель
            AddLabeledControl("Производитель*:", ref y, labelWidth, out textBoxManufacturer, textBoxWidth);

            // Форма выпуска
            AddLabeledControl("Форма выпуска:", ref y, labelWidth, out textBoxForm, textBoxWidth);

            // Дозировка и единица измерения
            AddLabeledControl("Дозировка*:", ref y, labelWidth, out textBoxDosage, 100);
            AddLabeledControl("Единица измерения:", ref y, 120, out textBoxUnit, 80, 30);

            // Тип отпуска
            AddLabeledControl("Тип отпуска:", ref y, labelWidth, out textBoxPrescription, textBoxWidth);

            // Цена
            AddLabeledControl("Цена*:", ref y, labelWidth, out textBoxPrice, textBoxWidth);

            // Количество
            AddLabeledControl("Количество*:", ref y, labelWidth, out textBoxQuantity, textBoxWidth);

            // Срок годности
            var lblExpiry = new Label
            {
                Text = "Срок годности*:",
                Location = new Point(10, y),
                Width = labelWidth,
                Font = new Font("Microsoft Sans Serif", 9f)
            };
            datePickerExpiry = new DateTimePicker
            {
                Location = new Point(160, y),
                Width = textBoxWidth,
                Format = DateTimePickerFormat.Short
            };
            y += 30;

            // Показания
            AddMultiLineControl("Показания:", ref y, labelWidth, out textBoxIndications, textBoxWidth, 60);

            // Противопоказания
            AddMultiLineControl("Противопоказания:", ref y, labelWidth, out textBoxContraindications, textBoxWidth, 60);

            // Кнопки
            buttonSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(160, y + 20),
                Size = new Size(80, 30),
                BackColor = Color.LightGreen
            };
            buttonSave.Click += ButtonSave_Click;

            buttonCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(270, y + 20),
                Size = new Size(80, 30),
                BackColor = Color.LightCoral
            };
            buttonCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            panel.Controls.Add(lblExpiry);
            panel.Controls.Add(datePickerExpiry);

            panel.Controls.Add(buttonSave);
            panel.Controls.Add(buttonCancel);

            this.Controls.Add(panel);
        }

        private void AddLabeledControl(string labelText, ref int y, int labelWidth, out TextBox textBox, int textBoxWidth, int offsetY = 30)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(10, y),
                Width = labelWidth,
                Font = new Font("Microsoft Sans Serif", 9f)
            };
            textBox = new TextBox
            {
                Location = new Point(160, y),
                Width = textBoxWidth,
                Font = new Font("Microsoft Sans Serif", 9f)
            };
            y += offsetY;

            this.Controls.Add(label);
            this.Controls.Add(textBox);
        }

        private void AddMultiLineControl(string labelText, ref int y, int labelWidth, out TextBox textBox, int textBoxWidth, int height)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(10, y),
                Width = labelWidth,
                Font = new Font("Microsoft Sans Serif", 9f)
            };
            textBox = new TextBox
            {
                Location = new Point(160, y),
                Width = textBoxWidth,
                Height = height,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Microsoft Sans Serif", 9f)
            };
            y += height + 10;

            this.Controls.Add(label);
            this.Controls.Add(textBox);
        }

        private void FillForm()
        {
            textBoxName.Text = _drug.Name;
            textBoxSubstance.Text = _drug.ActiveSubstance;
            textBoxManufacturer.Text = _drug.Manufacturer;
            textBoxForm.Text = _drug.Form;
            textBoxDosage.Text = _drug.Dosage.ToString();
            textBoxUnit.Text = _drug.DosageUnit;
            textBoxPrescription.Text = _drug.PrescriptionType;
            textBoxPrice.Text = _drug.Price.ToString();
            textBoxQuantity.Text = _drug.Quantity.ToString();
            datePickerExpiry.Value = _drug.ExpiryDate > DateTime.MinValue ? _drug.ExpiryDate : DateTime.Now.AddYears(1);

            if (_drug.Indications != null)
                textBoxIndications.Text = string.Join(Environment.NewLine, _drug.Indications);

            if (_drug.Contraindications != null)
                textBoxContraindications.Text = string.Join(Environment.NewLine, _drug.Contraindications);
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                _drug.Name = textBoxName.Text.Trim();
                _drug.ActiveSubstance = textBoxSubstance.Text.Trim();
                _drug.Manufacturer = textBoxManufacturer.Text.Trim();
                _drug.Form = textBoxForm.Text.Trim();
                _drug.Dosage = decimal.Parse(textBoxDosage.Text);
                _drug.DosageUnit = textBoxUnit.Text.Trim();
                _drug.PrescriptionType = textBoxPrescription.Text.Trim();
                _drug.Price = decimal.Parse(textBoxPrice.Text);
                _drug.Quantity = int.Parse(textBoxQuantity.Text);
                _drug.ExpiryDate = datePickerExpiry.Value;

                _drug.Indications = textBoxIndications.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                _drug.Contraindications = textBoxContraindications.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var drugs = _dataService.LoadDrugs();
                if (_isEditMode)
                {
                    var index = drugs.FindIndex(d => d.Id == _drug.Id);
                    if (index != -1)
                        drugs[index] = _drug;
                    else
                        drugs.Add(_drug);
                }
                else
                {
                    _drug.Id = _dataService.GetNextId();
                    drugs.Add(_drug);
                }

                _dataService.SaveDrugs(drugs);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(textBoxName.Text))
            {
                MessageBox.Show("Введите название препарата", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(textBoxSubstance.Text))
            {
                MessageBox.Show("Введите действующее вещество", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxSubstance.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(textBoxManufacturer.Text))
            {
                MessageBox.Show("Введите производителя", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxManufacturer.Focus();
                return false;
            }

            if (!decimal.TryParse(textBoxPrice.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxPrice.Focus();
                return false;
            }

            if (!int.TryParse(textBoxQuantity.Text, out int quantity) || quantity < 0)
            {
                MessageBox.Show("Введите корректное количество", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxQuantity.Focus();
                return false;
            }

            if (!decimal.TryParse(textBoxDosage.Text, out decimal dosage) || dosage <= 0)
            {
                MessageBox.Show("Введите корректную дозировку", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxDosage.Focus();
                return false;
            }

            return true;
        }
    }
}