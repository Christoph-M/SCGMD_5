using UnityEngine;
using UnityEditor;
using System.Reflection;

public static class AudioUtility {
	private static Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
	private static System.Type audioUtilClass   = unityEditorAssembly.GetType ("UnityEditor.AudioUtil");

	private static MethodInfo playClip          = audioUtilClass.GetMethod ("PlayClip",              BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
	private static MethodInfo isPlaying         = audioUtilClass.GetMethod ("IsClipPlaying",         BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
	private static MethodInfo getSamplePosition = audioUtilClass.GetMethod ("GetClipSamplePosition", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
	private static MethodInfo setSamplePosition = audioUtilClass.GetMethod ("SetClipSamplePosition", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip), typeof(int) }, null);
	private static MethodInfo pauseClip         = audioUtilClass.GetMethod ("PauseClip",             BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
	private static MethodInfo resumeClip        = audioUtilClass.GetMethod ("ResumeClip",            BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip)              }, null);
	private static MethodInfo stopAllClips      = audioUtilClass.GetMethod ("StopAllClips",          BindingFlags.Static | BindingFlags.Public, null, new System.Type[] {                                }, null);

	private static bool isPaused = false;


	/// <summary>
	/// Plays/Pauses/Resumes the specified AudioClip.
	/// </summary>
	/// <param name="clip">The AudioClip to be played/paused/resumed.</param>
	public static void PlayPauseClip(AudioClip clip) {
		if (!IsPlaying (clip)) {
			playClip.Invoke (null, new object[] { clip });
		} else if (!isPaused) {
			AudioUtility.PauseClip (clip);
		} else {
			AudioUtility.ResumeClip (clip);
		}
	}

	/// <summary>
	/// Pauses the specified AudioClip.
	/// </summary>
	/// <param name="clip">The AudioClip to be paused.</param>
	public static void PauseClip(AudioClip clip) {
		isPaused = true;
		pauseClip.Invoke (null, new object[] { clip });
	}

	/// <summary>
	/// Resumes the specified AudioClip.
	/// </summary>
	/// <param name="clip">The AudioClip to be resumed</param>
	public static void ResumeClip(AudioClip clip) {
		isPaused = false;
		resumeClip.Invoke (null, new object[] { clip });
	}

	/// <summary>
	/// Determines if the specified AudioClip is playing.
	/// </summary>
	/// <returns><c>true</c> if the specified AudioClip is playing; otherwise, <c>false</c>.</returns>
	/// <param name="clip">The AudioClip to be checked.</param>
	public static bool IsPlaying (AudioClip clip) {
		return (bool)isPlaying.Invoke (null, new object[] { clip });
	}
	/// <summary>
	/// Returns isPaused.
	/// </summary>
	/// <returns><c>true</c> if an Audioclip is paused; otherwise, <c>false</c>.</returns>
	public static bool IsPaused() {
		return isPaused;
	}

	/// <summary>
	/// Gets the sample position of the specified AudioClip.
	/// </summary>
	/// <returns>The sample position.</returns>
	/// <param name="clip">The AudioClip to get the sample position from.</param>
	public static int GetSamplePosition(AudioClip clip) {
		return (int)getSamplePosition.Invoke (null, new object[] { clip });
	}

	/// <summary>
	/// Sets the sample position of the specified AudioClip.
	/// </summary>
	/// <param name="clip">The AudioClip to set the sample position to.</param>
	/// <param name="pos">Position.</param>
	public static void SetSamplePosition(AudioClip clip, int pos) {
		setSamplePosition.Invoke (null, new object[] { clip, pos });
	}

	/// <summary>
	/// Stops all AudioClips.
	/// </summary>
	public static void StopAllClips() {
		if (isPaused) isPaused = false;
		stopAllClips.Invoke (null, new object[] {});
	}
}
