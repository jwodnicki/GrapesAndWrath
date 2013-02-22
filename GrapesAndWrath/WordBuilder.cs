using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrapesAndWrath
{
	public class WordBuilder
	{
		private WordPile wp;
		public async void Initialize()
		{
			wp = new WordPile();

			string[] wordFiles = { "Zynga", "TWL 06", "SOWPODS" };
			foreach (string wordFile in wordFiles)
			{
				await wp.AddAsync(wordFile);
			}
		}

		public async Task<List<WordScore>> GetWordsAsync(string wordSource, string lettersAsc)
		{
			var results = new List<WordScore>();
			if (lettersAsc.Length == 0)
			{
				return results;
			}
			if (lettersAsc.Last() == '_')
			{
				for (int i = 0; i < 26; i++)
				{
					var s = lettersAsc.Substring(0, lettersAsc.Length - 1) + Convert.ToChar((int)'A' + i);
					results.AddRange(await GetWordsAsync(wordSource, String.Join(String.Empty, s.ToArray().OrderBy(x => x))));
				}
			}
			results.AddRange(wp.GetWords(wordSource, lettersAsc));
			return results.GroupBy(x => x.Word).Select(g => g.First()).OrderBy(x => x.Score).ThenBy(x => x.Word).ToList();
		}
	}
}
