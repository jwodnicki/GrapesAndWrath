using System;
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

		public MainWindow()
		{
			InitializeComponent();

			resultsTable = new DataTable();
			resultsTable.Columns.Add("Word");
			resultsTable.Columns.Add("Length", System.Type.GetType("System.UInt16"), "LEN(Word)");
			resultsTable.Columns.Add("Score", System.Type.GetType("System.UInt16"));
			resultsTable.DefaultView.Sort = "Length desc";
			resultGrid.ItemsSource = resultsTable.DefaultView;

			inputLetters.KeyUp += eventInput;
			inputRx.KeyUp += eventInput;
			combo.SelectionChanged += eventCombo;
			inputLetters.Focus();

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
					heresTheGrapesAndHeresTheWrath(workNext[0], workNext[1], workNext[2]);
					workNext = null;
				}
			};
			andre.RunWorkerAsync();

			workLast = new string[2];
			wordCache = new Dictionary<string, List<WordScore>>();
		}

		private void eventInput(object sender, KeyEventArgs e)
		{
			heresTheGrapesAndHeresTheWrath(combo.Text, inputLetters.Text, inputRx.Text);
		}
		private void eventCombo(object sender, SelectionChangedEventArgs e)
		{
			heresTheGrapesAndHeresTheWrath(((ComboBoxItem)e.AddedItems[0]).Content.ToString(), inputLetters.Text, inputRx.Text);
		}
		private void heresTheGrapesAndHeresTheWrath(string wordSource, string letters, string rx)
		{
			if (
				wordSource.Equals(workLast[0]) &&
				letters.Equals(workLast[1]) &&
				rx.Equals(workLast[2])
				)
			{
				return;
			}
			if (letters.Length < 2)
			{
				renderGrid(new List<WordScore>(), "");
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
					workNext = new string[3] { wordSource, letters, rx };
					return;
				}
			}
			workLast = new string[3] { wordSource, letters, rx };

			var lettersAsc = String.Join(String.Empty, Regex.Replace(letters.ToUpper(), "[^A-Z]", "_").OrderBy(x => x));

			string cacheKey = wordSource + '.' + lettersAsc;
			if (wordCache.ContainsKey(cacheKey))
			{
				renderGrid(wordCache[cacheKey], rx);
			}
			else
			{
				bool showProgress = lettersAsc.Length > 2 && lettersAsc.Substring(lettersAsc.Length - 2, 2).Equals("__");
				if (showProgress)
				{
					progress.Visibility = Visibility.Visible;
					statusText.Text = "Processing \"" + letters + "\"";
				}
				List<WordScore> results = null;
				andre = new BackgroundWorker();
				andre.WorkerSupportsCancellation = true;
				andre.DoWork += (sender, e) =>
				{
					results = wordBuilder.GetWords(wordSource, lettersAsc).GroupBy(x => x.Word).Select(g => g.First()).OrderByDescending(x => x.Score).ThenBy(x => x.Word).ToList();
					wordCache[cacheKey] = results;
				};
				andre.RunWorkerCompleted += (sender, e) =>
				{
					renderGrid(results, rx);
					if (showProgress)
					{
						progress.Visibility = Visibility.Hidden;
					}
					if (workNext != null)
					{
						heresTheGrapesAndHeresTheWrath(workNext[0], workNext[1], workNext[2]);
						workNext = null;
					}
				};
				andre.RunWorkerAsync();
			}
		}
		private void renderGrid(List<WordScore> results, string rx)
		{
			int count = 0;
			Regex rxt = new Regex(rx);

			resultsTable.Clear();
			foreach (WordScore word in results)
			{
				if (rxt.Match(word.Word).Success)
				{
					var row = resultsTable.NewRow();
					row["Word"] = word.Word;
					row["Score"] = word.Score;
					resultsTable.Rows.Add(row);
					count++;
				}
			}

			statusText.Text = string.Format("{0:n0}", count + " words found");

			// XXX fixplz
			resultGrid.Columns[1].Width = DataGridLength.Auto;
			resultGrid.Columns[2].Width = DataGridLength.Auto;
		}
	}
}
