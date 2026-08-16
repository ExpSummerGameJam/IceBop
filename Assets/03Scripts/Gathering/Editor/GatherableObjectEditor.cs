using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GatherableObject))]
public sealed class GatherableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GatherableObject gatherableObject = (GatherableObject)target;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("JJData Preview", EditorStyles.boldLabel);

        if (GatheringDataLookup.TryGetGatherable(gatherableObject.GatherableId, out GatherableDefinition definition))
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Gatherable ID", definition.gatherableId);
                EditorGUILayout.TextField("Name", definition.displayName);
                if (gatherableObject.DropMode == GatherableDropMode.DataIngredient)
                {
                    DrawIngredient("Drop Ingredient", definition.ingredientId);
                }
                else
                {
                    EditorGUILayout.EnumPopup("Drop Mode", gatherableObject.DropMode);
                    DrawIngredient("Drop Candidate A", gatherableObject.ConfiguredDropIngredientIdA);
                    DrawIngredient("Drop Candidate B", gatherableObject.ConfiguredDropIngredientIdB);
                }

                EditorGUILayout.IntField("Drop Count", definition.SafeDropCount);
                EditorGUILayout.Toggle("Can Gather Once", definition.canGatherOnce);
                EditorGUILayout.TextField("Available Sprite Path", definition.availableSpriteResourcePath);
                EditorGUILayout.TextField("Depleted Sprite Path", definition.depletedSpriteResourcePath);
                DrawSprite("Available Sprite", definition.availableSpriteResourcePath);
                DrawSprite("Depleted Sprite", definition.depletedSpriteResourcePath);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No matching gatherableId was found in GatheringData.xlsx.", MessageType.Warning);
        }

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload Xlsx Data"))
            {
                GatheringDataLookup.ClearCache();
                Repaint();
            }

            if (GUILayout.Button("Apply Sprites"))
            {
                Undo.RecordObject(gatherableObject, "Apply Gatherable Sprites");
                gatherableObject.ApplySpritesFromData();
                EditorUtility.SetDirty(gatherableObject);
            }
        }
    }

    private static void DrawIngredient(string label, int ingredientId)
    {
        if (GatheringDataLookup.TryGetIngredient(ingredientId, out IngredientDefinition ingredient))
        {
            EditorGUILayout.TextField(label, $"{ingredient.displayName} ({ingredient.ingredientKey}, {ingredient.ingredientId})");
            EditorGUILayout.TextField("Icon/Sprite Key", ingredient.iconKey);
            DrawIngredientSprite("Ingredient Sprite", ingredient);
        }
        else
        {
            EditorGUILayout.IntField($"{label} ID", ingredientId);
        }
    }

    private static void DrawSprite(string label, string resourcePath)
    {
        Sprite sprite = string.IsNullOrWhiteSpace(resourcePath) ? null : Resources.Load<Sprite>(resourcePath);
        EditorGUILayout.ObjectField(label, sprite, typeof(Sprite), false);
    }

    private static void DrawIngredientSprite(string label, IngredientDefinition ingredient)
    {
        Sprite sprite = InventoryVisualUtility.GetIngredientSprite(ingredient);
        EditorGUILayout.ObjectField(label, sprite, typeof(Sprite), false);
    }
}
