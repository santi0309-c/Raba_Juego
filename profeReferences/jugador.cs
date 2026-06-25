using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jugador : MonoBehaviour
{
    public Transform puntoRespawn;
    private int puntaje= 0;
    public int tam = 1;

    public float tiempoJuego = 30.0f;
    public float tiempo = 0.0f;
    
    //public int sumar
    public void afectar(int a) {
        if (tiempo < tiempoJuego) puntaje = puntaje + a;}

    // Start is called before the first frame update
    void Start()
    {
        
    }

 public void OnTriggerStay(Collider other){
        Debug.Log("Frutilla");
        GameObject objeto = other.gameObject;
        if (objeto.tag == "Pelota"){
              if (Input.GetKey(KeyCode.K)) {Debug.Log("kkk");};
        }
 }
 


    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < -10 ) { transform.position = puntoRespawn.position;}

        tiempo += Time.deltaTime;
        Debug.Log(puntaje);
    }

    private void OnGUI(){
        GUI.contentColor = Color.black;  
        if (tiempo < tiempoJuego) {
            if(puntaje < 25) {
                GUI.Label(new Rect(100, 10, 90, 40), "Puntaje: " + puntaje);     
                GUI.Label(new Rect(300, 10, 90, 40), "Tiempo: " + tiempo);       
                }
            else { GUI.Label(new Rect(100, 10, 90, 40), "Ganaste");}
        }
        else {
               if (puntaje < 25) { GUI.Label(new Rect(100, 10, 90, 40), "Perdiste");}
               else  { GUI.Label(new Rect(100, 10, 90, 40), "Ganaste");}
              }
     }
        
        //if (tiempo < limiteDeTiempo)
       // {
        //    GUI.Label(new Rect(100, 10, 90, 40), "Puntaje: " + puntajeJugador);


 }



