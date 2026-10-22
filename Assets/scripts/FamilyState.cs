using System.Collections.Generic;
using UnityEngine;

//small deterministic random generator (xorshift32): the same seed gives the same numbers on every device,
//so everybody who replays the same log gets the same table. The family board must never use UnityEngine.Random.
public class SeededRandom
{
	uint state;

	public SeededRandom (int seed)
	{
		state = (uint)seed * 2654435761u + 0x9E3779B9u;
		if (state == 0) {
			state = 1;
		}
	}

	//a number from 0 up to (not including) max
	public int Next (int max)
	{
		state ^= state << 13;
		state ^= state >> 17;
		state ^= state << 5;
		return (int)(state % (uint)max);
	}
}

//one set that a player found on the shared board: who, and the slots of the cards.
//A player is known by the uid (the anonymous sign-in); the name is a label that the player can change.
public class FamilyClaim
{
	public string uid;
	public string player; //the name of the player at the time
	public int[] slots;
	public int n = -1; //number in the log of the database; -1 for a claim that is not in the database

	public FamilyClaim (string uid, string player, int[] slots)
	{
		this.uid = uid;
		this.player = player;
		this.slots = slots;
	}
}

//the shared board of the family mode is a seed plus a log of found sets; the table follows from replaying the log.
//This class holds that seed and log, the players (in the order they first appear, which decides their color)
//and how many sets each of them found.
public class FamilyState
{
	public const int BaseSeed = 1022; //the seed of the first board
	const string SeasonKey = "FamilySeason";
	const string LogKey = "FamilyLog";

	public int season; //offline only; the online board is one board for now
	public int seed;
	public bool online; //the log is in the database (FamilyBackend) and not in PlayerPrefs
	public int nextN; //online: the number the next claim gets in the database
	public SeededRandom random;
	public string localPlayer = "You"; //the name of the player on this device
	public string localId = "You"; //the uid of the player on this device (the offline board uses the name)

	public List<string> players = new List<string> (); //the ids of the players, in the order they found their first set
	public Dictionary<string, int> counts = new Dictionary<string, int> ();
	public Dictionary<string, string> names = new Dictionary<string, string> (); //the latest name of every player
	public List<FamilyClaim> log = new List<FamilyClaim> ();
	public List<string> foundBy = new List<string> (); //finder of every set on the table now, parallel to the found sets of DeckView

	//the offline board of the current season (no database), with the random generator at its start; the log is replayed by DeckView
	public static FamilyState Create ()
	{
		FamilyState state = new FamilyState ();
		state.season = PlayerPrefs.GetInt (SeasonKey, 0);
		state.seed = BaseSeed + state.season;
		state.random = new SeededRandom (state.seed);
		state.log = Parse (PlayerPrefs.GetString (LogKey + state.season, ""));
		return state;
	}

	//the board from the database: its seed and the claims so far
	public static FamilyState CreateOnline (string localPlayer, string localId, int seed, List<FamilyClaim> claims)
	{
		FamilyState state = new FamilyState ();
		state.online = true;
		state.localPlayer = localPlayer;
		state.localId = localId;
		state.names [localId] = localPlayer;
		state.seed = seed;
		state.random = new SeededRandom (seed);
		state.log = claims;
		foreach (FamilyClaim claim in claims) {
			state.nextN = Mathf.Max (state.nextN, claim.n + 1);
		}
		return state;
	}

	//the next season: a new seed and an empty log
	public static void NextSeason ()
	{
		PlayerPrefs.SetInt (SeasonKey, PlayerPrefs.GetInt (SeasonKey, 0) + 1);
		PlayerPrefs.Save ();
	}

	//start the table again from its seed, before the log is replayed (the log itself stays)
	public void ResetPlay ()
	{
		random = new SeededRandom (seed);
		players.Clear ();
		counts.Clear ();
		names.Clear ();
		foundBy.Clear ();
	}

	//a player of a set; the name is the latest one that came with a claim
	public void AddPlayer (string id, string name)
	{
		if (!counts.ContainsKey (id)) {
			players.Add (id);
			counts [id] = 0;
		}
		names [id] = name;
	}

	public string Name (string id)
	{
		string name;
		return names.TryGetValue (id, out name) ? name : id;
	}

	//the player on this device changes their name; sets found before show the new name too
	public void Rename (string name)
	{
		localPlayer = name;
		names [localId] = name;
	}

	//the color number of a player: the order in which the players found their first set; a player without a set yet gets the next one
	public int PlayerIndex (string id)
	{
		int index = players.IndexOf (id);
		return index >= 0 ? index : players.Count;
	}

	public int Count (string id)
	{
		int count;
		return counts.TryGetValue (id, out count) ? count : 0;
	}

	//a claim of the offline board is kept on this device
	public void AddClaim (string id, string name, int[] slots)
	{
		log.Add (new FamilyClaim (id, name, slots));
		if (!online) {
			PlayerPrefs.SetString (LogKey + season, Format (log));
			PlayerPrefs.Save ();
		}
	}

	//"Anna|3,5,9,12;Ben|0,4,8,13"
	static string Format (List<FamilyClaim> claims)
	{
		List<string> parts = new List<string> ();
		foreach (FamilyClaim claim in claims) {
			parts.Add (claim.uid + "|" + string.Join (",", System.Array.ConvertAll (claim.slots, s => s.ToString ())));
		}
		return string.Join (";", parts);
	}

	static List<FamilyClaim> Parse (string text)
	{
		List<FamilyClaim> claims = new List<FamilyClaim> ();
		foreach (string part in text.Split (new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries)) {
			string[] halves = part.Split ('|');
			if (halves.Length != 2) {
				continue;
			}
			string[] numbers = halves [1].Split (',');
			int[] slots = new int[numbers.Length];
			bool valid = true;
			for (int i = 0; i < numbers.Length; i++) {
				valid &= int.TryParse (numbers [i], out slots [i]);
			}
			if (valid) {
				claims.Add (new FamilyClaim (halves [0], halves [0], slots)); //the offline board has no uid: the name is the id
			}
		}
		return claims;
	}

	//a player name can not hold the separators of the log
	public static string CleanName (string name)
	{
		return name.Replace ("|", "").Replace (";", "").Replace (",", "").Replace ("\"", "").Replace ("\\", "").Trim ();
	}
}
