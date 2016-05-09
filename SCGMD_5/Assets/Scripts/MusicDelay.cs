using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class test : System.Object {

	public int furz;

	public float rülps;

	public bool arsch() {
		return true;
	}
}

public class MusicDelay : MonoBehaviour {
	public AudioSource audioSource;

	public int playDelay = 5;

	public test objectf;

	// Use this for initialization
	void Start () {
		objectf = new test();
		audioSource.timeSamples = playDelay * audioSource.clip.frequency;
		audioSource.Play ();
		Debug.Log (audioSource.clip.frequency * audioSource.clip.length);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
