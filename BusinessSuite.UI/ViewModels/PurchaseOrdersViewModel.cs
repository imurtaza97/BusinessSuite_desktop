using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using BusinessSuite.UI.Views;

namespace BusinessSuite.UI.ViewModels;

public partial class PurchaseOrdersViewModel : ViewModelBase
{
    private readonly PurchaseOrderRepository _poRepository;
    private readonly PurchaseOrderPdfService _pdfService;
    private readonly int _businessId;

    [ObservableProperty]
    private ObservableCollection<PurchaseOrder> _purchaseOrders = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintPOCommand))]
    private PurchaseOrder? _selectedPO;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    private List<PurchaseOrder> _allPOs = new();

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            PurchaseOrders = new ObservableCollection<PurchaseOrder>(_allPOs);
        }
        else
        {
            var query = SearchQuery.ToLower();
            var filtered = _allPOs.Where(i => 
                (i.PONumber?.ToLower().Contains(query) ?? false) || 
                (i.Vendor?.VendorName?.ToLower().Contains(query) ?? false))
                .ToList();
            PurchaseOrders = new ObservableCollection<PurchaseOrder>(filtered);
        }
    }

    [ObservableProperty]
    private bool _isBusy;

    public PurchaseOrdersViewModel(int businessId)
    {
        var db = new AppDbContext();
        _poRepository = new PurchaseOrderRepository(db);
        _pdfService = new PurchaseOrderPdfService();
        _businessId = businessId;
        
        LoadPOsCommand = new AsyncRelayCommand(LoadPOsAsync);
        AddPOCommand = new AsyncRelayCommand(AddPOAsync);
        EditPOCommand = new AsyncRelayCommand(EditPOAsync);
        DeletePOCommand = new AsyncRelayCommand(DeletePOAsync);
        PrintPOCommand = new AsyncRelayCommand(PrintPOAsync);
        RefreshCommand = new AsyncRelayCommand(LoadPOsAsync);
    }

    public IAsyncRelayCommand LoadPOsCommand { get; }
    public IAsyncRelayCommand AddPOCommand { get; }
    public IAsyncRelayCommand EditPOCommand { get; }
    public IAsyncRelayCommand DeletePOCommand { get; }
    public IAsyncRelayCommand PrintPOCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public event Action<PurchaseOrder?>? RequestPOForm;

    private async Task LoadPOsAsync()
    {
        IsBusy = true;
        try
        {
            var pos = await _poRepository.GetAllAsync(_businessId);
            _allPOs = pos.ToList();
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddPOAsync()
    {
        RequestPOForm?.Invoke(null);
        await Task.CompletedTask;
    }

    private async Task EditPOAsync()
    {
        if (SelectedPO == null) return;
        
        IsBusy = true;
        try
        {
            var fullPO = await _poRepository.GetByIdAsync(SelectedPO.PurchaseOrderID);
            if (fullPO != null)
            {
                RequestPOForm?.Invoke(fullPO);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeletePOAsync()
    {
        if (SelectedPO == null) return;

        var success = await _poRepository.DeleteAsync(SelectedPO.PurchaseOrderID);
        if (success)
        {
            await LoadPOsAsync();
        }
    }

    private async Task PrintPOAsync()
    {
        if (SelectedPO == null) return;

        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var optionsVm = new InvoiceExportOptionsViewModel();
        var optionsWin = new InvoiceExportOptionsWindow { DataContext = optionsVm };
        var action = await optionsWin.ShowDialog<ExportAction>(desktop.MainWindow!);

        if (action == ExportAction.Cancel) return;

        IsBusy = true;
        try
        {
            var fullPO = await _poRepository.GetByIdAsync(SelectedPO.PurchaseOrderID);
            if (fullPO == null) return;

            string? finalPath = null;
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);

            if (action == ExportAction.Download || action == ExportAction.Both)
            {
                if (topLevel != null)
                {
                    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Save Purchase Order PDF",
                        SuggestedFileName = $"{fullPO.PONumber}.pdf",
                        FileTypeChoices = new[] { new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } } }
                    });

                    if (file == null) return;
                    finalPath = file.Path.LocalPath;
                }
            }
            else
            {
                finalPath = Path.Combine(Path.GetTempPath(), $"{fullPO.PONumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }

            if (string.IsNullOrEmpty(finalPath)) return;

            await Task.Run(() => _pdfService.GeneratePO(fullPO, finalPath));

            if (action == ExportAction.Print || action == ExportAction.Both)
            {
                Process.Start(new ProcessStartInfo(finalPath) { UseShellExecute = true });
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
