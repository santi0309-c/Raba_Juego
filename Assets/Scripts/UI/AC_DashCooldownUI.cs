using UnityEngine;
using UnityEngine.UI;

public class AC_DashCooldownUI : MonoBehaviour
{
    public AC_PlayerController player1;
    public AC_PlayerController player2;

    public Text dashTextP1;
    public Text dashTextP2;

    public Color readyColor = Color.green;
    public Color cooldownColor = Color.yellow;

    private bool autoWired;

    private void Start()
    {
        AutoWireIfNeeded();
        autoWired = true;
    }

    private void Update()
    {
        if (!autoWired)
        {
            AutoWireIfNeeded();
        }
        UpdateDashText(player1, dashTextP1);
        UpdateDashText(player2, dashTextP2);
    }

    private void UpdateDashText(AC_PlayerController player, Text label)
    {
        if (player == null || label == null) return;

        float remaining = player.DashCooldownRemaining;

        string blockText;
        if (player.IsBlocking)
        {
            blockText = " | BLOQUEANDO";
        }
        else
        {
            blockText = " | LIBRE";
        }

        if (remaining <= 0f)
        {
            label.text = "DASH: LISTO" + blockText;
            label.color = readyColor;
        }
        else
        {
            label.text = "DASH: " + remaining.ToString("F1") + "s" + blockText;
            label.color = cooldownColor;
        }
    }

    private void AutoWireIfNeeded()
    {
        if (player1 == null && AC_GameManager.Instance != null)
        {
            player1 = AC_GameManager.Instance.player1;
        }

        if (player2 == null && AC_GameManager.Instance != null)
        {
            player2 = AC_GameManager.Instance.player2;
        }

        if (dashTextP1 == null)
        {
            GameObject textObject = GameObject.Find("DashTextP1");
            if (textObject != null)
            {
                dashTextP1 = textObject.GetComponent<Text>();
            }
        }

        if (dashTextP2 == null)
        {
            GameObject textObject = GameObject.Find("DashTextP2");
            if (textObject != null)
            {
                dashTextP2 = textObject.GetComponent<Text>();
            }
        }
    }
}
