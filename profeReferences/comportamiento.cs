using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class comportamiento : MonoBehaviour
{
    public int valor = 5;
    public float tiempoJuego = 10.0f;
    public bool subiendo = true;
    public char Letras = 'a';
    public string palabra = "hola";
    public Vector3 nuevaPosicion;
    public Transform objeto;
    public GameObject cosa;
    private float mover = 0.2f;
    private int j;

    // Start is called before the first frame update
    void Start()
    {
        nuevaPosicion = new Vector3(2, 2, 2);
        transform.position = nuevaPosicion;
        for (j=0;j <10 ;j++ ) {
            Debug.Log("hola");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("Espacio");
        }

        if (Input.GetKey(KeyCode.A)) {
            Debug.Log("AAA");
        }

        if (Input.GetKeyUp(KeyCode.L)) {
            Debug.Log("LLL");
        }
        transform.position = new Vector3(nuevaPosicion.x, mover, nuevaPosicion.z);
        if (subiendo == true)  {
            mover += 0.2f;
            if (mover > 10) { subiendo = false; }
        }
        else if(subiendo == false){
             mover -= 0.2f;
            if (mover < 0) { subiendo = true; }
        }

    }
}
