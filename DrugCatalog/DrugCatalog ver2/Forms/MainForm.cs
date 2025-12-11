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
        private readonly IUserService _userService;
        private readonly IReminderService _reminderService;
        private User _currentUser;
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
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabelUser;
        private ToolStripStatusLabel statusLabelTime;
        private ToolStripStatusLabel statusLabelReminders;

        public MainForm(IXmlDataService dataService, IUserService userService, User currentUser)
        {
            _dataService = dataService;
            _userService = userService;
            _currentUser = currentUser;
            _categoryService = new CategoryService();
            // ВАЖНО: Передаем сервис данных для списания остатков
            _reminderService = new ReminderService(_dataService);
            _currentFilePath = null;

            InitializeComponent();
            CreateStatusBar();
            LoadDrugs();
            UpdateWindowTitle();
            CheckExpiredDrugsOnStartup();
            UpdateUserInterface();
            StartReminderService();
        }

        // Метод для смены языка
        private void ChangeLanguage(string lang)
        {
            Locale.SetLanguage(lang);
            this.Controls.Clear();
            this.MainMenuStrip = null;
            InitializeComponent();
            CreateStatusBar();
            RefreshAllTabs();
            UpdateWindowTitle();
            UpdateUserInterface();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = Locale.Get("AppTitle");
            this.Size = new Size(1300, 750);
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

            var fileMenu = new ToolStripMenuItem(Locale.Get("MenuFile"));
            var newFileMenuItem = new ToolStripMenuItem(Locale.Get("MenuNew"), null, (s, e) => CreateNewFile()) { ShortcutKeys = Keys.Control | Keys.N };
            var openMenuItem = new ToolStripMenuItem(Locale.Get("MenuOpen"), null, (s, e) => LoadFromXmlFile()) { ShortcutKeys = Keys.Control | Keys.O };
            var saveMenuItem = new ToolStripMenuItem(Locale.Get("MenuSave"), null, (s, e) => SaveToXmlFile()) { ShortcutKeys = Keys.Control | Keys.S };
            var saveAsMenuItem = new ToolStripMenuItem(Locale.Get("MenuSaveAs"), null, (s, e) => SaveAsToXmlFile());
            var exitMenuItem = new ToolStripMenuItem(Locale.Get("MenuExit"), null, (s, e) => this.Close()) { ShortcutKeys = Keys.Alt | Keys.F4 };

            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { newFileMenuItem, openMenuItem, new ToolStripSeparator(), saveMenuItem, saveAsMenuItem, new ToolStripSeparator(), exitMenuItem });

            var editMenu = new ToolStripMenuItem(Locale.Get("MenuEdit"));
            var newDrugMenuItem = new ToolStripMenuItem(Locale.Get("MenuNewDrug"), null, (s, e) => AddDrug()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.N };
            var editDrugMenuItem = new ToolStripMenuItem(Locale.Get("MenuEditDrug"), null, (s, e) => EditSelectedDrug()) { ShortcutKeys = Keys.Control | Keys.E };
            var deleteDrugMenuItem = new ToolStripMenuItem(Locale.Get("MenuDelDrug"), null, (s, e) => DeleteSelectedDrug()) { ShortcutKeys = Keys.Delete };
            var cleanupMenuItem = new ToolStripMenuItem(Locale.Get("MenuCleanup"), null, (s, e) => CleanupExpiredDrugs()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.Delete };

            editMenu.DropDownItems.AddRange(new ToolStripItem[] { newDrugMenuItem, new ToolStripSeparator(), editDrugMenuItem, deleteDrugMenuItem, new ToolStripSeparator(), cleanupMenuItem });

            var viewMenu = new ToolStripMenuItem(Locale.Get("MenuView"));
            var refreshMenuItem = new ToolStripMenuItem(Locale.Get("MenuRefresh"), null, (s, e) => LoadDrugs()) { ShortcutKeys = Keys.F5 };
            var searchMenuItem = new ToolStripMenuItem(Locale.Get("MenuSearch"), null, (s, e) => textBoxSearch.Focus()) { ShortcutKeys = Keys.Control | Keys.F };
            var viewAllMenuItem = new ToolStripMenuItem(Locale.Get("MenuViewAll"), null, (s, e) => tabControl.SelectedIndex = 0);
            var viewExpiringMenuItem = new ToolStripMenuItem(Locale.Get("MenuViewExp"), null, (s, e) => tabControl.SelectedIndex = 1);
            var viewByManufacturerMenuItem = new ToolStripMenuItem(Locale.Get("MenuViewMan"), null, (s, e) => tabControl.SelectedIndex = 2);
            var viewByCategoryMenuItem = new ToolStripMenuItem(Locale.Get("MenuViewCat"), null, (s, e) => tabControl.SelectedIndex = 3);

            viewMenu.DropDownItems.AddRange(new ToolStripItem[] { refreshMenuItem, searchMenuItem, new ToolStripSeparator(), viewAllMenuItem, viewExpiringMenuItem, viewByManufacturerMenuItem, viewByCategoryMenuItem });

            var remindersMenu = new ToolStripMenuItem(Locale.Get("MenuReminders"));
            var manageRemindersMenuItem = new ToolStripMenuItem(Locale.Get("MenuManageRem"), null, (s, e) => ShowRemindersManagement()) { ShortcutKeys = Keys.Control | Keys.R };
            var testNotificationMenuItem = new ToolStripMenuItem(Locale.Get("MenuTestNotif"), null, (s, e) => TestNotification());
            var showActiveRemindersMenuItem = new ToolStripMenuItem(Locale.Get("MenuActiveRem"), null, (s, e) => ShowActiveReminders());

            remindersMenu.DropDownItems.AddRange(new ToolStripItem[] { manageRemindersMenuItem, new ToolStripSeparator(), testNotificationMenuItem, showActiveRemindersMenuItem });

            // НОВОЕ: Меню выбора языка
            var langMenu = new ToolStripMenuItem(Locale.Get("MenuLanguage"));
            var ruItem = new ToolStripMenuItem("Русский", null, (s, e) => ChangeLanguage("Ru"));
            var enItem = new ToolStripMenuItem("English", null, (s, e) => ChangeLanguage("En"));
            langMenu.DropDownItems.AddRange(new ToolStripItem[] { ruItem, enItem });

            var userMenu = new ToolStripMenuItem(Locale.Get("MenuUser"));
            var profileMenuItem = new ToolStripMenuItem(Locale.Get("MenuProfile"), null, (s, e) => ShowUserProfile());
            var changePasswordMenuItem = new ToolStripMenuItem(Locale.Get("MenuPass"), null, (s, e) => ChangePassword());
            var switchUserMenuItem = new ToolStripMenuItem(Locale.Get("MenuSwitch"), null, (s, e) => SwitchUser()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.L };
            var usersMenuItem = new ToolStripMenuItem(Locale.Get("MenuUserMan"), null, (s, e) => ShowUserManagement());
            usersMenuItem.Visible = _currentUser.Role == UserRole.Admin;
            var logoutMenuItem = new ToolStripMenuItem(Locale.Get("MenuLogout"), null, (s, e) => Logout()) { ShortcutKeys = Keys.Control | Keys.Q };

            userMenu.DropDownItems.AddRange(new ToolStripItem[] { profileMenuItem, changePasswordMenuItem, new ToolStripSeparator(), switchUserMenuItem, new ToolStripSeparator(), usersMenuItem, new ToolStripSeparator(), logoutMenuItem });

            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, viewMenu, remindersMenu, langMenu, userMenu });

            this.MainMenuStrip = mainMenuStrip;
            this.Controls.Add(mainMenuStrip);
        }

        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;

            statusLabelUser = new ToolStripStatusLabel
            {
                Text = $"{Locale.Get("StUser")}: {_currentUser.FullName} ({_currentUser.Role})",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            statusLabelTime = new ToolStripStatusLabel
            {
                Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                TextAlign = ContentAlignment.MiddleRight
            };

            statusLabelReminders = new ToolStripStatusLabel
            {
                Text = Locale.Get("StRemActive"),
                ForeColor = Color.Green,
                TextAlign = ContentAlignment.MiddleRight
            };

            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabelUser, statusLabelReminders, statusLabelTime });
            this.Controls.Add(statusStrip);

            var timer = new Timer();
            timer.Interval = 60000;
            timer.Tick += (s, e) => {
                statusLabelTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                UpdateRemindersStatus();
            };
            timer.Start();
        }

        private void CreateContextMenus()
        {
            contextMenuGrid = new ContextMenuStrip();

            var addToolStripMenuItem = new ToolStripMenuItem(Locale.Get("CtxAdd"), null, (s, e) => AddDrug()) { ShortcutKeys = Keys.Control | Keys.N };
            var editToolStripMenuItem = new ToolStripMenuItem(Locale.Get("CtxEdit"), null, (s, e) => EditSelectedDrug()) { ShortcutKeys = Keys.Control | Keys.E };
            var deleteToolStripMenuItem = new ToolStripMenuItem(Locale.Get("CtxDel"), null, (s, e) => DeleteSelectedDrug()) { ShortcutKeys = Keys.Delete };
            var addReminderToolStripMenuItem = new ToolStripMenuItem(Locale.Get("CtxRemind"), null, (s, e) => AddReminderForSelectedDrug()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.R };
            var cleanupToolStripMenuItem = new ToolStripMenuItem(Locale.Get("CtxClean"), null, (s, e) => CleanupExpiredDrugs()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.Delete };
            var refreshToolStripMenuItem = new ToolStripMenuItem(Locale.Get("CtxRefresh"), null, (s, e) => LoadDrugs()) { ShortcutKeys = Keys.F5 };

            contextMenuGrid.Items.AddRange(new ToolStripItem[] { addToolStripMenuItem, new ToolStripSeparator(), editToolStripMenuItem, deleteToolStripMenuItem, new ToolStripSeparator(), addReminderToolStripMenuItem, new ToolStripSeparator(), cleanupToolStripMenuItem, new ToolStripSeparator(), refreshToolStripMenuItem });

            contextMenuGrid.Opening += (s, e) =>
            {
                var dataGridView = GetCurrentDataGridView();
                bool hasSelection = dataGridView != null && dataGridView.SelectedRows.Count > 0;
                editToolStripMenuItem.Enabled = hasSelection;
                deleteToolStripMenuItem.Enabled = hasSelection;
                addReminderToolStripMenuItem.Enabled = hasSelection;
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
                Text = Locale.Get("LblSearch"),
                Location = new Point(10, 25),
                Size = new Size(120, 20),
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold)
            };

            textBoxSearch = new TextBox
            {
                Location = new Point(140, 22),
                Width = 300,
                Height = 25,
                Text = Locale.Get("PhSearch"),
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular)
            };

            textBoxSearch.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBoxSearch.AutoCompleteSource = AutoCompleteSource.CustomSource;
            var autoCompleteCollection = new AutoCompleteStringCollection();
            textBoxSearch.AutoCompleteCustomSource = autoCompleteCollection;

            foreach (var drug in DrugDictionary.CommonDrugs.Keys)
                autoCompleteCollection.Add(drug);

            textBoxSearch.Enter += (s, e) => {
                if (textBoxSearch.Text == Locale.Get("PhSearch"))
                {
                    textBoxSearch.Text = "";
                    textBoxSearch.ForeColor = Color.Black;
                }
            };

            textBoxSearch.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(textBoxSearch.Text))
                {
                    textBoxSearch.Text = Locale.Get("PhSearch");
                    textBoxSearch.ForeColor = Color.Gray;
                }
            };
            textBoxSearch.ForeColor = Color.Gray;
            textBoxSearch.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) SearchDrugs(); };

            var buttonSearch = new Button
            {
                Text = Locale.Get("BtnFind"),
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
                Text = Locale.Get("BtnReset"),
                Location = new Point(540, 22),
                Size = new Size(70, 25),
                BackColor = Color.LightSlateGray,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonClearSearch.Click += (s, e) =>
            {
                textBoxSearch.Text = Locale.Get("PhSearch");
                textBoxSearch.ForeColor = Color.Gray;
                RefreshDataGrid(dataGridViewAllDrugs, _drugs ?? new List<Drug>());
            };

            panelSearch.Controls.AddRange(new Control[] { labelSearch, textBoxSearch, buttonSearch, buttonClearSearch });

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            tabControl = new TabControl { Dock = DockStyle.Fill, Appearance = TabAppearance.Normal, SizeMode = TabSizeMode.Fixed, Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular) };
            tabControl.ItemSize = new Size(180, 25);

            // Tab 1: All
            var tabAllDrugs = new TabPage(Locale.Get("TabAll"));
            var allDrugsPanel = new Panel { Dock = DockStyle.Fill };
            var panelAllDrugsControls = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.LightSteelBlue, Padding = new Padding(10, 5, 10, 5) };

            buttonAutoDeleteAll = CreateAutoDeleteButton();
            buttonCleanupAll = CreateCleanupButton();
            panelAllDrugsControls.Controls.AddRange(new Control[] { buttonAutoDeleteAll, buttonCleanupAll });

            dataGridViewAllDrugs = CreateDataGridView();
            allDrugsPanel.Controls.Add(dataGridViewAllDrugs);
            allDrugsPanel.Controls.Add(panelAllDrugsControls);
            tabAllDrugs.Controls.Add(allDrugsPanel);

            // Tab 2: Expiring
            var tabExpiring = new TabPage(Locale.Get("TabExp"));
            var expiringPanel = new Panel { Dock = DockStyle.Fill };
            var panelExpiringControls = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.LightSteelBlue, Padding = new Padding(10, 5, 10, 5) };

            buttonAutoDeleteExpiring = CreateAutoDeleteButton();
            buttonCleanupExpiring = CreateCleanupButton();
            panelExpiringControls.Controls.AddRange(new Control[] { buttonAutoDeleteExpiring, buttonCleanupExpiring });

            dataGridViewExpiring = CreateDataGridView();
            expiringPanel.Controls.Add(dataGridViewExpiring);
            expiringPanel.Controls.Add(panelExpiringControls);
            tabExpiring.Controls.Add(expiringPanel);

            // Tab 3: Manufacturers
            var tabByManufacturer = new TabPage(Locale.Get("TabMan"));
            var manufacturerPanel = new Panel { Dock = DockStyle.Fill };
            var panelManufacturerFilter = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.LightSteelBlue, Padding = new Padding(10) };
            var labelManufacturer = new Label { Text = Locale.Get("LblManFilter"), Location = new Point(10, 15), Size = new Size(150, 20), Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold) };
            comboBoxManufacturers = new ComboBox { Location = new Point(170, 12), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular) };
            comboBoxManufacturers.SelectedIndexChanged += (s, e) => FilterByManufacturer();
            var panelManufacturerControls = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.LightSteelBlue, Padding = new Padding(10, 5, 10, 5) };

            buttonAutoDeleteManufacturer = CreateAutoDeleteButton();
            buttonCleanupManufacturer = CreateCleanupButton();
            panelManufacturerControls.Controls.AddRange(new Control[] { buttonAutoDeleteManufacturer, buttonCleanupManufacturer });
            panelManufacturerFilter.Controls.AddRange(new Control[] { labelManufacturer, comboBoxManufacturers });

            dataGridViewByManufacturer = CreateDataGridView();
            manufacturerPanel.Controls.Add(dataGridViewByManufacturer);
            manufacturerPanel.Controls.Add(panelManufacturerControls);
            manufacturerPanel.Controls.Add(panelManufacturerFilter);
            tabByManufacturer.Controls.Add(manufacturerPanel);

            // Tab 4: Categories
            var tabByCategory = new TabPage(Locale.Get("TabCat"));
            var categoryPanel = new Panel { Dock = DockStyle.Fill };
            var panelCategoryFilter = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.LightSteelBlue, Padding = new Padding(10) };
            var labelCategory = new Label { Text = Locale.Get("LblCatFilter"), Location = new Point(10, 15), Size = new Size(120, 20), Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold) };
            comboBoxCategories = new ComboBox { Location = new Point(140, 12), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Regular) };
            comboBoxCategories.SelectedIndexChanged += (s, e) => FilterByCategory();
            var panelCategoryControls = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.LightSteelBlue, Padding = new Padding(10, 5, 10, 5) };

            buttonAutoDeleteCategory = CreateAutoDeleteButton();
            buttonCleanupCategory = CreateCleanupButton();
            panelCategoryControls.Controls.AddRange(new Control[] { buttonAutoDeleteCategory, buttonCleanupCategory });
            panelCategoryFilter.Controls.AddRange(new Control[] { labelCategory, comboBoxCategories });

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

        private Button CreateAutoDeleteButton()
        {
            var btn = new Button
            {
                Text = _autoDeleteEnabled ? Locale.Get("BtnAutoOn") : Locale.Get("BtnAutoOff"),
                Size = new Size(140, 25),
                Location = new Point(10, 5),
                BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral,
                ForeColor = Color.Black,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            btn.Click += (s, e) => ToggleAutoDelete();
            return btn;
        }

        private Button CreateCleanupButton()
        {
            var btn = new Button
            {
                Text = Locale.Get("BtnCleanExp"),
                Size = new Size(150, 25),
                Location = new Point(160, 5),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            btn.Click += (s, e) => CleanupExpiredDrugs();
            return btn;
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
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.AliceBlue },
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.SteelBlue,
                    ForeColor = Color.White,
                    Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold)
                }
            };
        }

        private void StartReminderService()
        {
            UpdateRemindersStatus();
        }

        private void UpdateRemindersStatus()
        {
            var activeReminders = _reminderService.GetReminders();
            int activeCount = activeReminders.Count;

            if (activeCount > 0)
            {
                statusLabelReminders.Text = $"{Locale.Get("StRemActive")}: {activeCount}";
                statusLabelReminders.ForeColor = Color.Green;
            }
            else
            {
                statusLabelReminders.Text = Locale.Get("StRemNone");
                statusLabelReminders.ForeColor = Color.Gray;
            }
        }

        private void ShowRemindersManagement()
        {
            var form = new RemindersManagementForm(_reminderService, _drugs);
            if (form.ShowDialog() == DialogResult.OK) UpdateRemindersStatus();
        }

        private void TestNotification()
        {
            var testReminder = new MedicationReminder { DrugName = "Test", Dosage = "1 tab", Notes = "Test" };
            _reminderService.ShowReminderNotification(testReminder);
        }

        private void ShowActiveReminders()
        {
            var activeReminders = _reminderService.GetReminders();
            if (activeReminders.Count == 0)
            {
                MessageBox.Show(Locale.Get("StRemNone"), Locale.Get("MenuActiveRem"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string reminderList = "";
            foreach (var reminder in activeReminders)
            {
                reminderList += $"💊 {reminder.DrugName} - {reminder.Dosage}\n⏰ {reminder.ReminderTime:HH:mm}\n\n";
            }
            MessageBox.Show(reminderList, Locale.Get("MenuActiveRem"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddReminderForSelectedDrug()
        {
            var dataGridView = GetCurrentDataGridView();
            if (dataGridView == null || dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show(Locale.Get("MsgSelRem"), Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedId = (int)dataGridView.SelectedRows[0].Cells["Id"].Value;
            var drug = _drugs.FirstOrDefault(d => d.Id == selectedId);

            if (drug != null)
            {
                var reminder = new MedicationReminder
                {
                    DrugId = drug.Id,
                    DrugName = drug.Name,
                    Dosage = $"{drug.Dosage} {drug.DosageUnit}",
                    ReminderTime = DateTime.Now.Date.AddHours(9),
                    Notes = ""
                };
                for (int i = 0; i < 7; i++) reminder.DaysOfWeek[i] = true;

                var form = new AddEditReminderForm(_reminderService, _drugs, reminder);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    UpdateRemindersStatus();
                    MessageBox.Show(Locale.Get("MsgSaveSuccess"), Locale.Get("MsgSaved"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void LoadDrugs()
        {
            try
            {
                _drugs = _dataService.LoadDrugs();
                if (_autoDeleteEnabled) AutoDeleteExpiredDrugs();
                RefreshAllTabs();
                UpdateSearchAutoComplete();
                UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Locale.Get("MsgLoadError")}: {ex.Message}", Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckExpiredDrugsOnStartup()
        {
            if (_drugs == null || _drugs.Count == 0) return;
            var expiredDrugs = _drugs.Where(d => d.ExpiryDate < DateTime.Now).ToList();
            if (expiredDrugs.Count > 0 && _autoDeleteEnabled)
            {
                PerformAutoDelete(expiredDrugs);
            }
        }

        private void AutoDeleteExpiredDrugs()
        {
            if (_drugs == null || _drugs.Count == 0) return;
            var expiredDrugs = _drugs.Where(d => d.ExpiryDate < DateTime.Now).ToList();
            if (expiredDrugs.Count > 0) PerformAutoDelete(expiredDrugs);
        }

        private void PerformAutoDelete(List<Drug> expiredDrugs)
        {
            foreach (var expiredDrug in expiredDrugs) _drugs.Remove(expiredDrug);
            _dataService.SaveDrugs(_drugs);
            RefreshAllTabs();
            UpdateWindowTitle();
        }

        private void ToggleAutoDelete()
        {
            _autoDeleteEnabled = !_autoDeleteEnabled;
            UpdateAutoDeleteButtons();
            MessageBox.Show(_autoDeleteEnabled ? Locale.Get("MsgAutoDelInfo") : Locale.Get("MsgAutoDelOffInfo"), "Auto-Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateAutoDeleteButtons()
        {
            string text = _autoDeleteEnabled ? Locale.Get("BtnAutoOn") : Locale.Get("BtnAutoOff");
            Color color = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral;

            if (buttonAutoDeleteAll != null) { buttonAutoDeleteAll.Text = text; buttonAutoDeleteAll.BackColor = color; }
            if (buttonAutoDeleteExpiring != null) { buttonAutoDeleteExpiring.Text = text; buttonAutoDeleteExpiring.BackColor = color; }
            if (buttonAutoDeleteManufacturer != null) { buttonAutoDeleteManufacturer.Text = text; buttonAutoDeleteManufacturer.BackColor = color; }
            if (buttonAutoDeleteCategory != null) { buttonAutoDeleteCategory.Text = text; buttonAutoDeleteCategory.BackColor = color; }
            UpdateWindowTitle();
        }

        private void CleanupExpiredDrugs()
        {
            if (_drugs == null || _drugs.Count == 0)
            {
                MessageBox.Show(Locale.Get("MsgNoDataClean"), Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var expiredDrugs = _drugs.Where(d => d.ExpiryDate < DateTime.Now).ToList();
            if (expiredDrugs.Count == 0)
            {
                MessageBox.Show(Locale.Get("MsgNoExpFound"), Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(Locale.Get("MsgCleanConfirm"), Locale.Get("BtnCleanExp"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                foreach (var drug in expiredDrugs) _drugs.Remove(drug);
                _dataService.SaveDrugs(_drugs);
                LoadDrugs();
                MessageBox.Show($"{Locale.Get("MsgCleanDone")}: {expiredDrugs.Count}", Locale.Get("MsgSaved"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateWindowTitle()
        {
            string fileName = _currentFilePath != null ? Path.GetFileName(_currentFilePath) : Locale.Get("NewFile");
            string autoDeleteStatus = _autoDeleteEnabled ? Locale.Get("AutoDelOn") : Locale.Get("AutoDelOff");
            this.Text = $"{Locale.Get("AppTitle")} - {_currentUser.FullName} ({_currentUser.Role}) - {fileName} ({_drugs?.Count ?? 0} {Locale.Get("DrugsCount")}){autoDeleteStatus}";
        }

        private void UpdateUserInterface()
        {
            UpdateWindowTitle();
            if (statusLabelUser != null)
                statusLabelUser.Text = $"{Locale.Get("StUser")}: {_currentUser.FullName} ({_currentUser.Role})";
        }

        private void ShowUserProfile()
        {
            MessageBox.Show($"{Locale.Get("StUser")}: {_currentUser.FullName}\n" +
                           $"Login: {_currentUser.Username}\n" +
                           $"Email: {_currentUser.Email}\n" +
                           $"Role: {_currentUser.Role}\n",
                           Locale.Get("MenuProfile"));
        }

        private void ChangePassword()
        {
            var form = new ChangePasswordForm(_userService, _currentUser.Id);
            form.ShowDialog();
        }

        private void SwitchUser()
        {
            if (MessageBox.Show(Locale.Get("MsgConfirmSwitch"), Locale.Get("MenuSwitch"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_drugs != null && _drugs.Count > 0) _dataService.SaveDrugs(_drugs);
                using (var loginForm = new LoginForm(_userService))
                {
                    if (loginForm.ShowDialog() == DialogResult.OK && loginForm.LoggedInUser != null)
                    {
                        _currentUser = loginForm.LoggedInUser;
                        UpdateUserInterface();
                        LoadDrugs();
                        MessageBox.Show($"{Locale.Get("MsgWelcome")}, {_currentUser.FullName}!", Locale.Get("MenuSwitch"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (MessageBox.Show(Locale.Get("MsgConfirmExit"), Locale.Get("MenuExit"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        this.Close();
                    }
                }
            }
        }

        private void ShowUserManagement()
        {
            var form = new UserManagementForm(_userService);
            form.ShowDialog();
        }

        private void Logout()
        {
            if (MessageBox.Show(Locale.Get("MsgConfirmExit"), Locale.Get("MenuExit"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Close();
            }
        }

        private void UpdateSearchAutoComplete()
        {
            if (_drugs == null) return;
            var autoCompleteCollection = new AutoCompleteStringCollection();
            foreach (var drug in DrugDictionary.CommonDrugs.Keys) autoCompleteCollection.Add(drug);
            foreach (var drug in _drugs)
            {
                if (!string.IsNullOrWhiteSpace(drug.Name)) autoCompleteCollection.Add(drug.Name);
                if (!string.IsNullOrWhiteSpace(drug.ActiveSubstance)) autoCompleteCollection.Add(drug.ActiveSubstance);
                if (!string.IsNullOrWhiteSpace(drug.Manufacturer)) autoCompleteCollection.Add(drug.Manufacturer);
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
            var manufacturers = drugs.Select(d => d.Manufacturer).Where(m => !string.IsNullOrEmpty(m)).Distinct().OrderBy(m => m).ToArray();
            comboBoxManufacturers.Items.AddRange(manufacturers);

            comboBoxCategories.Items.Clear();
            var categories = _categoryService.GetCategories();
            foreach (var category in categories) comboBoxCategories.Items.Add(category.Name);

            if (comboBoxManufacturers.Items.Count > 0) comboBoxManufacturers.SelectedIndex = 0;
            if (comboBoxCategories.Items.Count > 0) comboBoxCategories.SelectedIndex = 0;

            tabControl.TabPages[0].Text = $"{Locale.Get("TabAll")} ({drugs.Count})";
            tabControl.TabPages[1].Text = $"{Locale.Get("TabExp")} ({expiringDrugs.Count})";
            tabControl.TabPages[2].Text = $"{Locale.Get("TabMan")} ({manufacturers.Length})";
            tabControl.TabPages[3].Text = $"{Locale.Get("TabCat")} ({categories.Count})";
        }

        private void RefreshDataGrid(DataGridView dataGridView, List<Drug> drugs)
        {
            dataGridView.Columns.Clear();

            dataGridView.Columns.Add("Category", Locale.Get("ColCat"));
            dataGridView.Columns.Add("Name", Locale.Get("ColName"));
            dataGridView.Columns.Add("ActiveSubstance", Locale.Get("ColSubst"));
            dataGridView.Columns.Add("Manufacturer", Locale.Get("ColManuf"));
            dataGridView.Columns.Add("Form", Locale.Get("ColForm"));
            dataGridView.Columns.Add("Dosage", Locale.Get("ColDosage"));
            dataGridView.Columns.Add("Quantity", Locale.Get("ColQty"));
            dataGridView.Columns.Add("ExpiryDate", Locale.Get("ColExp"));

            var idColumn = new DataGridViewTextBoxColumn { Name = "Id", Visible = false };
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
                var categoryName = category?.Name ?? "Other";

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
                    row.DefaultCellStyle.ForeColor = Color.Black;

                    var expiryDate = drug.ExpiryDate;
                    if (expiryDate < DateTime.Now)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                        row.DefaultCellStyle.SelectionBackColor = Color.Red;
                    }
                    else if (expiryDate <= DateTime.Now.AddDays(30))
                    {
                        row.DefaultCellStyle.ForeColor = Color.DarkRed;
                        row.DefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold);
                        row.Cells["ExpiryDate"].Style.BackColor = Color.LightYellow;
                    }
                    else
                    {
                        row.DefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Regular);
                        row.DefaultCellStyle.SelectionBackColor = Color.DarkBlue;
                    }
                }
            }
        }

        private void CreateNewFile()
        {
            if (MessageBox.Show(Locale.Get("MsgNewFile"), Locale.Get("NewFile"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _drugs = new List<Drug>();
                _currentFilePath = null;
                _dataService.SaveDrugs(_drugs);
                RefreshAllTabs();
                UpdateWindowTitle();
                MessageBox.Show(Locale.Get("MsgNewFile"), Locale.Get("MsgSaved"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AddDrug()
        {
            var addForm = new AddEditDrugForm(_dataService, _categoryService, _drugs);
            if (addForm.ShowDialog() == DialogResult.OK) LoadDrugs();
        }

        private void EditSelectedDrug()
        {
            var dataGridView = GetCurrentDataGridView();
            if (dataGridView == null || dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show(Locale.Get("MsgSelEdit"), Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedId = (int)dataGridView.SelectedRows[0].Cells["Id"].Value;
            var drug = _drugs.FirstOrDefault(d => d.Id == selectedId);

            if (drug != null)
            {
                var editForm = new AddEditDrugForm(_dataService, _categoryService, _drugs, drug);
                if (editForm.ShowDialog() == DialogResult.OK) LoadDrugs();
            }
        }

        private void DeleteSelectedDrug()
        {
            var dataGridView = GetCurrentDataGridView();
            if (dataGridView == null || dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show(Locale.Get("MsgSelDel"), Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedId = (int)dataGridView.SelectedRows[0].Cells["Id"].Value;
            var drug = _drugs.FirstOrDefault(d => d.Id == selectedId);

            if (drug != null)
            {
                if (MessageBox.Show($"{Locale.Get("MsgConfirmDelDrug")} '{drug.Name}'?", Locale.Get("MsgConfirmDelDrug"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _drugs.Remove(drug);
                    _dataService.SaveDrugs(_drugs);
                    LoadDrugs();
                    MessageBox.Show(Locale.Get("MsgDeleted"), Locale.Get("MsgSaved"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void SearchDrugs()
        {
            var searchText = textBoxSearch.Text;
            if (searchText == Locale.Get("PhSearch"))
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
                var category = _categoryService.GetCategories().FirstOrDefault(c => c.Name == selectedCategoryName);
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
                openFileDialog.Title = Locale.Get("MenuOpen");
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var filePath = openFileDialog.FileName;
                        var loadedDrugs = LoadDrugsFromFile(filePath);

                        if (loadedDrugs.Count > 0)
                        {
                            int maxId = _drugs.Count > 0 ? _drugs.Max(d => d.Id) : 0;
                            foreach (var drug in loadedDrugs) drug.Id = ++maxId;

                            _drugs.AddRange(loadedDrugs);
                            _dataService.SaveDrugs(_drugs);
                            LoadDrugs();
                            MessageBox.Show($"{Locale.Get("MsgSaved")} {loadedDrugs.Count} drugs", Locale.Get("MsgSaved"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{Locale.Get("MsgLoadError")}: {ex.Message}", Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(Locale.Get("MsgSaveSuccess"), Locale.Get("MsgSaved"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{Locale.Get("MsgLoadError")}: {ex.Message}", Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                SaveAsToXmlFile();
            }
        }

        private void SaveAsToXmlFile()
        {
            if (_drugs == null || _drugs.Count == 0) return;

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                saveFileDialog.Title = Locale.Get("MenuSaveAs");
                saveFileDialog.FileName = $"drugs_export_{DateTime.Now:yyyyMMdd_HHmmss}.xml";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var filePath = saveFileDialog.FileName;
                        SaveDrugsToFile(_drugs, filePath);
                        _currentFilePath = filePath;
                        UpdateWindowTitle();
                        MessageBox.Show(Locale.Get("MsgSaveSuccess"), Locale.Get("MsgSaved"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{Locale.Get("MsgLoadError")}: {ex.Message}", Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private List<Drug> LoadDrugsFromFile(string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<Drug>), new XmlRootAttribute("Drugs"));
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                return (List<Drug>)serializer.Deserialize(stream) ?? new List<Drug>();
            }
        }

        private void SaveDrugsToFile(List<Drug> drugs, string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<Drug>), new XmlRootAttribute("Drugs"));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(stream, drugs);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F5: LoadDrugs(); return true;
                case Keys.Control | Keys.N: AddDrug(); return true;
                case Keys.Control | Keys.E: EditSelectedDrug(); return true;
                case Keys.Delete: DeleteSelectedDrug(); return true;
                case Keys.Control | Keys.F: textBoxSearch.Focus(); return true;
                case Keys.Control | Keys.O: LoadFromXmlFile(); return true;
                case Keys.Control | Keys.S: SaveToXmlFile(); return true;
                case Keys.Control | Keys.Shift | Keys.Delete: CleanupExpiredDrugs(); return true;
                case Keys.Control | Keys.Q: Logout(); return true;
                case Keys.Control | Keys.Shift | Keys.L: SwitchUser(); return true;
                case Keys.Control | Keys.R: ShowRemindersManagement(); return true;
                case Keys.Control | Keys.Shift | Keys.R: AddReminderForSelectedDrug(); return true;
                default: return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _reminderService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}