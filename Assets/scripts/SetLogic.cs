//pure game logic for the 5-dimensional set game; no Unity objects involved
public static class SetLogic
{
	public const int Dimensions = 5; //color, shape, number, structure, scale
	public const int SetSize = 4; //cards needed for one set

	// return true if all three values are different or the same
	public static bool IsSet (int a1, int a2, int a3)
	{
		return ((a1 == a2 && a1 == a3) || (a1 != a2 && a1 != a3 && a2 != a3));
	}

	// return true if all four values are different or the same
	public static bool IsSet4 (int a1, int a2, int a3, int a4)
	{
		return ((a1 == a2 && a1 == a3 && a1 == a4) ||
		(a4 != a1 && a4 != a2 && a4 != a3 && a1 != a2 && a1 != a3 && a2 != a3));
	}

	//true if the four cards (card indexes) form a set in every dimension
	public static bool IsSet4Cards (int[] cardIndexes)
	{
		int[][] d = new int[SetSize][];
		for (int c = 0; c < SetSize; c++) {
			d [c] = IndexToDimension (cardIndexes [c]);
		}
		for (int dim = 0; dim < Dimensions; dim++) {
			if (!IsSet4 (d [0] [dim], d [1] [dim], d [2] [dim], d [3] [dim])) {
				return false;
			}
		}
		return true;
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

	//count the sets of four among the active slots; dimensions is [dimension, slot]
	public static int CountSets (int[,] dimensions, bool[] active)
	{
		int slots = active.Length;
		int count = 0;

		for (int k1 = 0; k1 < slots; k1++) {
			if (!active [k1]) continue;
			for (int k2 = k1 + 1; k2 < slots; k2++) {
				if (!active [k2]) continue;
				for (int k3 = k2 + 1; k3 < slots; k3++) {
					if (!active [k3]) continue;
					//the first three cards must already fit as a set of three
					if (!AllDimensions3 (dimensions, k1, k2, k3)) continue;
					for (int k4 = k3 + 1; k4 < slots; k4++) {
						if (!active [k4]) continue;
						if (AllDimensions4 (dimensions, k1, k2, k3, k4)) {
							count++;
						}
					}
				}
			}
		}
		return count;
	}

	static bool AllDimensions3 (int[,] d, int k1, int k2, int k3)
	{
		for (int dim = 0; dim < Dimensions; dim++) {
			if (!IsSet (d [dim, k1], d [dim, k2], d [dim, k3])) {
				return false;
			}
		}
		return true;
	}

	static bool AllDimensions4 (int[,] d, int k1, int k2, int k3, int k4)
	{
		for (int dim = 0; dim < Dimensions; dim++) {
			if (!IsSet4 (d [dim, k1], d [dim, k2], d [dim, k3], d [dim, k4])) {
				return false;
			}
		}
		return true;
	}
}
