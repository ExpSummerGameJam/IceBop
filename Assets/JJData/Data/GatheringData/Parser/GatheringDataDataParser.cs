using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;




public static class GatheringDataDataParser
{
    readonly private static string _dataFilePath = "JJData/";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void SetXlsxData()
    {
        ConvertRowData2Gatherables();
    }
    public static void ConvertRowData2Gatherables()
    {
        int stringLength = 0;
        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, "GatheringData/Gatherables"));
        Span<byte> buffer = binAsset.bytes;
        int offset = 0;
        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));
        offset += 8;
        GatheringData.Gatherables = new GatheringData_Gatherables[rows];
        for(int row = 0; row < rows; row++)
        {
            GatheringData_Gatherables sheetRowData = new();
            sheetRowData.gatherableId = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.ingredientId = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.displayName = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.availableSpriteResourcePath = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.depletedSpriteResourcePath = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.dropCount = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.canGatherOnce = TypeByte2TypeConverter.ConvertTypeByte2bool(buffer.Slice(offset, 1));
            offset += 1;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.dropMode = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.configuredDropIngredientIdA = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.configuredDropIngredientIdB = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.memo = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            GatheringData.Gatherables[row] = sheetRowData;
        }
        Resources.UnloadAsset(binAsset);
    }

}
