using System.ComponentModel;
using System.Windows;
using Wpf.Ui.Controls;
using Apex_Clock.ViewModels;

namespace Apex_Clock.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>When false, closing hides the window to the tray instead.</summary>
    public bool AllowClose { get; set; }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
