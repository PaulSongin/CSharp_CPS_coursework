using System.Collections.Generic;

public interface IXmlDataService
{
    List<Drug> LoadDrugs();
    void SaveDrugs(List<Drug> drugs);
    int GetNextId();
}