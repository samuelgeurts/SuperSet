using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckModel : MonoBehaviour {


	List<int> cards; //lazy instansiation, 

	public IEnumerable<int> GetCards()
	{
		foreach (int i in cards) {
			yield return i;
		}
	}


//	public int NumberOfSets() {




//	}

	//dimensions = 3..5 and setSize = 3 or 4 (values per dimension): the deck only holds the cards that fit
	public void Shuffle(int dimensions = SetLogic.Dimensions, int setSize = 4) {
		if (cards == null) {
			cards = new List<int> ();
		} else {
			cards.Clear ();
		}

		int deckSize = SetLogic.DeckSize (dimensions, setSize);
		for (int i = 0; i < deckSize; i++) {
			cards.Add (SetLogic.DeckCardIndex (i, dimensions, setSize));
		}

		//Shuffle part
		int n = cards.Count;
		while (n > 1) {
			n--;
			int k = Random.Range (0, n + 1);
			int temp = cards [k];
			cards [k] = cards [n];
			cards [n] = temp;
		}

	}

	void Start ()
	{
		Shuffle();
	}



}
