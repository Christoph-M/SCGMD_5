using UnityEngine;
using UnityEditor;
using System.Collections;

public class GUIanimTest : EditorWindow {

	private Vector2 startPos, currentPos, midPos, endPos;

	private float t, oldT;

	[MenuItem ("Window/GUI Anim Test")]
	public static void ShowWindow() {
		EditorWindow.GetWindow (typeof(GUIanimTest));
	}

	void OnEnable() {
		startPos = new Vector2 (10, 10);
		currentPos = Vector2.zero;
		midPos = new Vector2 (100, 100);
		endPos = new Vector2 (200, 10);

		oldT = Time.realtimeSinceStartup;
	}

	int b = 0;
	void Update() {
		if (b == 0) {
			t += Time.realtimeSinceStartup - oldT;
			currentPos = Vector2.Lerp (startPos, midPos, t);
			if (t >= 1.0f) b = 1;
		} else if (b == 1) {
			t -= Time.realtimeSinceStartup - oldT;
			currentPos = Vector2.Lerp (endPos, midPos, t);
			if (t <= 0.0f) b = 2;
		} else if (b == 2) {
			t += Time.realtimeSinceStartup - oldT;
			currentPos = Vector2.Lerp (endPos, startPos, t);
			if (t >= 1.0f) { b = 0; t = 0.0f; }
		}

		Repaint ();

		oldT = Time.realtimeSinceStartup;
	}

	void OnGUI() {
		EditorGUI.DrawRect (new Rect (currentPos.x, currentPos.y, 50, 100), Color.black);
	}
}
