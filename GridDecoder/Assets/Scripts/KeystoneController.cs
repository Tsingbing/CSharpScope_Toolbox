using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class KeystoneSettings {
	
	// Quad control variables
	public Vector3[] vertices;
	public GameObject[] corners;
	public int selectedCorner;

	public KeystoneSettings(Vector3[] newVertices, GameObject[] newCorners) {
		this.vertices = newVertices;
		this.corners = newCorners;
	}
}

public class KeystoneController : MonoBehaviour
{

	KeystoneSettings settings;

	public Vector3[] _vertices;
	private GameObject[] _corners;

	public string _settingsFileName = "keystoneSettings.json";

	public bool _useKeystone = true;
	public bool _debug = false;

	private float speed = 0.5f;

	void Start ()
	{
		settings = new KeystoneSettings(_vertices, _corners);
		Destroy (this.GetComponent <MeshCollider> ()); //destroy so we can make one in dynamically 
		transform.gameObject.AddComponent <MeshCollider> (); //add new collider 
	
		Mesh mesh = GetComponent<MeshFilter> ().mesh; // get this GO mesh
		mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };	

		cornerMaker (); //make the corners for visual controls 
	}

	void Update ()
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		mesh.vertices = settings.vertices;
		 
		// Zero out the left and bottom edges, 
		// leaving a right trapezoid with two sides on the axes and a vertex at the origin.
		var shiftedPositions = new Vector2[] {
			Vector2.zero,
			new Vector2 (0, settings.vertices [1].y - settings.vertices [0].y),
			new Vector2 (settings.vertices [2].x - settings.vertices [1].x, settings.vertices [2].y - settings.vertices [3].y),
			new Vector2 (settings.vertices [3].x - settings.vertices [0].x, 0)
		};
		mesh.uv = shiftedPositions;

		var widths_heights = new Vector2[4];
		widths_heights [0].x = widths_heights [3].x = shiftedPositions [3].x;
		widths_heights [1].x = widths_heights [2].x = shiftedPositions [2].x;
		widths_heights [0].y = widths_heights [1].y = shiftedPositions [1].y;
		widths_heights [2].y = widths_heights [3].y = shiftedPositions [2].y;
		mesh.uv2 = widths_heights;

		onOffObjects (_useKeystone); // toggles onoff at each click
		if (_useKeystone) {
			OnSceneControl ();
		}

		transform.GetComponent<MeshCollider> ().sharedMesh = mesh;// make new collider based on updated mesh 
	}

	/// Methods section 

	private void cornerMaker ()
	{

		settings.corners = new GameObject[settings.vertices.Length]; // make corners spheres 
		for (int i = 0; i < settings.vertices.Length; i++) {
			var obj = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			obj.transform.SetParent (transform);
			obj.GetComponent<Renderer> ().material.color = i == settings.selectedCorner ? Color.green : Color.red;
			settings.corners [i] = obj;
		}
	}


	private void onOffObjects (bool visible)
	{

		for (int i = 0; i < settings.vertices.Length; i++) {
			GameObject sphere = settings.corners [i];
			sphere.transform.position = transform.TransformPoint (settings.vertices [i]);
			sphere.SetActive (visible);
		}
	}

	private void OnSceneControl ()
	{
		if (!_useKeystone)
			return;

		if (Input.anyKey && _debug)
			Debug.Log("Current input is " + Input.inputString);

		if (Input.GetKey (KeyCode.S)) {
			saveSettings ();
			return;
		} else if (Input.GetKey (KeyCode.L)) {
			loadSettings ();
			return;
		} else {
			updateSelection ();
		}
	}

	/// <summary>
	/// Saves the settings to a JSON.
	/// </summary>
	/// <returns><c>true</c>, if settings were saved, <c>false</c> otherwise.</returns>
	private bool saveSettings() {
		if (_debug)
			Debug.Log ("Saving settings.");

		if (!writeJSON ())
			return false;
		return true;
	}

	/// <summary>
	/// Loads the settings.
	/// </summary>
	/// <returns><c>true</c>, if settings were loaded, <c>false</c> otherwise.</returns>
	private bool loadSettings() {
		if (_debug)
			Debug.Log ("Loading settings.");

		if (!loadJSON ())
			return false;
		return true;
	}

	/// <summary>
	/// Updates the selection for each keypress event.
	/// </summary>
	private void updateSelection() {
		var corner = Input.GetKeyDown ("1") ? 0 : (Input.GetKeyDown ("2") ? 1 : (Input.GetKeyDown ("3") ? 2 : (Input.GetKeyDown ("4") ? 3 : settings.selectedCorner)));
		if (corner != settings.selectedCorner) {
			settings.corners [settings.selectedCorner].GetComponent<Renderer> ().material.color = Color.red;
			settings.corners [corner].GetComponent<Renderer> ().material.color = Color.green;
			settings.selectedCorner = corner;
			if (_debug) 
				Debug.Log ("Selection changed to " + settings.selectedCorner.ToString ());
		}

		if (Input.GetKey (KeyCode.LeftShift))
			speed *= 10;
		else if (Input.GetKey (KeyCode.LeftControl) && Input.GetKey (KeyCode.LeftAlt))
			speed *= 0.1f;
		else if (Input.GetKey (KeyCode.LeftAlt))
			speed *= 0.01f; 

		var v = settings.vertices [settings.selectedCorner];

		if (Input.GetKeyDown (KeyCode.UpArrow))
			v = v + speed * Vector3.up;
		else if (Input.GetKeyDown (KeyCode.DownArrow))
			v = v + speed * Vector3.down;
		else if (Input.GetKeyDown (KeyCode.LeftArrow))
			v = v + speed * Vector3.left;
		else if (Input.GetKeyDown (KeyCode.RightArrow))
			v = v + speed * Vector3.right;

		settings.vertices [settings.selectedCorner] = v;
	}

	/// <summary>
	/// Loads a JSON file from fileName.
	/// Following https://unity3d.com/learn/tutorials/topics/scripting/loading-game-data-json
	/// </summary>
	/// <returns><c>true</c>, if JSO was loaded, <c>false</c> otherwise.</returns>
	/// <param name="fileName">File name.</param>
	public bool loadJSON() {
		string filePath = Path.Combine(Application.streamingAssetsPath, _settingsFileName);

		if (File.Exists(filePath))
		{
			// Read the json from the file into a string
			string dataAsJson = File.ReadAllText(filePath); 
			settings = JsonUtility.FromJson<KeystoneSettings>(dataAsJson);

			if (_debug) 
				Debug.Log("Keystone data loaded " + settings);
			return true;
		}
		else
		{
			Debug.LogError("Cannot load game data!");
			return false;
		}
	}

	/// <summary>
	/// Writes the JSON
	/// </summary>
	/// <returns><c>true</c>, if JSO was writed, <c>false</c> otherwise.</returns>
	public bool writeJSON() {
		string dataAsJson = JsonUtility.ToJson (settings);

		string filePath = Application.dataPath + _settingsFileName;
		File.WriteAllText (filePath, dataAsJson);

		return true;
	}

}



