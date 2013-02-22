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
			BackgroundWorker worker = new BackgroundWorker();
			worker.WorkerReportsProgress = true;
			worker.ProgressChanged    += (sender, e) => progress.Value = e.ProgressPercentage;
			worker.DoWork             += (sender, e) => wordBuilder.Initialize(worker);
			worker.RunWorkerCompleted += (sender, e) => progress.Visibility = Visibility.Collapsed;
			worker.RunWorkerAsync();

			wordCache = new Dictionary<string, List<WordScore>>();
		}

		private void eventInput(object sender, KeyEventArgs e)
		{
			HeresTheGrapesAndHeresTheWrath(combo.Text);
		}
		private void eventCombo(object sender, SelectionChangedEventArgs e)
		{
			HeresTheGrapesAndHeresTheWrath(((ComboBoxItem)e.AddedItems[0]).Content.ToString());
		}
		private async void HeresTheGrapesAndHeresTheWrath(string wtfWpf)
		{
			List<WordScore> words;
			var text = String.Join(String.Empty, Regex.Replace(input.Text.ToUpper(), "[^A-Z]", "_").OrderBy(x => x));

			string cacheKey = wtfWpf + '.' + text;
			if (wordCache.ContainsKey(cacheKey))
			{
				words = wordCache[cacheKey];
			}
			else
			{
				words = await wordBuilder.GetWordsAsync(wtfWpf, text);
				wordCache[cacheKey] = words;
			}

			resultsTable.Clear();
			foreach (WordScore word in words)
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
