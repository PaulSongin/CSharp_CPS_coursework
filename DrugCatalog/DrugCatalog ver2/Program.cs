using DrugCatalog_ver2.Forms;
using DrugCatalog_ver2.Models;
using System;
using System.Windows.Forms;

namespace DrugCatalog_ver2
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Инициализация сервисов
            var xmlDataService = new XmlDataService();
            var userService = new UserService(xmlDataService);

            // Показываем форму входа
            using (var loginForm = new LoginForm(userService))
            {
                if (loginForm.ShowDialog() == DialogResult.OK && loginForm.LoggedInUser != null)
                {
                    // Запускаем главную форму с авторизованным пользователем
                    var mainForm = new MainForm(xmlDataService, userService, loginForm.LoggedInUser);
                    Application.Run(mainForm);
                }
                else
                {
                    Application.Exit();
                }
            }
        }
    }
}