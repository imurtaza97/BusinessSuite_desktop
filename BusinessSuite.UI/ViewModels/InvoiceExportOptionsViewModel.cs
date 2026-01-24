using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public enum ExportAction
{
    Download,
    Print,
    Both,
    Cancel
}

public partial class InvoiceExportOptionsViewModel : ViewModelBase
{
    [RelayCommand]
    private void ChooseDownload() => RequestClose?.Invoke(ExportAction.Download);

    [RelayCommand]
    private void ChoosePrint() => RequestClose?.Invoke(ExportAction.Print);

    [RelayCommand]
    private void ChooseBoth() => RequestClose?.Invoke(ExportAction.Both);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(ExportAction.Cancel);

    public event Action<ExportAction>? RequestClose;
}
