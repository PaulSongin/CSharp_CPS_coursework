using DrugCatalog_ver2.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class ChangePasswordForm : Form
    {
        private readonly IUserService _userService;
        private readonly int _userId;
        private TextBox textBoxCurrentPassword, textBoxNewPassword, textBoxConfirmPassword;
        private Button buttonSave, buttonCancel;

        public ChangePasswordForm(IUserService userService, int userId)
        {
            _userService = userService;
            _userId = userId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Смена пароля";
            this.Size = new Size(400, 250);
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

            AddPasswordField("Текущий пароль:", ref y, out textBoxCurrentPassword);
            AddPasswordField("Новый пароль:", ref y, out textBoxNewPassword);
            AddPasswordField("Подтвердите пароль:", ref y, out textBoxConfirmPassword);

            var buttonPanel = new Panel
            {
                Location = new Point(20, y + 20),
                Size = new Size(350, 40)
            };

            buttonSave = new Button { Text = "Сохранить", Location = new Point(80, 5), Size = new Size(100, 30) };
            buttonCancel = new Button { Text = "Отмена", Location = new Point(200, 5), Size = new Size(100, 30) };

            buttonSave.Click += (s, e) => ChangePassword();
            buttonCancel.Click += (s, e) => this.Close();

            buttonPanel.Controls.Add(buttonSave);
            buttonPanel.Controls.Add(buttonCancel);

            this.Controls.Add(buttonPanel);
        }

        private void AddPasswordField(string labelText, ref int y, out TextBox textBox)
        {
            var label = new Label { Text = labelText, Location = new Point(20, y), Size = new Size(150, 20) };
            textBox = new TextBox { Location = new Point(180, y), Size = new Size(180, 20), UseSystemPasswordChar = true };
            y += 30;

            this.Controls.Add(label);
            this.Controls.Add(textBox);
        }

        private void ChangePassword()
        {
            try
            {
                if (string.IsNullOrEmpty(textBoxCurrentPassword.Text) ||
                    string.IsNullOrEmpty(textBoxNewPassword.Text) ||
                    string.IsNullOrEmpty(textBoxConfirmPassword.Text))
                {
                    MessageBox.Show("Заполните все поля");
                    return;
                }

                if (textBoxNewPassword.Text != textBoxConfirmPassword.Text)
                {
                    MessageBox.Show("Пароли не совпадают");
                    return;
                }

                _userService.ChangePassword(_userId, textBoxNewPassword.Text);
                MessageBox.Show("Пароль успешно изменен");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}