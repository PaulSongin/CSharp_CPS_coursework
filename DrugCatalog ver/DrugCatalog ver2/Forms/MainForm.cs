using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DrugCatalog_ver.Forms
{
    public partial class MainForm : Form
    {
        private List<Drug> _drugs;
        private readonly IXmlDataService _dataService;

        private DataGridView dataGridViewDrugs;
        private TextBox textBoxSearch;
        private Button buttonAdd;
        private Button buttonEdit;
        private Button buttonDelete;
        private Button buttonSearch;
        private Button buttonExpiring;
        private Button buttonRefresh;

        public MainForm()
        {
            InitializeComponent();
            _dataService = new XmlDataService();
            LoadDrugs();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Настройка формы
            this.Text = "Каталог лекарственных препаратов";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Microsoft Sans Serif", 9f);

            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            // Панель поиска
            var panelSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.LightGray
            };

            // Метка поиска
            var labelSearch = new Label
            {
                Text = "Поиск:",
                Location = new Point(10, 18),
                Size = new Size(40, 20)
            };

            textBoxSearch = new TextBox
            {
                Location = new Point(55, 15),
                Width = 200,
                Text = "Введите для поиска..."
            };

            // Обработчики для placeholder эффекта
            textBoxSearch.Enter += (s, e) => {
                if (textBoxSearch.Text == "Введите для поиска...")
                {
                    textBoxSearch.Text = "";
                    textBoxSearch.ForeColor = Color.Black;
                }
            };

            textBoxSearch.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(textBoxSearch.Text))
                {
                    textBoxSearch.Text = "Введите для поиска...";
                    textBoxSearch.ForeColor = Color.Gray;
                }
            };
            textBoxSearch.ForeColor = Color.Gray;

            buttonSearch = new Button
            {
                Text = "Поиск",
                Location = new Point(265, 15),
                Size = new Size(80, 25),
                BackColor = Color.LightBlue
            };
            buttonSearch.Click += ButtonSearch_Click;

            buttonExpiring = new Button
            {
                Text = "Срок годности",
                Location = new Point(355, 15),
                Size = new Size(120, 25),
                BackColor = Color.LightYellow
            };
            buttonExpiring.Click += ButtonExpiring_Click;

            panelSearch.Controls.AddRange(new Control[] {
                labelSearch, textBoxSearch, buttonSearch, buttonExpiring
            });

            // Панель кнопок действий
            var panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.LightGray
            };

            buttonAdd = new Button
            {
                Text = "Добавить",
                Location = new Point(10, 15),
                Size = new Size(80, 25),
                BackColor = Color.LightGreen
            };
            buttonAdd.Click += ButtonAdd_Click;

            buttonEdit = new Button
            {
                Text = "Редактировать",
                Location = new Point(100, 15),
                Size = new Size(100, 25),
                BackColor = Color.LightBlue
            };
            buttonEdit.Click += ButtonEdit_Click;

            buttonDelete = new Button
            {
                Text = "Удалить",
                Location = new Point(210, 15),
                Size = new Size(80, 25),
                BackColor = Color.LightCoral
            };
            buttonDelete.Click += ButtonDelete_Click;

            buttonRefresh = new Button
            {
                Text = "Обновить",
                Location = new Point(300, 15),
                Size = new Size(80, 25),
                BackColor = Color.LightGoldenrodYellow
            };
            buttonRefresh.Click += (s, e) => LoadDrugs();

            panelButtons.Controls.AddRange(new Control[] {
                buttonAdd, buttonEdit, buttonDelete, buttonRefresh
            });

            // DataGridView для отображения препаратов
            dataGridViewDrugs = new DataGridView
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

            // Добавление всех элементов на форму
            this.Controls.AddRange(new Control[] {
                dataGridViewDrugs, panelSearch, panelButtons
            });
        }

        private void LoadDrugs()
        {
            try
            {
                _drugs = _dataService.LoadDrugs();
                RefreshDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshDataGrid()
        {
            dataGridViewDrugs.Columns.Clear();

            // Создание колонок
            dataGridViewDrugs.Columns.Add("Id", "ID");
            dataGridViewDrugs.Columns.Add("Name", "Название");
            dataGridViewDrugs.Columns.Add("ActiveSubstance", "Действующее вещество");
            dataGridViewDrugs.Columns.Add("Manufacturer", "Производитель");
            dataGridViewDrugs.Columns.Add("Form", "Форма");
            dataGridViewDrugs.Columns.Add("Dosage", "Дозировка");
            dataGridViewDrugs.Columns.Add("Price", "Цена");
            dataGridViewDrugs.Columns.Add("Quantity", "Кол-во");
            dataGridViewDrugs.Columns.Add("ExpiryDate", "Срок годности");

            // Настройка ширины колонок
            dataGridViewDrugs.Columns["Id"].Width = 40;
            dataGridViewDrugs.Columns["Name"].Width = 120;
            dataGridViewDrugs.Columns["ActiveSubstance"].Width = 130;
            dataGridViewDrugs.Columns["Manufacturer"].Width = 110;
            dataGridViewDrugs.Columns["Form"].Width = 80;
            dataGridViewDrugs.Columns["Dosage"].Width = 70;
            dataGridViewDrugs.Columns["Price"].Width = 70;
            dataGridViewDrugs.Columns["Quantity"].Width = 50;
            dataGridViewDrugs.Columns["ExpiryDate"].Width = 90;

            // Заполнение данными
            dataGridViewDrugs.Rows.Clear();
            foreach (var drug in _drugs)
            {
                dataGridViewDrugs.Rows.Add(
                    drug.Id,
                    drug.Name,
                    drug.ActiveSubstance,
                    drug.Manufacturer,
                    drug.Form,
                    $"{drug.Dosage} {drug.DosageUnit}",
                    $"{drug.Price:C}",
                    drug.Quantity,
                    drug.ExpiryDate.ToString("dd.MM.yyyy")
                );
            }
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            var addForm = new AddEditDrugForm(_dataService);
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadDrugs();
            }
        }

        private void ButtonEdit_Click(object sender, EventArgs e)
        {
            if (dataGridViewDrugs.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите препарат для редактирования", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedId = (int)dataGridViewDrugs.SelectedRows[0].Cells["Id"].Value;
            var drug = _drugs.FirstOrDefault(d => d.Id == selectedId);

            if (drug != null)
            {
                var editForm = new AddEditDrugForm(_dataService, drug);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadDrugs();
                }
            }
        }

        private void ButtonDelete_Click(object sender, EventArgs e)
        {
            if (dataGridViewDrugs.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите препарат для удаления", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedId = (int)dataGridViewDrugs.SelectedRows[0].Cells["Id"].Value;
            var drug = _drugs.FirstOrDefault(d => d.Id == selectedId);

            if (drug != null)
            {
                var result = MessageBox.Show(
                    $"Удалить препарат '{drug.Name}'?",
                    "Подтверждение удаления",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    _drugs.Remove(drug);
                    _dataService.SaveDrugs(_drugs);
                    LoadDrugs();
                    MessageBox.Show("Препарат удален", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ButtonSearch_Click(object sender, EventArgs e)
        {
            var searchText = textBoxSearch.Text;
            if (searchText == "Введите для поиска...")
            {
                RefreshDataGrid();
                return;
            }

            var searchTerm = searchText.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                RefreshDataGrid();
                return;
            }

            var filteredDrugs = _drugs.Where(d =>
                (d.Name != null && d.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.ActiveSubstance != null && d.ActiveSubstance.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.Manufacturer != null && d.Manufacturer.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();

            dataGridViewDrugs.Rows.Clear();
            foreach (var drug in filteredDrugs)
            {
                dataGridViewDrugs.Rows.Add(
                    drug.Id,
                    drug.Name,
                    drug.ActiveSubstance,
                    drug.Manufacturer,
                    drug.Form,
                    $"{drug.Dosage} {drug.DosageUnit}",
                    $"{drug.Price:C}",
                    drug.Quantity,
                    drug.ExpiryDate.ToString("dd.MM.yyyy")
                );
            }
        }

        private void ButtonExpiring_Click(object sender, EventArgs e)
        {
            var expiringDrugs = _drugs.Where(d => d.ExpiryDate <= DateTime.Now.AddDays(30)).ToList();

            if (expiringDrugs.Count == 0)
            {
                MessageBox.Show("Нет препаратов с истекающим сроком годности", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dataGridViewDrugs.Rows.Clear();
            foreach (var drug in expiringDrugs)
            {
                dataGridViewDrugs.Rows.Add(
                    drug.Id,
                    drug.Name,
                    drug.ActiveSubstance,
                    drug.Manufacturer,
                    drug.Form,
                    $"{drug.Dosage} {drug.DosageUnit}",
                    $"{drug.Price:C}",
                    drug.Quantity,
                    drug.ExpiryDate.ToString("dd.MM.yyyy")
                );
            }

            MessageBox.Show($"Найдено {expiringDrugs.Count} препаратов с истекающим сроком годности",
                "Результат", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}