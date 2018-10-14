using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Numberofsets : MonoBehaviour {

	public static int numberofSets = 0;
	Text score;

	// Use this for initialization
	void Start () {
		score = GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update () {
		score.text = "Number of Sets: " + numberofSets;
		
	}
}
