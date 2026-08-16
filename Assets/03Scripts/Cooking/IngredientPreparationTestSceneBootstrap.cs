using UnityEngine;

public sealed class IngredientPreparationTestSceneBootstrap : MonoBehaviour
{
    [SerializeField] private Vector2 cuttingStationPosition = new(-4.3f, 0.35f);
    [SerializeField] private Vector2 dustStationPosition = new(0f, 0.35f);
    [SerializeField] private Vector2 grindingStationPosition = new(4.3f, 0.35f);

    private void Awake()
    {
        Camera camera = EnsureCamera();
        CreateStationLabel("Cutting", cuttingStationPosition);
        CreateStationLabel("Dust Wipe", dustStationPosition);
        CreateStationLabel("Grinding", grindingStationPosition);

        CreateStationBoard("CuttingTest_Board", cuttingStationPosition);
        PreparationKnifeTool2D knife = CreateKnife(camera, cuttingStationPosition + new Vector2(-1.1f, -1.65f));
        CreateCuttableIngredient(knife, cuttingStationPosition + new Vector2(0f, 0.18f));

        CreateStationBoard("DustWipeTest_Board", dustStationPosition);
        PreparationPointerTool2D cloth = CreatePointerTool(
            "DustWipeTest_Cloth",
            camera,
            dustStationPosition + new Vector2(-1.05f, -1.65f),
            CookingRuntimeVisuals2D.GetClothSprite(),
            Vector3.one,
            26);
        CreateDustWipeIngredient(cloth, dustStationPosition + new Vector2(0f, 0.18f));

        CreateStationBoard("GrindingTest_Board", grindingStationPosition);
        PreparationPointerTool2D grinder = CreatePointerTool(
            "GrindingTest_Grinder",
            camera,
            grindingStationPosition + new Vector2(-1.05f, -1.65f),
            CreateGrinderSprite(),
            new Vector3(0.64f, 0.64f, 1f),
            26);
        CreateGrindableIngredient(grinder, grindingStationPosition + new Vector2(0f, 0.18f));

        CreateSceneLabel();
    }

    private static Camera EnsureCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.11f, 0.13f, 0.15f, 1f);
        return camera;
    }

    private static void CreateStationBoard(string name, Vector2 position)
    {
        GameObject board = CookingRuntimeVisuals2D.CreateBoard(new Vector3(position.x, position.y, 0.1f), new Vector2(3.45f, 2.18f), -2);
        board.name = name;

        BoxCollider2D collider = board.AddComponent<BoxCollider2D>();
        SpriteRenderer renderer = board.GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null)
        {
            collider.size = renderer.sprite.bounds.size;
            collider.offset = renderer.sprite.bounds.center;
        }
        else
        {
            collider.size = Vector2.one;
        }
    }

    private static void CreateStationLabel(string labelText, Vector2 stationPosition)
    {
        GameObject label = new($"{labelText}_Label");
        label.transform.position = new Vector3(stationPosition.x, stationPosition.y + 1.65f, 0f);

        TextMesh text = label.AddComponent<TextMesh>();
        text.text = labelText;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 36;
        text.characterSize = 0.07f;
        text.color = new Color(0.92f, 0.95f, 0.92f, 1f);

        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 20;
    }

    private void CreateCuttableIngredient(PreparationKnifeTool2D knife, Vector2 position)
    {
        GameObject ingredient = new("CuttingTest_Ingredient");
        ingredient.transform.position = position;
        ingredient.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        SpriteRenderer renderer = ingredient.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateIngredientSprite(
            "CuttingTest_IngredientSprite",
            new Color(0.95f, 0.38f, 0.12f, 1f),
            new Color(1f, 0.62f, 0.18f, 1f),
            new Color(1f, 0.82f, 0.36f, 1f));
        renderer.sortingOrder = 5;

        BoxCollider2D collider = ingredient.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = renderer.sprite.bounds.size;

        CuttableIngredient2D cuttable = ingredient.AddComponent<CuttableIngredient2D>();
        cuttable.BindKnife(knife);
    }

    private void CreateDustWipeIngredient(PreparationPointerTool2D cloth, Vector2 position)
    {
        GameObject ingredient = new("DustWipeTest_Ingredient");
        ingredient.transform.position = position;
        ingredient.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        SpriteRenderer renderer = ingredient.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateIngredientSprite(
            "DustWipeTest_IngredientSprite",
            new Color(0.32f, 0.72f, 0.32f, 1f),
            new Color(0.52f, 0.88f, 0.42f, 1f),
            new Color(0.74f, 0.94f, 0.52f, 1f));
        renderer.sortingOrder = 5;

        BoxCollider2D collider = ingredient.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = renderer.sprite.bounds.size;

        DustWipeIngredient2D dustWipe = ingredient.AddComponent<DustWipeIngredient2D>();
        dustWipe.BindCloth(cloth);
    }

    private void CreateGrindableIngredient(PreparationPointerTool2D grinder, Vector2 position)
    {
        GameObject ingredient = new("GrindingTest_Ingredient");
        ingredient.transform.position = position;
        ingredient.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        SpriteRenderer renderer = ingredient.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateIngredientSprite(
            "GrindingTest_IngredientSprite",
            new Color(0.72f, 0.56f, 0.30f, 1f),
            new Color(0.93f, 0.76f, 0.42f, 1f),
            new Color(1f, 0.90f, 0.62f, 1f));
        renderer.sortingOrder = 5;

        BoxCollider2D collider = ingredient.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = renderer.sprite.bounds.size;

        GrindableIngredient2D grindable = ingredient.AddComponent<GrindableIngredient2D>();
        grindable.BindGrindingTool(grinder);
    }

    private static void CreateSceneLabel()
    {
        GameObject label = new("PreparationTest_Label");
        label.transform.position = new Vector3(0f, 4.45f, 0f);

        TextMesh text = label.AddComponent<TextMesh>();
        text.text = "Drag each tool on its ingredient";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 42;
        text.characterSize = 0.075f;
        text.color = new Color(0.92f, 0.95f, 0.92f, 1f);

        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 20;
    }

    private PreparationKnifeTool2D CreateKnife(Camera camera, Vector2 position)
    {
        GameObject knife = new("CuttingTest_Knife");
        knife.transform.position = position;
        knife.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        SpriteRenderer renderer = knife.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateKnifeSprite();
        renderer.sortingOrder = 25;

        BoxCollider2D collider = knife.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = renderer.sprite.bounds.size;
        collider.offset = renderer.sprite.bounds.center;

        PreparationKnifeTool2D tool = knife.AddComponent<PreparationKnifeTool2D>();
        tool.Configure(camera, new Vector2(0.92f, 0f));
        return tool;
    }

    private static PreparationPointerTool2D CreatePointerTool(
        string name,
        Camera camera,
        Vector2 position,
        Sprite sprite,
        Vector3 scale,
        int sortingOrder)
    {
        GameObject toolObject = new(name);
        toolObject.transform.position = position;
        toolObject.transform.localScale = scale;

        SpriteRenderer renderer = toolObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        BoxCollider2D collider = toolObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = sprite.bounds.size;
        collider.offset = sprite.bounds.center;

        PreparationPointerTool2D tool = toolObject.AddComponent<PreparationPointerTool2D>();
        tool.Configure(camera);
        return tool;
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
        return sprite;
    }

    private static Sprite CreateKnifeSprite()
    {
        const int width = 160;
        const int height = 48;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

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

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        sprite.name = "CuttingTest_KnifeSprite";
        return sprite;
    }

    private static Sprite CreateClothSprite()
    {
        const int width = 96;
        const int height = 72;
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

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        sprite.name = "DustWipeTest_ClothSprite";
        return sprite;
    }

    private static Sprite CreateGrinderSprite()
    {
        const int width = 88;
        const int height = 128;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

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

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        sprite.name = "GrindingTest_GrinderSprite";
        return sprite;
    }
}
