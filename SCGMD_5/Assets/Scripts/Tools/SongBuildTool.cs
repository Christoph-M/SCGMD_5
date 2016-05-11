using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;


public class ReadOnly : PropertyAttribute { }

[CustomPropertyDrawer(typeof(ReadOnly))]
public class ReadOnlyDrawer : PropertyDrawer {
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		GUI.enabled = false;
		{
			int i = label.text.IndexOf (" ");

			EditorGUIUtility.labelWidth = 45.0f;
			{
				EditorGUI.PropertyField (position, property, new GUIContent (label.text.Substring (i + 1)), true);
			}
			EditorGUIUtility.labelWidth = 150.0f;
		}
		GUI.enabled = true;
	}
}


[Serializable]
public class NoteContainer {
	public int[,] notes;

	public NoteContainer(int noteCount) {
		notes = new int[noteCount, 4];
	}
}


public class SongBuildTool : EditorWindow {
	[ReadOnly] public AudioClip[] availableSongs;
	[ReadOnly] public int[] sampleCount;
	[ReadOnly] public int[] noteCount;


	private Vector2 scrollPosition, scrollSongList;

	private string songPath;
	private ScriptableObject target;
	private SerializedObject so;
	private SerializedProperty availableSongsProperty, sampleCountProperty, noteCountProperty;

	private int notesPerSecond = 10;
	private int selectedSong;
	private int audioPosition, audioSamplePosition, audioNotePosition;
	private int longNoteLength;
	private int noteJump;

	private string playPauseLabel;


	private List<int[,]> notes;


	private int oldSelectedSong = 1;
	private int oldAudioPosition = 0;
	private int oldAudioSamplePosition = 0;
	private int oldAudioNotePosition = 0;



	private static Assembly unityEditorAssembly;
	private static System.Type audioUtilClass;
	private static MethodInfo playClip, isPlaying, getSamplePosition, setSamplePosition, pauseClip, resumeClip, stopAllClips;

	private static bool isPaused = false;



	[MenuItem ("Window/Song Editor")]
	public static void ShowWindow() {
		EditorWindow.GetWindow (typeof(SongBuildTool));
	}



	void OnEnable() {
		target = this;
		so = new SerializedObject (target);
		availableSongsProperty = so.FindProperty ("availableSongs");
		sampleCountProperty    = so.FindProperty ("sampleCount");
		noteCountProperty      = so.FindProperty ("noteCount");

		songPath = "Assets/Audio/Music";
		this.FindSongs ();

		selectedSong = 1;
		this.ResetAudioPosition ();

		longNoteLength = 5;
		noteJump = 1;

		playPauseLabel = "►";


		unityEditorAssembly = typeof(AudioImporter).Assembly;
		audioUtilClass = unityEditorAssembly.GetType ("UnityEditor.AudioUtil");

		playClip          = audioUtilClass.GetMethod ("PlayClip",              BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
		isPlaying         = audioUtilClass.GetMethod ("IsClipPlaying",         BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
		getSamplePosition = audioUtilClass.GetMethod ("GetClipSamplePosition", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
		setSamplePosition = audioUtilClass.GetMethod ("SetClipSamplePosition", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip), typeof(int) }, null);
		pauseClip         = audioUtilClass.GetMethod ("PauseClip",             BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
		resumeClip        = audioUtilClass.GetMethod ("ResumeClip",            BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
		stopAllClips      = audioUtilClass.GetMethod ("StopAllClips",          BindingFlags.Static | BindingFlags.Public, null, new System.Type[] {                                }, null);
	}

	void OnDisable() {
		StopAllClips ();
	}

	void OnInspectorUpdate() {
		if (oldSelectedSong != selectedSong) this.SwitchSong ();


		int action = 0;
		if (oldAudioPosition       != audioPosition)       action += 1;
		if (oldAudioSamplePosition != audioSamplePosition) action += 2;
		if (oldAudioNotePosition   != audioNotePosition)   action += 4;

		switch (action) {
			case 0:
				if (IsPlaying (availableSongs [selectedSong - 1]) && !isPaused) {
					audioPosition       = audioSamplePosition /  availableSongs [selectedSong - 1].frequency;
					audioSamplePosition = GetSamplePosition (availableSongs [selectedSong - 1]);
					audioNotePosition   = audioSamplePosition / (availableSongs [selectedSong - 1].frequency / notesPerSecond);
				} break;
			case 1:
				audioSamplePosition = audioPosition       *  availableSongs [selectedSong - 1].frequency;
				audioNotePosition   = audioSamplePosition / (availableSongs [selectedSong - 1].frequency / notesPerSecond); break;
			case 2: case 3: case 6: case 7:
				audioPosition       = audioSamplePosition /  availableSongs [selectedSong - 1].frequency;
				audioNotePosition   = audioSamplePosition / (availableSongs [selectedSong - 1].frequency / notesPerSecond); break;
			case 4: case 5:
				audioSamplePosition = audioNotePosition   * (availableSongs [selectedSong - 1].frequency / notesPerSecond);
				audioPosition       = audioSamplePosition /  availableSongs [selectedSong - 1].frequency; break;
		}

		if (action > 0) SetSamplePosition (availableSongs [selectedSong - 1], audioSamplePosition);


		oldSelectedSong        = selectedSong;
		oldAudioPosition       = audioPosition;
		oldAudioSamplePosition = audioSamplePosition;
		oldAudioNotePosition   = audioNotePosition;


		if (!IsPlaying(availableSongs[selectedSong - 1])) {
			isPaused = false;
			playPauseLabel = "►";
		}


		Repaint ();
	}

	void OnGUI () {
		scrollPosition = GUILayout.BeginScrollView (scrollPosition);
		{
			GUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button ("...", GUILayout.MaxWidth (30.0f))) {
					if (EditorUtility.DisplayDialog ("Choose music folder?", "Choosing a new music folder will reload all songs!\nAll unsaved data will be lost!", "Choose folder", "Cancel")) {
						string newSongPath = EditorUtility.OpenFolderPanel ("Choose music folder", "Assets/", "");

						if (newSongPath != "") {
							if (newSongPath.Contains (Application.dataPath)) {
								int i = newSongPath.IndexOf ("Assets");

								songPath = newSongPath.Substring (i);
						
								this.FindSongs ();
							} else {
								EditorUtility.DisplayDialog ("Error", "Chosen path is not in Assets folder", "OK");
							}
						}
					}
				}

				songPath = EditorGUILayout.TextField (songPath);
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();

				if (GUILayout.Button ("Update song list", GUILayout.MaxWidth (150.0f))) {
					if (EditorUtility.DisplayDialog ("Update song list?", "Updating the song list will reload all songs!\nAll unsaved data will be lost!", "Update", "Cancel")) {
						this.FindSongs ();
					}
				}

				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			so.Update ();
			scrollSongList = GUILayout.BeginScrollView (scrollSongList);
			{
				GUILayout.BeginHorizontal ();
				{
					EditorGUILayout.PropertyField (availableSongsProperty, true);
					EditorGUILayout.PropertyField (sampleCountProperty, true, GUILayout.MaxWidth (230.0f));
					EditorGUILayout.PropertyField (noteCountProperty, true, GUILayout.MaxWidth (200.0f));
				}
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();
			so.ApplyModifiedProperties ();

			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();

			EditorGUIUtility.labelWidth = 80.0f;
			{
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label("Song Select", GUILayout.MaxWidth(80.0f));
					EditorGUILayout.PropertyField (availableSongsProperty.GetArrayElementAtIndex(selectedSong - 1), new GUIContent(""));
					selectedSong = EditorGUILayout.IntSlider (selectedSong, 1, availableSongs.Length);
				}
				GUILayout.EndHorizontal ();
				audioPosition = EditorGUILayout.IntSlider ("Time:", audioPosition, 0, (int)availableSongs [selectedSong - 1].length);
				audioSamplePosition = EditorGUILayout.IntSlider ("Sample:", audioSamplePosition, 0, sampleCount [selectedSong - 1]);
			}
			EditorGUIUtility.labelWidth = 150.0f;

			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();

			for (int i = 0; i < 4; ++i) {
				this.DisplayNoteEditor (i);

				EditorGUILayout.Space ();
			}

			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();

				if (GUILayout.Button ("׀◄", GUILayout.MaxWidth (90.0f))) {
					if (audioNotePosition >= noteJump) {
						audioNotePosition -= noteJump;
					} else {
						this.ResetAudioPosition ();
					}
				}

				EditorGUIUtility.labelWidth = 55.0f;
				{
					audioNotePosition = EditorGUILayout.IntField ("Notes at:", audioNotePosition, GUILayout.MaxWidth (EditorGUIUtility.labelWidth + 35.0f));
				}
				EditorGUIUtility.labelWidth = 150.0f;

				if (GUILayout.Button ("►׀", GUILayout.MaxWidth (90.0f))) {
					if (audioNotePosition <= noteCount [selectedSong - 1] - noteJump) {
						audioNotePosition += noteJump;
					} else {
						audioNotePosition = noteCount [selectedSong - 1];
					}
				}

				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();

				if (GUILayout.Button (new GUIContent("Remove all notes", "Removes all notes in current note"), GUILayout.MaxWidth (204.0f))) {
					for (int i = 0; i < 4; ++i) {
						notes [selectedSong - 1] [audioNotePosition, i] = -1;
					}
				}
				GUILayout.Label ("", GUILayout.MaxWidth (200.0f));
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();

				EditorGUIUtility.labelWidth = 80.0f;
				{
					noteJump = EditorGUILayout.IntSlider ("Skip notes:", noteJump, 1, noteCount [selectedSong - 1], GUILayout.MaxWidth(278.0f));
				}
				EditorGUIUtility.labelWidth = 150.0f;

				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();

				if (GUILayout.Button (new GUIContent("Reset song", "Resets all notes in current song"), GUILayout.MaxWidth (100.0f))) {
					if (EditorUtility.DisplayDialog ("Reset " + availableSongs[selectedSong - 1].name + "?", "Are you sure you want to reset all notes in " + availableSongs[selectedSong - 1].name + "?", "Reset", "Cancel")) {
						this.ResetNotes (selectedSong - 1);

						this.ResetAudioPosition ();
					}
				}

				if (GUILayout.Button (new GUIContent("Reset all songs", "Resets all notes in all songs"), GUILayout.MaxWidth (100.0f))) {
					if (EditorUtility.DisplayDialog ("Reset all songs?", "Are you sure you want to reset all notes in all songs?", "Reset", "Cancel")) {
						for (int i = 0; i < availableSongs.Length; ++i) {
							this.ResetNotes (i);

							EditorUtility.DisplayProgressBar ("Resetting notes:", availableSongs [i].name, Mathf.InverseLerp (0, availableSongs.Length, i));
						}

						EditorUtility.ClearProgressBar ();

						this.ResetAudioPosition ();
					}
				}
				GUILayout.Label (GUI.tooltip, GUILayout.MaxWidth (200.0f));
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			EditorGUIUtility.labelWidth = 80.0f;
			{
				audioNotePosition = EditorGUILayout.IntSlider ("Note:", audioNotePosition, 0, noteCount [selectedSong - 1]);
			}
			EditorGUIUtility.labelWidth = 150.0f;

			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button ("Save song")) {
					this.SaveSong (selectedSong - 1);
				}

				if (GUILayout.Button ("Load song")) {
					if (EditorUtility.DisplayDialog ("Load " + availableSongs[selectedSong - 1].name + "?", "Are you sure you want to load " + availableSongs[selectedSong - 1].name + "?\nAll unsaved data will be lost!", "Load", "Cancel")) {
						this.LoadSong (selectedSong - 1);
					}
				}

				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();

				if (GUILayout.Button ("Save all")) {
					for (int i = 0; i < availableSongs.Length; ++i) {
						this.SaveSong (i);
						EditorUtility.DisplayProgressBar ("Saving notes", availableSongs[i].name, Mathf.InverseLerp(0, availableSongs.Length, i));
					}

					EditorUtility.ClearProgressBar ();
				}

				if (GUILayout.Button ("Load all")) {
					if (EditorUtility.DisplayDialog ("Load all songs?", "Are you sure you want to load all songs?\nAll unsaved data will be lost!", "Load", "Cancel")) {
						for (int i = 0; i < availableSongs.Length; ++i) {
							this.LoadSong (i);
							EditorUtility.DisplayProgressBar ("Loading notes", availableSongs [i].name, Mathf.InverseLerp (0, availableSongs.Length, i));
						}

						EditorUtility.ClearProgressBar ();
					}
				}
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button ("׀◄◄")) {
					if (audioNotePosition == 0) {
						if (selectedSong <= 1) {
							selectedSong = availableSongs.Length;
						} else {
							--selectedSong;
						}

						this.SwitchSong ();
					} else {
						this.ResetAudioPosition ();
					}
				}

				if (GUILayout.Button (playPauseLabel)) {
					PlayPauseSong (availableSongs [selectedSong - 1]);
					SetSamplePosition (availableSongs [selectedSong - 1], audioSamplePosition);

					if (isPaused) {
						playPauseLabel = "►";
					} else {
						playPauseLabel = "▌▌";
					}
				}

				if (GUILayout.Button ("■")) {
					StopAllClips ();

					this.ResetAudioPosition ();

					playPauseLabel = "►";
				}

				if (GUILayout.Button ("►►׀")) {
					if (audioNotePosition == noteCount [selectedSong - 1]) {
						if (selectedSong >= availableSongs.Length) {
							selectedSong = 1;
						} else {
							++selectedSong;
						}

						this.SwitchSong ();
					} else {
						audioNotePosition = noteCount [selectedSong - 1];
					}
				}
			}
			GUILayout.EndHorizontal ();
		}
		GUILayout.EndScrollView ();
	}

	private void DisplayNoteEditor(int i) {
		GUILayout.BeginHorizontal ();
		{
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			if (GUILayout.Button("Add note", GUILayout.MaxWidth (100.0f))) {
				notes [selectedSong - 1] [audioNotePosition, i] = 0;
			}

			if (GUILayout.Button("Add hold note", GUILayout.MaxWidth (100.0f))) {
				notes [selectedSong - 1] [audioNotePosition, i] = longNoteLength;
			}

			EditorGUIUtility.labelWidth = 50.0f;
			{
				longNoteLength = EditorGUILayout.IntField ("Length:", longNoteLength, GUILayout.MaxWidth (EditorGUIUtility.labelWidth + 50.0f));

				if (longNoteLength < 0) {
					longNoteLength = 2;
				} else if (longNoteLength > (noteCount [selectedSong - 1] - audioNotePosition)) {
					longNoteLength = noteCount [selectedSong - 1] - audioNotePosition;
				}
			}
			EditorGUIUtility.labelWidth = 150.0f;

			EditorGUILayout.Space ();

			EditorGUIUtility.labelWidth = 40.0f;
			{
				notes [selectedSong - 1] [audioNotePosition, i] = EditorGUILayout.IntField ("Note:", notes [selectedSong - 1] [audioNotePosition, i], GUILayout.MaxWidth (EditorGUIUtility.labelWidth + 50.0f));

				if (notes [selectedSong - 1] [audioNotePosition, i] < -1) {
					notes [selectedSong - 1] [audioNotePosition, i] = -1;
				} else if (notes [selectedSong - 1] [audioNotePosition, i] > (noteCount [selectedSong - 1] - audioNotePosition)) {
					notes [selectedSong - 1] [audioNotePosition, i] = noteCount [selectedSong - 1] - audioNotePosition;
				}

				EditorGUILayout.LabelField (" ", GUILayout.MaxWidth (50.0f));
			}
			EditorGUIUtility.labelWidth = 150.0f;

			if (GUILayout.Button("Remove note", GUILayout.MaxWidth (100.0f))) {
				notes [selectedSong - 1] [audioNotePosition, i] = -1;
			}

			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
		}
		GUILayout.EndHorizontal ();
	}



	private void FindSongs() {
		string[] guids = AssetDatabase.FindAssets ("t:AudioClip", new string[] { songPath });

		if (guids.Length > 0) {
			availableSongs = new AudioClip[guids.Length];
			sampleCount = new int[guids.Length];
			noteCount = new int[guids.Length];

			for (int i = 0; i < guids.Length; ++i) {
				availableSongs [i] = AssetDatabase.LoadAssetAtPath (AssetDatabase.GUIDToAssetPath (guids [i]), typeof(AudioClip)) as AudioClip;
				EditorUtility.DisplayProgressBar ("Loading songs", "Loaded: " + availableSongs[i].name, Mathf.InverseLerp(0, availableSongs.Length, i));

				sampleCount [i] = availableSongs [i].samples;
				noteCount [i] = sampleCount [i] / (availableSongs [i].frequency / notesPerSecond);
			}

			EditorUtility.ClearProgressBar ();
			
			this.CreateNotes ();
		} else {
			EditorUtility.DisplayDialog ("Error", "Selected folder does not contain any AudioClips", "OK");
		}
	}

	private void CreateNotes() {
		notes = new List<int[,]>();

		for (int i = 0; i < availableSongs.Length; ++i) {
			notes.Add (new int[noteCount [i] + 1, 4]);

			if (this.CheckSave (i)) {
				this.LoadSong (i);
			} else {
				this.ResetNotes (i);
			}
		}
	}

	private void ResetNotes(int i) {
		for (int j = 0; j < notes [i].GetLength (0); ++j) {
			for (int k = 0; k < notes [i].GetLength (1); ++k) {
				notes [i] [j, k] = -1;
			}

			if (j % 4 == 0) EditorUtility.DisplayProgressBar ("Resetting notes", availableSongs [i].name, Mathf.InverseLerp (0, notes [i].GetLength (0), j));
		}

		EditorUtility.ClearProgressBar ();
	}

	private void SwitchSong() {
		this.ResetAudioPosition ();

		if (IsPlaying (availableSongs [selectedSong - 1])) {
			StopAllClips ();

			PlayPauseSong (availableSongs [selectedSong - 1]);
		}
	}

	private void ResetAudioPosition() {
		audioPosition = 0;
		audioSamplePosition = 0;
		audioNotePosition = 0;
	}


	public void SaveSong(int song) {
		BinaryFormatter binaryFormatter = new BinaryFormatter ();

		FileStream file = File.Open (Application.persistentDataPath + "/" + availableSongs [song].name + "_notes.dat", FileMode.Create);
		{
			NoteContainer noteContainer = new NoteContainer (noteCount[song] + 1);

			for (int i = 0; i < notes [song].GetLength (0); ++i) {
				for (int j = 0; j < notes [song].GetLength (1); ++j) {
					noteContainer.notes [i, j] = notes [song] [i, j];
				}

				if (i % 4 == 0) EditorUtility.DisplayProgressBar ("Saving notes", availableSongs [song].name, Mathf.InverseLerp (0, notes [song].GetLength (0), i));
			}

			EditorUtility.ClearProgressBar ();

			binaryFormatter.Serialize (file, noteContainer);
		}
		file.Close ();
	}

	public void LoadSong(int song) {
		if (this.CheckSave(song)) {
			BinaryFormatter binaryFormatter = new BinaryFormatter ();
			NoteContainer noteContainer;

			FileStream file = File.Open (Application.persistentDataPath + "/" + availableSongs [song].name + "_notes.dat", FileMode.Open);
			{
				noteContainer = (NoteContainer)binaryFormatter.Deserialize (file);
			}
			file.Close ();

			for (int i = 0; i < noteContainer.notes.GetLength (0); ++i) {
				for (int j = 0; j < noteContainer.notes.GetLength (1); ++j) {
					notes [song] [i, j] = noteContainer.notes [i, j];
				}

				if (i % 4 == 0) EditorUtility.DisplayProgressBar ("Loading notes", availableSongs [song].name, Mathf.InverseLerp (0, noteContainer.notes.GetLength (0), i));
			}

			EditorUtility.ClearProgressBar ();
		}
	}

	public bool CheckSave(int song) {
		return File.Exists (Application.persistentDataPath + "/" + availableSongs [song].name + "_notes.dat");
	}



	public static void PlayPauseSong(AudioClip clip) {
		if (!IsPlaying (clip)) {
			playClip.Invoke (null, new object[] { clip });
		} else if (!isPaused) {
			isPaused = true;
			pauseClip.Invoke (null, new object[] { clip });
		} else {
			isPaused = false;
			resumeClip.Invoke (null, new object[] { clip });
		}
	}
	
	public static bool IsPlaying (AudioClip clip) {
		return (bool)isPlaying.Invoke (null, new object[] { clip });
	}

	public static int GetSamplePosition(AudioClip clip) {
		return (int)getSamplePosition.Invoke (null, new object[] { clip });
	}

	public static void SetSamplePosition(AudioClip clip, int pos) {
		setSamplePosition.Invoke (null, new object[] { clip, pos });
	}

	public static void StopAllClips() {
		if (isPaused) isPaused = false;
		stopAllClips.Invoke (null, new object[] {});
	}
}
