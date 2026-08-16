using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;




public static class CookingDataDataParser
{
    readonly private static string _dataFilePath = "JJData/";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void SetXlsxData()
    {
        ConvertRowData2IngredientCategories();
        ConvertRowData2Ingredients();
        ConvertRowData2AbilityDefinitions();
        ConvertRowData2Dishes();
        ConvertRowData2Recipes();
        ConvertRowData2RecipeSteps();
    }
    public static void ConvertRowData2AbilityDefinitions()
    {
        int stringLength = 0;
        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, "CookingData/AbilityDefinitions"));
        Span<byte> buffer = binAsset.bytes;
        int offset = 0;
        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));
        offset += 8;
        CookingData.AbilityDefinitions = new CookingData_AbilityDefinitions[rows];
        for(int row = 0; row < rows; row++)
        {
            CookingData_AbilityDefinitions sheetRowData = new();
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.abilityKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.displayName = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.description = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            CookingData.AbilityDefinitions[row] = sheetRowData;
        }
        Resources.UnloadAsset(binAsset);
    }

    public static void ConvertRowData2Recipes()
    {
        int stringLength = 0;
        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, "CookingData/Recipes"));
        Span<byte> buffer = binAsset.bytes;
        int offset = 0;
        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));
        offset += 8;
        CookingData.Recipes = new CookingData_Recipes[rows];
        for(int row = 0; row < rows; row++)
        {
            CookingData_Recipes sheetRowData = new();
            sheetRowData.recipeId = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.recipeKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.combinationText = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.dishId = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.dishKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.dishName = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.requiredIngredientIds = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.requiredIngredientNames = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.abilityEffect = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.anchorTongueCharges = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.rapidFreezeCharges = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.accelJumpCharges = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.additionalDescription = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.recipeLocation = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.specialCookingMethod = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.isExactRecipe = TypeByte2TypeConverter.ConvertTypeByte2bool(buffer.Slice(offset, 1));
            offset += 1;
            CookingData.Recipes[row] = sheetRowData;
        }
        Resources.UnloadAsset(binAsset);
    }

    public static void ConvertRowData2Dishes()
    {
        int stringLength = 0;
        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, "CookingData/Dishes"));
        Span<byte> buffer = binAsset.bytes;
        int offset = 0;
        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));
        offset += 8;
        CookingData.Dishes = new CookingData_Dishes[rows];
        for(int row = 0; row < rows; row++)
        {
            CookingData_Dishes sheetRowData = new();
            sheetRowData.dishId = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.dishKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.displayName = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.dishSpriteResourcePath = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.requiredIngredientIds = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.memo = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.abilityEffect = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.anchorTongueCharges = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.rapidFreezeCharges = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.accelJumpCharges = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.recipeLocation = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.specialCookingMethod = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.finishingStageKind = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            CookingData.Dishes[row] = sheetRowData;
        }
        Resources.UnloadAsset(binAsset);
    }

    public static void ConvertRowData2IngredientCategories()
    {
        int stringLength = 0;
        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, "CookingData/IngredientCategories"));
        Span<byte> buffer = binAsset.bytes;
        int offset = 0;
        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));
        offset += 8;
        CookingData.IngredientCategories = new CookingData_IngredientCategories[rows];
        for(int row = 0; row < rows; row++)
        {
            CookingData_IngredientCategories sheetRowData = new();
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.categoryKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.displayName = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.sortOrder = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.description = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            CookingData.IngredientCategories[row] = sheetRowData;
        }
        Resources.UnloadAsset(binAsset);
    }

    public static void ConvertRowData2RecipeSteps()
    {
        int stringLength = 0;
        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, "CookingData/RecipeSteps"));
        Span<byte> buffer = binAsset.bytes;
        int offset = 0;
        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));
        offset += 8;
        CookingData.RecipeSteps = new CookingData_RecipeSteps[rows];
        for(int row = 0; row < rows; row++)
        {
            CookingData_RecipeSteps sheetRowData = new();
            sheetRowData.recipeId = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.stepOrder = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.actionName = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.targetIngredientIds = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.preparationOverride = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.description = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            CookingData.RecipeSteps[row] = sheetRowData;
        }
        Resources.UnloadAsset(binAsset);
    }

    public static void ConvertRowData2Ingredients()
    {
        int stringLength = 0;
        TextAsset binAsset = Resources.Load<TextAsset>(Path.Combine(_dataFilePath, "CookingData/Ingredients"));
        Span<byte> buffer = binAsset.bytes;
        int offset = 0;
        long rows = TypeByte2TypeConverter.ConvertTypeByte2long(buffer.Slice(offset, 8));
        offset += 8;
        CookingData.Ingredients = new CookingData_Ingredients[rows];
        for(int row = 0; row < rows; row++)
        {
            CookingData_Ingredients sheetRowData = new();
            sheetRowData.ingredientId = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.ingredientKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.displayName = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.categoryKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.grade = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.isDefaultUnlocked = TypeByte2TypeConverter.ConvertTypeByte2bool(buffer.Slice(offset, 1));
            offset += 1;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.iconKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.flavorTag = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            sheetRowData.worldDropForce = TypeByte2TypeConverter.ConvertTypeByte2float(buffer.Slice(offset, 4));
            offset += 4;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.memo = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.preparationKind = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.ingredientRole = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.abilityKey = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.abilityDisplayName = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.region = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            stringLength = TypeByte2TypeConverter.ConvertTypeByte2int(buffer.Slice(offset, 4));
            offset += 4;
            sheetRowData.description = TypeByte2TypeConverter.ConvertTypeByte2string(buffer.Slice(offset, stringLength));
            offset += stringLength;
            CookingData.Ingredients[row] = sheetRowData;
        }
        Resources.UnloadAsset(binAsset);
    }

}
