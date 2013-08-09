using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GrapesAndWrath
{
	public class Word
	{
		public string Value { get; set; }
		public int Length { get { return Value.Length; } }
		public int Score
		{
			get
			{
				var letterCountInWord = new Dictionary<char, int>();
				return Value.Aggregate(0, (sum, i) =>
				{
					if (!Global.LetterCountInRack.ContainsKey(i)) return sum;
					if (!letterCountInWord.ContainsKey(i)) letterCountInWord[i] = 0;
					return sum + (Global.LetterCountInRack[i] > letterCountInWord[i]++ ? Global.ScoreMap[Global.SourceMaskCurrent][i] : 0);
				});
			}
		}
		public Word(string value) { Value = value; }
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
		private Dictionary<char, WordTrie> wt0;

		public WordPile(Action<int> reportProgress)
		{
			wt0 = new Dictionary<char, WordTrie>();

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
		private void Add(string word, byte sourceMask)
		{
			Global.WordMask.Add(word, sourceMask);

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

		public List<Word> GetWords(string lettersAsc)
		{
			return GetWords(wt0, lettersAsc);
		}
		private List<Word> GetWords(Dictionary<char, WordTrie> wt, string lettersAsc)
		{
			var results = new List<Word>();
			for (int i = 0; i < lettersAsc.Length; i++)
			{
				if (wt.ContainsKey(lettersAsc[i]))
				{
					foreach (string word in wt[lettersAsc[i]].Words)
					{
						results.Add(new Word(word));
					}
					results.AddRange(GetWords(wt[lettersAsc[i]].Next, lettersAsc.Substring(0, i) + lettersAsc.Substring(i + 1)));
				}
			}
			return results;
		}
	}
}