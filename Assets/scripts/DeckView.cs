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
	public int numberofSets; //sets on the table that are not found yet
	public List<int> shownCardIndex = new List<int> ();

	//difficulty: number of dimensions (3..5) and set size (3 or 4 cards, also the number of values per dimension)
	public int dimensionCount = 5;
	public int setSize = 4;
	public const int MaxBoardSize = 36;

	public List<CardModel> selectedCards = new List<CardModel> ();
	List<int[]> boardSets = new List<int[]> (); //all sets on the table, as slot numbers
	List<int[]> foundSets = new List<int[]> (); //the sets of the table the player already found

	//select or deselect a clicked card; one card too many is ignored until one is deselected
	public void ToggleSelection (CardModel card)
	{
		if (gameOver) {
			return;
		}
		if (selectedCards.Remove (card)) {
			card.SetSelected (false);
		} else if (selectedCards.Count < setSize) {
			selectedCards.Add (card);
			card.SetSelected (true);
			if (selectedCards.Count == setSize) {
				CheckSelection ();
			}
		}
	}

	public int score; //total over all games; kept between sessions
	const string ScoreKey = "TotalScore";

	void SaveScore ()
	{
		PlayerPrefs.SetInt (ScoreKey, score);
		PlayerPrefs.Save ();
	}

	string message = "";
	float messageUntil;

	//true if the selected cards form a set: every dimension all the same or all different
	bool SelectionIsSet ()
	{
		int[] indexes = new int[setSize];
		for (int c = 0; c < setSize; c++) {
			indexes [c] = selectedCards [c].cardIndex;
		}
		return SetLogic.IsSetCards (indexes, dimensionCount);
	}

	void CheckSelection ()
	{
		if (!SelectionIsSet ()) {
			ShowMessage ("Geen set");
			ClearSelection ();
			return;
		}

		int[] found = new int[setSize];
		for (int c = 0; c < setSize; c++) {
			found [c] = System.Array.IndexOf (slots, selectedCards [c]);
		}
		System.Array.Sort (found);
		ClearSelection ();

		if (IsFound (found)) {
			ShowMessage ("Die set had je al");
			return;
		}
		foundSets.Add (found);
		score += SetLogic.PointsPerSet (dimensionCount, setSize);
		SaveScore ();
		foreach (int slot in found) {
			slots [slot].SetFound (true);
		}
		numberofSets = boardSets.Count - foundSets.Count;
		Numberofsets.numberofSets = numberofSets;

		if (numberofSets > 0) {
			//more sets on the table: the cards stay, also the ones shared with the next set
			ShowMessage ("Set gevonden! Nog " + numberofSets + " op tafel");
		} else {
			ShowMessage ("Set gevonden!");
			ReplaceFoundCards ();
		}
	}

	bool IsFound (int[] set)
	{
		foreach (int[] other in foundSets) {
			if (System.Linq.Enumerable.SequenceEqual (other, set)) {
				return true;
			}
		}
		return false;
	}

	//all sets on the table are found: put new cards on the places of every card from a found set
	void ReplaceFoundCards ()
	{
		HashSet<int> used = new HashSet<int> ();
		foreach (int[] set in foundSets) {
			foreach (int slot in set) {
				used.Add (slot);
			}
		}
		foreach (int slot in used) {
			ChangeCard (slot);
		}
		EnsureSetOnBoard ();
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

	static readonly string[] dimensionNames = { "kleur", "vorm", "aantal", "vulling", "grootte" };

	//the rule of the game, for the difficulty that is chosen now
	string RuleText ()
	{
		string names = "";
		for (int d = 0; d < dimensionCount; d++) {
			names += (d == 0 ? "" : (d == dimensionCount - 1 ? " en " : ", ")) + dimensionNames [d];
		}
		return "Spelregel: kies " + setSize + " kaarten waarbij ieder kenmerk (" + names +
		") op alle kaarten gelijk is OF op alle kaarten verschillend. Gevonden sets krijgen een rode rand; " +
		"nieuwe kaarten komen pas als alle sets op tafel gevonden zijn.";
	}

	GUIStyle bigStyle;
	GUIStyle smallStyle;
	GUIStyle ruleStyle;
	GUIStyle buttonStyle;

	//a button of the menu; the chosen option is colored. Returns true when another option was clicked
	bool MenuButton (Rect rect, string text, bool chosen)
	{
		Color old = GUI.backgroundColor;
		if (chosen) {
			GUI.backgroundColor = new Color (0.3f, 0.85f, 0.75f);
		}
		bool clicked = GUI.Button (rect, text, buttonStyle);
		GUI.backgroundColor = old;
		return clicked && !chosen;
	}

	void OnGUI ()
	{
		if (bigStyle == null) {
			bigStyle = new GUIStyle (GUI.skin.label);
			bigStyle.fontSize = 22;
			bigStyle.normal.textColor = Color.white;
			smallStyle = new GUIStyle (GUI.skin.label);
			smallStyle.fontSize = 14;
			smallStyle.normal.textColor = new Color (0.8f, 0.85f, 1f);
			ruleStyle = new GUIStyle (smallStyle);
			ruleStyle.fontSize = 12;
			ruleStyle.wordWrap = true;
			buttonStyle = new GUIStyle (GUI.skin.button);
			buttonStyle.fontSize = 13;
		}

		GUI.Label (new Rect (10, 2, Screen.width - 20, 40), RuleText (), ruleStyle);

		//menu in a narrow column on the left, under each other, so it never covers the cards
		const float x = 10;
		const float w = 105;
		const float top = 48;
		GUI.Label (new Rect (x, top, w, 22), "Score", smallStyle);
		GUI.Label (new Rect (x, top + 17, w + 40, 32), score.ToString (), bigStyle);
		GUI.Label (new Rect (x, top + 57, w, 22), "Punten per set", smallStyle);
		GUI.Label (new Rect (x, top + 74, w, 32), "+" + SetLogic.PointsPerSet (dimensionCount, setSize), bigStyle);

		GUI.Label (new Rect (x, top + 115, w, 22), "Eigenschappen", smallStyle);
		for (int d = 3; d <= 5; d++) {
			string text = d + "   +" + SetLogic.PointsPerSet (d, setSize);
			if (MenuButton (new Rect (x, top + 137 + (d - 3) * 30, w, 26), text, d == dimensionCount)) {
				SetDifficulty (d, setSize);
			}
		}

		GUI.Label (new Rect (x, top + 233, w, 22), "Kaarten per set", smallStyle);
		for (int s = 3; s <= 4; s++) {
			string text = s + " kaarten +" + SetLogic.PointsPerSet (dimensionCount, s);
			if (MenuButton (new Rect (x, top + 255 + (s - 3) * 30, w, 26), text, s == setSize)) {
				SetDifficulty (dimensionCount, s);
			}
		}

		if (Time.time < messageUntil) {
			GUI.Label (new Rect (x, top + 325, w + 30, 70), message, smallStyle);
		}

		if (gameOver) {
			float bw = 320;
			float bh = 160;
			Rect box = new Rect ((Screen.width - bw) / 2, (Screen.height - bh) / 2, bw, bh);
			GUI.Box (box, "");
			GUI.Label (new Rect (box.x + 20, box.y + 15, bw - 40, 32), "Spel afgelopen", bigStyle);
			GUI.Label (new Rect (box.x + 20, box.y + 50, bw - 40, 32), "Score: " + score, bigStyle);
			if (GUI.Button (new Rect (box.x + 20, box.y + 100, bw - 40, 36), "Nieuw spel")) {
				NewGame ();
			}
		}
	}

	void Start ()
	{
		deck = GetComponent<DeckModel> ();
		dimensionCount = Mathf.Clamp (PlayerPrefs.GetInt ("Dimensions", dimensionCount), 3, 5);
		setSize = Mathf.Clamp (PlayerPrefs.GetInt ("SetSize", setSize), 3, 4);
		score = PlayerPrefs.GetInt (ScoreKey, 0);
		cardPrefab.SetActive (false); //the card in the scene is only a template, not a playing card
		LoadBoard ();
	}

	//the table of one difficulty; kept while the player looks at another difficulty
	class Board
	{
		public List<int> deckOrder; //shuffled order of all cards
		public int nextCard; //position in deckOrder of the next card to draw
		public List<int> cardIndex; //card on every slot
		public bool[] slotActive;
		public List<int[]> foundSets;
		public bool gameOver;
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
		if (!boards.TryGetValue (BoardKey (), out board)) {
			StartNewBoard ();
		} else {
			deckOrder = board.deckOrder;
			nextCard = board.nextCard;
			shownCardIndex = board.cardIndex;
			slotActive = board.slotActive;
			foundSets = board.foundSets;
			gameOver = board.gameOver;
			for (int slot = 0; slot < boardSize; slot++) {
				SetDimensions (slot);
			}
			FindSets ();
		}
		ShowCards ();
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

	//put the next card from the deck on a slot; hides the slot when the deck is empty
	void ChangeCard (int slot)
	{
		if (nextCard >= deckOrder.Count) {
			slotActive [slot] = false;
		} else {
			shownCardIndex [slot] = deckOrder [nextCard++];
			SetDimensions (slot);
		}
		CardModel model = slots [slot];
		if (model != null) {
			model.SetFound (false);
			model.gameObject.SetActive (slotActive [slot]);
			model.cardIndex = shownCardIndex [slot];
			model.ToggleFace (true);
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
		int columns = SetLogic.BoardColumns (dimensionCount, setSize);
		int rows = boardSize / columns;

		for (int slot = 0; slot < boardSize; slot++) {
			int coordx = slot % columns;
			int coordy = slot / columns;

			//center a smaller board in the space of the full 9 x 4 board
			float cox = cardOffsetx * (coordx + (9 - columns) / 2f);
			float coy = cardOffsety * (coordy + (4 - rows) / 2f);

			GameObject cardCopy = (GameObject)Instantiate (cardPrefab); //creat a copy of "cardPrefab"
			cardCopy.SetActive (true); //the template is inactive, so is its copy
			Vector3 temp = start + new Vector3 (cox, coy); //tempory position startposition + offset times current card
			cardCopy.transform.position = temp;

			CardModel cardModel = cardCopy.GetComponent<CardModel> ();
			cardModel.cardIndex = shownCardIndex [slot];
			cardModel.deckView = this;
			cardModel.ToggleFace (true);
			slots [slot] = cardModel;
			cardCopy.SetActive (slotActive [slot]);
		}

		//the sets that were found already keep their red border
		foreach (int[] set in foundSets) {
			foreach (int slot in set) {
				slots [slot].SetFound (true);
			}
		}
	}

	//change the difficulty; the table of that difficulty stays as the player left it
	public void SetDifficulty (int dimensions, int size)
	{
		StoreBoard ();
		dimensionCount = Mathf.Clamp (dimensions, 3, 5);
		setSize = Mathf.Clamp (size, 3, 4);
		PlayerPrefs.SetInt ("Dimensions", dimensionCount);
		PlayerPrefs.SetInt ("SetSize", setSize);
		message = "";
		LoadBoard ();
	}

	//after "Spel afgelopen": a new table for the same difficulty
	void NewGame ()
	{
		StoreBoard ();
		boards.Remove (BoardKey ());
		message = "";
		LoadBoard ();
	}

}
