﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using SimpleJSON;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
//#if UNITY_EDITOR
//using System.IO;
//#endif


public class MicControlC : MonoBehaviour {



	//the variables that do the users magic

	public string[] audBySec = new string[10];
	public string tokenUrl = "https://token.beyondverbal.com/token";
	private string apiKey = "78c4e354-8573-4ac8-95a3-980b478ac833"; //"322360d1-236c-4902-bb9c-1ce56fb84578";
	private string startUrl = "https://apiv5.beyondverbal.com/v5/recording/";
	public string wavFile;
	public string analysisUrl;
	public string requestData;
	public string token;
	public string startResponseString;
	public string recordingId;
	public JSONNode currentAnalysis;
	public AudioClip audClip;
	public AudioClip audBuffer;

	private ToneAnalysis vocalToneResults = new ToneAnalysis ();
	private float timeIdx = 1.0f;

	public bool doNotDestroyOnLoad=false;

	public ToneAnalysis getVocalToneResults(){
		return vocalToneResults;
	}

	void Start () {
		requestData = "apiKey=" + apiKey + "&grant_type=client_credentials";

		token = authRequest(tokenUrl, Encoding.UTF8.GetBytes(requestData));
		//start
		//		Debug.Log(token);
		startResponseString = CreateWebRequest(startUrl + "start", Encoding.UTF8.GetBytes("{ dataFormat: { type: \"WAV\" } }"), token);
		//		Debug.Log (startResponseString);
		var startResponseObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(startResponseString);
		if (startResponseObj["status"] != "success")
		{
			Debug.Log("Response Status: " + startResponseObj["status"]);
			return;
		}
		recordingId = startResponseObj["recordingId"];
		StartCoroutine (yieldedStart ());
	}

	private IEnumerator yieldedStart(){


		//wait till level is loaded
		yield return new WaitUntil(() => audClip != null);
		Debug.Log ("STARTING BEYOND VERBAL!!!");
		InvokeRepeating("RecordChunk", 1.0f, 1.0f);
//		InvokeRepeating("Analyze", 10.0f, 1.0f);
		yield return 0;
		//make this controller persistent
		if(doNotDestroyOnLoad){
			DontDestroyOnLoad (transform.gameObject);
		}

	}



	void RecordChunk(){
		if (!audBuffer) {
			audBuffer = AudioClip.Create ("audioBuffer", audClip.samples*10, audClip.channels, audClip.frequency, false);
		}
		SaveWavFile (audClip);
		float[] samples = new float[audClip.samples * audClip.channels];
		audClip.GetData(samples, 0);
		audBuffer.SetData (samples, Mathf.RoundToInt((timeIdx-1.0f) * audClip.frequency));
		if (timeIdx < 10.0f) {
			timeIdx += 1.0f;
		} else {
			Analyze ();
		}
	}

	void Analyze(){
		wavFile = SaveWavFile (audBuffer);
		analysisUrl = startUrl + recordingId;
		new Thread(() => 
			{
				Thread.CurrentThread.IsBackground = true; 
				/* run your code here */ 
				var bytes = File.ReadAllBytes(wavFile);
				Debug.Log ("POST 10");
				var analysisResponseString = CreateWebRequest(analysisUrl, bytes, token);
				Debug.Log ("Listen 10");
				currentAnalysis = JSON.Parse(analysisResponseString);
				startResponseString = CreateWebRequest(startUrl + "start", Encoding.UTF8.GetBytes("{ dataFormat: { type: \"WAV\" } }"), token);
				var startResponseObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(startResponseString);
				if (startResponseObj["status"] != "success")
				{
					Debug.Log("Response Status: " + startResponseObj["status"]);
					return;
				}
				vocalToneResults.TemperVal = Single.Parse(currentAnalysis["result"]["analysisSegments"][0]["analysis"]["Temper"]["Value"]);
				vocalToneResults.ArousalVal = Single.Parse(currentAnalysis["result"]["analysisSegments"][0]["analysis"]["Arousal"]["Value"]);
				vocalToneResults.ValenceVal = Single.Parse(currentAnalysis["result"]["analysisSegments"][0]["analysis"]["Valence"]["Value"]);
				vocalToneResults.TemperGroup = currentAnalysis["result"]["analysisSegments"][0]["analysis"]["Temper"]["Group"];
				vocalToneResults.ArousalGroup = currentAnalysis["result"]["analysisSegments"][0]["analysis"]["Arousal"]["Group"];
				vocalToneResults.ValenceGroup = currentAnalysis["result"]["analysisSegments"][0]["analysis"]["Valence"]["Group"];
				recordingId = startResponseObj["recordingId"];
				Debug.Log(vocalToneResults.TemperVal);
				Debug.Log(vocalToneResults.TemperGroup);
				Debug.Log(vocalToneResults.ArousalVal);
				Debug.Log(vocalToneResults.ArousalGroup);
				Debug.Log(vocalToneResults.ValenceVal);
				Debug.Log(vocalToneResults.ValenceGroup);

			}).Start();
		float[] samples = new float[audBuffer.samples * audBuffer.channels];
		audBuffer.GetData(samples, Mathf.RoundToInt((1.0f) * audClip.frequency));
		audBuffer.SetData (samples, 0);
	}




	//apply the mic input data stream to a float and or array;
	void Update () {

	}

	public static bool MyRemoteCertificateValidationCallback(System.Object sender,
		X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		bool isOk = true;
		// If there are errors in the certificate chain,
		// look at each error to determine the cause.
		if (sslPolicyErrors != SslPolicyErrors.None) {
			for (int i=0; i<chain.ChainStatus.Length; i++) {
				if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown) {
					continue;
				}
				chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
				chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
				chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan (0, 1, 0);
				chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
				bool chainIsValid = chain.Build ((X509Certificate2)certificate);
				if (!chainIsValid) {
					isOk = false;
					break;
				}
			}
		}
		return isOk;
	}

	private static string authRequest(string url, byte[] data)
	{
		JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
		HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
		request.Method = "POST";
		request.ContentType = "application/x-www-form-urlencoded";
		//		request.ServicePoint.SetTcpKeepAlive(false, 0, 0);
		request.ServicePoint.UseNagleAlgorithm = false;
		request.ReadWriteTimeout = 1000000;
		request.Timeout = 10000000;
		request.SendChunked = false;
		request.AllowWriteStreamBuffering = true;
		//		request.AllowReadStreamBuffering = false;
		request.KeepAlive = true;

		ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
		using (var requestStream = request.GetRequestStream())
		{
			requestStream.Write(data, 0, data.Length);
		}

		using (var response = request.GetResponse() as HttpWebResponse)
		using (var responseStream = response.GetResponseStream())
		using (var streamReader = new StreamReader(responseStream, Encoding.UTF8))
		{
			var res = streamReader.ReadToEnd();
			var responceContent = JsonConvert.DeserializeObject<Dictionary<string, string>>(res, jsonSerializerSettings);
			return responceContent["access_token"];

		}
	}

	private static string CreateWebRequest(string url, byte[] data, string token = null)
	{
		//		JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
		HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
		request.Method = "POST";
		request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

		request.KeepAlive = true;
		//		request.ServicePoint.SetTcpKeepAlive(true, 10000, 10000);

		request.Timeout = 10000000;
		request.SendChunked = true;
		request.AllowWriteStreamBuffering = true;
		//		request.AllowReadStreamBuffering = false;
		ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
		if (string.IsNullOrEmpty(token) == false)
			request.Headers.Add("Authorization", "Bearer " + token);

		using (var requestStream = request.GetRequestStream())
		{
			requestStream.Write(data, 0, data.Length);
		}

		using (var response = request.GetResponse() as HttpWebResponse)
		using (var responseStream = response.GetResponseStream())
		using (var streamReader = new StreamReader(responseStream, Encoding.UTF8))
		{
			return streamReader.ReadToEnd();
		}
	}
		
	public string SaveWavFile (AudioClip aud)
	{
		string filepath;
		WavUtility.FromAudioClip(aud, out filepath, true);
		return filepath;
	}


	public void SetAudClip(AudioClip externClip){
		Debug.Log("SET CLIP");
		audClip = externClip;
		Debug.Log (audClip);
	}
		


}