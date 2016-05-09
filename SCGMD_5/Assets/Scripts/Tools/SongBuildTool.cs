using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class ReadOnly : PropertyAttribute { }

[CustomPropertyDrawer(typeof(ReadOnly))]
public class ReadOnlyDrawer : PropertyDrawer {
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		GUI.enabled = false;
		EditorGUI.PropertyField (position, property, label, true);
		GUI.enabled = true;
	}
}

public class SongBuildTool : EditorWindow {
	[ReadOnly] public AudioClip[] availableSongs;
	[ReadOnly] public int[] sampleCount;
	[ReadOnly] public int[] noteCount;


	private Vector2 scrollPosition;

	private string songPath;
	private ScriptableObject target;
	private SerializedObject so;
	private SerializedProperty availableSongsProperty, sampleCountProperty, noteCountProperty;

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



	[MenuItem ("Window/SongEditor")]
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
		bool isPlaying = IsPlaying (availableSongs [selectedSong - 1]) && !isPaused;

		if (oldSelectedSong != selectedSong) this.SwitchSong ();

		if (oldAudioPosition == audioPosition) {
			if (isPlaying) audioPosition = GetSamplePosition (availableSongs [selectedSong - 1]) / availableSongs [selectedSong - 1].frequency;
		} else {
			audioSamplePosition = audioPosition * availableSongs [selectedSong - 1].frequency;
			audioNotePosition = audioSamplePosition / 10000;

			SetSamplePosition (availableSongs [selectedSong - 1], audioSamplePosition);
		}

		if (oldAudioSamplePosition == audioSamplePosition) {
			if (isPlaying) audioSamplePosition = GetSamplePosition (availableSongs [selectedSong - 1]);
		} else {
			audioPosition = audioSamplePosition / availableSongs [selectedSong - 1].frequency;
			audioNotePosition = audioSamplePosition / 10000;

			SetSamplePosition (availableSongs [selectedSong - 1], audioSamplePosition);
		}

		if (oldAudioNotePosition == audioNotePosition) {
			if (isPlaying) audioNotePosition = GetSamplePosition (availableSongs [selectedSong - 1]) / 10000;
		} else {
			audioSamplePosition = audioNotePosition * 10000;
			audioPosition = audioSamplePosition / availableSongs [selectedSong - 1].frequency;

			SetSamplePosition (availableSongs [selectedSong - 1], audioSamplePosition);
		}

		oldSelectedSong = selectedSong;
		oldAudioPosition = audioPosition;
		oldAudioSamplePosition = audioSamplePosition;
		oldAudioNotePosition = audioNotePosition;

		Repaint ();
	}

	void OnGUI () {
		scrollPosition = GUILayout.BeginScrollView (scrollPosition);
		{
			GUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button ("...", GUILayout.MaxWidth (30.0f))) {
					string newSongPath = EditorUtility.OpenFolderPanel ("Choose Music Folder", "Assets/", "");

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

				songPath = EditorGUILayout.TextField (songPath);
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();

				if (GUILayout.Button ("Update Song List", GUILayout.MaxWidth (150.0f))) {
					this.FindSongs ();
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
			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.PropertyField (availableSongsProperty, true);
				EditorGUILayout.PropertyField (sampleCountProperty, true, GUILayout.MaxWidth (230.0f));
				EditorGUILayout.PropertyField (noteCountProperty, true, GUILayout.MaxWidth (200.0f));
			}
			GUILayout.EndHorizontal ();
			so.ApplyModifiedProperties ();

			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				availableSongs [selectedSong - 1] = EditorGUILayout.ObjectField ("Song Select", availableSongs [selectedSong - 1], typeof(AudioClip), true) as AudioClip;
				selectedSong = EditorGUILayout.IntSlider (selectedSong, 1, availableSongs.Length);
			}
			GUILayout.EndHorizontal ();
			audioPosition = EditorGUILayout.IntSlider ("Time:", audioPosition, 0, (int)availableSongs [selectedSong - 1].length);
			audioSamplePosition = EditorGUILayout.IntSlider ("Sample:", audioSamplePosition, 0, sampleCount [selectedSong - 1]);

			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				if (GUILayout.Button("Add Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 0] = 0;
				}

				if (GUILayout.Button("Add Hold Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 0] = longNoteLength;
				}

				EditorGUILayout.LabelField ("Length:", GUILayout.MaxWidth (50.0f));
				longNoteLength = EditorGUILayout.IntField (longNoteLength, GUILayout.MaxWidth (50.0f));

				EditorGUILayout.Space ();

				EditorGUILayout.LabelField ("Note:", GUILayout.MaxWidth (40.0f));
				notes [selectedSong - 1] [audioNotePosition, 0] = EditorGUILayout.IntField (notes [selectedSong - 1] [audioNotePosition, 0], GUILayout.MaxWidth (50.0f));
				EditorGUILayout.LabelField (" ", GUILayout.MaxWidth (50.0f));

				if (GUILayout.Button("Remove Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 0] = -1;
				}

				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				if (GUILayout.Button("Add Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 1] = 0;
				}

				if (GUILayout.Button("Add Hold Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 1] = longNoteLength;
				}

				EditorGUILayout.LabelField ("Length:", GUILayout.MaxWidth (50.0f));
				longNoteLength = EditorGUILayout.IntField (longNoteLength, GUILayout.MaxWidth (50.0f));

				EditorGUILayout.Space ();

				EditorGUILayout.LabelField ("Note:", GUILayout.MaxWidth (40.0f));
				notes [selectedSong - 1] [audioNotePosition, 1] = EditorGUILayout.IntField (notes [selectedSong - 1] [audioNotePosition, 1], GUILayout.MaxWidth (50.0f));
				EditorGUILayout.LabelField (" ", GUILayout.MaxWidth (50.0f));

				if (GUILayout.Button("Remove Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 1] = -1;
				}

				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				if (GUILayout.Button("Add Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 2] = 0;
				}

				if (GUILayout.Button("Add Hold Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 2] = longNoteLength;
				}

				EditorGUILayout.LabelField ("Length:", GUILayout.MaxWidth (50.0f));
				longNoteLength = EditorGUILayout.IntField (longNoteLength, GUILayout.MaxWidth (50.0f));

				EditorGUILayout.Space ();

				EditorGUILayout.LabelField ("Note:", GUILayout.MaxWidth (40.0f));
				notes [selectedSong - 1] [audioNotePosition, 2] = EditorGUILayout.IntField (notes [selectedSong - 1] [audioNotePosition, 2], GUILayout.MaxWidth (50.0f));
				EditorGUILayout.LabelField (" ", GUILayout.MaxWidth (50.0f));

				if (GUILayout.Button("Remove Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 2] = -1;
				}

				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				if (GUILayout.Button("Add Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 3] = 0;
				}

				if (GUILayout.Button("Add Hold Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 3] = longNoteLength;
				}

				EditorGUILayout.LabelField ("Length:", GUILayout.MaxWidth (50.0f));
				longNoteLength = EditorGUILayout.IntField (longNoteLength, GUILayout.MaxWidth (50.0f));

				EditorGUILayout.Space ();

				EditorGUILayout.LabelField ("Note:", GUILayout.MaxWidth (40.0f));
				notes [selectedSong - 1] [audioNotePosition, 3] = EditorGUILayout.IntField (notes [selectedSong - 1] [audioNotePosition, 3], GUILayout.MaxWidth (50.0f));
				EditorGUILayout.LabelField (" ", GUILayout.MaxWidth (50.0f));

				if (GUILayout.Button("Remove Note", GUILayout.MaxWidth (100.0f))) {
					notes [selectedSong - 1] [audioNotePosition, 3] = -1;
				}

				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();
			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();

				EditorGUILayout.LabelField ("Notes at:", GUILayout.MaxWidth (55.0f));
				EditorGUILayout.LabelField (audioNotePosition.ToString(), GUILayout.MaxWidth (50.0f));

				EditorGUILayout.Space ();
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			{
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();
				EditorGUILayout.Space ();

				if (GUILayout.Button (new GUIContent("Remove all", "Removes all notes in current note"), GUILayout.MaxWidth (150.0f))) {
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
				EditorGUILayout.Space ();

				if (GUILayout.Button (new GUIContent("Reset all songs", "Resets all notes in all songs"), GUILayout.MaxWidth (150.0f))) {
					this.ResetNotes ();
				}
				GUILayout.Label (GUI.tooltip, GUILayout.MaxWidth (200.0f));
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			audioNotePosition = EditorGUILayout.IntSlider ("Note:", audioNotePosition, 0, noteCount [selectedSong - 1]);

			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();

			GUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button ("׀◄◄")) {
					if (audioPosition == 0) {
						if (selectedSong <= 1) {
							selectedSong = availableSongs.Length;
						} else {
							--selectedSong;
						}

						this.SwitchSong ();
					} else {
						audioPosition = 0;
					}
				}

				if (GUILayout.Button ("׀◄")) {
					if (audioNotePosition >= noteJump) audioNotePosition -= noteJump;
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

				if (GUILayout.Button ("►׀")) {
					if (audioNotePosition <= noteCount [selectedSong - 1] - noteJump) audioNotePosition += noteJump;
				}

				if (GUILayout.Button ("►►׀")) {
					if (selectedSong >= availableSongs.Length) {
						selectedSong = 1;
					} else {
						++selectedSong;
					}

					this.SwitchSong ();
				}
			}
			GUILayout.EndHorizontal ();

			noteJump = EditorGUILayout.IntSlider ("Skip notes:", noteJump, 0, noteCount [selectedSong - 1]);
		}
		GUILayout.EndScrollView ();
	}



	private void FindSongs() {
		string[] guids = AssetDatabase.FindAssets ("t:AudioClip", new string[] { songPath });

		if (guids.Length > 0) {
			availableSongs = new AudioClip[guids.Length];
			sampleCount = new int[guids.Length];
			noteCount = new int[guids.Length];

			for (int i = 0; i < guids.Length; ++i) {
				availableSongs [i] = AssetDatabase.LoadAssetAtPath (AssetDatabase.GUIDToAssetPath (guids [i]), typeof(AudioClip)) as AudioClip;

				sampleCount [i] = availableSongs [i].samples;
				Debug.Log ("Sample rate: " + availableSongs [i].frequency + "; Song length: " + availableSongs [i].length + "; Sample count: " + sampleCount [i]);
				noteCount [i] = sampleCount [i] / 10000;
			}

			this.ResetNotes ();
		} else {
			EditorUtility.DisplayDialog ("Error", "Selected folder does not contain any AudioClips", "OK");
		}
	}

	private void ResetNotes() {
		notes = new List<int[,]>();
		for (int i = 0; i < availableSongs.Length; ++i) {
			notes.Add(new int[noteCount[i], 4]);

			for (int j = 0; j < notes [i].GetLength (0); ++j) {
				for (int k = 0; k < notes [i].GetLength (1); ++k) {
					notes [i] [j, k] = -1;
				}
			}
		}
	}

	private void SwitchSong() {
		if (IsPlaying (availableSongs [selectedSong - 1])) {
			StopAllClips ();

			this.ResetAudioPosition ();

			PlayPauseSong (availableSongs [selectedSong - 1]);
		}

		audioPosition = 0;
	}

	private void ResetAudioPosition() {
		audioPosition = 0;
		audioSamplePosition = 0;
		audioNotePosition = 0;
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
