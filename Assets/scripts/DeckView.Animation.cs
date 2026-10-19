using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the moving parts: the draw pile and the discard pile, dealing cards, showing a found set and shaking a wrong one
public partial class DeckView
{

	bool busy; //an animation runs: the player cannot pick cards or change the difficulty

	int discardCount; //cards on the discard pile
	int discardTopIndex = -1; //card on top of the discard pile, -1 when it is empty
	int drawShown; //cards left on the draw pile, as shown on the screen

	const int PileLayers = 4;
	SpriteRenderer[] drawLayers = new SpriteRenderer[PileLayers];
	SpriteRenderer[] discardLayers = new SpriteRenderer[PileLayers - 1];
	SpriteRenderer discardPlace, dim;
	CardModel discardTop;
	int shownTopIndex = -2;
	int pileLayerId, pileOrder;

	const int LiftOrder = 100; //sorting boost of a card that moves over the table

	//everything a card does in one move: from where to where, when it starts and how long it takes
	class Flight
	{
		public CardModel card;
		public Vector3 from;
		public System.Func<Vector3> to;
		public System.Func<float> toScale; //size compared with a card on the table; null means normal size
		public float fromScale = 1f;
		public float delay, duration;
		public System.Action onStart, onArrive;
		public bool started, finished;
	}

	static float EaseOut (float k)
	{
		k = Mathf.Clamp01 (k);
		return 1f - (1f - k) * (1f - k) * (1f - k);
	}

	//move all cards of the list, each from its own start time, until every one has arrived
	IEnumerator RunFlights (List<Flight> flights)
	{
		float t = 0f;
		while (true) {
			bool allDone = true;
			foreach (Flight f in flights) {
				if (f.finished) {
					continue;
				}
				allDone = false;
				float local = t - f.delay;
				if (local < 0f) {
					continue;
				}
				if (!f.started) {
					f.started = true;
					if (f.onStart != null) {
						f.onStart ();
					}
				}
				float k = Mathf.Clamp01 (local / f.duration);
				float e = EaseOut (k);
				float toScale = f.toScale != null ? f.toScale () : 1f;
				f.card.transform.position = Vector3.Lerp (f.from, f.to (), e);
				f.card.transform.localScale = f.card.BaseScale * Mathf.Lerp (f.fromScale, toScale, e);
				if (k >= 1f) {
					f.finished = true;
					if (f.onArrive != null) {
						f.onArrive ();
					}
				}
			}
			if (allDone) {
				yield break;
			}
			yield return null;
			t += Time.deltaTime;
		}
	}

	//make the sprites of the piles and the dimming layer once
	void EnsurePiles ()
	{
		if (drawLayers [0] != null || cardPrefab == null) {
			return;
		}
		SpriteRenderer template = cardPrefab.GetComponent<SpriteRenderer> ();
		pileLayerId = template.sortingLayerID;
		pileOrder = template.sortingOrder;
		Vector3 cardScale = cardPrefab.transform.localScale;
		Vector2 size = CardArt.CardSprite.bounds.size;
		for (int i = 0; i < PileLayers; i++) {
			drawLayers [i] = MakePileSprite ("DrawPile" + i, CardArt.BackSprite, pileOrder - 6 + i);
		}
		for (int i = 0; i < PileLayers - 1; i++) {
			discardLayers [i] = MakePileSprite ("DiscardPile" + i, CardArt.BackSprite, pileOrder - 6 + i);
		}
		discardPlace = MakePileSprite ("DiscardPlace", UiStyle.RoundedSprite (size.x * 0.08f), pileOrder - 7);
		discardPlace.drawMode = SpriteDrawMode.Sliced;
		discardPlace.size = size;
		discardPlace.transform.localScale = cardScale;
		foreach (SpriteRenderer layer in drawLayers) {
			layer.transform.localScale = cardScale;
		}
		foreach (SpriteRenderer layer in discardLayers) {
			layer.transform.localScale = cardScale;
		}
		discardPlace.color = new Color (1f, 1f, 1f, 0.1f);

		GameObject top = (GameObject)Instantiate (cardPrefab);
		top.SetActive (true);
		top.name = "DiscardTop";
		discardTop = top.GetComponent<CardModel> ();
		top.GetComponent<BoxCollider2D> ().enabled = false; //a pile is no card to pick

		dim = MakePileSprite ("Dim", Sprite.Create (Texture2D.whiteTexture, new Rect (0, 0, 4, 4), new Vector2 (0.5f, 0.5f), 4f), pileOrder + LiftOrder / 2);
		dim.transform.SetParent (Camera.main.transform, false);
		dim.transform.localPosition = new Vector3 (0f, 0f, 10f);
		dim.enabled = false;
	}

	SpriteRenderer MakePileSprite (string objectName, Sprite sprite, int order)
	{
		GameObject pile = new GameObject (objectName);
		SpriteRenderer renderer = pile.AddComponent<SpriteRenderer> ();
		renderer.sprite = sprite;
		renderer.sortingLayerID = pileLayerId;
		renderer.sortingOrder = order;
		return renderer;
	}

	//put the piles on their places and show as many layers as there are cards on them
	void PlacePiles ()
	{
		if (drawLayers [0] == null) {
			return;
		}
		Vector3 step = new Vector3 (0.07f, -0.07f, 0f);
		int drawCount = drawShown <= 0 ? 0 : Mathf.Clamp (1 + drawShown / 6, 1, PileLayers);
		for (int i = 0; i < PileLayers; i++) {
			drawLayers [i].transform.position = drawPilePos + step * (PileLayers - 1 - i);
			drawLayers [i].enabled = i >= PileLayers - drawCount;
		}
		int below = discardCount == 0 ? 0 : Mathf.Clamp (discardCount / 4, 0, PileLayers - 1);
		for (int i = 0; i < PileLayers - 1; i++) {
			discardLayers [i].transform.position = discardPilePos + step * (PileLayers - 1 - i);
			discardLayers [i].enabled = i >= PileLayers - 1 - below;
		}
		discardPlace.transform.position = discardPilePos;
		discardPlace.enabled = discardCount == 0;
		discardTop.gameObject.SetActive (discardCount > 0);
		discardTop.transform.position = discardPilePos;
		if (discardCount > 0 && shownTopIndex != discardTopIndex) {
			shownTopIndex = discardTopIndex;
			discardTop.cardIndex = discardTopIndex;
			discardTop.ToggleFace (true);
		}
	}

	//dim the table behind a set that is shown (0 = not at all)
	void SetDim (float alpha)
	{
		Camera cam = Camera.main;
		float height = cam.orthographicSize * 2.2f;
		dim.transform.localScale = new Vector3 (height * cam.aspect, height, 1f);
		dim.color = new Color (0.02f, 0.04f, 0.09f, alpha);
		dim.enabled = alpha > 0.001f;
	}

	IEnumerator FadeDim (float from, float to, float duration)
	{
		for (float t = 0f; t < duration; t += Time.deltaTime) {
			SetDim (Mathf.Lerp (from, to, t / duration));
			yield return null;
		}
		SetDim (to);
	}

	const float DimAlpha = 0.7f;

	//the size of the cards of a set that is shown, compared with the cards on the table
	float EnlargeScale (int count)
	{
		Camera cam = Camera.main;
		float visibleWidth = 2f * cam.orthographicSize * cam.aspect;
		return Mathf.Clamp (0.92f * visibleWidth / (count * CardWorldSize ().x * 1.06f), 0.7f, 1.5f);
	}

	//the place of one card of a set that is shown: in a row in the middle of the table
	Vector3 EnlargedPos (int index, int count)
	{
		float pitch = CardWorldSize ().x * EnlargeScale (count) * 1.06f;
		return new Vector3 ((index - (count - 1) / 2f) * pitch, 0f, 0f);
	}

	//deal cards from the draw pile to their slots; the data of the slots is already changed
	IEnumerator DealSlots (List<int> slotList, float stagger, float flight)
	{
		if (slotList.Count == 0) {
			yield break;
		}
		GameSound.Deal (slotList.Count, stagger);
		List<Flight> flights = new List<Flight> ();
		for (int k = 0; k < slotList.Count; k++) {
			int slot = slotList [k];
			int order = k;
			CardModel card = slots [slot];
			card.gameObject.SetActive (false);
			card.inFlight = true;
			Flight f = new Flight ();
			f.card = card;
			f.from = drawPilePos;
			f.to = () => slotPos [slot];
			f.delay = k * stagger;
			f.duration = flight;
			f.onStart = () => {
				card.ClearFound ();
				card.ToggleFace (false);
				card.SetLift (LiftOrder + order);
				card.transform.position = drawPilePos;
				card.gameObject.SetActive (true);
				if (drawShown > 0) {
					drawShown--;
				}
				PlacePiles ();
			};
			f.onArrive = () => {
				card.cardIndex = shownCardIndex [slot];
				card.ToggleFace (true);
				card.SetLift (0);
				card.inFlight = false;
				card.transform.position = slotPos [slot];
			};
			flights.Add (f);
		}
		yield return RunFlights (flights);
	}

	//a new table: all cards are dealt from the draw pile, in about a second
	IEnumerator DealBoard ()
	{
		busy = true;
		List<int> all = new List<int> ();
		for (int slot = 0; slot < boardSize; slot++) {
			if (slotActive [slot]) {
				all.Add (slot);
			}
		}
		drawShown = deckOrder.Count;
		PlacePiles ();
		yield return DealSlots (all, Mathf.Min (0.1f, 0.7f / Mathf.Max (1, all.Count)), 0.3f);
		drawShown = deckOrder.Count - nextCard;
		PlacePiles ();
		busy = false;
	}

	//a set was found: show it big in the middle for two seconds; when it was the last set on the table,
	//all cards of found sets go to the discard pile and new cards are dealt
	IEnumerator FoundSequence (int[] set, bool roundDone)
	{
		busy = true;
		GameSound.Pling ();
		int count = set.Length;
		List<Flight> flights = new List<Flight> ();
		for (int i = 0; i < count; i++) {
			int index = i;
			CardModel card = slots [set [i]];
			Flight f = new Flight ();
			f.card = card;
			f.from = card.transform.position;
			f.to = () => EnlargedPos (index, count);
			f.toScale = () => EnlargeScale (count);
			f.duration = 0.3f;
			f.onStart = () => {
				card.inFlight = true;
				card.SetLift (LiftOrder + index);
			};
			flights.Add (f);
		}
		StartCoroutine (FadeDim (0f, DimAlpha, 0.25f));
		yield return RunFlights (flights);
		yield return new WaitForSeconds (2f);

		StartCoroutine (FadeDim (DimAlpha, 0f, 0.3f));
		if (roundDone) {
			yield return DiscardAndReplace ();
		} else {
			//more sets on the table: the cards go back to their places, with their colored border
			flights = new List<Flight> ();
			for (int i = 0; i < count; i++) {
				int slot = set [i];
				CardModel card = slots [slot];
				Flight f = new Flight ();
				f.card = card;
				f.from = card.transform.position;
				f.fromScale = EnlargeScale (count);
				f.to = () => slotPos [slot];
				f.duration = 0.3f;
				f.onArrive = () => {
					card.SetLift (0);
					card.inFlight = false;
					card.transform.position = slotPos [slot];
				};
				flights.Add (f);
			}
			yield return RunFlights (flights);
		}
		busy = false;
	}

	//the cards of all found sets fly to the discard pile; then their places get new cards from the deck
	IEnumerator DiscardAndReplace ()
	{
		List<int> used = new List<int> ();
		foreach (int[] set in foundSets) {
			foreach (int slot in set) {
				if (!used.Contains (slot)) {
					used.Add (slot);
				}
			}
		}
		used.Sort ();

		GameSound.Deal (used.Count, 0.05f);
		List<Flight> flights = new List<Flight> ();
		for (int k = 0; k < used.Count; k++) {
			int order = k;
			CardModel card = slots [used [k]];
			Flight f = new Flight ();
			f.card = card;
			f.from = card.transform.position;
			f.fromScale = card.transform.localScale.x / card.BaseScale.x;
			f.to = () => discardPilePos;
			f.delay = k * 0.05f;
			f.duration = 0.3f;
			f.onStart = () => {
				card.inFlight = true;
				card.SetLift (LiftOrder + order);
			};
			f.onArrive = () => {
				discardCount++;
				discardTopIndex = card.cardIndex;
				PlacePiles ();
				card.SetLift (0);
				card.gameObject.SetActive (false);
				card.inFlight = false;
			};
			flights.Add (f);
		}
		yield return RunFlights (flights);

		//the data: new cards for the used places, and a set on the table again
		drawShown = deckOrder.Count - nextCard;
		int[] before = shownCardIndex.ToArray ();
		foreach (int slot in used) {
			ChangeCard (slot);
		}
		EnsureSetOnBoard ();
		List<int> dealt = new List<int> ();
		for (int slot = 0; slot < boardSize; slot++) {
			if (!slotActive [slot]) {
				slots [slot].gameObject.SetActive (false);
			} else if (used.Contains (slot) || before [slot] != shownCardIndex [slot]) {
				dealt.Add (slot);
			}
		}
		yield return DealSlots (dealt, 0.1f, 0.3f);
		drawShown = deckOrder.Count - nextCard;
		PlacePiles ();
	}

	//a wrong set: a low tone, the cards get a red frame and shake for a moment
	IEnumerator WrongSequence (List<CardModel> cards)
	{
		busy = true;
		GameSound.Wrong ();
		int[] places = new int[cards.Count];
		for (int i = 0; i < cards.Count; i++) {
			places [i] = System.Array.IndexOf (slots, cards [i]);
			cards [i].inFlight = true;
			cards [i].ShowWrong (true);
		}
		const float duration = 0.4f;
		for (float t = 0f; t < duration; t += Time.deltaTime) {
			float shake = Mathf.Sin (t * 70f) * 0.12f * (1f - t / duration);
			for (int i = 0; i < cards.Count; i++) {
				cards [i].transform.position = slotPos [places [i]] + new Vector3 (shake, 0f, 0f);
			}
			yield return null;
		}
		for (int i = 0; i < cards.Count; i++) {
			cards [i].ShowWrong (false);
			cards [i].inFlight = false;
			cards [i].transform.position = slotPos [places [i]];
		}
		busy = false;
	}

}
