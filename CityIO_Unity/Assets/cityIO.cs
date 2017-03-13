using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


/// <summary> class start </summary>

[System.Serializable]  // have to have this in every JSON class!
public class Grid
{
	public int type;
	public int x;
	public int y;
	public int rot;
}

[System.Serializable] // have to have this in every JSON class!
public class Objects
{
	public int slider1;
	public int toggle1;
	public int toggle2;
	public int toggle3;
	public int toggle4;
	public int dockID;
	public int dockRotation;
	public int IDMax;
	public List<int> density;
	public int pop_young;
	public int pop_mid;
	public int pop_old;
}

[System.Serializable]// have to have this in every JSON class!
public class Table
{
	public List<Grid> grid;
	public Objects objects;
	public string id;
	public long timestamp;

	public static Table CreateFromJSON (string jsonString)
	{ // static function that returns Table which holds Class objects 
		return JsonUtility.FromJson<Table> (jsonString);
	}
}

/// <summary> class end </summary>


public class cityIO : MonoBehaviour
{

//	private string localJson = "file:///Users/noyman/GIT/KendallAgents/Assets/Resources/scripts/citymatrix_volpe.json";
	private string urlStart = "http://45.55.73.103/table/citymatrix_";
	public string urlTable = "volpe";

	private string url;

	public int delayWWW;
	private WWW www;
	private string cityIOtext;
	private string cityIOtext_Old;
	//this one look for changes

	public int tableX;
	public int tableY;
	public int cellWorldSize;
	public float cellShrink;
	public float floorHeight;

	private GameObject cube;

	public Table Cells;
	public GameObject gridParent;
	public static List<GameObject> gridObjects = new List<GameObject> ();
	//new list!



	public Color[] colors;



	IEnumerator Start ()
	{
		while (true) {
			
			url = urlStart + urlTable;
			//WWW www = new WWW (url);
			WWW www = new WWW (url);

			yield return www;
			yield return new WaitForSeconds (delayWWW);
			cityIOtext = www.text; //just a cleaner Var
			if (cityIOtext != cityIOtext_Old) {
				cityIOtext_Old = cityIOtext; //new data has arrived from server 
				jsonHandler ();

			}
		}
	}


	void jsonHandler ()
	{
		Cells = Table.CreateFromJSON (cityIOtext); // get parsed JSON into Cells variable --- MUST BE BEFORE CALLING ANYTHING FROM CELLS!!
		drawTable ();
	
		// prints last update time to console 
		System.DateTime epochStart = new System.DateTime (1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
		var lastUpdateTime = epochStart.AddSeconds (System.Math.Round (Cells.timestamp / 1000d)).ToLocalTime ();
		print ("Table was updated." + '\n' + "Following JSON from CityIO server was created at: " + lastUpdateTime + '\n' + cityIOtext);
	}


	void drawTable ()
	{

		foreach (Transform child in gridParent.transform) {
			GameObject.Destroy (child.gameObject); // strat clean 
			gridObjects.Clear (); // clean list!!!
		}

		for (int i = 0; i < Cells.grid.Count; i++) {

			cube = GameObject.CreatePrimitive (PrimitiveType.Cube); //make cell cube  
			cube.transform.parent = gridParent.transform; //put into parent object for later control 
			cube.transform.position = new Vector3 ((Cells.grid [i].x * cellWorldSize), 0, (Cells.grid [i].y * cellWorldSize)); //compensate for scale shift due to height


			for (int n = -10; n < Cells.objects.density.Count; n++) { //go through all 'densities' to match Type to Height 
				if (Cells.grid [i].type == n) {
					
					if (Cells.grid [i].type >= 0) { //if this cell is one of the buildings types
				
						cube.transform.localScale = new Vector3 (cellShrink * cellWorldSize, (Cells.objects.density [n] * floorHeight), cellShrink * cellWorldSize);
						cube.transform.position = new Vector3 (cube.transform.position.x, (Cells.objects.density [n] * floorHeight) / 2, cube.transform.position.z); //compensate for scale shift and x,y array
						cube.AddComponent<NavMeshObstacle> ();
						cube.GetComponent<NavMeshObstacle> ().carving = true; 
						cube.GetComponent<Renderer> ().material.color = colors [Cells.grid [i].type];

						//cube.tag = "amenity"; // to become attractor 

				
					} else { //if road, green or other non building 
				
						cube.transform.position = new Vector3 (cube.transform.position.x, -5, cube.transform.position.z); //hide base plates 
						cube.transform.localScale = new Vector3 (cellShrink * cellWorldSize, 1, cellShrink * cellWorldSize);
						cube.GetComponent<Renderer> ().material.color = Color.black;
					}
				
				}
			}

			gridObjects.Add (cube); //add this Gobj to list
		}


	}

}

