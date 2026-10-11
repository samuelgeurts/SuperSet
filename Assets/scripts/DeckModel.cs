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

	//dimensions = 3..5; a deck with fewer dimensions only holds the cards that differ in those dimensions
	public void Shuffle(int dimensions = SetLogic.Dimensions) {
		if (cards == null) {
			cards = new List<int> ();
		} else {
			cards.Clear ();
		}

		int deckSize = 1 << (2 * dimensions); //4 values per dimension
		for (int i = 0; i < deckSize; i++) {
			cards.Add (SetLogic.DeckCardIndex (i));
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
