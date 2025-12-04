using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
[XmlRoot("Drug")]
public class Drug
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ActiveSubstance { get; set; }
    public string Manufacturer { get; set; }
    public string Form { get; set; }
    public decimal Dosage { get; set; }
    public string DosageUnit { get; set; }
    public string PrescriptionType { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }

    [XmlArray("Indications")]
    [XmlArrayItem("Indication")]
    public List<string> Indications { get; set; }

    [XmlArray("Contraindications")]
    [XmlArrayItem("Contraindication")]
    public List<string> Contraindications { get; set; }

    public Drug()
    {
        Indications = new List<string>();
        Contraindications = new List<string>();
    }
}