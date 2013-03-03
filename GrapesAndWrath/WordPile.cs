﻿using System;
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
		public string Word;
		public int Score;
	}
	class WordTrie
	{
		public List<string> Words;
		public SortedList<char, WordTrie> Next;
		public WordTrie()
		{
			Words = new List<string>();
			Next = new SortedList<char, WordTrie>();
		}
	}

	class WordPile
	{
		private Dictionary<string, byte> wordSourceMap;
		private Dictionary<int, Dictionary<char, int>> scoreMap;
		private SortedList<char, WordTrie> wt0;
		private Dictionary<string, byte> wordMask;

		public WordPile(Action<int> reportProgress)
		{
			wt0 = new SortedList<char, WordTrie>();

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
			if (!wt0.ContainsKey(lettersAsc[0]))
			{
				return new List<WordScore>();
			}
			return GetWords(wordSourceMap[wordSource], wt0, lettersAsc);
		}
		private List<WordScore> GetWords(int wordSourceMask, SortedList<char, WordTrie> wt, string lettersAsc)
		{
			var results = new List<WordScore>();

			foreach (KeyValuePair<char, WordTrie> w in wt)
			{
				bool matchFound = false;
				string lettersNext = null;

				for (int i = 0; i < lettersAsc.Length; i++)
				{
					if (lettersAsc[i] < w.Key)
					{
						continue;
					}
					if (lettersAsc[i] > w.Key)
					{
						break;
					}
					lettersNext = lettersAsc.Substring(0, i) + lettersAsc.Substring(i + 1);
					matchFound = true;
					break;
				}
				if (matchFound)
				{
					foreach (string word in w.Value.Words)
					{
						if ((wordMask[word] & wordSourceMask) != 0)
						{
							int score = 0;
							foreach (char c in word)
							{
								score += scoreMap[wordSourceMask][c];
							}
							results.Add(new WordScore() { Word = word, Score = score });
						}
					}
					if (lettersNext != null)
					{
						results.AddRange(GetWords(wordSourceMask, w.Value.Next, lettersNext));
					}
				}
			}

			return results;
		}
	}
}