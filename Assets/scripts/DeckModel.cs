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

	public void Shuffle() {
		if (cards == null) {
			cards = new List<int> ();
		} else {
			cards.Clear ();
		}

		for (int i = 0; i < 1024; i++) {
			cards.Add (i);
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
