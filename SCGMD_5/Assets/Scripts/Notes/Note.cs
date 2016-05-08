using UnityEngine;
using System.Collections;

public class Note : MonoBehaviour {
	public KeyCode key;

	public Color defaultColor;
	public Color inTriggerColor;
	public Color hitColor;
	public Color missColor;

	public float speed = 5.0f;


	private SpriteRenderer hitZone = null;

	private bool inTrigger = false;
	private float travelTime = 0.0f;
	// Use this for initialization
	void Start () {
	
	}

	void OnTriggerEnter2D(Collider2D other) {
		if (other.tag == "Hitzone") {
			inTrigger = true;

			other.GetComponent<SpriteRenderer> ().color = inTriggerColor;

			hitZone = other.gameObject.GetComponent<SpriteRenderer> ();
		}

		if (other.tag == "Killzone") {
			Destroy (this.gameObject);
		}
	}

	void OnTriggerExit2D(Collider2D other) {
		if (other.tag == "Hitzone") {
			inTrigger = false;

			other.GetComponent<SpriteRenderer> ().color = missColor;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (key) && hitZone && inTrigger) {
			this.enabled = false;

			StartCoroutine (this.NoteHit ());
			StartCoroutine (this.Grow ());
		}

		this.transform.position += Vector3.left * speed * Time.deltaTime;
		travelTime += Time.deltaTime;
	}

	private IEnumerator NoteHit() {
		hitZone.color = hitColor;

		yield return new WaitForSeconds (0.1f);

		hitZone.color = defaultColor;
		hitZone = null;
	}

	private IEnumerator Grow() {
		float time = 0.0f;

		while (time < 0.2f) {
			this.transform.localScale *= 1.03f;

			Color oldColor = this.GetComponent<SpriteRenderer> ().color;
			Color newcolor = new Color (oldColor.r, oldColor.g, oldColor.b, oldColor.a - Time.deltaTime * 5.0f);

			this.GetComponent<SpriteRenderer> ().color = newcolor;

			time += Time.deltaTime;

			yield return new WaitForSeconds (0.01f);
		}

		Destroy (this.gameObject);
	}
}
