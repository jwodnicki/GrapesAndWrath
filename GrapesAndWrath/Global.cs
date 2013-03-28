using System.Collections.Generic;

namespace GrapesAndWrath
{
	public class Global
	{
		public static Dictionary<char, int> PointsZynga = new Dictionary<char, int>(){
				{'A', 1},
				{'B', 4},
				{'C', 4},
				{'D', 2},
				{'E', 1},
				{'F', 4},
				{'G', 3},
				{'H', 3},
				{'I', 1},
				{'J',10},
				{'K', 5},
				{'L', 2},
				{'M', 4},
				{'N', 2},
				{'O', 1},
				{'P', 4},
				{'Q',10},
				{'R', 1},
				{'S', 1},
				{'T', 1},
				{'U', 2},
				{'V', 5},
				{'W', 4},
				{'X', 8},
				{'Y', 3},
				{'Z',10}
			};

		public static Dictionary<char, int> PointsScrabble = new Dictionary<char, int>(){
				{'A', 1},
				{'B', 3},
				{'C', 3},
				{'D', 2},
				{'E', 1},
				{'F', 4},
				{'G', 2},
				{'H', 4},
				{'I', 1},
				{'J', 8},
				{'K', 5},
				{'L', 1},
				{'M', 3},
				{'N', 1},
				{'O', 1},
				{'P', 3},
				{'Q',10},
				{'R', 1},
				{'S', 1},
				{'T', 1},
				{'U', 1},
				{'V', 4},
				{'W', 4},
				{'X', 8},
				{'Y', 4},
				{'Z',10}
			};

		public static Dictionary<byte, Dictionary<char, int>> ScoreMap = new Dictionary<byte, Dictionary<char, int>>()
			{
				{1 << 0, PointsZynga},
				{1 << 1, PointsScrabble},
				{1 << 2, PointsScrabble}
			};

		public static Dictionary<string, byte> SourceMap = new Dictionary<string, byte>(){
				{"Zynga"  , 1 << 0},
				{"TWL 06" , 1 << 1},
				{"SOWPODS", 1 << 2}
			};

		public static byte SourceMaskCurrent = 1 << 0;
		public static Dictionary<string, byte> WordMask = new Dictionary<string, byte>();
	}
}
