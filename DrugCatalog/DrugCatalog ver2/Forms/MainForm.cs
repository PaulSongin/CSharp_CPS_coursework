using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace DrugCatalog_ver2.Forms
{
    public partial class MainForm : Form
    {
        private List<Drug> _drugs;
        private readonly IXmlDataService _dataService;
        private readonly ICategoryService _categoryService;
        private string _currentFilePath;
        private bool _autoDeleteEnabled = true;

        private TabControl tabControl;
        private DataGridView dataGridViewAllDrugs;
        private DataGridView dataGridViewExpiring;
        private DataGridView dataGridViewByManufacturer;
        private DataGridView dataGridViewByCategory;
        private ComboBox comboBoxManufacturers;
        private ComboBox comboBoxCategories;
        private TextBox textBoxSearch;

        private Button buttonAutoDeleteAll, buttonCleanupAll;
        private Button buttonAutoDeleteExpiring, buttonCleanupExpiring;
        private Button buttonAutoDeleteManufacturer, buttonCleanupManufacturer;
        private Button buttonAutoDeleteCategory, buttonCleanupCategory;

        private ContextMenuStrip contextMenuGrid;
        private MenuStrip mainMenuStrip;

        public MainForm()
        {
            InitializeComponent();
            _dataService = new XmlDataService();
            _categoryService = new CategoryService();
            _currentFilePath = null;

            LoadDrugs();
            UpdateWindowTitle();
            CheckExpiredDrugsOnStartup();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Каталог лекарственных препаратов";
            this.Size = new Size(1300, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Microsoft Sans Serif", 9f);

            CreateMainMenu();
            CreateContextMenus();
            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateMainMenu()
        {
            mainMenuStrip = new MenuStrip();
            mainMenuStrip.Dock = DockStyle.Top;

            var fileMenu = new ToolStripMenuItem("Файл");

            var newFileMenuItem = new ToolStripMenuItem("Новый", null, (s, e) => CreateNewFile());
            newFileMenuItem.ShortcutKeys = Keys.Control | Keys.N;

            var openMenuItem = new ToolStripMenuItem("Открыть...", null, (s, e) => LoadFromXmlFile());
            openMenuItem.ShortcutKeys = Keys.Control | Keys.O;

            var saveMenuItem = new ToolStripMenuItem("Сохранить", null, (s, e) => SaveToXmlFile());
            saveMenuItem.ShortcutKeys = Keys.Control | Keys.S;

            var saveAsMenuItem = new ToolStripMenuItem("Сохранить как...", null, (s, e) => SaveAsToXmlFile());

            var separator = new ToolStripSeparator();

            var exitMenuItem = new ToolStripMenuItem("Выход", null, (s, e) => this.Close());
            exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;

            fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
                newFileMenuItem,
                openMenuItem,
                new ToolStripSeparator(),
                saveMenuItem,
                saveAsMenuItem,
                new ToolStripSeparator(),
                exitMenuItem
            });

            var editMenu = new ToolStripMenuItem("Правка");

            var newDrugMenuItem = new ToolStripMenuItem("Новый препарат", null, (s, e) => AddDrug());
            newDrugMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.N;

            var editDrugMenuItem = new ToolStripMenuItem("Редактировать препарат", null, (s, e) => EditSelectedDrug());
            editDrugMenuItem.ShortcutKeys = Keys.Control | Keys.E;

            var deleteDrugMenuItem = new ToolStripMenuItem("Удалить препарат", null, (s, e) => DeleteSelectedDrug());
            deleteDrugMenuItem.ShortcutKeys = Keys.Delete;

            var cleanupMenuItem = new ToolStripMenuItem("Очистить просроченные", null, (s, e) => CleanupExpiredDrugs());
            cleanupMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.Delete;

            editMenu.DropDownItems.AddRange(new ToolStripItem[] {
                newDrugMenuItem,
                new ToolStripSeparator(),
                editDrugMenuItem,
                deleteDrugMenuItem,
                new ToolStripSeparator(),
                cleanupMenuItem
            });

            var viewMenu = new ToolStripMenuItem("Вид");

            var refreshMenuItem = new ToolStripMenuItem("Обновить", null, (s, e) => LoadDrugs());
            refreshMenuItem.ShortcutKeys = Keys.F5;

            var searchMenuItem = new ToolStripMenuItem("Поиск", null, (s, e) => textBoxSearch.Focus());
            searchMenuItem.ShortcutKeys = Keys.Control | Keys.F;

            var viewSeparator = new ToolStripSeparator();

            var viewAllMenuItem = new ToolStripMenuItem("Все препараты", null, (s, e) => tabControl.SelectedIndex = 0);
            var viewExpiringMenuItem = new ToolStripMenuItem("С истекающим сроком", null, (s, e) => tabControl.SelectedIndex = 1);
            var viewByManufacturerMenuItem = new ToolStripMenuItem("По производителям", null, (s, e) => tabControl.SelectedIndex = 2);
            var viewByCategoryMenuItem = new ToolStripMenuItem("По категориям", null, (s, e) => tabControl.SelectedIndex = 3);

            viewMenu.DropDownItems.AddRange(new ToolStripItem[] {
                refreshMenuItem,
                searchMenuItem,
                viewSeparator,
                viewAllMenuItem,
                viewExpiringMenuItem,
                viewByManufacturerMenuItem,
                viewByCategoryMenuItem
            });

            mainMenuStrip.Items.AddRange(new ToolStripItem[] {
                fileMenu,
                editMenu,
                viewMenu
            });

            this.MainMenuStrip = mainMenuStrip;
            this.Controls.Add(mainMenuStrip);
        }

        private void CreateContextMenus()
        {
            contextMenuGrid = new ContextMenuStrip();

            var addToolStripMenuItem = new ToolStripMenuItem("➕ Добавить препарат");
            addToolStripMenuItem.Click += (s, e) => AddDrug();
            addToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;

            var editToolStripMenuItem = new ToolStripMenuItem("✏️ Редактировать препарат");
            editToolStripMenuItem.Click += (s, e) => EditSelectedDrug();
            editToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.E;

            var deleteToolStripMenuItem = new ToolStripMenuItem("❌ Удалить препарат");
            deleteToolStripMenuItem.Click += (s, e) => DeleteSelectedDrug();
            deleteToolStripMenuItem.ShortcutKeys = Keys.Delete;

            var cleanupToolStripMenuItem = new ToolStripMenuItem("🧹 Очистить просроченные");
            cleanupToolStripMenuItem.Click += (s, e) => CleanupExpiredDrugs();
            cleanupToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.Delete;

            var refreshToolStripMenuItem = new ToolStripMenuItem("🔄 Обновить");
            refreshToolStripMenuItem.Click += (s, e) => LoadDrugs();
            refreshToolStripMenuItem.ShortcutKeys = Keys.F5;

            contextMenuGrid.Items.AddRange(new ToolStripItem[] {
                addToolStripMenuItem,
                new ToolStripSeparator(),
                editToolStripMenuItem,
                deleteToolStripMenuItem,
                new ToolStripSeparator(),
                cleanupToolStripMenuItem,
                new ToolStripSeparator(),
                refreshToolStripMenuItem
            });

            contextMenuGrid.Opening += (s, e) =>
            {
                var dataGridView = GetCurrentDataGridView();
                bool hasSelection = dataGridView != null && dataGridView.SelectedRows.Count > 0;

                editToolStripMenuItem.Enabled = hasSelection;
                deleteToolStripMenuItem.Enabled = hasSelection;
            };
        }

        private void CreateControls()
        {
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5),
                Location = new Point(0, mainMenuStrip.Height + 5)
            };

            var panelSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.LightSteelBlue,
                Padding = new Padding(10, 20, 10, 10),
                Margin = new Padding(0, 10, 0, 0)
            };

            var labelSearch = new Label
            {
                Text = "Поиск препаратов:",
                Location = new Point(10, 25),
                Size = new Size(120, 20),
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold)
            };

            textBoxSearch = new TextBox
            {
                Location = new Point(140, 22),
                Width = 300,
                Height = 25,
                Text = "Введите название, вещество или производителя...",
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular)
            };

            textBoxSearch.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBoxSearch.AutoCompleteSource = AutoCompleteSource.CustomSource;
            var autoCompleteCollection = new AutoCompleteStringCollection();
            textBoxSearch.AutoCompleteCustomSource = autoCompleteCollection;

            foreach (var drug in DrugDictionary.CommonDrugs.Keys)
            {
                autoCompleteCollection.Add(drug);
            }

            textBoxSearch.Enter += (s, e) => {
                if (textBoxSearch.Text == "Введите название, вещество или производителя...")
                {
                    textBoxSearch.Text = "";
                    textBoxSearch.ForeColor = Color.Black;
                }
            };

            textBoxSearch.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(textBoxSearch.Text))
                {
                    textBoxSearch.Text = "Введите название, вещество или производителя...";
                    textBoxSearch.ForeColor = Color.Gray;
                }
            };
            textBoxSearch.ForeColor = Color.Gray;
            textBoxSearch.KeyPress += (s, e) => {
                if (e.KeyChar == (char)Keys.Enter)
                    SearchDrugs();
            };

            var buttonSearch = new Button
            {
                Text = "Найти",
                Location = new Point(450, 22),
                Size = new Size(80, 25),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonSearch.Click += (s, e) => SearchDrugs();

            var buttonClearSearch = new Button
            {
                Text = "Сброс",
                Location = new Point(540, 22),
                Size = new Size(70, 25),
                BackColor = Color.LightSlateGray,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonClearSearch.Click += (s, e) =>
            {
                textBoxSearch.Text = "Введите название, вещество или производителя...";
                textBoxSearch.ForeColor = Color.Gray;
                RefreshDataGrid(dataGridViewAllDrugs, _drugs ?? new List<Drug>());
            };

            panelSearch.Controls.AddRange(new Control[] {
                labelSearch, textBoxSearch, buttonSearch, buttonClearSearch
            });

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.Normal,
                SizeMode = TabSizeMode.Fixed,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular)
            };

            tabControl.ItemSize = new Size(180, 25);

            // Вкладка 1: Все препараты
            var tabAllDrugs = new TabPage("Все препараты");
            var allDrugsPanel = new Panel { Dock = DockStyle.Fill };

            var panelAllDrugsControls = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.LightSteelBlue,
                Padding = new Padding(10, 5, 10, 5)
            };

            buttonAutoDeleteAll = new Button
            {
                Text = _autoDeleteEnabled ? "✅ Автоудаление ВКЛ" : "❌ Автоудаление ВЫКЛ",
                Size = new Size(140, 25),
                Location = new Point(10, 5),
                BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral,
                ForeColor = Color.Black,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonAutoDeleteAll.Click += (s, e) => ToggleAutoDelete();

            buttonCleanupAll = new Button
            {
                Text = "🧹 Очистить просроченные",
                Size = new Size(150, 25),
                Location = new Point(160, 5),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonCleanupAll.Click += (s, e) => CleanupExpiredDrugs();

            panelAllDrugsControls.Controls.AddRange(new Control[] {
                buttonAutoDeleteAll, buttonCleanupAll
            });

            dataGridViewAllDrugs = CreateDataGridView();

            allDrugsPanel.Controls.Add(dataGridViewAllDrugs);
            allDrugsPanel.Controls.Add(panelAllDrugsControls);
            tabAllDrugs.Controls.Add(allDrugsPanel);

            // Вкладка 2: С истекающим сроком
            var tabExpiring = new TabPage("С истекающим сроком");
            var expiringPanel = new Panel { Dock = DockStyle.Fill };

            var panelExpiringControls = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.LightSteelBlue,
                Padding = new Padding(10, 5, 10, 5)
            };

            buttonAutoDeleteExpiring = new Button
            {
                Text = _autoDeleteEnabled ? "✅ Автоудаление ВКЛ" : "❌ Автоудаление ВЫКЛ",
                Size = new Size(140, 25),
                Location = new Point(10, 5),
                BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral,
                ForeColor = Color.Black,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonAutoDeleteExpiring.Click += (s, e) => ToggleAutoDelete();

            buttonCleanupExpiring = new Button
            {
                Text = "🧹 Очистить просроченные",
                Size = new Size(150, 25),
                Location = new Point(160, 5),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonCleanupExpiring.Click += (s, e) => CleanupExpiredDrugs();

            panelExpiringControls.Controls.AddRange(new Control[] {
                buttonAutoDeleteExpiring, buttonCleanupExpiring
            });

            dataGridViewExpiring = CreateDataGridView();

            expiringPanel.Controls.Add(dataGridViewExpiring);
            expiringPanel.Controls.Add(panelExpiringControls);
            tabExpiring.Controls.Add(expiringPanel);

            // Вкладка 3: По производителям
            var tabByManufacturer = new TabPage("По производителям");
            var manufacturerPanel = new Panel { Dock = DockStyle.Fill };

            var panelManufacturerFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.LightSteelBlue,
                Padding = new Padding(10)
            };

            var labelManufacturer = new Label
            {
                Text = "Выберите производителя:",
                Location = new Point(10, 15),
                Size = new Size(150, 20),
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold)
            };

            comboBoxManufacturers = new ComboBox
            {
                Location = new Point(170, 12),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular)
            };
            comboBoxManufacturers.SelectedIndexChanged += (s, e) => FilterByManufacturer();

            var panelManufacturerControls = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.LightSteelBlue,
                Padding = new Padding(10, 5, 10, 5)
            };

            buttonAutoDeleteManufacturer = new Button
            {
                Text = _autoDeleteEnabled ? "✅ Автоудаление ВКЛ" : "❌ Автоудаление ВЫКЛ",
                Size = new Size(140, 25),
                Location = new Point(10, 5),
                BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral,
                ForeColor = Color.Black,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonAutoDeleteManufacturer.Click += (s, e) => ToggleAutoDelete();

            buttonCleanupManufacturer = new Button
            {
                Text = "🧹 Очистить просроченные",
                Size = new Size(150, 25),
                Location = new Point(160, 5),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonCleanupManufacturer.Click += (s, e) => CleanupExpiredDrugs();

            panelManufacturerControls.Controls.AddRange(new Control[] {
                buttonAutoDeleteManufacturer, buttonCleanupManufacturer
            });

            panelManufacturerFilter.Controls.AddRange(new Control[] {
                labelManufacturer, comboBoxManufacturers
            });

            dataGridViewByManufacturer = CreateDataGridView();

            manufacturerPanel.Controls.Add(dataGridViewByManufacturer);
            manufacturerPanel.Controls.Add(panelManufacturerControls);
            manufacturerPanel.Controls.Add(panelManufacturerFilter);
            tabByManufacturer.Controls.Add(manufacturerPanel);

            // Вкладка 4: По категориям (НОВАЯ ВКЛАДКА)
            var tabByCategory = new TabPage("По категориям");
            var categoryPanel = new Panel { Dock = DockStyle.Fill };

            var panelCategoryFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.LightSteelBlue,
                Padding = new Padding(10)
            };

            var labelCategory = new Label
            {
                Text = "Выберите категорию:",
                Location = new Point(10, 15),
                Size = new Size(120, 20),
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold)
            };

            comboBoxCategories = new ComboBox
            {
                Location = new Point(140, 12),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular)
            };
            comboBoxCategories.SelectedIndexChanged += (s, e) => FilterByCategory();

            var panelCategoryControls = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.LightSteelBlue,
                Padding = new Padding(10, 5, 10, 5)
            };

            buttonAutoDeleteCategory = new Button
            {
                Text = _autoDeleteEnabled ? "✅ Автоудаление ВКЛ" : "❌ Автоудаление ВЫКЛ",
                Size = new Size(140, 25),
                Location = new Point(10, 5),
                BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral,
                ForeColor = Color.Black,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonAutoDeleteCategory.Click += (s, e) => ToggleAutoDelete();

            buttonCleanupCategory = new Button
            {
                Text = "🧹 Очистить просроченные",
                Size = new Size(150, 25),
                Location = new Point(160, 5),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonCleanupCategory.Click += (s, e) => CleanupExpiredDrugs();

            panelCategoryControls.Controls.AddRange(new Control[] {
                buttonAutoDeleteCategory, buttonCleanupCategory
            });

            panelCategoryFilter.Controls.AddRange(new Control[] {
                labelCategory, comboBoxCategories
            });

            dataGridViewByCategory = CreateDataGridView();

            categoryPanel.Controls.Add(dataGridViewByCategory);
            categoryPanel.Controls.Add(panelCategoryControls);
            categoryPanel.Controls.Add(panelCategoryFilter);
            tabByCategory.Controls.Add(categoryPanel);

            tabControl.TabPages.Add(tabAllDrugs);
            tabControl.TabPages.Add(tabExpiring);
            tabControl.TabPages.Add(tabByManufacturer);
            tabControl.TabPages.Add(tabByCategory);

            dataGridViewAllDrugs.DoubleClick += (s, e) => EditSelectedDrug();
            dataGridViewExpiring.DoubleClick += (s, e) => EditSelectedDrug();
            dataGridViewByManufacturer.DoubleClick += (s, e) => EditSelectedDrug();
            dataGridViewByCategory.DoubleClick += (s, e) => EditSelectedDrug();

            contentPanel.Controls.Add(tabControl);
            mainPanel.Controls.Add(contentPanel);
            mainPanel.Controls.Add(panelSearch);

            this.Controls.Add(mainPanel);
        }

        private DataGridView CreateDataGridView()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                ContextMenuStrip = contextMenuGrid,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.AliceBlue
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.SteelBlue,
                    ForeColor = Color.White,
                    Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold)
                }
            };
        }

        private void LoadDrugs()
        {
            try
            {
                _drugs = _dataService.LoadDrugs();

                if (_autoDeleteEnabled)
                {
                    AutoDeleteExpiredDrugs();
                }

                RefreshAllTabs();
                UpdateSearchAutoComplete();
                UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckExpiredDrugsOnStartup()
        {
            if (_drugs == null || _drugs.Count == 0) return;

            var expiredDrugs = _drugs.Where(d => d.ExpiryDate < DateTime.Now).ToList();
            if (expiredDrugs.Count > 0 && _autoDeleteEnabled)
            {
                var result = MessageBox.Show(
                    $"Обнаружено {expiredDrugs.Count} просроченных препаратов. Удалить их автоматически?\n\n" +
                    "Автоудаление включено. Вы можете отключить его кнопкой 'Автоудаление'.",
                    "Обнаружены просроченные препараты",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    PerformAutoDelete(expiredDrugs);
                }
            }
        }

        private void AutoDeleteExpiredDrugs()
        {
            if (_drugs == null || _drugs.Count == 0) return;

            var expiredDrugs = _drugs.Where(d => d.ExpiryDate < DateTime.Now).ToList();
            if (expiredDrugs.Count > 0)
            {
                PerformAutoDelete(expiredDrugs);
            }
        }

        private void PerformAutoDelete(List<Drug> expiredDrugs)
        {
            int deletedCount = 0;
            foreach (var expiredDrug in expiredDrugs)
            {
                _drugs.Remove(expiredDrug);
                deletedCount++;
            }

            _dataService.SaveDrugs(_drugs);

            if (deletedCount > 0)
            {
                RefreshAllTabs();
                UpdateWindowTitle();

                this.Text = $"Каталог лекарственных препаратов - Автоудалено {deletedCount} просроченных";

                var timer = new Timer();
                timer.Interval = 3000;
                timer.Tick += (s, e) =>
                {
                    UpdateWindowTitle();
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private void ToggleAutoDelete()
        {
            _autoDeleteEnabled = !_autoDeleteEnabled;
            UpdateAutoDeleteButtons();

            string message = _autoDeleteEnabled ?
                "Автоудаление включено. Просроченные препараты будут удаляться автоматически при загрузке данных." :
                "Автоудаление отключено. Просроченные препараты будут сохраняться в базе данных.";

            MessageBox.Show(message, "Автоудаление",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateAutoDeleteButtons()
        {
            if (buttonAutoDeleteAll != null)
            {
                buttonAutoDeleteAll.Text = _autoDeleteEnabled ? "✅ Автоудаление ВКЛ" : "❌ Автоудаление ВЫКЛ";
                buttonAutoDeleteAll.BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral;
            }

            if (buttonAutoDeleteExpiring != null)
            {
                buttonAutoDeleteExpiring.Text = _autoDeleteEnabled ? "✅ Автоудаление ВКЛ" : "❌ Автоудаление ВЫКЛ";
                buttonAutoDeleteExpiring.BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral;
            }

            if (buttonAutoDeleteManufacturer != null)
            {
                buttonAutoDeleteManufacturer.Text = _autoDeleteEnabled ? "✅ Автоудаление ВКЛ" : "❌ Автоудаление ВЫКЛ";
                buttonAutoDeleteManufacturer.BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral;
            }

            if (buttonAutoDeleteCategory != null)
            {
                buttonAutoDeleteCategory.Text = _autoDeleteEnabled ? "✅ Автоудаление ВКЛ" : "❌ Автоудаление ВЫКЛ";
                buttonAutoDeleteCategory.BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral;
            }
        }

        private void CleanupExpiredDrugs()
        {
            if (_drugs == null || _drugs.Count == 0)
            {
                MessageBox.Show("Нет данных для очистки", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var expiredDrugs = _drugs.Where(d => d.ExpiryDate < DateTime.Now).ToList();

            if (expiredDrugs.Count == 0)
            {
                MessageBox.Show("Просроченных препаратов не найдено", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Найдено {expiredDrugs.Count} просроченных препаратов:\n\n" +
                string.Join("\n", expiredDrugs.Select(d => $"- {d.Name} (годен до: {d.ExpiryDate:dd.MM.yyyy})")) +
                "\n\nУдалить все просроченные препараты?",
                "Очистка просроченных препаратов",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                foreach (var drug in expiredDrugs)
                {
                    _drugs.Remove(drug);
                }

                _dataService.SaveDrugs(_drugs);
                LoadDrugs();

                MessageBox.Show($"Удалено {expiredDrugs.Count} просроченных препаратов", "Очистка завершена",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateWindowTitle()
        {
            string fileName = _currentFilePath != null ? Path.GetFileName(_currentFilePath) : "Новый файл";
            string autoDeleteStatus = _autoDeleteEnabled ? " [АВТОУДАЛЕНИЕ ВКЛ]" : " [АВТОУДАЛЕНИЕ ВЫКЛ]";
            this.Text = $"Каталог лекарственных препаратов - {fileName} ({_drugs?.Count ?? 0} препаратов){autoDeleteStatus}";
        }

        private void UpdateSearchAutoComplete()
        {
            if (_drugs == null) return;

            var autoCompleteCollection = new AutoCompleteStringCollection();

            foreach (var drug in DrugDictionary.CommonDrugs.Keys)
            {
                autoCompleteCollection.Add(drug);
            }

            foreach (var drug in _drugs)
            {
                if (!string.IsNullOrWhiteSpace(drug.Name))
                    autoCompleteCollection.Add(drug.Name);

                if (!string.IsNullOrWhiteSpace(drug.ActiveSubstance))
                    autoCompleteCollection.Add(drug.ActiveSubstance);

                if (!string.IsNullOrWhiteSpace(drug.Manufacturer))
                    autoCompleteCollection.Add(drug.Manufacturer);
            }

            textBoxSearch.AutoCompleteCustomSource = autoCompleteCollection;
        }

        private void RefreshAllTabs()
        {
            var drugs = _drugs ?? new List<Drug>();

            RefreshDataGrid(dataGridViewAllDrugs, drugs);

            var expiringDrugs = drugs.Where(d => d.ExpiryDate <= DateTime.Now.AddDays(30)).ToList();
            RefreshDataGrid(dataGridViewExpiring, expiringDrugs);

            comboBoxManufacturers.Items.Clear();
            var manufacturers = drugs.Select(d => d.Manufacturer)
                                     .Where(m => !string.IsNullOrEmpty(m))
                                     .Distinct()
                                     .OrderBy(m => m)
                                     .ToArray();
            comboBoxManufacturers.Items.AddRange(manufacturers);

            comboBoxCategories.Items.Clear();
            var categories = _categoryService.GetCategories();
            foreach (var category in categories)
            {
                comboBoxCategories.Items.Add(category.Name);
            }

            if (comboBoxManufacturers.Items.Count > 0)
                comboBoxManufacturers.SelectedIndex = 0;

            if (comboBoxCategories.Items.Count > 0)
                comboBoxCategories.SelectedIndex = 0;

            tabControl.TabPages[0].Text = $"Все препараты ({drugs.Count})";
            tabControl.TabPages[1].Text = $"С истекающим сроком ({expiringDrugs.Count})";
            tabControl.TabPages[2].Text = $"По производителям ({manufacturers.Length})";
            tabControl.TabPages[3].Text = $"По категориям ({categories.Count})";
        }

        private void RefreshDataGrid(DataGridView dataGridView, List<Drug> drugs)
        {
            dataGridView.Columns.Clear();

            dataGridView.Columns.Add("Category", "Категория");
            dataGridView.Columns.Add("Name", "Название");
            dataGridView.Columns.Add("ActiveSubstance", "Действующее вещество");
            dataGridView.Columns.Add("Manufacturer", "Производитель");
            dataGridView.Columns.Add("Form", "Форма выпуска");
            dataGridView.Columns.Add("Dosage", "Дозировка");
            dataGridView.Columns.Add("Quantity", "Количество");
            dataGridView.Columns.Add("ExpiryDate", "Срок годности");

            var idColumn = new DataGridViewTextBoxColumn
            {
                Name = "Id",
                Visible = false
            };
            dataGridView.Columns.Add(idColumn);

            dataGridView.Columns["Category"].Width = 120;
            dataGridView.Columns["Name"].Width = 130;
            dataGridView.Columns["ActiveSubstance"].Width = 160;
            dataGridView.Columns["Manufacturer"].Width = 120;
            dataGridView.Columns["Form"].Width = 100;
            dataGridView.Columns["Dosage"].Width = 80;
            dataGridView.Columns["Quantity"].Width = 70;
            dataGridView.Columns["ExpiryDate"].Width = 100;

            dataGridView.Rows.Clear();
            foreach (var drug in drugs)
            {
                var category = _categoryService.GetCategory(drug.CategoryId);
                var categoryName = category?.Name ?? "Другое";

                dataGridView.Rows.Add(
                    categoryName,
                    drug.Name,
                    drug.ActiveSubstance,
                    drug.Manufacturer,
                    drug.Form,
                    $"{drug.Dosage} {drug.DosageUnit}",
                    drug.Quantity,
                    drug.ExpiryDate.ToString("dd.MM.yyyy"),
                    drug.Id
                );
            }

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                var drugId = (int)row.Cells["Id"].Value;
                var drug = drugs.FirstOrDefault(d => d.Id == drugId);

                if (drug != null)
                {
                    var categoryColor = _categoryService.GetCategoryColor(drug.CategoryId);
                    row.DefaultCellStyle.BackColor = categoryColor;

                    var expiryDate = drug.ExpiryDate;
                    if (expiryDate < DateTime.Now)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                        row.DefaultCellStyle.SelectionBackColor = Color.Red;
                    }
                    else if (expiryDate <= DateTime.Now.AddDays(30))
                    {
                        row.DefaultCellStyle.BackColor = Color.LightYellow;
                        row.DefaultCellStyle.SelectionBackColor = Color.Orange;
                    }
                    else
                    {
                        row.DefaultCellStyle.SelectionBackColor = Color.DarkBlue;
                    }
                }
            }
        }

        private void CreateNewFile()
        {
            var result = MessageBox.Show(
                "Создать новый файл? Текущие несохраненные данные будут потеряны.",
                "Создание нового файла",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _drugs = new List<Drug>();
                _currentFilePath = null;
                _dataService.SaveDrugs(_drugs);
                RefreshAllTabs();
                UpdateWindowTitle();

                MessageBox.Show("Создан новый пустой файл", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AddDrug()
        {
            var addForm = new AddEditDrugForm(_dataService, _categoryService, _drugs);
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadDrugs();
            }
        }

        private void EditSelectedDrug()
        {
            var dataGridView = GetCurrentDataGridView();
            if (dataGridView == null || dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите препарат для редактирования", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedId = (int)dataGridView.SelectedRows[0].Cells["Id"].Value;
            var drug = _drugs.FirstOrDefault(d => d.Id == selectedId);

            if (drug != null)
            {
                var editForm = new AddEditDrugForm(_dataService, _categoryService, _drugs, drug);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadDrugs();
                }
            }
        }

        private void DeleteSelectedDrug()
        {
            var dataGridView = GetCurrentDataGridView();
            if (dataGridView == null || dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите препарат для удаления", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedId = (int)dataGridView.SelectedRows[0].Cells["Id"].Value;
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

        private void SearchDrugs()
        {
            var searchText = textBoxSearch.Text;
            if (searchText == "Введите название, вещество или производителя...")
            {
                RefreshDataGrid(dataGridViewAllDrugs, _drugs);
                return;
            }

            var searchTerm = searchText.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                RefreshDataGrid(dataGridViewAllDrugs, _drugs);
                return;
            }

            var filteredDrugs = _drugs.Where(d =>
                (d.Name != null && d.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.ActiveSubstance != null && d.ActiveSubstance.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.Manufacturer != null && d.Manufacturer.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();

            RefreshDataGrid(dataGridViewAllDrugs, filteredDrugs);
            tabControl.SelectedIndex = 0;
        }

        private void FilterByManufacturer()
        {
            if (comboBoxManufacturers.SelectedItem != null)
            {
                var selectedManufacturer = comboBoxManufacturers.SelectedItem.ToString();
                var filteredDrugs = _drugs.Where(d => d.Manufacturer == selectedManufacturer).ToList();
                RefreshDataGrid(dataGridViewByManufacturer, filteredDrugs);
            }
        }

        private void FilterByCategory()
        {
            if (comboBoxCategories.SelectedItem != null)
            {
                var selectedCategoryName = comboBoxCategories.SelectedItem.ToString();
                var category = _categoryService.GetCategories()
                    .FirstOrDefault(c => c.Name == selectedCategoryName);

                if (category != null)
                {
                    var filteredDrugs = _drugs.Where(d => d.CategoryId == category.Id).ToList();
                    RefreshDataGrid(dataGridViewByCategory, filteredDrugs);
                }
            }
        }

        private DataGridView GetCurrentDataGridView()
        {
            switch (tabControl.SelectedIndex)
            {
                case 0: return dataGridViewAllDrugs;
                case 1: return dataGridViewExpiring;
                case 2: return dataGridViewByManufacturer;
                case 3: return dataGridViewByCategory;
                default: return null;
            }
        }

        private void LoadFromXmlFile()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                openFileDialog.Title = "Выберите XML файл с препаратами";
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var filePath = openFileDialog.FileName;
                        var loadedDrugs = LoadDrugsFromFile(filePath);

                        if (loadedDrugs.Count > 0)
                        {
                            var message = $"Найдено {loadedDrugs.Count} препаратов в файле. Выберите действие:\n\n" +
                                         "• Заменить текущие данные - полностью заменит текущий список\n" +
                                         "• Добавить к текущим данным - добавит препараты к существующим\n" +
                                         "• Отмена - оставит текущие данные без изменений";

                            var result = MessageBox.Show(
                                message,
                                "Загрузка препаратов",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button1
                            );

                            if (result == DialogResult.Yes)
                            {
                                int newId = 1;
                                foreach (var drug in loadedDrugs)
                                {
                                    drug.Id = newId++;
                                }

                                _drugs = loadedDrugs;
                                _currentFilePath = filePath;
                                _dataService.SaveDrugs(_drugs);
                                LoadDrugs();

                                MessageBox.Show($"Успешно загружено {loadedDrugs.Count} препаратов (замена данных)", "Успех",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else if (result == DialogResult.No)
                            {
                                int maxId = _drugs.Count > 0 ? _drugs.Max(d => d.Id) : 0;
                                foreach (var drug in loadedDrugs)
                                {
                                    drug.Id = ++maxId;
                                }

                                _drugs.AddRange(loadedDrugs);
                                _dataService.SaveDrugs(_drugs);
                                LoadDrugs();

                                MessageBox.Show($"Успешно добавлено {loadedDrugs.Count} препаратов", "Успех",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show("В выбранном файле нет данных о препаратах или файл имеет неверный формат", "Информация",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveToXmlFile()
        {
            if (_currentFilePath != null)
            {
                try
                {
                    SaveDrugsToFile(_drugs, _currentFilePath);
                    MessageBox.Show($"Успешно сохранено {_drugs.Count} препаратов", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения файла: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                SaveAsToXmlFile();
            }
        }

        private void SaveAsToXmlFile()
        {
            if (_drugs == null || _drugs.Count == 0)
            {
                MessageBox.Show("Нет данных для сохранения", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                saveFileDialog.Title = "Сохранить список препаратов в XML";
                saveFileDialog.FileName = $"drugs_export_{DateTime.Now:yyyyMMdd_HHmmss}.xml";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var filePath = saveFileDialog.FileName;
                        SaveDrugsToFile(_drugs, filePath);
                        _currentFilePath = filePath;
                        UpdateWindowTitle();

                        MessageBox.Show($"Успешно сохранено {_drugs.Count} препаратов в файл", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения файла: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private List<Drug> LoadDrugsFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Файл не найден");
                }

                var serializer = new XmlSerializer(typeof(List<Drug>),
                    new XmlRootAttribute("Drugs"));

                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    return (List<Drug>)serializer.Deserialize(stream) ?? new List<Drug>();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка чтения XML файла: {ex.Message}");
            }
        }

        private void SaveDrugsToFile(List<Drug> drugs, string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<Drug>),
                    new XmlRootAttribute("Drugs"));

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(stream, drugs);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка записи XML файла: {ex.Message}");
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F5:
                    LoadDrugs();
                    return true;
                case Keys.Control | Keys.N:
                    AddDrug();
                    return true;
                case Keys.Control | Keys.E:
                    EditSelectedDrug();
                    return true;
                case Keys.Delete:
                    DeleteSelectedDrug();
                    return true;
                case Keys.Control | Keys.F:
                    textBoxSearch.Focus();
                    return true;
                case Keys.Control | Keys.O:
                    LoadFromXmlFile();
                    return true;
                case Keys.Control | Keys.S:
                    SaveToXmlFile();
                    return true;
                case Keys.Control | Keys.Shift | Keys.Delete:
                    CleanupExpiredDrugs();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}