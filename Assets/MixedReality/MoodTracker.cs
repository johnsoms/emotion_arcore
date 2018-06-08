using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoodTracker : MonoBehaviour {

	UIManager uiManager;

	// Mood Tracker Attributes
	Renderer moodTrackerRenderer;
	private float currRotationX = -90.0f;
	// Use this for initialization
	void Start () {
		uiManager = FindObjectOfType<UIManager> ();
		moodTrackerRenderer = gameObject.GetComponent<Renderer> ();
//		gameObject.transform.localScale = new Vector3 (uiManager.normalizedMoodTrackerCoordinates.z * 0.2f,uiManager.normalizedMoodTrackerCoordinates.z * 0.2f,uiManager.normalizedMoodTrackerCoordinates.z * 0.2f);
		/*
		//To stop memory leak, delete previous material
		if (assignMaterial != null) {
			UnityEditor.AssetDatabase.DeleteAsset (UnityEditor.AssetDatabase.GetAssetPath (assignMaterial));
		}

		//Add material to cube if not assigned
		assignMaterial = new Material(Shader.Find("Diffuse"));
		*/
	}

	// Update is called once per frame
	void Update () {

		//Update uiManager-cube position
		Vector3 moodTrackerPosition = new Vector3 (uiManager.normalizedMoodTrackerCoordinates.x, uiManager.normalizedMoodTrackerCoordinates.y, uiManager.normalizedMoodTrackerCoordinates.z);
		gameObject.transform.localPosition = moodTrackerPosition;
//		gameObject.transform.lossyScale = new Vector3 (uiManager.normalizedMoodTrackerCoordinates.z * 0.2f,uiManager.normalizedMoodTrackerCoordinates.z * 0.2f,uiManager.normalizedMoodTrackerCoordinates.z * 0.2f);
		gameObject.transform.localScale = new Vector3 (uiManager.normalizedMoodTrackerCoordinates.w,uiManager.normalizedMoodTrackerCoordinates.w,uiManager.normalizedMoodTrackerCoordinates.w);

//		gameObject.transform.rotation = uiManager.emojiOrientation;
		gameObject.transform.Rotate(new Vector3(uiManager.GetRotation() - currRotationX,0f,0f));
		currRotationX = uiManager.GetRotation ();
		//Update mood-cube color
//		Color moodColor = uiManager.GetMoodTrackerColor();
//		moodTrackerRenderer.material.color = moodColor;
		Material emoji = uiManager.GetMoodTrackerEmoji();
		moodTrackerRenderer.material = emoji;
		//Debug.Log ("Color: " + moodColor);


	}
}
