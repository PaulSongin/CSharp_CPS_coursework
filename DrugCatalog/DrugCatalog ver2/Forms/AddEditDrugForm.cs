using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class AddEditDrugForm : Form
    {
        private readonly IXmlDataService _dataService;
        private readonly ICategoryService _categoryService;
        private readonly List<Drug> _allDrugs;
        private Drug _drug;
        private bool _isEditMode;

        private ComboBox comboBoxName, comboBoxSubstance, comboBoxManufacturer, comboBoxForm, comboBoxUnit, comboBoxPrescription;
        private ComboBox comboBoxDosage, comboBoxQuantity, comboBoxCategory;
        private DateTimePicker datePickerExpiry;
        private TextBox textBoxIndications, textBoxContraindications;
        private Button buttonSave, buttonCancel;

        public AddEditDrugForm(IXmlDataService dataService, ICategoryService categoryService, List<Drug> allDrugs, Drug drug = null)
        {
            _dataService = dataService;
            _categoryService = categoryService;
            _allDrugs = allDrugs ?? new List<Drug>();
            _isEditMode = drug != null;
            _drug = drug ?? new Drug();

            InitializeComponent();
            SetupAutoComplete();
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
            this.Font = new Font("Microsoft Sans Serif", 9f);

            CreateControls();

            this.ResumeLayout(false);
        }

        private void SetupAutoComplete()
        {
            foreach (var drugName in DrugDictionary.CommonDrugs.Keys)
            {
                comboBoxName.Items.Add(drugName);
            }

            var existingNames = _allDrugs.Select(d => d.Name)
                                       .Where(n => !string.IsNullOrEmpty(n))
                                       .Distinct()
                                       .OrderBy(n => n)
                                       .ToArray();
            foreach (var name in existingNames)
            {
                if (!comboBoxName.Items.Contains(name))
                    comboBoxName.Items.Add(name);
            }

            var substances = _allDrugs.Select(d => d.ActiveSubstance)
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .Distinct()
                                    .OrderBy(s => s)
                                    .ToArray();
            comboBoxSubstance.Items.AddRange(substances);

            comboBoxManufacturer.Items.AddRange(DrugDictionary.CommonManufacturers);
            comboBoxForm.Items.AddRange(DrugDictionary.CommonForms);
            comboBoxUnit.Items.AddRange(DrugDictionary.CommonDosageUnits);
            comboBoxPrescription.Items.AddRange(DrugDictionary.CommonPrescriptionTypes);
            comboBoxDosage.Items.AddRange(DrugDictionary.GetCommonDosages());
            comboBoxQuantity.Items.AddRange(DrugDictionary.GetCommonQuantities());

            // ДОБАВЛЕНО: Заполнение категорий
            var categories = _categoryService.GetCategories();
            foreach (var category in categories)
            {
                comboBoxCategory.Items.Add(category.Name);
            }

            SetupComboBoxAutoComplete(comboBoxName);
            SetupComboBoxAutoComplete(comboBoxSubstance);
            SetupComboBoxAutoComplete(comboBoxManufacturer);
            SetupComboBoxAutoComplete(comboBoxForm);
            SetupComboBoxAutoComplete(comboBoxUnit);
            SetupComboBoxAutoComplete(comboBoxPrescription);
            SetupComboBoxAutoComplete(comboBoxDosage);
            SetupComboBoxAutoComplete(comboBoxQuantity);
            SetupComboBoxAutoComplete(comboBoxCategory);
        }

        private void SetupComboBoxAutoComplete(ComboBox comboBox)
        {
            comboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        private void CreateControls()
        {
            int y = 10;
            int labelWidth = 150;
            int controlWidth = 300;

            // ДОБАВЛЕНО: ComboBox для категории
            AddComboBoxControl("Категория*:", ref y, labelWidth, out comboBoxCategory, controlWidth);

            // Заполняем категории
            var categories = _categoryService.GetCategories();
            foreach (var category in categories)
            {
                comboBoxCategory.Items.Add(category.Name);
            }
            comboBoxCategory.SelectedIndex = 0;

            AddComboBoxControl("Название*:", ref y, labelWidth, out comboBoxName, controlWidth);
            comboBoxName.SelectedIndexChanged += (s, e) => UpdateDrugInfoFromDictionary();

            AddComboBoxControl("Действующее вещество*:", ref y, labelWidth, out comboBoxSubstance, controlWidth);
            AddComboBoxControl("Производитель*:", ref y, labelWidth, out comboBoxManufacturer, controlWidth);
            AddComboBoxControl("Форма выпуска:", ref y, labelWidth, out comboBoxForm, controlWidth);

            // Панель для дозировки и единиц измерения
            var dosagePanel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(450, 30)
            };

            var lblDosage = new Label
            {
                Text = "Дозировка*:",
                Location = new Point(0, 5),
                Width = labelWidth
            };

            comboBoxDosage = new ComboBox
            {
                Location = new Point(150, 2),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            comboBoxDosage.Items.AddRange(DrugDictionary.GetCommonDosages());
            SetupComboBoxAutoComplete(comboBoxDosage);

            var lblUnit = new Label
            {
                Text = "Единица измерения:",
                Location = new Point(260, 5),
                Width = 120
            };

            comboBoxUnit = new ComboBox
            {
                Location = new Point(380, 2),
                Width = 70,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            comboBoxUnit.Items.AddRange(DrugDictionary.CommonDosageUnits);
            SetupComboBoxAutoComplete(comboBoxUnit);

            dosagePanel.Controls.AddRange(new Control[] { lblDosage, comboBoxDosage, lblUnit, comboBoxUnit });
            this.Controls.Add(dosagePanel);
            y += 35;

            AddComboBoxControl("Тип отпуска:", ref y, labelWidth, out comboBoxPrescription, controlWidth);
            AddComboBoxControl("Количество*:", ref y, labelWidth, out comboBoxQuantity, controlWidth);

            var lblExpiry = new Label
            {
                Text = "Срок годности*:",
                Location = new Point(10, y),
                Width = labelWidth
            };
            datePickerExpiry = new DateTimePicker
            {
                Location = new Point(160, y),
                Width = controlWidth,
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddYears(1)
            };
            this.Controls.Add(lblExpiry);
            this.Controls.Add(datePickerExpiry);
            y += 35;

            AddMultiLineControl("Показания:", ref y, labelWidth, out textBoxIndications, controlWidth, 60);
            AddMultiLineControl("Противопоказания:", ref y, labelWidth, out textBoxContraindications, controlWidth, 60);

            // Кнопки
            var buttonPanel = new Panel
            {
                Location = new Point(10, y + 20),
                Size = new Size(450, 40)
            };

            buttonSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(120, 5),
                Size = new Size(100, 30),
                BackColor = Color.LightGreen,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold)
            };
            buttonSave.Click += ButtonSave_Click;

            buttonCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(240, 5),
                Size = new Size(100, 30),
                BackColor = Color.LightCoral,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular)
            };
            buttonCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            buttonPanel.Controls.Add(buttonSave);
            buttonPanel.Controls.Add(buttonCancel);
            this.Controls.Add(buttonPanel);

            // Подсказка
            var tipLabel = new Label
            {
                Text = "* - обязательные поля",
                Location = new Point(10, y + 70),
                Size = new Size(200, 20),
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            this.Controls.Add(tipLabel);
        }

        private void UpdateDrugInfoFromDictionary()
        {
            if (DrugDictionary.CommonDrugs.ContainsKey(comboBoxName.Text))
            {
                var description = DrugDictionary.CommonDrugs[comboBoxName.Text];

                // ДОБАВЛЕНО: Автоматическое определение категории
                var categoryId = DrugDictionary.DetermineCategory(comboBoxName.Text);
                var category = _categoryService.GetCategory(categoryId);
                if (category != null)
                {
                    comboBoxCategory.Text = category.Name;
                }

                // Заполнение показаний, если поле пустое
                if (string.IsNullOrEmpty(textBoxIndications.Text))
                {
                    textBoxIndications.Text = description;
                }

                // Автоматическое определение формы выпуска
                if (comboBoxName.Text.Contains("таблет") && string.IsNullOrEmpty(comboBoxForm.Text))
                {
                    comboBoxForm.Text = "Таблетки";
                }
                else if ((comboBoxName.Text.Contains("мазь") || comboBoxName.Text.Contains("крем")) && string.IsNullOrEmpty(comboBoxForm.Text))
                {
                    comboBoxForm.Text = "Мазь";
                }
                else if (comboBoxName.Text.Contains("сироп") && string.IsNullOrEmpty(comboBoxForm.Text))
                {
                    comboBoxForm.Text = "Сироп";
                }
                else if ((comboBoxName.Text.Contains("капс") || comboBoxName.Text.Contains("капсул")) && string.IsNullOrEmpty(comboBoxForm.Text))
                {
                    comboBoxForm.Text = "Капсулы";
                }
                else if (comboBoxName.Text.Contains("раствор") && string.IsNullOrEmpty(comboBoxForm.Text))
                {
                    comboBoxForm.Text = "Раствор";
                }
                else if (comboBoxName.Text.Contains("инъекц") && string.IsNullOrEmpty(comboBoxForm.Text))
                {
                    comboBoxForm.Text = "Инъекции";
                }
            }
        }

        private void AddComboBoxControl(string labelText, ref int y, int labelWidth, out ComboBox comboBox, int controlWidth, int offsetY = 30)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(10, y),
                Width = labelWidth
            };
            comboBox = new ComboBox
            {
                Location = new Point(160, y),
                Width = controlWidth,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            y += offsetY;

            this.Controls.Add(label);
            this.Controls.Add(comboBox);
        }

        private void AddMultiLineControl(string labelText, ref int y, int labelWidth, out TextBox textBox, int controlWidth, int height)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(10, y),
                Width = labelWidth
            };
            textBox = new TextBox
            {
                Location = new Point(160, y),
                Width = controlWidth,
                Height = height,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            y += height + 10;

            this.Controls.Add(label);
            this.Controls.Add(textBox);
        }

        private void FillForm()
        {
            // ДОБАВЛЕНО: Заполнение категории
            var category = _categoryService.GetCategory(_drug.CategoryId);
            if (category != null)
            {
                comboBoxCategory.Text = category.Name;
            }
            else
            {
                comboBoxCategory.SelectedIndex = 0;
            }

            comboBoxName.Text = _drug.Name ?? "";
            comboBoxSubstance.Text = _drug.ActiveSubstance ?? "";
            comboBoxManufacturer.Text = _drug.Manufacturer ?? "";
            comboBoxForm.Text = _drug.Form ?? "";
            comboBoxDosage.Text = _drug.Dosage > 0 ? _drug.Dosage.ToString() : "";
            comboBoxUnit.Text = _drug.DosageUnit ?? "";
            comboBoxPrescription.Text = _drug.PrescriptionType ?? "";
            comboBoxQuantity.Text = _drug.Quantity > 0 ? _drug.Quantity.ToString() : "";

            if (_drug.ExpiryDate > DateTime.MinValue)
            {
                datePickerExpiry.Value = _drug.ExpiryDate;
            }
            else
            {
                datePickerExpiry.Value = DateTime.Now.AddYears(1);
            }

            if (_drug.Indications != null && _drug.Indications.Count > 0)
            {
                textBoxIndications.Text = string.Join(Environment.NewLine, _drug.Indications);
            }

            if (_drug.Contraindications != null && _drug.Contraindications.Count > 0)
            {
                textBoxContraindications.Text = string.Join(Environment.NewLine, _drug.Contraindications);
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                // ДОБАВЛЕНО: Сохранение категории
                var selectedCategory = _categoryService.GetCategories()
                    .FirstOrDefault(c => c.Name == comboBoxCategory.Text);
                _drug.CategoryId = selectedCategory?.Id ?? 1;

                _drug.Name = comboBoxName.Text.Trim();
                _drug.ActiveSubstance = comboBoxSubstance.Text.Trim();
                _drug.Manufacturer = comboBoxManufacturer.Text.Trim();
                _drug.Form = comboBoxForm.Text.Trim();

                if (decimal.TryParse(comboBoxDosage.Text, out decimal dosage))
                {
                    _drug.Dosage = dosage;
                }

                _drug.DosageUnit = comboBoxUnit.Text.Trim();
                _drug.PrescriptionType = comboBoxPrescription.Text.Trim();

                if (int.TryParse(comboBoxQuantity.Text, out int quantity))
                {
                    _drug.Quantity = quantity;
                }

                _drug.ExpiryDate = datePickerExpiry.Value;

                // Обработка многострочных полей
                _drug.Indications = textBoxIndications.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line))
                    .ToList();

                _drug.Contraindications = textBoxContraindications.Text
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line))
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
            // ДОБАВЛЕНО: Валидация категории
            if (string.IsNullOrWhiteSpace(comboBoxCategory.Text))
            {
                MessageBox.Show("Выберите категорию препарата", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBoxCategory.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(comboBoxName.Text))
            {
                MessageBox.Show("Введите название препарата", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBoxName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(comboBoxSubstance.Text))
            {
                MessageBox.Show("Введите действующее вещество", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBoxSubstance.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(comboBoxManufacturer.Text))
            {
                MessageBox.Show("Введите производителя", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBoxManufacturer.Focus();
                return false;
            }

            if (!decimal.TryParse(comboBoxDosage.Text, out decimal dosage) || dosage <= 0)
            {
                MessageBox.Show("Введите корректную дозировку", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBoxDosage.Focus();
                return false;
            }

            if (!int.TryParse(comboBoxQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBoxQuantity.Focus();
                return false;
            }

            if (datePickerExpiry.Value <= DateTime.Now)
            {
                MessageBox.Show("Срок годности должен быть в будущем", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                datePickerExpiry.Focus();
                return false;
            }

            return true;
        }

        // ДОБАВЛЕНО: Обработка нажатия клавиш
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Enter:
                    if (buttonSave.Focused || buttonCancel.Focused)
                    {
                        return base.ProcessCmdKey(ref msg, keyData);
                    }
                    else
                    {
                        // Переход к следующему контролу при нажатии Enter
                        this.SelectNextControl(this.ActiveControl, true, true, true, true);
                        return true;
                    }
                case Keys.Escape:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return true;
                case Keys.Control | Keys.S:
                    ButtonSave_Click(null, null);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}