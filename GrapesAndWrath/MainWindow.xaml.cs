using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shell;

namespace GrapesAndWrath
{
	public partial class MainWindow : Window
	{
		private WordBuilder wordBuilder;
		private Dictionary<string, List<Word>> wordCache;
		private string workLast, workNext;
		private BackgroundWorker andre;
		private CollectionViewSource view;
		private List<Word> resultsLast;

		public MainWindow()
		{
			InitializeComponent();
			inputLetters.Focus();

			// XXX couldn't get the xml-ns to work
			view = (CollectionViewSource)FindResource("view");
			view.SortDescriptions.Add(new SortDescription("Length", ListSortDirection.Descending));
			view.SortDescriptions.Add(new SortDescription("Score", ListSortDirection.Descending));
			view.SortDescriptions.Add(new SortDescription("Value", ListSortDirection.Ascending));

			wordBuilder = new WordBuilder();
			wordCache = new Dictionary<string, List<Word>>();

			TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;

			andre = new BackgroundWorker();
			andre.WorkerReportsProgress = true;
			andre.ProgressChanged += (sender, e) =>
			{
				progress.Value = e.ProgressPercentage;
				TaskbarItemInfo.ProgressValue = (double)e.ProgressPercentage / 100;
			};
			andre.DoWork += (sender, e) => wordBuilder.Initialize(andre);
			andre.RunWorkerCompleted += (sender, e) =>
			{
				progress.Visibility = Visibility.Hidden;
				TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
				statusText.Text = "Ready";

				if (workNext != null)
				{
					HeresTheGrapesAndHeresTheWrath(workNext);
					workNext = null;
				}
			};
			andre.RunWorkerAsync();
		}

		private void OnLetterInput(object sender, KeyEventArgs e)
		{
			string letters = inputLetters.Text;

			if (letters.Equals(workLast)) { return; }
			if (letters.Length < 2)
			{
				ClearGrid();
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
					workNext = letters;
					return;
				}
			}
			workLast = letters;

			HeresTheGrapesAndHeresTheWrath(letters);
		}
		private void OnComboChange(object sender, SelectionChangedEventArgs e)
		{
			if (inputLetters == null) { return; }
			Global.SourceMaskCurrent = Global.SourceMap[((ComboBoxItem)e.AddedItems[0]).Content.ToString()];
			HeresTheGrapesAndHeresTheWrath(inputLetters.Text);
		}
		private void OnRxInput(object sender, KeyEventArgs e)
		{
			RenderGrid(resultsLast);
		}

		private void HeresTheGrapesAndHeresTheWrath(string letters)
		{
			var lettersAsc = String.Join(String.Empty, Regex.Replace(letters.ToUpper(), "[^A-Z]", "_").OrderBy(x => x));

			if (wordCache.ContainsKey(lettersAsc))
			{
				RenderGrid(wordCache[lettersAsc]);
			}
			else
			{
				bool showProgress = lettersAsc.Substring(lettersAsc.Length - 2, 2).Equals("__");
				if (showProgress)
				{
					progress.Visibility = Visibility.Visible;
					TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
					statusText.Text = "Processing \"" + letters + "\"";
				}
				andre = new BackgroundWorker();
				andre.WorkerSupportsCancellation = true;
				andre.DoWork += (sender, e) =>
				{
					wordCache[lettersAsc] = wordBuilder.GetWords(lettersAsc).GroupBy(x => x.Value).Select(g => g.First()).ToList();
				};
				andre.RunWorkerCompleted += (sender, e) =>
				{
					RenderGrid(wordCache[lettersAsc]);
					if (showProgress)
					{
						progress.Visibility = Visibility.Hidden;
						TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
					}
					if (workNext != null)
					{
						HeresTheGrapesAndHeresTheWrath(workNext);
						workNext = null;
					}
				};
				andre.RunWorkerAsync();
			}
		}

		private void OnClear(object sender, EventArgs e)
		{
			inputLetters.Text = "";
			inputRx.Text = "";
			ClearGrid();
			inputLetters.Focus();
		}

		private void RenderGrid(List<Word> results)
		{
			view.Source = results = results.FindAll(x => (Global.WordMask[x.Value] & Global.SourceMaskCurrent) != 0);

			bool rxApplied = false;
			try
			{
				Regex rxt = new Regex(inputRx.Text);
				view.Filter += (sender, e) => e.Accepted = rxt.Match(((Word)e.Item).Value).Success;
				rxApplied = inputRx.Text != "";
			}
			catch { }

			if (rxApplied)
			{
				int filteredCount = view.View.Cast<Word>().Count();
				statusText.Text = results.Count != filteredCount ?
					string.Format("{0:n0} / {1:n0} words found", filteredCount, results.Count) :
					string.Format("{0:n0} words found", results.Count);
			}
			else
			{
				statusText.Text = string.Format("{0:n0} words found", results.Count);
			}

			resultsLast = results;
		}

		private void ClearGrid()
		{
			RenderGrid(new List<Word>());
			statusText.Text = "Ready";
		}
	}
}
