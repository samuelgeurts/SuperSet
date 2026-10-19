using System.Collections.Generic;
using UnityEngine;

//the sounds of the game
public enum Sfx
{
	CardDeal, //one card is dealt from the draw pile to the table
	CardDiscard, //one card goes to the discard pile
	SetFound, //a set is found
	SetWrong //the cards are not a set
}

//plays the sounds of the game by name.
//The sounds are WAV files in Assets/Resources/Audio/Sfx. To use another sound, put a file with the same name there
//(for example set-found.wav). If a file is missing that sound is silent, which is no problem.
//tools/make-sounds.py makes the original files again.
public static class AudioManager
{
	const string Folder = "Audio/Sfx/";
	const string EnabledKey = "Sound";
	const int Voices = 8; //how many sounds can play at the same moment

	static AudioSource[] voices;
	static int nextVoice;
	static Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip> ();

	//false when the player muted the game; kept between sessions
	public static bool Enabled {
		get { return PlayerPrefs.GetInt (EnabledKey, 1) == 1; }
		set {
			PlayerPrefs.SetInt (EnabledKey, value ? 1 : 0);
			PlayerPrefs.Save ();
		}
	}

	//the name of the WAV file of a sound, without the extension
	public static string FileName (Sfx sound)
	{
		switch (sound) {
		case Sfx.CardDeal:
			return "card-deal";
		case Sfx.CardDiscard:
			return "card-discard";
		case Sfx.SetFound:
			return "set-found";
		default:
			return "set-wrong";
		}
	}

	static AudioClip Load (Sfx sound)
	{
		AudioClip clip;
		if (!clips.TryGetValue (sound, out clip)) {
			clip = Resources.Load<AudioClip> (Folder + FileName (sound));
			if (clip == null) {
				Debug.LogWarning ("Sound file not found: Assets/Resources/" + Folder + FileName (sound) + ".wav");
			}
			clips [sound] = clip;
		}
		return clip;
	}

	static void EnsureVoices ()
	{
		if (voices != null && voices [0] != null) {
			return;
		}
		GameObject holder = new GameObject ("AudioManager");
		Object.DontDestroyOnLoad (holder);
		voices = new AudioSource[Voices];
		for (int i = 0; i < Voices; i++) {
			voices [i] = holder.AddComponent<AudioSource> ();
			voices [i].spatialBlend = 0f;
			voices [i].playOnAwake = false;
		}
	}

	//play a sound; pitch 1 is the normal pitch (a little change per card makes a row of cards sound less machine-like)
	public static void Play (Sfx sound, float volume = 1f, float pitch = 1f)
	{
		if (!Enabled) {
			return;
		}
		AudioClip clip = Load (sound);
		if (clip == null) {
			return;
		}
		EnsureVoices ();
		AudioSource voice = voices [nextVoice];
		nextVoice = (nextVoice + 1) % Voices;
		voice.clip = clip;
		voice.volume = volume;
		voice.pitch = pitch;
		voice.Play ();
	}
}
