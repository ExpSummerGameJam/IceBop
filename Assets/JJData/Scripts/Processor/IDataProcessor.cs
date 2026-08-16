using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IDataProcessor
{
    Task ProcessData(string xlsxFilePath, string dataClassFilePath, string dataFilePath, string resourcesFilePath, IClassGenerator classGenerator, ISheetDataGenerator dataGenerator, IParserGenerator parserGenerator, int maxSheetsAtOnce = 2);

}