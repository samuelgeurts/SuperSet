using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugChangeCard : MonoBehaviour {

	RaycastHit hit;

	CardFlipper flipper;
	CardModel cardModel; //this is private
	int cardIndex = 0;

	public GameObject card;

	void Awake()
	{
		cardModel = card.GetComponent<CardModel> ();
		flipper = card.GetComponent<CardFlipper> ();
	}

	void OnGUI()
	{
		if (GUI.Button(new Rect (10, 10, 100, 28), "Hit me!")) {
			

			if (cardIndex >= cardModel.faces.Length) {
				cardIndex = 0;
				//cardModel.ToggleFace(false);
				flipper.FlipCard(cardModel.faces[cardModel.faces.Length - 1], cardModel.cardBack, -1);
			} else {
				//cardModel.cardIndex = cardIndex;
				//cardModel.ToggleFace (true);
				if (cardIndex > 0){
					flipper.FlipCard (cardModel.faces [cardIndex - 1], cardModel.faces [cardIndex], cardIndex);
				} else{
					flipper.FlipCard(cardModel.cardBack, cardModel.faces[cardIndex], cardIndex);
				}

				cardIndex++;
			}
		}

	}

//	void Update () {
//
//		if (Input.GetMouseButtonDown (0) && hit.card.tag == "Player") {
//			print ("You Clicked the Player!");
//			myLight = hit.transform.gameObject.GetComponent<Light> ();
//			myLight.enabled = true;
//			Selected = true;             
//		}
//
//	}





}
