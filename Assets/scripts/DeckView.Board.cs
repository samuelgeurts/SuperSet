using System.Collections.Generic;
using UnityEngine;

//the tables (one per difficulty), the deck and dealing of cards
public partial class DeckView
{

	//the table of one difficulty; kept while the player looks at another difficulty
	class Board
	{
		public List<int> deckOrder; //shuffled order of all cards
		public int nextCard; //position in deckOrder of the next card to draw
		public List<int> cardIndex; //card on every slot
		public bool[] slotActive;
		public List<int[]> foundSets;
		public bool gameOver;
		public int discardCount; //cards on the discard pile
		public int discardTop; //card on top of the discard pile, -1 when it is empty
	}

	Dictionary<int, Board> boards = new Dictionary<int, Board> ();

	int BoardKey ()
	{
		return dimensionCount * 10 + setSize;
	}

	int boardSize; //cards on the table
	List<int> deckOrder;
	int nextCard;
	CardModel[] slots = new CardModel[MaxBoardSize];
	bool[] slotActive = new bool[MaxBoardSize];
	bool gameOver;

	//show the table of the chosen difficulty: the one kept from before, or a new one
	void LoadBoard ()
	{
		boardSize = SetLogic.BoardSize (dimensionCount, setSize);
		Board board;
		bool isNew = !boards.TryGetValue (BoardKey (), out board);
		if (isNew) {
			StartNewBoard ();
		} else {
			deckOrder = board.deckOrder;
			nextCard = board.nextCard;
			shownCardIndex = board.cardIndex;
			slotActive = board.slotActive;
			foundSets = board.foundSets;
			gameOver = board.gameOver;
			discardCount = board.discardCount;
			discardTopIndex = board.discardTop;
			for (int slot = 0; slot < boardSize; slot++) {
				SetDimensions (slot);
			}
			FindSets ();
		}
		drawShown = deckOrder.Count - nextCard;
		ShowCards ();
		if (isNew) {
			StartCoroutine (DealBoard ());
		}
	}

	//keep the table of the current difficulty and take its cards from the screen
	void StoreBoard ()
	{
		Board board = new Board ();
		board.deckOrder = deckOrder;
		board.nextCard = nextCard;
		board.cardIndex = shownCardIndex;
		board.slotActive = slotActive;
		board.foundSets = foundSets;
		board.gameOver = gameOver;
		board.discardCount = discardCount;
		board.discardTop = discardTopIndex;
		boards [BoardKey ()] = board;

		ClearSelection ();
		foreach (CardModel model in slots) {
			if (model != null) {
				Destroy (model.gameObject);
			}
		}
		System.Array.Clear (slots, 0, MaxBoardSize);
	}

	//deal a new table: shuffle the deck and put the first cards on it
	void StartNewBoard ()
	{
		deck.Shuffle (dimensionCount, setSize);
		deckOrder = new List<int> (deck.GetCards ());
		shownCardIndex = new List<int> ();
		slotActive = new bool[MaxBoardSize];
		for (int slot = 0; slot < boardSize; slot++) {
			shownCardIndex.Add (deckOrder [slot]);
			slotActive [slot] = true;
			SetDimensions (slot);
		}
		nextCard = boardSize;
		foundSets = new List<int[]> ();
		discardCount = 0;
		discardTopIndex = -1;
		EnsureSetOnBoard ();
	}

	//copy the dimensions of the card on a slot to the table used to look for sets
	void SetDimensions (int slot)
	{
		int[] dimensions = SetLogic.IndexToDimension (shownCardIndex [slot]);
		for (int j = 0; j < 5; j++) {
			shownCardDimension [j, slot] = dimensions [j];
		}
	}

	//take the next card from the deck for a slot; the slot is no longer used when the deck is empty.
	//Only the data changes here: the card on the screen is dealt by DealSlots.
	void ChangeCard (int slot)
	{
		if (nextCard >= deckOrder.Count) {
			slotActive [slot] = false;
		} else {
			shownCardIndex [slot] = deckOrder [nextCard++];
			SetDimensions (slot);
		}
	}

	//find the sets on the table; while there is none, swap random cards for new ones
	void EnsureSetOnBoard ()
	{
		foundSets.Clear ();
		FindSets ();
		while (boardSets.Count == 0 && nextCard < deckOrder.Count) {
			int slot = Random.Range (0, boardSize);
			if (slotActive [slot]) {
				ChangeCard (slot);
				FindSets ();
			}
		}
		gameOver = boardSets.Count == 0; //deck empty and no set left on the table
	}

	//look up all sets on the table and publish the number that is not found yet for the UI text
	void FindSets ()
	{
		boardSets = SetLogic.FindSets (shownCardDimension, slotActive, setSize, dimensionCount);
		numberofSets = boardSets.Count - foundSets.Count;
		Numberofsets.numberofSets = numberofSets;
	}

	void ShowCards ()
	{
		for (int slot = 0; slot < boardSize; slot++) {
			GameObject cardCopy = (GameObject)Instantiate (cardPrefab); //creat a copy of "cardPrefab"
			cardCopy.SetActive (true); //the template is inactive, so is its copy

			CardModel cardModel = cardCopy.GetComponent<CardModel> ();
			cardModel.cardIndex = shownCardIndex [slot];
			cardModel.deckView = this;
			cardModel.ToggleFace (true);
			slots [slot] = cardModel;
			cardCopy.SetActive (slotActive [slot]);
		}

		//the sets that were found already keep their colored border
		for (int i = 0; i < foundSets.Count; i++) {
			foreach (int slot in foundSets [i]) {
				slots [slot].AddFound (UiStyle.FoundColor (i));
			}
		}
		ApplyLayout ();
	}

	//change the difficulty; the table of that difficulty stays as the player left it
	public void SetDifficulty (int dimensions, int size)
	{
		if (busy) {
			return;
		}
		StoreBoard ();
		dimensionCount = Mathf.Clamp (dimensions, 3, 5);
		setSize = Mathf.Clamp (size, 3, 4);
		PlayerPrefs.SetInt ("Dimensions", dimensionCount);
		PlayerPrefs.SetInt ("SetSize", setSize);
		message = "";
		LoadBoard ();
	}

	//after "Deck finished": a new table for the same difficulty
	void NewGame ()
	{
		if (busy) {
			return;
		}
		StoreBoard ();
		boards.Remove (BoardKey ());
		message = "";
		LoadBoard ();
	}

}
