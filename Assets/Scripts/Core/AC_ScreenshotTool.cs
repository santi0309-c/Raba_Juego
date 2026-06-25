using UnityEngine;

/// <summary>
/// Presiona F12 para guardar un screenshot en la carpeta Screenshots/.
/// </summary>
public class AC_ScreenshotTool : MonoBehaviour
{
    [Header("Tecla para capturar")]
    public KeyCode teclaScreenshot = KeyCode.F12;

    [Header("Multiplicador de resolución")]
    [Range(1, 4)]
    public int superSize = 1;

    private void Update()
    {
        if (Input.GetKeyDown(teclaScreenshot))
        {
            string carpeta = System.IO.Path.Combine(Application.dataPath, "../Screenshots/");
            System.IO.Directory.CreateDirectory(carpeta);

            string nombre = "raba_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            string ruta = System.IO.Path.Combine(carpeta, nombre);

            ScreenCapture.CaptureScreenshot(ruta, superSize);
            Debug.Log("[Screenshot] Guardado: " + ruta);
        }
    }
}
