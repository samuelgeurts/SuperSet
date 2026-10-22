using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//the shared family board in Firestore, through the REST API (anonymous sign-in, then plain HTTPS requests).
//  boards/{boardId}                  the board: seed
//  boards/{boardId}/claims/{000123}  the log: one document per found set, numbered 0, 1, 2, ...
//The number in the name of a claim is a lock: a player can only write the next number, so a player who has not seen
//the latest claim gets "already exists" and has to update first. The first claim wins.
//All methods are coroutines for DeckView.StartCoroutine; the result goes to the callback.
public class FamilyBackend
{
	public enum ClaimResult { Accepted, Taken, Failed }

	string idToken, refreshToken;
	float tokenValidUntil;
	public string uid;

	const string TokenKey = "FirebaseRefreshToken";

	//---- the shapes of the JSON that JsonUtility reads
	[Serializable] class SignUpReply { public string idToken, refreshToken, localId, expiresIn; }
	[Serializable] class RefreshReply { public string id_token, refresh_token, user_id, expires_in; }
	[Serializable] class IntValue { public string integerValue; }
	[Serializable] class StringValue { public string stringValue; }
	[Serializable] class ClaimFields { public IntValue n; public StringValue player; public StringValue slots; public StringValue uid; }
	[Serializable] class ClaimDocument { public string name; public ClaimFields fields; }
	[Serializable] class QueryItem { public ClaimDocument document; }
	[Serializable] class QueryReply { public QueryItem[] items; }
	[Serializable] class BoardFields { public IntValue seed; }
	[Serializable] class BoardDocument { public BoardFields fields; }

	//one HTTP request; code 0 means no connection
	IEnumerator Send (string method, string url, string json, bool authorized, Action<long, string> done)
	{
		using (UnityWebRequest request = new UnityWebRequest (url, method)) {
			if (json != null) {
				request.uploadHandler = new UploadHandlerRaw (System.Text.Encoding.UTF8.GetBytes (json));
				request.SetRequestHeader ("Content-Type", "application/json");
			}
			request.downloadHandler = new DownloadHandlerBuffer ();
			request.timeout = 15;
			if (authorized) {
				request.SetRequestHeader ("Authorization", "Bearer " + idToken);
			}
			yield return request.SendWebRequest ();
			done (request.responseCode, request.downloadHandler.text);
		}
	}

	//sign in without an account: a new anonymous user, or the same one again through the refresh token of last time
	public IEnumerator SignIn (Action<bool> done)
	{
		string saved = PlayerPrefs.GetString (TokenKey, "");
		if (saved != "") {
			long code = 0;
			string text = "";
			yield return Send ("POST", FirebaseConfig.TokenUrl + "/token?key=" + FirebaseConfig.Key,
				"{\"grant_type\":\"refresh_token\",\"refresh_token\":\"" + saved + "\"}", false, (c, t) => { code = c; text = t; });
			if (code == 200) {
				RefreshReply reply = JsonUtility.FromJson<RefreshReply> (text);
				idToken = reply.id_token;
				refreshToken = reply.refresh_token;
				uid = reply.user_id;
				tokenValidUntil = Time.realtimeSinceStartup + float.Parse (reply.expires_in) - 120f;
				PlayerPrefs.SetString (TokenKey, refreshToken);
				done (true);
				yield break;
			}
			if (code == 0) {
				done (false); //no connection: keep the token for later
				yield break;
			}
		}
		long signUpCode = 0;
		string signUpText = "";
		yield return Send ("POST", FirebaseConfig.AuthUrl + "/accounts:signUp?key=" + FirebaseConfig.Key,
			"{\"returnSecureToken\":true}", false, (c, t) => { signUpCode = c; signUpText = t; });
		if (signUpCode != 200) {
			Debug.LogWarning ("Family board: sign-in failed (" + signUpCode + ") " + signUpText);
			done (false);
			yield break;
		}
		SignUpReply signUp = JsonUtility.FromJson<SignUpReply> (signUpText);
		idToken = signUp.idToken;
		refreshToken = signUp.refreshToken;
		uid = signUp.localId;
		tokenValidUntil = Time.realtimeSinceStartup + float.Parse (signUp.expiresIn) - 120f;
		PlayerPrefs.SetString (TokenKey, refreshToken);
		PlayerPrefs.Save ();
		done (true);
	}

	//the sign-in lasts an hour: sign in again (with the refresh token) a little before it ends
	IEnumerator EnsureSignedIn (Action<bool> done)
	{
		if (idToken != null && Time.realtimeSinceStartup < tokenValidUntil) {
			done (true);
			yield break;
		}
		yield return SignIn (done);
	}

	//the seed of the board; the first player to open the board makes it
	public IEnumerator LoadBoard (string boardId, Action<int> done)
	{
		bool signedIn = false;
		yield return EnsureSignedIn (ok => signedIn = ok);
		if (!signedIn) {
			done (-1);
			yield break;
		}
		string url = FirebaseConfig.FirestoreUrl + "/boards/" + boardId;
		for (int attempt = 0; attempt < 2; attempt++) {
			long code = 0;
			string text = "";
			yield return Send ("GET", url, null, true, (c, t) => { code = c; text = t; });
			if (code == 200) {
				BoardDocument board = JsonUtility.FromJson<BoardDocument> (text);
				done (int.Parse (board.fields.seed.integerValue));
				yield break;
			}
			if (code != 404) {
				Debug.LogWarning ("Family board: loading the board failed (" + code + ") " + text);
				done (-1);
				yield break;
			}
			//the board does not exist yet: make it (a 409 means somebody else was first, which is fine)
			yield return Send ("POST", FirebaseConfig.FirestoreUrl + "/boards?documentId=" + boardId,
				"{\"fields\":{\"seed\":{\"integerValue\":\"" + FamilyState.BaseSeed + "\"}}}", true, (c, t) => { code = c; text = t; });
			if (code != 200 && code != 409) {
				Debug.LogWarning ("Family board: making the board failed (" + code + ") " + text);
				done (-1);
				yield break;
			}
		}
		done (-1);
	}

	//all claims from number fromN on, in order; null when there is no connection
	public IEnumerator FetchClaims (string boardId, int fromN, Action<List<FamilyClaim>> done)
	{
		bool signedIn = false;
		yield return EnsureSignedIn (ok => signedIn = ok);
		if (!signedIn) {
			done (null);
			yield break;
		}
		List<FamilyClaim> claims = new List<FamilyClaim> ();
		const int pageSize = 300;
		int from = fromN;
		while (true) {
			string query = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"claims\"}]," +
			"\"where\":{\"fieldFilter\":{\"field\":{\"fieldPath\":\"n\"},\"op\":\"GREATER_THAN_OR_EQUAL\",\"value\":{\"integerValue\":\"" + from + "\"}}}," +
			"\"orderBy\":[{\"field\":{\"fieldPath\":\"n\"},\"direction\":\"ASCENDING\"}],\"limit\":" + pageSize + "}}";
			long code = 0;
			string text = "";
			yield return Send ("POST", FirebaseConfig.FirestoreUrl + "/boards/" + boardId + ":runQuery", query, true, (c, t) => { code = c; text = t; });
			if (code != 200) {
				if (code != 0) {
					Debug.LogWarning ("Family board: loading the claims failed (" + code + ") " + text);
				}
				done (null);
				yield break;
			}
			QueryReply reply = JsonUtility.FromJson<QueryReply> ("{\"items\":" + text + "}");
			int count = 0;
			foreach (QueryItem item in reply.items) {
				if (item.document == null || item.document.fields == null || item.document.fields.n == null ||
				    string.IsNullOrEmpty (item.document.fields.n.integerValue)) {
					continue; //an empty answer holds only the time of the query
				}
				ClaimFields f = item.document.fields;
				FamilyClaim claim = ParseClaim (f);
				if (claim != null) {
					claims.Add (claim);
					from = claim.n + 1;
				}
				count++;
			}
			if (count < pageSize) {
				break;
			}
		}
		done (claims);
	}

	static FamilyClaim ParseClaim (ClaimFields f)
	{
		if (f.player == null || f.slots == null) {
			return null;
		}
		string[] numbers = f.slots.stringValue.Split (',');
		int[] slots = new int[numbers.Length];
		for (int i = 0; i < numbers.Length; i++) {
			if (!int.TryParse (numbers [i], out slots [i])) {
				return null;
			}
		}
		FamilyClaim claim = new FamilyClaim (f.player.stringValue, slots);
		claim.n = int.Parse (f.n.integerValue);
		return claim;
	}

	//write claim number n; Taken means somebody else wrote number n first
	public IEnumerator PostClaim (string boardId, int n, string player, int[] slots, Action<ClaimResult> done)
	{
		bool signedIn = false;
		yield return EnsureSignedIn (ok => signedIn = ok);
		if (!signedIn) {
			done (ClaimResult.Failed);
			yield break;
		}
		string json = "{\"fields\":{\"n\":{\"integerValue\":\"" + n + "\"}," +
		"\"player\":{\"stringValue\":\"" + player + "\"}," +
		"\"slots\":{\"stringValue\":\"" + string.Join (",", Array.ConvertAll (slots, s => s.ToString ())) + "\"}," +
		"\"uid\":{\"stringValue\":\"" + uid + "\"}}}";
		long code = 0;
		string text = "";
		yield return Send ("POST", FirebaseConfig.FirestoreUrl + "/boards/" + boardId + "/claims?documentId=" + n.ToString ("D6"), json, true,
			(c, t) => { code = c; text = t; });
		if (code == 200) {
			done (ClaimResult.Accepted);
		} else if (code == 409) {
			done (ClaimResult.Taken);
		} else {
			if (code != 0) {
				Debug.LogWarning ("Family board: writing the claim failed (" + code + ") " + text);
			}
			done (ClaimResult.Failed);
		}
	}
}
