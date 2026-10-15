using System.IO;
using UnityEditor;
using UnityEngine;

//writes a picture of generated cards to check how they look:
//Unity -batchmode -quit -executeMethod CardPreview.Dump -previewOutput <file.png>
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
		Texture2D sheet = new Texture2D (columns * cellW, rows * cellH, TextureFormat.RGBA32, false);
		Color[] white = new Color[sheet.width * sheet.height];
		for (int i = 0; i < white.Length; i++) {
			white [i] = new Color (0.1f, 0.15f, 0.25f);
		}
		sheet.SetPixels (white);

		for (int row = 0; row < rows; row++) {
			for (int col = 0; col < columns; col++) {
				int shape = row / 2;
				int fill = (row % 2) * 2 + (col / 8);
				int number = col % 4;
				int color = (col / 4) % 2 + (row % 2) * 2;
				int scale = (col / 2) % 4;
				int index = SetLogic.DimensionToIndex (new int[] { color, shape, number, fill, scale });
				Blit (sheet, DrawCard (index), col * cellW, (rows - 1 - row) * cellH);
			}
		}
		sheet.Apply ();
		File.WriteAllBytes (output, sheet.EncodeToPNG ());
		Debug.Log ("Card preview written to " + output);
		EditorApplication.Exit (0);
	}

	//draw one card with a camera into a texture of 160 x 240
	static Texture2D DrawCard (int cardIndex)
	{
		GameObject cardObject = new GameObject ("Card");
		cardObject.AddComponent<SpriteRenderer> ();
		BoxCollider2D box = cardObject.AddComponent<BoxCollider2D> ();
		CardModel model = cardObject.AddComponent<CardModel> ();
		cardObject.transform.position = Vector3.zero;
		model.cardIndex = cardIndex;
		model.ToggleFace (true);

		GameObject cameraObject = new GameObject ("Camera");
		Camera cam = cameraObject.AddComponent<Camera> ();
		cam.orthographic = true;
		cam.orthographicSize = CardArt.CardSprite.bounds.size.y / 2f + 0.1f;
		cam.transform.position = new Vector3 (0f, 0f, -10f);
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = new Color (0.1f, 0.15f, 0.25f);
		RenderTexture target = RenderTexture.GetTemporary (160, 240, 24, RenderTextureFormat.ARGB32);
		cam.targetTexture = target;
		cam.Render ();

		RenderTexture.active = target;
		Texture2D result = new Texture2D (160, 240, TextureFormat.RGBA32, false);
		result.ReadPixels (new Rect (0, 0, 160, 240), 0, 0);
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
