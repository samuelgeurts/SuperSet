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
	const float HatchPeriod = 16f; //distance between the lines of a fill, in mark texels; the same for every size
	const float HatchWidth = 3f;

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

	static float Clamp01 (float v)
	{
		return Mathf.Clamp01 (v);
	}

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
				Color32 color = Color32.Lerp (new Color32 (255, 255, 255, 255), CardBorder, edge);
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
	static float Stripe (float c)
	{
		float f = c / HatchPeriod;
		f -= Mathf.Floor (f);
		float distance = Mathf.Min (f, 1f - f) * HatchPeriod;
		return Clamp01 (HatchWidth / 2f + 0.5f - distance);
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
						float lines = Stripe ((ux + uy) * 0.7071f);
						if (fill == 2) {
							lines = Mathf.Max (lines, Stripe ((ux - uy) * 0.7071f));
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
