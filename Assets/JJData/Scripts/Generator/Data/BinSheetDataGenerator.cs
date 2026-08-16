using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public class BinSheetDataGenerator : ISheetDataGenerator
{
    private ConcurrentDictionary<string, FileStream> _writer = new();
    private ConcurrentDictionary<string, int> _sheetRowNum = new();
    private ConcurrentDictionary<string, Dictionary<int, string>> _sheetVariableType = new();
    private String2TypeByteConverter _converter = new();

    ~BinSheetDataGenerator()
    {
        if (_writer != null)
        {
            foreach(var a in  _writer)
            {
                a.Value.Dispose();
            }
        }
    }

    public void StartSheetDataGenerate(string sheetName, string xlsxName, string sheetDataFilePath)
    {
        _converter = new();
        _sheetVariableType[sheetName] = new();
        _sheetRowNum[sheetName] = 0;
        string path = $"{sheetDataFilePath}/{xlsxName}/{sheetName}.bytes";

        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }
        if (!File.Exists(path))
        {
            var a = File.Create(path);
            a.Dispose();
        }
        else
        {
            File.Delete(path);
        }

        FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        _writer[sheetName] = fs;
        _writer[sheetName].Write(_converter.Convert("long", "0"));
    }

    public void SetSheetVariableType(string sheetName, Dictionary<int, string> dataType)
    {
        _sheetVariableType[sheetName] = dataType;
    }

    public void FinishSheetDataGenerate(string sheetName)
    {
        _writer.Remove(sheetName, out var writer);
        writer.Seek(0, SeekOrigin.Begin);
        writer.Write(_converter.Convert("long", _sheetRowNum[sheetName].ToString()));
        writer.Dispose();
    }

    public void WriteRowData(string sheetName, Dictionary<int, string> rowData, HashSet<int> skipColumns)
    {
        _sheetRowNum[sheetName] += 1;
        foreach (var cellValue in rowData)
        {
            try
            {
                if (skipColumns.Contains(cellValue.Key))
                    continue;
                byte[] dataBytes = _converter.Convert(_sheetVariableType[sheetName][cellValue.Key], cellValue.Value);
                _writer[sheetName].Write(dataBytes);
            }
            catch(Exception ex)
            {
                UnityEngine.Debug.LogWarning($"{cellValue.Key} 에 {cellValue.Value} {ex.Message}");
            }
        }
    }
}