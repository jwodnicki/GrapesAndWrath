using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GrapesAndWrath
{
	public class WordScore
	{
		public string Word;
		public int Score;
	}
	class WordTrie
	{
		public Dictionary<string, int> Words;
		public SortedList<char, WordTrie> Next;
		public WordTrie()
		{
			Words = new Dictionary<string, int>();
			Next = new SortedList<char, WordTrie>();
		}
	}

	class WordPile
	{
		private Dictionary<string, int> wordSourceMap;
		private Dictionary<int, Dictionary<char, int>> scoreMap;
		private SortedList<char, WordTrie> wt0;

		public WordPile()
		{
			wt0 = new SortedList<char, WordTrie>();

			wordSourceMap = new Dictionary<string, int>(){
				{"Zynga"  , 1},
				{"TWL 06" , 2},
				{"SOWPODS", 4}
			};

			var data = new Data();
			scoreMap = new Dictionary<int, Dictionary<char, int>>()
			{
				{1, data.pointsZynga},
				{2, data.pointsScrabble},
				{4, data.pointsScrabble}
			};
		}

		public async Task AddAsync(string wordSource)
		{
			string word;

			Assembly ass = this.GetType().Assembly;
			using (Stream stream = ass.GetManifestResourceStream("GrapesAndWrath.Resources." + wordSource + ".txt"))
			{
				using (TextReader reader = new StreamReader(stream))
				{
					while ((word = reader.ReadLine()) != null)
					{
						Add(wordSourceMap[wordSource], word);
					}
				}
			}
		}

		private void Add(int wordSourceMask, string word)
		{
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
			if (wtPrev.Words.ContainsKey(word))
			{
				wtPrev.Words[word] |= wordSourceMask;
			}
			else
			{
				wtPrev.Words.Add(word, wordSourceMask);
			}
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
					foreach (string word in w.Value.Words.Keys)
					{
						if ((w.Value.Words[word] & wordSourceMask) != 0)
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