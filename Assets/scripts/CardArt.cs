using System.Collections.Generic;
using UnityEngine;

//draws the cards in code, so no image per card is needed: a white card with 1 to 4 marks.
//A mark has a shape, a fill, a color and a size (scale). The size is drawn into the mark itself,
//so the lines of the fill keep the same distance on every size.
//The dimensions follow SetLogic.IndexToDimension: color, shape, number, structure (fill), scale.
public static class CardArt
{
	//the card: 160 x 240 units of 1/42, drawn on a texture of twice that size
	const int CardWidth = 320;
	const int CardHeight = 480;
	const float CardPixelsPerUnit = 84f;
	static readonly Color32 CardBorder = new Color32 (129, 130, 133, 255);

	//shape coordinates are in units of 51 card pixels (a normal mark is 2 units wide); this is the size of a unit
	const float UnitInCard = 51f / 42f;
	const float TexelsPerUnit = 120f;
	const float MarkPixelsPerUnit = TexelsPerUnit / UnitInCard;
	const float LineWidth = 3.5f; //in mark texels
	const float HatchPeriod = 12f; //distance between the lines of a fill, in mark texels; the same for every size
	//the width of the lines: the fills get equal steps of darkness (outline, diagonal lines, crossed lines, solid);
	//one set of lines covers a third of the inside, two crossed sets cover two thirds
	public static float HatchWidthSingle = HatchPeriod / 3f;
	public static float HatchWidthCrossed = HatchPeriod * (1f - Mathf.Sqrt (1f / 3f));

	//distance between the marks of a card, in card units
	public const float MarkSpacing = 48f / 42f;

	static readonly Color32[] colors = {
		new Color32 (15, 117, 188, 255), //blue
		new Color32 (146, 41, 142, 255), //purple
		new Color32 (247, 149, 32, 255), //orange
		new Color32 (59, 181, 74, 255), //green
	};

	//width and height of a mark per scale, compared with the normal mark
	static readonly Vector2[] markScales = {
		new Vector2 (1f, 1f),
		new Vector2 (0.5f, 0.79f),
		new Vector2 (1.49f, 1.26f),
		new Vector2 (0.78f, 1.32f),
	};

	static Sprite cardSprite;

	//the white rounded card with a thin grey border
	public static Sprite CardSprite {
		get {
			if (cardSprite == null) {
				cardSprite = MakeCardSprite ();
			}
			return cardSprite;
		}
	}

	static Sprite backSprite;

	//the back of a card: dark blue with a teal frame and diamond, the size of the white card
	public static Sprite BackSprite {
		get {
			if (backSprite == null) {
				backSprite = MakeBackSprite ();
			}
			return backSprite;
		}
	}

	//signed distance (in pixels, positive outside) to a rounded box around the middle of the texture
	static float RoundedBoxDistance (float x, float y, float halfX, float halfY, float radius)
	{
		float qx = Mathf.Abs (x - CardWidth / 2f) - (halfX - radius);
		float qy = Mathf.Abs (y - CardHeight / 2f) - (halfY - radius);
		return Mathf.Sqrt (Mathf.Max (qx, 0f) * Mathf.Max (qx, 0f) + Mathf.Max (qy, 0f) * Mathf.Max (qy, 0f))
		+ Mathf.Min (Mathf.Max (qx, qy), 0f) - radius;
	}

	static float Clamp01 (float v)
	{
		return Mathf.Clamp01 (v);
	}

	//a value between -1 and 1 for a pixel; the same every time, so the paper looks the same on every start
	static float Noise (int x, int y)
	{
		uint h = (uint)x * 374761393u + (uint)y * 668265263u;
		h = (h ^ (h >> 13)) * 1274126177u;
		h ^= h >> 16;
		return (h & 0xFFFF) / 32767.5f - 1f;
	}

	//---------------------------------------------------------------- the logo on the back: a stroke font

	static void Arc (List<Vector2> line, float cx, float cy, float rx, float ry, float from, float to, int steps)
	{
		for (int i = 0; i <= steps; i++) {
			float angle = Mathf.Lerp (from, to, i / (float)steps) * Mathf.Deg2Rad;
			line.Add (new Vector2 (cx + rx * Mathf.Cos (angle), cy + ry * Mathf.Sin (angle)));
		}
	}

	static List<Vector2> Line (params float[] xy)
	{
		List<Vector2> line = new List<Vector2> ();
		for (int i = 0; i < xy.Length; i += 2) {
			line.Add (new Vector2 (xy [i], xy [i + 1]));
		}
		return line;
	}

	//the lines of a letter in a box that is 1 high for S and t and 0.62 high for the small letters; returns the width
	static float Letter (char c, List<List<Vector2>> lines)
	{
		switch (c) {
		case 'S': {
				List<Vector2> s = new List<Vector2> ();
				Arc (s, 0.37f, 0.745f, 0.31f, 0.245f, 22f, 270f, 18);
				Arc (s, 0.37f, 0.255f, 0.31f, 0.245f, 90f, -158f, 18);
				lines.Add (s);
				return 0.74f;
			}
		case 'u': {
				List<Vector2> u = Line (0.06f, 0.62f, 0.06f, 0.27f);
				Arc (u, 0.28f, 0.27f, 0.22f, 0.27f, 180f, 360f, 14);
				lines.Add (u);
				lines.Add (Line (0.5f, 0.62f, 0.5f, 0f));
				return 0.56f;
			}
		case 'p': {
				lines.Add (Line (0.06f, 0.62f, 0.06f, -0.32f));
				List<Vector2> bowl = new List<Vector2> ();
				Arc (bowl, 0.31f, 0.31f, 0.25f, 0.31f, 180f, -180f, 28);
				lines.Add (bowl);
				return 0.6f;
			}
		case 'e': {
				List<Vector2> e = Line (0.04f, 0.31f, 0.56f, 0.31f);
				Arc (e, 0.3f, 0.31f, 0.26f, 0.31f, 0f, 322f, 26);
				lines.Add (e);
				return 0.6f;
			}
		case 'r': {
				lines.Add (Line (0.06f, 0.62f, 0.06f, 0f));
				List<Vector2> r = new List<Vector2> ();
				Arc (r, 0.28f, 0.4f, 0.22f, 0.22f, 180f, 55f, 10);
				lines.Add (r);
				return 0.42f;
			}
		default: { //t
				List<Vector2> t = Line (0.2f, 0.88f, 0.2f, 0.14f);
				Arc (t, 0.4f, 0.14f, 0.2f, 0.14f, 180f, 270f, 8);
				lines.Add (t);
				lines.Add (Line (0f, 0.64f, 0.44f, 0.64f));
				return 0.46f;
			}
		}
	}

	const float LetterGap = 0.16f;

	static float WordWidth (string word)
	{
		float width = 0f;
		foreach (char c in word) {
			width += Letter (c, new List<List<Vector2>> ()) + LetterGap;
		}
		return width - LetterGap;
	}

	static float DistanceToSegment (float px, float py, Vector2 a, Vector2 b)
	{
		float dx = b.x - a.x;
		float dy = b.y - a.y;
		float length = dx * dx + dy * dy;
		float t = length > 0f ? Mathf.Clamp01 (((px - a.x) * dx + (py - a.y) * dy) / length) : 0f;
		float ex = px - (a.x + t * dx);
		float ey = py - (a.y + t * dy);
		return Mathf.Sqrt (ex * ex + ey * ey);
	}

	//draw a word with round strokes into a coverage map of the card texture; unit is the height of a tall letter in pixels
	static void DrawWord (float[] cover, string word, float left, float baseline, float unit, float strokePixels)
	{
		float x = left;
		foreach (char c in word) {
			List<List<Vector2>> lines = new List<List<Vector2>> ();
			float advance = Letter (c, lines);

			//the segments of this letter in pixels, and the box they cover
			List<Vector2[]> segments = new List<Vector2[]> ();
			float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
			foreach (List<Vector2> line in lines) {
				for (int i = 0; i + 1 < line.Count; i++) {
					Vector2 a = new Vector2 (x + line [i].x * unit, baseline + line [i].y * unit);
					Vector2 b = new Vector2 (x + line [i + 1].x * unit, baseline + line [i + 1].y * unit);
					segments.Add (new Vector2[] { a, b });
					minX = Mathf.Min (minX, Mathf.Min (a.x, b.x));
					maxX = Mathf.Max (maxX, Mathf.Max (a.x, b.x));
					minY = Mathf.Min (minY, Mathf.Min (a.y, b.y));
					maxY = Mathf.Max (maxY, Mathf.Max (a.y, b.y));
				}
			}
			float half = strokePixels / 2f;
			int x0 = Mathf.Max (0, Mathf.FloorToInt (minX - half - 2f));
			int x1 = Mathf.Min (CardWidth - 1, Mathf.CeilToInt (maxX + half + 2f));
			int y0 = Mathf.Max (0, Mathf.FloorToInt (minY - half - 2f));
			int y1 = Mathf.Min (CardHeight - 1, Mathf.CeilToInt (maxY + half + 2f));
			for (int y = y0; y <= y1; y++) {
				for (int px = x0; px <= x1; px++) {
					float best = float.MaxValue;
					foreach (Vector2[] segment in segments) {
						best = Mathf.Min (best, DistanceToSegment (px + 0.5f, y + 0.5f, segment [0], segment [1]));
					}
					float value = Clamp01 (half - best + 0.5f);
					int index = y * CardWidth + px;
					cover [index] = Mathf.Max (cover [index], value);
				}
			}
			x += (advance + LetterGap) * unit;
		}
	}

	//the back of a card: dark blue with a double frame, a fine diagonal pattern and the name of the game in the middle
	static Sprite MakeBackSprite ()
	{
		float[] superCover = new float[CardWidth * CardHeight];
		float[] setCover = new float[CardWidth * CardHeight];
		const float textWidth = 232f;
		float unitSuper = textWidth / WordWidth ("Super");
		float unitSet = textWidth / WordWidth ("Set");
		DrawWord (superCover, "Super", (CardWidth - textWidth) / 2f, 283f, unitSuper, 0.095f * unitSuper);
		DrawWord (setCover, "Set", (CardWidth - textWidth) / 2f, 133f, unitSet, 0.095f * unitSet);

		Color32[] pixels = new Color32[CardWidth * CardHeight];
		Color32 body = new Color32 (22, 35, 61, 255);
		Color32 bodyLight = new Color32 (31, 49, 84, 255);
		Color32 teal = new Color32 (45, 212, 191, 255);
		Color32 paper = new Color32 (232, 238, 252, 255);
		for (int y = 0; y < CardHeight; y++) {
			for (int x = 0; x < CardWidth; x++) {
				float px = x + 0.5f;
				float py = y + 0.5f;
				float outside = RoundedBoxDistance (px, py, CardWidth / 2f, CardHeight / 2f, 16f);
				float cover = Clamp01 (0.5f - outside);
				float edge = Clamp01 (outside + 3f + 0.5f);

				//a soft light from the top left over the dark blue, and fine diagonal lines
				float light = Clamp01 ((py / CardHeight) * 0.6f + (1f - px / CardWidth) * 0.4f);
				Color32 color = Color32.Lerp (body, bodyLight, light * 0.7f);
				float diagonal = Mathf.Abs (((px + py) % 14f) - 7f);
				color = Color32.Lerp (color, teal, Clamp01 (1.2f - diagonal) * 0.07f);

				//two thin frames, the outer one stronger
				float frame1 = RoundedBoxDistance (px, py, CardWidth / 2f - 18f, CardHeight / 2f - 18f, 9f);
				float frame2 = RoundedBoxDistance (px, py, CardWidth / 2f - 28f, CardHeight / 2f - 28f, 5f);
				float rings = Mathf.Max (Clamp01 (2f - Mathf.Abs (frame1)), 0.45f * Clamp01 (1.2f - Mathf.Abs (frame2)));
				color = Color32.Lerp (color, teal, rings);

				//the logo: Super in white, Set in teal, over the pattern
				color = Color32.Lerp (color, paper, superCover [y * CardWidth + x]);
				color = Color32.Lerp (color, teal, setCover [y * CardWidth + x]);

				color = Color32.Lerp (color, CardBorder, edge);
				color.a = (byte)(cover * 255f);
				pixels [y * CardWidth + x] = color;
			}
		}
		return MakeSprite (pixels, CardWidth, CardHeight, CardPixelsPerUnit);
	}

	//the front of a card: paper that is a little off white, with a fine grain, a soft shade along the rounded edge and a thin light line inside it
	static Sprite MakeCardSprite ()
	{
		Color32[] pixels = new Color32[CardWidth * CardHeight];
		const float radius = 16f;
		const float border = 3f;
		float halfX = CardWidth / 2f;
		float halfY = CardHeight / 2f;
		for (int y = 0; y < CardHeight; y++) {
			for (int x = 0; x < CardWidth; x++) {
				float qx = Mathf.Abs (x + 0.5f - halfX) - (halfX - radius);
				float qy = Mathf.Abs (y + 0.5f - halfY) - (halfY - radius);
				float outside = Mathf.Sqrt (Mathf.Max (qx, 0f) * Mathf.Max (qx, 0f) + Mathf.Max (qy, 0f) * Mathf.Max (qy, 0f))
				                + Mathf.Min (Mathf.Max (qx, qy), 0f) - radius;
				float cover = Clamp01 (0.5f - outside);
				float edge = Clamp01 (outside + border + 0.5f);

				float depth = -outside; //positive inside the card
				float rim = Clamp01 (1f - depth / 14f);
				float shade = 1f - 0.07f * rim * rim; //the edge is a little darker, like a card that bends away
				float lift = 0.014f * (y / (float)CardHeight - 0.5f); //a little lighter at the top
				float grain = Noise (x, y) * 0.011f + Noise (x / 5, y / 5) * 0.007f; //fine and coarser paper fibres
				float shine = 0.02f * Clamp01 (1.5f - Mathf.Abs (depth - 5f)); //a thin light line inside the border

				byte r = (byte)(255f * Clamp01 (0.986f * shade + lift + grain + shine));
				byte g = (byte)(255f * Clamp01 (0.979f * shade + lift + grain + shine));
				byte b = (byte)(255f * Clamp01 (0.958f * shade + lift + grain + shine));
				Color32 color = Color32.Lerp (new Color32 (r, g, b, 255), CardBorder, edge);
				color.a = (byte)(cover * 255f);
				pixels [y * CardWidth + x] = color;
			}
		}
		return MakeSprite (pixels, CardWidth, CardHeight, CardPixelsPerUnit);
	}

	static Sprite MakeSprite (Color32[] pixels, int width, int height, float pixelsPerUnit)
	{
		Texture2D texture = new Texture2D (width, height, TextureFormat.RGBA32, true);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Trilinear;
		texture.SetPixels32 (pixels);
		texture.Apply (true);
		return Sprite.Create (texture, new Rect (0, 0, width, height), new Vector2 (0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
	}

	//outlines of the shapes (cloud, bolt, star, lens) as x, y pairs around the middle of the mark, traced from the card design
	static readonly float[][] outlines = {
		//cloud
		new float[] { 0.995f, 0.015f, 0.941f, 0.078f, 0.902f, 0.098f, 0.809f, 0.127f, 0.794f, 0.123f, 0.760f, 0.137f, 0.711f, 0.137f, 0.701f, 0.147f, 0.701f, 0.211f, 0.676f, 0.255f, 0.598f, 0.314f, 0.515f, 0.343f, 0.500f, 0.338f, 0.480f, 0.353f, 0.436f, 0.353f, 0.417f, 0.363f, 0.382f, 0.358f, 0.348f, 0.373f, 0.137f, 0.373f, 0.103f, 0.358f, 0.069f, 0.363f, 0.049f, 0.353f, 0.005f, 0.353f, -0.015f, 0.338f, -0.029f, 0.343f, -0.162f, 0.294f, -0.191f, 0.275f, -0.275f, 0.275f, -0.289f, 0.284f, -0.539f, 0.284f, -0.618f, 0.265f, -0.696f, 0.221f, -0.750f, 0.142f, -0.799f, 0.137f, -0.819f, 0.123f, -0.833f, 0.127f, -0.848f, 0.118f, -0.882f, 0.118f, -0.966f, 0.078f, -0.995f, 0.034f, -1.000f, -0.010f, -0.946f, -0.074f, -0.907f, -0.093f, -0.814f, -0.123f, -0.799f, -0.118f, -0.765f, -0.132f, -0.716f, -0.132f, -0.706f, -0.142f, -0.706f, -0.206f, -0.681f, -0.250f, -0.603f, -0.309f, -0.520f, -0.338f, -0.505f, -0.333f, -0.485f, -0.348f, -0.441f, -0.348f, -0.422f, -0.358f, -0.387f, -0.353f, -0.353f, -0.368f, -0.142f, -0.368f, -0.108f, -0.353f, -0.074f, -0.358f, -0.054f, -0.348f, -0.010f, -0.348f, 0.010f, -0.333f, 0.025f, -0.338f, 0.157f, -0.289f, 0.186f, -0.270f, 0.270f, -0.270f, 0.284f, -0.279f, 0.534f, -0.279f, 0.613f, -0.260f, 0.691f, -0.216f, 0.745f, -0.137f, 0.794f, -0.132f, 0.814f, -0.118f, 0.828f, -0.123f, 0.843f, -0.113f, 0.877f, -0.113f, 0.961f, -0.074f, 0.990f, -0.029f },
		//bolt
		new float[] { -0.980f, 0.402f, -0.980f, 0.191f, -0.971f, 0.181f, -0.931f, 0.181f, -0.907f, 0.172f, -0.877f, 0.176f, -0.853f, 0.162f, -0.804f, 0.167f, -0.775f, 0.152f, -0.735f, 0.157f, -0.701f, 0.142f, -0.667f, 0.147f, -0.637f, 0.132f, -0.583f, 0.137f, -0.559f, 0.123f, -0.500f, 0.123f, -0.475f, 0.113f, -0.446f, 0.118f, -0.422f, 0.103f, -0.373f, 0.108f, -0.343f, 0.093f, -0.304f, 0.098f, -0.270f, 0.083f, -0.235f, 0.088f, -0.206f, 0.074f, -0.147f, 0.078f, -0.137f, 0.049f, -0.230f, -0.088f, -0.319f, -0.245f, -0.275f, -0.245f, -0.245f, -0.260f, -0.191f, -0.255f, -0.167f, -0.270f, -0.118f, -0.265f, -0.088f, -0.279f, -0.034f, -0.275f, -0.010f, -0.289f, 0.069f, -0.289f, 0.093f, -0.299f, 0.127f, -0.294f, 0.162f, -0.309f, 0.225f, -0.309f, 0.250f, -0.319f, 0.284f, -0.314f, 0.319f, -0.328f, 0.373f, -0.324f, 0.402f, -0.338f, 0.456f, -0.333f, 0.480f, -0.348f, 0.529f, -0.343f, 0.559f, -0.358f, 0.613f, -0.353f, 0.637f, -0.368f, 0.716f, -0.368f, 0.740f, -0.377f, 0.775f, -0.373f, 0.809f, -0.387f, 0.873f, -0.387f, 0.897f, -0.397f, 0.931f, -0.392f, 0.966f, -0.407f, 1.005f, -0.407f, 1.015f, -0.397f, 1.015f, -0.186f, 1.005f, -0.176f, 0.966f, -0.176f, 0.941f, -0.167f, 0.912f, -0.172f, 0.887f, -0.157f, 0.838f, -0.162f, 0.809f, -0.147f, 0.770f, -0.152f, 0.735f, -0.137f, 0.701f, -0.142f, 0.672f, -0.127f, 0.618f, -0.132f, 0.593f, -0.118f, 0.534f, -0.118f, 0.510f, -0.108f, 0.480f, -0.113f, 0.456f, -0.098f, 0.407f, -0.103f, 0.377f, -0.088f, 0.338f, -0.093f, 0.304f, -0.078f, 0.270f, -0.083f, 0.240f, -0.069f, 0.181f, -0.074f, 0.172f, -0.044f, 0.265f, 0.093f, 0.353f, 0.250f, 0.309f, 0.250f, 0.279f, 0.265f, 0.225f, 0.260f, 0.201f, 0.275f, 0.152f, 0.270f, 0.123f, 0.284f, 0.069f, 0.279f, 0.044f, 0.294f, -0.034f, 0.294f, -0.059f, 0.304f, -0.093f, 0.299f, -0.127f, 0.314f, -0.191f, 0.314f, -0.216f, 0.324f, -0.250f, 0.319f, -0.284f, 0.333f, -0.338f, 0.328f, -0.368f, 0.343f, -0.422f, 0.338f, -0.446f, 0.353f, -0.495f, 0.348f, -0.525f, 0.363f, -0.578f, 0.358f, -0.603f, 0.373f, -0.681f, 0.373f, -0.706f, 0.382f, -0.740f, 0.377f, -0.775f, 0.392f, -0.838f, 0.392f, -0.863f, 0.402f, -0.897f, 0.397f, -0.931f, 0.412f, -0.971f, 0.412f },
		//star
		new float[] { -1.029f, 0.005f, -1.010f, -0.015f, -0.912f, -0.020f, -0.877f, -0.034f, -0.833f, -0.034f, -0.779f, -0.054f, -0.745f, -0.054f, -0.735f, -0.064f, -0.672f, -0.074f, -0.618f, -0.103f, -0.578f, -0.113f, -0.529f, -0.147f, -0.471f, -0.201f, -0.471f, -0.284f, -0.534f, -0.382f, -0.515f, -0.382f, -0.456f, -0.348f, -0.407f, -0.338f, -0.338f, -0.309f, -0.309f, -0.309f, -0.284f, -0.294f, -0.270f, -0.299f, -0.260f, -0.289f, -0.225f, -0.289f, -0.191f, -0.275f, 0.078f, -0.265f, 0.108f, -0.279f, 0.162f, -0.275f, 0.186f, -0.289f, 0.260f, -0.294f, 0.324f, -0.319f, 0.338f, -0.314f, 0.529f, -0.382f, 0.466f, -0.294f, 0.451f, -0.245f, 0.466f, -0.201f, 0.510f, -0.152f, 0.574f, -0.113f, 0.657f, -0.078f, 0.672f, -0.083f, 0.809f, -0.039f, 0.828f, -0.044f, 0.838f, -0.034f, 0.873f, -0.034f, 0.907f, -0.020f, 1.005f, -0.015f, 1.025f, 0.000f, 1.005f, 0.020f, 0.907f, 0.025f, 0.873f, 0.039f, 0.828f, 0.039f, 0.775f, 0.059f, 0.740f, 0.059f, 0.730f, 0.069f, 0.667f, 0.078f, 0.613f, 0.108f, 0.574f, 0.118f, 0.525f, 0.152f, 0.466f, 0.206f, 0.466f, 0.289f, 0.529f, 0.387f, 0.510f, 0.387f, 0.451f, 0.353f, 0.402f, 0.343f, 0.333f, 0.314f, 0.304f, 0.314f, 0.279f, 0.299f, 0.265f, 0.304f, 0.255f, 0.294f, 0.221f, 0.294f, 0.186f, 0.279f, -0.083f, 0.270f, -0.113f, 0.284f, -0.167f, 0.279f, -0.191f, 0.294f, -0.265f, 0.299f, -0.328f, 0.324f, -0.343f, 0.319f, -0.534f, 0.387f, -0.471f, 0.299f, -0.456f, 0.250f, -0.471f, 0.206f, -0.515f, 0.157f, -0.578f, 0.118f, -0.662f, 0.083f, -0.676f, 0.088f, -0.814f, 0.044f, -0.833f, 0.049f, -0.843f, 0.039f, -0.877f, 0.039f, -0.912f, 0.025f, -1.010f, 0.020f },
		//lens
		new float[] { -1.000f, -0.005f, -1.000f, -0.025f, -0.848f, -0.127f, -0.672f, -0.225f, -0.422f, -0.319f, -0.328f, -0.333f, -0.304f, -0.348f, -0.206f, -0.353f, -0.172f, -0.368f, 0.123f, -0.368f, 0.147f, -0.353f, 0.201f, -0.358f, 0.211f, -0.348f, 0.338f, -0.328f, 0.363f, -0.314f, 0.377f, -0.319f, 0.632f, -0.225f, 0.828f, -0.127f, 0.956f, -0.049f, 0.966f, -0.034f, 0.985f, -0.034f, 0.995f, -0.025f, 0.985f, 0.015f, 0.946f, 0.020f, 0.936f, 0.034f, 0.765f, 0.152f, 0.574f, 0.250f, 0.544f, 0.255f, 0.515f, 0.275f, 0.490f, 0.275f, 0.436f, 0.304f, 0.422f, 0.299f, 0.397f, 0.314f, 0.265f, 0.338f, 0.240f, 0.353f, -0.118f, 0.363f, -0.157f, 0.348f, -0.191f, 0.353f, -0.225f, 0.338f, -0.245f, 0.343f, -0.255f, 0.333f, -0.289f, 0.333f, -0.333f, 0.314f, -0.363f, 0.314f, -0.397f, 0.294f, -0.480f, 0.275f, -0.559f, 0.235f, -0.593f, 0.230f, -0.608f, 0.216f, -0.750f, 0.152f, -0.951f, 0.020f, -0.990f, 0.015f },
	};

	//the outline of a shape in the size of a scale, as texel coordinates around the middle of the mark
	static Vector2[] ScaledOutline (int shape, int scale)
	{
		float[] flat = outlines [shape];
		Vector2[] points = new Vector2[flat.Length / 2];
		for (int i = 0; i < points.Length; i++) {
			points [i] = new Vector2 (flat [2 * i] * markScales [scale].x * TexelsPerUnit, flat [2 * i + 1] * markScales [scale].y * TexelsPerUnit);
		}
		return points;
	}

	//signed distance from a point to a polygon in texels: positive inside, negative outside
	static float Distance (Vector2[] poly, float px, float py)
	{
		bool inside = false;
		float best = float.MaxValue;
		int n = poly.Length;
		for (int i = 0; i < n; i++) {
			Vector2 a = poly [i];
			Vector2 b = poly [(i + 1) % n];
			if ((a.y > py) != (b.y > py) && px < (b.x - a.x) * (py - a.y) / (b.y - a.y) + a.x) {
				inside = !inside;
			}
			float dx = b.x - a.x;
			float dy = b.y - a.y;
			float length = dx * dx + dy * dy;
			float t = length > 0f ? Mathf.Clamp01 (((px - a.x) * dx + (py - a.y) * dy) / length) : 0f;
			float ex = px - (a.x + t * dx);
			float ey = py - (a.y + t * dy);
			float d = ex * ex + ey * ey;
			if (d < best) {
				best = d;
			}
		}
		float distance = Mathf.Sqrt (best);
		return inside ? distance : -distance;
	}

	//a thin line pattern: 1 on a line, 0 between, along the coordinate c
	static float Stripe (float c, float width)
	{
		float f = c / HatchPeriod;
		f -= Mathf.Floor (f);
		float distance = Mathf.Min (f, 1f - f) * HatchPeriod;
		return Clamp01 (width / 2f + 0.5f - distance);
	}

	static Dictionary<int, Sprite> markSprites = new Dictionary<int, Sprite> ();

	//the distance of every texel of a mark to its outline; shared by all fills and colors of a shape in a size
	class Field
	{
		public int halfWidth, halfHeight;
		public float[] distance; //width * height
		public int Width { get { return 2 * halfWidth; } }
		public int Height { get { return 2 * halfHeight; } }
	}

	static Dictionary<int, Field> fields = new Dictionary<int, Field> ();

	static Field GetField (int shape, int scale)
	{
		int key = shape * 4 + scale;
		Field field;
		if (fields.TryGetValue (key, out field)) {
			return field;
		}
		Vector2[] outline = ScaledOutline (shape, scale);

		//a texture just big enough for the outline, with the middle of the shape in the middle of the texture
		float reachX = 0f;
		float reachY = 0f;
		foreach (Vector2 point in outline) {
			reachX = Mathf.Max (reachX, Mathf.Abs (point.x));
			reachY = Mathf.Max (reachY, Mathf.Abs (point.y));
		}
		field = new Field ();
		field.halfWidth = Mathf.CeilToInt (reachX) + 4;
		field.halfHeight = Mathf.CeilToInt (reachY) + 4;
		int width = field.Width;
		int height = field.Height;

		//the distance is measured on every second texel, which is much faster, and smoothed in between
		int gridWidth = width / 2 + 2;
		int gridHeight = height / 2 + 2;
		float[] grid = new float[gridWidth * gridHeight];
		for (int j = 0; j < gridHeight; j++) {
			for (int i = 0; i < gridWidth; i++) {
				grid [j * gridWidth + i] = Distance (outline, 2 * i - field.halfWidth, 2 * j - field.halfHeight);
			}
		}
		field.distance = new float[width * height];
		for (int y = 0; y < height; y++) {
			float gy = (y + 0.5f) / 2f;
			int j = (int)gy;
			float fy = gy - j;
			for (int x = 0; x < width; x++) {
				float gx = (x + 0.5f) / 2f;
				int i = (int)gx;
				float fx = gx - i;
				float top = Mathf.Lerp (grid [j * gridWidth + i], grid [j * gridWidth + i + 1], fx);
				float bottom = Mathf.Lerp (grid [(j + 1) * gridWidth + i], grid [(j + 1) * gridWidth + i + 1], fx);
				field.distance [y * width + x] = Mathf.Lerp (top, bottom, fy);
			}
		}
		fields [key] = field;
		return field;
	}

	//fill: 0 solid, 1 diagonal lines, 2 crossed lines, 3 outline only
	public static Sprite MarkSprite (int shape, int fill, int color, int scale)
	{
		int key = ((shape * 4 + fill) * 4 + color) * 4 + scale;
		Sprite sprite;
		if (!markSprites.TryGetValue (key, out sprite)) {
			Field field = GetField (shape, scale);
			int width = field.Width;
			int height = field.Height;
			Color32 rgb = colors [color];
			Color32[] pixels = new Color32[width * height];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					float ux = x + 0.5f - field.halfWidth;
					float uy = y + 0.5f - field.halfHeight;
					float d = field.distance [y * width + x];
					float cover = Clamp01 (d + 0.5f);
					float ring = cover * Clamp01 (LineWidth - d + 0.5f);
					float alpha;
					if (fill == 0) {
						alpha = cover;
					} else if (fill == 3) {
						alpha = ring;
					} else {
						//the lines run through the middle of the mark, so every mark has the same pattern
						float lineWidth = fill == 2 ? HatchWidthCrossed : HatchWidthSingle;
						float lines = Stripe ((ux + uy) * 0.7071f, lineWidth);
						if (fill == 2) {
							lines = Mathf.Max (lines, Stripe ((ux - uy) * 0.7071f, lineWidth));
						}
						alpha = Mathf.Max (ring, cover * lines);
					}
					rgb.a = (byte)(alpha * 255f);
					pixels [y * width + x] = rgb;
				}
			}
			sprite = MakeSprite (pixels, width, height, MarkPixelsPerUnit);
			markSprites [key] = sprite;
		}
		return sprite;
	}
}
