using System.Collections.Generic;
using UnityEngine;

//the top bar, the drop-down menus, the rule banner, the message and the end screen
public partial class DeckView
{

	static readonly string[] dimensionNames = { "color", "shape", "number", "fill", "size" };

	//the rule of the game, for the difficulty that is chosen now
	string RuleText ()
	{
		string names = "";
		for (int d = 0; d < dimensionCount; d++) {
			names += (d == 0 ? "" : (d == dimensionCount - 1 ? " and " : ", ")) + dimensionNames [d];
		}
		return "Rule: pick " + setSize + " cards where every attribute (" + names +
		") is the same on all cards OR different on all cards. Each set you find gets its own border color; " +
		"new cards only come when all sets on the table are found.";
	}

	//a button of the menu; the chosen option is colored. Returns true when another option was clicked
	bool MenuButton (Rect rect, string text, bool chosen)
	{
		bool clicked = GUI.Button (rect, text, chosen ? UiStyle.ChosenButtonStyle : UiStyle.ButtonStyle);
		return clicked && !chosen;
	}

	//the rule of the game is open at the start; it folds up once the player begins, and the ? button opens it again
	bool ruleOpen = true;

	float RuleHeight {
		get { return Portrait ? 100 : 40; }
	}

	void SetRuleOpen (bool open)
	{
		ruleOpen = open;
		ApplyLayout ();
	}

	//the drop-down menus of the top bar: at most one is open
	enum TopMenu { None, Difficulty, Mode }

	TopMenu openMenu = TopMenu.None;
	int menuClosedFrame = -1;

	void CloseMenu ()
	{
		openMenu = TopMenu.None;
		menuClosedFrame = Time.frameCount;
	}

	//one word for how hard a game is, by the points it gives: more attributes and more cards per set are harder
	static string DifficultyName (int dimensions, int size)
	{
		string[,] names = {
			{ "Easy", "Medium" },
			{ "Hard", "Expert" },
			{ "Master", "Extreme" }
		};
		return names [dimensions - 3, size - 3];
	}

	//a small white square, drawn in a color; used for icons
	static void DrawSquare (Rect rect, Color color)
	{
		Color old = GUI.color;
		GUI.color = color;
		GUI.DrawTexture (rect, Texture2D.whiteTexture);
		GUI.color = old;
	}

	//the top bar: ? and the score on the left, the difficulty in the middle, the game mode on the right
	void DrawTopBar (float width)
	{
		GUI.Box (new Rect (0, 0, width, BarHeight), "", UiStyle.BarStyle);

		//left: the rule and the score
		if (GUI.Button (new Rect (8, 6, HelpButtonSize, ButtonHeight), "?", ruleOpen ? UiStyle.ChosenButtonStyle : UiStyle.ButtonStyle)) {
			SetRuleOpen (!ruleOpen);
		}
		GUI.Label (new Rect (48, 4, 70, 14), "Score", UiStyle.LabelStyle);
		GUI.Label (new Rect (48, 17, 90, 24), score.ToString (), UiStyle.ScoreStyle16);

		//the difficulty and the game mode wait while cards are moving
		GUI.enabled = !busy;

		//middle: the difficulty
		float diffWidth = Portrait ? 124 : 300;
		Rect diffRect = new Rect ((width - diffWidth) / 2, 6, diffWidth, ButtonHeight);
		string diffText = DifficultyName (dimensionCount, setSize) + (Portrait ? "  " + dimensionCount + "·" + setSize : "  ·  " + dimensionCount + " attributes  ·  " + setSize + " cards");
		if (GUI.Button (diffRect, diffText, openMenu == TopMenu.Difficulty ? UiStyle.ChosenButtonStyle : UiStyle.ButtonStyle)) {
			bool wasOpen = openMenu == TopMenu.Difficulty;
			openMenu = wasOpen ? TopMenu.None : TopMenu.Difficulty;
		}
		GUI.DrawTexture (new Rect (diffRect.xMax - 18, diffRect.y + 11, 10, 10), UiStyle.Chevron);

		//right: the game mode; there is room next to it for an account later
		Rect modeRect = new Rect (width - 8 - ButtonHeight, 6, ButtonHeight, ButtonHeight);
		if (GUI.Button (modeRect, "", openMenu == TopMenu.Mode ? UiStyle.ChosenButtonStyle : UiStyle.ButtonStyle)) {
			openMenu = openMenu == TopMenu.Mode ? TopMenu.None : TopMenu.Mode;
		}
		for (int i = 0; i < 4; i++) {
			DrawSquare (new Rect (modeRect.x + 8 + (i % 2) * 9, modeRect.y + 8 + (i / 2) * 9, 7, 7), Color.white);
		}
		GUI.enabled = true;

		//the sound on or off, left of the game mode
		Rect soundRect = new Rect (modeRect.x - 4 - ButtonHeight, 6, ButtonHeight, ButtonHeight);
		if (GUI.Button (soundRect, "", UiStyle.ButtonStyle)) {
			AudioManager.Enabled = !AudioManager.Enabled;
		}
		GUI.DrawTexture (new Rect (soundRect.x + 4, soundRect.y + 4, 24, 24), AudioManager.Enabled ? SoundIcon.On : SoundIcon.Off);
	}

	//the open drop-down menu; a click outside it closes it
	void DrawTopMenu (float width)
	{
		if (openMenu == TopMenu.None) {
			return;
		}
		Rect panel;
		if (openMenu == TopMenu.Difficulty) {
			float w = 230;
			panel = new Rect ((width - w) / 2, BarHeight + 2, w, 252);
		} else {
			float w = 210;
			panel = new Rect (width - 8 - w, BarHeight + 2, w, 188);
		}

		//a click outside the menu and the two buttons closes it
		Event e = Event.current;
		if (e.type == EventType.MouseDown && !panel.Contains (e.mousePosition) && e.mousePosition.y > BarHeight) {
			CloseMenu ();
			return;
		}

		GUI.Box (panel, "", UiStyle.MenuPanelStyle);
		float x = panel.x + 10;
		float y = panel.y + 8;
		float w2 = panel.width - 20;
		if (openMenu == TopMenu.Difficulty) {
			GUI.Label (new Rect (x, y, w2, 18), "Attributes", UiStyle.LabelStyle);
			for (int d = 3; d <= 5; d++) {
				if (MenuButton (new Rect (x, y + 20 + (d - 3) * ButtonStep, w2, ButtonHeight), d + "   +" + SetLogic.PointsPerSet (d, setSize) + " per set", d == dimensionCount)) {
					SetDifficulty (d, setSize);
					CloseMenu ();
					return;
				}
			}
			y += 20 + 3 * ButtonStep + 4;
			GUI.Label (new Rect (x, y, w2, 18), "Cards per set", UiStyle.LabelStyle);
			for (int s = 3; s <= 4; s++) {
				if (MenuButton (new Rect (x, y + 20 + (s - 3) * ButtonStep, w2, ButtonHeight), s + " cards   +" + SetLogic.PointsPerSet (dimensionCount, s), s == setSize)) {
					SetDifficulty (dimensionCount, s);
					CloseMenu ();
					return;
				}
			}
		} else {
			GUI.Label (new Rect (x, y, w2, 18), "Game mode", UiStyle.LabelStyle);
			MenuButton (new Rect (x, y + 20, w2, ButtonHeight), "Classic", true);
			string[] soon = { "Adventure", "Family board", "Online" };
			GUI.enabled = false;
			for (int i = 0; i < soon.Length; i++) {
				GUI.Button (new Rect (x, y + 20 + (i + 1) * ButtonStep, w2, ButtonHeight), soon [i] + "  (soon)", UiStyle.ButtonStyle);
			}
			GUI.enabled = true;
		}
	}

	//the rule of the game in a banner under the top bar, with a button to close it
	void DrawRule (float width)
	{
		if (!ruleOpen) {
			return;
		}
		float y = BarHeight + 4;
		GUI.Box (new Rect (8, y, width - 16 - HelpButtonSize - 6, RuleHeight), RuleText (), UiStyle.BannerStyle);
		if (GUI.Button (new Rect (width - 8 - HelpButtonSize, y, HelpButtonSize, HelpButtonSize), "x", UiStyle.ButtonStyle)) {
			SetRuleOpen (false);
		}
	}

	void DrawMessage (float width)
	{
		if (Time.time < messageUntil) {
			float y = BarHeight + 4 + (ruleOpen ? RuleHeight + 4 : 0);
			GUI.Label (new Rect (8, y, width - 16, MessageHeight), message, UiStyle.MessageCenteredStyle);
		}
	}

	void OnGUI ()
	{
		UiStyle.EnsureStyles ();

		//draw in GUI units; the matrix scales them to the screen (clicks follow the same matrix)
		float scale = UiScale ();
		GUI.matrix = Matrix4x4.Scale (new Vector3 (scale, scale, 1f));
		float width = Screen.width / scale;
		float height = Screen.height / scale;

		DrawRule (width);
		DrawMessage (width);
		DrawTopBar (width);
		DrawPileCounts (scale);
		DrawTopMenu (width);

		if (gameOver && !busy) {
			float bw = Mathf.Min (320f, width - 16f);
			float bh = 170;
			Rect box = new Rect ((width - bw) / 2, (height - bh) / 2, bw, bh);
			GUI.Box (box, "", UiStyle.PanelStyle);
			GUI.Label (new Rect (box.x + 24, box.y + 18, bw - 48, 32), "Deck finished", UiStyle.TitleStyle);
			GUI.Label (new Rect (box.x + 24, box.y + 56, bw - 48, 32), "Score: " + score, UiStyle.ValueStyle);
			if (GUI.Button (new Rect (box.x + 24, box.y + 106, bw - 48, 40), "Continue", UiStyle.ActionButtonStyle)) {
				NewGame ();
			}
		}
	}

}
