using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Mesen.Utilities;
using Mesen.GUI.Config;
using Mesen.GUI;
using Mesen.ViewModels;
using Avalonia.Interactivity;
using Mesen.Windows;

namespace Mesen.Views
{
	public class InputConfigView : UserControl
	{
		public InputConfigView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
