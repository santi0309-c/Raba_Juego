using UnityEngine;

public class portal : MonoBehaviour
{

	public Transform destino;


	void OnCollisionEnter (Collision otro){
         //    if(otro.tag == "jugador") 
         otro.gameObject.transform.position = destino.position + new Vector3(0.5f,0,0);
	}

   }
