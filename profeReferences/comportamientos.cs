using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class comportamientos : MonoBehaviour
{
    public void Cargarscena(string nombre) {
        SceneManager.LoadScene(nombre);
    }

    public void Salir() {
        Debug.Log("sali del juego");
        Application.Quit();
    }
}
