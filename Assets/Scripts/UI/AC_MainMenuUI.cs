using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AC_MainMenuUI : MonoBehaviour
{
    public GameObject menuPrefab;
    public int startupCountdownFrom = 3;

    private AC_GameManager gameManager;
    private Canvas canvas;
    private GameObject menuRoot;
    private Text footerText;
    private Font uiFont;
    private Text countdownText;

    public void Configure(AC_GameManager manager)
    {
        gameManager = manager;

        if (menuPrefab != null)
        {
            menuRoot = Instantiate(menuPrefab);
            menuRoot.name = "AC_MainMenu";
            canvas = menuRoot.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            Transform footerTransform = menuRoot.transform.Find("Footer");
            if (footerTransform != null)
            {
                footerText = footerTransform.GetComponent<Text>();
            }

            ConectarBoton("PlayButton", OnPlayPressed);
            ConectarBoton("ExitButton", OnExitPressed);

            EnsureEventSystem();
            EnsureCountdownText();
            return;
        }

        EnsureCanvas();
        EnsureEventSystem();
        EnsureMenu();
        EnsureCountdownText();
    }

    private void ConectarBoton(string nombre, UnityEngine.Events.UnityAction accion)
    {
        Transform botonTransform = menuRoot.transform.Find(nombre);
        if (botonTransform == null) return;

        Button boton = botonTransform.GetComponent<Button>();
        if (boton != null)
        {
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(accion);
        }
    }

    public void HideMenuRoot()
    {
        EnsureMenu();
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
    }

    private Coroutine countdownRoutine;

    public void ShowMenu(bool visible, string footerMessage)
    {
        EnsureMenu();
        if (menuRoot == null) return;

        if (footerText != null)
        {
            footerText.text = footerMessage;
        }

        if (visible)
        {
            StopCountdown();
            menuRoot.SetActive(true);
        }
        else
        {
            menuRoot.SetActive(false);
        }
    }

    public IEnumerator RunStartupCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }
        countdownRoutine = StartCoroutine(CuentaRegresiva());
        yield return countdownRoutine;
    }

    private IEnumerator CuentaRegresiva()
    {
        HideMenuRoot();
        EnsureCountdownText();
        if (countdownText == null)
        {
            yield break;
        }

        for (int i = startupCountdownFrom; i > 0; i--)
        {
            countdownText.text = i.ToString();
            countdownText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
        }

        countdownText.gameObject.SetActive(false);
        countdownRoutine = null;
    }

    public void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
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

        menuRoot = new GameObject("AC_MainMenu", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        menuRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = menuRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(760f, 720f);

        Image rootImage = menuRoot.GetComponent<Image>();
        rootImage.color = new Color(0.05f, 0.08f, 0.12f, 0.92f);

        float y = 280f;
        CreateLabel("Title", "ABRAZO COMPETITIVO", new Vector2(0.5f, 0.5f), new Vector2(700f, 90f), new Vector2(0f, y), 60, TextAnchor.MiddleCenter, Color.white);
        y -= 85f;
        CreateLabel("Subtitle", "Empuja, abraza, salta y no te caigas.", new Vector2(0.5f, 0.5f), new Vector2(700f, 50f), new Vector2(0f, y), 26, TextAnchor.MiddleCenter, new Color(0.86f, 0.91f, 0.97f));
        y -= 130f;
        CreateLabel("Controls", BuildControlsText(), new Vector2(0.5f, 0.5f), new Vector2(700f, 180f), new Vector2(0f, y), 22, TextAnchor.MiddleCenter, new Color(0.93f, 0.93f, 0.93f));
        y -= 140f;
        footerText = CreateLabel("Footer", "Listo para jugar", new Vector2(0.5f, 0.5f), new Vector2(700f, 50f), new Vector2(0f, y), 24, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));
        y -= 95f;
        CreateButton("PlayButton", "Jugar", new Vector2(0.25f, 0.5f), new Vector2(260f, 80f), new Vector2(0f, y), OnPlayPressed);
        CreateButton("ExitButton", "Salir", new Vector2(0.75f, 0.5f), new Vector2(260f, 80f), new Vector2(0f, y), OnExitPressed);
    }

    private Text CreateLabel(string objectName, string content, Vector2 anchor, Vector2 size, Vector2 anchoredPosition, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(menuRoot.transform, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        Text label = labelObject.GetComponent<Text>();
        label.font = uiFont;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.text = content;

        AddShadow(labelObject);
        return label;
    }

    private void CreateButton(string objectName, string content, Vector2 anchor, Vector2 size, Vector2 anchoredPosition, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(menuRoot.transform, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

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
        button.onClick.RemoveAllListeners();
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
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = content;

        AddShadow(textObject);
    }

    private void EnsureCountdownText()
    {
        if (countdownText != null) return;
        if (canvas == null) EnsureCanvas();

        GameObject go = new GameObject("CountdownText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(400f, 400f);

        Text t = go.GetComponent<Text>();
        t.font = uiFont;
        t.fontSize = 180;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.text = "";

        AddShadow(go);
        go.SetActive(false);
        countdownText = t;
    }

    private void AddShadow(GameObject target)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = target.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private string BuildControlsText()
    {
        return "P1  WASD mover | Q saltar | Space abrazo | Shift dash | Ctrl bloqueo\n" +
               "P2  Flechas mover | End saltar | Enter abrazo | Right Shift dash | Right Ctrl bloqueo\n" +
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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
