using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;




public static class DataBaseNameDataParser
{
    readonly private static string _dataFilePath = "JJData/";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void SetXlsxData()
    {
        ConvertRowData2TestClass();
    }
    public static void ConvertRowData2TestClass()
    {
        int stringLength = 0;
        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, "DataBaseName/TestClass"));
        Span<byte> buffer = binAsset.bytes;
        int offset = 0;
        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));
        offset += 8;
        DataBaseName.TestClass = new DataBaseName_TestClass[rows];
        for(int row = 0; row < rows; row++)
        {
            DataBaseName_TestClass sheetRowData = new();
            sheetRowData.Test12 = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.Test23 = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.Test34 = TypeByte2TypeConverter.ConvertTypeByte2float(buffer.Slice(offset, 4));
            offset += 4;
            DataBaseName.TestClass[row] = sheetRowData;
        }
        Resources.UnloadAsset(binAsset);
    }

}
