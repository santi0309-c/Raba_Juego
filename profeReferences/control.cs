using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class control : MonoBehaviour
{
	public int puntajeJugador = 0;
	public float limiteDeTiempo = 10.0f;
	public float tiempo = 0.0f;
	public int vidasJugador=1;


	public void incremento(int valor){
		if(tiempo<limiteDeTiempo) {puntajeJugador += valor;}
	}

    // Update is called once per frame
    void Update()
    {
		tiempo = tiempo + Time.deltaTime;
		Debug.Log(puntajeJugador);
    }


	private void OnGUI(){
		GUI.contentColor = Color.black;
		if(tiempo<limiteDeTiempo){
		        GUI.Label (new Rect (100, 10, 90, 40), ("Puntaje: " + puntajeJugador));
		        GUI.Label (new Rect (250, 10, 90, 40), ("Tiempo: " + tiempo));
		}
		else {
			if(puntajeJugador < 5){
				GUI.Label (new Rect (100, 10, 90, 40), "PERDISTE");
			}
			else{
				GUI.Label (new Rect (100, 10, 90, 40), "GANASTE");
			}
		}
	}
}
