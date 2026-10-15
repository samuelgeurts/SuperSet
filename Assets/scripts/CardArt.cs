using System.Collections.Generic;
using UnityEngine;

//draws the cards in code, so no image per card is needed: a white card with 1 to 4 marks.
//A mark has a shape, a fill and a color; its size (scale) is set on the mark itself.
//The dimensions follow SetLogic.IndexToDimension: color, shape, number, structure (fill), scale.
public static class CardArt
{
	//the card: 160 x 240 units of 1/42, drawn on a texture of twice that size
	const int CardWidth = 320;
	const int CardHeight = 480;
	const float CardPixelsPerUnit = 84f;
	static readonly Color32 CardBorder = new Color32 (129, 130, 133, 255);

	//a mark: shape coordinates run from -1 to 1 in x; the texture holds them with a small margin
	const int MarkWidth = 256;
	const int MarkHeight = 128;
	const float MarkTexelsPerUnit = MarkWidth / 2.1f;
	const float MarkPixelsPerUnit = 100f; //a mark of the normal size is 0.64 of the card wide
	const float LineWidth = 3.5f; //in mark texels
	const float HatchPeriod = 8f;
	const float HatchWidth = 3f;

	//distance between the marks of a card, in card units
	public const float MarkSpacing = 1.14f;

	static readonly Color32[] colors = {
		new Color32 (15, 117, 188, 255), //blue
		new Color32 (146, 41, 142, 255), //purple
		new Color32 (247, 149, 32, 255), //orange
		new Color32 (59, 181, 74, 255), //green
	};

	//size of a mark per scale: normal, small, wide, narrow and tall
	static readonly Vector2[] markScales = {
		new Vector2 (1f, 1f),
		new Vector2 (0.5f, 0.5f),
		new Vector2 (1.5f, 1.1f),
		new Vector2 (0.8f, 1.2f),
	};

	public static Vector2 MarkScale (int scale)
	{
		return markScales [scale];
	}

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

	//outline of a shape as a closed polygon
	static Vector2[] ShapeOutline (int shape)
	{
		List<Vector2> p = new List<Vector2> ();
		switch (shape) {
		case 0: //cloud: an oval with a scalloped edge
			for (int i = 0; i < 84; i++) {
				float t = 2f * Mathf.PI * i / 84f;
				float bump = Mathf.Abs (Mathf.Cos (3.5f * t));
				p.Add (new Vector2 (Mathf.Cos (t) * (0.86f + 0.14f * bump), 0.36f * Mathf.Sin (t) * (0.76f + 0.24f * bump)));
			}
			break;
		case 1: //bolt: a slanted zigzag
			p.Add (new Vector2 (-0.95f, -0.10f));
			p.Add (new Vector2 (0.05f, 0.29f));
			p.Add (new Vector2 (0.05f, 0.10f));
			p.Add (new Vector2 (0.95f, 0.05f));
			p.Add (new Vector2 (-0.05f, -0.34f));
			p.Add (new Vector2 (-0.05f, -0.15f));
			break;
		case 2: //star: eight points
			for (int i = 0; i < 16; i++) {
				float t = Mathf.PI * i / 8f;
				float r = (i % 2 == 0) ? 1f : 0.52f;
				p.Add (new Vector2 (r * Mathf.Cos (t), 0.38f * r * Mathf.Sin (t)));
			}
			break;
		default: //lens: two arcs that meet in a point at both ends
			const int steps = 40;
			for (int i = 0; i <= steps; i++) {
				float x = -1f + 2f * i / steps;
				p.Add (new Vector2 (x, 0.33f * Mathf.Pow (Mathf.Max (0f, 1f - x * x), 0.85f)));
			}
			for (int i = steps - 1; i >= 1; i--) {
				float x = -1f + 2f * i / steps;
				p.Add (new Vector2 (x, -0.33f * Mathf.Pow (Mathf.Max (0f, 1f - x * x), 0.85f)));
			}
			break;
		}
		return p.ToArray ();
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
		float distance = Mathf.Sqrt (best) * MarkTexelsPerUnit;
		return inside ? distance : -distance;
	}

	//distance field per shape, made once
	static float[][] fields = new float[4][];

	static float[] Field (int shape)
	{
		if (fields [shape] == null) {
			Vector2[] outline = ShapeOutline (shape);
			float[] field = new float[MarkWidth * MarkHeight];
			for (int y = 0; y < MarkHeight; y++) {
				for (int x = 0; x < MarkWidth; x++) {
					float ux = (x + 0.5f - MarkWidth / 2f) / MarkTexelsPerUnit;
					float uy = (y + 0.5f - MarkHeight / 2f) / MarkTexelsPerUnit;
					field [y * MarkWidth + x] = Distance (outline, ux, uy);
				}
			}
			fields [shape] = field;
		}
		return fields [shape];
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

	//fill: 0 solid, 1 diagonal lines, 2 crossed lines, 3 outline only
	public static Sprite MarkSprite (int shape, int fill, int color)
	{
		int key = (shape * 4 + fill) * 4 + color;
		Sprite sprite;
		if (!markSprites.TryGetValue (key, out sprite)) {
			float[] field = Field (shape);
			Color32 rgb = colors [color];
			Color32[] pixels = new Color32[MarkWidth * MarkHeight];
			for (int y = 0; y < MarkHeight; y++) {
				for (int x = 0; x < MarkWidth; x++) {
					float d = field [y * MarkWidth + x];
					float cover = Clamp01 (d + 0.5f);
					float ring = cover * Clamp01 (LineWidth - d + 0.5f);
					float alpha;
					if (fill == 0) {
						alpha = cover;
					} else if (fill == 3) {
						alpha = ring;
					} else {
						float lines = Stripe ((x + y) * 0.7071f);
						if (fill == 2) {
							lines = Mathf.Max (lines, Stripe ((x - y) * 0.7071f));
						}
						alpha = Mathf.Max (ring, cover * lines);
					}
					rgb.a = (byte)(alpha * 255f);
					pixels [y * MarkWidth + x] = rgb;
				}
			}
			sprite = MakeSprite (pixels, MarkWidth, MarkHeight, MarkPixelsPerUnit);
			markSprites [key] = sprite;
		}
		return sprite;
	}
}
