using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


[System.Serializable]
public class ColorSettings {
	// Color sample objects
	public Vector3[] position = new Vector3[4];
	public float scannerScale;
}

public class Scanners : MonoBehaviour
{
	// webcam and scanner vars
	public static GameObject[,] scannersList;
	public static int[,] currentIds;

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
	public bool _debug = true;
	public bool _isCalibrating;
	public int _gridSize = 2; // i.e. 2x2 reading for one cell

	private bool setup = true;

	// Color calibration
	ColorSettings colorSettings = new ColorSettings();
	GameObject[] sampleCubes;
	private string colorRedName = "Sample red";
	private string colorBlackName = "Sample black";
	private string colorWhiteName = "Sample white";
	private string colorGrayName = "Sample gray";
	private string colorTexturedQuadName = "KeystonedTextureQuad";

	public string _colorSettingsFileName = "_sampleColorSettings.json";

	// red, black, white, gray
	// 0 - white
	// 1 - black
	// 2 - red
	// 3 - unknown / gray
	private Vector3[] sampledColors = new Vector3[4];
	private Texture2D hitTex;

	enum Brick { RL = 0, RM = 1, RS = 2, OL = 3, OM = 4, OS = 5, ROAD = 6 };

	private Dictionary<string, Brick> idList = new Dictionary<string, Brick>
	{
		{ "2000", Brick.RL },
		{ "2010", Brick.RM }, 
		{ "2001", Brick.RS },
		{ "2100", Brick.OL }, 
		{ "2011", Brick.OM },
		{ "2110", Brick.OS },
		{ "2101", Brick.ROAD }
	};

	IEnumerator Start ()
	{
		initVariables ();
		EventManager.StartListening ("reload", OnReload);
	
		while (true) {
			if (!refresh)
				yield return new WaitForEndOfFrame ();
			SetTexture ();
			yield return new WaitForSeconds (_refreshRate);

			// Assign render texture from keystoned quad texture copy & copy it to a Texture2D
			AssignRenderTexture();

			if (_isCalibrating) {
				CalibrateColors ();
			} 
			else {
				// Assign scanner colors
				ScanColors();

				if (_debug)
					PrintMatrix ();
			}
			if (setup)
				setup = false;
		}
	}

	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update() {
		onKeyPressed();
	}

	/// <summary>
	/// Initializes the variables.
	/// </summary>
	private void initVariables() {
		scannersList = new GameObject[_numOfScannersX, _numOfScannersY];
		currentIds = new int[_numOfScannersX / _gridSize, _numOfScannersY / _gridSize];
		MakeScanners ();

		// Find copy mesh with RenderTexture
		keystonedQuad = GameObject.Find (colorTexturedQuadName);
		if (!keystonedQuad)
			Debug.Log ("Keystoned quad not found.");

		_texture = new Texture2D (GetComponent<Renderer> ().material.mainTexture.width, 
			GetComponent<Renderer> ().material.mainTexture.height);
	}

	/// <summary>
	/// Calibrates the colors based on sample points.
	/// </summary>
	private void CalibrateColors() {
		sampleCubes = new GameObject[4];
		sampleCubes[0] = GameObject.Find (colorWhiteName);
		sampleCubes[1] = GameObject.Find (colorBlackName);
		sampleCubes[2] = GameObject.Find (colorRedName);
		sampleCubes[3] = GameObject.Find (colorGrayName);

		for (int i = 0; i < sampleCubes.Length; i++) {
			if (setup) { 
				sampleCubes [i].transform.localScale = new Vector3 (_scannerScale, _scannerScale, _scannerScale); 
				sampleCubes [i].transform.position = new Vector3(sampleCubes [i].transform.position.x, keystonedQuad.transform.position.y + 0.2f, sampleCubes [i].transform.position.z);
			}
			if (Physics.Raycast (sampleCubes[i].transform.position, Vector3.down, out hit, 6)) {
				int _locX = Mathf.RoundToInt (hit.textureCoord.x * hitTex.width);
				int _locY = Mathf.RoundToInt (hit.textureCoord.y * hitTex.height); 
				Color pixel = hitTex.GetPixel (_locX, _locY);
				sampleCubes [i].GetComponent<Renderer> ().material.color = pixel;
				sampledColors[i] =  new Vector3 (pixel.r, pixel.g, pixel.b);
			}
		}
	}

	/// <summary>
	/// Scans the colors.
	/// </summary>
	private void ScanColors() {
		for (int i = 0; i < _numOfScannersX; i += _gridSize) {
			for (int j = 0; j < _numOfScannersY; j += _gridSize) {
				string key = "";

				for (int k = 0; k < _gridSize; k++) {
					for (int m = 0; m < _gridSize; m++) {
						key += findColor (i + k, j + m); 
					}
				} 

				// keys read counterclockwise
				key = new string(key.ToCharArray().Reverse().ToArray());

				if (idList.ContainsKey (key)) {
					currentIds [i / _gridSize, j / _gridSize] = (int)idList [key];
				} else { // check rotation independence
					bool isRotation = false;
					string keyConcat = key + key;
					foreach(string idKey in idList.Keys) {
						if (keyConcat.Contains (idKey)) {
							currentIds [i / _gridSize, j / _gridSize] = (int)idList [idKey];
							isRotation = true;
							break;
						}
					}
					if (!isRotation)
						currentIds [i / _gridSize, j / _gridSize] = -1;
				}
			}
		}
	}



	/// <summary>
	/// Prints the ID matrix.
	/// </summary>
	private void PrintMatrix() {
		string matrix = "";

		if ((int)(currentIds.Length) <= 1) {
			Debug.Log ("Empty dictionary.");
			return;
		}
		for (int i = 0; i < currentIds.GetLength(0); i++) {
			for (int j = 0; j < currentIds.GetLength(1); j++) {
				matrix += currentIds [i, j] + "";
			}
			matrix += "\n";
		}
		Debug.Log (matrix);
	}

	/// <summary>
	/// Finds the color below scanner item[i, j].
	/// </summary>
	/// <param name="i">The row index.</param>
	/// <param name="j">The column index.</param>
	private int findColor(int i, int j) {
		if (Physics.Raycast (scannersList [i, j].transform.position, Vector3.down, out hit, 6)) {
			// Get local tex coords w.r.t. triangle

			if (!hitTex) {
				Debug.Log ("No hit texture");
				scannersList [i, j].GetComponent<Renderer> ().material.color = Color.magenta;
				return -1;
			} else {
				int _locX = Mathf.RoundToInt (hit.textureCoord.x * hitTex.width);
				int _locY = Mathf.RoundToInt (hit.textureCoord.y * hitTex.height); 
				Color pixel = hitTex.GetPixel (_locX, _locY);
				int pixelID = ClosestColor (pixel);
				Color currPixel = new Color(sampledColors [pixelID].x, sampledColors [pixelID].y, sampledColors [pixelID].z);

				//paint scanner with the found color 
				scannersList [i, j].GetComponent<Renderer> ().material.color = currPixel;

				if (_showRays) {
					Debug.DrawLine (scannersList [i, j].transform.position, hit.point, pixel, 200, false);
					Debug.Log (hit.point);
				}
				return pixelID;
			}
		} else { 
			scannersList [i, j].GetComponent<Renderer> ().material.color = Color.magenta; //paint scanner with Out of bounds  color 
			return -1;
		}
	}

	/// <summary>
	/// Assigns the render texture to a Texture2D.
	/// </summary>
	/// <returns>The render texture as Texture2D.</returns>
	private void AssignRenderTexture() {
		RenderTexture rt = GameObject.Find (colorTexturedQuadName).transform.GetComponent<Renderer> ().material.mainTexture as RenderTexture;
		RenderTexture.active = rt;
		hitTex = new Texture2D (rt.width, rt.height, TextureFormat.RGB24, false);
		hitTex.ReadPixels( new Rect(0, 0, rt.width, rt.height), 0, 0);
	}

	/// <summary>
	/// Sets the texture.
	/// </summary>
	private void SetTexture() {
		if (_useWebcam) {
			if (Webcam.isPlaying())
          {
                _texture.SetPixels((GetComponent<Renderer>().material.mainTexture as WebCamTexture).GetPixels()); //for webcam 
          }
          else return;
		}
		else {
			_texture.SetPixels ((GetComponent<Renderer> ().material.mainTexture as Texture2D).GetPixels ()); // for texture map 
		};
		_texture.Apply ();
	}

	/// <summary>
	/// Finds the closest color to the given scan colors.
	/// </summary>
	/// <returns>The closest color's index in the colors array.</returns>
	/// <param name="pixel">Pixel.</param>
	private int ClosestColor(Color pixel) {
		Vector3 currPixel = new Vector3 (pixel.r, pixel.g, pixel.b);
		double minDistance = Double.PositiveInfinity;
		int minColor = -1;

		for (int i = 0; i < sampledColors.Length; i++) {
			double currDistance = Vector3.Distance (sampledColors [i], currPixel);
			if (currDistance < minDistance) {
				minDistance = currDistance;
				minColor = i;
			}
		}
		return minColor;
	}

	/// <summary>
	/// Initialize scanners.
	/// </summary>
	private void MakeScanners ()
	{
		for (int x = 0; x < _numOfScannersX; x++) {
			for (int y = 0; y < _numOfScannersY; y++) {
				_scanner = GameObject.CreatePrimitive (PrimitiveType.Cube);
				_scanner.name = "grid_" + y + _numOfScannersX * x;
				_scanner.transform.localScale = new Vector3 (_scannerScale, _scannerScale, _scannerScale);  
				_scanner.transform.position = new Vector3 (x * _scannerScale * 2, GameObject.Find (colorTexturedQuadName).transform.position.y + 0.1f, y * _scannerScale * 2);
				_scanner.transform.Rotate (90, 0, 0); 
				_scanner.transform.parent = _gridParent.transform;
				scannersList[x, y] = this._scanner;
			}
		}
	}

	/// <summary>
	/// Loads the color sampler objects from a JSON.
	/// </summary>
	private void LoadSamplers() {
		if (_debug)
			Debug.Log ("Loading color sampling settings.");

		string dataAsJson = JsonParser.loadJSON (_colorSettingsFileName, _debug);
		colorSettings = JsonUtility.FromJson<ColorSettings>(dataAsJson);

		for (int i = 0; i < sampleCubes.Length; i++) {
			sampleCubes [i].transform.position = colorSettings.position [i];
			sampleCubes[i].transform.localScale = new Vector3 (colorSettings.scannerScale, colorSettings.scannerScale, colorSettings.scannerScale);
		}
	}

	/// <summary>
	/// Saves the color sampler objects to a JSON.
	/// </summary>
	private void SaveSamplers() {
		if (_debug)
			Debug.Log ("Saving color sampling settings.");

		colorSettings.scannerScale = _scannerScale;

		for (int i = 0; i < sampleCubes.Length; i++) {
			colorSettings.position [i] = sampleCubes [i].transform.position;
		}

		string dataAsJson = JsonUtility.ToJson (colorSettings);
		JsonParser.writeJSON (_colorSettingsFileName, dataAsJson);
	}

	/// <summary>
	/// Raises the scene control event.
	/// </summary>
	private void onKeyPressed ()
	{
		if (Input.GetKey (KeyCode.S)) {
			Debug.Log ("Key pressed to save color settings.");
			SaveSamplers ();
		} else if (Input.GetKey (KeyCode.L)) {
			Debug.Log ("Key pressed to load color settings.");
			LoadSamplers ();
		}
	}

	/// <summary>
	/// Reloads configuration / keystone settings when the scene is refreshed.
	/// </summary>
	void OnReload() {
		Debug.Log ("Color config was reloaded!");

		LoadSamplers ();
	}

}