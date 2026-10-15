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

	//border colors of the found sets on the table, one per set so they can be told apart
	public static readonly Color[] FoundColors = {
		new Color (0.93f, 0.2f, 0.25f), //red
		new Color (1f, 0.76f, 0.1f), //amber
		new Color (0.3f, 0.85f, 0.35f), //green
		new Color (0.75f, 0.4f, 1f), //purple
		new Color (1f, 0.5f, 0.8f), //pink
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
	public static GUIStyle TitleStyle, ValueStyle, LabelStyle, RuleStyle, MessageStyle;

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
		MessageStyle = new GUIStyle (LabelStyle);
		MessageStyle.fontSize = 14;
		MessageStyle.fontStyle = FontStyle.Bold;
		MessageStyle.normal.textColor = AccentHover;
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
