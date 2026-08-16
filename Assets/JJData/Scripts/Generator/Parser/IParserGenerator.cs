using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public interface IParserGenerator
{
    public void StartDataParserGenerate(string xlsxName, string[] sheetNames, string dataFilePath);
    public void BuildSetXlsxDataMethod(StringBuilder sb, string[] sheetNames);
    public void FinishDataParserGenerate(string xlsxName, string dataClassPath);

    public void GenerateSheetDataParserMethod(string xlsxName, string sheetName, Dictionary<int, string> dataTypePair, Dictionary<int, string> dataNamePair, HashSet<int> skipColumns);
    public void BuildClassDefault(string className, StringBuilder sb, bool isStatic = false);
    public void FinishClass(string className, string path, StringBuilder sb);
}