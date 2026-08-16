using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ParserGenerator : IParserGenerator
{
    StringBuilder _xlsxSb = new();
    ConcurrentDictionary<string, StringBuilder> _sheetSb = new();

    public void StartDataParserGenerate(string xlsxName, string[] sheetNames, string dataFilePath)
    {
        _xlsxSb = new();
        string className = $"{xlsxName}DataParser";
        BuildClassDefault(className, _xlsxSb, true);
        _xlsxSb.AppendLine($"    readonly private static string _dataFilePath = \"{dataFilePath}\";");

        BuildSetXlsxDataMethod(_xlsxSb, sheetNames);
    }

    public void BuildSetXlsxDataMethod(StringBuilder sb, string[] sheetNames)
    {
        sb.AppendLine("    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
        sb.AppendLine("    public static void SetXlsxData()");
        sb.AppendLine("    {");
        foreach (string sheetName in sheetNames)
        {
            sb.AppendLine($"        ConvertRowData2{sheetName}();");
        }
        sb.AppendLine("    }");
    }

    public void FinishDataParserGenerate(string xlsxName, string dataClassPath)
    {
        foreach (var sheetSb in _sheetSb.Values)
        {
            _xlsxSb.AppendLine(sheetSb.ToString());
        }

        string className = $"{xlsxName}DataParser";
        string dataParserPath = Path.Combine(dataClassPath, xlsxName, "Parser");
        FinishClass(className, dataParserPath, _xlsxSb);
    }

    public void GenerateSheetDataParserMethod(string xlsxName, string sheetName, Dictionary<int, string> dataTypePair, Dictionary<int, string> dataNamePair, HashSet<int> skipColumns)
    {
        StringBuilder sb = new();
        sb.AppendLine($"    public static void ConvertRowData2{sheetName}()");
        sb.AppendLine("    {");
        sb.AppendLine("        int stringLength = 0;");
        sb.AppendLine($"        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, \"{xlsxName}/{sheetName}\"));");
        sb.AppendLine("        Span<byte> buffer = binAsset.bytes;");
        sb.AppendLine("        int offset = 0;");
        sb.AppendLine("        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));");
        sb.AppendLine("        offset += 8;");
        sb.AppendLine($"        {xlsxName}.{sheetName} = new {xlsxName}_{sheetName}[rows];");
        sb.AppendLine("        for(int row = 0; row < rows; row++)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {xlsxName}_{sheetName} sheetRowData = new();");

        foreach (var dataName in dataNamePair)
        {
            try
            {
                if (skipColumns.Contains(dataName.Key))
                    continue;
                if (dataTypePair[dataName.Key] == "string")
                    BuildStringSetter(sb, dataName.Value);
                else
                    BuildVariableSetter(sb, String2TypeByteConverter.TypeByteLength[dataTypePair[dataName.Key]], dataName.Value, dataTypePair[dataName.Key]);
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"{dataName.Key}와 {dataName.Value}에 오류 발생 {ex.Message}");
            }
        }

        sb.AppendLine($"            {xlsxName}.{sheetName}[row] = sheetRowData;");
        sb.AppendLine("        }");
        sb.AppendLine("        Resources.UnloadAsset(binAsset);");
        sb.AppendLine("    }");

        _sheetSb[sheetName] = sb;
    }

    private void BuildStringSetter(StringBuilder sb, string dataName)
    {
        sb.AppendLine($"            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));");
        sb.AppendLine("            offset += 4;");
        sb.AppendLine($"            sheetRowData.{dataName} = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));");
        sb.AppendLine("            offset += stringLength;");
    }

    private void BuildVariableSetter(StringBuilder sb, int byteLength, string dataName, string dataType)
    {
        sb.AppendLine($"            sheetRowData.{dataName} = TypeByte2TypeConverter.ConvertTypeByte2{dataType}(buffer.Slice(offset, {byteLength}));");
        sb.AppendLine($"            offset += {byteLength};");
    }

    public void BuildClassDefault(string className, StringBuilder sb, bool isStatic = false)
    {
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System;");
        sb.Append("\n\n\n\n");
        sb.AppendLine($"public {(isStatic ? "static" : "")} class {className}");
        sb.AppendLine("{");
    }

    public void FinishClass(string className, string path, StringBuilder sb)
    {
        sb.AppendLine("}");

        string filePath = $"{path}/{className}.cs";

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        if (File.Exists(filePath))
            File.Delete(filePath);

        File.WriteAllText(filePath, sb.ToString());
    }
}
