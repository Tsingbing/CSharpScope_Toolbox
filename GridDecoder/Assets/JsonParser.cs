using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;  

public class JsonParser : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	/// <summary>
	/// Loads a JSON file from fileName.
	/// Following https://unity3d.com/learn/tutorials/topics/scripting/loading-game-data-json
	/// </summary>
	/// <returns><c>true</c>, if JSO was loaded, <c>false</c> otherwise.</returns>
	/// <param name="fileName">File name.</param>
	public bool loadJSON(string fileName) {
		string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

		if (File.Exists(filePath))
		{
			// Read the json from the file into a string
			//string dataAsJson = File.ReadAllText(filePath); 
			// Pass the json to JsonUtility, and tell it to create a GameData object from it
			//GameData loadedData = JsonUtility.FromJson<GameData>(dataAsJson);

			// Retrieve the allRoundData property of loadedData
			//allRoundData = loadedData.allRoundData;
			return true;
		}
		else
		{
			Debug.LogError("Cannot load game data!");
			return false;
		}
	}
}
