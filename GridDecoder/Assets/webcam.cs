using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class webcam : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
		WebCamTexture webcam = new WebCamTexture (WebCamTexture.devices [0].name); //SET up the cam

//		webcam.requestedFPS = 15;  
//		webcam.requestedHeight = 800;  
//		webcam.requestedWidth = 800;  
//
		webcam.Play (); // play camera
		Renderer renderer = GetComponent<Renderer> ();
		renderer.material.mainTexture = webcam; //put cam tex onto quad

	}

}
