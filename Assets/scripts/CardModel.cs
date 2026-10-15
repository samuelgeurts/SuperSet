using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardModel : MonoBehaviour {
	SpriteRenderer spriteRenderer; //reference component for this object

	//[SerializeField] private GameObject targetObject;
	//[SerializeField] private string targetMessage;

	public Color highlightColor = Color.cyan;
	public Sprite cardBack;

	//private Color highlightColor = new Color(1.0f,1.0f,0.9f,0.7f);
	public int cardIndex; //the card: its dimensions say what is drawn (see CardArt)

	List<GameObject> marks = new List<GameObject> (); //the shapes on the face of the card

	public void ToggleFace(bool showFace)
	{
		if (spriteRenderer == null) {
			spriteRenderer = GetComponent<SpriteRenderer>(); //not set yet when the card is made outside play mode
		}
		foreach (GameObject mark in marks) {
			Destroy (mark);
		}
		marks.Clear ();
		if (!showFace) {
			spriteRenderer.sprite = cardBack;
			return;
		}
		spriteRenderer.sprite = CardArt.CardSprite;

		//1 to 4 marks under each other, in the color, shape, fill and size of the card
		int[] dimension = SetLogic.IndexToDimension (cardIndex);
		int count = dimension [2] + 1;
		Sprite sprite = CardArt.MarkSprite (dimension [1], dimension [3], dimension [0]);
		Vector2 size = CardArt.MarkScale (dimension [4]);
		for (int i = 0; i < count; i++) {
			GameObject mark = new GameObject ("Mark");
			mark.transform.SetParent (transform, false);
			mark.transform.localPosition = new Vector3 (0f, ((count - 1) / 2f - i) * CardArt.MarkSpacing, 0f);
			mark.transform.localScale = new Vector3 (size.x, size.y, 1f);
			SpriteRenderer renderer = mark.AddComponent<SpriteRenderer> ();
			renderer.sprite = sprite;
			renderer.sortingLayerID = spriteRenderer.sortingLayerID;
			renderer.sortingOrder = spriteRenderer.sortingOrder + 1;
			marks.Add (mark);
		}
	}

	Vector3 baseScale; //scale of the unselected card

	void Awake() {
		spriteRenderer = GetComponent<SpriteRenderer>(); //angle brackets specify the type
		baseScale = transform.localScale;
		spriteRenderer.sprite = CardArt.CardSprite;
		FitColliderToSprite ();
		CreateShadow ();
	}

	//make the whole card clickable: size the collider to the card sprite, not the prefab default
	void FitColliderToSprite ()
	{
		BoxCollider2D box = GetComponent<BoxCollider2D> ();
		Sprite sprite = spriteRenderer.sprite;
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



	List<GameObject> outlines = new List<GameObject> (); //borders behind the card, one per found set the card is part of

	//add a border in the color of a found set; a card in several sets gets several rings, the newest outside
	public void AddFound(Color color)
	{
		int ring = outlines.Count;
		outlines.Add (CreateBehind ("FoundOutline", color, 0.06f + 0.05f * ring, Vector2.zero, -1 - ring));
	}

	public void ClearFound()
	{
		foreach (GameObject outline in outlines) {
			Destroy (outline);
		}
		outlines.Clear ();
	}

	//a soft shadow under the card
	void CreateShadow()
	{
		CreateBehind ("Shadow", new Color (0f, 0f, 0f, 0.35f), 0.03f, new Vector2 (0.03f, -0.04f), -2);
	}

	//a rounded rectangle behind the card: margin and offset are fractions of the card width
	GameObject CreateBehind(string objectName, Color color, float margin, Vector2 offset, int order)
	{
		Sprite face = spriteRenderer.sprite;
		Vector2 size = face.bounds.size;
		float m = size.x * margin;

		GameObject behind = new GameObject (objectName);
		behind.transform.SetParent (transform, false);
		behind.transform.localPosition = face.bounds.center + new Vector3 (offset.x * size.x, offset.y * size.x, 0f);
		SpriteRenderer border = behind.AddComponent<SpriteRenderer> ();
		border.sprite = UiStyle.RoundedSprite (size.x * 0.08f);
		border.drawMode = SpriteDrawMode.Sliced;
		border.size = new Vector2 (size.x + 2 * m, size.y + 2 * m);
		border.color = color;
		border.sortingLayerID = spriteRenderer.sortingLayerID;
		border.sortingOrder = spriteRenderer.sortingOrder + order;
		return behind;
	}

}
