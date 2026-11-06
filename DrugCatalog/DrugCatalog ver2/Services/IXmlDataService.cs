using System.Collections.Generic;

public interface IXmlDataService
{
    /// <summary>Файл, с которым сейчас работает каталог.</summary>
    string CurrentFilePath { get; }

    List<Drug> LoadDrugs();
    void SaveDrugs(List<Drug> drugs);
    int GetNextId();

    /// <summary>Читает файл, не делая его текущим.</summary>
    List<Drug> ReadDrugs(string filePath);

    /// <summary>Делает файл текущим. Содержимое файла не меняется.</summary>
    void OpenFile(string filePath);

    /// <summary>Записывает список в файл и делает его текущим. Прежний файл не меняется.</summary>
    void SaveDrugsAs(List<Drug> drugs, string filePath);

    /// <summary>Возвращается к основному каталогу приложения.</summary>
    void OpenMainCatalog();
}
