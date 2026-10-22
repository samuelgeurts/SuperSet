using System.Collections.Generic;
using UnityEngine;

//deckview needs deck
[RequireComponent (typeof(DeckModel))]

public partial class DeckView : MonoBehaviour
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

	//family mode: one shared board (seed + log of found sets) that everybody plays on; see FamilyState
	bool familyMode;
	FamilyState family;

	//select or deselect a clicked card; one card too many is ignored until one is deselected
	public void ToggleSelection (CardModel card)
	{
		if (gameOver || busy || askingName) {
			return;
		}
		if (openMenu != TopMenu.None || scoresOpen || menuClosedFrame == Time.frameCount) {
			CloseMenu (); //a click on a card only closes the open menu
			return;
		}
		if (selectedCards.Remove (card)) {
			card.SetSelected (false);
		} else if (selectedCards.Count < setSize) {
			if (ruleOpen) {
				SetRuleOpen (false);
			}
			selectedCards.Add (card);
			card.SetSelected (true, NextFoundColor ()); //the color the set will get
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
			List<CardModel> wrongCards = new List<CardModel> (selectedCards);
			ClearSelection ();
			StartCoroutine (WrongSequence (wrongCards));
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
		if (FamilyOnline) {
			StartCoroutine (PostFamilyClaim (found)); //it counts when the database accepts it
			return;
		}
		AcceptFound (found);
	}

	//a set of the player is found (and, on the online family board, accepted by the database)
	void AcceptFound (int[] found)
	{
		RegisterFound (found, familyMode ? family.localId : null, familyMode ? family.localPlayer : null);
		if (familyMode) {
			family.AddClaim (family.localId, family.localPlayer, found);
		}
		foreach (int slot in found) {
			slots [slot].AddFound (FoundColorOf (foundSets.Count - 1));
		}

		if (numberofSets > 0) {
			//more sets on the table: the cards stay, also the ones shared with the next set
			ShowMessage ("Set found! " + numberofSets + " more on the table");
		} else {
			ShowMessage ("Set found!");
		}
		StartCoroutine (FoundSequence (found, numberofSets <= 0));
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

	//a turned phone or a resized window: lay the table out again
	void Update ()
	{
		if (Screen.width != layoutWidth || Screen.height != layoutHeight) {
			ApplyLayout ();
		}
		if (ruleOpen && Time.time - ruleOpenSince > RuleAutoCloseSeconds) {
			SetRuleOpen (false);
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

}
