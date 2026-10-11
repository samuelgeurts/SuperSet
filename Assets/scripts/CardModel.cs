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

	Vector3 baseScale; //scale of the unselected card

	void Awake() {
		spriteRenderer = GetComponent<SpriteRenderer>(); //angle brackets specify the type
		baseScale = transform.localScale;
		FitColliderToSprite ();
	}

	//make the whole card clickable: size the collider to the card sprite, not the prefab default
	void FitColliderToSprite ()
	{
		BoxCollider2D box = GetComponent<BoxCollider2D> ();
		Sprite sprite = (faces != null && faces.Length > 0) ? faces [0] : spriteRenderer.sprite;
		if (box != null && sprite != null) {
			box.size = sprite.bounds.size;
			box.offset = sprite.bounds.center;
		}
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
	public DeckView deckView; //set by DeckView when it creates the card
	public bool selected { get; private set; }

	public void OnMouseDown()
	{
		if (deckView != null) {
			deckView.ToggleSelection (this);
		}
	}

	public void SetSelected(bool value)
	{
		selected = value;
		transform.localScale = value ? baseScale * 1.05f : baseScale;
		spriteRenderer.color = value ? highlightColor : Color.white;
	}


}
