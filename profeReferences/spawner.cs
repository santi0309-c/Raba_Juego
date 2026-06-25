using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawner : MonoBehaviour
{
    public Transform respawn;
    public int posicionEjeY = -20;
    void Update()
    {
        if(transform.position.y < posicionEjeY) {
            if(tag == "Player") GetComponent<control>().vidasJugador -= 1;
            transform.position = respawn.position;
            GetComponent<Rigidbody>().linearVelocity = new Vector3(0,0,0);
            GetComponent<Rigidbody>().angularVelocity = new Vector3(0,0,0);
        }

        if (Input.GetKeyUp(KeyCode.B)) transform.position = respawn.position;
    }
}
