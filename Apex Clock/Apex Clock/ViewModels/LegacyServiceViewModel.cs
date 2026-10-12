using Apex_Clock.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apex_Clock.ViewModels;

/// <summary>Offers to remove the Windows service left by versions before 1.2; hidden when there is none.</summary>
public sealed partial class LegacyServiceViewModel : ObservableObject
{
    private readonly ILegacyServiceRemover _remover;

    public LegacyServiceViewModel(ILegacyServiceRemover remover)
    {
        _remover = remover;
        IsInstalled = remover.IsInstalled();
        Message = "";
    }

    /// <summary>The old service is still on this PC.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisible))]
    public partial bool IsInstalled { get; private set; }

    /// <summary>Outcome of the last removal attempt.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisible))]
    public partial string Message { get; private set; }

    /// <summary>Shown while the service exists, and afterwards to confirm the removal.</summary>
    public bool IsVisible => IsInstalled || Message.Length > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    public partial bool IsBusy { get; private set; }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private async Task RemoveAsync()
    {
        IsBusy = true;
        Message = "Waiting for administrator approval…";
        try
        {
            string? error = await _remover.RemoveAsync();
            IsInstalled = _remover.IsInstalled();
            Message = error ?? "Old service removed. Apex Clock now runs on its own.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRemove() => !IsBusy && IsInstalled;
}
