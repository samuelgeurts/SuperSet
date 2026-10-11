using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIButton : MonoBehaviour {
	[SerializeField] private GameObject targetObject;
	[SerializeField] private string targetMessage;
	public Color highlightColor = Color.cyan;
	Vector3 baseScale;

	void Awake()
	{
		baseScale = transform.localScale;
	}

	public void OnMouseOver()
	{
		SpriteRenderer sprite = GetComponent<SpriteRenderer> ();
		if (sprite != null) {
			sprite.color = highlightColor;
		}
	}

	public void OnMouseExit()
	{
		SpriteRenderer sprite = GetComponent<SpriteRenderer> ();
		if (sprite != null) {
			sprite.color = Color.white;
		}
	}

	public void OnMouseDown()
	{
		transform.localScale = baseScale * 0.9f;
	}


	public void OnMouseUp()
	{
		transform.localScale = baseScale;
		if (targetObject != null) {
			targetObject.SendMessage (targetMessage);
		}
	}


}
