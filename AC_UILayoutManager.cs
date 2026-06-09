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

    private void Awake()
    {
        SetupTextElement(scoreText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -margin));
        SetupTextElement(statusText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);

        if (dashTextP1 != null)
        {
            SetupTextElement(dashTextP1, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(margin, margin));
            dashTextP1.alignment = TextAnchor.LowerLeft;
        }

        if (dashTextP2 != null)
        {
            SetupTextElement(dashTextP2, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-margin, margin));
            dashTextP2.alignment = TextAnchor.LowerRight;
        }
    }

    private void SetupTextElement(Text text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
    {
        if (text == null) return;

        RectTransform rt = text.rectTransform;

        // Anclajes responsivos
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;

        // Tamaño se ajusta al contenido
        ContentSizeFitter fitter = text.gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = text.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Texto grande y legible
        text.fontSize = 28;
        text.color = Color.white;

        // Sombra para legibilidad sobre fondo
        Shadow shadow = text.gameObject.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }
}
