using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IClassGenerator
{
    public void GenerateXlsxClass(string dataClassFilePath, string xlsxName, string[] sheetNames);
    public void GenerateSheetClass(string dataClassFilePath, string xlsxName, string sheetName, HashSet<int> skipColumns, params Dictionary<int, string>[] infoParameters);
}