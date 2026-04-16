using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GimFileInfo
{
    public string fileFlag;
    public string fileName;
    public string designer;
    public string organization;
    public string softName;
    public string softMajorVersion;
    public string softMinorVersion;
    public string specificationMajorVersion;
    public string specificationMinorVersion;
    public string createTime;
    public string dataSize;

    public override string ToString()
    {
        return string.Format("File Flag: {0}\nFile Name: {1}\nDesigner: {2}\nOrganization: {3}\nSoft Name: {4}\nSoft Version: {5} {6}\nSpecification Version: {7} {8}\nCreate Time: {9}\nData Size: {10}", fileFlag, fileName, designer, organization, softName, softMajorVersion, softMinorVersion, specificationMajorVersion, specificationMinorVersion, createTime, dataSize);
    }
}
