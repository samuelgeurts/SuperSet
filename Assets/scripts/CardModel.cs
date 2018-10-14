using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardModel : MonoBehaviour {
	SpriteRenderer spriteRenderer; //reference component for this object

	//[SerializeField] private GameObject targetObject;
	//[SerializeField] private string targetMessage;

	public Color highlightColor = Color.cyan;
	public Sprite[] faces;
	public Sprite cardBack;

	//private Color highlightColor = new Color(1.0f,1.0f,0.9f,0.7f);
	public int cardIndex; //public face of our card model e.g faces[cardIndex]

	public void ToggleFace(bool showFace)
	{
		if (showFace) {
			spriteRenderer.sprite = faces [cardIndex];
		} else {
			spriteRenderer.sprite = cardBack;
		}
	}

	void Awake() {
		spriteRenderer = GetComponent<SpriteRenderer>(); //angle brackets specify the type
	}

//
//	public void OnMouseOver()
//	{
//		transform.localScale = new Vector3 (1.05f, 1.05f, 1.0f);
//
////		SpriteRenderer sprite = GetComponent<SpriteRenderer> ();
////		if (sprite != null) {
////			sprite.color = highlightColor;
////		}
//	}
//
//	public void OnMouseExit()
//	{
//		transform.localScale = new Vector3 (1f, 1f, 1.0f);
//
////		SpriteRenderer sprite = GetComponent<SpriteRenderer> ();
////		if (sprite != null) {
////			sprite.color = Color.white;
////		}
//	}
//
	public void OnMouseClick()
	{
		transform.localScale = new Vector3 (1.05f, 1.05f, 1.0f);

		SpriteRenderer sprite = GetComponent<SpriteRenderer>();
		if (sprite != null) {
			sprite.color = highlightColor;
		}


	}


}
