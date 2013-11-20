using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	public class UserInterface : ViewModel
	{
		private string _letters;
		public string Letters
		{
			get { return _letters; }
			set
			{
				_letters = value;
				NotifyPropertyChanged("Letters");

				if (_letters.Equals(workLast)) { return; }
				if (_letters.Length < 2)
				{
					ClearResults();
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
						workNext = _letters;
						return;
					}
				}
				workLast = _letters;

				HeresTheGrapesAndHeresTheWrath(_letters);
			}
		}

		private string _regex;
		public string RegEx
		{
			get { return _regex; }
			set
			{
				_regex = value;
				NotifyPropertyChanged("RegEx");

				Render((List<Word>)View.Source);
			}
		}

		private string _status;
		public string Status
		{
			get { return _status; }
			set
			{
				_status = value;
				NotifyPropertyChanged("Status");
			}
		}

		private bool _focusLetters;
		public bool FocusLetters
		{
			get { return _focusLetters; }
			set
			{
				_focusLetters = value;
				NotifyPropertyChanged("FocusLetters");
			}
		}

		private string _wordSource;
		public string WordSource
		{
			get { return _wordSource; }
			set
			{
				_wordSource = value;
				NotifyPropertyChanged("WordSource");

				if (Letters == null) { return; }
				Global.SourceMaskCurrent = Global.SourceMap[_wordSource];
				HeresTheGrapesAndHeresTheWrath(Letters);
			}
		}
		public ObservableCollection<string> WordSources { get; set; }

		private TaskbarItemProgressState _taskbarProgressState;
		public TaskbarItemProgressState TaskbarProgressState
		{
			get { return _taskbarProgressState; }
			set
			{
				_taskbarProgressState = value;
				NotifyPropertyChanged("TaskbarProgressState");
			}
		}

		private double _taskbarProgressValue;
		public double TaskbarProgressValue
		{
			get { return _taskbarProgressValue; }
			set
			{
				_taskbarProgressValue = value;
				NotifyPropertyChanged("TaskbarProgressValue");
			}
		}

		private int _progressBarValue;
		public int ProgressBarValue
		{
			get { return _progressBarValue; }
			set
			{
				_progressBarValue = value;
				NotifyPropertyChanged("ProgressBarValue");
			}
		}

		private Visibility _progressBarVisibility;
		public Visibility ProgressBarVisibility
		{
			get { return _progressBarVisibility; }
			set
			{
				_progressBarVisibility = value;
				NotifyPropertyChanged("ProgressBarVisibility");
			}
		}

		private CollectionViewSource _view;
		public CollectionViewSource View
		{
			get { return _view; }
			set
			{
				_view = value;
				NotifyPropertyChanged("View");
			}
		}

		public ICommand ClearCommand { get; set; }

		private WordBuilder wordBuilder;
		private Dictionary<string, List<Word>> wordCache;
		private string workLast, workNext;
		private BackgroundWorker andre;

		public UserInterface()
		{
			wordBuilder = new WordBuilder();
			wordCache = new Dictionary<string, List<Word>>();

			ClearCommand = new DelegateCommand(ClearAll);
			View = new CollectionViewSource();

			TaskbarProgressState = TaskbarItemProgressState.Normal;
			WordSources = new ObservableCollection<string>() { "Zynga", "TWL 06", "SOWPODS" };
			WordSource = "Zynga";
			Status = "Loading...";
			FocusLetters = true;

			andre = new BackgroundWorker() { WorkerReportsProgress = true };
			andre.ProgressChanged += (sender, e) =>
			{
				ProgressBarValue = e.ProgressPercentage;
				TaskbarProgressValue = (double)e.ProgressPercentage / 100;
			};
			andre.DoWork += (sender, e) => wordBuilder.Initialize(andre);
			andre.RunWorkerCompleted += (sender, e) =>
			{
				ProgressBarVisibility = Visibility.Hidden;
				TaskbarProgressState = TaskbarItemProgressState.None;
				Status = "Ready";

				if (workNext != null)
				{
					HeresTheGrapesAndHeresTheWrath(workNext);
					workNext = null;
				}
			};
			andre.RunWorkerAsync();
		}

		private void HeresTheGrapesAndHeresTheWrath(string letters)
		{
			var lettersAsc = String.Join(String.Empty, Regex.Replace(letters.ToUpper(), "[^A-Z]", "_").OrderBy(x => x));
			Global.LetterCountInRack = lettersAsc.GroupBy(c => c).ToDictionary(t => t.Key, t => t.Count());

			if (wordCache.ContainsKey(lettersAsc))
			{
				Render(wordCache[lettersAsc]);
			}
			else
			{
				bool showProgress = lettersAsc.Substring(lettersAsc.Length - 2, 2).Equals("__");
				if (showProgress)
				{
					ProgressBarVisibility = Visibility.Visible;
					TaskbarProgressState = TaskbarItemProgressState.Indeterminate;
					Status = "Processing \"" + letters + "\"";
				}
				andre = new BackgroundWorker() { WorkerReportsProgress = true };
				andre.DoWork += (sender, e) =>
				{
					wordCache[lettersAsc] = wordBuilder.GetWords(lettersAsc).GroupBy(x => x.Value).Select(g => g.First()).ToList();
				};
				andre.RunWorkerCompleted += (sender, e) =>
				{
					Render(wordCache[lettersAsc]);
					if (showProgress)
					{
						ProgressBarVisibility = Visibility.Hidden;
						TaskbarProgressState = TaskbarItemProgressState.None;
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

		private void Render(List<Word> results)
		{
			using (View.DeferRefresh())
			{
				View.Source = results = results.FindAll(x => (Global.WordMask[x.Value] & Global.SourceMaskCurrent) != 0);

				bool rxApplied = false;
				try
				{
					Regex rx = new Regex(RegEx);
					View.Filter += (sender, e) => e.Accepted = rx.Match(((Word)e.Item).Value).Success;
					rxApplied = RegEx != "";
				}
				catch { }

				if (rxApplied)
				{
					int filteredCount = View.View.Cast<Word>().Count();
					Status = results.Count != filteredCount ?
						string.Format("{0:n0} / {1:n0} words found", filteredCount, results.Count) :
						string.Format("{0:n0} words found", results.Count);
				}
				else
				{
					Status = string.Format("{0:n0} words found", results.Count);
				}
			}

			// XXX ugh.
			using (View.DeferRefresh())
			{
				View.SortDescriptions.Add(new SortDescription("Length", ListSortDirection.Descending));
				View.SortDescriptions.Add(new SortDescription("Score", ListSortDirection.Descending));
				View.SortDescriptions.Add(new SortDescription("Value", ListSortDirection.Ascending));
			}
		}

		private void ClearResults()
		{
			Render(new List<Word>());
			Status = "Ready";
		}

		public void ClearAll()
		{
			Letters = "";
			RegEx = "";
			ClearResults();
			FocusLetters = false;
			FocusLetters = true;
		}
	}
}
