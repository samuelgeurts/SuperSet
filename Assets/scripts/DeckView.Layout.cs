using System.Collections.Generic;
using UnityEngine;

//layout of the cards and the camera, and the size of the GUI
public partial class DeckView
{

	//the screen is higher than wide (a phone held upright): the board and the menu are laid out vertically
	static bool Portrait {
		get { return Screen.height > Screen.width; }
	}

	const float BarHeight = 44;
	const float ButtonHeight = 32;
	const float ButtonStep = 36;
	const float HelpButtonSize = 32;
	const float MessageHeight = 22;

	//size of one GUI unit in screen pixels, so the menu keeps its size relative to the screen on a phone
	static float UiScale ()
	{
		float scale = Portrait ? Screen.width / 360f : Mathf.Min (Screen.width / 640f, Screen.height / 360f);
		return Mathf.Clamp (scale, 0.5f, 4f);
	}

	//the part of the screen for the cards, in GUI units (y from the top): what the top bar leaves free
	Rect Playfield (float w, float h)
	{
		float top = BarHeight + 4 + (ruleOpen ? RuleHeight + 4 : 0) + MessageHeight;
		return new Rect (4, top, w - 8, h - top - 4);
	}

	int layoutWidth, layoutHeight;

	//the number of columns that makes the cards biggest; fieldWide and fieldHigh are the free space in card widths and heights.
	//A table with a full last row wins over a tiny gain.
	static int BestColumns (int count, float fieldWide, float fieldHigh)
	{
		int best = 1;
		float bestSize = 0f;
		for (int c = 1; c <= count; c++) {
			int r = (count + c - 1) / c;
			float size = Mathf.Min (fieldWide / c, fieldHigh / r);
			if (count % c == 0) {
				size *= 1.04f;
			}
			if (size > bestSize) {
				bestSize = size;
				best = c;
			}
		}
		return best;
	}

	//put the cards in a grid that suits the screen and zoom the camera so the grid fits in the space the menu leaves free.
	//The number of columns follows the shape of that space, so a phone held upright gets more rows than columns.
	void ApplyLayout ()
	{
		layoutWidth = Screen.width;
		layoutHeight = Screen.height;
		Camera cam = Camera.main;
		if (slots [0] == null || cam == null || Screen.width == 0 || Screen.height == 0) {
			return;
		}

		float scale = UiScale ();
		Rect field = Playfield (Screen.width / scale, Screen.height / scale);
		float cellWidth = Mathf.Abs (cardOffsetx);
		float cellHeight = Mathf.Abs (cardOffsety);
		int columns = BestColumns (boardSize, field.width * scale / cellWidth, field.height * scale / cellHeight);
		int rows = (boardSize + columns - 1) / columns;

		//the grid is centered on the world origin; a shorter last row is centered too
		for (int slot = 0; slot < boardSize; slot++) {
			int row = slot / columns;
			int inRow = Mathf.Min (columns, boardSize - row * columns);
			float x = (slot % columns - (inRow - 1) / 2f) * cellWidth;
			float y = ((rows - 1) / 2f - row) * cellHeight;
			slots [slot].transform.position = new Vector3 (x, y, 0f);
		}

		//zoom so the grid fills the free space, but not far bigger than the original size on a small table
		float pixelsPerUnit = Mathf.Min (field.width * scale / (columns * cellWidth), field.height * scale / (rows * cellHeight));
		pixelsPerUnit = Mathf.Min (pixelsPerUnit, Screen.height / 40f * 1.6f);
		cam.orthographicSize = Screen.height / (2f * pixelsPerUnit);

		//move the camera so the middle of the grid lies in the middle of the free space
		float centerX = (field.x + field.width / 2f) * scale; //pixels from the left
		float centerY = Screen.height - (field.y + field.height / 2f) * scale; //pixels from the bottom
		cam.transform.position = new Vector3 ((Screen.width / 2f - centerX) / pixelsPerUnit,
			(Screen.height / 2f - centerY) / pixelsPerUnit, cam.transform.position.z);
	}

}
