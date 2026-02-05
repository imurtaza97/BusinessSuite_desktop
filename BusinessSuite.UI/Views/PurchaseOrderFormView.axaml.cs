using Avalonia.Controls;

namespace BusinessSuite.UI.Views;

public partial class PurchaseOrderFormView : UserControl
{
    public PurchaseOrderFormView()
    {
        InitializeComponent();
    }

    private async void OnAttachBillClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.PurchaseOrderFormViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Attach Vendor Bill",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            { 
                new Avalonia.Platform.Storage.FilePickerFileType("Images and PDFs") 
                { 
                    Patterns = new[] { "*.pdf", "*.jpg", "*.jpeg", "*.png" } 
                } 
            }
        });

        if (files.Count > 0)
        {
            await vm.AttachBillAsync(files[0].Path.LocalPath);
        }
    }

    private void OnClearBillClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.PurchaseOrderFormViewModel vm)
        {
            vm.VendorBillPath = null;
            vm.VendorBillFileName = null;
        }
    }
}
