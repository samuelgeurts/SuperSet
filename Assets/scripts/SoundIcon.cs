using UnityEngine;

//the speaker icons of the sound button, drawn in code: a speaker with sound waves, and a speaker with a cross
public static class SoundIcon
{
	const int Size = 32;
	static Texture2D on, off;

	public static Texture2D On {
		get {
			if (on == null) {
				on = Make (true);
			}
			return on;
		}
	}

	public static Texture2D Off {
		get {
			if (off == null) {
				off = Make (false);
			}
			return off;
		}
	}

	//true if the point lies on the speaker (a box with a cone), the waves or the cross
	static bool Hit (float x, float y, bool waves)
	{
		bool speaker = (x >= 5f && x <= 11f && y >= 12f && y <= 20f)
		               || (x > 11f && x <= 18f && Mathf.Abs (y - 16f) <= 4f + (x - 11f) / 7f * 6f);
		if (speaker) {
			return true;
		}
		if (waves) {
			float dx = x - 18f;
			float dy = y - 16f;
			float distance = Mathf.Sqrt (dx * dx + dy * dy);
			bool inAngle = dx > 0f && Mathf.Abs (Mathf.Atan2 (dy, dx)) < 0.9f;
			return inAngle && (Mathf.Abs (distance - 7f) < 1.1f || Mathf.Abs (distance - 11.5f) < 1.1f);
		}
		float cx = x - 25.5f;
		float cy = y - 16f;
		return Mathf.Abs (cx) <= 4.5f && Mathf.Abs (cy) <= 4.5f && (Mathf.Abs (cx - cy) < 1.2f || Mathf.Abs (cx + cy) < 1.2f);
	}

	static Texture2D Make (bool waves)
	{
		Texture2D texture = new Texture2D (Size, Size, TextureFormat.RGBA32, false);
		texture.filterMode = FilterMode.Bilinear;
		for (int y = 0; y < Size; y++) {
			for (int x = 0; x < Size; x++) {
				int hits = 0;
				for (int sy = 0; sy < 4; sy++) {
					for (int sx = 0; sx < 4; sx++) {
						if (Hit (x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f, waves)) {
							hits++;
						}
					}
				}
				texture.SetPixel (x, y, new Color (1f, 1f, 1f, hits / 16f));
			}
		}
		texture.Apply ();
		return texture;
	}
}
