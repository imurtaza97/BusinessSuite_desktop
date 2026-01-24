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

public partial class InvoicesViewModel : ViewModelBase
{
    private readonly InvoiceRepository _invoiceRepository;
    private readonly InvoicePdfService _pdfService;
    private readonly int _businessId;

    [ObservableProperty]
    private ObservableCollection<Invoice> _invoices = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintInvoiceCommand))]
    private Invoice? _selectedInvoice;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    private List<Invoice> _allInvoices = new();

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            Invoices = new ObservableCollection<Invoice>(_allInvoices);
        }
        else
        {
            var query = SearchQuery.ToLower();
            var filtered = _allInvoices.Where(i => 
                (i.InvoiceNumber?.ToLower().Contains(query) ?? false) || 
                (i.Customer?.CustomerName?.ToLower().Contains(query) ?? false))
                .ToList();
            Invoices = new ObservableCollection<Invoice>(filtered);
        }
    }

    [ObservableProperty]
    private bool _isBusy;

    public InvoicesViewModel(int businessId)
    {
        var db = new AppDbContext();
        _invoiceRepository = new InvoiceRepository(db);
        _pdfService = new InvoicePdfService();
        _businessId = businessId;
        
        LoadInvoicesCommand = new AsyncRelayCommand(LoadInvoicesAsync);
        AddInvoiceCommand = new AsyncRelayCommand(AddInvoiceAsync);
        EditInvoiceCommand = new AsyncRelayCommand(EditInvoiceAsync);
        DeleteInvoiceCommand = new AsyncRelayCommand(DeleteInvoiceAsync);
        PrintInvoiceCommand = new AsyncRelayCommand(PrintInvoiceAsync);
        RefreshCommand = new AsyncRelayCommand(LoadInvoicesAsync);
    }

    public IAsyncRelayCommand LoadInvoicesCommand { get; }
    public IAsyncRelayCommand AddInvoiceCommand { get; }
    public IAsyncRelayCommand EditInvoiceCommand { get; }
    public IAsyncRelayCommand DeleteInvoiceCommand { get; }
    public IAsyncRelayCommand PrintInvoiceCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public event Action<Invoice?>? RequestInvoiceForm;

    private async Task LoadInvoicesAsync()
    {
        IsBusy = true;
        try
        {
            var invoices = await _invoiceRepository.GetAllAsync(_businessId);
            _allInvoices = invoices.ToList();
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddInvoiceAsync()
    {
        RequestInvoiceForm?.Invoke(null);
        await Task.CompletedTask;
    }

    private async Task EditInvoiceAsync()
    {
        if (SelectedInvoice == null) return;
        
        IsBusy = true;
        try
        {
            var fullInvoice = await _invoiceRepository.GetByIdAsync(SelectedInvoice.InvoiceID);
            if (fullInvoice != null)
            {
                RequestInvoiceForm?.Invoke(fullInvoice);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteInvoiceAsync()
    {
        if (SelectedInvoice == null) return;

        var success = await _invoiceRepository.DeleteAsync(SelectedInvoice.InvoiceID);
        if (success)
        {
            await LoadInvoicesAsync();
        }
    }

    private async Task PrintInvoiceAsync()
    {
        if (SelectedInvoice == null) return;

        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var optionsVm = new InvoiceExportOptionsViewModel();
        var optionsWin = new InvoiceExportOptionsWindow { DataContext = optionsVm };
        var action = await optionsWin.ShowDialog<ExportAction>(desktop.MainWindow!);

        if (action == ExportAction.Cancel) return;

        IsBusy = true;
        try
        {
            var fullInvoice = await _invoiceRepository.GetByIdAsync(SelectedInvoice.InvoiceID);
            if (fullInvoice == null) return;

            string? finalPath = null;
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);

            if (action == ExportAction.Download || action == ExportAction.Both)
            {
                if (topLevel != null)
                {
                    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Save Invoice PDF",
                        SuggestedFileName = $"Invoice_{fullInvoice.InvoiceNumber}.pdf",
                        FileTypeChoices = new[] { new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } } }
                    });

                    if (file == null) return;
                    finalPath = file.Path.LocalPath;
                }
            }
            else
            {
                finalPath = Path.Combine(Path.GetTempPath(), $"Invoice_{fullInvoice.InvoiceNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }

            if (string.IsNullOrEmpty(finalPath)) return;

            await Task.Run(() => _pdfService.GenerateInvoice(fullInvoice, finalPath));

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
