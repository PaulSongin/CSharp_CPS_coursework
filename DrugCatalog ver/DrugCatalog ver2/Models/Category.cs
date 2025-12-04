using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    [XmlIgnore]
    public List<Drug> Drugs { get; set; } = new List<Drug>();
}