using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

	public GameObject gameManager;
	public GameObject uiManager;
	private UIManager uiManagerScript;

	public GameObject facialEmotionAnalyzerObject;
	public GameObject facialEmotionAnalyzer2Object;
	public GameObject wordSentimentEmotionAnalyzerObject;
	public GameObject vocalEmotionAnalyzerObject;

	private FacialEmotionAnalyzer facialAnalyzer;
	private FacialEmotionAnalyzer2 facialAnalyzer2;
	private SentimentAnalyzer wordAnalyzer;
	// private VocalEmotionAnalyzer vocalAnalyzer;
	private MicControlC vocalAnalyzer;

	// Flags for enabling and disabling certain emotion analysis features
	public bool useFacialEmotion = false;
	public bool useWordSentimentEmotion = false;
	public bool useVocalToneEmotion = false;

	public bool useBasicEmotions = false;
	public bool useAugmentedBasicEmotions = false;
	public bool useDimensionalEmotionsAV = false;

	public EmotionStruct currentFacialEmotion = new EmotionStruct();
	public EmotionStruct currentFacialEmotion2 = new EmotionStruct();
	public EmotionStruct currentWordSentimentEmotion = new EmotionStruct();
	public ToneAnalysis currentVocalEmotion = new ToneAnalysis();
	public FACSStruct currentFACS = new FACSStruct();
	public FACSStruct currentFACS2 = new FACSStruct();
	private float emotionThreshold = 10.0f;

	void Awake()
	{
		
	}

	IEnumerator passAudio()
	{
		while (true)
		{
			vocalAnalyzer.SetAudClip(wordAnalyzer.GetAudClip());
			yield return new WaitForSeconds(1.0f);
		}
	}

	// Use this for initialization
	void Start () {

		uiManagerScript = uiManager.GetComponent<UIManager> ();

		// Initialize the current emotion
		// currentFacialEmotion = new EmotionStruct();
		// currentWordSentimentEmotion = new EmotionStruct();
		// currentVocalEmotion = new ToneAnalysis();

		// Enable and disable emotion modality game objects based on the flags
		// if (useFacialEmotion)
		// {
		// 	facialEmotionAnalyzerObject.SetActive(false);
		// }

		// if (useWordSentimentEmotion)
		// {
		// 	wordSentimentEmotionAnalyzerObject.SetActive(false);
		// }

		// if (useVocalToneEmotion)
		// {
		// 	vocalEmotionAnalyzerObject.SetActive(false);
		// }

		// // Find the script for facial emotion analysis
		// try
		// {
		// 	facialAnalyzer = (FacialEmotionAnalyzer) facialEmotionAnalyzerObject.GetComponent(typeof(FacialEmotionAnalyzer)); // this seems to fail silently...
		// }
		// catch (System.Exception)
		// {
		// 	Debug.Log("Unable to find facial emotion analyzer. This functionality will be disabled.");
		// 	useFacialEmotion = false;
		// }

		// // Find the script for sentiment analysis of speech to text results
		// try
		// {
		// 	wordAnalyzer = (SentimentAnalyzer) wordSentimentEmotionAnalyzerObject.GetComponent(typeof(SentimentAnalyzer));
		// }
		// catch (System.Exception)
		// {
		// 	Debug.Log("Unable to find the sentiment analyzer. This functionality will be disabled.");
		// 	useWordSentimentEmotion = false;
		// }
		// // Find the script for vocal analysis results
		// try
		// {
		// 	vocalAnalyzer = (MicControlC) vocalEmotionAnalyzerObject.GetComponent(typeof(MicControlC));
		// }
		// catch (System.Exception)
		// {
		// 	Debug.Log("Unable to find the vocal analyzer. This functionality will be disabled.");
		// 	useVocalToneEmotion = false;
		// }


		if (!useFacialEmotion)
		{
			facialEmotionAnalyzerObject.SetActive(false);
		}
		else
		{
			facialAnalyzer = (FacialEmotionAnalyzer) facialEmotionAnalyzerObject.GetComponent(typeof(FacialEmotionAnalyzer));
			facialAnalyzer2 = (FacialEmotionAnalyzer2) facialEmotionAnalyzer2Object.GetComponent(typeof(FacialEmotionAnalyzer2));
			Debug.Log (facialAnalyzer2);
		}

		if (!useWordSentimentEmotion)
		{
			wordSentimentEmotionAnalyzerObject.SetActive(false);
		}
		else
		{
			wordAnalyzer = (SentimentAnalyzer) wordSentimentEmotionAnalyzerObject.GetComponent(typeof(SentimentAnalyzer));
		}

		if (!useVocalToneEmotion)
		{
			vocalEmotionAnalyzerObject.SetActive(false);
		}
		else
		{
			vocalAnalyzer = (MicControlC) vocalEmotionAnalyzerObject.GetComponent(typeof(MicControlC));
			StartCoroutine (passAudio ());
		}
	}
	
	// Update is called once per frame
	void Update () {
		// Pull in the most recent emotional state for each of the modalities
		if (useFacialEmotion)
		{
			currentFacialEmotion = facialAnalyzer.getCurrentEmotions();
			currentFacialEmotion2 = facialAnalyzer2.getCurrentEmotions();
			currentFACS = facialAnalyzer.getCurrentFACS ();
			currentFACS2 = facialAnalyzer2.getCurrentFACS ();
			// Debug.Log("got facial emotion struct");
		}
			
		if (useWordSentimentEmotion)
		{
			currentWordSentimentEmotion = wordAnalyzer.getCurrentEmotions();
		}
			
		if (useVocalToneEmotion)
		{
			currentVocalEmotion = vocalAnalyzer.getVocalToneResults();
		}

		//Set mood tracker attributes
		CalculateMoodTrackerGeometry ();
	}

	public EmotionStruct getCurrentFacialEmotion()
	{
		return currentFacialEmotion;
	}

	public EmotionStruct getCurrentFacialEmotion2()
	{
		return currentFacialEmotion2;
	}

	public FACSStruct getCurrentFACS()
	{
		return currentFACS;
	}

	public FACSStruct getCurrentFACS2()
	{
		return currentFACS2;
	}

	public EmotionStruct getCurrentWordSentimentEmotion()
	{
		return currentWordSentimentEmotion;
	}

	public ToneAnalysis getCurrentVocalEmotion()
	{
		return currentVocalEmotion;
	}

	// Returns the strongest emotion present in the current cumulative emotion
	public float getValueOfStrongestEmotion(EmotionStruct emotions)
	{
		if (emotions.joy > emotions.fear &&
			emotions.joy > emotions.disgust &&
			emotions.joy > emotions.sadness && 
			emotions.joy > emotions.anger && 
			emotions.joy > emotions.surprise &&
			emotions.joy > emotionThreshold)
		{
			return emotions.joy;
		}
		else if (emotions.fear > emotions.disgust &&
				 emotions.fear > emotions.sadness &&
				 emotions.fear > emotions.anger &&
				 emotions.fear > emotions.surprise &&
				 emotions.fear > emotionThreshold)
		{
			return emotions.fear;
		}
		else if (emotions.disgust > emotions.sadness &&
				 emotions.disgust > emotions.anger &&
				 emotions.disgust > emotions.surprise &&
				 emotions.disgust > emotionThreshold)
		{
			return emotions.disgust;
		}
		else if (emotions.sadness > emotions.anger &&
				 emotions.sadness > emotions.surprise &&
				 emotions.sadness > emotionThreshold)
		{
			return emotions.sadness;
		}
		else if (emotions.anger > emotions.surprise &&
				 emotions.anger > emotionThreshold)
		{
			return emotions.anger;
		}
		else if(emotions.surprise > emotionThreshold)
		{
			return emotions.surprise;
		}
		else
		{
			return 0.0f;
		}
	}

	// Returns the strongest emotion present in the current cumulative emotion
	public string getValueOfStrongestEmotionString(EmotionStruct emotions)
	{
		if (emotions.joy > emotions.fear &&
			emotions.joy > emotions.disgust &&
			emotions.joy > emotions.sadness && 
			emotions.joy > emotions.anger && 
			emotions.joy > emotions.surprise &&
			emotions.joy > emotionThreshold)
		{
			return "joy";
		}
		else if (emotions.fear > emotions.disgust &&
			emotions.fear > emotions.sadness &&
			emotions.fear > emotions.anger &&
			emotions.fear > emotions.surprise &&
			emotions.fear > emotionThreshold)
		{
			return "fear";
		}
		else if (emotions.disgust > emotions.sadness &&
			emotions.disgust > emotions.anger &&
			emotions.disgust > emotions.surprise &&
			emotions.disgust > emotionThreshold)
		{
			return "disgust";
		}
		else if (emotions.sadness > emotions.anger &&
			emotions.sadness > emotions.surprise &&
			emotions.sadness > emotionThreshold)
		{
			return "sadness";
		}
		else if (emotions.anger > emotions.surprise &&
			emotions.anger > emotionThreshold)
		{
			return "anger";
		}
		else if(emotions.surprise > emotionThreshold)
		{
			return "surprise";
		}
		else
		{
			return "neutral";
		}
	}

	// Returns a color to be used by the user interface based on the current synthesized emotion
	public Color calculateEmotionColor(EmotionStruct emotions)
	{
		if (emotions.joy > emotions.fear &&
			emotions.joy > emotions.disgust &&
			emotions.joy > emotions.sadness && 
			emotions.joy > emotions.anger && 
			emotions.joy > emotions.surprise &&
			emotions.joy > emotionThreshold)
		{
			return new Color(0.0f, 1.0f, 0.0f, 1.0f);		// green
		}
		else if (emotions.fear > emotions.disgust &&
				 emotions.fear > emotions.sadness &&
				 emotions.fear > emotions.anger &&
				 emotions.fear > emotions.surprise &&
				 emotions.fear > emotionThreshold)
		{
			return new Color(1.0f, 0.0f, 1.0f, 1.0f);		// magenta
		}
		else if (emotions.disgust > emotions.sadness &&
				 emotions.disgust > emotions.anger &&
				 emotions.disgust > emotions.surprise &&
				 emotions.disgust > emotionThreshold)
		{
			return new Color(1.0f, 1.0f, 0.0f, 1.0f);		// yellow
		}
		else if (emotions.sadness > emotions.anger &&
				 emotions.sadness > emotions.surprise &&
				 emotions.sadness > emotionThreshold)
		{
			return new Color(0.0f, 0.0f, 1.0f, 1.0f);		// blue
		}
		else if (emotions.anger > emotions.surprise &&
				 emotions.anger > emotionThreshold)
		{
			return new Color(1.0f, 0.0f, 0.0f, 1.0f);		// red
		}
		else if(emotions.surprise > emotionThreshold)
		{
			return new Color(1.0f, 1.0f, 1.0f, 1.0f);		// white
		}
		else
		{
			return new Color(0.0f, 0.0f, 0.0f, 1.0f);		// black
		}
	}

	public float[] calculateEmotionHueDist(EmotionStruct emotions){
		float[] hueDist = {emotions.anger, emotions.fear,  emotions.joy, emotions.surprise, emotions.sadness, emotions.disgust};
		return hueDist;
	}

////////////////////////////////////// SET/CALCULATE MOOD TRACKER COORDINATES START /////////////////////////////////////////////////
	private void CalculateMoodTrackerGeometry (){
		Vector4 moodTrackerCoordinates = facialAnalyzer.GetMoodTrackerGeometry ();
		uiManagerScript.SetMoodTrackerGeometry (moodTrackerCoordinates);
		uiManagerScript.SetEmojiOrientation (facialAnalyzer.GetHeadOrientation ());
	}
/////////////////////////////////////// SET/CALCULATE MOOD TRACKER COORDINATES END //////////////////////////////////////////////////

}
