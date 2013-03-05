﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GrapesAndWrath
{
	public partial class MainWindow : Window
	{
		private DataTable resultsTable;
		private WordBuilder wordBuilder;
		private Dictionary<string, List<WordScore>> wordCache;

		private string[] workLast, workNext;
		private BackgroundWorker andre;
		private List<WordScore> results;

		public MainWindow()
		{
			InitializeComponent();

			// XXX oh god how does datagrid work
			resultsTable = new DataTable();
			resultsTable.Columns.Add("Word");
			resultsTable.Columns.Add("Length", System.Type.GetType("System.UInt16"), "LEN(Word)");
			resultsTable.Columns.Add("Score", System.Type.GetType("System.UInt16"));
			resultsTable.DefaultView.Sort = "Length desc";
			resultGrid.ItemsSource = resultsTable.DefaultView;

			input.KeyUp += eventInput;
			combo.SelectionChanged += eventCombo;
			input.Focus();

			wordBuilder = new WordBuilder();
			andre = new BackgroundWorker();
			andre.WorkerReportsProgress = true;
			andre.ProgressChanged += (sender, e) => progress.Value = e.ProgressPercentage;
			andre.DoWork += (sender, e) => wordBuilder.Initialize(andre);
			andre.RunWorkerCompleted += (sender, e) =>
			{
				progress.Visibility = Visibility.Hidden;
				statusText.Text = "Ready";

				if (workNext != null)
				{
					heresTheGrapesAndHeresTheWrath(workNext[0], workNext[1]);
					workNext = null;
				}
			};
			andre.RunWorkerAsync();

			workLast = new string[2];
			wordCache = new Dictionary<string, List<WordScore>>();
		}

		private void eventInput(object sender, KeyEventArgs e)
		{
			heresTheGrapesAndHeresTheWrath(combo.Text, input.Text);
		}
		private void eventCombo(object sender, SelectionChangedEventArgs e)
		{
			heresTheGrapesAndHeresTheWrath(((ComboBoxItem)e.AddedItems[0]).Content.ToString(), input.Text);
		}
		private void heresTheGrapesAndHeresTheWrath(string wordSource, string letters)
		{
			if (wordSource.Equals(workLast[0]) && letters.Equals(workLast[1]))
			{
				return;
			}
			if (andre.IsBusy)
			{
				if (andre.WorkerSupportsCancellation)
				{
					andre.CancelAsync();
				}
				else
				{
					workNext = new string[2] { wordSource, letters };
					return;
				}
			}
			workLast = new string[2] { wordSource, letters };

			var lettersAsc = String.Join(String.Empty, Regex.Replace(letters.ToUpper(), "[^A-Z]", "_").OrderBy(x => x));

			string cacheKey = wordSource + '.' + lettersAsc;
			if (wordCache.ContainsKey(cacheKey))
			{
				results = wordCache[cacheKey];
				statusText.Text = string.Format("{0:n0}", renderGrid()) + " words found";
			}
			else
			{
				bool showProgress = lettersAsc.Length > 2 && lettersAsc.Substring(lettersAsc.Length - 2, 2).Equals("__");
				if (showProgress)
				{
					progress.Visibility = Visibility.Visible;
					statusText.Text = "Processing \"" + letters + "\"";
				}
				andre = new BackgroundWorker();
				andre.WorkerSupportsCancellation = true;
				andre.DoWork += (sender, e) =>
				{
					results = wordBuilder.GetWords(wordSource, lettersAsc).GroupBy(x => x.Word).Select(g => g.First()).OrderByDescending(x => x.Score).ThenBy(x => x.Word).ToList();
					wordCache[cacheKey] = results;
				};
				andre.RunWorkerCompleted += (sender, e) =>
				{
					statusText.Text = string.Format("{0:n0}", renderGrid()) + " words found";
					if (showProgress)
					{
						progress.Visibility = Visibility.Hidden;
					}
					if (workNext != null)
					{
						heresTheGrapesAndHeresTheWrath(workNext[0], workNext[1]);
						workNext = null;
					}
				};
				andre.RunWorkerAsync();
			}
		}
		private int renderGrid()
		{
			int count = 0;

			resultsTable.Clear();
			foreach (WordScore word in results)
			{
				var row = resultsTable.NewRow();
				row["Word"] = word.Word;
				row["Score"] = word.Score;
				resultsTable.Rows.Add(row);
				count++;
			}

			// XXX ugh.
			resultGrid.Columns[1].Width = DataGridLength.Auto;
			resultGrid.Columns[2].Width = DataGridLength.Auto;

			return count;
		}
	}
}
