using DrugCatalog_ver2.Models;
using System;
using System.IO;

namespace DrugCatalog_ver2.Services
{
    /// <summary>
    /// Расположение данных приложения. Папка находится вне папки сборки и вне репозитория,
    /// поэтому ни пересборка, ни команды git её не затрагивают.
    /// </summary>
    public static class AppPaths
    {
        public static string DataDirectory { get; private set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DrugCatalog");

        public static string DrugsFile => Path.Combine(DataDirectory, "drugs.xml");
        public static string RemindersFile => Path.Combine(DataDirectory, "reminders.xml");
        public static string UsersFile => Path.Combine(DataDirectory, "users.xml");
        public static string CategoriesFile => Path.Combine(DataDirectory, "categories.xml");
        public static string BackupDirectory => Path.Combine(DataDirectory, "Backups");

        /// <summary>
        /// Создаёт папку данных и копирует в неё данные прежних версий, которые лежали рядом с exe,
        /// если таких файлов там ещё нет. Исходные файлы не меняются.
        /// </summary>
        public static void PrepareDataDirectory()
        {
            try
            {
                Directory.CreateDirectory(DataDirectory);

                var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                CopyIfMissing(Path.Combine(exeDirectory, "Data", "drugs.xml"), DrugsFile);
                CopyIfMissing(Path.Combine(exeDirectory, "Data", "reminders.xml"), RemindersFile);
                CopyIfMissing(Path.Combine(exeDirectory, "users.xml"), UsersFile);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new DataFileException(string.Format(Locale.Get("MsgFileWrite"), DataDirectory, ex.Message), ex);
            }
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (File.Exists(destination) || !File.Exists(source)) return;

            // Итоговое имя файл получает только после полного копирования, чтобы сбой не оставил его обрезанным.
            var tempPath = destination + ".tmp";
            File.Copy(source, tempPath, true);
            File.Move(tempPath, destination);
        }
    }
}
