using UnityEngine;

//the look of the game: dark blue with a teal accent. Everything is drawn from code, no extra image files.
public static class UiStyle
{
	public static readonly Color Background = new Color (0.043f, 0.071f, 0.125f); //#0B1220
	public static readonly Color BackgroundTop = new Color (0.106f, 0.165f, 0.290f); //#1B2A4A
	public static readonly Color Panel = new Color (0.086f, 0.137f, 0.239f, 0.92f);
	public static readonly Color Button = new Color (0.133f, 0.208f, 0.333f);
	public static readonly Color ButtonHover = new Color (0.173f, 0.271f, 0.439f);
	public static readonly Color Accent = new Color (0.055f, 0.624f, 0.557f); //teal
	public static readonly Color AccentHover = new Color (0.176f, 0.831f, 0.749f);
	public static readonly Color Text = new Color (0.91f, 0.933f, 0.988f);
	public static readonly Color TextDim = new Color (0.624f, 0.702f, 0.851f);
	public static readonly Color Found = new Color (0.93f, 0.2f, 0.25f);

	//border colors of the sets on the table, one per set so they can be told apart: the first set has the color of the
	//selection (teal), then each next set has its own. The selection frame shows the color the set will get.
	public static readonly Color[] FoundColors = {
		new Color (0.176f, 0.831f, 0.749f), //teal
		new Color (0.98f, 0.80f, 0.08f), //gold
		new Color (0.96f, 0.45f, 0.71f), //pink
		new Color (0.65f, 0.55f, 0.98f), //violet
		new Color (0.98f, 0.45f, 0.30f), //orange red
		new Color (1f, 1f, 1f), //white
	};

	public static Color FoundColor (int setNumber)
	{
		return FoundColors [setNumber % FoundColors.Length];
	}

	const int TextureSize = 48;
	const int CornerPixels = 16;

	//a white rounded square with soft edges, used for sprites and (tinted) for the GUI
	static Texture2D RoundedTexture (Color color)
	{
		Texture2D texture = new Texture2D (TextureSize, TextureSize, TextureFormat.RGBA32, false);
		texture.filterMode = FilterMode.Bilinear;
		texture.wrapMode = TextureWrapMode.Clamp;
		for (int y = 0; y < TextureSize; y++) {
			for (int x = 0; x < TextureSize; x++) {
				//distance outside the rounded rectangle; 0 or less is inside
				float dx = Mathf.Max (CornerPixels - x - 0.5f, x + 0.5f - (TextureSize - CornerPixels), 0f);
				float dy = Mathf.Max (CornerPixels - y - 0.5f, y + 0.5f - (TextureSize - CornerPixels), 0f);
				float outside = Mathf.Sqrt (dx * dx + dy * dy) - CornerPixels;
				float alpha = Mathf.Clamp01 (0.5f - outside);
				texture.SetPixel (x, y, new Color (color.r, color.g, color.b, color.a * alpha));
			}
		}
		texture.Apply ();
		return texture;
	}

	//a rounded frame: only a ring along the edge is drawn, the middle is empty
	static Texture2D FrameTexture (Color color, float ringPixels)
	{
		Texture2D texture = new Texture2D (TextureSize, TextureSize, TextureFormat.RGBA32, false);
		texture.filterMode = FilterMode.Bilinear;
		texture.wrapMode = TextureWrapMode.Clamp;
		for (int y = 0; y < TextureSize; y++) {
			for (int x = 0; x < TextureSize; x++) {
				float dx = Mathf.Max (CornerPixels - x - 0.5f, x + 0.5f - (TextureSize - CornerPixels), 0f);
				float dy = Mathf.Max (CornerPixels - y - 0.5f, y + 0.5f - (TextureSize - CornerPixels), 0f);
				float outside = Mathf.Sqrt (dx * dx + dy * dy) - CornerPixels;
				//along the straight edges the distance to the edge counts, in the corners the distance to the arc
				float edge = Mathf.Min (Mathf.Min (x + 0.5f, TextureSize - x - 0.5f), Mathf.Min (y + 0.5f, TextureSize - y - 0.5f));
				float depth = (dx > 0f || dy > 0f) ? -outside : edge;
				float alpha = Mathf.Clamp01 (0.5f - outside) * Mathf.Clamp01 (ringPixels - depth + 0.5f);
				texture.SetPixel (x, y, new Color (color.r, color.g, color.b, color.a * alpha));
			}
		}
		texture.Apply ();
		return texture;
	}

	static Sprite frameSprite;

	//a frame in the game world (selection); cornerWorld is the size of a corner in world units, the ring is half of it.
	//Use it with drawMode Sliced and set the size of the SpriteRenderer.
	public static Sprite FrameSprite (float cornerWorld)
	{
		if (frameSprite == null) {
			frameSprite = Sprite.Create (FrameTexture (Color.white, CornerPixels / 2f), new Rect (0, 0, TextureSize, TextureSize),
				new Vector2 (0.5f, 0.5f), CornerPixels / cornerWorld, 0, SpriteMeshType.FullRect,
				new Vector4 (CornerPixels, CornerPixels, CornerPixels, CornerPixels));
		}
		return frameSprite;
	}

	//a soft shadow: a rounded rectangle that fades out, to put under cards; use it with drawMode Sliced.
	//The rectangle itself is as big as the size you set minus twice the margin.
	const int ShadowTexture = 96;
	const int ShadowInset = 28;
	public const float SoftShadowMargin = ShadowInset / 100f;
	static Sprite softShadow;

	public static Sprite SoftShadow {
		get {
			if (softShadow == null) {
				Texture2D texture = new Texture2D (ShadowTexture, ShadowTexture, TextureFormat.RGBA32, false);
				texture.filterMode = FilterMode.Bilinear;
				texture.wrapMode = TextureWrapMode.Clamp;
				float half = (ShadowTexture - 2 * ShadowInset) / 2f;
				const float radius = 12f;
				for (int y = 0; y < ShadowTexture; y++) {
					for (int x = 0; x < ShadowTexture; x++) {
						float qx = Mathf.Abs (x + 0.5f - ShadowTexture / 2f) - (half - radius);
						float qy = Mathf.Abs (y + 0.5f - ShadowTexture / 2f) - (half - radius);
						float outside = Mathf.Sqrt (Mathf.Max (qx, 0f) * Mathf.Max (qx, 0f) + Mathf.Max (qy, 0f) * Mathf.Max (qy, 0f))
						                + Mathf.Min (Mathf.Max (qx, qy), 0f) - radius;
						float alpha = Mathf.SmoothStep (1f, 0f, Mathf.Clamp01 (outside / 22f));
						texture.SetPixel (x, y, new Color (0f, 0f, 0f, alpha));
					}
				}
				texture.Apply ();
				softShadow = Sprite.Create (texture, new Rect (0, 0, ShadowTexture, ShadowTexture), new Vector2 (0.5f, 0.5f), 100f, 0,
					SpriteMeshType.FullRect, new Vector4 (ShadowInset, ShadowInset, ShadowInset, ShadowInset));
			}
			return softShadow;
		}
	}

	static Sprite roundedSprite;

	//rounded rectangle for in the game world (shadows, borders); cornerWorld is the size of a corner in world units.
	//Use it with drawMode Sliced and set the size of the SpriteRenderer.
	public static Sprite RoundedSprite (float cornerWorld)
	{
		if (roundedSprite == null) {
			roundedSprite = Sprite.Create (RoundedTexture (Color.white), new Rect (0, 0, TextureSize, TextureSize),
				new Vector2 (0.5f, 0.5f), CornerPixels / cornerWorld, 0, SpriteMeshType.FullRect,
				new Vector4 (CornerPixels, CornerPixels, CornerPixels, CornerPixels));
		}
		return roundedSprite;
	}

	static Texture2D FlatTexture (Color color)
	{
		Texture2D texture = new Texture2D (2, 2, TextureFormat.RGBA32, false);
		texture.SetPixels (new Color[] { color, color, color, color });
		texture.Apply ();
		return texture;
	}

	static Texture2D chevron;

	//a small white triangle pointing down, for drop-down buttons
	public static Texture2D Chevron {
		get {
			if (chevron == null) {
				const int size = 32;
				chevron = new Texture2D (size, size, TextureFormat.RGBA32, false);
				chevron.filterMode = FilterMode.Bilinear;
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						//the triangle: wide at the top (y = 24) and a point at the bottom (y = 6)
						int hits = 0;
						for (int sy = 0; sy < 4; sy++) {
							for (int sx = 0; sx < 4; sx++) {
								float px = x + (sx + 0.5f) / 4f;
								float py = y + (sy + 0.5f) / 4f;
								float half = (py - 6f) / 18f * 12f; //half the width at this height
								if (py >= 6f && py <= 24f && Mathf.Abs (px - size / 2f) <= half) {
									hits++;
								}
							}
						}
						chevron.SetPixel (x, y, new Color (1f, 1f, 1f, hits / 16f));
					}
				}
				chevron.Apply ();
			}
			return chevron;
		}
	}

	static GUIStyle MakeBox (Color color, Color text)
	{
		GUIStyle style = new GUIStyle ();
		style.normal.background = RoundedTexture (color);
		style.normal.textColor = text;
		style.border = new RectOffset (CornerPixels, CornerPixels, CornerPixels, CornerPixels);
		return style;
	}

	static GUIStyle MakeButton (Color normal, Color hover, int fontSize)
	{
		GUIStyle style = MakeBox (normal, Text);
		style.hover.background = RoundedTexture (hover);
		style.hover.textColor = Color.white;
		style.active.background = RoundedTexture (hover);
		style.active.textColor = Color.white;
		style.alignment = TextAnchor.MiddleCenter;
		style.fontSize = fontSize;
		style.fontStyle = FontStyle.Bold;
		style.margin = new RectOffset (0, 0, 0, 0);
		return style;
	}

	public static GUIStyle PanelStyle, BannerStyle, ButtonStyle, ChosenButtonStyle, ActionButtonStyle;
	public static GUIStyle TitleStyle, ValueStyle, LabelStyle, RuleStyle, MessageStyle, MessageCenteredStyle, ScoreStyle16, BarStyle, MenuPanelStyle, CountStyle;

	//create the GUI styles once; call from OnGUI
	public static void EnsureStyles ()
	{
		if (PanelStyle != null) {
			return;
		}
		PanelStyle = MakeBox (Panel, Text);
		BannerStyle = MakeBox (new Color (0.086f, 0.137f, 0.239f, 0.75f), TextDim);
		BannerStyle.padding = new RectOffset (12, 12, 5, 5);
		BannerStyle.fontSize = 12;
		BannerStyle.wordWrap = true;
		BannerStyle.alignment = TextAnchor.MiddleLeft;

		ButtonStyle = MakeButton (Button, ButtonHover, 14);
		ChosenButtonStyle = MakeButton (Accent, AccentHover, 14);
		ActionButtonStyle = MakeButton (Accent, AccentHover, 16);

		TitleStyle = new GUIStyle ();
		TitleStyle.fontSize = 24;
		TitleStyle.fontStyle = FontStyle.Bold;
		TitleStyle.normal.textColor = Color.white;
		ValueStyle = new GUIStyle (TitleStyle);
		ValueStyle.normal.textColor = AccentHover;
		LabelStyle = new GUIStyle ();
		LabelStyle.fontSize = 12;
		LabelStyle.normal.textColor = TextDim;
		LabelStyle.wordWrap = true;
		RuleStyle = BannerStyle;
		MenuPanelStyle = MakeBox (new Color (0.075f, 0.12f, 0.21f, 1f), Text);
		BarStyle = new GUIStyle ();
		BarStyle.normal.background = FlatTexture (new Color (0.086f, 0.137f, 0.239f, 0.9f));
		ScoreStyle16 = new GUIStyle (LabelStyle);
		ScoreStyle16.fontSize = 17;
		ScoreStyle16.fontStyle = FontStyle.Bold;
		ScoreStyle16.normal.textColor = AccentHover;
		MessageStyle = new GUIStyle (LabelStyle);
		MessageStyle.fontSize = 14;
		MessageStyle.fontStyle = FontStyle.Bold;
		MessageStyle.normal.textColor = AccentHover;
		MessageCenteredStyle = new GUIStyle (MessageStyle);
		MessageCenteredStyle.alignment = TextAnchor.MiddleCenter;
		CountStyle = new GUIStyle (LabelStyle);
		CountStyle.alignment = TextAnchor.UpperCenter;
		CountStyle.fontSize = 12;
		CountStyle.fontStyle = FontStyle.Bold;
	}

	//sets up the camera and a gradient behind the cards; returns nothing, the background follows the camera by itself
	public static void CreateBackground ()
	{
		Camera cam = Camera.main;
		if (cam == null) {
			return;
		}
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = Background;

		Texture2D gradient = new Texture2D (1, 256, TextureFormat.RGBA32, false);
		gradient.wrapMode = TextureWrapMode.Clamp;
		for (int y = 0; y < 256; y++) {
			gradient.SetPixel (0, y, Color.Lerp (Background, BackgroundTop, y / 255f));
		}
		gradient.Apply ();

		GameObject background = new GameObject ("Background");
		SpriteRenderer renderer = background.AddComponent<SpriteRenderer> ();
		renderer.sprite = Sprite.Create (gradient, new Rect (0, 0, 1, 256), new Vector2 (0.5f, 0.5f), 256f);
		renderer.sortingOrder = -100;
		background.AddComponent<BackgroundFit> ();
	}
}

//keeps the background gradient exactly as big as what the camera sees
public class BackgroundFit : MonoBehaviour
{
	void LateUpdate ()
	{
		Camera cam = Camera.main;
		if (cam == null) {
			return;
		}
		float height = cam.orthographicSize * 2f;
		float width = height * cam.aspect;
		transform.position = new Vector3 (cam.transform.position.x, cam.transform.position.y, 0f);
		transform.localScale = new Vector3 (width * 256f, height, 1f); //the gradient sprite is 1 pixel wide
	}
}
