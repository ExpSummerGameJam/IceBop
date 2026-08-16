using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;




public static class DialogueDataDataParser
{
    readonly private static string _dataFilePath = "JJData/";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void SetXlsxData()
    {
        ConvertRowData2Dialogues();
    }
    public static void ConvertRowData2Dialogues()
    {
        int stringLength = 0;
        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, "DialogueData/Dialogues"));
        Span<byte> buffer = binAsset.bytes;
        int offset = 0;
        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));
        offset += 8;
        DialogueData.Dialogues = new DialogueData_Dialogues[rows];
        for(int row = 0; row < rows; row++)
        {
            DialogueData_Dialogues sheetRowData = new();
            sheetRowData.dialogueId = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.sceneKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.lineOrder = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.speaker = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.lineText = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            DialogueData.Dialogues[row] = sheetRowData;
        }
        Resources.UnloadAsset(binAsset);
    }

}
