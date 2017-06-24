﻿using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class keyStone : MonoBehaviour
{

	// quad control vars
	public Vector3[] _vertices;
	private GameObject[] corners;
	public bool _useKeystone = true;
	public int selectedCorner;

	void Start ()
	{

		Destroy (this.GetComponent <MeshCollider> ()); //destroy so we can make one in dynamically 
		transform.gameObject.AddComponent <MeshCollider> (); //add new collider 
	
		Mesh mesh = GetComponent<MeshFilter> ().mesh; // get this GO mesh
		mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };	// lower left & upper right

		cornerMaker (); //make the corners for visual controls 
	}

	void Update ()
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		mesh.vertices = _vertices;

		// Update is vertices changed
		if (!_useKeystone)
			return;
		 
		// Zero out the left and bottom edges, 
		// leaving a right trapezoid with two sides on the axes and a vertex at the origin.
		// 0: 0, 0
		// 1: 0, 1
		// 2: 1, 1
		// 3: 1, 0
//		var shiftedPositions = new List<Vector4> {
//			Vector4.zero,
//			new Vector4 (0, _vertices [1].y - _vertices [0].y, 0, _vertices [1].y - _vertices [0].y),
//			new Vector4 (_vertices [2].x - _vertices [1].x, _vertices [2].y - _vertices [3].y, 0, _vertices [2].y - _vertices [3].y),
//			new Vector4 (_vertices [3].x - _vertices [0].x, 0, 0, 0)
//		};
//		mesh.uv = shiftedPositions;



		float xTop = _vertices [2].x - _vertices [1].x;
		float xBottom = _vertices [3].x - _vertices [0].x;
		float yHeight = _vertices [1].y - _vertices [0].y;

		Debug.Log ("x: " + xTop + "\ty : " + yHeight); 
		Debug.Log ("x: " + _vertices [0].x + " " + _vertices [1].x + " " + _vertices [2].x + " " + _vertices [3].x + " "); 

		var shiftedPositions = new List<Vector4> {
			new Vector4 (0, 0, 0, xBottom),
			new Vector4 (0, yHeight, 0, yHeight),
			new Vector4 (xTop, _vertices [2].y - _vertices [3].y, 0, xBottom / xTop),
			new Vector4 (xBottom, 0, 0, xBottom)
		};
		mesh.SetUVs (1, shiftedPositions);
		mesh.SetUVs (0, shiftedPositions);
//
//		var widths_heights = new Vector2[4];
//		widths_heights [0].x = widths_heights [3].x = shiftedPositions [3].x;
//		widths_heights [1].x = widths_heights [2].x = shiftedPositions [2].x;
//		widths_heights [0].y = widths_heights [1].y = shiftedPositions [1].y;
//		widths_heights [2].y = widths_heights [3].y = shiftedPositions [2].y;
//		mesh.uv2 = widths_heights;

		mesh.RecalculateNormals();

		onOffObjects (_useKeystone); // toggles onoff at each click
		if (_useKeystone) {
			OnSceneControl ();
		}

		transform.GetComponent<MeshCollider> ().sharedMesh = mesh;// make new collider based on updated mesh 
	}

	/// Methods section 

	private void cornerMaker ()
	{

		corners = new GameObject[_vertices.Length]; // make corners spheres 
		for (int i = 0; i < _vertices.Length; i++) {
			var obj = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			obj.transform.SetParent (transform);
			obj.GetComponent<Renderer> ().material.color = i == selectedCorner ? Color.green : Color.red;
			corners [i] = obj;
		}
	}


	private void onOffObjects (bool visible)
	{

		for (int i = 0; i < _vertices.Length; i++) {
			GameObject sphere = corners [i];
			sphere.transform.position = transform.TransformPoint (_vertices [i]);
			sphere.SetActive (visible);
		}
	}

	private void OnSceneControl ()
	{
//		if (Input.anyKey)
//		{
//			Debug.Log(Input.inputString);
//		}

		if (!_useKeystone)
			return;

		var corner = Input.GetKeyDown ("1") ? 0 : (Input.GetKeyDown ("2") ? 1 : (Input.GetKeyDown ("3") ? 2 : (Input.GetKeyDown ("4") ? 3 : selectedCorner)));
		if (corner != selectedCorner) {
			corners [selectedCorner].GetComponent<Renderer> ().material.color = Color.red;
			corners [corner].GetComponent<Renderer> ().material.color = Color.green;
			selectedCorner = corner;
			Debug.Log("Selection changed to " + selectedCorner.ToString());
		}

		float speed = 0.5f;

		if (Input.GetKey (KeyCode.LeftShift))
			speed *= 10;
		else if (Input.GetKey (KeyCode.LeftControl) && Input.GetKey (KeyCode.LeftAlt))
			speed *= 0.1f;
		else if (Input.GetKey (KeyCode.LeftAlt))
			speed *=0.01f; 

		var v = _vertices [selectedCorner];

		if (Input.GetKeyDown (KeyCode.UpArrow))
			v = v + speed * Vector3.up;
		else if (Input.GetKeyDown (KeyCode.DownArrow))
			v = v + speed * Vector3.down;
		else if (Input.GetKeyDown (KeyCode.LeftArrow))
			v = v + speed * Vector3.left;
		else if (Input.GetKeyDown (KeyCode.RightArrow))
			v = v + speed * Vector3.right;

		_vertices [selectedCorner] = v;
	}
}