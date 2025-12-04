using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

public class XmlDataService : IXmlDataService
{
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "drugs.xml");

    public List<Drug> LoadDrugs()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new List<Drug>();

            var serializer = new XmlSerializer(typeof(List<Drug>), new XmlRootAttribute("Drugs"));
            using (var stream = new FileStream(_filePath, FileMode.Open))
            {
                return (List<Drug>)serializer.Deserialize(stream);
            }
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            return new List<Drug>();
        }
    }

    public void SaveDrugs(List<Drug> drugs)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var serializer = new XmlSerializer(typeof(List<Drug>), new XmlRootAttribute("Drugs"));
            using (var stream = new FileStream(_filePath, FileMode.Create))
            {
                serializer.Serialize(stream, drugs);
            }
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show($"Ошибка сохранения данных: {ex.Message}");
        }
    }

    public int GetNextId()
    {
        var drugs = LoadDrugs();
        return drugs.Count > 0 ? drugs.Max(d => d.Id) + 1 : 1;
    }
}