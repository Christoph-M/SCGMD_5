using UnityEngine;
using System.Collections;

public class MusicDelay : MonoBehaviour {
	public AudioSource audioSource;

	public int playDelay = 5;

	// Use this for initialization
	void Start () {
		audioSource.timeSamples = playDelay * audioSource.clip.frequency;
		audioSource.Play ();
		Debug.Log (audioSource.clip.frequency * audioSource.clip.length);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
