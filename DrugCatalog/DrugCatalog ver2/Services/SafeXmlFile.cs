using DrugCatalog_ver2.Models;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DrugCatalog_ver2.Services
{
    /// <summary>Ошибка чтения или записи файла данных. Текст можно показывать пользователю.</summary>
    public class DataFileException : Exception
    {
        public DataFileException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Чтение и запись XML-файлов данных. Новая версия сначала пишется во временный файл и только потом
    /// заменяет старую, а старая копируется в резервные копии, поэтому сбой записи не портит данные.
    /// </summary>
    public static class SafeXmlFile
    {
        private const int MaxBackupsPerFile = 50;

        public static T Read<T>(string path, XmlSerializer serializer)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return (T)serializer.Deserialize(stream);
                }
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                throw new DataFileException(string.Format(Locale.Get("MsgFileRead"), path, GetReason(ex)), ex);
            }
        }

        public static void Write<T>(string path, XmlSerializer serializer, T value)
        {
            // Файл, который не читается, не заменяем: в нём могут оставаться данные пользователя.
            bool fileExists = File.Exists(path);
            if (fileExists) Read<T>(path, serializer);

            var tempPath = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serializer.Serialize(stream, value);
                }

                if (fileExists)
                {
                    BackUp(path);
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                TryDelete(tempPath);
                throw new DataFileException(string.Format(Locale.Get("MsgFileWrite"), path, GetReason(ex)), ex);
            }
        }

        private static void BackUp(string path)
        {
            Directory.CreateDirectory(AppPaths.BackupDirectory);
            var name = Path.GetFileNameWithoutExtension(path);
            File.Copy(path, Path.Combine(AppPaths.BackupDirectory, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.xml"), true);

            var outdatedBackups = new DirectoryInfo(AppPaths.BackupDirectory).GetFiles(name + "_*.xml")
                .OrderByDescending(file => file.CreationTimeUtc)
                .Skip(MaxBackupsPerFile);
            foreach (var backup in outdatedBackups)
            {
                TryDelete(backup.FullName);
            }
        }

        // Оставшийся лишний файл не мешает работе, поэтому ошибка удаления не прерывает сохранение.
        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (Exception ex) when (IsFileError(ex)) { }
        }

        // XmlSerializer сообщает об испорченном XML через InvalidOperationException.
        private static bool IsFileError(Exception ex)
        {
            return ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException;
        }

        private static string GetReason(Exception ex)
        {
            return ex.InnerException?.Message ?? ex.Message;
        }
    }
}
