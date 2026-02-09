using DrugCatalog_ver2.Models;
using DrugCatalog_ver2.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class AddEditUserForm : Form
    {
        private readonly IUserService _userService;
        private User _user;
        private bool _isEditMode;

        private TextBox textBoxUsername, textBoxFullName, textBoxEmail, textBoxPassword;
        private ComboBox comboBoxRole;
        private Button buttonSave, buttonCancel;
        private Label labelPassInfo;

        public AddEditUserForm(IUserService userService, User user = null)
        {
            _userService = userService;
            _user = user;
            _isEditMode = user != null;

            InitializeComponent();
            if (_isEditMode) FillForm();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = _isEditMode ? Locale.Get("TitleEditUser") : Locale.Get("TitleAddUser");
            this.Size = new Size(400, 450);
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
            int labelW = 120;
            int controlW = 230;

            AddLabel(Locale.Get("LblLoginReq"), y, labelW);
            textBoxUsername = new TextBox { Location = new Point(140, y), Width = controlW };
            if (_isEditMode) textBoxUsername.ReadOnly = true;
            this.Controls.Add(textBoxUsername);
            y += 40;

            AddLabel(Locale.Get("LblFullName"), y, labelW);
            textBoxFullName = new TextBox { Location = new Point(140, y), Width = controlW };
            this.Controls.Add(textBoxFullName);
            y += 40;

            AddLabel(Locale.Get("LblEmail"), y, labelW);
            textBoxEmail = new TextBox { Location = new Point(140, y), Width = controlW };
            this.Controls.Add(textBoxEmail);
            y += 40;

            AddLabel(Locale.Get("LblRole"), y, labelW);
            comboBoxRole = new ComboBox { Location = new Point(140, y), Width = controlW, DropDownStyle = ComboBoxStyle.DropDownList };
            comboBoxRole.Items.AddRange(Enum.GetNames(typeof(UserRole))); 
            comboBoxRole.SelectedIndex = 0; 
            this.Controls.Add(comboBoxRole);
            y += 40;

            AddLabel(Locale.Get("LblPass"), y, labelW);
            textBoxPassword = new TextBox { Location = new Point(140, y), Width = controlW, UseSystemPasswordChar = true };
            this.Controls.Add(textBoxPassword);
            y += 25;

            if (_isEditMode)
            {
                labelPassInfo = new Label
                {
                    Text = Locale.Get("MsgPassEmptyInfo"),
                    Location = new Point(140, y),
                    Size = new Size(230, 30),
                    Font = new Font("Microsoft Sans Serif", 7f, FontStyle.Italic),
                    ForeColor = Color.Gray
                };
                this.Controls.Add(labelPassInfo);
                y += 30;
            }
            else
            {
                y += 15;
            }

            var btnPanel = new Panel { Location = new Point(20, y + 10), Size = new Size(350, 40) };

            buttonSave = new Button { Text = Locale.Get("Save"), Location = new Point(70, 0), Size = new Size(100, 30), BackColor = Color.LightGreen };
            buttonSave.Click += ButtonSave_Click;

            buttonCancel = new Button { Text = Locale.Get("Cancel"), Location = new Point(180, 0), Size = new Size(100, 30), BackColor = Color.LightCoral };
            buttonCancel.Click += (s, e) => this.Close();

            btnPanel.Controls.Add(buttonSave);
            btnPanel.Controls.Add(buttonCancel);
            this.Controls.Add(btnPanel);
        }

        private void AddLabel(string text, int y, int w)
        {
            this.Controls.Add(new Label { Text = text, Location = new Point(10, y), Width = w, TextAlign = ContentAlignment.MiddleLeft });
        }

        private void FillForm()
        {
            textBoxUsername.Text = _user.Username;
            textBoxFullName.Text = _user.FullName;
            textBoxEmail.Text = _user.Email;
            comboBoxRole.SelectedItem = _user.Role.ToString();
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textBoxUsername.Text)) throw new Exception(Locale.Get("MsgFillAll"));
                if (!_isEditMode && string.IsNullOrWhiteSpace(textBoxPassword.Text)) throw new Exception(Locale.Get("MsgFillAll"));

                UserRole role = (UserRole)Enum.Parse(typeof(UserRole), comboBoxRole.SelectedItem.ToString());

                if (_isEditMode)
                {
                    _user.FullName = textBoxFullName.Text;
                    _user.Email = textBoxEmail.Text;
                    _user.Role = role;

                    _userService.UpdateUser(_user);

                    if (!string.IsNullOrWhiteSpace(textBoxPassword.Text))
                    {
                        _userService.ChangePassword(_user.Id, textBoxPassword.Text);
                    }
                }
                else
                {
                    _userService.Register(
                        textBoxUsername.Text,
                        textBoxPassword.Text,
                        textBoxFullName.Text,
                        textBoxEmail.Text,
                        role
                    );
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Locale.Get("MsgError")}: {ex.Message}", Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}