using DrugCatalog_ver2.Services;
using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DrugCatalog_ver2.Forms
{
    public partial class UserManagementForm : Form
    {
        private readonly IUserService _userService;
        private DataGridView dataGridViewUsers;
        private Button buttonAdd, buttonEdit, buttonDelete, buttonClose;

        public UserManagementForm(IUserService userService)
        {
            _userService = userService;
            InitializeComponent();
            LoadUsers();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Управление пользователями";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft Sans Serif", 9f);

            CreateControls();

            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            dataGridViewUsers = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(10, 10),
                Size = new Size(760, 400)
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            buttonAdd = new Button { Text = "Добавить", Size = new Size(100, 30), Location = new Point(10, 10) };
            buttonEdit = new Button { Text = "Редактировать", Size = new Size(100, 30), Location = new Point(120, 10) };
            buttonDelete = new Button { Text = "Удалить", Size = new Size(100, 30), Location = new Point(230, 10) };
            buttonClose = new Button { Text = "Закрыть", Size = new Size(100, 30), Location = new Point(340, 10) };

            buttonAdd.Click += (s, e) => AddUser();
            buttonEdit.Click += (s, e) => EditUser();
            buttonDelete.Click += (s, e) => DeleteUser();
            buttonClose.Click += (s, e) => this.Close();

            buttonPanel.Controls.AddRange(new Control[] { buttonAdd, buttonEdit, buttonDelete, buttonClose });

            this.Controls.Add(dataGridViewUsers);
            this.Controls.Add(buttonPanel);
        }

        private void LoadUsers()
        {
            var users = _userService.GetAllUsers();

            dataGridViewUsers.Columns.Clear();
            dataGridViewUsers.Columns.Add("Id", "ID");
            dataGridViewUsers.Columns.Add("Username", "Логин");
            dataGridViewUsers.Columns.Add("FullName", "ФИО");
            dataGridViewUsers.Columns.Add("Email", "Email");
            dataGridViewUsers.Columns.Add("Role", "Роль");
            dataGridViewUsers.Columns.Add("CreatedAt", "Дата регистрации");
            dataGridViewUsers.Columns.Add("LastLogin", "Последний вход");

            dataGridViewUsers.Rows.Clear();
            foreach (var user in users)
            {
                dataGridViewUsers.Rows.Add(
                    user.Id,
                    user.Username,
                    user.FullName,
                    user.Email,
                    user.Role.ToString(),
                    user.CreatedAt.ToString("dd.MM.yyyy"),
                    user.LastLogin.ToString("dd.MM.yyyy HH:mm")
                );
            }
        }

        private void AddUser()
        {
            // Реализуй форму добавления пользователя
        }

        private void EditUser()
        {
            // Реализуй форму редактирования пользователя
        }

        private void DeleteUser()
        {
            if (dataGridViewUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите пользователя для удаления");
                return;
            }

            var userId = (int)dataGridViewUsers.SelectedRows[0].Cells["Id"].Value;
            var username = dataGridViewUsers.SelectedRows[0].Cells["Username"].Value.ToString();

            if (MessageBox.Show($"Удалить пользователя '{username}'?", "Подтверждение",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    _userService.DeleteUser(userId);
                    LoadUsers();
                    MessageBox.Show("Пользователь удален");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }
    }
}