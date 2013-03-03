using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace GrapesAndWrath
{
	public class WordBuilder
	{
		private WordPile wp;
		public void Initialize(BackgroundWorker worker)
		{
			wp = new WordPile(x => worker.ReportProgress(x));
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
