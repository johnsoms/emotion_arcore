using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


using Affdex;
using Affdex2;

public class UIManager : MonoBehaviour {

	public GameObject gameManagerObject;
	private GameManager gameManagerScript;

	public Camera mainCamera;
	public GameObject inputDeviceCamera;
	public GameObject inputDeviceCamera2;
	//public GameObject webcamRenderPlane;
	public GameObject webcamRenderQuad;
	public GameObject frontcamRenderQuad;
	public Text FacialEmotionText;
	public Text FACSText;
	public Text SentimentEmotionText;
	public Text VocalPADText;
	private Affdex.CameraInput camInputScript;
	private Affdex2.CameraInput camInputScript2;
	//private Renderer planeRenderer;
	public Material joy1;
	public Material joy2;
	public Material anger1;
	public Material anger2;
	public Material fear1;
	public Material fear2;
	public Material sadness1;
	public Material sadness2;
	public Material surprise1;
	public Material surprise2;
	public Material disgust1;
	public Material disgust2;
	public Material neutral;
	public Quaternion emojiOrientation;
	public int count = 0;

	public bool back = true;

	private Dictionary<string,Material[]> facialEmojiDict;
	//Facial Emotion Emoji Associations

	// Emotion modality colors
	private Color currentFacialEmotionColor;
	private Color previousFacialEmotionColor;
	private Color currentWordSentimentEmotionColor;
	private Color previousWordSentimentEmotionColor;
	private Color previousBackground;
	private Color currentBackground;
	private float currentHue;

	private Material currentEmotionEmoji;
	private Material currentEmotionEmoji2;
	public Image facialEmotionBar;
	private float currentFacialEmotionBarWidth;
	private float previousFacialEmotionBarWidth;

	public Image wordSentimentEmotionBar;
	private float currentWordSentimentEmotionBarWidth;
	private float previousWordSentimentEmotionBarWidth;

	private ToneAnalysis vocalToneResults;

	private float[] currentHueDist = {0f,0f,0f,0f,0f,0f,0f,0f};
	private float currentSaturation = 1f;
	private float currentValue = 0f;
	private float currentStrength = 0f;

	private float rotationX = -90.0f;
	private float rotationX2 = -90.0f;
	// Use this for initialization
	void Start () {
		facialEmojiDict = new Dictionary<string,Material[]>{{"anger", new Material[]{anger1,anger2}},{"fear",new Material[]{fear1,fear2}},{"joy",new Material[]{joy1,joy2}},{"surprise",new Material[]{surprise1,surprise2}},{"sadness",new Material[]{sadness1,sadness2}},{"disgust",new Material[]{disgust1,disgust2}},{"neutral",new Material[]{neutral,neutral}}};
		gameManagerScript = (GameManager) gameManagerObject.GetComponent(typeof(GameManager));
		camInputScript = (Affdex.CameraInput) inputDeviceCamera.GetComponent<Affdex.CameraInput>();

		camInputScript2 = (Affdex2.CameraInput) inputDeviceCamera.GetComponent<Affdex2.CameraInput>();
		//planeRenderer = (Renderer) webcamRenderPlane.GetComponent<Renderer>();
		quadRenderer = webcamRenderQuad.GetComponent<Renderer> ();
//		// Camera feed parameters
		if (camInputScript.Texture == null) {
			Debug.Log ("Camera not started");
			feedWidth = camInputScript.targetWidth;
			feedHeight = camInputScript.targetHeight;
			camReady = false;
		}

		frontRenderer = frontcamRenderQuad.GetComponent<Renderer> ();

		if (camInputScript2.Texture == null) {
			Debug.Log ("Camera2 not started");
			frontFeedWidth = camInputScript2.targetWidth;
			frontFeedHeight = camInputScript2.targetHeight;
			frontcamReady = false;
		}

		SetFeed ();



		//Apply webcam texture to quad gameobject
		quadRenderer.material.mainTexture = camInputScript.Texture;
		frontRenderer.material.mainTexture = camInputScript2.Texture;
		// Initalize the colors


		previousFacialEmotionColor = new Color();
		currentFacialEmotionColor = new Color();

		previousWordSentimentEmotionColor = new Color();
		currentWordSentimentEmotionColor = new Color();

		// Initialize the bar widths
		currentFacialEmotionBarWidth = 0.0f;
		previousFacialEmotionBarWidth = 0.0f;

		currentWordSentimentEmotionBarWidth = 0.0f;
		previousWordSentimentEmotionBarWidth = 0.0f;


		// Start the background emotion updater
		StartCoroutine(RequestEmotionUpdate());
	}
	
	// Update is called once per frame
	void Update () {
		count++;
		// Display the webcam input
		//planeRenderer.material.mainTexture = camInputScript.Texture;
		Debug.Log(back);
//		camInputScript.gameObject.SetActive (false);
//
//		camInputScript2.gameObject.SetActive (true);
//
//		camInputScript.enabled = false;
//		camInputScript2.enabled = true;

		if (count>5) {
			camInputScript2.Play ();
			camInputScript.Stop ();
//			camInputScript = (Affdex.CameraInput) inputDeviceCamera.GetComponent<Affdex.CameraInput>();
			count = -5;
//			camInputScript.Restart();
//			if (camInputScript.Texture == null) {
//				Debug.Log ("Camera not started");
//				feedWidth = camInputScript.targetWidth;
//				feedHeight = camInputScript.targetHeight;
//				camReady = false;
//			}
//			SetFeed ();
			frontRenderer.material.mainTexture = camInputScript2.Texture;
			camInputScript2.ProcessFrame ();
		} else if (count > 0) {
			camInputScript.Play ();
			camInputScript2.Stop ();
			//			camInputScript = (Affdex.CameraInput) inputDeviceCamera.GetComponent<Affdex.CameraInput>();
			quadRenderer.material.mainTexture = camInputScript.Texture;

			camInputScript.ProcessFrame ();
//
//			camInputScript.Stop ();
//			camInputScript2.Play ();
//			camInputScript2.ProcessFrame ();
//			frontRenderer.material.mainTexture = camInputScript2.Texture;
		}
		back = !back;
		if (gameManagerScript.useVocalToneEmotion)
		{
			vocalToneResults = gameManagerScript.getCurrentVocalEmotion ();
		}
			
	}

	private IEnumerator RequestEmotionUpdate()
	{
		// Debug.Log("Entered REQUEST EMOTION UPDATE COROUTINE.");
		while (true) 
		{
			yield return new WaitForSeconds(colorUpdateTime);

			if (gameManagerScript.useFacialEmotion)
			{
				EmotionStruct currentEmotions = gameManagerScript.getCurrentFacialEmotion();
				EmotionStruct currentEmotions2 = gameManagerScript.getCurrentFacialEmotion2();
				FACSStruct currentFACS = gameManagerScript.getCurrentFACS ();
				FACSStruct currentFACS2 = gameManagerScript.getCurrentFACS2 ();
				VocalPADText.text = "                      "+Affdex.CameraInput.sampleRate;
				FacialEmotionText.text = "Joy: " + currentEmotions2.joy + "\nAnger: " + currentEmotions2.anger + "\nFear: " + currentEmotions2.fear + "\nDisgust: " + currentEmotions2.disgust + "\nSadness: " + currentEmotions2.sadness + "\nContempt: " + currentEmotions2.contempt + "\nValence: " + currentEmotions2.valence + "\nEngagement: "+currentEmotions2.engagement;
				FACSText.text =  "Attention: "+ currentFACS2.Attention + "\nBrowFurrow: "+ currentFACS2.BrowFurrow + "\nBrowRaise: "+ currentFACS2.BrowRaise + "\nChinRaise: "+ currentFACS2.ChinRaiser + "\nEyeClose: "+ currentFACS2.EyeClosure + "\nInnerEyebrowRaise: "+ currentFACS2.InnerEyeBrowRaise + "\nLipCornerDepress: "+ currentFACS2.LipCornerDepressor + "\nLipPress: "+ currentFACS2.LipPress + "\nLipPucker: "+ currentFACS2.LipPucker + "\nLipSuck: "+ currentFACS2.LipSuck + "\nMouthOpen: "+ currentFACS2.MouthOpen + "\nNoseWrinkle: "+ currentFACS2.NoseWrinkler + "\nSmile: "+ currentFACS2.Smile +"\nSmirk: "+ currentFACS2.Smirk + "\nUpperLipRaise"+currentFACS2.UpperLipRaiser;
				// Update facial emotion colors
				if (gameManagerScript.useAugmentedBasicEmotions) {
					currentValue = (currentEmotions.valence + 175f) / 300f;
					currentHueDist = gameManagerScript.calculateEmotionHueDist (currentEmotions);
					currentSaturation = sum (currentHueDist) / 100f;
					currentHue = getStrongestHue(currentHueDist);
				} else if (gameManagerScript.useBasicEmotions) {

				}
				else {
					currentValue = (currentEmotions.valence + 175f) / 300f;
					currentSaturation = currentEmotions.engagement / 100f;
					currentHue = (currentValue + currentSaturation)/2;
				}
				string currentStrongestEmotion = getStrongestEmotion (currentHueDist);
				string currentStrongestEmotion2 = getStrongestEmotion2 (currentEmotions2);
				int threshidx = 0;
				if (getStrongestEmotionVal (currentHueDist) >= 50.0f) {
					threshidx = 1;
				}
				currentEmotionEmoji = facialEmojiDict [currentStrongestEmotion] [threshidx];
				currentEmotionEmoji2 = facialEmojiDict [currentStrongestEmotion2] [threshidx];
				if (currentStrongestEmotion == "joy") {
						rotationX = 0f;
				} else {
					rotationX = -90.0f;
				}
				if (currentStrongestEmotion2 == "joy") {
					rotationX2 = 0f;
				} else {
					rotationX2 = -90.0f;
				}
				previousFacialEmotionColor = currentFacialEmotionColor;
				currentFacialEmotionColor = gameManagerScript.calculateEmotionColor(gameManagerScript.getCurrentFacialEmotion());

				// Update the emotion bars
				previousFacialEmotionBarWidth = currentFacialEmotionBarWidth;
				currentFacialEmotionBarWidth = gameManagerScript.getValueOfStrongestEmotion(gameManagerScript.getCurrentFacialEmotion()) * 2;
			}

			if (gameManagerScript.useWordSentimentEmotion)
			{
				// Update word sentiment emotion colors
				previousWordSentimentEmotionColor = currentWordSentimentEmotionColor;
				currentWordSentimentEmotionColor = gameManagerScript.calculateEmotionColor(gameManagerScript.getCurrentWordSentimentEmotion());

				EmotionStruct currentEmotions = gameManagerScript.getCurrentWordSentimentEmotion();

				SentimentEmotionText.text = "Joy: " + currentEmotions.joy + "\nAnger: " + currentEmotions.anger + "\nFear: " + currentEmotions.fear + "\nDisgust: " + currentEmotions.disgust + "\nSadness: " + currentEmotions.sadness;

				previousWordSentimentEmotionBarWidth = currentWordSentimentEmotionBarWidth;
				currentWordSentimentEmotionBarWidth = gameManagerScript.getValueOfStrongestEmotion(gameManagerScript.getCurrentWordSentimentEmotion()) * 2;
			}
			if (gameManagerScript.useVocalToneEmotion) 
			{
				if (gameManagerScript.useFacialEmotion) {
					currentSaturation += vocalToneResults.ArousalVal / 100f;
					currentValue += vocalToneResults.ValenceVal / 100f;
					currentSaturation /= 2f;
					currentValue /= 2f;
				}
				else
				{
					currentSaturation = vocalToneResults.ArousalVal / 100f;
					currentValue = vocalToneResults.ValenceVal / 100f;
				}
			}

			currentBackground = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
			StartCoroutine(UpdateBackgroundColor());
		}
	}

	// Coroutine enumerator for updating the current emotion color using linear interpolation over a predefined amount of time
	private IEnumerator UpdateBackgroundColor()
	{		
		// Debug.Log("Entered UPDATE BACKGROUND COLOR COROUTINE.");
		float t = 0;
		while (t < 1)
		{
			previousBackground = mainCamera.backgroundColor;

			// Now the loop will execute on every end of frame until the condition is true

			if (gameManagerScript.useFacialEmotion)
			{
				// Update the facial emotion bar
//				facialEmotionBar.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(previousFacialEmotionBarWidth, currentFacialEmotionBarWidth, t),
//					facialEmotionBar.rectTransform.sizeDelta.y);
//				facialEmotionBar.color = Color.Lerp(previousFacialEmotionColor, currentFacialEmotionColor, t);
			}

			if (gameManagerScript.useWordSentimentEmotion)
			{
				// Update the word sentiment emotion bar
//				wordSentimentEmotionBar.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(previousWordSentimentEmotionBarWidth, currentWordSentimentEmotionBarWidth, t),
//					wordSentimentEmotionBar.rectTransform.sizeDelta.y);
//				wordSentimentEmotionBar.color = Color.Lerp(previousWordSentimentEmotionColor, currentWordSentimentEmotionColor, t);	
			}
			mainCamera.backgroundColor = Color.Lerp(previousBackground, currentBackground, t);
			t += Time.deltaTime / lerpTime;

			yield return new WaitForEndOfFrame();
		}
	}

	/////////////////////////////////////////// SET MOOD TRACKER ATTRIBUTES  START //////////////////////////////////////////////////////
	[HideInInspector] public Vector4 normalizedMoodTrackerCoordinates;
	[HideInInspector] public Vector3 moodTrackerSize;
	[HideInInspector] public Color moodTrackerColor;

	// Define moodtracker scaling and offset by hit-and-trial
	[Space(10)]
	private float offsetX = 0.1f;
	private float offsetY = 0.1f;
	private float scaleXpercent  = 0.1f;
	private float scaleYpercent  = 0.1f;

	// Update the colors on-screen every X seconds
	public float colorUpdateTime = 0.5f;
	public float lerpTime = 0.25f;
	[Space(10)]

	private Color currentEmotionColor;
	private Color previousEmotionColor;

	public void SetMoodTrackerGeometry (Vector4 moodTrackerCoordinates){

		float flipTrackerX = flipHorizontal ? -1f : 1f;
		float flipTrackerY = flipVertical ? 1f : -1f;

		float xValue = moodTrackerCoordinates.x;
		float yValue = moodTrackerCoordinates.y;
		float interOcularDistance = moodTrackerCoordinates.z;
		float scale = moodTrackerCoordinates.w;

		// Debug.Log ("xValue: " + xValue + " yValue: " + yValue + " IOD: " + interOcularDistance);
		// Mapping - Camera feed to Mixed Reality Worldspace
		// Offset X/Y to make cube appear above face and 
		// incline towards a side(20% or 40% screen width or height)
		// Account for Horizontal flip and Vertical flip
		// Works good for width = 1280 and height = 720

		// Mapping detected facial coordinates to Worldspace
		float originX = feedWidth / 2f;
		float originY = feedHeight / 2f;

		float recenterX = flipTrackerX * (xValue - originX);
		float recenterY = flipTrackerY * (yValue - originY);

		// Normalizing final Coordinates
		float scaleX = scaleXpercent * feedWidth;
		float scaleY = scaleYpercent * feedHeight;

		float offsetXpercent = offsetX * interOcularDistance / originX;
		float offsetYpercent = offsetY * interOcularDistance / originY;

		float normalizeX = (recenterX / scaleX) + offsetXpercent;
		float normalizeY = (recenterY / scaleY) + offsetYpercent;

		// Assigning values
		normalizedMoodTrackerCoordinates.x = normalizeX;
		normalizedMoodTrackerCoordinates.y = normalizeY;
		normalizedMoodTrackerCoordinates.z = 10f;
		normalizedMoodTrackerCoordinates.w = scale / 100f;


	}

	public Color GetMoodTrackerColor(){
		return currentEmotionColor;
	}
	public Material GetMoodTrackerEmoji(){
		return currentEmotionEmoji;
	}
	public Material GetMoodTrackerEmoji2(){
		return currentEmotionEmoji2;
	}
	public float GetRotation(){
		return rotationX;
	}
	public float GetRotation2(){
		return rotationX2;
	}
	public void SetEmojiOrientation(Quaternion orientation){
		orientation.eulerAngles = new Vector3 (rotationX-orientation.eulerAngles.y,180f+orientation.eulerAngles.y,-1f*orientation.eulerAngles.z);
		emojiOrientation = orientation;
	}
	/////////////////////////////////////////// SET MOOD TRACKER ATTRIBUTES  END ////////////////////////////////////////////////////////

//////////////////////////////////////////////// SET CAMERA FEED  START /////////////////////////////////////////////////////////////
	// Configure Webcam output object
	[Space(10)]
	public float displayHeight = 0.54f;
	private bool flipHorizontal = true;
	public bool flipVertical = true;

	private Renderer quadRenderer;
	private float feedWidth;
	private float feedHeight;
	private bool camReady;

	private Renderer frontRenderer;
	private float frontFeedWidth;
	private float frontFeedHeight;
	private bool frontcamReady;

	public void SetFeed (){

		float flipDisplayX = flipHorizontal ? 1f : -1f;
		float flipDisplayY = flipVertical ? 1f : -1f;

		// Set the webcam-Render-Quad to have the same aspect ratio as the video feed
		float aspectRatio = feedWidth / feedHeight;
		webcamRenderQuad.transform.localScale = new Vector3 (-10*flipDisplayX*aspectRatio*displayHeight, -10*flipDisplayY*displayHeight, 1.0f);


		Debug.Log (" Feed Width: " + feedWidth + " Feed Height: " + feedHeight + " Aspect Ratio: " + aspectRatio);

		//New code
		//For setting up Cam Quad Display
		Texture2D targetTexture = new Texture2D((int)feedWidth, (int)feedHeight, TextureFormat.BGRA32, false);
		quadRenderer.material.mainTexture = targetTexture;

		float frontFlipDisplayX = flipHorizontal ? 1f : -1f;
		float frontFlipDisplayY = flipVertical ? 1f : -1f;

		// Set the webcam-Render-Quad to have the same aspect ratio as the video feed
		float frontaspectRatio = frontFeedWidth / frontFeedHeight;
		frontcamRenderQuad.transform.localScale = new Vector3 (-10*flipDisplayX*frontaspectRatio*displayHeight, -10*flipDisplayY*displayHeight, 1.0f);


//		Debug.Log (" Feed Width: " + feedWidth + " Feed Height: " + feedHeight + " Aspect Ratio: " + aspectRatio);

		//New code
		//For setting up Cam Quad Display
		Texture2D frontTargetTexture = new Texture2D((int)frontFeedWidth, (int)frontFeedHeight, TextureFormat.BGRA32, false);
	
		frontRenderer.material.mainTexture = frontTargetTexture;

	}

	public float getStrongestHue(float[] dist){
		int idx = -1;
		float[] hues = { 1f, 60f / 360f, 120f / 360f, 180f/ 360f, 240f / 360f, 300f / 360f };
		float max = float.MinValue;
		for (int i = 0; i < dist.Length; ++i) {
			if (dist[i] > max) {
				max = dist[i];
				idx = i;
			}
		}
		return hues[idx];

//		float hue = 0f;
//		for (int i = 0; i < dist.Length; ++i) 
//		{
//			Debug.Log (i);
//			Debug.Log (dist [i]);
//			hue += hues [i] * dist [i];
//		}
//		Debug.Log (hue*360f);
//		return hue / 100f;
	}
	public string getStrongestEmotion(float[] dist){
		int idx = -1;
		string[] emotions = { "anger", "fear", "joy", "surprise", "sadness", "disgust" };
		float max = float.MinValue;
		for (int i = 0; i < dist.Length; ++i) {
			if (dist[i] > max) {
				max = dist[i];
				idx = i;
			}
		}
		if (max > 10.0f) {
			return emotions [idx];
		}
		else{
			return "neutral";
			}
	}
	public string getStrongestEmotion2(EmotionStruct emotions){
		int idx = -1;
		string[] emotionNames = { "anger", "fear", "joy", "surprise", "sadness", "disgust" };
		float[] emotionVals = {
			emotions.anger,
			emotions.fear,
			emotions.joy,
			emotions.surprise,
			emotions.sadness,
			emotions.disgust
		};
		float max = float.MinValue;
		for (int i = 0; i < emotionNames.Length; ++i) {
			if (emotionVals[i] > max) {
				max = emotionVals[i];
				idx = i;
			}
		}
		if (max > 10.0f) {
			return emotionNames[idx];
		}
		else{
			return "neutral";
		}
	}
	public float getStrongestEmotionVal(float[] dist){
		float max = float.MinValue;
		for (int i = 0; i < dist.Length; ++i) {
			if (dist[i] > max) {
				max = dist[i];
			}
		}
		return max;
	}


	public float sum(float[] array){
		float sum = 0f;
		for (int i = 0; i < array.Length; i++) {
			sum += array [i];
		}
		return sum;
	}
///////////////////////////////////////////////// SET CAMERA FEED  END //////////////////////////////////////////////////////////////

		

}
