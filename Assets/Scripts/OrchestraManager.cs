using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class OrchestraManager : MonoBehaviour
{
    [Header("Sections")]
    [SerializeField] private OrchestraSection strings;
    [SerializeField] private OrchestraSection woodwinds;
    [SerializeField] private OrchestraSection brass;
    [SerializeField] private OrchestraSection percussion;

    [Header("Playback")]
    [SerializeField] private float startDelay = 10f;
    [SerializeField] private float fadeDuration = 0.1f;
    [SerializeField] private float durationWarningThresholdSeconds = 0.5f;

    [Header("HUD")]
    [SerializeField] private OrchestraHUD hud;
    [SerializeField] private TMP_Text finishedText;
    [SerializeField] private SimpleFPSController playerController;

    [Header("Start Menu")]
    [SerializeField] private string gameTitle = "Orchestra Conductor";
    [SerializeField] private string producerName = "Xinyue Guo";

    private SongPlaybackState _playbackState = SongPlaybackState.Preparing;
    private double _scheduledStartDspTime;
    private double _referenceDuration;
    private bool _hasValidSetup;
    private bool _hasStarted;
    private GameObject _startMenuRoot;
    private Volume _menuBlurVolume;

    public SongPlaybackState PlaybackState => _playbackState;
    public bool IsPreparing => _playbackState == SongPlaybackState.Preparing;
    public bool IsFinished => _playbackState == SongPlaybackState.Finished;
    public float CurrentPlaybackTime
    {
        get
        {
            if (!_hasValidSetup)
            {
                return 0f;
            }

            if (_playbackState == SongPlaybackState.Preparing)
            {
                return 0f;
            }

            double elapsed = AudioSettings.dspTime - _scheduledStartDspTime;
            return Mathf.Clamp((float)elapsed, 0f, TotalDuration);
        }
    }

    public float CountdownRemaining
    {
        get
        {
            if (!_hasValidSetup || _playbackState != SongPlaybackState.Preparing)
            {
                return 0f;
            }

            return Mathf.Max(0f, (float)(_scheduledStartDspTime - AudioSettings.dspTime));
        }
    }

    public float TotalDuration => (float)_referenceDuration;
    public bool StringsActive => strings != null && strings.IsActive;
    public bool WoodwindsActive => woodwinds != null && woodwinds.IsActive;
    public bool BrassActive => brass != null && brass.IsActive;
    public bool PercussionActive => percussion != null && percussion.IsActive;

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<SimpleFPSController>();
        }

        _hasValidSetup = ValidateSections();
        InitializeSections();
        ApplySectionAudibility(false, false, false, false);
        StopAllSections();
        playerController?.SetInputEnabled(false);
        hud?.SetVisible(false);

        BuildStartMenu();
        SetMenuBlurActive(true);
        RefreshHud();
    }

    private void Update()
    {
        if (!_hasStarted)
        {
            return;
        }

        HandleGlobalInput();

        if (!_hasValidSetup)
        {
            RefreshHud();
            return;
        }

        UpdatePlaybackState();
        HandleSectionInput();
        UpdateVolumes();
        RefreshHud();
    }

    public void RestartSong()
    {
        if (!_hasStarted)
        {
            return;
        }

        StopAllSections();
        ApplySectionAudibility(false, false, false, false);
        SchedulePlaybackFromStart();
        RefreshHud();
    }

    private void HandleGlobalInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            RestartSong();
        }
    }

    private void HandleSectionInput()
    {
        if (_playbackState == SongPlaybackState.Finished)
        {
            ApplySectionAudibility(false, false, false, false);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            ApplySectionAudibility(false, false, false, false);
            return;
        }

        bool tutti = keyboard.spaceKey.isPressed;

        ApplySectionAudibility(
            tutti || keyboard.digit1Key.isPressed,
            tutti || keyboard.digit2Key.isPressed,
            tutti || keyboard.digit3Key.isPressed,
            tutti || keyboard.digit4Key.isPressed);
    }

    private void UpdatePlaybackState()
    {
        if (_playbackState == SongPlaybackState.Preparing && AudioSettings.dspTime >= _scheduledStartDspTime)
        {
            _playbackState = SongPlaybackState.Playing;
        }

        if (_playbackState == SongPlaybackState.Playing && CurrentPlaybackTime >= TotalDuration)
        {
            _playbackState = SongPlaybackState.Finished;
            ApplySectionAudibility(false, false, false, false);
        }

    }

    private void SchedulePlaybackFromStart()
    {
        _playbackState = SongPlaybackState.Preparing;
        _scheduledStartDspTime = AudioSettings.dspTime + startDelay;

        foreach (OrchestraSection section in EnumerateSections())
        {
            section.SchedulePlay(_scheduledStartDspTime);
        }
    }

    private void StopAllSections()
    {
        foreach (OrchestraSection section in EnumerateSections())
        {
            section.Stop();
        }
    }

    private void ApplySectionAudibility(bool stringsOn, bool woodwindsOn, bool brassOn, bool percussionOn)
    {
        strings?.SetActive(stringsOn);
        woodwinds?.SetActive(woodwindsOn);
        brass?.SetActive(brassOn);
        percussion?.SetActive(percussionOn);
    }

    private void InitializeSections()
    {
        foreach (OrchestraSection section in EnumerateSections())
        {
            section?.Initialize();
        }
    }

    private void UpdateVolumes()
    {
        foreach (OrchestraSection section in EnumerateSections())
        {
            section?.UpdateVolumes(fadeDuration);
        }
    }

    private bool ValidateSections()
    {
        bool isValid = true;
        AudioClip referenceClip = null;
        AudioSource referenceSource = null;
        List<string> durationWarnings = new();

        foreach (OrchestraSection section in EnumerateSections())
        {
            if (section == null)
            {
                Debug.LogError("An orchestra section reference is missing on OrchestraManager.", this);
                isValid = false;
                continue;
            }

            if (!section.HasSources)
            {
                Debug.LogError($"Section '{section.SectionName}' has no AudioSources assigned.", this);
                isValid = false;
                continue;
            }

            AudioSource[] sources = section.Sources;
            int playableSourceCount = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null)
                {
                    Debug.LogError($"Section '{section.SectionName}' contains a null AudioSource at index {i}.", this);
                    isValid = false;
                    continue;
                }

                if (source.clip == null)
                {
                    continue;
                }

                playableSourceCount++;

                if (referenceClip == null)
                {
                    referenceClip = source.clip;
                    referenceSource = source;
                    _referenceDuration = source.clip.length;
                    continue;
                }

                float durationDelta = Mathf.Abs(source.clip.length - referenceClip.length);
                if (durationDelta > durationWarningThresholdSeconds)
                {
                    durationWarnings.Add(
                        $"{referenceSource.name}: {referenceClip.length:F1} sec | {source.name}: {source.clip.length:F1} sec");
                }
            }

            if (playableSourceCount == 0)
            {
                Debug.LogError($"Section '{section.SectionName}' does not contain any AudioSources with an AudioClip assigned.", this);
                isValid = false;
            }
        }

        if (durationWarnings.Count > 0)
        {
            Debug.LogWarning("Audio stem durations differ:\n" + string.Join("\n", durationWarnings), this);
        }

        return isValid && referenceClip != null;
    }

    private IEnumerable<OrchestraSection> EnumerateSections()
    {
        yield return strings;
        yield return woodwinds;
        yield return brass;
        yield return percussion;
    }

    private void RefreshHud()
    {
        if (hud == null)
        {
            return;
        }

        float hudTime = _playbackState == SongPlaybackState.Preparing ? CountdownRemaining : CurrentPlaybackTime;
        hud.SetPlaybackState(_playbackState, hudTime, TotalDuration, _hasValidSetup);
    }

    private void StartPerformance()
    {
        _hasStarted = true;
        _playbackState = _hasValidSetup ? SongPlaybackState.Preparing : SongPlaybackState.Finished;

        hud?.SetVisible(true);
        playerController?.SetInputEnabled(true);
        SetMenuBlurActive(false);

        if (_startMenuRoot != null)
        {
            Destroy(_startMenuRoot);
            _startMenuRoot = null;
        }

        if (_hasValidSetup)
        {
            SchedulePlaybackFromStart();
        }

        RefreshHud();
    }

    private void BuildStartMenu()
    {
        EnsureEventSystem();

        _startMenuRoot = new GameObject("StartMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = _startMenuRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = _startMenuRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlay = CreateUiObject("GlassOverlay", _startMenuRoot.transform);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(1f, 1f, 1f, 0.08f);
        StretchToFullScreen(overlay.GetComponent<RectTransform>());

        GameObject title = CreateText("Title", _startMenuRoot.transform, gameTitle, 78, FontStyles.Bold);
        TextMeshProUGUI titleLabel = title.GetComponent<TextMeshProUGUI>();
        titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
        titleLabel.overflowMode = TextOverflowModes.Overflow;
        titleLabel.fontStyle = FontStyles.Bold;
        ApplyOutlinedTitleStyle(titleLabel);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(1500f, 130f);
        titleRect.anchoredPosition = new Vector2(0f, 165f);

        GameObject subtitle = CreateText("Producer", _startMenuRoot.transform, producerName, 28, FontStyles.Normal);
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 0f);
        subtitleRect.anchorMax = new Vector2(0.5f, 0f);
        subtitleRect.pivot = new Vector2(0.5f, 0f);
        subtitleRect.sizeDelta = new Vector2(600f, 48f);
        subtitleRect.anchoredPosition = new Vector2(0f, 54f);

        GameObject playButtonObject = CreateUiObject("PlayButton", _startMenuRoot.transform);
        Image playButtonImage = playButtonObject.AddComponent<Image>();
        playButtonImage.color = new Color(0.22f, 0.7f, 0.34f, 0.96f);
        Button playButton = playButtonObject.AddComponent<Button>();
        ColorBlock colors = playButton.colors;
        colors.normalColor = new Color(0.22f, 0.7f, 0.34f, 0.96f);
        colors.highlightedColor = new Color(0.3f, 0.8f, 0.42f, 1f);
        colors.pressedColor = new Color(0.16f, 0.52f, 0.25f, 1f);
        colors.selectedColor = colors.highlightedColor;
        playButton.colors = colors;
        playButton.targetGraphic = playButtonImage;
        playButton.onClick.AddListener(StartPerformance);

        RectTransform playButtonRect = playButtonObject.GetComponent<RectTransform>();
        playButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
        playButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
        playButtonRect.sizeDelta = new Vector2(340f, 104f);
        playButtonRect.anchoredPosition = new Vector2(0f, 10f);

        GameObject playLabel = CreateText("Label", playButtonObject.transform, "Play", 46, FontStyles.Bold);
        RectTransform playLabelRect = playLabel.GetComponent<RectTransform>();
        StretchToFullScreen(playLabelRect);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    private void SetMenuBlurActive(bool isActive)
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        if (_menuBlurVolume == null)
        {
            _menuBlurVolume = targetCamera.GetComponent<Volume>();
            if (_menuBlurVolume == null)
            {
                _menuBlurVolume = targetCamera.gameObject.AddComponent<Volume>();
            }

            _menuBlurVolume.isGlobal = true;
            _menuBlurVolume.priority = 100f;

            if (_menuBlurVolume.profile == null)
            {
                _menuBlurVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }
        }

        if (!_menuBlurVolume.profile.TryGet(out DepthOfField depthOfField))
        {
            depthOfField = _menuBlurVolume.profile.Add<DepthOfField>(true);
        }

        depthOfField.active = isActive;
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(0.1f);
        depthOfField.gaussianEnd.Override(3.5f);
        depthOfField.gaussianMaxRadius.Override(2f);
        _menuBlurVolume.enabled = isActive;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreateText(string name, Transform parent, string text, float fontSize, FontStyles style)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        return textObject;
    }

    private static void ApplyOutlinedTitleStyle(TextMeshProUGUI label)
    {
        if (label.fontSharedMaterial == null)
        {
            return;
        }

        Material materialInstance = new(label.fontSharedMaterial);
        materialInstance.SetFloat(ShaderUtilities.ID_FaceDilate, 0.08f);
        materialInstance.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
        materialInstance.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
        label.fontMaterial = materialInstance;
    }

    private static void StretchToFullScreen(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
