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
    private readonly LedgerService _ledgerService;
    private readonly int _businessId;

    [ObservableProperty]
    private ObservableCollection<Invoice> _invoices = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditInvoiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteInvoiceCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrintInvoiceCommand))]
    private Invoice? _selectedInvoice;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 25;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadInvoicesAsync();
    }

    [ObservableProperty]
    private bool _isBusy;

    public InvoicesViewModel(int businessId, LedgerService ledgerService)
    {
        var db = new AppDbContext();
        _invoiceRepository = new InvoiceRepository(db);
        _pdfService = new InvoicePdfService();
        _ledgerService = ledgerService;
        _businessId = businessId;
        
        LoadInvoicesCommand = new AsyncRelayCommand(LoadInvoicesAsync);
        AddInvoiceCommand = new AsyncRelayCommand(AddInvoiceAsync);
        EditInvoiceCommand = new AsyncRelayCommand(EditInvoiceAsync, CanModifySelectedInvoice);
        DeleteInvoiceCommand = new AsyncRelayCommand(DeleteInvoiceAsync, CanDeleteSelectedInvoice);
        PrintInvoiceCommand = new AsyncRelayCommand(PrintInvoiceAsync, CanModifySelectedInvoice);
        RefreshCommand = new AsyncRelayCommand(() => { CurrentPage = 1; return LoadInvoicesAsync(); });
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
    }

    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }

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
            TotalCount = await _invoiceRepository.GetCountAsync(_businessId, SearchQuery);
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var invoices = await _invoiceRepository.GetPaginatedAsync(_businessId, CurrentPage, PageSize, SearchQuery);
            Invoices = new ObservableCollection<Invoice>(invoices);
            
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadInvoicesAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadInvoicesAsync();
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

        if (!SelectedInvoice.IsDraft)
        {
            SetStatusMessage("Cannot delete a finalized invoice. Please cancel or return the invoice instead.", "#B45309");
            return;
        }

        ClearStatusMessage();
        IsBusy = true;
        try
        {
            var success = await _invoiceRepository.DeleteAsync(SelectedInvoice.InvoiceID);

            if (success)
            {
                SetStatusMessage("Draft invoice deleted successfully.", "#047857");
                await LoadInvoicesAsync();
            }
            else
            {
                SetStatusMessage("Failed to delete draft invoice. Please try again.", "#B45309");
            }
        }
        catch (Exception ex)
        {
            SetStatusMessage($"Invoice delete failed: {ex.Message}", "#B45309");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDeleteSelectedInvoice() => SelectedInvoice != null && SelectedInvoice.IsDraft;

    private bool CanModifySelectedInvoice() => SelectedInvoice != null;

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
                        SuggestedFileName = $"{GetSafeFileName(fullInvoice.InvoiceNumber)}.pdf",
                        FileTypeChoices = new[] { new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } } }
                    });

                    if (file == null) return;
                    finalPath = file.Path.LocalPath;
                }
            }
            else
            {
                finalPath = Path.Combine(Path.GetTempPath(), $"{GetSafeFileName(fullInvoice.InvoiceNumber)}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
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

    private static string GetSafeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "document";

        var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).Distinct().ToArray();
        var safeName = string.Concat(input.Select(c => invalidChars.Contains(c) ? '_' : c));
        return string.IsNullOrWhiteSpace(safeName) ? "document" : safeName;
    }
}
