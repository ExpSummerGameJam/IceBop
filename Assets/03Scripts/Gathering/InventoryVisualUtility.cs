using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class InventoryVisualUtility
{
    private const string IngredientAtlasResourcePath = "Atlas/IngredientAtlas";
    private const string GatheringAtlasResourcePath = "Atlas/Gatering";
#if UNITY_EDITOR
    private const string IngredientAtlasAssetPath = "Assets/01Resources/Atlas/IngredientAtlas.spriteatlasv2";
    private const string GatheringAtlasAssetPath = "Assets/01Resources/Atlas/Gatering.spriteatlasv2";
#endif

    private static Sprite whiteSprite;
    private static SpriteAtlas ingredientAtlas;
    private static SpriteAtlas gatheringAtlas;
    private static readonly Dictionary<string, Sprite> ingredientSpritesByKey = new();
    private static readonly Dictionary<string, Sprite> gatheringSpritesByKey = new();

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite != null)
                return whiteSprite;

            Texture2D texture = new(16, 16, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply();

            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
            whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return whiteSprite;
        }
    }

    public static Sprite GetIngredientSprite(IngredientDefinition definition)
    {
        if (definition == null)
            return null;

        Sprite sprite = GetIngredientSpriteByKey(definition.iconKey);
        if (sprite != null)
            return sprite;

        sprite = GetIngredientSpriteByKey(definition.ingredientKey);
        if (sprite != null)
            return sprite;

        if (!string.IsNullOrWhiteSpace(definition.ingredientKey) &&
            !definition.ingredientKey.Trim().StartsWith("ingredient_", System.StringComparison.OrdinalIgnoreCase))
            return GetIngredientSpriteByKey($"ingredient_{definition.ingredientKey}");

        return null;
    }

    public static Sprite GetGatherableSprite(string resourcePathOrKey)
    {
        return GetAtlasSprite(resourcePathOrKey, gatheringSpritesByKey, GetGatheringAtlas);
    }

    public static Color ColorFromId(int id)
    {
        float hue = Mathf.Repeat(id * 0.173f, 1f);
        return Color.HSVToRGB(hue, 0.55f, 0.95f);
    }

    private static Sprite GetIngredientSpriteByKey(string rawKey)
    {
        return GetAtlasSprite(rawKey, ingredientSpritesByKey, GetIngredientAtlas);
    }

    private static Sprite GetAtlasSprite(string rawKey, Dictionary<string, Sprite> cache, System.Func<SpriteAtlas> resolveAtlas)
    {
        string key = NormalizeAtlasKey(rawKey);
        if (string.IsNullOrEmpty(key))
            return null;

        if (cache.TryGetValue(key, out Sprite cachedSprite))
            return cachedSprite;

        SpriteAtlas atlas = resolveAtlas();
        if (atlas == null)
            return null;

        Sprite sprite = atlas.GetSprite(key);
        if (sprite == null)
            return null;

        cache[key] = sprite;
        return sprite;
    }

    private static SpriteAtlas GetIngredientAtlas()
    {
        if (ingredientAtlas != null)
            return ingredientAtlas;

        ingredientAtlas = Resources.Load<SpriteAtlas>(IngredientAtlasResourcePath);

#if UNITY_EDITOR
        if (ingredientAtlas == null)
            ingredientAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(IngredientAtlasAssetPath);
#endif

        return ingredientAtlas;
    }

    private static SpriteAtlas GetGatheringAtlas()
    {
        if (gatheringAtlas != null)
            return gatheringAtlas;

        gatheringAtlas = Resources.Load<SpriteAtlas>(GatheringAtlasResourcePath);

#if UNITY_EDITOR
        if (gatheringAtlas == null)
            gatheringAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(GatheringAtlasAssetPath);
#endif

        return gatheringAtlas;
    }

    private static string NormalizeAtlasKey(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return string.Empty;

        string key = rawKey.Trim().Replace('\\', '/');

        int slashIndex = key.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < key.Length - 1)
            key = key.Substring(slashIndex + 1);

        int dotIndex = key.LastIndexOf('.');
        if (dotIndex > 0)
            key = key.Substring(0, dotIndex);

        return key;
    }
}
