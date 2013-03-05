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

		private string[] nextWork;
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

				if (nextWork != null)
				{
					heresTheGrapesAndHeresTheWrath(nextWork[0], nextWork[1]);
					nextWork = null;
				}
			};
			andre.RunWorkerAsync();

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
			if (andre.IsBusy)
			{
				if (andre.WorkerSupportsCancellation)
				{
					andre.CancelAsync();
				}
				else
				{
					nextWork = new string[2] { wordSource, letters };
					return;
				}
			}

			var lettersAsc = String.Join(String.Empty, Regex.Replace(letters.ToUpper(), "[^A-Z]", "_").OrderBy(x => x));

			string cacheKey = wordSource + '.' + lettersAsc;
			if (wordCache.ContainsKey(cacheKey))
			{
				results = wordCache[cacheKey];
				renderGrid();
			}
			else
			{
				progress.Visibility = Visibility.Visible;
				andre = new BackgroundWorker();
				andre.WorkerSupportsCancellation = true;
				andre.DoWork += (sender, e) =>
				{
					results = wordBuilder.GetWords(wordSource, lettersAsc).GroupBy(x => x.Word).Select(g => g.First()).OrderByDescending(x => x.Score).ThenBy(x => x.Word).ToList();
					wordCache[cacheKey] = results;
				};
				andre.RunWorkerCompleted += (sender, e) =>
				{
					progress.Visibility = Visibility.Hidden;
					renderGrid();
					if (nextWork != null)
					{
						heresTheGrapesAndHeresTheWrath(nextWork[0], nextWork[1]);
						nextWork = null;
					}
				};
				andre.RunWorkerAsync();
			}
		}
		private void renderGrid()
		{
			resultsTable.Clear();
			foreach (WordScore word in results)
			{
				var row = resultsTable.NewRow();
				row["Word"] = word.Word;
				row["Score"] = word.Score;
				resultsTable.Rows.Add(row);
			}

			// XXX ugh.
			resultGrid.Columns[1].Width = DataGridLength.Auto;
			resultGrid.Columns[2].Width = DataGridLength.Auto;
		}
	}
}
