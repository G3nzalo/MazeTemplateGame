using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShoot : MonoBehaviour
{
    // Asigna una tecla para capturar la pantalla
    public KeyCode screenshotKey = KeyCode.P;

    // Factor de supermuestreo para aumentar la resolución de la captura (1 = resolución actual, 2 = el doble de la resolución, etc.)
    public int superSize = 1;

    void Update()
    {
        // Captura la pantalla cuando se presiona la tecla asignada
        if (Input.GetKeyDown(screenshotKey))
        {
            CaptureScreenshot();
        }
    }

    void CaptureScreenshot()
    {
        // Define el nombre del archivo y la ruta donde se guardará la captura
        string fileName = "screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
        string filePath = System.IO.Path.Combine(Application.dataPath, fileName);

        // Captura la pantalla con la resolución ajustada por el superSize y guarda la imagen en la ruta especificada
        ScreenCapture.CaptureScreenshot(filePath, superSize);
        Debug.Log("Screenshot saved to: " + filePath);
    }
}
