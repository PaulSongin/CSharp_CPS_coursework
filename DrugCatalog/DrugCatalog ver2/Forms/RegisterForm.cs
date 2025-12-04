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

            this.Text = "Регистрация нового пользователя";
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

            // Заголовок
            var labelTitle = new Label
            {
                Text = "Регистрация",
                Font = new Font("Microsoft Sans Serif", 14f, FontStyle.Bold),
                ForeColor = Color.SteelBlue,
                Size = new Size(400, 30),
                Location = new Point(25, y),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 40;

            // Полное имя
            AddTextField("Полное имя:", ref y, out textBoxFullName);

            // Email
            AddTextField("Email:", ref y, out textBoxEmail);

            // Логин
            AddTextField("Логин*:", ref y, out textBoxUsername);

            // Пароль
            AddPasswordField("Пароль*:", ref y, out textBoxPassword);

            // Подтверждение пароля
            AddPasswordField("Подтверждение пароля*:", ref y, out textBoxConfirmPassword);

            // Статус
            labelStatus = new Label
            {
                Location = new Point(25, y),
                Size = new Size(400, 20),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 30;

            // Кнопки
            var buttonPanel = new Panel
            {
                Location = new Point(25, y),
                Size = new Size(400, 40)
            };

            buttonRegister = new Button
            {
                Text = "Зарегистрироваться",
                Location = new Point(80, 5),
                Size = new Size(150, 30),
                BackColor = Color.LightGreen,
                Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Bold)
            };
            buttonRegister.Click += (s, e) => AttemptRegistration();

            buttonCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(250, 5),
                Size = new Size(100, 30),
                BackColor = Color.LightCoral
            };
            buttonCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            buttonPanel.Controls.Add(buttonRegister);
            buttonPanel.Controls.Add(buttonCancel);

            // Подсказка
            var tipLabel = new Label
            {
                Text = "* - обязательные поля. Пароль должен содержать минимум 6 символов.",
                Location = new Point(25, y + 45),
                Size = new Size(400, 20),
                Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            this.Controls.AddRange(new Control[] {
                labelTitle, textBoxFullName, textBoxEmail, textBoxUsername,
                textBoxPassword, textBoxConfirmPassword, labelStatus,
                buttonPanel, tipLabel
            });
        }

        private void AddTextField(string labelText, ref int y, out TextBox textBox)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(25, y),
                Size = new Size(150, 20)
            };

            textBox = new TextBox
            {
                Location = new Point(180, y),
                Size = new Size(240, 25)
            };
            y += 35;

            this.Controls.Add(label);
        }

        private void AddPasswordField(string labelText, ref int y, out TextBox textBox)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(25, y),
                Size = new Size(150, 20)
            };

            textBox = new TextBox
            {
                Location = new Point(180, y),
                Size = new Size(240, 25),
                UseSystemPasswordChar = true
            };
            y += 35;

            this.Controls.Add(label);
        }

        private void AttemptRegistration()
        {
            try
            {
                if (!ValidateForm())
                    return;

                var user = _userService.Register(
                    textBoxUsername.Text.Trim(),
                    textBoxPassword.Text,
                    textBoxFullName.Text.Trim(),
                    textBoxEmail.Text.Trim()
                );

                RegisteredUsername = user.Username;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(textBoxUsername.Text))
            {
                ShowError("Введите логин");
                textBoxUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(textBoxPassword.Text))
            {
                ShowError("Введите пароль");
                textBoxPassword.Focus();
                return false;
            }

            if (textBoxPassword.Text != textBoxConfirmPassword.Text)
            {
                ShowError("Пароли не совпадают");
                textBoxConfirmPassword.Focus();
                return false;
            }

            if (!_userService.ValidatePassword(textBoxPassword.Text))
            {
                ShowError("Пароль должен содержать минимум 6 символов");
                textBoxPassword.Focus();
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            labelStatus.Text = message;
            labelStatus.ForeColor = Color.Red;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Enter:
                    AttemptRegistration();
                    return true;
                case Keys.Escape:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}