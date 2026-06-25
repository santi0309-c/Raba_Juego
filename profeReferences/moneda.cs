using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moneda : MonoBehaviour
{
	public int valorMoneda=5;

	public void OnTriggerEnter(Collider otro){
		if(otro.gameObject.name == "pepe"){
			otro.GetComponent<control>().incremento(valorMoneda);
			Destroy(gameObject);
		}
	
	}
    
}





/*
	public int puntaje=0; // variable puntaje valores enteros
	public float tiempoJuego = 30.0f; // tipo flotante (números decimales)
	public bool reiniciarJuego = false; // tipo booleano true false
	public string nombreJugador = "Pepe"; // Cadena de caracteres
	public char letraJugador = 'A'; // un solo caracter entre comillas simples
*/