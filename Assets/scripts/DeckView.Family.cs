using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the family board online: connecting, the name of the player, writing our sets to the database
//and showing the sets of the others (FamilyBackend does the talking with Firestore)
public partial class DeckView
{

	const string NameKey = "FamilyName";
	const float PollSeconds = 30f; //how often the database is asked for new sets of the others (every ask costs one read)
	const float PresenceSeconds = 60f; //how often this player says they are here and the list of online players is loaded

	FamilyBackend backend = new FamilyBackend ();
	FamilyState pendingFamily; //the board from the database, waiting for LoadBoard
	Queue<FamilyClaim> remoteClaims = new Queue<FamilyClaim> (); //sets of the others that are not shown yet
	bool askingName;
	bool renaming; //the name box is for changing the name of the player who is already playing
	string nameInput = "";
	bool loopRunning;
	bool pollNow; //ask the database at once

	bool FamilyOnline {
		get { return familyMode && family != null && family.online; }
	}

	//the family board is chosen and its table exists (while connecting, family mode is on but there is no table yet)
	bool FamilyShown {
		get { return familyMode && family != null; }
	}

	//the family board needs a name for the player and a connection; without a Firebase key it is the offline board
	void StartFamilyConnect ()
	{
		string saved = PlayerPrefs.GetString (NameKey, "");
		if (saved == "") {
			askingName = true; //OnGUI asks for a name
			busy = true;
			return;
		}
		StartCoroutine (ConnectFamily (saved));
	}

	void ConfirmFamilyName ()
	{
		string playerName = FamilyState.CleanName (nameInput);
		if (playerName == "") {
			return;
		}
		PlayerPrefs.SetString (NameKey, playerName);
		PlayerPrefs.Save ();
		askingName = false;
		if (renaming) {
			renaming = false;
			family.Rename (playerName); //the new name goes with the next set; the old sets show it too
			return;
		}
		StartCoroutine (ConnectFamily (playerName));
	}

	//the player wants another name
	void StartRename ()
	{
		nameInput = family.localPlayer;
		renaming = true;
		askingName = true;
	}

	void CancelFamilyName ()
	{
		askingName = false;
		if (renaming) {
			renaming = false;
			return;
		}
		busy = false;
		familyMode = false; //the classic table was stored when the family board was chosen
		UseClassicDifficulty ();
		LoadBoard ();
	}

	//get the seed and the sets so far from the database and show the table
	IEnumerator ConnectFamily (string playerName)
	{
		busy = true;
		message = "Connecting...";
		messageUntil = Time.time + 60f;
		int seed = -1;
		yield return backend.LoadBoard (FirebaseConfig.BoardId, s => seed = s);
		List<FamilyClaim> claims = null;
		if (seed >= 0) {
			yield return backend.FetchClaims (FirebaseConfig.BoardId, 0, c => claims = c);
		}
		busy = false;
		if (seed < 0 || claims == null) {
			LoadBoard (); //no connection: the board on this device
			message = "No connection: this board is only on this device";
			messageUntil = Time.time + 4f;
			yield break;
		}
		pendingFamily = FamilyState.CreateOnline (playerName, backend.uid, seed, claims);
		message = "";
		messageUntil = 0f;
		LoadBoard ();
		StartFamilyLoop ();
	}

	void StartFamilyLoop ()
	{
		if (!loopRunning) {
			StartCoroutine (FamilyLoop ());
		}
	}

	//while the family board is on the screen: ask the database for new sets now and then, and show them one by one
	IEnumerator FamilyLoop ()
	{
		loopRunning = true;
		float next = 0f;
		float nextPresence = 0f;
		while (FamilyOnline) {
			if (Time.time >= nextPresence) {
				nextPresence = Time.time + PresenceSeconds;
				yield return RefreshPresence ();
				if (!FamilyOnline) {
					break;
				}
			}
			if (Time.time >= next || pollNow) {
				pollNow = false;
				next = Time.time + PollSeconds;
				FamilyState state = family;
				List<FamilyClaim> claims = null;
				yield return backend.FetchClaims (FirebaseConfig.BoardId, state.nextN, c => claims = c);
				if (claims != null) {
					foreach (FamilyClaim claim in claims) {
						if (claim.n >= state.nextN) { //a set we wrote ourselves is known already
							state.nextN = claim.n + 1;
							remoteClaims.Enqueue (claim);
						}
					}
				}
			}
			if (!busy && remoteClaims.Count > 0 && FamilyOnline) {
				ShowRemoteClaim (remoteClaims.Dequeue ());
			}
			yield return null;
		}
		loopRunning = false;
	}

	//say that this player is here, then load who else is here; nothing is written while the game is in the background
	IEnumerator RefreshPresence ()
	{
		if (!Application.isFocused && !Application.isBatchMode) {
			yield break;
		}
		FamilyState state = family;
		System.DateTime? serverTime = null;
		yield return backend.Heartbeat (FirebaseConfig.BoardId, state.localPlayer, t => serverTime = t);
		if (serverTime == null) {
			yield break;
		}
		Dictionary<string, string> online = null;
		yield return backend.FetchOnline (FirebaseConfig.BoardId, serverTime.Value, o => online = o);
		if (online != null && state == family) {
			state.onlineNames = online;
		}
	}

	//a set of another player: it gets their color and is shown big for a moment, like a set of our own
	void ShowRemoteClaim (FamilyClaim claim)
	{
		int[] set = (int[])claim.slots.Clone ();
		System.Array.Sort (set);
		family.log.Add (claim);
		if (!ClaimIsValid (set)) {
			return; //the same set again, or cards that were replaced already
		}
		ClearSelection ();
		RegisterFound (set, claim.uid, claim.player);
		foreach (int slot in set) {
			slots [slot].AddFound (FoundColorOf (foundSets.Count - 1));
		}
		ShowMessage (claim.player + " found a set!");
		StartCoroutine (FoundSequence (set, numberofSets <= 0));
	}

	//our own set: write it to the database first; it only counts when nobody else was faster
	IEnumerator PostFamilyClaim (int[] found)
	{
		busy = true;
		FamilyState state = family;
		int number = state.nextN;
		FamilyBackend.ClaimResult result = FamilyBackend.ClaimResult.Failed;
		yield return backend.PostClaim (FirebaseConfig.BoardId, number, state.localPlayer, found, r => result = r);
		busy = false;
		if (result == FamilyBackend.ClaimResult.Accepted) {
			state.nextN = Mathf.Max (state.nextN, number + 1); //a poll may have seen our own set already
			AcceptFound (found);
		} else if (result == FamilyBackend.ClaimResult.Taken) {
			ShowMessage ("Somebody was faster, updating...");
			pollNow = true;
		} else {
			ShowMessage ("No connection, try again");
		}
	}

	//the box that asks for the name of the player, the first time the family board is opened online
	void DrawNameBox (float width, float height)
	{
		if (!askingName) {
			return;
		}
		float bw = Mathf.Min (320f, width - 16f);
		float bh = 180;
		Rect box = new Rect ((width - bw) / 2, (height - bh) / 2, bw, bh);
		GUI.Box (box, "", UiStyle.PanelStyle);
		GUI.Label (new Rect (box.x + 24, box.y + 14, bw - 48, 32), renaming ? "New name" : "Your name", UiStyle.TitleStyle);
		nameInput = GUI.TextField (new Rect (box.x + 24, box.y + 58, bw - 48, 36), nameInput, 14);
		GUI.enabled = FamilyState.CleanName (nameInput) != "";
		if (GUI.Button (new Rect (box.x + 24, box.y + 108, (bw - 56) / 2, 40), renaming ? "OK" : "Play", UiStyle.ActionButtonStyle)) {
			ConfirmFamilyName ();
		}
		GUI.enabled = true;
		if (GUI.Button (new Rect (box.x + 32 + (bw - 56) / 2, box.y + 108, (bw - 56) / 2, 40), "Cancel", UiStyle.ButtonStyle)) {
			CancelFamilyName ();
		}
	}

}
