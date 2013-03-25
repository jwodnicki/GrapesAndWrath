using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GrapesAndWrath
{
	public partial class MainWindow : Window
	{
		private WordBuilder wordBuilder;
		private Dictionary<string, List<WordScore>> wordCache;
		private string[] workLast, workNext;
		private BackgroundWorker andre;
		private CollectionViewSource view;

		public MainWindow()
		{
			InitializeComponent();
			inputLetters.Focus();

			// XXX couldn't get the xml-ns to work
			view = (CollectionViewSource)FindResource("view");
			view.SortDescriptions.Add(new SortDescription("Length", ListSortDirection.Descending));
			view.SortDescriptions.Add(new SortDescription("Score", ListSortDirection.Descending));
			view.SortDescriptions.Add(new SortDescription("Word", ListSortDirection.Ascending));

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
			if (inputLetters == null) { return; }
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
				clearGrid();
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
				andre = new BackgroundWorker();
				andre.WorkerSupportsCancellation = true;
				andre.DoWork += (sender, e) =>
				{
					wordCache[cacheKey] = wordBuilder.GetWords(wordSource, lettersAsc).GroupBy(x => x.Word).Select(g => g.First()).ToList();
				};
				andre.RunWorkerCompleted += (sender, e) =>
				{
					renderGrid(wordCache[cacheKey], rx);
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

		private void eventClear(object sender, EventArgs e)
		{
			inputLetters.Text = "";
			inputRx.Text = "";
			clearGrid();
			inputLetters.Focus();
		}

		private void renderGrid(List<WordScore> results, string rx)
		{
			view.Source = results;

			bool rxApplied = false;
			try
			{
				Regex rxt = new Regex(rx);
				view.Filter += (sender, e) => e.Accepted = rxt.Match(((WordScore)e.Item).Word).Success;
				rxApplied = rx != "";
			}
			catch { }
			if (rxApplied)
			{
				int filteredCount = view.View.Cast<WordScore>().Count();
				statusText.Text = results.Count != filteredCount ?
					string.Format("{0:n0} / {1:n0} words found", filteredCount, results.Count) :
					string.Format("{0:n0} words found", results.Count);
			}
			else
			{
				statusText.Text = string.Format("{0:n0} words found", results.Count);
			}
		}

		private void clearGrid()
		{
			renderGrid(new List<WordScore>(), "");
			statusText.Text = "Ready";
		}
	}
}
