using DrugCatalog_ver2.Services;
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

            var xmlDataService = new XmlDataService();
            var userService = new UserService(xmlDataService);

            using (var loginForm = new LoginForm(userService))
            {
                if (loginForm.ShowDialog() == DialogResult.OK && loginForm.LoggedInUser != null)
                {
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