using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnNotes : MonoBehaviour, AudioProcessor.AudioCallbacks {
	public List<GameObject> Notes;

	public List<GameObject> Spawns;


	private AudioProcessor processor;

	// Use this for initialization
	void Start () {
		processor = FindObjectOfType<AudioProcessor>();
		processor.addAudioCallback(this);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void onOnbeatDetected(){
		processor.tapTempo ();

		int note = Random.Range (0, 4);

		Instantiate (Notes [note], Spawns [note].transform.position, Notes [note].transform.rotation);

		if (Random.Range (0, 100) == 1) {
			if (note == 3) {
				Instantiate (Notes [note - 1], Spawns [note - 1].transform.position, Notes [note - 1].transform.rotation);
			} else {
				Instantiate (Notes [note + 1], Spawns [note + 1].transform.position, Notes [note + 1].transform.rotation);
			}
		}
	}

	//This event will be called every frame while music is playing
	public void onSpectrum(float[] spectrum){
		//The spectrum is logarithmically averaged
		//to 12 bands

		for (int i = 0; i < spectrum.Length; ++i)
		{
			Vector3 start = new Vector3(i, 0, 0);
			Vector3 end = new Vector3(i, spectrum[i], 0);
			Debug.DrawLine(start, end);
		}
	}

	private enum SpawnObjects {RedSpawn, BlueSpawn, GreenSpawn, YellowSpawn}
}
