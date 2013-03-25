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
		private Dictionary<string, Results> wordCache;
		private string[] workLast, workNext;
		private BackgroundWorker andre;

		public MainWindow()
		{
			InitializeComponent();
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
			wordCache = new Dictionary<string, Results>();
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
					wordCache[cacheKey] = new Results(
						wordBuilder.GetWords(wordSource, lettersAsc)
						.GroupBy(x => x.Word)
						.Select(g => g.First())
						.OrderByDescending(x => x.Length)
						.ThenByDescending(x => x.Score)
						.ThenBy(x => x.Word)
						.ToList()
						);
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

		private void renderGrid(Results results, string rx)
		{
			var view = CollectionViewSource.GetDefaultView(results);

			bool rxApplied = false;
			try
			{
				Regex rxt = new Regex(rx);
				view.Filter = delegate(object item)
				{
					return rxt.Match(((WordScore)item).Word).Success;
				};
				rxApplied = rx != "";
			}
			catch { }
			if (rxApplied)
			{
				int filteredCount = view.Cast<WordScore>().Count();
				statusText.Text = results.Count != filteredCount ?
					string.Format("{0:n0} / {1:n0}", filteredCount, results.Count + " words found") :
					string.Format("{0:n0}", results.Count + " words found");
			}
			else
			{
				statusText.Text = string.Format("{0:n0}", results.Count + " words found");
			}

			grid.ItemsSource = view;
		}

		private void clearGrid()
		{
			renderGrid(new Results(), "");
		}
	}
}
