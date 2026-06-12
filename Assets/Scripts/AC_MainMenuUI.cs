using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AC_MainMenuUI : MonoBehaviour
{
    private AC_GameManager gameManager;
    private Canvas canvas;
    private GameObject menuRoot;
    private Text footerText;
    private Font uiFont;

    public void Configure(AC_GameManager manager)
    {
        gameManager = manager;
        EnsureCanvas();
        EnsureEventSystem();
        EnsureMenu();
    }

    public void ShowMenu(bool visible, string footerMessage)
    {
        EnsureMenu();
        if (menuRoot != null)
        {
            menuRoot.SetActive(visible);
        }

        if (footerText != null)
        {
            footerText.text = footerMessage;
        }
    }

    private void EnsureCanvas()
    {
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.SetParent(null);
    }

    private void EnsureMenu()
    {
        if (canvas == null)
        {
            EnsureCanvas();
        }

        if (menuRoot != null)
        {
            return;
        }

        menuRoot = new GameObject("AC_MainMenu", typeof(RectTransform), typeof(Image));
        menuRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = menuRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image rootImage = menuRoot.GetComponent<Image>();
        rootImage.color = new Color(0.05f, 0.08f, 0.12f, 0.88f);

        CreateLabel("Title", "ABRAZO COMPETITIVO", new Vector2(0.5f, 0.78f), 52, TextAnchor.MiddleCenter, Color.white);
        CreateLabel("Subtitle", "Empuja, abraza, salta y no te caigas.", new Vector2(0.5f, 0.7f), 24, TextAnchor.MiddleCenter, new Color(0.86f, 0.91f, 0.97f));
        CreateLabel("Controls", BuildControlsText(), new Vector2(0.5f, 0.52f), 24, TextAnchor.MiddleCenter, new Color(0.93f, 0.93f, 0.93f));
        footerText = CreateLabel("Footer", "Listo para jugar", new Vector2(0.5f, 0.26f), 22, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));

        CreateButton("PlayButton", "Jugar", new Vector2(0.42f, 0.14f), new Vector2(240f, 72f), OnPlayPressed);
        CreateButton("ExitButton", "Salir", new Vector2(0.58f, 0.14f), new Vector2(240f, 72f), OnExitPressed);
    }

    private Text CreateLabel(string objectName, string content, Vector2 anchor, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(menuRoot.transform, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(1100f, fontSize * 4f);

        Text label = labelObject.GetComponent<Text>();
        label.font = uiFont;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.text = content;
        return label;
    }

    private void CreateButton(string objectName, string content, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(menuRoot.transform, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.33f, 0.46f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.23f, 0.45f, 0.62f, 1f);
        colors.pressedColor = new Color(0.12f, 0.25f, 0.35f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(callback);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = content;
    }

    private string BuildControlsText()
    {
        return "P1  WASD mover | Q saltar | Space abrazo | Shift dash | Ctrl bloqueo\n" +
               "P2  Flechas mover | Keypad0 saltar | Enter abrazo | Right Shift dash | Right Ctrl bloqueo\n" +
               "Caerse del mapa resta 1 punto y te devuelve al spawn.";
    }

    private void OnPlayPressed()
    {
        if (gameManager != null)
        {
            gameManager.BeginMatchFromMenu();
        }
    }

    private void OnExitPressed()
    {
        Application.Quit();
    }
}
