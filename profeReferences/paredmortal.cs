using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class paredmortal : MonoBehaviour
{

	public Transform respawn;

	void OnCollisionEnter (Collision otro){
		otro.gameObject.transform.position = respawn.position;
	}
	// Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
