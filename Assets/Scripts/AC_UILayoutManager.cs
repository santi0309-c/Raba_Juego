using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FIX #3: Posiciona automáticamente los carteles de UI usando anchors relativos.
/// Agregar al Canvas. Asignar las referencias a los Text.
/// No requiere configuración manual de RectTransform — este script ajusta todo en Awake.
/// </summary>
public class AC_UILayoutManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public Text scoreText;
    public Text statusText;
    public Text dashTextP1;
    public Text dashTextP2;

    [Header("Margen desde bordes (en píxeles)")]
    public float margin = 24f;

    public void AutoWire(Text currentStatusText, Text currentScoreText)
    {
        if (scoreText == null)
        {
            scoreText = currentScoreText != null ? currentScoreText : FindText("ScoreText");
        }

        if (statusText == null)
        {
            statusText = currentStatusText != null ? currentStatusText : FindText("StatusText");
        }

        if (dashTextP1 == null)
        {
            dashTextP1 = FindText("DashTextP1");
        }

        if (dashTextP2 == null)
        {
            dashTextP2 = FindText("DashTextP2");
        }

        ApplyLayout();
    }

    private void Awake()
    {
        AutoWire(null, null);
    }

    private void ApplyLayout()
    {
        SetupTextElement(scoreText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -margin), TextAnchor.UpperCenter);
        SetupTextElement(statusText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -margin * 2f - 10f), TextAnchor.UpperCenter);

        if (dashTextP1 != null)
        {
            SetupTextElement(dashTextP1, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(margin, margin), TextAnchor.LowerLeft);
        }

        if (dashTextP2 != null)
        {
            SetupTextElement(dashTextP2, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-margin, margin), TextAnchor.LowerRight);
        }
    }

    private void SetupTextElement(Text text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        if (text == null) return;

        RectTransform rt = text.rectTransform;

        // Anclajes responsivos
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
        rt.anchoredPosition = anchoredPosition;

        // Tamaño se ajusta al contenido
        ContentSizeFitter fitter = text.gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = text.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Texto legible y con tamaño base
        text.fontSize = Mathf.Max(text.fontSize, 24);
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        // Sombra para legibilidad sobre fondo
        Shadow shadow = text.gameObject.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(1f, -1f);

        rt.localScale = Vector3.one;
    }

    private Text FindText(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null ? target.GetComponent<Text>() : null;
    }
}
