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

	Vector3[] slotPos = new Vector3[MaxBoardSize]; //where the card of every slot lies on the table
	Vector3 drawPilePos, discardPilePos;

	//the size of a card in world units
	Vector2 CardWorldSize ()
	{
		Vector3 cardScale = cardPrefab.transform.localScale;
		return new Vector2 (CardArt.CardSprite.bounds.size.x * cardScale.x, CardArt.CardSprite.bounds.size.y * cardScale.y);
	}

	//put the cards in a grid that suits the screen and zoom the camera so the grid fits in the space the menu leaves free.
	//The number of columns follows the shape of that space, so a phone held upright gets more rows than columns.
	//Next to the grid (below it on a phone held upright) is room for the draw pile and the discard pile.
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
		float fx = field.x * scale; //the free space in pixels, from the left and from the top
		float fy = field.y * scale;
		float fw = field.width * scale;
		float fh = field.height * scale;
		float cellWidth = Mathf.Abs (cardOffsetx);
		float cellHeight = Mathf.Abs (cardOffsety);
		Vector2 cardSize = CardWorldSize ();

		//the piles need a strip as wide (or high) as a card, and the size of a card depends on the grid: try a few times
		float strip = 0f;
		float usedStrip = 0f;
		float gx = fx, gy = fy, gw = fw, gh = fh;
		int columns = 1, rows = 1;
		float pixelsPerUnit = 1f;
		for (int pass = 0; pass < 3; pass++) {
			usedStrip = strip;
			gx = fx;
			gy = fy;
			gw = fw;
			gh = fh;
			if (Portrait) {
				gh -= usedStrip;
			} else {
				gx += usedStrip;
				gw -= 2f * usedStrip;
			}
			columns = BestColumns (boardSize, gw / cellWidth, gh / cellHeight);
			rows = (boardSize + columns - 1) / columns;

			//zoom so the grid fills the free space, but not far bigger than the original size on a small table
			pixelsPerUnit = Mathf.Min (gw / (columns * cellWidth), gh / (rows * cellHeight));
			pixelsPerUnit = Mathf.Min (pixelsPerUnit, Screen.height / 40f * 1.6f);
			strip = (Portrait ? cardSize.y : cardSize.x) * pixelsPerUnit + 16f;
		}
		cam.orthographicSize = Screen.height / (2f * pixelsPerUnit);

		//move the camera so the middle of the grid lies in the middle of the free space
		float centerX = gx + gw / 2f; //pixels from the left
		float centerY = Screen.height - (gy + gh / 2f); //pixels from the bottom
		cam.transform.position = new Vector3 ((Screen.width / 2f - centerX) / pixelsPerUnit,
			(Screen.height / 2f - centerY) / pixelsPerUnit, cam.transform.position.z);

		//the grid is centered on the world origin; a shorter last row is centered too
		for (int slot = 0; slot < boardSize; slot++) {
			int row = slot / columns;
			int inRow = Mathf.Min (columns, boardSize - row * columns);
			float x = (slot % columns - (inRow - 1) / 2f) * cellWidth;
			float y = ((rows - 1) / 2f - row) * cellHeight;
			slotPos [slot] = new Vector3 (x, y, 0f);
			if (!slots [slot].inFlight) {
				slots [slot].transform.position = slotPos [slot];
			}
		}

		//the piles: on the left and right of the grid, or below it on a phone held upright
		float leftX, rightX, pileY; //pixels from the left and from the bottom
		if (Portrait) {
			leftX = fx + fw * 0.28f;
			rightX = fx + fw * 0.72f;
			pileY = Screen.height - (fy + fh - usedStrip / 2f);
		} else {
			leftX = fx + usedStrip / 2f;
			rightX = fx + fw - usedStrip / 2f;
			pileY = Screen.height - (fy + fh / 2f);
		}
		drawPilePos = new Vector3 ((leftX - centerX) / pixelsPerUnit, (pileY - centerY) / pixelsPerUnit, 0f);
		discardPilePos = new Vector3 ((rightX - centerX) / pixelsPerUnit, (pileY - centerY) / pixelsPerUnit, 0f);
		EnsurePiles ();
		PlacePiles ();
	}

}
