using TMPro;
using UnityEngine;

public class OrchestraHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text timelineText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private TMP_Text songTitleText;
    [SerializeField] private TMP_Text controlsLeftText;
    [SerializeField] private TMP_Text controlsRightText;
    [SerializeField] private TMP_Text finishedText;

    private void Reset()
    {
        if (hudRoot == null)
        {
            hudRoot = gameObject;
        }
    }

    private void Awake()
    {
        if (hudRoot == null)
        {
            hudRoot = gameObject;
        }

        EnsureHudTexts();

        if (stateText != null)
        {
            stateText.gameObject.SetActive(true);
        }

        if (finishedText != null)
        {
            finishedText.gameObject.SetActive(false);
        }
    }

    private void EnsureHudTexts()
    {
        Transform parent = transform.parent != null ? transform.parent : transform;

        songTitleText ??= CreateScreenText(parent, "SongTitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(720f, 120f), 30f, FontStyles.Bold, TextAlignmentOptions.Center, "BEETHOVEN\nSYMPHONY NO. 5 - I. ALLEGRO CON BRIO");
        controlsLeftText ??= CreateScreenText(parent, "ControlsPanelLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 22f), new Vector2(360f, 120f), 15f, FontStyles.Normal, TextAlignmentOptions.BottomLeft, "[1] STRINGS\n[2] WOODWINDS\n[3] BRASS\n[4] PERCUSSION");
        controlsRightText ??= CreateScreenText(parent, "ControlsPanelRight", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 22f), new Vector2(360f, 120f), 15f, FontStyles.Normal, TextAlignmentOptions.BottomRight, "[SPACE] TUTTI\n[R] RESTART\n[ESC] RELEASE MOUSE");
        timelineText ??= CreateScreenText(parent, "TimelineText", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 112f), new Vector2(320f, 48f), 20f, FontStyles.Bold, TextAlignmentOptions.BottomRight, string.Empty);
        stateText ??= CreateScreenText(parent, "StateText", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 82f), new Vector2(420f, 40f), 14f, FontStyles.Normal, TextAlignmentOptions.BottomRight, string.Empty);
        finishedText ??= CreateScreenText(parent, "FinishedText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 96f), 34f, FontStyles.Bold, TextAlignmentOptions.Center, "Performance Finished\nPress R to restart");
    }

    private static TMP_Text CreateScreenText(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        string textValue)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(anchorMax.x, anchorMin.y);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = new Color(0.97f, 0.97f, 0.95f);
        text.text = textValue;
        return text;
    }

    public void SetVisible(bool isVisible)
    {
        if (hudRoot != null)
        {
            hudRoot.SetActive(isVisible);
        }

        if (songTitleText != null)
        {
            songTitleText.gameObject.SetActive(isVisible);
        }

        if (controlsLeftText != null)
        {
            controlsLeftText.gameObject.SetActive(isVisible);
        }

        if (controlsRightText != null)
        {
            controlsRightText.gameObject.SetActive(isVisible);
        }

        if (timelineText != null)
        {
            timelineText.gameObject.SetActive(isVisible);
        }

        if (stateText != null)
        {
            stateText.gameObject.SetActive(isVisible);
        }

        if (finishedText != null)
        {
            finishedText.gameObject.SetActive(false);
        }
    }

    public void SetPlaybackState(SongPlaybackState state, float currentTime, float totalTime, bool hasValidSetup)
    {
        if (timelineText != null)
        {
            if (hasValidSetup)
            {
                timelineText.text = $"{FormatTime(currentTime)} / {FormatTime(totalTime)}";
            }
            else
            {
                timelineText.text = "Assign stems in OrchestraManager";
            }
        }

        if (stateText != null)
        {
            if (!hasValidSetup)
            {
                stateText.text = "Assign stems in OrchestraManager";
            }
            else if (state == SongPlaybackState.Preparing)
            {
                int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(currentTime));
                stateText.text = $"Performance begins in {remainingSeconds}";
            }
            else if (state == SongPlaybackState.Playing)
            {
                stateText.text = "Performance in progress";
            }
            else
            {
                stateText.text = "Performance finished";
            }
        }

        if (finishedText != null)
        {
            finishedText.gameObject.SetActive(hasValidSetup && state == SongPlaybackState.Finished);
        }
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
