using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Keystone settings (loaded from JSON).
/// </summary>
[System.Serializable]
public class KeystoneSettings {
	
	// Quad control variables
	public Vector3[] vertices = new Vector3[4];

	public KeystoneSettings(Vector3[] newVertices) {
		this.vertices = newVertices;
	}
}

public class KeystoneController : MonoBehaviour
{

	KeystoneSettings settings;

	public Vector3[] _vertices;
	private Vector3[] vertices;
	private GameObject[] _corners;
	public int selectedCorner;
	private Mesh mesh;
	private Vector2[] widths_heights = new Vector2[4];
	private bool needUpdate = true;

	public string _settingsFileName = "_keystoneSettings.json";

	public bool _useKeystone = true;
	public bool _debug = false;
	private float speed = 0.001f;

	private float q0; 
	private float q1; 
	private float q2;
	private float q3;

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start ()
	{
		EventManager.StartListening ("reload", OnReloadKeystone);

		Destroy (this.GetComponent <MeshCollider> ()); //destroy so we can make one in dynamically 
		transform.gameObject.AddComponent <MeshCollider> (); //add new collider 
	
		mesh = GetComponent<MeshFilter> ().mesh; // get this GO mesh
		mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };	
		LoadSettings ();

		CornerMaker (); //make the corners for visual controls 
	}

	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update ()
	{
		if (_useKeystone) {
			OnSceneControl ();

			GetHomogeneousCoords ();

			if (needUpdate) {
				SetupMesh ();
				needUpdate = false;
			}
		}
			
		onOffObjects (_useKeystone); // toggles onoff at each click
	}

	/// <summary>
	/// Reinitializes the mesh.
	/// </summary>
	private void SetupMesh() {
		mesh.vertices = vertices;

		// Zero out the left and bottom edges, 
		// leaving a right trapezoid with two sides on the axes and a vertex at the origin.

		q0 = 1;
		q1 = (vertices [1].y - vertices [0].y);
		q2 = (vertices [2].x - vertices [1].x) * (vertices [2].y - vertices [3].y);
		q3 = (vertices [3].x - vertices [0].x);


		List<Vector4>  shiftedPositions = new List<Vector4> {
			new Vector4(0, 0, 0, q0),
			new Vector4 (0, q1, 0, q1),
			new Vector4 (q2 / (vertices [2].y - vertices [3].y), q2 / (vertices [2].x - vertices [1].x), 0, q2),
			new Vector4 (q3, 0, 0, q3)
		};

		mesh.SetUVs (0, shiftedPositions);

		widths_heights [0].x = widths_heights [3].x = shiftedPositions [3].x;
		widths_heights [1].x = widths_heights [2].x = shiftedPositions [2].x;
		widths_heights [0].y = widths_heights [1].y = shiftedPositions [1].y;
		widths_heights [2].y = widths_heights [3].y = shiftedPositions [2].y;

		//mesh.uv2 = widths_heights;

		Vector2[] qs = new Vector2[] {
			new Vector2(q0, 1),
			new Vector2(q1, 1),
			new Vector2(q2, 1),
			new Vector2(q3, 1)
		};

		mesh.uv2 = qs;




		transform.GetComponent<MeshCollider> ().sharedMesh = mesh;
	}

	/// <summary>
	/// Gets the homogeneous coordinates for perspective correction.
	/// Based on https://bitlush.com/blog/arbitrary-quadrilaterals-in-opengl-es-2-0
	/// </summary>
	private void GetHomogeneousCoords() {
		float ax = vertices [2].x - vertices [0].x;
		float ay = vertices [2].y - vertices [0].y;
		float bx = vertices [1].x - vertices [3].x;
		float by = vertices [1].y - vertices [3].y;

		float cross = ax * by - ay * bx;

		if (cross != 0) {
			float cy = vertices [0].y - vertices [3].y;
			float cx = vertices [0].x - vertices [3].x;

			float s = (ax * cy - ay * cx) / cross;

			if (s > 0 && s < 1) {
				float t = (bx * cy - by * cx) / cross;

				if (t > 0 && t < 1) {
					q0 = 1 / (1 - t);
					q1 = 1 / (1 - s);
					q2 = 1 / t;
					q3 = 1 / s;

					Debug.Log ("Q0 is " + q0);
					Debug.Log ("Q1 is " + q1);
					Debug.Log ("Q2 is " + q2);
					Debug.Log ("Q3 is " + q3);

					q0 = 1;
					q1 = 1;
					q2 = 1;
					q3 = 1;


				}
			}
		}
	}


	/// Methods section 
	private void CornerMaker ()
	{
		_corners = new GameObject[vertices.Length]; // make corners spheres 
		for (int i = 0; i < vertices.Length; i++) {
			var obj = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			obj.transform.SetParent (transform);
			obj.GetComponent<Renderer> ().material.color = i == selectedCorner ? Color.green : Color.red;
			_corners [i] = obj;
		}
	}

	/// <summary>
	/// Ons the off objects.
	/// </summary>
	/// <param name="visible">If set to <c>true</c> visible.</param>
	private void onOffObjects (bool visible)
	{
		for (int i = 0; i < vertices.Length; i++) {
			GameObject sphere = _corners [i];
			sphere.transform.position = transform.TransformPoint (vertices [i]);
			sphere.SetActive (visible);
		}
	}

	/// <summary>
	/// Raises the scene control event.
	/// </summary>
	private void OnSceneControl ()
	{
		if (Input.anyKey && _debug)
			Debug.Log("Current input is " + Input.inputString);

		if (Input.GetKey (KeyCode.S)) {
			SaveSettings ();
			return;
		} else if (Input.GetKey (KeyCode.L)) {
			LoadSettings ();
			return;
		}

		if (!_useKeystone)
			return;
		UpdateSelection ();
	}

	/// <summary>
	/// Saves the settings to a JSON.
	/// </summary>
	/// <returns><c>true</c>, if settings were saved, <c>false</c> otherwise.</returns>
	private bool SaveSettings() {
		if (_debug)
			Debug.Log ("Saving keystone settings.");

		settings.vertices = vertices;

		string dataAsJson = JsonUtility.ToJson (settings);
		JsonParser.writeJSON (_settingsFileName, dataAsJson);

		return true;
	}

	/// <summary>
	/// Loads the settings.
	/// </summary>
	/// <returns><c>true</c>, if settings were loaded, <c>false</c> otherwise.</returns>
	private bool LoadSettings() {
		if (_debug)
			Debug.Log ("Loading keystone settings.");

		string dataAsJson = JsonParser.loadJSON (_settingsFileName, _debug);

		if (dataAsJson.Length == 0)
			settings = new KeystoneSettings (_vertices);
		else {
			settings = JsonUtility.FromJson<KeystoneSettings> (dataAsJson);
			vertices = settings.vertices;
		}
		SetupMesh ();

		return true;
	}

	/// <summary>
	/// Updates the selection for each keypress event.
	/// </summary>
	private void UpdateSelection() {
		if (Input.anyKey)
			needUpdate = true;
		
		var corner = Input.GetKeyDown ("1") ? 0 : (Input.GetKeyDown ("2") ? 1 : (Input.GetKeyDown ("3") ? 2 : (Input.GetKeyDown ("4") ? 3 : selectedCorner)));

		if (corner != selectedCorner) {
			_corners [selectedCorner].GetComponent<Renderer> ().material.color = Color.red;
			_corners [corner].GetComponent<Renderer> ().material.color = Color.green;
			selectedCorner = corner;
			if (_debug) 
				Debug.Log ("Selection changed to " + selectedCorner.ToString ());
		}

		if (Input.GetKey (KeyCode.LeftShift))
			speed *= 10;
		else if (Input.GetKey (KeyCode.LeftControl) && Input.GetKey (KeyCode.LeftAlt))
			speed *= 0.1f;
		else if (Input.GetKey (KeyCode.LeftAlt))
			speed *= 0.01f; 

		var v = vertices [selectedCorner];

		if (Input.GetKey (KeyCode.UpArrow))
			v = v + speed * Vector3.up;
		else if (Input.GetKey (KeyCode.DownArrow))
			v = v + speed * Vector3.down;
		else if (Input.GetKey (KeyCode.LeftArrow))
			v = v + speed * Vector3.left;
		else if (Input.GetKey (KeyCode.RightArrow))
			v = v + speed * Vector3.right;
		else needUpdate = false;

		vertices [selectedCorner] = v;
	}


	/// <summary>
	/// Reloads configuration / keystone settings when the scene is refreshed.
	/// </summary>
	void OnReloadKeystone() {
		Debug.Log ("Keystone config was reloaded!");

		LoadSettings ();
	}
}



