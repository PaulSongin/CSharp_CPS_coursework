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

            XmlDataService xmlDataService;
            UserService userService;
            try
            {
                AppPaths.PrepareDataDirectory();
                xmlDataService = new XmlDataService();
                userService = new UserService(xmlDataService);
            }
            catch (DataFileException ex)
            {
                MessageBox.Show(ex.Message, Locale.Get("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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
