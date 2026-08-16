using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

public class ClassGenerator : IClassGenerator
{
    public void GenerateXlsxClass(string dataClassFilePath, string xlsxName, string[] sheetNames)
    {
        StringBuilder sb = new();
        string path = Path.Combine(dataClassFilePath, xlsxName);

        BuildClassDefault(xlsxName, sb, true); // Sheet 기본 틀 제작
        foreach (var sheetName in sheetNames) // Sheet 데이터를 저장하기 위한 List<SheetName> 들을 생성
            sb.AppendLine($"    public static {xlsxName}_{sheetName}[] {sheetName};");

        FinishClass(xlsxName, path, sb); // Sheet.cs 마무리, 생성
    }

    public void GenerateSheetClass(string dataClassFilePath, string xlsxName, string sheetName, HashSet<int> skipColumns, params Dictionary<int, string>[] infoParameters)
    {
        GenerateSheetClass(dataClassFilePath, xlsxName, sheetName, skipColumns, infoParameters[0], infoParameters[1], infoParameters[2]);
    }

    private void GenerateSheetClass(string dataClassFilePath, string xlsxName, string sheetName, HashSet<int> skipColumns, Dictionary<int, string> descriptionPair, Dictionary<int, string> dataTypePair, Dictionary<int, string> dataNamePair)
    {
        StringBuilder sb = new();

        BuildClassDefault($"{xlsxName}_{sheetName}", sb);

        string path = Path.Combine(dataClassFilePath, xlsxName, "sheet");

        // 각 필드 생성
        foreach (var dataType in dataTypePair)
        {
            try
            {
                int key = dataType.Key;

                if (skipColumns.Contains(key))
                    continue; // #처리된 열은 스킵

                // 설명이 있다면 주석 추가
                if (descriptionPair.ContainsKey(key) && !string.IsNullOrEmpty(descriptionPair[key]))
                {
                    sb.AppendLine($"    /// <summary>");
                    sb.AppendLine($"    /// {descriptionPair[key]}");
                    sb.AppendLine($"    /// </summary>");
                }
                // 필드 선언
                sb.AppendLine($"    public {dataType.Value} {dataNamePair[key]};");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"{dataType.Key} 에 {dataType.Value} {ex.Message}");
            }
        }

        FinishClass($"{xlsxName}_{sheetName}", path, sb);
    }

    /// <summary>
    /// Class 내용의 기본 세팅
    /// </summary>
    private void BuildClassDefault(string className, StringBuilder sb, bool isStatic = false)
    {
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.Append("\n");
        sb.AppendLine("[System.Serializable]");
        sb.AppendLine($"public {(isStatic ? "static" : "")} class {className}");
        sb.AppendLine("{");
    }

    /// <summary>
    /// Class를 닫고 cs 파일을 생성한다.
    /// </summary>
    private void FinishClass(string className, string path, StringBuilder sb)
    {
        sb.AppendLine("}");

        string filePath = $"{path}/{className}.cs";

        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        if (File.Exists(filePath))
            File.Delete(filePath);

        File.Create(filePath).Close();
        File.WriteAllText(filePath, sb.ToString());
    }
}