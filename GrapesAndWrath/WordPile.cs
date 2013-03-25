using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GrapesAndWrath
{
	public class WordScore
	{
		public string Word { get; set; }
		public int Score { get; set; }
		public int Length { get { return Word.Length; } }
	}

	class WordTrie
	{
		public List<string> Words;
		public Dictionary<char, WordTrie> Next;
		public WordTrie()
		{
			Words = new List<string>();
			Next = new Dictionary<char, WordTrie>();
		}
	}

	class WordPile
	{
		private Dictionary<string, byte> wordSourceMap;
		private Dictionary<int, Dictionary<char, int>> scoreMap;
		private Dictionary<char, WordTrie> wt0;
		private Dictionary<string, byte> wordMask;

		public WordPile(Action<int> reportProgress)
		{
			wt0 = new Dictionary<char, WordTrie>();

			wordSourceMap = new Dictionary<string, byte>(){
				{"Zynga"  , 1 << 0},
				{"TWL 06" , 1 << 1},
				{"SOWPODS", 1 << 2}
			};
			wordMask = new Dictionary<string, byte>();

			var data = new Data();
			scoreMap = new Dictionary<int, Dictionary<char, int>>()
			{
				{1 << 0, data.pointsZynga},
				{1 << 1, data.pointsScrabble},
				{1 << 2, data.pointsScrabble}
			};

			string s;
			Assembly ass = this.GetType().Assembly;
			using (Stream gz = ass.GetManifestResourceStream("GrapesAndWrath.Resources.All.gz"))
			{
				using (GZipStream reader = new GZipStream(gz, CompressionMode.Decompress))
				{
					byte[] buf = new byte[4096];
					using (MemoryStream memory = new MemoryStream())
					{
						int count = 0;
						do
						{
							count = reader.Read(buf, 0, 4096);
							if (count > 0)
							{
								memory.Write(buf, 0, count);
							}
						}
						while (count > 0);
						s = Encoding.ASCII.GetString(memory.ToArray());
					}
				}
			}
			int i = 0, j = 10;
			reportProgress(j);
			foreach (string w in s.Split('\n'))
			{
				string[] t = w.Split(',');
				if (t[0].Length > 0)
				{
					Add(t[0], Convert.ToByte(t[1]));
					if ((i++ % 3000) == 0)
					{
						reportProgress(j++);
					}
				}
			}
		}
		private void Add(string word, byte wordSourceMask)
		{
			wordMask.Add(word, wordSourceMask);

			char[] wordAsc = word.OrderBy(c => c).ToArray();
			var wt = wt0;
			WordTrie wtPrev = null;
			foreach (char c in wordAsc)
			{
				if (wt.ContainsKey(c))
				{
					wtPrev = wt[c];
					wt = wt[c].Next;
				}
				else
				{
					wtPrev = new WordTrie();
					wt.Add(c, wtPrev);
					wt = wtPrev.Next;
				}
			}
			wtPrev.Words.Add(word);
		}

		public List<WordScore> GetWords(string wordSource, string lettersAsc)
		{
			return GetWords(wordSourceMap[wordSource], wt0, lettersAsc);
		}
		private List<WordScore> GetWords(int wordSourceMask, Dictionary<char, WordTrie> wt, string lettersAsc)
		{
			var results = new List<WordScore>();
			for (int i = 0; i < lettersAsc.Length; i++)
			{
				if (wt.ContainsKey(lettersAsc[i]))
				{
					foreach (string word in wt[lettersAsc[i]].Words.FindAll(x => (wordMask[x] & wordSourceMask) != 0))
					{
						results.Add(new WordScore() { Word = word, Score = word.Aggregate(0, (sum, c) => sum + scoreMap[wordSourceMask][c]) });
					}
					results.AddRange(GetWords(wordSourceMask, wt[lettersAsc[i]].Next, lettersAsc.Substring(0, i) + lettersAsc.Substring(i + 1)));
				}
			}
			return results;
		}
	}
}