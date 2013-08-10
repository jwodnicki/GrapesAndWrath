using System;
using System.Windows.Input;

namespace GrapesAndWrath
{
	public class DelegateCommand : ICommand
	{
		private Action executeAction;
		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action executeAction)
		{
			this.executeAction = executeAction;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			executeAction();
		}
	}
}
