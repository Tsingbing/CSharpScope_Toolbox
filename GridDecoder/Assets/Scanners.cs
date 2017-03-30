using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;



public class Scanners : MonoBehaviour
{

	// webcam and scanner vars
	public static List<GameObject> scannersList = new List<GameObject> ();
	public GameObject _gridParent;
	public int _numOfScannersX;
	public int _numOfScannersY;
	private GameObject _scanner;
	RaycastHit hit;
	RenderTexture rTex;
	Texture2D _texture;
	GameObject keystonedQuad;

	public int _refreshRate = 1;
	public float _scannerScale = 0.5f;
	public bool _useWebcam;
	public bool _showRays = false;
	public float xOffset = 0;
	public float zOffset = 0;


	IEnumerator Start ()
	{
		scannersMaker ();

		keystonedQuad = GameObject.Find ("KeystonedTextureQuad");
		if (!keystonedQuad) {
			Debug.Log ("Keystoned quad not found.");
		}
		else {
			Debug.Log ("Keystoned quad's position: " + keystonedQuad.transform.position.x);
			Debug.Log ("Grid position: " + _gridParent.transform.position.x);
//			xOffset = keystonedQuad.transform.position.x - _gridParent.transform.position.x;
//			zOffset = keystonedQuad.transform.position.z - _gridParent.transform.position.z;
			Debug.Log ("X offset is " + xOffset);
			Debug.Log ("Z offset is " + zOffset);
		}

	
		while (true) {

			yield return new WaitForEndOfFrame ();

			_texture = new Texture2D (GetComponent<Renderer> ().material.mainTexture.width, 
				                     GetComponent<Renderer> ().material.mainTexture.height);

			if (_useWebcam) {
				_texture.SetPixels ((GetComponent<Renderer> ().material.mainTexture as WebCamTexture).GetPixels ()); //for webcam 
			}
			else {
				_texture.SetPixels ((GetComponent<Renderer> ().material.mainTexture as Texture2D).GetPixels ()); // for texture map 
			};
				
			_texture.Apply (); //need fixing!!!

			yield return new WaitForSeconds (_refreshRate);

			for (int i = 0; i < scannersList.Count; i++) {
				if (Physics.Raycast (scannersList [i].transform.position, Vector3.down, out hit, 6)) {
						// Get local tex coords w.r.t. triangle

					int _locX = Mathf.RoundToInt (hit.textureCoord.x * _texture.width - xOffset);
					int _locY = Mathf.RoundToInt (hit.textureCoord.y * _texture.height - zOffset); 

					RenderTexture rt = hit.transform.GetComponent<Renderer> ().material.mainTexture as RenderTexture;
					RenderTexture.active = rt;
					Texture2D hitTex = new Texture2D (rt.width, rt.height, TextureFormat.RGB24, false);

					hitTex.ReadPixels( new Rect(0, 0, rt.width, rt.height), 0, 0);

					if (!hitTex) {
						Debug.Log ("No hit texture");
						scannersList [i].GetComponent<Renderer> ().material.color = Color.magenta;
					}
					else {
						Color pixel = hitTex.GetPixel (_locX, _locY); 
						scannersList [i].GetComponent<Renderer> ().material.color = pixel; //paint scanner with the found color 
						if (_showRays) {
							Debug.DrawLine (scannersList [i].transform.position, hit.point, pixel, 200, false);
							Debug.Log (hit.point);
						}
					}

				} else { 
					scannersList [i].GetComponent<Renderer> ().material.color = Color.magenta; //paint scanner with Out of bounds  color 
				}
			}
		}
	}

	private void scannersMaker ()
	{
		for (int x = 0; x < _numOfScannersX; x++) {
			for (int y = 0; y < _numOfScannersY; y++) {
				_scanner = GameObject.CreatePrimitive (PrimitiveType.Cube);
				_scanner.name = "grid_" + scannersList.Count;
				_scanner.transform.localScale = new Vector3 (_scannerScale, _scannerScale, _scannerScale);  
				_scanner.transform.position = new Vector3 (x * _scannerScale * 2, 25, y * _scannerScale * 2);
				_scanner.transform.Rotate (90, 0, 0); 
				_scanner.transform.parent = _gridParent.transform;
				scannersList.Add (this._scanner);
			}
		}
		scannersList = scannersList.OrderBy (i => i.name).ToList ();
	}


}