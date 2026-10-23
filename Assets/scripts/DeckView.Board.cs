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
		public int discardSets; //sets on the discard pile
		public int discardTop; //card on top of the discard pile, -1 when it is empty
		public FamilyState family; //only the family board has one
	}

	Dictionary<int, Board> boards = new Dictionary<int, Board> ();

	int BoardKey ()
	{
		return familyMode ? 1000 : dimensionCount * 10 + setSize;
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
			discardSets = board.discardSets;
			discardTopIndex = board.discardTop;
			family = board.family;
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
		board.discardSets = discardSets;
		board.discardTop = discardTopIndex;
		board.family = family;
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
		if (familyMode) {
			family = pendingFamily != null ? pendingFamily : FamilyState.Create ();
			pendingFamily = null;
		}
		DealFreshTable ();
		if (familyMode) {
			ReplayFamilyLog ();
		}
	}

	//a table with a shuffled deck and its first cards; the family board uses the seed for it
	void DealFreshTable ()
	{
		deck.Shuffle (dimensionCount, setSize, familyMode ? family.random : null);
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
		discardSets = 0;
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

	const int MaxSwaps = 300; //most cards that are swapped to get a set on the table

	//find the sets on the table; while there is none, swap random cards for new ones
	void EnsureSetOnBoard ()
	{
		foundSets.Clear ();
		if (familyMode) {
			family.foundBy.Clear ();
		}
		FindSets ();
		//A card that is swapped away is not lost: it goes to the bottom of the draw pile, and will be dealt later.
		//The number of swaps is limited, so a table where no set can come never keeps the game busy.
		int swaps = 0;
		while (boardSets.Count == 0 && nextCard < deckOrder.Count && swaps < MaxSwaps) {
			int slot = familyMode ? family.random.Next (boardSize) : Random.Range (0, boardSize);
			if (slotActive [slot]) {
				deckOrder.Add (shownCardIndex [slot]);
				ChangeCard (slot);
				FindSets ();
				swaps++;
			}
		}
		gameOver = boardSets.Count == 0; //no set left on the table, and none came after the swaps
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

		ShowFoundBorders ();
		ApplyLayout ();
	}

	//the sets that were found already keep their colored border
	void ShowFoundBorders ()
	{
		for (int i = 0; i < foundSets.Count; i++) {
			foreach (int slot in foundSets [i]) {
				slots [slot].AddFound (FoundColorOf (i));
			}
		}
	}

	//border color of a found set: one per set, or (family mode) the color of the player who found it
	Color FoundColorOf (int foundIndex)
	{
		if (familyMode) {
			return UiStyle.FoundColor (family.PlayerIndex (family.foundBy [foundIndex]));
		}
		return UiStyle.FoundColor (foundIndex);
	}

	//the color the set that is being picked will get
	Color NextFoundColor ()
	{
		if (familyMode) {
			return UiStyle.FoundColor (family.PlayerIndex (family.localId));
		}
		return UiStyle.FoundColor (foundSets.Count);
	}

	//a set was found, by the player or read from the log: keep it, count it and see how many sets are left on the table
	void RegisterFound (int[] found, string playerId, string playerName)
	{
		foundSets.Add (found);
		if (familyMode) {
			family.foundBy.Add (playerId);
			family.AddPlayer (playerId, playerName);
			family.counts [playerId]++;
		} else {
			score += SetLogic.PointsPerSet (dimensionCount, setSize);
			SaveScore ();
		}
		numberofSets = boardSets.Count - foundSets.Count;
		Numberofsets.numberofSets = numberofSets;
	}

	//the cards of a claim in the log must really form a set of cards on the table that was not found yet
	bool ClaimIsValid (int[] claim)
	{
		if (gameOver || claim.Length != setSize) {
			return false;
		}
		int[] indexes = new int[setSize];
		for (int i = 0; i < setSize; i++) {
			int slot = claim [i];
			if (slot < 0 || slot >= boardSize || !slotActive [slot] || System.Array.IndexOf (claim, slot) != i) {
				return false;
			}
			indexes [i] = shownCardIndex [slot];
		}
		return SetLogic.IsSetCards (indexes, dimensionCount) && !IsFound (claim);
	}

	//build the shared table from the log: every claim is replayed on the data only (no animation).
	//A claim that is no longer valid, for instance because the cards were replaced, is skipped.
	void ReplayFamilyLog ()
	{
		if (family.log.Count == 0 && FamilyDemo && !family.online) {
			AddDemoClaims ();
			family.ResetPlay ();
			DealFreshTable (); //the same table again, now built only from the log
		}
		foreach (FamilyClaim claim in new List<FamilyClaim> (family.log)) {
			int[] slotList = (int[])claim.slots.Clone ();
			System.Array.Sort (slotList);
			if (!ClaimIsValid (slotList)) {
				continue;
			}
			RegisterFound (slotList, claim.uid, claim.player);
			if (numberofSets <= 0) {
				ReplaceFoundCardsData ();
			}
		}
	}

	//phase 1 only: no database yet, so two made-up players find a few sets to show colors and the player list.
	//They play on the fresh table and their claims go in the log like real ones. Phase 2 removes this.
	const bool FamilyDemo = true;

	void AddDemoClaims ()
	{
		string[] bots = { "Anna", "Ben" };
		for (int n = 0; n < 9 && boardSets.Count - foundSets.Count > 0; n++) {
			int[] set = null;
			foreach (int[] candidate in boardSets) {
				if (!IsFound (candidate)) {
					set = candidate;
					break;
				}
			}
			if (set == null || (n >= 5 && boardSets.Count - foundSets.Count < 2)) {
				break;
			}
			string bot = bots [n % bots.Length];
			family.AddClaim (bot, bot, set);
			RegisterFound (set, bot, bot);
			if (numberofSets <= 0) {
				ReplaceFoundCardsData ();
			}
		}
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

	//switch between the classic game and the family board; both keep their table
	public void SetFamilyMode (bool on)
	{
		if (busy || on == familyMode) {
			return;
		}
		StoreBoard ();
		familyMode = on;
		if (on) {
			dimensionCount = SetLogic.Dimensions; //the family board is always the hardest one
			setSize = 4;
		} else {
			UseClassicDifficulty ();
		}
		message = "";
		if (on && FirebaseConfig.Configured && !boards.ContainsKey (BoardKey ())) {
			StartFamilyConnect (); //the table comes from the database
			return;
		}
		LoadBoard ();
		if (on && FamilyOnline) {
			StartFamilyLoop (); //the table was kept: catch up with the sets of the others
		}
	}

	//the difficulty the player chose for the classic game
	void UseClassicDifficulty ()
	{
		dimensionCount = Mathf.Clamp (PlayerPrefs.GetInt ("Dimensions", 5), 3, 5);
		setSize = Mathf.Clamp (PlayerPrefs.GetInt ("SetSize", 4), 3, 4);
	}

	//after "Deck finished": a new table for the same difficulty (the family board starts a new season with a new seed)
	void NewGame ()
	{
		if (busy) {
			return;
		}
		StoreBoard ();
		if (familyMode) {
			FamilyState.NextSeason ();
		}
		boards.Remove (BoardKey ());
		message = "";
		LoadBoard ();
	}

}
