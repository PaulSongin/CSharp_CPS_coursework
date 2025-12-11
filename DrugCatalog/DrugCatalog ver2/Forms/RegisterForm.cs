using DrugCatalog_ver2.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class RegisterForm : Form
    {
        private readonly IUserService _userService;
        private TextBox textBoxUsername, textBoxPassword, textBoxConfirmPassword;
        private TextBox textBoxFullName, textBoxEmail;
        private Button buttonRegister, buttonCancel;
        private Label labelStatus;

        public string RegisteredUsername { get; private set; }

        public RegisterForm(IUserService userService)
        {
            _userService = userService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = Locale.Get("TitleReg");
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Font = new Font("Microsoft Sans Serif", 10f);

            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            int y = 20;

            var labelTitle = new Label
            {
                Text = Locale.Get("HeaderReg"),
                Font = new Font("Microsoft Sans Serif", 14f, FontStyle.Bold),
                ForeColor = Color.SteelBlue,
                Size = new Size(400, 30),
                Location = new Point(25, y),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 40;

            AddTextField(Locale.Get("LblFullName"), ref y, out textBoxFullName);
            AddTextField(Locale.Get("LblEmail"), ref y, out textBoxEmail);
            AddTextField(Locale.Get("LblLoginReq"), ref y, out textBoxUsername);
            AddPasswordField(Locale.Get("LblPassReq"), ref y, out textBoxPassword);
            AddPasswordField(Locale.Get("LblConfPassReq"), ref y, out textBoxConfirmPassword);

            labelStatus = new Label
            {
                Location = new Point(25, y),
                Size = new Size(400, 20),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 30;

            var buttonPanel = new Panel { Location = new Point(25, y), Size = new Size(400, 40) };

            buttonRegister = new Button
            {
                Text = Locale.Get("BtnRegister"),
                Location = new Point(80, 5),
                Size = new Size(160, 30),
                BackColor = Color.LightGreen,
                Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Bold)
            };
            buttonRegister.Click += (s, e) => AttemptRegistration();

            buttonCancel = new Button
            {
                Text = Locale.Get("Cancel"),
                Location = new Point(260, 5),
                Size = new Size(100, 30),
                BackColor = Color.LightCoral
            };
            buttonCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            buttonPanel.Controls.Add(buttonRegister);
            buttonPanel.Controls.Add(buttonCancel);

            var tipLabel = new Label
            {
                Text = Locale.Get("LblRegTip"),
                Location = new Point(25, y + 45),
                Size = new Size(400, 20),
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            this.Controls.AddRange(new Control[] { labelTitle, textBoxFullName, textBoxEmail, textBoxUsername, textBoxPassword, textBoxConfirmPassword, labelStatus, buttonPanel, tipLabel });
        }

        private void AddTextField(string labelText, ref int y, out TextBox textBox)
        {
            var label = new Label { Text = labelText, Location = new Point(25, y), Size = new Size(150, 20) };
            textBox = new TextBox { Location = new Point(180, y), Size = new Size(240, 25) };
            y += 35;
            this.Controls.Add(label);
            this.Controls.Add(textBox);
        }

        private void AddPasswordField(string labelText, ref int y, out TextBox textBox)
        {
            var label = new Label { Text = labelText, Location = new Point(25, y), Size = new Size(150, 20) };
            textBox = new TextBox { Location = new Point(180, y), Size = new Size(240, 25), UseSystemPasswordChar = true };
            y += 35;
            this.Controls.Add(label);
            this.Controls.Add(textBox);
        }

        private void AttemptRegistration()
        {
            try
            {
                if (!ValidateForm()) return;
                var user = _userService.Register(textBoxUsername.Text.Trim(), textBoxPassword.Text, textBoxFullName.Text.Trim(), textBoxEmail.Text.Trim());
                RegisteredUsername = user.Username;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(textBoxUsername.Text)) { ShowError(Locale.Get("MsgFillAll")); return false; }
            if (string.IsNullOrWhiteSpace(textBoxPassword.Text)) { ShowError(Locale.Get("MsgFillAll")); return false; }
            if (textBoxPassword.Text != textBoxConfirmPassword.Text) { ShowError(Locale.Get("MsgPassMismatch")); return false; }
            if (!_userService.ValidatePassword(textBoxPassword.Text)) { ShowError(Locale.Get("MsgPassLen")); return false; }
            return true;
        }

        private void ShowError(string message) { labelStatus.Text = message; labelStatus.ForeColor = Color.Red; }
    }
}