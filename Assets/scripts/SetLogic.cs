using System.Collections.Generic;

//pure game logic for the set game; no Unity objects involved.
//A set is setSize cards (3 or 4) with setSize values per dimension: in every dimension the cards
//are all the same or all different. dimensionCount (3..5) of the 5 dimensions are used.
public static class SetLogic
{
	public const int Dimensions = 5; //color, shape, number, structure, scale

	//rules per difficulty; index = dimensionCount - 3, then setSize - 3
	//cards on the table: tuned (by simulation) so that on average 1 to 2 sets lie on the table
	static readonly int[,] boardSizes = { { 6, 12 }, { 9, 24 }, { 15, 36 } };
	static readonly int[,] boardColumns = { { 3, 4 }, { 3, 6 }, { 5, 9 } };

	public static int BoardSize (int dimensionCount, int setSize)
	{
		return boardSizes [dimensionCount - 3, setSize - 3];
	}

	public static int BoardColumns (int dimensionCount, int setSize)
	{
		return boardColumns [dimensionCount - 3, setSize - 3];
	}

	//points for one found set: harder with more dimensions (x setSize per extra dimension) and with bigger sets
	public static int PointsPerSet (int dimensionCount, int setSize)
	{
		int points = setSize - 2;
		for (int i = 3; i < dimensionCount; i++) {
			points *= setSize;
		}
		return points;
	}

	//true if the values of one dimension are all the same or all different
	static bool AllSameOrAllDifferent (int[,] d, int dim, int[] slots, int count)
	{
		bool same = true;
		bool different = true;
		for (int i = 1; i < count; i++) {
			for (int j = 0; j < i; j++) {
				if (d [dim, slots [i]] == d [dim, slots [j]]) {
					different = false;
				} else {
					same = false;
				}
			}
		}
		return same || different;
	}

	//true if the cards (card indexes) form a set in every used dimension
	public static bool IsSetCards (int[] cardIndexes, int dimensionCount)
	{
		int count = cardIndexes.Length;
		int[,] d = new int[Dimensions, count];
		int[] slots = new int[count];
		for (int c = 0; c < count; c++) {
			int[] coord = IndexToDimension (cardIndexes [c]);
			for (int dim = 0; dim < Dimensions; dim++) {
				d [dim, c] = coord [dim];
			}
			slots [c] = c;
		}
		for (int dim = 0; dim < dimensionCount; dim++) {
			if (!AllSameOrAllDifferent (d, dim, slots, count)) {
				return false;
			}
		}
		return true;
	}

	//number of cards in a deck: setSize values for each used dimension
	public static int DeckSize (int dimensionCount, int setSize)
	{
		int size = 1;
		for (int i = 0; i < dimensionCount; i++) {
			size *= setSize;
		}
		return size;
	}

	//card index of the n-th card of a deck: n is read as digits in base setSize, one per used dimension
	//(the unused dimensions stay 0, and every value stays below setSize)
	public static int DeckCardIndex (int n, int dimensionCount, int setSize)
	{
		int[] coord = new int[Dimensions];
		for (int dim = 0; dim < dimensionCount; dim++) {
			coord [dim] = n % setSize;
			n /= setSize;
		}
		return DimensionToIndex (coord);
	}

	public static int DimensionToIndex (int[] coord5)
	{
		int temp;
		//scale
		if (coord5 [4] == 0) {
			temp = 0;
		} else if (coord5 [4] == 1) {
			temp = 16;
		} else if (coord5 [4] == 2) {
			temp = 512;
		} else {
			temp = 512 + 16;
		}
		//color + shape + number + structure + scale
		return (coord5 [0] * 4) + (coord5 [1] * 128) + coord5 [2] + (coord5 [3] * 32) + temp;
	}

	public static int[] IndexToDimension (int index)
	{
		int[] temp = new int[Dimensions];

		int color = (index % 16) / 4;
		int shape = (index / 128) % 4;
		int number = index % 4;
		int structure = (index / 32) % 4;
		int scale;

		int scaletemp = (index / 16) % 2;

		if ((scaletemp == 0) && index < 512) {
			scale = 0;
		} else if (scaletemp == 1 && index < 512) {
			scale = 1;
		} else if (scaletemp == 0 && index >= 512) {
			scale = 2;
		} else {
			scale = 3;
		}

		temp [0] = color;
		temp [1] = shape;
		temp [2] = number;
		temp [3] = structure;
		temp [4] = scale;
		return temp;
	}

	//find all sets among the active slots; dimensions is [dimension, slot]; each set is a list of slot numbers
	public static List<int[]> FindSets (int[,] dimensions, bool[] active, int setSize, int dimensionCount)
	{
		List<int[]> found = new List<int[]> ();
		Search (dimensions, active, dimensionCount, new int[setSize], 0, 0, found);
		return found;
	}

	static void Search (int[,] d, bool[] active, int dimensionCount, int[] chosen, int depth, int from, List<int[]> found)
	{
		for (int k = from; k < active.Length; k++) {
			if (!active [k]) continue;
			chosen [depth] = k;
			//the cards chosen so far must already fit: every part of a set is itself all the same or all different
			if (!PrefixFits (d, dimensionCount, chosen, depth + 1)) continue;
			if (depth + 1 == chosen.Length) {
				found.Add ((int[])chosen.Clone ());
			} else {
				Search (d, active, dimensionCount, chosen, depth + 1, k + 1, found);
			}
		}
	}

	static bool PrefixFits (int[,] d, int dimensionCount, int[] chosen, int count)
	{
		for (int dim = 0; dim < dimensionCount; dim++) {
			if (!AllSameOrAllDifferent (d, dim, chosen, count)) {
				return false;
			}
		}
		return true;
	}
}
