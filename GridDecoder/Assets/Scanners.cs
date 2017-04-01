using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;



public class Scanners : MonoBehaviour
{

	// webcam and scanner vars
	public static GameObject[,] scannersList;
	public GameObject _gridParent;
	public int _numOfScannersX;
	public int _numOfScannersY;
	private GameObject _scanner;
	RaycastHit hit;
	RenderTexture rTex;
	Texture2D _texture;
	GameObject keystonedQuad;

	public int _refreshRate = 10;
	public float _scannerScale = 0.5f;
	public bool _useWebcam;
	public bool _showRays = false;
	public float xOffset = 0;
	public float zOffset = 0;
	public bool refresh = false;

	// red, black, white, gray
	private Vector3[] colors = new Vector3[] { new Vector3(1f, 0f, 0f), new Vector3(1f, 1f, 1f), new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.5f, 0.5f)};


	IEnumerator Start ()
	{
		scannersList = new GameObject[_numOfScannersX, _numOfScannersY];
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

		_texture = new Texture2D (GetComponent<Renderer> ().material.mainTexture.width, 
			GetComponent<Renderer> ().material.mainTexture.height);
	
		while (true) {

			if (!refresh) {
				yield return new WaitForEndOfFrame ();
			}
			setTexture ();
			yield return new WaitForSeconds (_refreshRate);

			// Assign render texture from keystoned quad texture copy & copy it to a Texture2D
			Texture2D hitTex = assignRenderTexture();

			// Assign scanner colors
			for (int i = 0; i < _numOfScannersX; i++) {
				for (int j = 0; j < _numOfScannersY; j++) {
					if (Physics.Raycast (scannersList [i, j].transform.position, Vector3.down, out hit, 6)) {
						// Get local tex coords w.r.t. triangle

						if (!hitTex) {
							Debug.Log ("No hit texture");
							scannersList [i, j].GetComponent<Renderer> ().material.color = Color.magenta;
						} else {
							int _locX = Mathf.RoundToInt (hit.textureCoord.x * hitTex.width - xOffset);
							int _locY = Mathf.RoundToInt (hit.textureCoord.y * hitTex.height - zOffset); 
							Color pixel = hitTex.GetPixel (_locX, _locY);
							pixel = closestColor (pixel);

							//paint scanner with the found color 
							scannersList [i, j].GetComponent<Renderer> ().material.color = pixel;

							if (_showRays) {
								Debug.DrawLine (scannersList [i, j].transform.position, hit.point, pixel, 200, false);
								Debug.Log (hit.point);
							}
						}
					} else { 
						scannersList [i, j].GetComponent<Renderer> ().material.color = Color.magenta; //paint scanner with Out of bounds  color 
					}
				}
			}
		}
	}

	/// <summary>
	/// Assigns the render texture to a Texture2D.
	/// </summary>
	/// <returns>The render texture as Texture2D.</returns>
	private Texture2D assignRenderTexture() {
		RenderTexture rt = GameObject.Find ("KeystonedTextureQuad").transform.GetComponent<Renderer> ().material.mainTexture as RenderTexture;
		RenderTexture.active = rt;
		Texture2D hitTex = new Texture2D (rt.width, rt.height, TextureFormat.RGB24, false);
		hitTex.ReadPixels( new Rect(0, 0, rt.width, rt.height), 0, 0);
		return hitTex;
	}

	/// <summary>
	/// Sets the texture.
	/// </summary>
	private void setTexture() {
		if (_useWebcam) {
			_texture.SetPixels ((GetComponent<Renderer> ().material.mainTexture as WebCamTexture).GetPixels ()); //for webcam 
		}
		else {
			_texture.SetPixels ((GetComponent<Renderer> ().material.mainTexture as Texture2D).GetPixels ()); // for texture map 
		};
		_texture.Apply ();
	}

	/// <summary>
	/// Finds the closest color to the given scan colors.
	/// </summary>
	/// <returns>The closest color found.</returns>
	/// <param name="pixel">Pixel.</param>
	private Color closestColor(Color pixel) {
		Vector3 currPixel = new Vector3 (pixel.r, pixel.g, pixel.b);
		float minDistance = 1000;
		Vector3 minColor = new Vector3 (0f, 0f, 0f);

		for (int i = 0; i < colors.Count (); i++) {
			float currDistance = Vector3.Distance (colors [i], currPixel);
			if (currDistance < minDistance) {
				minDistance = currDistance;
				minColor = colors [i];
			}
		}

		Color closestColor = new Color (minColor.x, minColor.y, minColor.z);
		return closestColor;
	}

	/// <summary>
	/// Initialize scanners.
	/// </summary>
	private void scannersMaker ()
	{
		for (int x = 0; x < _numOfScannersX; x++) {
			for (int y = 0; y < _numOfScannersY; y++) {
				_scanner = GameObject.CreatePrimitive (PrimitiveType.Cube);
				_scanner.name = "grid_" + y + _numOfScannersX * x;
				_scanner.transform.localScale = new Vector3 (_scannerScale, _scannerScale, _scannerScale);  
				_scanner.transform.position = new Vector3 (x * _scannerScale * 2, 25, y * _scannerScale * 2);
				_scanner.transform.Rotate (90, 0, 0); 
				_scanner.transform.parent = _gridParent.transform;
				scannersList[x, y] = this._scanner;
			}
		}
		//scannersList = scannersList.OrderBy (i => i.name).ToList ();
	}


}