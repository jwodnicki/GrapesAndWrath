using System.Windows;
using System.Windows.Data;

namespace GrapesAndWrath
{
	public partial class MainWindow : Window
	{
		private UserInterface ui;

		public MainWindow()
		{
			InitializeComponent();
			ui = new UserInterface((CollectionViewSource)FindResource("xamlView"), TaskbarItemInfo);
			xamlPanel.DataContext = ui;
			ui.Initialize();
		}
	}
}
