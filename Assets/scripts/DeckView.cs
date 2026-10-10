using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using System;
using UnityEngine.SceneManagement;

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
	public int[] set1 = new int[4];
	public int numberofSets;
	public List<int> shownCardIndex = new List<int> ();

	public const int SetSize = 4; //cards needed for one set
	public List<CardModel> selectedCards = new List<CardModel> ();

	//select or deselect a clicked card; a fifth card is ignored until one is deselected
	public void ToggleSelection (CardModel card)
	{
		if (selectedCards.Remove (card)) {
			card.SetSelected (false);
		} else if (selectedCards.Count < SetSize) {
			selectedCards.Add (card);
			card.SetSelected (true);
		}
	}


	//	//function to check if all parameters are the same
	//	bool AllSame(params int[] list)
	//	{
	//		return (list as IEnumerable<int>).Equals ();
	//	}
	//
	//	bool AllDifferent(params int[] list)
	//	{
	//		for list
	//
	//		return (list as IEnumerable<int>). ();
	//	}

	// return true if al three cards are different or the same
	bool IsSet (int a1, int a2, int a3)
	{
		return ((a1 == a2 && a1 == a3) || (a1 != a2 && a1 != a3 && a2 != a3));
	}

	bool IsSet4 (int a1, int a2, int a3, int a4)
	{
		return ((a1 == a2 && a1 == a3 && a1 == a4) ||
		(a4 != a1 && a4 != a2 && a4 != a3 && a1 != a2 && a1 != a3 && a2 != a3));
	}

	void Start ()
	{
		deck = GetComponent<DeckModel> ();
		ShowCards ();

		numberofSets = NumberOfSets(shownCardDimension);
			
	}

	//List<int> foundSets = new List<int>();

	public int DimensionToIndex (int[] coord5)
	{

		int temp;
		//scale
		if (coord5 [4] == 0) {
			temp = 0;
		} else if (coord5 [4] == 1) {
			temp = 16;
		} else if (coord5 [4] == 2) {
			temp = 512;
		} else { 
			temp = 512 + 16;
		}
		//color + shape + number + structure + scale
		return (coord5 [0] * 4) + (coord5 [1] * 128) + coord5 [2] + (coord5 [3] * 32) + temp;
	}

	public int[] IndexToDimension (int index)
	{
	
		int[] temp = new int[5];

		int color = (index % 16) / 4;
		int shape = (index / 128) % 4;
		int number = index % 4;
		int structure = (index / 32) % 4;
		int scale;

		int scaletemp = (index / 16) % 2;

		if ((scaletemp == 0) && index < 512) {
			scale = 0; 
		} else if (scaletemp == 1 && index < 512) {
			scale = 1;
		} else if (scaletemp == 0 && index >= 512) {
			scale = 2;
		} else {
			scale = 3;
		}
			
		temp [0] = color;
		temp [1] = shape;
		temp [2] = number;
		temp [3] = structure;
		temp [4] = scale;

		//Debug.Log ("Color = " + dimensions[0]);
		//Debug.Log ("Shape = " + dimensions[1]);
		//Debug.Log ("Number = " + dimensions[2]);
		//Debug.Log ("Structure = " + dimensions[3]);
		//Debug.Log ("Scale = " + dimensions[4]);

		return temp;
	}

	public int NumberOfSets (int[,] array)
	{
		Numberofsets.numberofSets = 0;
		int numberofSets = 0;
		//cards belonging to found set
		int[] card1 = new int[5];
		int[] card2 = new int[5];
		int[] card3 = new int[5];
		int[] card4 = new int[5];

		//shuffle through all carts
		for (int k1 = 0; k1 < 36; k1++) {
			for (int k2 = k1 + 1; k2 < 36; k2++) {

				//exclude same card
				//if (k2 != k1) {

				for (int k3 = k2 + 1; k3 < 36; k3++) {

					if (//k3 != k2 && k3 != k1 && //exclude same card
						IsSet (array [0, k1], array [0, k2], array [0, k3]) && //check color
						IsSet (array [1, k1], array [1, k2], array [1, k3]) && //check ...
						IsSet (array [2, k1], array [2, k2], array [2, k3]) &&
						IsSet (array [3, k1], array [3, k2], array [3, k3]) &&
						IsSet (array [4, k1], array [4, k2], array [4, k3])) {

						//Debug.Log ("k1 = " + k1);
						//Debug.Log ("k2 = " + k2);
						//Debug.Log ("k3 = " + k3);

						for (int k4 = k3 + 1; k4 < 36; k4++) {

							// found a set
							if (//k4 != k1 && k4 != k2 && k4 != k3 && 
								IsSet4 (array [0, k1], array [0, k2], array [0, k3], array [0, k4]) &&
								IsSet4 (array [1, k1], array [1, k2], array [1, k3], array [1, k4]) &&
								IsSet4 (array [2, k1], array [2, k2], array [2, k3], array [2, k4]) &&
								IsSet4 (array [3, k1], array [3, k2], array [3, k3], array [3, k4]) &&
								IsSet4 (array [4, k1], array [4, k2], array [4, k3], array [4, k4])) {

								//2d array to 4 1d arrays
								for (int l1 = 0; l1 < 5; l1++) {
									card1 [l1] = array [l1, k1];
									card2 [l1] = array [l1, k2];
									card3 [l1] = array [l1, k3];
									card4 [l1] = array [l1, k4];
								}
								//index of set cards
								set1 [0] = DimensionToIndex (card1);
								set1 [1] = DimensionToIndex (card2);
								set1 [2] = DimensionToIndex (card3);
								set1 [3] = DimensionToIndex (card4);

								Debug.Log ("cards are = " + set1 [0] + " " + set1 [1] + " " + set1 [2] + " " + set1 [3]);

								numberofSets++;
								Numberofsets.numberofSets++;
							}
						}
					}
				}
				//if (numberofSets == 0) {
				//	SceneManager.LoadScene("Card Deck Test");
				//}
					
					
				//}
			}
		}
		return numberofSets;
	}

	void ShowCards ()
	{ 
		int cardCount = 0;

//		foreach(int i in deck.GetCards())
//		{
//			float co = cardOffset * cardCount;
//
//			GameObject cardCopy = (GameObject)Instantiate (cardPrefab); //creat a copy of "cardPrefab"
//			Vector3 temp = start + new Vector3 (co, 0f); //tempory position startposition + offset times current card
//			cardCopy.transform.position = temp;
//
//			CardModel cardModel = cardCopy.GetComponent<CardModel> ();
//			cardModel.cardIndex = i;
//			cardModel.ToggleFace (true);
//
//
//			cardCount++;
//			//cardCopy.transform.position.x =  
//		}

		int coordx = 0; 
		int coordy = 0;
		//int[][] dimensions;

		foreach (int i in deck.GetCards()) {
			if (cardCount < 36) {
				if (coordx >= 9) {
					coordx = 0;
					coordy++;
				}
					
				float cox = cardOffsetx * coordx;
				float coy = cardOffsety * coordy;

				//Console.Write("{coordx} ");
				//Console.WriteLine ();

				GameObject cardCopy = (GameObject)Instantiate (cardPrefab); //creat a copy of "cardPrefab"
				Vector3 temp = start + new Vector3 (cox, coy); //tempory position startposition + offset times current card
				cardCopy.transform.position = temp;

				CardModel cardModel = cardCopy.GetComponent<CardModel> ();
				cardModel.cardIndex = i;
				cardModel.deckView = this;
				cardModel.ToggleFace (true);



				shownCardIndex.Add (i); //stores the shown card indexes in the List: shownCardIndex

				int[] dimensions = IndexToDimension (i);
				//generated 2d array with 5d coordinates
				for (int j = 0; j < 5; j++) {
					shownCardDimension [j, cardCount] = dimensions [j];
				}


				//for showing indexes and coordinates/dimensions at console
//				int indexx = DimensionToIndex (dimensions);
//				Debug.Log ("Card number = " + i);
//				Debug.Log ("Color = " + dimensions [0]);
//				Debug.Log ("Shape = " + dimensions [1]);
//				Debug.Log ("Number = " + dimensions [2]);
//				Debug.Log ("Structure = " + dimensions [3]);
//				Debug.Log ("Scale = " + dimensions [4]);
//				Debug.Log ("Indexx = " + indexx);
//				Debug.Log ("card1 = " + set1[0] + " " + set1[1]+ " " + set1[1]+ " " + set1[1]);







					

				cardCount++;
				coordx++;


			}
		}



	}

	public void Restart ()
	{
		SceneManager.LoadScene ("Card Deck Test");
	}


}
