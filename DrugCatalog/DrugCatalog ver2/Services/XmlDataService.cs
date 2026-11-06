using DrugCatalog_ver2.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

public class XmlDataService : IXmlDataService
{
    private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(List<Drug>), new XmlRootAttribute("Drugs"));

    public string CurrentFilePath { get; private set; } = AppPaths.DrugsFile;

    public List<Drug> LoadDrugs()
    {
        return File.Exists(CurrentFilePath) ? ReadDrugs(CurrentFilePath) : new List<Drug>();
    }

    public List<Drug> ReadDrugs(string filePath)
    {
        return SafeXmlFile.Read<List<Drug>>(filePath, Serializer) ?? new List<Drug>();
    }

    public void SaveDrugs(List<Drug> drugs)
    {
        SafeXmlFile.Write(CurrentFilePath, Serializer, drugs);
    }

    public void SaveDrugsAs(List<Drug> drugs, string filePath)
    {
        SafeXmlFile.Write(filePath, Serializer, drugs);
        CurrentFilePath = filePath;
    }

    public void OpenFile(string filePath)
    {
        CurrentFilePath = filePath;
    }

    public void OpenMainCatalog()
    {
        CurrentFilePath = AppPaths.DrugsFile;
    }

    public int GetNextId()
    {
        var drugs = LoadDrugs();
        return drugs.Count > 0 ? drugs.Max(d => d.Id) + 1 : 1;
    }
}
