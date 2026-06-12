using UnityEngine;

public class AC_SceneDecorator : MonoBehaviour
{
    private AC_GameManager gameManager;
    private Transform spawn1;
    private Transform spawn2;
    private Transform arenaCenter;
    private TextMesh player1Sign;
    private TextMesh player2Sign;
    private TextMesh centerSign;

    public void Configure(AC_GameManager manager, Transform firstSpawn, Transform secondSpawn, Transform center)
    {
        gameManager = manager;
        spawn1 = firstSpawn;
        spawn2 = secondSpawn;
        arenaCenter = center;
        EnsureSigns();
        RefreshStaticSigns();
    }

    private void LateUpdate()
    {
        if (gameManager == null)
        {
            return;
        }

        EnsureSigns();
        RefreshDynamicSign();
        FaceCamera(player1Sign);
        FaceCamera(player2Sign);
        FaceCamera(centerSign);
    }

    private void EnsureSigns()
    {
        if (player1Sign == null)
        {
            player1Sign = CreateSign("P1_Control_Sign", new Color(0.32f, 0.7f, 1f));
        }

        if (player2Sign == null)
        {
            player2Sign = CreateSign("P2_Control_Sign", new Color(1f, 0.48f, 0.48f));
        }

        if (centerSign == null)
        {
            centerSign = CreateSign("Arena_Info_Sign", new Color(1f, 0.92f, 0.45f));
        }
    }

    private TextMesh CreateSign(string objectName, Color color)
    {
        GameObject signObject = new GameObject(objectName);
        signObject.transform.SetParent(null);

        TextMesh mesh = signObject.AddComponent<TextMesh>();
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 64;
        mesh.characterSize = 0.08f;
        mesh.color = color;
        return mesh;
    }

    private void RefreshStaticSigns()
    {
        if (player1Sign != null && spawn1 != null && gameManager != null)
        {
            player1Sign.text = gameManager.GetControlSignText(1);
            player1Sign.transform.position = spawn1.position + new Vector3(-1.4f, 2.2f, 0f);
        }

        if (player2Sign != null && spawn2 != null && gameManager != null)
        {
            player2Sign.text = gameManager.GetControlSignText(2);
            player2Sign.transform.position = spawn2.position + new Vector3(1.4f, 2.2f, 0f);
        }

        if (centerSign != null && arenaCenter != null)
        {
            centerSign.transform.position = arenaCenter.position + new Vector3(0f, 3f, 0f);
        }
    }

    private void RefreshDynamicSign()
    {
        if (centerSign == null || gameManager == null)
        {
            return;
        }

        if (arenaCenter != null)
        {
            centerSign.transform.position = arenaCenter.position + new Vector3(0f, 3f, 0f);
        }

        centerSign.text = gameManager.GetArenaBillboardText();
    }

    private void FaceCamera(TextMesh sign)
    {
        if (sign == null || Camera.main == null)
        {
            return;
        }

        Vector3 direction = sign.transform.position - Camera.main.transform.position;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        sign.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
