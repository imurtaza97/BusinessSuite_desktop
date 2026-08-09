using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using BusinessSuite.UI.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class VendorsViewModel : ViewModelBase
{
    private readonly VendorRepository _vendorRepository;
    private readonly LedgerService _ledgerService;
    private readonly EntityDeletionService _deletionService;
    private readonly AuditTrailService _auditService;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<Vendor> _vendors = new();
    [ObservableProperty] private decimal _selectedVendorBalance;
    [ObservableProperty] private ObservableCollection<FinanceLedger> _selectedVendorTransactions = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditVendorCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteVendorCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddPaymentCommand))]
    private Vendor? _selectedVendor;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteVendorCommand))]
    private ObservableCollection<Vendor> _selectedVendors = new();

    partial void OnSelectedVendorChanged(Vendor? value)
    {
        if (value != null)
        {
            _ = LoadVendorDetailsAsync(value.VendorID);
        }
        else
        {
            SelectedVendorBalance = 0;
            SelectedVendorTransactions.Clear();
        }
    }

    private async Task LoadVendorDetailsAsync(int vendorId)
    {
        SelectedVendorBalance = await _ledgerService.GetVendorBalanceAsync(_businessId, vendorId);
        var transactions = await _ledgerService.GetVendorTransactionsAsync(_businessId, vendorId);
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
            SelectedVendorTransactions = new ObservableCollection<FinanceLedger>(transactions);
        });
    }

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
        _ = LoadVendorsAsync();
    }

    [ObservableProperty]
    private bool _isBusy;

    public VendorsViewModel(int businessId, LedgerService ledgerService)
    {
        var db = new AppDbContext();
        _vendorRepository = new VendorRepository(db);
        _ledgerService = ledgerService;
        _deletionService = new EntityDeletionService(new AppDbContextFactory());
        _auditService = new AuditTrailService(new AppDbContext());
        _businessId = businessId;
        
        LoadVendorsCommand = new AsyncRelayCommand(LoadVendorsAsync);
        AddVendorCommand = new AsyncRelayCommand(AddVendorAsync);
        EditVendorCommand = new AsyncRelayCommand(EditVendorAsync, () => SelectedVendor != null);
        DeleteVendorCommand = new AsyncRelayCommand(DeleteVendorAsync);
        AddPaymentCommand = new AsyncRelayCommand(AddPaymentAsync, () => SelectedVendor != null);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
    }

    private bool CanDeleteVendors() => SelectedVendors.Count > 0;

    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }

    public IAsyncRelayCommand AddPaymentCommand { get; }

    public IAsyncRelayCommand LoadVendorsCommand { get; }
    public IAsyncRelayCommand AddVendorCommand { get; }
    public IAsyncRelayCommand EditVendorCommand { get; }
    public IAsyncRelayCommand DeleteVendorCommand { get; }

    private async Task LoadVendorsAsync()
    {
        IsBusy = true;
        try
        {
            TotalCount = await _vendorRepository.GetCountAsync(_businessId, SearchQuery);
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var vendors = await _vendorRepository.GetPaginatedAsync(_businessId, CurrentPage, PageSize, SearchQuery);
            Vendors = new ObservableCollection<Vendor>(vendors);

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
            await LoadVendorsAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadVendorsAsync();
        }
    }

    private async Task AddVendorAsync()
    {
        ClearStatusMessage();
        var vm = new VendorFormViewModel(_businessId);
        var dialog = new Views.VendorFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<Vendor?>(desktop.MainWindow!);
            if (result != null)
            {
                result.BusinessId = _businessId;
                IsBusy = true;
                try
                {
                    var success = await _vendorRepository.AddAsync(result);
                    if (success)
                    {
                        _ = _auditService.LogCreatedAsync(
                            _businessId, "Vendor", result.VendorID,
                            AppState.Instance.GetCurrentUserId(),
                            $"Vendor '{result.VendorName}' created");
                        await LoadVendorsAsync();
                        SetStatusMessage("Vendor added successfully.", "#047857");
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    private async Task EditVendorAsync()
    {
        if (SelectedVendor == null) return;
        ClearStatusMessage();
        
        var vm = new VendorFormViewModel(_businessId, SelectedVendor);
        var dialog = new Views.VendorFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<Vendor?>(desktop.MainWindow!);
            if (result != null)
            {
                result.BusinessId = _businessId;
                result.VendorID = SelectedVendor.VendorID;
                
                IsBusy = true;
                try
                {
                    var success = await _vendorRepository.UpdateAsync(result);
                    if (success)
                    {
                        _ = _auditService.LogFieldModifiedAsync(
                            _businessId, "Vendor", result.VendorID,
                            "All", SelectedVendor?.VendorName, result.VendorName,
                            AppState.Instance.GetCurrentUserId());
                        await LoadVendorsAsync();
                        SelectedVendor = result;
                        SetStatusMessage("Vendor updated successfully.", "#047857");
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    private async Task DeleteVendorAsync()
    {
        var selectedVendors = SelectedVendors.ToList();
        if (selectedVendors.Count == 0 && SelectedVendor != null)
            selectedVendors.Add(SelectedVendor);

        if (selectedVendors.Count == 0)
        {
            SetStatusMessage("Select one or more vendors to delete.", "#B45309");
            return;
        }

        ClearStatusMessage();
        int count = selectedVendors.Count;
        string confirmMsg = count == 1 ? "Are you sure you want to delete this vendor?" : $"Are you sure you want to delete {count} vendors?";
        bool confirmed = await ShowConfirmDeleteDialog(confirmMsg);
        if (!confirmed) return;
        
        IsBusy = true;
        try
        {
            int successCount = 0;
            int failCount = 0;
            string lastError = string.Empty;
            
            foreach (var vendor in selectedVendors)
            {
                var (success, message) = await _deletionService.DeleteVendorAsync(vendor.VendorID);
                if (success)
                {
                    _ = _auditService.LogDeletedAsync(
                        _businessId, "Vendor", vendor.VendorID,
                        AppState.Instance.GetCurrentUserId(),
                        "Vendor deleted by user");
                    successCount++;
                    Vendors.Remove(vendor);
                }
                else
                {
                    failCount++;
                    lastError = message;
                }
            }
            
            SelectedVendor = null;
            
            if (successCount > 0 && failCount == 0)
            {
                SetStatusMessage($"{successCount} vendor(s) deleted successfully.", "#047857");
            }
            else if (successCount > 0 && failCount > 0)
            {
                SetStatusMessage($"{successCount} deleted, {failCount} failed: {lastError}", "#FB923C");
            }
            else
            {
                SetStatusMessage($"Failed to delete: {lastError}", "#B45309");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddPaymentAsync()
    {
        if (SelectedVendor == null) return;
        
        var vm = new PaymentFormViewModel(_businessId, "Vendor", SelectedVendor.VendorID, SelectedVendor.VendorName, _ledgerService);
        var dialog = new Views.PaymentFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<bool>(desktop.MainWindow!);
            if (result)
            {
                await LoadVendorDetailsAsync(SelectedVendor.VendorID);
            }
        }
    }

    private async Task<bool> ShowConfirmDeleteDialog(string message = "Are you sure you want to delete the selected item?")
    {
        var dialog = new Views.ConfirmDeleteWindow(message);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return await dialog.ShowDialog<bool>(desktop.MainWindow!);
        }
        return false;
    }
}
