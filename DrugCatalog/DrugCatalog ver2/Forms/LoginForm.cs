using DrugCatalog_ver2.Services;
using DrugCatalog_ver2.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class LoginForm : Form
    {
        private readonly IUserService _userService;
        private TextBox textBoxUsername, textBoxPassword;
        private Button buttonLogin, buttonRegister;
        private CheckBox checkBoxRemember;
        private Label labelStatus;

        public User LoggedInUser { get; private set; }

        public LoginForm(IUserService userService)
        {
            _userService = userService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Вход в систему - Каталог препаратов";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Font = new Font("Microsoft Sans Serif", 10f);
            this.BackColor = Color.White;

            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            // Заголовок
            var labelTitle = new Label
            {
                Text = "Вход в систему",
                Font = new Font("Microsoft Sans Serif", 16f, FontStyle.Bold),
                ForeColor = Color.SteelBlue,
                Size = new Size(300, 40),
                Location = new Point(50, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Поле логина
            var labelUsername = new Label
            {
                Text = "Логин:",
                Location = new Point(50, 80),
                Size = new Size(100, 20)
            };

            textBoxUsername = new TextBox
            {
                Location = new Point(50, 105),
                Size = new Size(300, 30),
                Font = new Font("Microsoft Sans Serif", 10f)
            };
            textBoxUsername.Text = "admin"; // для тестирования

            // Поле пароля
            var labelPassword = new Label
            {
                Text = "Пароль:",
                Location = new Point(50, 145),
                Size = new Size(100, 20)
            };

            textBoxPassword = new TextBox
            {
                Location = new Point(50, 170),
                Size = new Size(300, 30),
                Font = new Font("Microsoft Sans Serif", 10f),
                UseSystemPasswordChar = true
            };
            textBoxPassword.Text = "admin123"; // для тестирования
            textBoxPassword.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                    AttemptLogin();
            };

            // Запомнить меня
            checkBoxRemember = new CheckBox
            {
                Text = "Запомнить меня",
                Location = new Point(50, 210),
                Size = new Size(150, 20),
                Checked = true
            };

            // Кнопка входа
            buttonLogin = new Button
            {
                Text = "Войти",
                Location = new Point(50, 250),
                Size = new Size(140, 35),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            buttonLogin.Click += (s, e) => AttemptLogin();

            // Кнопка регистрации
            buttonRegister = new Button
            {
                Text = "Регистрация",
                Location = new Point(210, 250),
                Size = new Size(140, 35),
                BackColor = Color.LightSlateGray,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            buttonRegister.Click += (s, e) => ShowRegistrationForm();

            // Статус
            labelStatus = new Label
            {
                Location = new Point(50, 300),
                Size = new Size(300, 20),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.AddRange(new Control[] {
                labelTitle, labelUsername, textBoxUsername,
                labelPassword, textBoxPassword, checkBoxRemember,
                buttonLogin, buttonRegister, labelStatus
            });
        }

        private void AttemptLogin()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textBoxUsername.Text) ||
                    string.IsNullOrWhiteSpace(textBoxPassword.Text))
                {
                    ShowError("Заполните все поля");
                    return;
                }

                LoggedInUser = _userService.Login(textBoxUsername.Text, textBoxPassword.Text);

                if (LoggedInUser != null)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ShowRegistrationForm()
        {
            var registerForm = new RegisterForm(_userService);
            if (registerForm.ShowDialog() == DialogResult.OK)
            {
                ShowSuccess("Регистрация успешна! Теперь вы можете войти.");
                textBoxUsername.Text = registerForm.RegisteredUsername;
                textBoxPassword.Text = "";
                textBoxPassword.Focus();
            }
        }

        private void ShowError(string message)
        {
            labelStatus.Text = message;
            labelStatus.ForeColor = Color.Red;
        }

        private void ShowSuccess(string message)
        {
            labelStatus.Text = message;
            labelStatus.ForeColor = Color.Green;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}