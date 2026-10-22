//the Firebase project of the family board. The Firebase Unity SDK does not work in WebGL, so the game talks to the
//REST API of Firebase Auth and Firestore with UnityWebRequest (see FamilyBackend).
public static class FirebaseConfig
{
	public const string ProjectId = "superset-75145";

	//The web API key of the Firebase web app (Project settings > Your apps). It is no secret: it only says which project
	//the requests are for; the Firestore rules protect the data. While it is empty the family board works offline only.
	public const string ApiKey = "AIzaSyAcG__UHESXVRpRJti3d4KljoboI6THLwA";

	public const string BoardId = "main"; //one public board for now; other boards (per family) come with phase 4

	//tests run against the local Firebase emulator (host name in the environment variable SUPERSET_FIREBASE_EMULATOR)
	static string EmulatorHost {
		get {
#if UNITY_EDITOR
			string host = System.Environment.GetEnvironmentVariable ("SUPERSET_FIREBASE_EMULATOR");
			return string.IsNullOrEmpty (host) ? null : host;
#else
			return null;
#endif
		}
	}

	//the key that goes in the requests (the emulator accepts any key, but wants one)
	public static string Key {
		get { return EmulatorHost != null ? "emulator" : ApiKey; }
	}

	public static bool Configured {
		get { return ApiKey != "" || EmulatorHost != null; }
	}

	public static string AuthUrl {
		get {
			return EmulatorHost != null ? "http://" + EmulatorHost + ":9099/identitytoolkit.googleapis.com/v1" : "https://identitytoolkit.googleapis.com/v1";
		}
	}

	public static string TokenUrl {
		get {
			return EmulatorHost != null ? "http://" + EmulatorHost + ":9099/securetoken.googleapis.com/v1" : "https://securetoken.googleapis.com/v1";
		}
	}

	//the documents of the database: .../documents/boards/main
	public static string FirestoreUrl {
		get {
			string root = EmulatorHost != null ? "http://" + EmulatorHost + ":8080" : "https://firestore.googleapis.com";
			return root + "/v1/projects/" + ProjectId + "/databases/(default)/documents";
		}
	}
}
