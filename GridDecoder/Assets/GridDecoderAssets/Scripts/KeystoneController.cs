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

	private float mua;
	private float mub;

	private float[] d = new float[4];
	float[] q = new float[4];

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

		q0 = 1;
		q1 = 1;
		q2 = 1;
		q3 = 1;

//		Vector2[]  shiftedPositions = new Vector2[] {
//			new Vector2(0, 0),
//			new Vector2 (0, q1),
//			new Vector2 (q2 / (vertices [2].y - vertices [3].y), q2 / (vertices [2].x - vertices [1].x)),
//			new Vector2 (q3, 0)
//		};

		Vector2[]  shiftedPositions = new Vector2[] {
			new Vector2(0, 0),
			new Vector2 (0, q1),
			new Vector2 (q2 , q2),
			new Vector2 (q3, 0)
		};
			
		if (IsIntersecting(shiftedPositions[1], shiftedPositions[0], shiftedPositions[3], shiftedPositions[2]))
		{
			Debug.Log ("Mua = " + mua + " Mub = " + mub);
		}

		// http://www.reedbeta.com/blog/2012/05/26/quadrilateral-interpolation-part-1/
		// calculate qi
		// uvqi = (di+d(i+2))/d(i+2) (i=0°≠3)

		q[0] = (d [0] + d [2]) / d [2];
		q[1] = (d [1] + d [3]) / d [3];
		q[2] = (d [2] + d [0]) / d [0];
		q[3] = (d [3] + d [1]) / d [1];


		for (int i = 0; i < shiftedPositions.Length; i++) {
			shiftedPositions [i] *= q [i];
		}

		List<Vector3> shiftedPositionsV3 = new List<Vector3> {
			new Vector3(shiftedPositions[0].x, shiftedPositions[0].y, q[0]),
			new Vector3 (shiftedPositions[1].x, shiftedPositions[1].y, q[1]),
			new Vector3 (shiftedPositions[2].x, shiftedPositions[2].y, q[2]),
			new Vector3 (shiftedPositions[3].x, shiftedPositions[3].y, q[3])
		};

		mesh.SetUVs (0, shiftedPositionsV3);
		//mesh.uv = shiftedPositions;

		Vector2[] qs = new Vector2[] {
			new Vector2(q[0], 1),
			new Vector2(q[1], 1),
			new Vector2(q[2], 1),
			new Vector2(q[3], 1)
		};

		mesh.uv2 = qs;

		transform.GetComponent<MeshCollider> ().sharedMesh = mesh;
	}
		
	/// <summary>
	/// Determines whether this instance is intersecting the specified p1 p2 p3 p4 a b.
	/// From http://mathforum.org/library/drmath/view/62814.html
	/// and https://github.com/Geistyp/Projective-Interpolation-to-Quadrilateral
	/// </summary>
	/// <returns><c>true</c> if this instance is intersecting the specified p1 p2 p3 p4 a b; otherwise, <c>false</c>.</returns>
	/// <param name="p1">P1.</param>
	/// <param name="p2">P2.</param>
	/// <param name="p3">P3.</param>
	/// <param name="p4">P4.</param>
	/// <param name="a">The alpha component.</param>
	/// <param name="b">The blue component.</param>
	/// 1, 0, 3, 2
	private bool IsIntersecting(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
	{
		Vector2 V1 = p3 - p1;
		Vector2 V2 = p4 - p2;
		Vector2 P21 = p2 - p1;

		if (_debug) {
			Debug.DrawLine (p3, p1, Color.red, 200, false);
			Debug.DrawLine (p4, p2, Color.blue, 200, false);
		}

		float V1cV2 = GetCrossProduct (V1, V2);

		mua = (GetCrossProduct(P21,V2)) / (V1cV2);
		mub = (GetCrossProduct(-P21, V1)) / (V1cV2);

		Vector2 pIntersection = p1 + mua * V1;

		if (_debug) {
			var obj = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			obj.transform.position = pIntersection;
			obj.transform.localScale = new Vector3 (0.1f, 0.1f, 0.1f);
			obj.GetComponent<Renderer> ().material.color = Color.green;
		}

		d[0] = Vector2.Distance(pIntersection, p2);
		d [1] = Vector2.Distance(pIntersection, p1);
		d [2] = Vector2.Distance(pIntersection, p4);
		d [3] = Vector2.Distance(pIntersection, p3);

		return true;
	}

	/// <summary>
	/// From http://james-ramsden.com/calculate-the-cross-product-c-code/
	/// </summary>
	/// <returns>The cross product.</returns>
	/// <param name="v1">V1.</param>
	/// <param name="v2">V2.</param>
	private float GetCrossProduct(Vector2 v1, Vector2 v2)
	{
		float rtnvector = (v1.x * v2.y - v2.x * v1.y);
		return rtnvector;
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



