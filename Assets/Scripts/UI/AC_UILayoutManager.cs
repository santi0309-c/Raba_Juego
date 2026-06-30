using UnityEngine;
using UnityEngine.UI;

public class AC_UILayoutManager : MonoBehaviour
{
    public Text scoreText;
    public Text statusText;
    public Text dashTextP1;
    public Text dashTextP2;

    public bool usarPrefab = false;

    public float margin = 24f;

    public void AutoWire(Text currentStatusText, Text currentScoreText)
    {
        if (scoreText == null)
        {
            if (currentScoreText != null)
            {
                scoreText = currentScoreText;
            }
            else
            {
                scoreText = FindText("ScoreText");
            }
        }

        if (statusText == null)
        {
            if (currentStatusText != null)
            {
                statusText = currentStatusText;
            }
            else
            {
                statusText = FindText("StatusText");
            }
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
        if (usarPrefab)
        {
            AplicarSoloEstilo(scoreText);
            AplicarSoloEstilo(statusText);
            AplicarSoloEstilo(dashTextP1);
            AplicarSoloEstilo(dashTextP2);
            return;
        }

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

    private void AplicarSoloEstilo(Text text)
    {
        if (text == null) return;
        text.fontSize = Mathf.Max(text.fontSize, 24);
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow shadow = text.gameObject.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    private void SetupTextElement(Text text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, TextAnchor alignment)
    {
        if (text == null) return;

        RectTransform rt = text.rectTransform;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
        rt.anchoredPosition = anchoredPosition;

        ContentSizeFitter fitter = text.gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = text.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        text.fontSize = Mathf.Max(text.fontSize, 24);
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

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
        if (target != null)
        {
            return target.GetComponent<Text>();
        }
        return null;
    }
}
