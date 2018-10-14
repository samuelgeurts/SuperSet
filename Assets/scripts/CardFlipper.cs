using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardFlipper : MonoBehaviour { //component adde to the card
	SpriteRenderer spriteRenderer;
	CardModel model;

	public AnimationCurve scaleCurve;
	public float duration = 0.5f;
	/*dont use a constructer but use Awake. Dont use contructor in 
	MonoBehaviour because Unity goes through some initialization 
	routines and could call them more then ones.*/

	void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer> ();
		model = GetComponent<CardModel> (); 
	}

	public void FlipCard(Sprite startImage, Sprite endImage, int cardIndex){
		StopCoroutine(Flip(startImage, endImage, cardIndex));		// stops the coroutine if a next click
		StartCoroutine(Flip(startImage, endImage, cardIndex));
	}

	IEnumerator Flip(Sprite startImage, Sprite endImage, int cardIndex) // so Flip is a coroutine
	// this function 
	{
		spriteRenderer.sprite = startImage;

		float time = 0f;
		while (time <= 1f) {
			float scale = scaleCurve.Evaluate (time);
			time = time + Time.deltaTime / duration; //deltaTime comes from the system, we can alter duration

			Vector3 localScale = transform.localScale; // grap LocalScale
			localScale.y = scale;		
			localScale.x = scale;						


			transform.localScale = localScale;

				if (time >= 0.5f) //at half time the image flips
				{
					spriteRenderer.sprite = endImage;
				}

					yield return new WaitForFixedUpdate(); // we have to do this because we run an IEnumeration class that comes with Unity
		}
		if(cardIndex == -1){ 
		model.ToggleFace (false);
		}
		else{
			model.cardIndex = cardIndex;
				model.ToggleFace(true);
		}
			

	}


}
