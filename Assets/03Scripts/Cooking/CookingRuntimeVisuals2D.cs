using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CookingRuntimeVisuals2D
{
    private const string HandkerchiefResourcePath = "Atlas/Pot/handkerchief";
    private const string CutboardResourcePath = "Atlas/Pot/cutboard";

#if UNITY_EDITOR
    private const string HandkerchiefAssetPath = "Assets/01Resources/Atlas/Pot/handkerchief.png";
    private const string CutboardAssetPath = "Assets/01Resources/Atlas/Pot/cutboard.png";
#endif

    private static Sprite knifeSprite;
    private static Sprite clothSprite;
    private static Sprite cutboardSprite;
    private static Sprite grinderSprite;
    private static Sprite ladleSprite;

    public static Sprite GetIngredientSprite(CookingIngredientEntry entry)
    {
        if (entry != null && entry.ingredientSprite != null)
            return entry.ingredientSprite;

        return GetIngredientSprite(entry != null ? entry.ingredient : null);
    }

    public static Sprite GetIngredientSprite(IngredientDefinition definition)
    {
        Sprite loaded = InventoryVisualUtility.GetIngredientSprite(definition);
        if (loaded != null)
            return loaded;

        return InventoryVisualUtility.WhiteSprite;
    }

    public static Color GetIngredientColor(CookingIngredientEntry entry)
    {
        if (entry != null && entry.ingredientColor.a > 0f)
            return entry.ingredientColor;

        return GetIngredientColor(entry != null ? entry.ingredient : null);
    }

    public static Color GetIngredientColor(IngredientDefinition definition)
    {
        int id = definition != null ? definition.ingredientId : 0;
        return InventoryVisualUtility.ColorFromId(id);
    }

    public static Sprite GetKnifeSprite()
    {
        if (knifeSprite == null)
            knifeSprite = CreateKnifeSprite();

        return knifeSprite;
    }

    public static Sprite GetClothSprite()
    {
        if (clothSprite == null)
            clothSprite = LoadSprite(HandkerchiefResourcePath,
#if UNITY_EDITOR
                HandkerchiefAssetPath,
#else
                null,
#endif
                "handkerchief") ?? CreateClothSprite();

        return clothSprite;
    }

    public static Sprite GetCutboardSprite()
    {
        if (cutboardSprite == null)
        {
            cutboardSprite = LoadSprite(CutboardResourcePath,
#if UNITY_EDITOR
                CutboardAssetPath,
#else
                null,
#endif
                "cutboard");
        }

        return cutboardSprite;
    }

    public static Sprite GetGrinderSprite()
    {
        if (grinderSprite == null)
            grinderSprite = CreateGrinderSprite();

        return grinderSprite;
    }

    public static Sprite GetLadleSprite()
    {
        if (ladleSprite == null)
            ladleSprite = CreateLadleSprite();

        return ladleSprite;
    }

    public static Sprite GetDishSprite(DishDefinition dish, IReadOnlyList<CookingIngredientEntry> ingredients)
    {
        if (dish != null && !string.IsNullOrWhiteSpace(dish.dishSpriteResourcePath))
        {
            Sprite loaded = Resources.Load<Sprite>(dish.dishSpriteResourcePath);
            if (loaded != null)
                return loaded;
        }

        Color color = new(0.95f, 0.78f, 0.36f, 1f);
        if (ingredients != null && ingredients.Count > 0)
        {
            color = Color.black;
            foreach (CookingIngredientEntry ingredient in ingredients)
                color += GetIngredientColor(ingredient);

            color /= ingredients.Count;
            color.a = 1f;
            color = Color.Lerp(color, Color.white, 0.2f);
        }

        return CreateDishSprite("RuntimeDish", color);
    }

    public static GameObject CreateBoard(Vector3 position, Vector2 size, int sortingOrder)
    {
        GameObject board = new("PreparationBoard");
        board.transform.position = position;

        SpriteRenderer renderer = board.AddComponent<SpriteRenderer>();
        Sprite cutboard = GetCutboardSprite();
        if (cutboard != null)
        {
            renderer.sprite = cutboard;
            renderer.color = Color.white;
            board.transform.localScale = Vector3.one;
        }
        else
        {
            renderer.sprite = InventoryVisualUtility.WhiteSprite;
            renderer.color = new Color(0.66f, 0.46f, 0.25f, 1f);
            board.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        renderer.sortingOrder = sortingOrder;

        return board;
    }

    public static SpriteRenderer CreateBlock(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        int sortingOrder)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localScale = localScale;

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = InventoryVisualUtility.WhiteSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static Sprite CreateIngredientSprite(string spriteName, Color bottomColor, Color topColor, Color stripeColor)
    {
        const int width = 256;
        const int height = 128;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color clear = new(0f, 0f, 0f, 0f);
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float ellipse = Mathf.Pow((u - 0.5f) / 0.48f, 2f) + Mathf.Pow((v - 0.5f) / 0.39f, 2f);
                if (ellipse > 1f)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                float stripe = Mathf.Sin((u * 8.5f + v * 1.5f) * Mathf.PI) * 0.5f + 0.5f;
                Color baseColor = Color.Lerp(bottomColor, topColor, v);
                texture.SetPixel(x, y, Color.Lerp(baseColor, stripeColor, stripe * 0.22f));
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        sprite.name = spriteName;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite CreateKnifeSprite()
    {
        const int width = 160;
        const int height = 48;
        Texture2D texture = CreateTexture(width, height);
        Color clear = new(0f, 0f, 0f, 0f);
        Color handle = new(0.22f, 0.12f, 0.08f, 1f);
        Color blade = new(0.82f, 0.86f, 0.9f, 1f);
        Color edge = new(0.96f, 0.98f, 1f, 1f);

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                Color color = clear;

                bool inHandle = u <= 0.38f && v >= 0.28f && v <= 0.72f;
                float bladeT = Mathf.Clamp01((u - 0.30f) / 0.70f);
                float bladeTop = Mathf.Lerp(0.74f, 0.52f, bladeT);
                float bladeBottom = Mathf.Lerp(0.26f, 0.48f, bladeT);
                bool inBlade = u >= 0.30f && v >= bladeBottom && v <= bladeTop;

                if (inBlade)
                    color = v < Mathf.Lerp(bladeBottom, bladeTop, 0.34f) ? edge : blade;

                if (inHandle)
                    color = handle;

                texture.SetPixel(x, y, color);
            }
        }

        return CreateSprite(texture, "RuntimeKnife", 64f);
    }

    private static Sprite CreateClothSprite()
    {
        const int width = 96;
        const int height = 72;
        Texture2D texture = CreateTexture(width, height);
        Color clear = new(0f, 0f, 0f, 0f);

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                bool inside = u > 0.08f && u < 0.92f && v > 0.10f && v < 0.90f;
                if (!inside)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                float checker = ((x / 12 + y / 12) % 2) == 0 ? 0.08f : 0f;
                texture.SetPixel(x, y, Color.Lerp(new Color(0.76f, 0.92f, 1f, 1f), Color.white, checker));
            }
        }

        return CreateSprite(texture, "RuntimeCloth", 64f);
    }

    private static Sprite LoadSprite(string resourcePath, string editorAssetPath, string spriteName)
    {
        Sprite[] resourceSprites = Resources.LoadAll<Sprite>(resourcePath);
        Sprite sprite = FindBestSprite(resourceSprites, spriteName);
        if (sprite != null)
            return sprite;

        sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
            return sprite;

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(editorAssetPath))
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(editorAssetPath);
            sprite = FindBestSprite(assets, spriteName);
            if (sprite != null)
                return sprite;
        }
#endif

        return null;
    }

    private static Sprite FindBestSprite(IEnumerable<UnityEngine.Object> assets, string spriteName)
    {
        if (assets == null)
            return null;

        Sprite firstSprite = null;
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is not Sprite sprite)
                continue;

            firstSprite ??= sprite;
            if (IsSpriteNameMatch(sprite.name, spriteName))
                return sprite;
        }

        return firstSprite;
    }

    private static bool IsSpriteNameMatch(string actualName, string expectedName)
    {
        if (string.IsNullOrWhiteSpace(actualName) || string.IsNullOrWhiteSpace(expectedName))
            return false;

        return string.Equals(actualName, expectedName, System.StringComparison.OrdinalIgnoreCase) ||
               actualName.StartsWith($"{expectedName}_", System.StringComparison.OrdinalIgnoreCase);
    }

    private static Sprite CreateGrinderSprite()
    {
        const int width = 88;
        const int height = 128;
        Texture2D texture = CreateTexture(width, height);
        Color clear = new(0f, 0f, 0f, 0f);
        Color body = new(0.72f, 0.76f, 0.78f, 1f);
        Color hole = new(0.28f, 0.31f, 0.32f, 1f);

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                bool inside = u > 0.18f && u < 0.82f && v > 0.08f && v < 0.92f;
                if (!inside)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                bool isHole = x % 18 > 5 && x % 18 < 11 && y % 18 > 6 && y % 18 < 12;
                texture.SetPixel(x, y, isHole ? hole : body);
            }
        }

        return CreateSprite(texture, "RuntimeGrinder", 64f);
    }

    private static Sprite CreateLadleSprite()
    {
        const int width = 96;
        const int height = 160;
        Texture2D texture = CreateTexture(width, height);
        Color clear = new(0f, 0f, 0f, 0f);
        Color metal = new(0.78f, 0.82f, 0.84f, 1f);
        Color shade = new(0.46f, 0.51f, 0.54f, 1f);

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                Color color = clear;

                bool inHandle = Mathf.Abs(u - 0.5f) < 0.08f && v > 0.28f && v < 0.98f;
                float bowl = Mathf.Pow((u - 0.5f) / 0.34f, 2f) + Mathf.Pow((v - 0.18f) / 0.16f, 2f);
                if (bowl <= 1f)
                    color = Color.Lerp(shade, metal, Mathf.Clamp01((v - 0.04f) / 0.24f));

                if (inHandle)
                    color = metal;

                texture.SetPixel(x, y, color);
            }
        }

        return CreateSprite(texture, "RuntimeLadle", 64f);
    }

    private static Sprite CreateDishSprite(string spriteName, Color foodColor)
    {
        const int width = 160;
        const int height = 120;
        Texture2D texture = CreateTexture(width, height);
        Color clear = new(0f, 0f, 0f, 0f);
        Color bowl = new(0.92f, 0.94f, 0.96f, 1f);
        Color rim = new(0.64f, 0.70f, 0.74f, 1f);

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float bowlShape = Mathf.Pow((u - 0.5f) / 0.47f, 2f) + Mathf.Pow((v - 0.42f) / 0.34f, 2f);
                float foodShape = Mathf.Pow((u - 0.5f) / 0.38f, 2f) + Mathf.Pow((v - 0.54f) / 0.18f, 2f);

                if (bowlShape > 1f)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                Color color = bowlShape > 0.74f ? rim : bowl;
                if (foodShape <= 1f)
                    color = Color.Lerp(foodColor, Color.white, Mathf.Sin(u * Mathf.PI) * 0.12f);

                texture.SetPixel(x, y, color);
            }
        }

        return CreateSprite(texture, spriteName, 64f);
    }

    private static Texture2D CreateTexture(int width, int height)
    {
        return new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private static Sprite CreateSprite(Texture2D texture, string spriteName, float pixelsPerUnit)
    {
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = spriteName;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
