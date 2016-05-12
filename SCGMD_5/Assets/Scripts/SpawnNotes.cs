using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

public class SpawnNotes : MonoBehaviour, AudioProcessor.AudioCallbacks {
	public List<GameObject> Notes;

	public List<GameObject> Spawns;

	public int selectedSong;


	private AudioClip song;

	private AudioSource audioSource;

	private AudioProcessor processor;

	private string songPath;
	private string songName;

	private int[,] notes;

	private int position, oldPosition;

	void Awake() {
		audioSource = GetComponent<AudioSource> ();

		this.LoadSongPath (selectedSong);
		this.LoadSong (selectedSong);
	}

	// Use this for initialization
	void Start () {
//		processor = FindObjectOfType<AudioProcessor>();
//		processor.addAudioCallback(this);

		int position = 0;
		int oldPosition = 0;

		audioSource.Play ();
	}
	
	// Update is called once per frame
	void Update () {
		position = audioSource.timeSamples / (audioSource.clip.frequency / 10);

		if (position > oldPosition) {
			for (int i = 0; i < 4; ++i) {
				if (notes [position, i] == 0) {
					Instantiate (Notes [i], Spawns [i].transform.position, Notes [i].transform.rotation);
				}
			}
		}

		oldPosition = position;
	}

	public void onOnbeatDetected(){
		processor.tapTempo ();

		int note = UnityEngine.Random.Range (0, 4);

		Instantiate (Notes [note], Spawns [note].transform.position, Notes [note].transform.rotation);

		if (UnityEngine.Random.Range (0, 100) == 1) {
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

	private void LoadSongPath(int song) {
		if (File.Exists (Application.persistentDataPath + "/song_list.dat")) {
			BinaryFormatter binaryFormatter = new BinaryFormatter ();
			SongListContainer songListContainer;

			FileStream file = File.Open (Application.persistentDataPath + "/song_list.dat", FileMode.Open);
			{
				songListContainer = (SongListContainer)binaryFormatter.Deserialize (file);
			}
			file.Close ();

			songPath = songListContainer.songPath;
			songPath = songPath.Replace ("Assets/Resources/", "");

			songName = songListContainer.names [song];

			songPath += "/" + songName;
		}
	}

	private void LoadSong(int song) {
		audioSource.clip = Resources.Load (songPath) as AudioClip;

		if (File.Exists (Application.persistentDataPath + "/" + songName + "_notes.dat")) {
			BinaryFormatter binaryFormatter = new BinaryFormatter ();
			NoteContainer noteContainer;
			
			FileStream file = File.Open (Application.persistentDataPath + "/" + songName + "_notes.dat", FileMode.Open);
			{
				noteContainer = (NoteContainer)binaryFormatter.Deserialize (file);
			}
			file.Close ();
			
			notes = new int[noteContainer.notes.GetLength (0), noteContainer.notes.GetLength (1)];

			for (int i = 0; i < noteContainer.notes.GetLength (0); ++i) {
				for (int j = 0; j < noteContainer.notes.GetLength (1); ++j) {
					notes [i, j] = noteContainer.notes [i, j];
				}
			}
		}
	}

	private enum SpawnObjects {RedSpawn, BlueSpawn, GreenSpawn, YellowSpawn}
}
