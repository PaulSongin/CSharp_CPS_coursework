using DrugCatalog_ver2.Models;
using DrugCatalog_ver2.Services;
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
        private IReminderService _reminderService;
        private User _currentUser;
        private string _currentFilePath;
        private bool _autoDeleteEnabled = true;
        private bool _isExiting = false;

        // UI Элементы
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
            _currentFilePath = null;

            InitializeComponent();

            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            this.FormClosing += MainForm_FormClosing;

            InitializeReminderService();
            CreateStatusBar();

            LoadDrugs();

            UpdateWindowTitle();
            CheckExpiredDrugsOnStartup();
            UpdateUserInterface();
            StartReminderService();
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

        private void InitializeReminderService()
        {
            Action openAction = () => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            };

            Action exitAction = () => {
                _isExiting = true;
                this.Close();
            };

            _reminderService = new ReminderService(_dataService, _currentUser.Id, openAction, exitAction);
            if (statusLabelReminders != null) UpdateRemindersStatus();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isExiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();

                _reminderService.ShowInfoNotification(
                    Locale.Get("AppTitle"),
                    Locale.Get("MsgMinimized") 
                );
            }
        }

        private void ChangeLanguage(string lang)
        {
            Locale.SetLanguage(lang);
            this.Controls.Clear();
            this.MainMenuStrip = null;
            InitializeComponent();
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            CreateStatusBar();
            RefreshAllTabs();
            UpdateWindowTitle();
            UpdateUserInterface();
            UpdateRemindersStatus();
        }

        private void CreateMainMenu()
        {
            mainMenuStrip = new MenuStrip();
            mainMenuStrip.Dock = DockStyle.Top;

            var fileMenu = new ToolStripMenuItem(Locale.Get("MenuFile"));
            fileMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuNew"), null, (s, e) => CreateNewFile()) { ShortcutKeys = Keys.Control | Keys.N });
            fileMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuOpen"), null, (s, e) => LoadFromXmlFile()) { ShortcutKeys = Keys.Control | Keys.O });
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuSave"), null, (s, e) => SaveToXmlFile()) { ShortcutKeys = Keys.Control | Keys.S });
            fileMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuSaveAs"), null, (s, e) => SaveAsToXmlFile()));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuExit"), null, (s, e) => Logout()) { ShortcutKeys = Keys.Alt | Keys.F4 });

            var editMenu = new ToolStripMenuItem(Locale.Get("MenuEdit"));
            editMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuNewDrug"), null, (s, e) => AddDrug()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.N });
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuEditDrug"), null, (s, e) => EditSelectedDrug()) { ShortcutKeys = Keys.Control | Keys.E });
            editMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuDelDrug"), null, (s, e) => DeleteSelectedDrug()) { ShortcutKeys = Keys.Delete });
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuCleanup"), null, (s, e) => CleanupExpiredDrugs()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.Delete });

            var viewMenu = new ToolStripMenuItem(Locale.Get("MenuView"));
            viewMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuRefresh"), null, (s, e) => LoadDrugs()) { ShortcutKeys = Keys.F5 });
            viewMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuSearch"), null, (s, e) => textBoxSearch.Focus()) { ShortcutKeys = Keys.Control | Keys.F });
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuViewAll"), null, (s, e) => tabControl.SelectedIndex = 0));
            viewMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuViewExp"), null, (s, e) => tabControl.SelectedIndex = 1));
            viewMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuViewMan"), null, (s, e) => tabControl.SelectedIndex = 2));
            viewMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuViewCat"), null, (s, e) => tabControl.SelectedIndex = 3));

            var remindersMenu = new ToolStripMenuItem(Locale.Get("MenuReminders"));
            remindersMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuManageRem"), null, (s, e) => ShowRemindersManagement()) { ShortcutKeys = Keys.Control | Keys.R });
            remindersMenu.DropDownItems.Add(new ToolStripSeparator());
            remindersMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuCalc"), null, (s, e) => ShowCalculator()));
            remindersMenu.DropDownItems.Add(new ToolStripSeparator());
            remindersMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuTestNotif"), null, (s, e) => TestNotification()));
            remindersMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuActiveRem"), null, (s, e) => ShowActiveReminders()));

            var langMenu = new ToolStripMenuItem(Locale.Get("MenuLanguage"));
            langMenu.DropDownItems.Add(new ToolStripMenuItem("Русский", null, (s, e) => ChangeLanguage("Ru")));
            langMenu.DropDownItems.Add(new ToolStripMenuItem("English", null, (s, e) => ChangeLanguage("En")));

            var userMenu = new ToolStripMenuItem(Locale.Get("MenuUser"));
            userMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuProfile"), null, (s, e) => ShowUserProfile()));
            userMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuPass"), null, (s, e) => ChangePassword()));
            userMenu.DropDownItems.Add(new ToolStripSeparator());
            userMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuSwitch"), null, (s, e) => SwitchUser()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.L });
            if (_currentUser.Role == UserRole.Admin)
            {
                userMenu.DropDownItems.Add(new ToolStripSeparator());
                userMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuUserMan"), null, (s, e) => ShowUserManagement()));
            }
            userMenu.DropDownItems.Add(new ToolStripSeparator());
            userMenu.DropDownItems.Add(new ToolStripMenuItem(Locale.Get("MenuLogout"), null, (s, e) => Logout()) { ShortcutKeys = Keys.Control | Keys.Q });

            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, viewMenu, remindersMenu, langMenu, userMenu });
            this.MainMenuStrip = mainMenuStrip;
            this.Controls.Add(mainMenuStrip);
        }

        private void CreateContextMenus()
        {
            contextMenuGrid = new ContextMenuStrip();
            contextMenuGrid.Items.Add(new ToolStripMenuItem(Locale.Get("CtxAdd"), null, (s, e) => AddDrug()) { ShortcutKeys = Keys.Control | Keys.N });
            contextMenuGrid.Items.Add(new ToolStripSeparator());
            contextMenuGrid.Items.Add(new ToolStripMenuItem(Locale.Get("CtxEdit"), null, (s, e) => EditSelectedDrug()) { ShortcutKeys = Keys.Control | Keys.E });
            contextMenuGrid.Items.Add(new ToolStripMenuItem(Locale.Get("CtxDel"), null, (s, e) => DeleteSelectedDrug()) { ShortcutKeys = Keys.Delete });
            contextMenuGrid.Items.Add(new ToolStripSeparator());
            contextMenuGrid.Items.Add(new ToolStripMenuItem(Locale.Get("CtxRemind"), null, (s, e) => AddReminderForSelectedDrug()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.R });
            contextMenuGrid.Items.Add(new ToolStripSeparator());
            contextMenuGrid.Items.Add(new ToolStripMenuItem(Locale.Get("CtxClean"), null, (s, e) => CleanupExpiredDrugs()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.Delete });
            contextMenuGrid.Items.Add(new ToolStripSeparator());
            contextMenuGrid.Items.Add(new ToolStripMenuItem(Locale.Get("CtxRefresh"), null, (s, e) => LoadDrugs()) { ShortcutKeys = Keys.F5 });

            contextMenuGrid.Opening += (s, e) =>
            {
                var dgv = GetCurrentDataGridView();
                bool hasSel = dgv != null && dgv.SelectedRows.Count > 0;
                contextMenuGrid.Items[2].Enabled = hasSel; // Edit
                contextMenuGrid.Items[3].Enabled = hasSel; // Delete
                contextMenuGrid.Items[5].Enabled = hasSel; // Reminder
            };
        }

        private void CreateControls()
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5), Location = new Point(0, mainMenuStrip.Height + 5) };

            var panelSearch = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.LightSteelBlue, Padding = new Padding(10, 20, 10, 10) };
            var labelSearch = new Label { Text = Locale.Get("LblSearch"), Location = new Point(10, 25), Size = new Size(120, 20), Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold) };

            textBoxSearch = new TextBox { Location = new Point(140, 22), Width = 300, Height = 25, Text = Locale.Get("PhSearch"), Font = new Font("Microsoft Sans Serif", 9f) };
            textBoxSearch.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBoxSearch.AutoCompleteSource = AutoCompleteSource.CustomSource;
            var acCollection = new AutoCompleteStringCollection();
            foreach (var d in DrugDictionary.GetCommonDrugs().Keys) acCollection.Add(d);
            textBoxSearch.AutoCompleteCustomSource = acCollection;
            textBoxSearch.Enter += (s, e) => { if (textBoxSearch.Text == Locale.Get("PhSearch")) { textBoxSearch.Text = ""; textBoxSearch.ForeColor = Color.Black; } };
            textBoxSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(textBoxSearch.Text)) { textBoxSearch.Text = Locale.Get("PhSearch"); textBoxSearch.ForeColor = Color.Gray; } };
            textBoxSearch.ForeColor = Color.Gray;
            textBoxSearch.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) SearchDrugs(); };

            var btnSearch = new Button { Text = Locale.Get("BtnFind"), Location = new Point(450, 22), Size = new Size(80, 25), BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSearch.Click += (s, e) => SearchDrugs();
            var btnReset = new Button { Text = Locale.Get("BtnReset"), Location = new Point(540, 22), Size = new Size(70, 25), BackColor = Color.LightSlateGray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnReset.Click += (s, e) => { textBoxSearch.Text = Locale.Get("PhSearch"); textBoxSearch.ForeColor = Color.Gray; RefreshDataGrid(dataGridViewAllDrugs, _drugs ?? new List<Drug>()); };

            panelSearch.Controls.AddRange(new Control[] { labelSearch, textBoxSearch, btnSearch, btnReset });

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            tabControl = new TabControl { Dock = DockStyle.Fill, Appearance = TabAppearance.Normal, SizeMode = TabSizeMode.Fixed, Font = new Font("Microsoft Sans Serif", 9f) };
            tabControl.ItemSize = new Size(180, 25);

            // Tabs
            AddTab(Locale.Get("TabAll"), out dataGridViewAllDrugs, out buttonAutoDeleteAll, out buttonCleanupAll, null, out var p1);
            AddTab(Locale.Get("TabExp"), out dataGridViewExpiring, out buttonAutoDeleteExpiring, out buttonCleanupExpiring, null, out var p2);

            // Tab Manufacturer
            var panelMan = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.LightSteelBlue, Padding = new Padding(10) };
            var lblMan = new Label { Text = Locale.Get("LblManFilter"), Location = new Point(10, 15), Size = new Size(150, 20), Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold) };
            comboBoxManufacturers = new ComboBox { Location = new Point(170, 12), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            comboBoxManufacturers.SelectedIndexChanged += (s, e) => FilterByManufacturer();
            panelMan.Controls.AddRange(new Control[] { lblMan, comboBoxManufacturers });
            AddTab(Locale.Get("TabMan"), out dataGridViewByManufacturer, out buttonAutoDeleteManufacturer, out buttonCleanupManufacturer, panelMan, out var manPanelContainer);

            // Tab Category
            var panelCat = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.LightSteelBlue, Padding = new Padding(10) };
            var lblCat = new Label { Text = Locale.Get("LblCatFilter"), Location = new Point(10, 15), Size = new Size(120, 20), Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold) };
            comboBoxCategories = new ComboBox { Location = new Point(140, 12), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            comboBoxCategories.SelectedIndexChanged += (s, e) => FilterByCategory();
            panelCat.Controls.AddRange(new Control[] { lblCat, comboBoxCategories });
            AddTab(Locale.Get("TabCat"), out dataGridViewByCategory, out buttonAutoDeleteCategory, out buttonCleanupCategory, panelCat, out var catPanelContainer);

            contentPanel.Controls.Add(tabControl);
            mainPanel.Controls.Add(contentPanel);
            mainPanel.Controls.Add(panelSearch);
            this.Controls.Add(mainPanel);
        }

        private void AddTab(string title, out DataGridView dgv, out Button btnAuto, out Button btnClean, Panel filterPanel, out Panel container)
        {
            var tab = new TabPage(title);
            container = new Panel { Dock = DockStyle.Fill };

            var controlsPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.LightSteelBlue, Padding = new Padding(10, 5, 10, 5) };
            btnAuto = CreateAutoDeleteButton();
            btnClean = CreateCleanupButton();
            controlsPanel.Controls.AddRange(new Control[] { btnAuto, btnClean });

            dgv = CreateDataGridView();
            dgv.DoubleClick += (s, e) => EditSelectedDrug();

            container.Controls.Add(dgv);
            container.Controls.Add(controlsPanel);
            if (filterPanel != null) container.Controls.Add(filterPanel);

            tab.Controls.Add(container);
            tabControl.TabPages.Add(tab);
        }

        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;
            statusLabelUser = new ToolStripStatusLabel { Text = $"{Locale.Get("StUser")}: {_currentUser.FullName}", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            statusLabelTime = new ToolStripStatusLabel { Text = DateTime.Now.ToString("dd.MM HH:mm"), TextAlign = ContentAlignment.MiddleRight };
            statusLabelReminders = new ToolStripStatusLabel { Text = Locale.Get("StRemActive"), ForeColor = Color.Green, TextAlign = ContentAlignment.MiddleRight };
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabelUser, statusLabelReminders, statusLabelTime });
            this.Controls.Add(statusStrip);
            var t = new Timer { Interval = 1000 };
            t.Tick += (s, e) => {
                statusLabelTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                UpdateRemindersStatus();
            };
            t.Start();
        }


        private Button CreateAutoDeleteButton()
        {
            var btn = new Button { Text = _autoDeleteEnabled ? Locale.Get("BtnAutoOn") : Locale.Get("BtnAutoOff"), Size = new Size(140, 25), Location = new Point(10, 5), BackColor = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral, FlatStyle = FlatStyle.Flat };
            btn.Click += (s, e) => ToggleAutoDelete();
            return btn;
        }

        private Button CreateCleanupButton()
        {
            var btn = new Button { Text = Locale.Get("BtnCleanExp"), Size = new Size(150, 25), Location = new Point(160, 5), BackColor = Color.Orange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
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
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.AliceBlue },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.SteelBlue, ForeColor = Color.White, Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold) },
                EnableHeadersVisualStyles = false
            };
        }

        private void StartReminderService() => UpdateRemindersStatus();

        private void UpdateRemindersStatus()
        {
            var count = _reminderService.GetReminders().Count;
            statusLabelReminders.Text = count > 0 ? $"{Locale.Get("StRemActive")}: {count}" : Locale.Get("StRemNone");
            statusLabelReminders.ForeColor = count > 0 ? Color.Green : Color.Gray;
        }

        private void ShowRemindersManagement()
        {
            var form = new RemindersManagementForm(_reminderService, _drugs);
            if (form.ShowDialog() == DialogResult.OK) UpdateRemindersStatus();
        }

        private void ShowCalculator()
        {
            if (_drugs == null || _drugs.Count == 0) { MessageBox.Show(Locale.Get("MsgNoDataClean")); return; }
            new DosageCalculatorForm(_drugs).ShowDialog();
        }

        private void TestNotification()
        {
            _reminderService.ShowReminderNotification(new MedicationReminder { DrugName = "Test", Dosage = "1 tab", Notes = "Test" });
        }

        private void ShowActiveReminders()
        {
            var list = _reminderService.GetReminders();
            if (list.Count == 0) MessageBox.Show(Locale.Get("StRemNone"));
            else MessageBox.Show(string.Join("\n", list.Select(r => $"💊 {r.DrugName} - {r.Dosage} ({r.ReminderTime:HH:mm})")));
        }

        private void AddReminderForSelectedDrug()
        {
            var dgv = GetCurrentDataGridView();
            if (dgv == null || dgv.SelectedRows.Count == 0) { MessageBox.Show(Locale.Get("MsgSelRem")); return; }
            var id = (int)dgv.SelectedRows[0].Cells["Id"].Value;
            var drug = _drugs.FirstOrDefault(d => d.Id == id);
            if (drug != null)
            {
                var r = new MedicationReminder { UserId = _currentUser.Id, DrugId = drug.Id, DrugName = drug.Name, Dosage = $"{drug.Dosage} {drug.DosageUnit}", ReminderTime = DateTime.Now.Date.AddHours(9), Notes = "" };
                for (int i = 0; i < 7; i++) r.DaysOfWeek[i] = true;
                if (new AddEditReminderForm(_reminderService, _drugs, r).ShowDialog() == DialogResult.OK)
                {
                    UpdateRemindersStatus();
                    MessageBox.Show(Locale.Get("MsgSaveSuccess"));
                }
            }
        }

        private void LoadDrugs()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilePath))
                {
                   
                    _drugs = _dataService.LoadDrugs();
                }
                else
                {
                    _drugs = LoadDrugsFromFile(_currentFilePath);
                }

                if (_autoDeleteEnabled && _drugs.Count > 0)
                    AutoDeleteExpiredDrugs();

                RefreshAllTabs();
                UpdateSearchAutoComplete();
                UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Locale.Get("MsgLoadError")}: {ex.Message}", Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                _drugs = new List<Drug>();
            }
        }

        private void CheckExpiredDrugsOnStartup()
        {
            if (_drugs != null && _drugs.Any(d => d.ExpiryDate < DateTime.Now) && _autoDeleteEnabled)
                PerformAutoDelete(_drugs.Where(d => d.ExpiryDate < DateTime.Now).ToList());
        }

        private void AutoDeleteExpiredDrugs()
        {
            var exp = _drugs.Where(d => d.ExpiryDate < DateTime.Now).ToList();
            if (exp.Count > 0) PerformAutoDelete(exp);
        }

        private void PerformAutoDelete(List<Drug> expired)
        {
            foreach (var d in expired) _drugs.Remove(d);
            _dataService.SaveDrugs(_drugs);
            RefreshAllTabs();
        }

        private void ToggleAutoDelete()
        {
            _autoDeleteEnabled = !_autoDeleteEnabled;
            UpdateAutoDeleteButtons();
            MessageBox.Show(_autoDeleteEnabled ? Locale.Get("MsgAutoDelInfo") : Locale.Get("MsgAutoDelOffInfo"));
        }

        private void UpdateAutoDeleteButtons()
        {
            string txt = _autoDeleteEnabled ? Locale.Get("BtnAutoOn") : Locale.Get("BtnAutoOff");
            Color col = _autoDeleteEnabled ? Color.LightGreen : Color.LightCoral;
            if (buttonAutoDeleteAll != null) { buttonAutoDeleteAll.Text = txt; buttonAutoDeleteAll.BackColor = col; }
            if (buttonAutoDeleteExpiring != null) { buttonAutoDeleteExpiring.Text = txt; buttonAutoDeleteExpiring.BackColor = col; }
            if (buttonAutoDeleteManufacturer != null) { buttonAutoDeleteManufacturer.Text = txt; buttonAutoDeleteManufacturer.BackColor = col; }
            if (buttonAutoDeleteCategory != null) { buttonAutoDeleteCategory.Text = txt; buttonAutoDeleteCategory.BackColor = col; }
        }

        private void CleanupExpiredDrugs()
        {
            if (_drugs == null || _drugs.Count == 0) { MessageBox.Show(Locale.Get("MsgNoDataClean")); return; }
            var exp = _drugs.Where(d => d.ExpiryDate < DateTime.Now).ToList();
            if (exp.Count == 0) { MessageBox.Show(Locale.Get("MsgNoExpFound")); return; }
            if (MessageBox.Show(Locale.Get("MsgCleanConfirm"), Locale.Get("BtnCleanExp"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                foreach (var d in exp) _drugs.Remove(d);
                _dataService.SaveDrugs(_drugs);
                LoadDrugs();
                MessageBox.Show($"{Locale.Get("MsgCleanDone")}: {exp.Count}");
            }
        }

        private void UpdateWindowTitle()
        {
            string file = _currentFilePath != null ? Path.GetFileName(_currentFilePath) : Locale.Get("NewFile");
            string del = _autoDeleteEnabled ? Locale.Get("AutoDelOn") : Locale.Get("AutoDelOff");
            this.Text = $"{Locale.Get("AppTitle")} - {_currentUser.FullName} - {file} ({_drugs?.Count ?? 0} {Locale.Get("DrugsCount")}){del}";
        }

        private void UpdateUserInterface()
        {
            UpdateWindowTitle();
            if (statusLabelUser != null) statusLabelUser.Text = $"{Locale.Get("StUser")}: {_currentUser.FullName}";
        }

        private void ShowUserProfile() => MessageBox.Show($"Login: {_currentUser.Username}\nEmail: {_currentUser.Email}\nRole: {_currentUser.Role}");
        private void ChangePassword() => new ChangePasswordForm(_userService, _currentUser.Id).ShowDialog();

        private void SwitchUser()
        {
            if (MessageBox.Show(Locale.Get("MsgConfirmSwitch"), Locale.Get("MenuSwitch"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_drugs != null && _drugs.Count > 0)
                {
                    if (MessageBox.Show(Locale.Get("MsgSaveSuccess") + "?", Locale.Get("MenuSave"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                        if (_currentFilePath != null) SaveDrugsToFile(_drugs, _currentFilePath); else SaveAsToXmlFile();
                }
                _reminderService?.Dispose();
                using (var login = new LoginForm(_userService))
                {
                    if (login.ShowDialog() == DialogResult.OK && login.LoggedInUser != null)
                    {
                        _currentUser = login.LoggedInUser;
                        _currentFilePath = null;
                        _drugs = new List<Drug>();
                        InitializeReminderService();
                        UpdateUserInterface();
                        RefreshAllTabs();
                        UpdateRemindersStatus();
                    }
                    else if (MessageBox.Show(Locale.Get("MsgConfirmExit"), Locale.Get("MenuExit"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        _isExiting = true; this.Close();
                    }
                    else InitializeReminderService();
                }
            }
        }

        private void ShowUserManagement() => new UserManagementForm(_userService).ShowDialog();

        private void Logout()
        {
            if (MessageBox.Show(Locale.Get("MsgConfirmExit"), Locale.Get("MenuExit"), MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _isExiting = true; this.Close();
            }
        }

        private void UpdateSearchAutoComplete()
        {
            if (_drugs == null) return;
            var col = new AutoCompleteStringCollection();
            foreach (var d in DrugDictionary.GetCommonDrugs().Keys) col.Add(d);
            foreach (var d in _drugs) { if (d.Name != null) col.Add(d.Name); }
            textBoxSearch.AutoCompleteCustomSource = col;
        }

        private void RefreshAllTabs()
        {
            var list = _drugs ?? new List<Drug>();
            RefreshDataGrid(dataGridViewAllDrugs, list);
            RefreshDataGrid(dataGridViewExpiring, list.Where(d => d.ExpiryDate <= DateTime.Now.AddDays(30)).ToList());

            comboBoxManufacturers.Items.Clear();
            comboBoxManufacturers.Items.AddRange(list.Select(d => d.Manufacturer).Distinct().ToArray());

            comboBoxCategories.Items.Clear();
            foreach (var c in _categoryService.GetCategories()) comboBoxCategories.Items.Add(c.Name);

            tabControl.TabPages[0].Text = $"{Locale.Get("TabAll")} ({list.Count})";
        }

        private void RefreshDataGrid(DataGridView dgv, List<Drug> list)
        {
            dgv.Columns.Clear();
            dgv.Columns.Add("Category", Locale.Get("ColCat"));
            dgv.Columns.Add("Name", Locale.Get("ColName"));
            dgv.Columns.Add("ActiveSubstance", Locale.Get("ColSubst"));
            dgv.Columns.Add("Manufacturer", Locale.Get("ColManuf"));
            dgv.Columns.Add("Form", Locale.Get("ColForm"));
            dgv.Columns.Add("Dosage", Locale.Get("ColDosage"));
            dgv.Columns.Add("Quantity", Locale.Get("ColQty"));
            dgv.Columns.Add("ExpiryDate", Locale.Get("ColExp"));
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });

            foreach (var d in list)
            {
                var catName = _categoryService.GetCategory(d.CategoryId)?.Name ?? "Other";
                dgv.Rows.Add(catName, d.Name, d.ActiveSubstance, d.Manufacturer, d.Form, $"{d.Dosage} {d.DosageUnit}", d.Quantity, d.ExpiryDate.ToString("dd.MM.yyyy"), d.Id);
            }

            foreach (DataGridViewRow row in dgv.Rows)
            {
                var id = (int)row.Cells["Id"].Value;
                var d = list.FirstOrDefault(x => x.Id == id);
                if (d != null)
                {
                    row.DefaultCellStyle.BackColor = _categoryService.GetCategoryColor(d.CategoryId);
                    if (d.ExpiryDate < DateTime.Now) { row.DefaultCellStyle.BackColor = Color.LightCoral; }
                    else if (d.ExpiryDate <= DateTime.Now.AddDays(30)) { row.DefaultCellStyle.ForeColor = Color.DarkRed; row.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold); }
                }
            }
        }

        private void LoadFromXmlFile()
        {
            using (var ofd = new OpenFileDialog { Filter = "XML|*.xml" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var loaded = LoadDrugsFromFile(ofd.FileName);
                    if (loaded.Count > 0)
                    {
                        var res = MessageBox.Show(string.Format(Locale.Get("MsgLoadOptionBody"), loaded.Count), Locale.Get("MsgLoadOptionTitle"), MessageBoxButtons.YesNoCancel);
                        if (res == DialogResult.Yes)
                        {
                            int id = 1; foreach (var d in loaded) d.Id = id++;
                            _drugs = loaded; _currentFilePath = ofd.FileName;
                        }
                        else if (res == DialogResult.No)
                        {
                            int max = _drugs.Count > 0 ? _drugs.Max(d => d.Id) : 0;
                            foreach (var d in loaded) d.Id = ++max;
                            _drugs.AddRange(loaded);
                        }
                        else return;

                        _dataService.SaveDrugs(_drugs); LoadDrugs();
                    }
                }
            }
        }

        private void SaveToXmlFile() => SaveDrugsToFile(_drugs, _currentFilePath ?? "drugs.xml");
        private void SaveAsToXmlFile()
        {
            using (var sfd = new SaveFileDialog { Filter = "XML|*.xml", FileName = "drugs_export.xml" })
                if (sfd.ShowDialog() == DialogResult.OK) { SaveDrugsToFile(_drugs, sfd.FileName); _currentFilePath = sfd.FileName; UpdateWindowTitle(); }
        }

        private void CreateNewFile()
        {
            if (MessageBox.Show(Locale.Get("MsgNewFile"), Locale.Get("NewFile"), MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _drugs = new List<Drug>(); _currentFilePath = null; _dataService.SaveDrugs(_drugs); RefreshAllTabs(); UpdateWindowTitle();
            }
        }

        private void AddDrug() { if (new AddEditDrugForm(_dataService, _categoryService, _drugs).ShowDialog() == DialogResult.OK) LoadDrugs(); }

        private void EditSelectedDrug()
        {
            var dgv = GetCurrentDataGridView();
            if (dgv != null && dgv.SelectedRows.Count > 0)
            {
                var id = (int)dgv.SelectedRows[0].Cells["Id"].Value;
                var d = _drugs.FirstOrDefault(x => x.Id == id);
                if (d != null && new AddEditDrugForm(_dataService, _categoryService, _drugs, d).ShowDialog() == DialogResult.OK) LoadDrugs();
            }
        }

        private void DeleteSelectedDrug()
        {
            var dgv = GetCurrentDataGridView();
            if (dgv != null && dgv.SelectedRows.Count > 0)
            {
                var id = (int)dgv.SelectedRows[0].Cells["Id"].Value;
                var d = _drugs.FirstOrDefault(x => x.Id == id);
                if (d != null && MessageBox.Show(Locale.Get("MsgConfirmDelDrug"), "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _drugs.Remove(d); _dataService.SaveDrugs(_drugs); LoadDrugs();
                }
            }
        }

        private void SearchDrugs()
        {
            if (string.IsNullOrWhiteSpace(textBoxSearch.Text) || textBoxSearch.Text == Locale.Get("PhSearch")) { RefreshDataGrid(dataGridViewAllDrugs, _drugs); return; }
            RefreshDataGrid(dataGridViewAllDrugs, _drugs.Where(d => d.Name.Contains(textBoxSearch.Text) || d.Manufacturer.Contains(textBoxSearch.Text)).ToList());
            tabControl.SelectedIndex = 0;
        }

        private void FilterByManufacturer()
        {
            if (comboBoxManufacturers.SelectedItem != null)
                RefreshDataGrid(dataGridViewByManufacturer, _drugs.Where(d => d.Manufacturer == comboBoxManufacturers.SelectedItem.ToString()).ToList());
        }

        private void FilterByCategory()
        {
            if (comboBoxCategories.SelectedItem != null)
            {
                var cat = _categoryService.GetCategories().FirstOrDefault(c => c.Name == comboBoxCategories.SelectedItem.ToString());
                if (cat != null) RefreshDataGrid(dataGridViewByCategory, _drugs.Where(d => d.CategoryId == cat.Id).ToList());
            }
        }

        private DataGridView GetCurrentDataGridView()
        {
            if (tabControl.SelectedIndex == 0) return dataGridViewAllDrugs;
            if (tabControl.SelectedIndex == 1) return dataGridViewExpiring;
            if (tabControl.SelectedIndex == 2) return dataGridViewByManufacturer;
            if (tabControl.SelectedIndex == 3) return dataGridViewByCategory;
            return null;
        }

        private List<Drug> LoadDrugsFromFile(string path)
        {
            try { var s = new XmlSerializer(typeof(List<Drug>), new XmlRootAttribute("Drugs")); using (var fs = new FileStream(path, FileMode.Open)) return (List<Drug>)s.Deserialize(fs); }
            catch { return new List<Drug>(); }
        }

        private void SaveDrugsToFile(List<Drug> list, string path)
        {
            try { var s = new XmlSerializer(typeof(List<Drug>), new XmlRootAttribute("Drugs")); using (var fs = new FileStream(path, FileMode.Create)) s.Serialize(fs, list); }
            catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F5) { LoadDrugs(); return true; }
            if (keyData == (Keys.Control | Keys.N)) { AddDrug(); return true; }
            if (keyData == (Keys.Control | Keys.F)) { textBoxSearch.Focus(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _reminderService?.Dispose();
            base.OnFormClosed(e);
        }
    }
}