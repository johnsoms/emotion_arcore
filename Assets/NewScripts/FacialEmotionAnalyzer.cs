using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using Affdex;

public class FacialEmotionAnalyzer : ImageResultsListener {

	private EmotionStruct currentEmotions;	// The cumulative current emotions over the past 10 second window
	private Vector4 moodTrackerParameters;
	private ArrayList emotionWindow;
	private int frameSampleCount = 0;
	private int frameSampleRate = 20;
	private FACSStruct currentFACS;
	private Quaternion orientation;

	// Use this for initialization
	void Start () {
		currentEmotions = new EmotionStruct();
		currentFACS = new FACSStruct ();
		emotionWindow = new ArrayList();
		emotionWindow.Capacity = 10;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public EmotionStruct getCurrentEmotions()
	{
		// Debug.Log("Got current emotions.");
		return currentEmotions;
	}
	public FACSStruct getCurrentFACS(){
		return currentFACS;
	}

	public override void onFaceFound(float timestamp, int faceId)
    {
        Debug.Log("Found the face");
    }

    public override void onFaceLost(float timestamp, int faceId)
    {
        Debug.Log("Lost the face");
    }

    public override void onImageResults(Dictionary<int, Face> faces)
    {
        // Debug.Log("Got face results");

        foreach (KeyValuePair<int, Face> pair in faces)
        {
            int FaceId = pair.Key;  // The Face Unique Id.
            Face face = pair.Value;    // Instance of the face class containing emotions, and facial expression values.

            //Retrieve the Emotions Scores
            // face.Emotions.TryGetValue(Emotions.Contempt, out currentContempt);
            // face.Emotions.TryGetValue(Emotions.Valence, out currentValence);

			// Ensure that emotion values are sampled only once a second, regardless of the frame rate
			if (frameSampleCount % frameSampleRate == 0)
			{

//				new Thread(() => 
//					{
//						Thread.CurrentThread.IsBackground = true; 
//						/* run your code here */ 
//						var bytes = File.ReadAllBytes(wavFile);
//						var analysisResponseString = CreateWebRequest(analysisUrl, bytes, token);
//						//							Debug.Log(analysisResponseString);
//						currentAnalysis = JSON.Parse(analysisResponseString);
//					}).Start()

				// Retrieve the emotion for this frame
				EmotionStruct nextEmotion = new EmotionStruct();
				face.Emotions.TryGetValue(Emotions.Joy, out nextEmotion.joy);
				face.Emotions.TryGetValue(Emotions.Fear, out nextEmotion.fear);
				face.Emotions.TryGetValue(Emotions.Disgust, out nextEmotion.disgust);
				face.Emotions.TryGetValue(Emotions.Sadness, out nextEmotion.sadness);
				face.Emotions.TryGetValue(Emotions.Anger, out nextEmotion.anger);
				face.Emotions.TryGetValue(Emotions.Surprise, out nextEmotion.surprise);
				face.Emotions.TryGetValue(Emotions.Contempt, out nextEmotion.contempt);
				face.Emotions.TryGetValue(Emotions.Valence, out nextEmotion.valence);
				face.Emotions.TryGetValue(Emotions.Engagement, out nextEmotion.engagement);

				// Retrieve FACS values
				face.Expressions.TryGetValue(Expressions.Smile, out currentFACS.Smile);
				face.Expressions.TryGetValue(Expressions.InnerBrowRaise, out currentFACS.InnerEyeBrowRaise);
				face.Expressions.TryGetValue(Expressions.BrowRaise, out currentFACS.BrowRaise);
				face.Expressions.TryGetValue(Expressions.BrowFurrow, out currentFACS.BrowFurrow);
				face.Expressions.TryGetValue(Expressions.NoseWrinkle, out currentFACS.NoseWrinkler);
				face.Expressions.TryGetValue(Expressions.UpperLipRaise, out currentFACS.UpperLipRaiser);
				face.Expressions.TryGetValue(Expressions.LipCornerDepressor, out currentFACS.LipCornerDepressor);
				face.Expressions.TryGetValue(Expressions.ChinRaise, out currentFACS.ChinRaiser);
				face.Expressions.TryGetValue(Expressions.LipPucker, out currentFACS.LipPucker);
				face.Expressions.TryGetValue(Expressions.LipPress, out currentFACS.LipPress);
				face.Expressions.TryGetValue(Expressions.LipSuck, out currentFACS.LipSuck);
				face.Expressions.TryGetValue(Expressions.MouthOpen, out currentFACS.MouthOpen);
				face.Expressions.TryGetValue(Expressions.Smirk, out currentFACS.Smirk);
				face.Expressions.TryGetValue(Expressions.EyeClosure, out currentFACS.EyeClosure);
				face.Expressions.TryGetValue(Expressions.Attention, out currentFACS.Attention);
				//Not considering surprise or disgust for now
//				nextEmotion.surprise = 0f;
//				nextEmotion.disgust = 0f;
				// currentEmotions = nextEmotion;
				// Debug.Log("NEW ANGER!: " + nextEmotion.anger);

				// Add in the next emotion captured by Affectiva
				if (emotionWindow.Count == 4)
				{
					// add the tenth analysis to complete the window
					emotionWindow.Add(nextEmotion);

					// calculate the currentEmotions for this window and assign the parameter
					calculateCurrentEmotion();

					// shift the window back to prepare for the next emotion data
					emotionWindow.RemoveAt(0);
				}
				else
				{
					// add the next element
					emotionWindow.Add (nextEmotion);
				}
			}
			
			frameSampleCount++;
			
			// check for overflow and reset if it occurs
			if (frameSampleCount < 0)
				frameSampleCount = 0;

			// Debug.Log("Frame sample count: " + frameSampleCount);

			//Retrieve the Smile Score
			// face.Expressions.TryGetValue(Expressions.Smile, out currentSmile);

			//Retrieve the Interocular distance, the distance between two outer eye corners.
			//Retrieve the coordinates of the facial landmarks (face feature points)
			FeaturePoint[] featurePointsList = face.FeaturePoints;
			Measurements measurementsList = face.Measurements;

			moodTrackerParameters.x = featurePointsList [12].x;
			moodTrackerParameters.y = featurePointsList [12].y;
			moodTrackerParameters.z = measurementsList.interOcularDistance;
			moodTrackerParameters.w = Mathf.Abs (featurePointsList [2].y - featurePointsList [12].y);
			orientation = face.Measurements.Orientation;

        }
    }

	public Vector4 GetMoodTrackerGeometry (){
		return moodTrackerParameters;
	}

	public Quaternion GetHeadOrientation(){
		return orientation;
	}

	// Sets the currentEmotions struct as an average of the past ten seconds worth of emotion collected from facial analysis.
	private void calculateCurrentEmotion()
	{
		if (emotionWindow.Count > 0)
		{
			EmotionStruct emotionSum = new EmotionStruct();
			foreach (EmotionStruct e in emotionWindow)
			{
				emotionSum.anger += e.anger;
				emotionSum.joy += e.joy;
				emotionSum.fear += e.fear;
				emotionSum.sadness += e.sadness;
				emotionSum.disgust += e.disgust;
				emotionSum.surprise += e.surprise;
				emotionSum.contempt += e.contempt;
				emotionSum.valence += e.valence;
				emotionSum.engagement += e.engagement;
			}

			currentEmotions.anger = emotionSum.anger / (float) emotionWindow.Count;
			currentEmotions.joy = emotionSum.joy / (float) emotionWindow.Count;
			currentEmotions.fear = emotionSum.fear / (float) emotionWindow.Count;
			currentEmotions.sadness = emotionSum.sadness / (float) emotionWindow.Count;
			currentEmotions.disgust = emotionSum.disgust / (float) emotionWindow.Count;
			currentEmotions.surprise = emotionSum.surprise / (float) emotionWindow.Count;
			currentEmotions.contempt = emotionSum.contempt / (float) emotionWindow.Count;
			currentEmotions.valence = emotionSum.valence / (float) emotionWindow.Count;
			currentEmotions.engagement = emotionSum.engagement / (float) emotionWindow.Count;
		}
		else
		{
			currentEmotions = new EmotionStruct();
		}

		// Debug.Log("emotion window count: " + emotionWindow.Count);
		// Debug.Log("facial anger" + currentEmotions.anger);
		// Debug.Log("facial joy" + currentEmotions.joy);
		// Debug.Log("facial fear" + currentEmotions.fear);
		// Debug.Log("facial sadness" + currentEmotions.sadness);
		// Debug.Log("facial disgust" + currentEmotions.disgust);
		// Debug.Log("facial surprise" + currentEmotions.surprise);
	}
}
