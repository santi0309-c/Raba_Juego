using UnityEngine;

public class AC_ScreenshotTool : MonoBehaviour
{
    public KeyCode teclaScreenshot = KeyCode.F12;
    public int superSize = 1;

    private void Update()
    {
        if (Input.GetKeyDown(teclaScreenshot))
        {
            string carpeta = Application.dataPath + "/../Screenshots/";
            System.IO.Directory.CreateDirectory(carpeta);

            string nombre = "raba_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            string ruta = carpeta + nombre;

            ScreenCapture.CaptureScreenshot(ruta, superSize);
            Debug.Log("[Screenshot] Guardado: " + ruta);
        }
    }
}
