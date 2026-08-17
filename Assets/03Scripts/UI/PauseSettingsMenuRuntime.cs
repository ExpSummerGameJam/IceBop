using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class PauseSettingsMenuRuntime : MonoBehaviour
{
    private static PauseSettingsMenuRuntime instance;
    private static Font cachedFont;

    [Header("Managers")]
    [SerializeField] private GameSettingsManager settings;

    [Header("Panels")]
    [SerializeField] private GameObject overlayObject;
    [SerializeField] private GameObject pausePanelObject;
    [SerializeField] private GameObject settingsPanelObject;

    [Header("Pause Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Settings Controls")]
    [SerializeField] private Button backButton;
    [SerializeField] private Slider backgroundVolumeSlider;
    [SerializeField] private Slider soundEffectVolumeSlider;
    [SerializeField] private Text backgroundVolumeValueText;
    [SerializeField] private Text soundEffectVolumeValueText;
    [SerializeField] private Button windowModeButton;
    [SerializeField] private Button fullScreenButton;

    private static Font MenuFont
    {
        get
        {
            if (cachedFont != null)
                return cachedFont;

            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return cachedFont;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveMissingReferences();
        ConfigureTexts();
        ConfigureButtons();
        ConfigureSliders();
        HideAll();
    }

    private void Update()
    {
        if (!WasEscapePressedThisFrame())
            return;

        if (settingsPanelObject != null && settingsPanelObject.activeSelf)
        {
            ShowPauseMenu();
            return;
        }

        if (pausePanelObject != null && pausePanelObject.activeSelf)
        {
            ResumeGame();
            return;
        }

        OpenPauseMenu();
    }

    public void ResumeGame()
    {
        HideAll();
        GamePauseState.SetPaused(false);
    }

    public void OpenSettings()
    {
        GamePauseState.SetPaused(true);
        SetActive(overlayObject, true);
        SetActive(pausePanelObject, false);
        SetActive(settingsPanelObject, true);
        RefreshSettingsControls();
        Select(backgroundVolumeSlider != null ? backgroundVolumeSlider.gameObject : null);
    }

    public void ShowPauseMenu()
    {
        GamePauseState.SetPaused(true);
        SetActive(overlayObject, true);
        SetActive(pausePanelObject, true);
        SetActive(settingsPanelObject, false);
        Select(resumeButton != null ? resumeButton.gameObject : null);
    }

    public void QuitGame()
    {
        GamePauseState.SetPaused(false);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OpenPauseMenu()
    {
        ShowPauseMenu();
    }

    private void HideAll()
    {
        SetActive(overlayObject, false);
        SetActive(pausePanelObject, false);
        SetActive(settingsPanelObject, false);
    }

    private void ResolveMissingReferences()
    {
        if (settings == null)
            settings = GetComponent<GameSettingsManager>();

        if (settings == null)
            settings = GameSettingsManager.EnsureExists();

        Transform canvas = transform.Find("PauseSettingsCanvas");
        Transform overlay = canvas != null ? canvas.Find("Overlay") : transform.Find("Overlay");

        if (overlayObject == null && overlay != null)
            overlayObject = overlay.gameObject;

        if (pausePanelObject == null && overlay != null)
            pausePanelObject = overlay.Find("PausePanel")?.gameObject;

        if (settingsPanelObject == null && overlay != null)
            settingsPanelObject = overlay.Find("SettingsPanel")?.gameObject;

        Transform pausePanel = pausePanelObject != null ? pausePanelObject.transform : null;
        Transform settingsPanel = settingsPanelObject != null ? settingsPanelObject.transform : null;

        if (resumeButton == null)
            resumeButton = pausePanel?.Find("ResumeButton")?.GetComponent<Button>();
        if (settingsButton == null)
            settingsButton = pausePanel?.Find("SettingsButton")?.GetComponent<Button>();
        if (quitButton == null)
            quitButton = pausePanel?.Find("QuitButton")?.GetComponent<Button>();

        if (backButton == null)
            backButton = settingsPanel?.Find("BackButton")?.GetComponent<Button>();
        if (backgroundVolumeSlider == null)
            backgroundVolumeSlider = settingsPanel?.Find("BackgroundVolumeSlider")?.GetComponent<Slider>();
        if (soundEffectVolumeSlider == null)
            soundEffectVolumeSlider = settingsPanel?.Find("SoundEffectVolumeSlider")?.GetComponent<Slider>();
        if (backgroundVolumeValueText == null)
            backgroundVolumeValueText = settingsPanel?.Find("BackgroundVolumeValue")?.GetComponent<Text>();
        if (soundEffectVolumeValueText == null)
            soundEffectVolumeValueText = settingsPanel?.Find("SoundEffectVolumeValue")?.GetComponent<Text>();
        if (windowModeButton == null)
            windowModeButton = settingsPanel?.Find("WindowModeButton")?.GetComponent<Button>();
        if (fullScreenButton == null)
            fullScreenButton = settingsPanel?.Find("FullScreenButton")?.GetComponent<Button>();

        if (settings == null)
            Debug.LogError("PauseSettingsMenuRuntime needs GameSettingsManager on the prefab or in the scene.", this);
    }

    private void ConfigureTexts()
    {
        foreach (Text text in GetComponentsInChildren<Text>(true))
        {
            if (text.font == null)
                text.font = MenuFont;
        }
    }

    private void ConfigureButtons()
    {
        ConfigureButton(resumeButton, ResumeGame, false);
        ConfigureButton(settingsButton, OpenSettings, false);
        ConfigureButton(quitButton, QuitGame, false);
        ConfigureButton(backButton, ShowPauseMenu, false);
        ConfigureButton(windowModeButton, () => SetFullScreen(false), false);
        ConfigureButton(fullScreenButton, () => SetFullScreen(true), false);
    }

    private static void ConfigureButton(Button button, UnityEngine.Events.UnityAction onClick, bool selected)
    {
        if (button == null)
            return;

        Graphic graphic = button.GetComponent<Graphic>();
        if (graphic != null)
            button.targetGraphic = graphic;

        button.colors = CreateButtonColors(selected);
        button.onClick.AddListener(() => GameSfxPlayer.Play(GameSfx.Button));
        button.onClick.AddListener(onClick);
    }

    private void ConfigureSliders()
    {
        ConfigureSlider(backgroundVolumeSlider);
        ConfigureSlider(soundEffectVolumeSlider);

        if (backgroundVolumeSlider != null)
            backgroundVolumeSlider.onValueChanged.AddListener(OnBackgroundVolumeChanged);
        if (soundEffectVolumeSlider != null)
            soundEffectVolumeSlider.onValueChanged.AddListener(OnSoundEffectVolumeChanged);
    }

    private static void ConfigureSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;

        if (slider.fillRect == null)
            slider.fillRect = slider.transform.Find("Fill Area/Fill") as RectTransform;

        if (slider.handleRect == null)
            slider.handleRect = slider.transform.Find("Handle Slide Area/Handle") as RectTransform;

        if (slider.targetGraphic == null && slider.handleRect != null)
            slider.targetGraphic = slider.handleRect.GetComponent<Graphic>();
    }

    private void RefreshSettingsControls()
    {
        if (settings == null)
            return;

        if (backgroundVolumeSlider != null)
            backgroundVolumeSlider.SetValueWithoutNotify(Mathf.Round(settings.BackgroundVolume01 * 100f));
        if (soundEffectVolumeSlider != null)
            soundEffectVolumeSlider.SetValueWithoutNotify(Mathf.Round(settings.SoundEffectVolume01 * 100f));

        UpdateVolumeTexts();
        UpdateModeButtons();
    }

    private void OnBackgroundVolumeChanged(float value)
    {
        if (settings == null)
            return;

        settings.SetBackgroundVolumePercent(value);
        UpdateVolumeTexts();
    }

    private void OnSoundEffectVolumeChanged(float value)
    {
        if (settings == null)
            return;

        settings.SetSoundEffectVolumePercent(value);
        UpdateVolumeTexts();
    }

    private void UpdateVolumeTexts()
    {
        if (backgroundVolumeValueText != null && backgroundVolumeSlider != null)
            backgroundVolumeValueText.text = Mathf.RoundToInt(backgroundVolumeSlider.value).ToString();

        if (soundEffectVolumeValueText != null && soundEffectVolumeSlider != null)
            soundEffectVolumeValueText.text = Mathf.RoundToInt(soundEffectVolumeSlider.value).ToString();
    }

    private void SetFullScreen(bool isFullScreen)
    {
        if (settings == null)
            return;

        settings.SetFullScreen(isFullScreen);
        UpdateModeButtons();
    }

    private void UpdateModeButtons()
    {
        if (settings == null)
            return;

        bool isFullScreen = settings.IsFullScreen;

        if (windowModeButton != null)
            windowModeButton.colors = CreateButtonColors(!isFullScreen);
        if (fullScreenButton != null)
            fullScreenButton.colors = CreateButtonColors(isFullScreen);
    }

    private static ColorBlock CreateButtonColors(bool selected)
    {
        Color normal = selected ? new Color(0.18f, 0.62f, 0.92f, 1f) : new Color(0.18f, 0.2f, 0.25f, 1f);
        Color highlighted = selected ? new Color(0.22f, 0.72f, 1f, 1f) : new Color(0.25f, 0.28f, 0.35f, 1f);
        Color pressed = selected ? new Color(0.14f, 0.52f, 0.78f, 1f) : new Color(0.12f, 0.14f, 0.18f, 1f);

        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = highlighted;
        colors.pressedColor = pressed;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.13f, 0.5f);
        colors.colorMultiplier = 1f;
        return colors;
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current == null || target == null)
            return;

        EventSystem.current.SetSelectedGameObject(target);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private static bool WasEscapePressedThisFrame()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        pressed |= keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.Escape);
#endif

        return pressed;
    }
}
