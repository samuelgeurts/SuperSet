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

//one set that a player found on the shared board: who, and the slots of the cards
public class FamilyClaim
{
	public string player;
	public int[] slots;

	public FamilyClaim (string player, int[] slots)
	{
		this.player = player;
		this.slots = slots;
	}
}

//the shared board of the family mode is a seed plus a log of found sets; the table follows from replaying the log.
//This class holds that seed and log, the players (in the order they first appear, which decides their color)
//and how many sets each of them found.
public class FamilyState
{
	public const string BoardId = "main"; //phase 2: one board per id in the database; for now there is one
	public const int BaseSeed = 1022; //phase 2: the seed comes from the board document
	const string SeasonKey = "FamilySeason";
	const string LogKey = "FamilyLog";

	public int season;
	public SeededRandom random;
	public string localPlayer = "You";

	public List<string> players = new List<string> ();
	public Dictionary<string, int> counts = new Dictionary<string, int> ();
	public List<FamilyClaim> log = new List<FamilyClaim> ();
	public List<string> foundBy = new List<string> (); //finder of every set on the table now, parallel to the found sets of DeckView

	public int Seed {
		get { return BaseSeed + season; }
	}

	//the board of the current season, with the random generator at its start; the log is replayed by DeckView
	public static FamilyState Create ()
	{
		FamilyState state = new FamilyState ();
		state.season = PlayerPrefs.GetInt (SeasonKey, 0);
		state.random = new SeededRandom (state.Seed);
		state.AddPlayer (state.localPlayer);
		state.log = Parse (PlayerPrefs.GetString (LogKey + state.season, ""));
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
		random = new SeededRandom (Seed);
		players.Clear ();
		counts.Clear ();
		foundBy.Clear ();
		AddPlayer (localPlayer);
	}

	public void AddPlayer (string name)
	{
		if (!counts.ContainsKey (name)) {
			players.Add (name);
			counts [name] = 0;
		}
	}

	public int PlayerIndex (string name)
	{
		return Mathf.Max (0, players.IndexOf (name));
	}

	public int Count (string name)
	{
		int count;
		return counts.TryGetValue (name, out count) ? count : 0;
	}

	public void AddClaim (string player, int[] slots)
	{
		log.Add (new FamilyClaim (player, slots));
		PlayerPrefs.SetString (LogKey + season, Format (log));
		PlayerPrefs.Save ();
	}

	//"Anna|3,5,9,12;Ben|0,4,8,13"
	static string Format (List<FamilyClaim> claims)
	{
		List<string> parts = new List<string> ();
		foreach (FamilyClaim claim in claims) {
			parts.Add (claim.player + "|" + string.Join (",", System.Array.ConvertAll (claim.slots, s => s.ToString ())));
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
				claims.Add (new FamilyClaim (halves [0], slots));
			}
		}
		return claims;
	}

	//a player name can not hold the separators of the log
	public static string CleanName (string name)
	{
		return name.Replace ("|", "").Replace (";", "").Replace (",", "").Trim ();
	}
}
