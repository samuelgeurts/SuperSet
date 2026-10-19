using System.Collections.Generic;
using UnityEngine;

//the sounds of the game, made in code so no audio files are needed: a deal rattle, a chime for a found set and a low tone for a wrong one
public static class GameSound
{
	const int SampleRate = 44100;
	const string EnabledKey = "Sound";

	static AudioSource source;
	static AudioClip pling, wrong;
	static Dictionary<int, AudioClip> deals = new Dictionary<int, AudioClip> ();

	//false when the player muted the game; kept between sessions
	public static bool Enabled {
		get { return PlayerPrefs.GetInt (EnabledKey, 1) == 1; }
		set {
			PlayerPrefs.SetInt (EnabledKey, value ? 1 : 0);
			PlayerPrefs.Save ();
		}
	}

	static void Play (AudioClip clip, float volume)
	{
		if (!Enabled || clip == null) {
			return;
		}
		if (source == null) {
			GameObject holder = new GameObject ("GameSound");
			Object.DontDestroyOnLoad (holder);
			source = holder.AddComponent<AudioSource> ();
			source.spatialBlend = 0f;
			source.playOnAwake = false;
		}
		source.PlayOneShot (clip, volume);
	}

	static AudioClip MakeClip (string name, float[] samples)
	{
		AudioClip clip = AudioClip.Create (name, samples.Length, 1, SampleRate, false);
		clip.SetData (samples, 0);
		return clip;
	}

	//a bright two-note chime for a found set
	public static void Pling ()
	{
		if (pling == null) {
			float[] samples = new float[Mathf.CeilToInt (0.7f * SampleRate)];
			AddNote (samples, 0f, 880f, 0.5f);
			AddNote (samples, 0.09f, 1318.5f, 0.5f);
			pling = MakeClip ("Pling", samples);
		}
		Play (pling, 0.8f);
	}

	//a soft bell: a tone with two overtones that dies away
	static void AddNote (float[] samples, float startTime, float frequency, float amplitude)
	{
		int start = Mathf.RoundToInt (startTime * SampleRate);
		for (int s = 0; start + s < samples.Length; s++) {
			float t = s / (float)SampleRate;
			float attack = Mathf.Clamp01 (t / 0.004f);
			float tone = Mathf.Sin (2f * Mathf.PI * frequency * t)
			             + 0.45f * Mathf.Sin (2f * Mathf.PI * frequency * 2f * t) * Mathf.Exp (-4f * t)
			             + 0.2f * Mathf.Sin (2f * Mathf.PI * frequency * 3f * t) * Mathf.Exp (-8f * t);
			samples [start + s] += tone * attack * Mathf.Exp (-6.5f * t) * amplitude * 0.6f;
		}
	}

	//a low, dull tone that falls a little, for a wrong set
	public static void Wrong ()
	{
		if (wrong == null) {
			float[] samples = new float[Mathf.CeilToInt (0.4f * SampleRate)];
			float phase = 0f;
			for (int s = 0; s < samples.Length; s++) {
				float t = s / (float)SampleRate;
				float frequency = Mathf.Lerp (190f, 140f, Mathf.Clamp01 (t / 0.3f));
				phase += 2f * Mathf.PI * frequency / SampleRate;
				float tone = Mathf.Sin (phase) + 0.35f * Mathf.Sin (2f * phase) + 0.2f * Mathf.Sign (Mathf.Sin (phase)); //a bit buzzy
				samples [s] = tone * Mathf.Clamp01 (t / 0.005f) * Mathf.Exp (-6f * t) * 0.4f;
			}
			wrong = MakeClip ("Wrong", samples);
		}
		Play (wrong, 0.8f);
	}

	//a short rattle of cards: one flick per card, a flick is a burst of noise that dies away fast
	public static void Deal (int count, float stagger)
	{
		int key = count * 1000 + Mathf.RoundToInt (stagger * 1000f);
		AudioClip clip;
		if (!deals.TryGetValue (key, out clip)) {
			float length = (count - 1) * stagger + 0.12f;
			float[] samples = new float[Mathf.CeilToInt (length * SampleRate)];
			System.Random random = new System.Random (7);
			for (int i = 0; i < count; i++) {
				int start = Mathf.RoundToInt (i * stagger * SampleRate);
				float previous = 0f;
				for (int s = 0; s < 0.07f * SampleRate && start + s < samples.Length; s++) {
					float t = s / (float)SampleRate;
					float noise = (float)(random.NextDouble () * 2.0 - 1.0);
					float high = noise - previous * 0.6f; //leaves the high part of the noise: a papery flick
					previous = noise;
					samples [start + s] += high * Mathf.Exp (-70f * t) * 0.5f;
				}
			}
			clip = MakeClip ("Deal", samples);
			deals [key] = clip;
		}
		Play (clip, 0.7f);
	}
}
