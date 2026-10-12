using Wpf.Ui.Controls;
using Apex_Clock.ViewModels;

namespace Apex_Clock.Views;

public partial class SettingsWindow : FluentWindow
{
    public SettingsWindow(SettingsWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
