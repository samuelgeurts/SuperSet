using System.IO;
using UnityEditor;
using UnityEngine;

//writes a picture of generated cards to check how they look, and prints how dark each fill is:
//Unity -batchmode -quit -executeMethod CardPreview.Dump -previewOutput <file.png>   (needs graphics, so no -nographics)
//Next to the sheet it writes <file>_detail.png with a back and a few fronts in full size.
public static class CardPreview
{
	public static void Dump ()
	{
		string output = "CardPreview.png";
		string[] args = System.Environment.GetCommandLineArgs ();
		for (int i = 0; i < args.Length - 1; i++) {
			if (args [i] == "-previewOutput") {
				output = args [i + 1];
			}
		}

		//every shape and fill in one color per shape, with 1 to 4 marks, in the four sizes
		int cellW = 160;
		int cellH = 240;
		int columns = 16;
		int rows = 8;
		Texture2D sheet = NewSheet (columns * cellW, rows * cellH);
		for (int row = 0; row < rows; row++) {
			for (int col = 0; col < columns; col++) {
				int shape = row / 2;
				int fill = (row % 2) * 2 + (col / 8);
				int number = col % 4;
				int color = (col / 4) % 2 + (row % 2) * 2;
				int scale = (col / 2) % 4;
				int index = SetLogic.DimensionToIndex (new int[] { color, shape, number, fill, scale });
				Blit (sheet, DrawCard (index, false, cellW, cellH), col * cellW, (rows - 1 - row) * cellH);
			}
		}
		sheet.Apply ();
		File.WriteAllBytes (output, sheet.EncodeToPNG ());

		//the card back and a few fronts in full size
		int bigW = 320;
		int bigH = 480;
		Texture2D detail = NewSheet (bigW * 5, bigH);
		int[] fronts = {
			SetLogic.DimensionToIndex (new int[] { 0, 0, 2, 0, 0 }),
			SetLogic.DimensionToIndex (new int[] { 1, 2, 3, 1, 0 }),
			SetLogic.DimensionToIndex (new int[] { 2, 1, 1, 2, 0 }),
			SetLogic.DimensionToIndex (new int[] { 3, 3, 2, 3, 0 }),
		};
		Blit (detail, DrawCard (0, true, bigW, bigH), 0, 0);
		for (int i = 0; i < fronts.Length; i++) {
			Blit (detail, DrawCard (fronts [i], false, bigW, bigH), (i + 1) * bigW, 0);
		}
		detail.Apply ();
		File.WriteAllBytes (Path.ChangeExtension (output, null) + "_detail.png", detail.EncodeToPNG ());

		//how dark is each fill compared with solid? Equal steps between outline and solid are the goal
		string report = "Fill darkness (outline, diagonal lines, crossed lines; solid = 1)\n";
		float[] total = new float[3];
		int count = 0;
		for (int shape = 0; shape < 4; shape++) {
			for (int scale = 0; scale < 4; scale++) {
				float solid = Mean (shape, 0, scale);
				float outline = Mean (shape, 3, scale) / solid;
				float hatch = Mean (shape, 1, scale) / solid;
				float crossed = Mean (shape, 2, scale) / solid;
				report += string.Format ("shape {0} scale {1}: {2:0.00} {3:0.00} {4:0.00}\n", shape, scale, outline, hatch, crossed);
				total [0] += outline;
				total [1] += hatch;
				total [2] += crossed;
				count++;
			}
		}
		report += string.Format ("average: outline {0:0.000}, diagonal {1:0.000}, crossed {2:0.000}, solid 1.000; steps {3:0.000} {4:0.000} {5:0.000}\n",
			total [0] / count, total [1] / count, total [2] / count,
			(total [1] - total [0]) / count, (total [2] - total [1]) / count, 1f - total [2] / count);
		Debug.Log (report);
		Debug.Log ("Card preview written to " + output);
		EditorApplication.Exit (0);
	}

	//the average opacity of a mark: how much ink there is
	static float Mean (int shape, int fill, int scale)
	{
		Color32[] pixels = CardArt.MarkSprite (shape, fill, 0, scale).texture.GetPixels32 ();
		double sum = 0;
		foreach (Color32 pixel in pixels) {
			sum += pixel.a;
		}
		return (float)(sum / pixels.Length);
	}

	static Texture2D NewSheet (int width, int height)
	{
		Texture2D sheet = new Texture2D (width, height, TextureFormat.RGBA32, false);
		Color[] fill = new Color[width * height];
		for (int i = 0; i < fill.Length; i++) {
			fill [i] = new Color (0.1f, 0.15f, 0.25f);
		}
		sheet.SetPixels (fill);
		return sheet;
	}

	//draw one card with a camera into a texture
	static Texture2D DrawCard (int cardIndex, bool back, int width, int height)
	{
		GameObject cardObject = new GameObject ("Card");
		cardObject.AddComponent<SpriteRenderer> ();
		cardObject.AddComponent<BoxCollider2D> ();
		CardModel model = cardObject.AddComponent<CardModel> ();
		cardObject.transform.position = Vector3.zero;
		model.cardIndex = cardIndex;
		model.ToggleFace (!back);

		GameObject cameraObject = new GameObject ("Camera");
		Camera cam = cameraObject.AddComponent<Camera> ();
		cam.orthographic = true;
		cam.orthographicSize = CardArt.CardSprite.bounds.size.y / 2f + 0.1f;
		cam.transform.position = new Vector3 (0f, 0f, -10f);
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = new Color (0.1f, 0.15f, 0.25f);
		RenderTexture target = RenderTexture.GetTemporary (width, height, 24, RenderTextureFormat.ARGB32);
		cam.targetTexture = target;
		cam.Render ();

		RenderTexture.active = target;
		Texture2D result = new Texture2D (width, height, TextureFormat.RGBA32, false);
		result.ReadPixels (new Rect (0, 0, width, height), 0, 0);
		result.Apply ();
		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary (target);
		Object.DestroyImmediate (cameraObject);
		Object.DestroyImmediate (cardObject);
		return result;
	}

	static void Blit (Texture2D target, Texture2D source, int x, int y)
	{
		target.SetPixels (x, y, source.width, source.height, source.GetPixels ());
	}
}
