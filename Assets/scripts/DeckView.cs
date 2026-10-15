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
			ShowMessage ("Not a set");
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
			ShowMessage ("You already found that set");
			return;
		}
		foundSets.Add (found);
		score += SetLogic.PointsPerSet (dimensionCount, setSize);
		SaveScore ();
		foreach (int slot in found) {
			slots [slot].AddFound (UiStyle.FoundColor (foundSets.Count - 1));
		}
		numberofSets = boardSets.Count - foundSets.Count;
		Numberofsets.numberofSets = numberofSets;

		if (numberofSets > 0) {
			//more sets on the table: the cards stay, also the ones shared with the next set
			ShowMessage ("Set found! " + numberofSets + " more on the table");
		} else {
			ShowMessage ("Set found!");
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

	static readonly string[] dimensionNames = { "color", "shape", "number", "fill", "size" };

	//the rule of the game, for the difficulty that is chosen now
	string RuleText ()
	{
		string names = "";
		for (int d = 0; d < dimensionCount; d++) {
			names += (d == 0 ? "" : (d == dimensionCount - 1 ? " and " : ", ")) + dimensionNames [d];
		}
		return "Rule: pick " + setSize + " cards where every attribute (" + names +
		") is the same on all cards OR different on all cards. Each set you find gets its own border color; " +
		"new cards only come when all sets on the table are found.";
	}

	//a button of the menu; the chosen option is colored. Returns true when another option was clicked
	bool MenuButton (Rect rect, string text, bool chosen)
	{
		bool clicked = GUI.Button (rect, text, chosen ? UiStyle.ChosenButtonStyle : UiStyle.ButtonStyle);
		return clicked && !chosen;
	}

	//the screen is higher than wide (a phone held upright): the board and the menu are laid out vertically
	static bool Portrait {
		get { return Screen.height > Screen.width; }
	}

	const float PortraitBannerHeight = 100;
	const float PortraitPanelHeight = 164;
	const float ButtonHeight = 32;
	const float ButtonStep = 36;

	//size of one GUI unit in screen pixels, so the menu keeps its size relative to the screen on a phone
	static float UiScale ()
	{
		float scale = Portrait ? Screen.width / 360f : Mathf.Min (Screen.width / 700f, Screen.height / 450f);
		return Mathf.Clamp (scale, 0.5f, 4f);
	}

	//the part of the screen for the cards, in GUI units (y from the top): what the menu leaves free
	static Rect Playfield (float w, float h)
	{
		if (Portrait) {
			float top = 4 + PortraitBannerHeight + 4;
			return new Rect (4, top, w - 8, h - top - PortraitPanelHeight - 4);
		}
		return new Rect (120, 52, w - 124, h - 56);
	}

	void DrawScore (float x, float y, float w)
	{
		GUI.Label (new Rect (x, y, w, 18), "Score", UiStyle.LabelStyle);
		GUI.Label (new Rect (x, y + 15, w, 30), score.ToString (), UiStyle.TitleStyle);
		GUI.Label (new Rect (x, y + 52, w, 18), "Points per set", UiStyle.LabelStyle);
		GUI.Label (new Rect (x, y + 67, w, 30), "+" + SetLogic.PointsPerSet (dimensionCount, setSize), UiStyle.ValueStyle);
	}

	void DrawDimensionButtons (float x, float y, float w)
	{
		GUI.Label (new Rect (x, y, w, 18), "Attributes", UiStyle.LabelStyle);
		for (int d = 3; d <= 5; d++) {
			string text = d + "   +" + SetLogic.PointsPerSet (d, setSize);
			if (MenuButton (new Rect (x, y + 20 + (d - 3) * ButtonStep, w, ButtonHeight), text, d == dimensionCount)) {
				SetDifficulty (d, setSize);
			}
		}
	}

	void DrawSizeButtons (float x, float y, float w)
	{
		GUI.Label (new Rect (x, y, w, 18), "Cards per set", UiStyle.LabelStyle);
		for (int s = 3; s <= 4; s++) {
			string text = s + " cards +" + SetLogic.PointsPerSet (dimensionCount, s);
			if (MenuButton (new Rect (x, y + 20 + (s - 3) * ButtonStep, w, ButtonHeight), text, s == setSize)) {
				SetDifficulty (dimensionCount, s);
			}
		}
	}

	void DrawMessage (Rect rect)
	{
		if (Time.time < messageUntil) {
			GUI.Label (rect, message, UiStyle.MessageStyle);
		}
	}

	void OnGUI ()
	{
		UiStyle.EnsureStyles ();

		//draw in GUI units; the matrix scales them to the screen (clicks follow the same matrix)
		float scale = UiScale ();
		GUI.matrix = Matrix4x4.Scale (new Vector3 (scale, scale, 1f));
		float width = Screen.width / scale;
		float height = Screen.height / scale;

		if (Portrait) {
			//the rule of the game in a banner at the top, the menu in a panel at the bottom
			GUI.Box (new Rect (4, 4, width - 8, PortraitBannerHeight), RuleText (), UiStyle.BannerStyle);
			float top = height - PortraitPanelHeight + 8;
			GUI.Box (new Rect (4, height - PortraitPanelHeight, width - 8, PortraitPanelHeight - 4), "", UiStyle.PanelStyle);
			float third = (width - 28) / 3f;
			DrawScore (14, top, third - 8);
			DrawDimensionButtons (14 + third, top, third - 8);
			DrawSizeButtons (14 + 2 * third, top, third - 8);
			DrawMessage (new Rect (14 + 2 * third, top + 98, third - 8, 44));
		} else {
			//the rule of the game in a banner at the top, the menu in a narrow panel on the left so it never covers the cards
			GUI.Box (new Rect (8, 4, width - 16, 40), RuleText (), UiStyle.BannerStyle);
			const float x = 10;
			const float w = 98;
			const float top = 54;
			GUI.Box (new Rect (2, top - 6, 112, 392), "", UiStyle.PanelStyle);
			DrawScore (x, top, w);
			DrawDimensionButtons (x, top + 104, w);
			DrawSizeButtons (x, top + 240, w);
			DrawMessage (new Rect (x, top + 338, w, 40));
		}

		if (gameOver) {
			float bw = 320;
			float bh = 170;
			Rect box = new Rect ((width - bw) / 2, (height - bh) / 2, bw, bh);
			GUI.Box (box, "", UiStyle.PanelStyle);
			GUI.Label (new Rect (box.x + 24, box.y + 18, bw - 48, 32), "Game over", UiStyle.TitleStyle);
			GUI.Label (new Rect (box.x + 24, box.y + 56, bw - 48, 32), "Score: " + score, UiStyle.ValueStyle);
			if (GUI.Button (new Rect (box.x + 24, box.y + 106, bw - 48, 40), "New game", UiStyle.ActionButtonStyle)) {
				NewGame ();
			}
		}
	}

	//a turned phone or a resized window: lay the table out again
	void Update ()
	{
		if (Screen.width != layoutWidth || Screen.height != layoutHeight) {
			ApplyLayout ();
		}
	}

	void Start ()
	{
		deck = GetComponent<DeckModel> ();
		dimensionCount = Mathf.Clamp (PlayerPrefs.GetInt ("Dimensions", dimensionCount), 3, 5);
		setSize = Mathf.Clamp (PlayerPrefs.GetInt ("SetSize", setSize), 3, 4);
		score = PlayerPrefs.GetInt (ScoreKey, 0);
		UiStyle.CreateBackground ();
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
			model.ClearFound ();
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

	int layoutWidth, layoutHeight;

	//the number of columns that makes the cards biggest; fieldWide and fieldHigh are the free space in card widths and heights.
	//A table with a full last row wins over a tiny gain.
	static int BestColumns (int count, float fieldWide, float fieldHigh)
	{
		int best = 1;
		float bestSize = 0f;
		for (int c = 1; c <= count; c++) {
			int r = (count + c - 1) / c;
			float size = Mathf.Min (fieldWide / c, fieldHigh / r);
			if (count % c == 0) {
				size *= 1.04f;
			}
			if (size > bestSize) {
				bestSize = size;
				best = c;
			}
		}
		return best;
	}

	//put the cards in a grid that suits the screen and zoom the camera so the grid fits in the space the menu leaves free.
	//The number of columns follows the shape of that space, so a phone held upright gets more rows than columns.
	void ApplyLayout ()
	{
		layoutWidth = Screen.width;
		layoutHeight = Screen.height;
		Camera cam = Camera.main;
		if (slots [0] == null || cam == null || Screen.width == 0 || Screen.height == 0) {
			return;
		}

		float scale = UiScale ();
		Rect field = Playfield (Screen.width / scale, Screen.height / scale);
		float cellWidth = Mathf.Abs (cardOffsetx);
		float cellHeight = Mathf.Abs (cardOffsety);
		int columns = BestColumns (boardSize, field.width * scale / cellWidth, field.height * scale / cellHeight);
		int rows = (boardSize + columns - 1) / columns;

		//the grid is centered on the world origin; a shorter last row is centered too
		for (int slot = 0; slot < boardSize; slot++) {
			int row = slot / columns;
			int inRow = Mathf.Min (columns, boardSize - row * columns);
			float x = (slot % columns - (inRow - 1) / 2f) * cellWidth;
			float y = ((rows - 1) / 2f - row) * cellHeight;
			slots [slot].transform.position = new Vector3 (x, y, 0f);
		}

		//zoom so the grid fills the free space, but not far bigger than the original size on a small table
		float pixelsPerUnit = Mathf.Min (field.width * scale / (columns * cellWidth), field.height * scale / (rows * cellHeight));
		pixelsPerUnit = Mathf.Min (pixelsPerUnit, Screen.height / 40f * 1.6f);
		cam.orthographicSize = Screen.height / (2f * pixelsPerUnit);

		//move the camera so the middle of the grid lies in the middle of the free space
		float centerX = (field.x + field.width / 2f) * scale; //pixels from the left
		float centerY = Screen.height - (field.y + field.height / 2f) * scale; //pixels from the bottom
		cam.transform.position = new Vector3 ((Screen.width / 2f - centerX) / pixelsPerUnit,
			(Screen.height / 2f - centerY) / pixelsPerUnit, cam.transform.position.z);
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

	//after "Game over": a new table for the same difficulty
	void NewGame ()
	{
		StoreBoard ();
		boards.Remove (BoardKey ());
		message = "";
		LoadBoard ();
	}

}
