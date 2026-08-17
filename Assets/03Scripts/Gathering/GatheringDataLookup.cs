using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
#endif

public static class GatheringDataLookup
{
    private const BindingFlags StaticFieldFlags = BindingFlags.Public | BindingFlags.Static;
    private const BindingFlags InstanceFieldFlags = BindingFlags.Public | BindingFlags.Instance;

    private static Dictionary<int, GatherableDefinition> gatherablesById;
    private static Dictionary<int, IngredientDefinition> ingredientsById;
    private static Dictionary<int, DishDefinition> dishesById;
    private static Dictionary<string, IngredientCategoryDefinition> ingredientCategoriesByKey;

    public static bool TryGetGatherable(int gatherableId, out GatherableDefinition definition)
    {
        EnsureGatherables();
        return gatherablesById.TryGetValue(gatherableId, out definition);
    }

    public static bool TryGetIngredient(int ingredientId, out IngredientDefinition definition)
    {
        EnsureIngredients();
        return ingredientsById.TryGetValue(ingredientId, out definition);
    }

    public static IngredientDefinition GetIngredientOrFallback(int ingredientId)
    {
        if (TryGetIngredient(ingredientId, out IngredientDefinition definition))
            return definition;

        if (ingredientId <= 0)
            return null;

        return new IngredientDefinition
        {
            ingredientId = ingredientId,
            ingredientKey = $"ingredient_{ingredientId}",
            displayName = $"Ingredient {ingredientId}",
            preparationKind = string.Empty,
            iconKey = $"ingredient_{ingredientId}",
            worldDropForce = 1.75f
        };
    }

    public static bool TryGetDish(int dishId, out DishDefinition definition)
    {
        EnsureDishes();
        return dishesById.TryGetValue(dishId, out definition);
    }

    public static bool TryGetIngredientCategory(string categoryKey, out IngredientCategoryDefinition definition)
    {
        EnsureIngredientCategories();

        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            definition = null;
            return false;
        }

        return ingredientCategoriesByKey.TryGetValue(categoryKey.Trim(), out definition);
    }

    public static IReadOnlyCollection<GatherableDefinition> GetGatherables()
    {
        EnsureGatherables();
        return gatherablesById.Values;
    }

    public static IReadOnlyCollection<DishDefinition> GetDishes()
    {
        EnsureDishes();
        return dishesById.Values;
    }

    public static bool TryFindDishForIngredients(IReadOnlyList<int> ingredientIds, out DishDefinition definition)
    {
        EnsureDishes();

        definition = dishesById.Values.FirstOrDefault(dish => dish != null && dish.MatchesIngredients(ingredientIds, false));
        if (definition != null)
            return true;

        definition = dishesById.Values.FirstOrDefault(dish => dish != null && dish.MatchesIngredients(ingredientIds, true));
        return definition != null;
    }

    public static void ClearCache()
    {
        gatherablesById = null;
        ingredientsById = null;
        dishesById = null;
        ingredientCategoriesByKey = null;
    }

    private static void EnsureGatherables()
    {
        if (gatherablesById != null)
            return;

        gatherablesById = new Dictionary<int, GatherableDefinition>();

        if (TryLoadGeneratedGatherables(gatherablesById))
            return;

#if UNITY_EDITOR
        LoadEditorXlsxGatherables(gatherablesById);
#endif
    }

    private static void EnsureIngredients()
    {
        if (ingredientsById != null)
            return;

        ingredientsById = new Dictionary<int, IngredientDefinition>();

        if (TryLoadGeneratedIngredients(ingredientsById))
            return;

#if UNITY_EDITOR
        LoadEditorXlsxIngredients(ingredientsById);
#endif
    }

    private static void EnsureDishes()
    {
        if (dishesById != null)
            return;

        dishesById = new Dictionary<int, DishDefinition>();

        if (TryLoadGeneratedDishes(dishesById))
            return;

#if UNITY_EDITOR
        LoadEditorXlsxDishes(dishesById);
#endif
    }

    private static void EnsureIngredientCategories()
    {
        if (ingredientCategoriesByKey != null)
            return;

        ingredientCategoriesByKey = new Dictionary<string, IngredientCategoryDefinition>();

        if (TryLoadGeneratedIngredientCategories(ingredientCategoriesByKey))
            return;

#if UNITY_EDITOR
        LoadEditorXlsxIngredientCategories(ingredientCategoriesByKey);
#endif
    }

    private static bool TryLoadGeneratedGatherables(Dictionary<int, GatherableDefinition> target)
    {
        Type dataType = FindType("GatheringData");
        FieldInfo sheetField = dataType?.GetField("Gatherables", StaticFieldFlags);
        if (sheetField == null)
            return false;

        IEnumerable rows = GetGeneratedRows(sheetField, "GatheringDataDataParser", "SetXlsxData");
        if (rows == null)
            return false;

        foreach (object row in rows)
        {
            if (row == null)
                continue;

            GatherableDefinition definition = new()
            {
                gatherableId = GetInt(row, "gatherableId"),
                ingredientId = GetInt(row, "ingredientId"),
                displayName = GetString(row, "displayName"),
                availableSpriteResourcePath = GetString(row, "availableSpriteResourcePath"),
                depletedSpriteResourcePath = GetString(row, "depletedSpriteResourcePath"),
                dropCount = GetInt(row, "dropCount"),
                canGatherOnce = GetBool(row, "canGatherOnce"),
                dropMode = GetString(row, "dropMode"),
                configuredDropIngredientIdA = GetInt(row, "configuredDropIngredientIdA"),
                configuredDropIngredientIdB = GetInt(row, "configuredDropIngredientIdB"),
                memo = GetString(row, "memo")
            };

            if (definition.gatherableId > 0)
                target[definition.gatherableId] = definition;
        }

        return target.Count > 0;
    }

    private static bool TryLoadGeneratedIngredients(Dictionary<int, IngredientDefinition> target)
    {
        Type dataType = FindType("CookingData");
        FieldInfo sheetField = dataType?.GetField("Ingredients", StaticFieldFlags);
        if (sheetField == null)
            return false;

        IEnumerable rows = GetGeneratedRows(sheetField, "CookingDataDataParser", "SetXlsxData");
        if (rows == null)
            return false;

        foreach (object row in rows)
        {
            if (row == null)
                continue;

            IngredientDefinition definition = new()
            {
                ingredientId = GetInt(row, "ingredientId"),
                ingredientKey = GetString(row, "ingredientKey"),
                displayName = GetString(row, "displayName"),
                categoryKey = GetString(row, "categoryKey"),
                preparationKind = GetString(row, "preparationKind"),
                iconKey = GetString(row, "iconKey"),
                worldDropForce = GetFloat(row, "worldDropForce", 1.75f),
                description = GetString(row, "description")
            };

            if (definition.ingredientId > 0)
                target[definition.ingredientId] = definition;
        }

        return target.Count > 0;
    }

    private static bool TryLoadGeneratedIngredientCategories(Dictionary<string, IngredientCategoryDefinition> target)
    {
        Type dataType = FindType("CookingData");
        FieldInfo sheetField = dataType?.GetField("IngredientCategories", StaticFieldFlags);
        if (sheetField == null)
            return false;

        IEnumerable rows = GetGeneratedRows(sheetField, "CookingDataDataParser", "SetXlsxData");
        if (rows == null)
            return false;

        foreach (object row in rows)
        {
            if (row == null)
                continue;

            IngredientCategoryDefinition definition = new()
            {
                categoryKey = GetString(row, "categoryKey"),
                displayName = GetString(row, "displayName"),
                sortOrder = GetInt(row, "sortOrder"),
                description = GetString(row, "description")
            };

            if (!string.IsNullOrWhiteSpace(definition.categoryKey))
                target[definition.categoryKey.Trim()] = definition;
        }

        return target.Count > 0;
    }

    private static bool TryLoadGeneratedDishes(Dictionary<int, DishDefinition> target)
    {
        Type dataType = FindType("CookingData");
        FieldInfo sheetField = dataType?.GetField("Dishes", StaticFieldFlags);
        if (sheetField == null)
            return false;

        IEnumerable rows = GetGeneratedRows(sheetField, "CookingDataDataParser", "SetXlsxData");
        if (rows == null)
            return false;

        foreach (object row in rows)
        {
            if (row == null)
                continue;

            DishDefinition definition = new()
            {
                dishId = GetInt(row, "dishId"),
                dishKey = GetString(row, "dishKey"),
                displayName = GetString(row, "displayName"),
                dishSpriteResourcePath = GetString(row, "dishSpriteResourcePath"),
                requiredIngredientIds = GetString(row, "requiredIngredientIds"),
                cookingMethod = GetFirstString(row, "cookingMethod", "cookingMethodKey", "method"),
                finishingStageKind = GetString(row, "finishingStageKind"),
                linkedAbilityId = GetFirstInt(row, "linkedAbilityId", "abilityId"),
                linkedAbilityKey = GetFirstString(row, "linkedAbilityKey", "abilityKey", "ability"),
                abilityEffect = GetString(row, "abilityEffect"),
                memo = GetString(row, "memo")
            };

            if (definition.dishId > 0)
                target[definition.dishId] = definition;
        }

        return target.Count > 0;
    }

    private static IEnumerable GetGeneratedRows(FieldInfo sheetField, string parserTypeName, string parserMethodName)
    {
        IEnumerable rows = sheetField.GetValue(null) as IEnumerable;
        if (rows != null)
            return rows;

        Type parserType = FindType(parserTypeName);
        MethodInfo parserMethod = parserType?.GetMethod(parserMethodName, StaticFieldFlags);
        if (parserMethod == null)
            return null;

        try
        {
            parserMethod.Invoke(null, null);
            return sheetField.GetValue(null) as IEnumerable;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"JJData parse cache is not ready. type = {parserTypeName}, error = {ex.GetBaseException().Message}");
            return null;
        }
    }

    private static Type FindType(string typeName)
    {
        Type directType = Type.GetType(typeName);
        if (directType != null)
            return directType;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }

    private static int GetInt(object source, string fieldName)
    {
        object value = GetFieldValue(source, fieldName);
        if (value is int intValue)
            return intValue;

        return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : 0;
    }

    private static bool GetBool(object source, string fieldName)
    {
        object value = GetFieldValue(source, fieldName);
        if (value is bool boolValue)
            return boolValue;

        string text = Convert.ToString(value, CultureInfo.InvariantCulture);
        return string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || text == "1";
    }

    private static string GetString(object source, string fieldName)
    {
        object value = GetFieldValue(source, fieldName);
        return value?.ToString() ?? string.Empty;
    }

    private static float GetFloat(object source, string fieldName, float fallback)
    {
        object value = GetFieldValue(source, fieldName);
        if (value is float floatValue)
            return floatValue;

        if (value is double doubleValue)
            return (float)doubleValue;

        return float.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
            ? result
            : fallback;
    }

    private static int GetFirstInt(object source, params string[] fieldNames)
    {
        foreach (string fieldName in fieldNames)
        {
            object value = GetFieldValue(source, fieldName);
            if (value == null)
                continue;

            if (value is int intValue)
                return intValue;

            if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;
        }

        return 0;
    }

    private static string GetFirstString(object source, params string[] fieldNames)
    {
        foreach (string fieldName in fieldNames)
        {
            string value = GetString(source, fieldName);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    private static object GetFieldValue(object source, string fieldName)
    {
        return source.GetType().GetField(fieldName, InstanceFieldFlags)?.GetValue(source);
    }

#if UNITY_EDITOR
    private static void LoadEditorXlsxGatherables(Dictionary<int, GatherableDefinition> target)
    {
        foreach (Dictionary<string, string> row in ReadXlsxRows("JJData/Xlsx/GatheringData.xlsx", "Gatherables"))
        {
            GatherableDefinition definition = new()
            {
                gatherableId = GetRowInt(row, "gatherableId"),
                ingredientId = GetRowInt(row, "ingredientId"),
                displayName = GetRowString(row, "displayName"),
                availableSpriteResourcePath = GetRowString(row, "availableSpriteResourcePath"),
                depletedSpriteResourcePath = GetRowString(row, "depletedSpriteResourcePath"),
                dropCount = GetRowInt(row, "dropCount"),
                canGatherOnce = GetRowBool(row, "canGatherOnce"),
                dropMode = GetRowString(row, "dropMode"),
                configuredDropIngredientIdA = GetRowInt(row, "configuredDropIngredientIdA"),
                configuredDropIngredientIdB = GetRowInt(row, "configuredDropIngredientIdB"),
                memo = GetRowString(row, "memo")
            };

            if (definition.gatherableId > 0)
                target[definition.gatherableId] = definition;
        }
    }

    private static void LoadEditorXlsxIngredients(Dictionary<int, IngredientDefinition> target)
    {
        foreach (Dictionary<string, string> row in ReadXlsxRows("JJData/Xlsx/CookingData.xlsx", "Ingredients"))
        {
            IngredientDefinition definition = new()
            {
                ingredientId = GetRowInt(row, "ingredientId"),
                ingredientKey = GetRowString(row, "ingredientKey"),
                displayName = GetRowString(row, "displayName"),
                categoryKey = GetRowString(row, "categoryKey"),
                preparationKind = GetRowString(row, "preparationKind"),
                iconKey = GetRowString(row, "iconKey"),
                worldDropForce = GetRowFloat(row, "worldDropForce", 1.75f),
                description = GetRowString(row, "description")
            };

            if (definition.ingredientId > 0)
                target[definition.ingredientId] = definition;
        }
    }

    private static void LoadEditorXlsxIngredientCategories(Dictionary<string, IngredientCategoryDefinition> target)
    {
        foreach (Dictionary<string, string> row in ReadXlsxRows("JJData/Xlsx/CookingData.xlsx", "IngredientCategories"))
        {
            IngredientCategoryDefinition definition = new()
            {
                categoryKey = GetRowString(row, "categoryKey"),
                displayName = GetRowString(row, "displayName"),
                sortOrder = GetRowInt(row, "sortOrder"),
                description = GetRowString(row, "description")
            };

            if (!string.IsNullOrWhiteSpace(definition.categoryKey))
                target[definition.categoryKey.Trim()] = definition;
        }
    }

    private static void LoadEditorXlsxDishes(Dictionary<int, DishDefinition> target)
    {
        foreach (Dictionary<string, string> row in ReadXlsxRows("JJData/Xlsx/CookingData.xlsx", "Dishes"))
        {
            DishDefinition definition = new()
            {
                dishId = GetRowInt(row, "dishId"),
                dishKey = GetRowString(row, "dishKey"),
                displayName = GetRowString(row, "displayName"),
                dishSpriteResourcePath = GetRowString(row, "dishSpriteResourcePath"),
                requiredIngredientIds = GetRowString(row, "requiredIngredientIds"),
                cookingMethod = GetFirstRowString(row, "cookingMethod", "cookingMethodKey", "method"),
                finishingStageKind = GetRowString(row, "finishingStageKind"),
                linkedAbilityId = GetFirstRowInt(row, "linkedAbilityId", "abilityId"),
                linkedAbilityKey = GetFirstRowString(row, "linkedAbilityKey", "abilityKey", "ability"),
                abilityEffect = GetRowString(row, "abilityEffect"),
                memo = GetRowString(row, "memo")
            };

            if (definition.dishId > 0)
                target[definition.dishId] = definition;
        }
    }

    private static IEnumerable<Dictionary<string, string>> ReadXlsxRows(string assetRelativePath, string sheetName)
    {
        string filePath = Path.Combine(Application.dataPath, assetRelativePath);
        if (!File.Exists(filePath))
            yield break;

        ZipArchive archive;
        try
        {
            archive = ZipFile.OpenRead(filePath);
        }
        catch (IOException ex)
        {
            Debug.LogWarning($"Could not read xlsx data. path = {filePath}, error = {ex.Message}");
            yield break;
        }

        using (archive)
        {
        List<string> sharedStrings = ReadSharedStrings(archive);
        string worksheetPath = GetWorksheetPath(archive, sheetName);
        if (string.IsNullOrEmpty(worksheetPath))
            yield break;

        ZipArchiveEntry worksheetEntry = archive.GetEntry(worksheetPath);
        if (worksheetEntry == null)
            yield break;

        XNamespace spreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        Dictionary<int, Dictionary<int, string>> rawRows = new();

        using Stream worksheetStream = worksheetEntry.Open();
        XDocument sheetDocument = XDocument.Load(worksheetStream);
        foreach (XElement rowElement in sheetDocument.Descendants(spreadsheetNs + "row"))
        {
            int rowIndex = GetAttributeInt(rowElement, "r", rawRows.Count + 1);
            Dictionary<int, string> cells = new();

            foreach (XElement cellElement in rowElement.Elements(spreadsheetNs + "c"))
            {
                string cellReference = cellElement.Attribute("r")?.Value;
                int columnIndex = ConvertColumnToIndex(cellReference);
                if (columnIndex <= 0)
                    continue;

                cells[columnIndex] = ReadCellValue(cellElement, spreadsheetNs, sharedStrings);
            }

            rawRows[rowIndex] = cells;
        }

        if (!rawRows.TryGetValue(3, out Dictionary<int, string> fieldNames))
            yield break;

        foreach (KeyValuePair<int, Dictionary<int, string>> rawRow in rawRows.OrderBy(pair => pair.Key))
        {
            if (rawRow.Key < 4)
                continue;

            Dictionary<string, string> row = new();
            bool hasValue = false;

            foreach (KeyValuePair<int, string> fieldName in fieldNames)
            {
                if (string.IsNullOrWhiteSpace(fieldName.Value))
                    continue;

                rawRow.Value.TryGetValue(fieldName.Key, out string value);
                value ??= string.Empty;
                row[fieldName.Value] = value;
                hasValue |= !string.IsNullOrWhiteSpace(value);
            }

            if (hasValue)
                yield return row;
        }
        }
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        List<string> sharedStrings = new();
        ZipArchiveEntry sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedStringsEntry == null)
            return sharedStrings;

        XNamespace spreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        using Stream stream = sharedStringsEntry.Open();
        XDocument document = XDocument.Load(stream);

        foreach (XElement item in document.Descendants(spreadsheetNs + "si"))
        {
            string text = string.Concat(item.Descendants(spreadsheetNs + "t").Select(element => element.Value));
            sharedStrings.Add(text);
        }

        return sharedStrings;
    }

    private static string GetWorksheetPath(ZipArchive archive, string sheetName)
    {
        XNamespace spreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace documentRelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        ZipArchiveEntry workbookEntry = archive.GetEntry("xl/workbook.xml");
        ZipArchiveEntry relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (workbookEntry == null || relationshipsEntry == null)
            return string.Empty;

        string relationshipId = string.Empty;
        using (Stream workbookStream = workbookEntry.Open())
        {
            XDocument workbookDocument = XDocument.Load(workbookStream);
            XElement sheetElement = workbookDocument
                .Descendants(spreadsheetNs + "sheet")
                .FirstOrDefault(element => string.Equals(element.Attribute("name")?.Value, sheetName, StringComparison.Ordinal));

            relationshipId = sheetElement?.Attribute(documentRelationshipNs + "id")?.Value;
        }

        if (string.IsNullOrEmpty(relationshipId))
            return string.Empty;

        using Stream relationshipsStream = relationshipsEntry.Open();
        XDocument relationshipsDocument = XDocument.Load(relationshipsStream);
        XElement relationshipElement = relationshipsDocument
            .Descendants(packageRelationshipNs + "Relationship")
            .FirstOrDefault(element => string.Equals(element.Attribute("Id")?.Value, relationshipId, StringComparison.Ordinal));

        string target = relationshipElement?.Attribute("Target")?.Value?.Replace('\\', '/');
        if (string.IsNullOrEmpty(target))
            return string.Empty;

        return target.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/')
            : $"xl/{target}";
    }

    private static string ReadCellValue(XElement cellElement, XNamespace spreadsheetNs, IReadOnlyList<string> sharedStrings)
    {
        string cellType = cellElement.Attribute("t")?.Value;
        if (cellType == "s")
        {
            string indexText = cellElement.Element(spreadsheetNs + "v")?.Value;
            return int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sharedStringIndex)
                   && sharedStringIndex >= 0
                   && sharedStringIndex < sharedStrings.Count
                ? sharedStrings[sharedStringIndex]
                : string.Empty;
        }

        if (cellType == "inlineStr")
            return string.Concat(cellElement.Descendants(spreadsheetNs + "t").Select(element => element.Value));

        return cellElement.Element(spreadsheetNs + "v")?.Value ?? string.Empty;
    }

    private static int ConvertColumnToIndex(string cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
            return 0;

        int columnIndex = 0;
        foreach (char character in cellReference)
        {
            if (char.IsDigit(character))
                break;

            columnIndex = columnIndex * 26 + (char.ToUpperInvariant(character) - 'A' + 1);
        }

        return columnIndex;
    }

    private static int GetAttributeInt(XElement element, string attributeName, int fallback)
    {
        string value = element.Attribute(attributeName)?.Value;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : fallback;
    }

    private static int GetRowInt(IReadOnlyDictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out string value)
               && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : 0;
    }

    private static bool GetRowBool(IReadOnlyDictionary<string, string> row, string key)
    {
        if (!row.TryGetValue(key, out string value))
            return false;

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    private static string GetRowString(IReadOnlyDictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out string value) ? value : string.Empty;
    }

    private static float GetRowFloat(IReadOnlyDictionary<string, string> row, string key, float fallback)
    {
        return row.TryGetValue(key, out string value)
               && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
            ? result
            : fallback;
    }

    private static int GetFirstRowInt(IReadOnlyDictionary<string, string> row, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (row.TryGetValue(key, out string value) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;
        }

        return 0;
    }

    private static string GetFirstRowString(IReadOnlyDictionary<string, string> row, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (row.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }
#endif
}
