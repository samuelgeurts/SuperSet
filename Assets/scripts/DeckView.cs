using System.Collections.Generic;
using UnityEngine;

//deckview needs deck
[RequireComponent (typeof(DeckModel))]

public class DeckView : MonoBehaviour
{

	DeckModel deck;

	public Vector3 start;
	public float cardOffsetx;
	public float cardOffsety;
	public GameObject cardPrefab;
	private int[,] shownCardDimension = new int[5, 36];
	public int numberofSets;
	public List<int> shownCardIndex = new List<int> ();

	public const int SetSize = SetLogic.SetSize; //cards needed for one set
	public List<CardModel> selectedCards = new List<CardModel> ();

	//select or deselect a clicked card; a fifth card is ignored until one is deselected
	public void ToggleSelection (CardModel card)
	{
		if (gameOver) {
			return;
		}
		if (selectedCards.Remove (card)) {
			card.SetSelected (false);
		} else if (selectedCards.Count < SetSize) {
			selectedCards.Add (card);
			card.SetSelected (true);
			if (selectedCards.Count == SetSize) {
				CheckSelection ();
			}
		}
	}

	public int score;
	string message = "";
	float messageUntil;
	GUIStyle labelStyle;

	//true if the selected cards form a set: every dimension all the same or all different
	bool SelectionIsSet ()
	{
		int[] indexes = new int[SetSize];
		for (int c = 0; c < SetSize; c++) {
			indexes [c] = selectedCards [c].cardIndex;
		}
		return SetLogic.IsSet4Cards (indexes);
	}

	void CheckSelection ()
	{
		if (SelectionIsSet ()) {
			score++;
			ShowMessage ("Set gevonden!");
			int[] found = new int[SetSize];
			for (int c = 0; c < SetSize; c++) {
				found [c] = System.Array.IndexOf (slots, selectedCards [c]);
			}
			ClearSelection ();
			foreach (int slot in found) {
				DrawCard (slot);
			}
			EnsureSetOnBoard ();
		} else {
			ShowMessage ("Geen set");
			ClearSelection ();
		}
	}

	void ClearSelection ()
	{
		foreach (CardModel card in selectedCards) {
			card.SetSelected (false);
		}
		selectedCards.Clear ();
	}

	void ShowMessage (string text)
	{
		message = text;
		messageUntil = Time.time + 1.5f;
	}

	void OnGUI ()
	{
		if (labelStyle == null) {
			labelStyle = new GUIStyle (GUI.skin.label);
			labelStyle.fontSize = 22;
			labelStyle.normal.textColor = Color.white;
		}
		GUI.Label (new Rect (10, 50, 400, 32), "Score: " + score, labelStyle);
		if (Time.time < messageUntil) {
			GUI.Label (new Rect (10, 82, 400, 32), message, labelStyle);
		}
		if (gameOver) {
			float w = 320;
			float h = 160;
			Rect box = new Rect ((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
			GUI.Box (box, "");
			GUI.Label (new Rect (box.x + 20, box.y + 15, w - 40, 32), "Spel afgelopen", labelStyle);
			GUI.Label (new Rect (box.x + 20, box.y + 50, w - 40, 32), "Score: " + score, labelStyle);
			if (GUI.Button (new Rect (box.x + 20, box.y + 100, w - 40, 36), "Nieuw spel")) {
				Restart ();
			}
		}
	}

	void Start ()
	{
		deck = GetComponent<DeckModel> ();
		StartGame ();
	}

	const int BoardSize = 36; //cards on the table
	List<int> deckOrder; //shuffled order of all cards
	int nextCard; //position in deckOrder of the next card to draw
	CardModel[] slots = new CardModel[BoardSize];
	bool[] slotActive = new bool[BoardSize];

	void StartGame ()
	{
		deck.Shuffle ();
		deckOrder = new List<int> (deck.GetCards ());
		ShowCards ();
		nextCard = BoardSize;
		EnsureSetOnBoard ();
	}

	//put a card from the deck in a slot; hides the slot when the deck is empty
	void DrawCard (int slot)
	{
		CardModel model = slots [slot];
		if (nextCard >= deckOrder.Count) {
			slotActive [slot] = false;
			model.gameObject.SetActive (false);
			return;
		}
		int index = deckOrder [nextCard++];
		model.cardIndex = index;
		model.ToggleFace (true);
		int[] dimensions = SetLogic.IndexToDimension (index);
		for (int j = 0; j < 5; j++) {
			shownCardDimension [j, slot] = dimensions [j];
		}
		shownCardIndex [slot] = index;
	}

	//count the sets on the table; while there is none, swap random cards for new ones
	void EnsureSetOnBoard ()
	{
		CountSets ();
		while (numberofSets == 0 && nextCard < deckOrder.Count) {
			int slot = Random.Range (0, BoardSize);
			if (slotActive [slot]) {
				DrawCard (slot);
				CountSets ();
			}
		}
		gameOver = numberofSets == 0; //deck empty and no set left on the table
	}

	bool gameOver;

	//count the sets on the table and publish the number for the UI text
	void CountSets ()
	{
		numberofSets = SetLogic.CountSets (shownCardDimension, slotActive);
		Numberofsets.numberofSets = numberofSets;
	}

	void ShowCards ()
	{ 
		int cardCount = 0;

		int coordx = 0; 
		int coordy = 0;

		foreach (int i in deck.GetCards()) {
			if (cardCount < 36) {
				if (coordx >= 9) {
					coordx = 0;
					coordy++;
				}
					
				float cox = cardOffsetx * coordx;
				float coy = cardOffsety * coordy;


				GameObject cardCopy = (GameObject)Instantiate (cardPrefab); //creat a copy of "cardPrefab"
				Vector3 temp = start + new Vector3 (cox, coy); //tempory position startposition + offset times current card
				cardCopy.transform.position = temp;

				CardModel cardModel = cardCopy.GetComponent<CardModel> ();
				cardModel.cardIndex = i;
				cardModel.deckView = this;
				cardModel.ToggleFace (true);
				slots [cardCount] = cardModel;
				slotActive [cardCount] = true;

				shownCardIndex.Add (i); //stores the shown card indexes in the List: shownCardIndex

				int[] dimensions = SetLogic.IndexToDimension (i);
				//generated 2d array with 5d coordinates
				for (int j = 0; j < 5; j++) {
					shownCardDimension [j, cardCount] = dimensions [j];
				}

				cardCount++;
				coordx++;

			}
		}

	}

	public void Restart ()
	{
		ClearSelection ();
		foreach (CardModel model in slots) {
			if (model != null) {
				Destroy (model.gameObject);
			}
		}
		System.Array.Clear (slotActive, 0, BoardSize);
		shownCardIndex.Clear ();
		score = 0;
		message = "";
		StartGame ();
	}

}
