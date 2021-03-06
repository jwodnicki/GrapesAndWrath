﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GrapesAndWrath
{
	public class WordBuilder
	{
		private WordPile wp;
		public void Initialize(BackgroundWorker worker)
		{
			wp = new WordPile(x => worker.ReportProgress(x));
		}

		public List<Word> GetWords(string lettersAsc)
		{
			var results = new List<Word>();
			if (lettersAsc.Length == 0)
			{
				return results;
			}
			if (lettersAsc.Last() == '_')
			{
				for (int i = 0; i < 26; i++)
				{
					var s = lettersAsc.Substring(0, lettersAsc.Length - 1) + Convert.ToChar((int)'A' + i);
					results.AddRange(GetWords(String.Join(String.Empty, s.ToArray().OrderBy(x => x))));
				}
			}
			else
			{
				results.AddRange(wp.GetWords(lettersAsc));
			}
			return results;
		}
	}
}
