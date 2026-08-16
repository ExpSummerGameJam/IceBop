using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface ISheetDataGenerator
{
    public void StartSheetDataGenerate(string sheetName, string xlsxName, string sheetDataFilePath);
    public void SetSheetVariableType(string sheetName, Dictionary<int, string> dataType);
    public void FinishSheetDataGenerate(string sheetName);
    public void WriteRowData(string sheetName, Dictionary<int, string> rowData, HashSet<int> skipColumns);
}