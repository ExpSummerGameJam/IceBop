using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Threading.Tasks;
using UnityEngine;

public class XlsxDataProcessor : IDataProcessor
{
    /// <summary>
    /// Xlsx을 데이터를 가공한다.
    /// </summary>
    /// <param name="xlsxFilePath">Xlsx를 읽을 경로/param>
    /// <param name="dataFilePath">가공한 Data가 저장 될 위치/param>
    /// <param name="dataGenerator">Process한 Data를 처리할 Data Generator/param>
    /// <param name="maxSheetsAtOnce">한번에 처리할 Sheet 갯수</param>
    /// <returns></returns>
    public async Task ProcessData(string xlsxFilePath, string dataClassFilePath, string dataFilePath, string resourcesFilePath, IClassGenerator classGenerator, ISheetDataGenerator dataGenerator, IParserGenerator parserGenerator, int maxSheetsAtOnce = 2)
    {
        Debug.Log($"Processor - {Path.GetFileName(xlsxFilePath)} : 작업 시작");
        string xlsxName = Path.GetFileNameWithoutExtension(xlsxFilePath);
        string tempPath = Path.GetTempFileName() + ".xlsx";
        File.Copy(xlsxFilePath, tempPath, true);

        Dictionary<int, string> sharedStringCache = new();
        string sharedStringsFilePath;

        try
        {
            // Zip 파일을 임시 폴더에 압축 해제
            string extractPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            ZipFile.ExtractToDirectory(tempPath, extractPath);

            // sharedStrings.xml의 경로 저장
            sharedStringsFilePath = Path.Combine(extractPath, "xl", "sharedStrings.xml");
            // sharedStrings 인덱스 생성
            BuildSharedStringsCache();

            // 워크북에서 시트 정보 추출
            string workbookPath = Path.Combine(extractPath, "xl", "workbook.xml");
            List<(string sheetName, string sheetPath)> sheets = GetSheetInfo(workbookPath, extractPath);

            // 데이터 틀이 되는 Xlsx Class 생성
            string[] sheetNames = sheets.Select(sheet => sheet.sheetName).ToArray();
            classGenerator.GenerateXlsxClass(dataClassFilePath, xlsxName, sheetNames); // XlsxClass 생성
            parserGenerator.StartDataParserGenerate(xlsxName, sheetNames, resourcesFilePath);
            ViewerGenerator viewerGenerator = new();
            viewerGenerator.StartViewerGenerate(xlsxName, sheetNames, dataClassFilePath);

            // 시트 처리 개수를 제한하기 위한 세마포어 생성
            SemaphoreSlim semaphore = new(maxSheetsAtOnce);
            await Task.WhenAll(sheets.Select(async sheet =>
            {
                await semaphore.WaitAsync();
                try
                {
                    ProcessSheetClassInfo(sheet);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
            parserGenerator.FinishDataParserGenerate(xlsxName, dataClassFilePath);
        }
        finally
        {
            File.Delete(tempPath);
            Debug.Log($"Processor - {Path.GetFileName(xlsxFilePath)} : 작업 끝");
        }
        #region Local


        // sharedStrings 인덱스 생성
        void BuildSharedStringsCache()
        {
            if (!File.Exists(sharedStringsFilePath))
                return;

            using FileStream fs = new(sharedStringsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using XmlReader reader = XmlReader.Create(fs, new XmlReaderSettings { IgnoreWhitespace = true, IgnoreComments = true });

            int index = 0; // 공유 문자열의 인덱스
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "si")
                {
                    if (reader.ReadToDescendant("t"))
                    {
                        string value = reader.ReadElementContentAsString();
                        sharedStringCache[index++] = value;
                    }
                }
            }
        }

        void ProcessSheetClassInfo((string sheetName, string sheetFilePath) sheetInfo)
        {
            using XmlReader sheetReader = XmlReader.Create(sheetInfo.sheetFilePath, new XmlReaderSettings { IgnoreWhitespace = true, IgnoreComments = true });

            // 필요한 데이터 구조 초기화
            Dictionary<int, string> descriptionPair = null;
            Dictionary<int, string> dataTypePair = null;
            Dictionary<int, string> dataNamePair = null;
            HashSet<int> skipColumns = new();

            dataGenerator.StartSheetDataGenerate(sheetInfo.sheetName, xlsxName, dataFilePath + resourcesFilePath);

            int currentRow = 0;
            int contextStartRow = (int)DataForm.Context;
            while (sheetReader.Read())
            {
                if (sheetReader.NodeType == XmlNodeType.Element && sheetReader.LocalName == "row")
                {
                    var currentRowData = ExtractRowData(sheetReader);
                    currentRow++;
                    if (currentRow == (int)DataForm.Description) // Description
                    {
                        descriptionPair = currentRowData;
                        foreach (var pair in descriptionPair)
                        {
                            if (pair.Value.StartsWith("#")) // #으로 시작시 스킵
                            {
                                if (!skipColumns.Contains(pair.Key))
                                    skipColumns.Add(pair.Key);
                            }
                        }
                    }
                    else if (currentRow == (int)DataForm.DataType) // DataType
                    {
                        dataTypePair = currentRowData;
                        foreach (var pair in dataTypePair)
                        {
                            if (pair.Value.StartsWith("#")) // #으로 시작시 스킵
                            {
                                if (!skipColumns.Contains(pair.Key))
                                    skipColumns.Add(pair.Key);
                            }
                            if (pair.Value.Trim() == "")
                            {
                                if (!skipColumns.Contains(pair.Key))
                                    skipColumns.Add(pair.Key);
                            }
                        }
                    }
                    else if (currentRow == (int)DataForm.DataName) // DataName
                    {
                        dataNamePair = currentRowData;
                        foreach (var pair in dataNamePair)
                        {
                            if (pair.Value.StartsWith("#")) // #으로 시작시 스킵
                            {
                                if (!skipColumns.Contains(pair.Key))
                                    skipColumns.Add(pair.Key);
                            }
                            if (pair.Value.Trim() == "")
                            {
                                if (!skipColumns.Contains(pair.Key))
                                    skipColumns.Add(pair.Key);
                            }
                        }

                        // SheetInfo 수집이 전부 끝났기에 SheetClass 생성
                        dataGenerator.SetSheetVariableType(sheetInfo.sheetName, dataTypePair);
                        classGenerator.GenerateSheetClass(dataClassFilePath, xlsxName, sheetInfo.sheetName, skipColumns, descriptionPair, dataTypePair, dataNamePair);
                        parserGenerator.GenerateSheetDataParserMethod(xlsxName, sheetInfo.sheetName, dataTypePair, dataNamePair, skipColumns);
                    }
                    else if (currentRow >= contextStartRow)
                    {
                        var rowData = currentRowData;
                        // SkipColumns에 해당하는 열은 제외
                        foreach (var skipColumn in skipColumns)
                        {
                            rowData.Remove(skipColumn);
                        }

                        int emptyData = 0;
                        foreach (var data in rowData)
                        {
                            if (data.Value.Trim() == "")
                                emptyData++;
                        }
                        if (rowData.Count == emptyData)
                            continue;

                        dataGenerator.WriteRowData(sheetInfo.sheetName, rowData, skipColumns);
                    }
                }
            }

            dataGenerator.FinishSheetDataGenerate(sheetInfo.sheetName);
        }

        // 특정 인덱스의 sharedString을 가져옴
        string GetSharedString(int index)
        {
            if (index < 0 || index >= sharedStringCache.Count)
                throw new IndexOutOfRangeException($"SharedString with index {index} not found.");

            return sharedStringCache[index];
        }



        // 행 데이터를 추출
        Dictionary<int, string> ExtractRowData(XmlReader reader)
        {
            Dictionary<int, string> rowData = new();
            while (reader.Read() && (reader.NodeType != XmlNodeType.EndElement || reader.LocalName != "row"))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "c")
                {
                    string cellType = reader.GetAttribute("t");
                    string cellRef = reader.GetAttribute("r");
                    int cellIndex = ConvertColumnToIndex(cellRef);
                    string cellValue = "";

                    if (cellType == "s")
                    {
                        if (reader.ReadToDescendant("v"))
                        {
                            int sharedStringIndex = int.Parse(reader.ReadElementContentAsString());
                            cellValue = GetSharedString(sharedStringIndex);
                        }
                    }
                    else if (cellType == "inlineStr")
                    {
                        if (reader.ReadToDescendant("t"))
                            cellValue = reader.ReadElementContentAsString();
                    }
                    else if (reader.ReadToDescendant("v"))
                    {
                        cellValue = reader.ReadElementContentAsString();
                    }

                    rowData[cellIndex] = cellValue;
                }
            }
            return rowData;
        }

        // 시트 정보를 가져옴
        List<(string sheetName, string sheetPath)> GetSheetInfo(string workbookPath, string extractPath)
        {
            using XmlReader reader = XmlReader.Create(workbookPath, new() { IgnoreWhitespace = true, IgnoreComments = true });
            List<(string sheetName, string sheetPath)> sheets = new();

            // 관계 파일 로드
            string relsPath = Path.Combine(Path.GetDirectoryName(workbookPath), "_rels", "workbook.xml.rels");
            Dictionary<string, string> sheetIdToTarget = new();

            if (File.Exists(relsPath))
            {
                XmlDocument relsDoc = new();
                relsDoc.Load(relsPath);
                XmlNodeList relationshipNodes = relsDoc.GetElementsByTagName("Relationship");
                foreach (XmlNode relationshipNode in relationshipNodes)
                {
                    string id = relationshipNode.Attributes["Id"].Value;
                    string target = relationshipNode.Attributes["Target"].Value;
                    sheetIdToTarget[id] = target;
                }
            }

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "sheet")
                {
                    string sheetName = reader.GetAttribute("name");
                    string sheetId = reader.GetAttribute("r:id");
                    if (sheetIdToTarget.TryGetValue(sheetId, out string target))
                    {
                        string sheetPath = ResolveRelationshipTarget(Path.GetDirectoryName(workbookPath), extractPath, target);
                        if (sheetName[0] != '#') // #은 주석
                            sheets.Add((sheetName, sheetPath));
                    }
                }
            }
            return sheets;
        }

        // OPC 관계의 Target을 실제 경로로 변환한다.
        // Target이 '/'로 시작하면 패키지 루트(extractPath) 기준 절대 경로, 아니면 관계 파일이 속한 파트(xl 폴더) 기준 상대 경로다.
        string ResolveRelationshipTarget(string basePartDirectory, string extractPath, string rawTarget)
        {
            bool isPackageAbsolute = rawTarget.StartsWith("/", StringComparison.Ordinal);
            string normalizedTarget = rawTarget.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return isPackageAbsolute
                ? Path.Combine(extractPath, normalizedTarget)
                : Path.Combine(basePartDirectory, normalizedTarget);
        }

        // 열 인덱스 변환
        int ConvertColumnToIndex(string cell)
        {
            int columnIndex = 0;
            int i = 0;

            while (i < cell.Length && !char.IsDigit(cell[i]))
            {
                columnIndex = columnIndex * 26 + (cell[i] - 'A' + 1);
                i++;
            }

            return columnIndex;
        }
        #endregion
    }
}
