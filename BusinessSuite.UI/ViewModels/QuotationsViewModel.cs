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
using BusinessSuite.UI.Services;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using BusinessSuite.UI.Views;

namespace BusinessSuite.UI.ViewModels;

public partial class QuotationsViewModel : ViewModelBase
{
    private readonly QuotationRepository _quotationRepository;
    private readonly QuotationPdfService _pdfService;
    private readonly AuditTrailService _auditService;
    private readonly int _businessId;

    [ObservableProperty]
    private ObservableCollection<Quotation> _quotations = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditQuotationCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteQuotationCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrintQuotationCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConvertToInvoiceCommand))]
    private Quotation? _selectedQuotation;

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
        _ = LoadQuotationsAsync();
    }

    [ObservableProperty]
    private bool _isBusy;

    public QuotationsViewModel(int businessId)
    {
        var db = new AppDbContext();
        _quotationRepository = new QuotationRepository(db);
        _pdfService = new QuotationPdfService();
        _auditService = new AuditTrailService(new AppDbContext());
        _businessId = businessId;
        
        LoadQuotationsCommand = new AsyncRelayCommand(LoadQuotationsAsync);
        AddQuotationCommand = new AsyncRelayCommand(AddQuotationAsync);
        EditQuotationCommand = new AsyncRelayCommand(EditQuotationAsync, CanModifySelectedQuotation);
        DeleteQuotationCommand = new AsyncRelayCommand(DeleteQuotationAsync, CanDeleteSelectedQuotation);
        PrintQuotationCommand = new AsyncRelayCommand(PrintQuotationAsync, CanModifySelectedQuotation);
        RefreshCommand = new AsyncRelayCommand(() => { CurrentPage = 1; return LoadQuotationsAsync(); });
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
        ConvertToInvoiceCommand = new AsyncRelayCommand(ConvertToInvoiceAsync, CanConvertToInvoice);
    }

    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }

    public IAsyncRelayCommand LoadQuotationsCommand { get; }
    public IAsyncRelayCommand AddQuotationCommand { get; }
    public IAsyncRelayCommand EditQuotationCommand { get; }
    public IAsyncRelayCommand DeleteQuotationCommand { get; }
    public IAsyncRelayCommand PrintQuotationCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand ConvertToInvoiceCommand { get; }

    public event Action<Quotation?>? RequestQuotationForm;
    public event Action<Invoice?>? RequestInvoiceForm;

    private async Task LoadQuotationsAsync()
    {
        IsBusy = true;
        try
        {
            TotalCount = await _quotationRepository.GetCountAsync(_businessId, SearchQuery);
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var quotations = await _quotationRepository.GetPaginatedAsync(_businessId, CurrentPage, PageSize, SearchQuery);
            Quotations = new ObservableCollection<Quotation>(quotations);
            
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
            await LoadQuotationsAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadQuotationsAsync();
        }
    }

    private async Task AddQuotationAsync()
    {
        RequestQuotationForm?.Invoke(null);
        await Task.CompletedTask;
    }

    private async Task EditQuotationAsync()
    {
        if (SelectedQuotation == null) return;
        
        IsBusy = true;
        try
        {
            var fullQuotation = await _quotationRepository.GetByIdAsync(SelectedQuotation.QuotationID);
            if (fullQuotation != null)
            {
                RequestQuotationForm?.Invoke(fullQuotation);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        if (!SelectedQuotation.IsDraft)
        {
            SetStatusMessage("Cannot delete a finalized quotation.", "#B45309");
            return;
        }

        ClearStatusMessage();
        IsBusy = true;
        try
        {
            var success = await _quotationRepository.DeleteAsync(SelectedQuotation.QuotationID);

            if (success)
            {
                _ = _auditService.LogDeletedAsync(
                    _businessId, "Quotation", SelectedQuotation.QuotationID,
                    AppState.Instance.GetCurrentUserId(),
                    $"Draft quotation {SelectedQuotation.QuotationNumber} deleted");
                SetStatusMessage("Draft quotation deleted successfully.", "#047857");
                await LoadQuotationsAsync();
            }
            else
            {
                SetStatusMessage("Failed to delete draft quotation. Please try again.", "#B45309");
            }
        }
        catch (Exception ex)
        {
            SetStatusMessage($"Quotation delete failed: {ex.Message}", "#B45309");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDeleteSelectedQuotation() => SelectedQuotation != null && SelectedQuotation.IsDraft;

    private bool CanModifySelectedQuotation() => SelectedQuotation != null;

    private bool CanConvertToInvoice() => SelectedQuotation != null;

    private async Task ConvertToInvoiceAsync()
    {
        if (SelectedQuotation == null) return;

        IsBusy = true;
        try
        {
            var fullQuotation = await _quotationRepository.GetByIdAsync(SelectedQuotation.QuotationID);
            if (fullQuotation == null) return;

            // Map Quotation to a prefilled new Invoice
            var prefilledInvoice = new Invoice
            {
                InvoiceID = 0, // indicates new invoice
                BusinessID = fullQuotation.BusinessID,
                CustomerID = fullQuotation.CustomerID,
                IsAutoRoundOff = fullQuotation.IsAutoRoundOff,
                TotalAmount = fullQuotation.TotalAmount,
                TotalTax = fullQuotation.TotalTax,
                Discount = fullQuotation.Discount,
                GrandTotal = fullQuotation.GrandTotal,
                TotalPaid = 0,
                Notes = $"Converted from Quotation {fullQuotation.QuotationNumber}. " + (fullQuotation.Notes ?? ""),
                DeliveryStatus = "Pending",
                PaymentStatus = "Unpaid",
                IsItemLevelDiscount = fullQuotation.IsItemLevelDiscount,
                PaymentMethod = fullQuotation.PaymentMethod,
                PaymentTerms = fullQuotation.PaymentTerms,
                TermsAndConditions = fullQuotation.TermsAndConditions,
                PlaceOfSupply = fullQuotation.PlaceOfSupply,
                ReverseCharge = fullQuotation.ReverseCharge,
                RoundOff = fullQuotation.RoundOff,
                TotalCGST = fullQuotation.TotalCGST,
                TotalSGST = fullQuotation.TotalSGST,
                TotalIGST = fullQuotation.TotalIGST,
                ShippingCharges = fullQuotation.ShippingCharges,
                IsDraft = true,
                Items = fullQuotation.Items.Select(qi => new InvoiceItem
                {
                    InvoiceItemID = 0,
                    ProductID = qi.ProductID,
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    TaxRate = qi.TaxRate,
                    TaxAmount = qi.TaxAmount,
                    HSNCode = qi.HSNCode,
                    Unit = qi.Unit,
                    TotalAmount = qi.TotalAmount,
                    Discount = qi.Discount,
                    CGST_Rate = qi.CGST_Rate,
                    CGST_Amount = qi.CGST_Amount,
                    SGST_Rate = qi.SGST_Rate,
                    SGST_Amount = qi.SGST_Amount,
                    IGST_Rate = qi.IGST_Rate,
                    IGST_Amount = qi.IGST_Amount,
                    ItemType = qi.ItemType
                }).ToList()
            };

            RequestInvoiceForm?.Invoke(prefilledInvoice);
        }
        catch (Exception ex)
        {
            SetStatusMessage($"Failed to convert quotation: {ex.Message}", "#B45309");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PrintQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        // Reuse InvoiceExportOptionsViewModel/Window as they are identical actions (Download/Print/Cancel)
        var optionsVm = new InvoiceExportOptionsViewModel();
        var optionsWin = new InvoiceExportOptionsWindow { DataContext = optionsVm };
        var action = await optionsWin.ShowDialog<ExportAction>(desktop.MainWindow!);

        if (action == ExportAction.Cancel) return;

        IsBusy = true;
        try
        {
            var fullQuotation = await _quotationRepository.GetByIdAsync(SelectedQuotation.QuotationID);
            if (fullQuotation == null) return;

            string? finalPath = null;
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);

            if (action == ExportAction.Download || action == ExportAction.Both)
            {
                if (topLevel != null)
                {
                    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Save Quotation PDF",
                        SuggestedFileName = $"{GetSafeFileName(fullQuotation.QuotationNumber)}.pdf",
                        FileTypeChoices = new[] { new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } } }
                    });

                    if (file == null) return;
                    finalPath = file.Path.LocalPath;
                }
            }
            else
            {
                finalPath = Path.Combine(Path.GetTempPath(), $"{GetSafeFileName(fullQuotation.QuotationNumber)}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }

            if (string.IsNullOrEmpty(finalPath)) return;

            await Task.Run(() => _pdfService.GenerateQuotation(fullQuotation, finalPath));

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
