using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BusinessSuite.BLL.Services;

namespace BusinessSuite.UI.ViewModels;

public partial class VendorsViewModel : ViewModelBase
{
    private readonly VendorRepository _vendorRepository;
    private readonly LedgerService _ledgerService;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<Vendor> _vendors = new();
    [ObservableProperty] private decimal _selectedVendorBalance;
    [ObservableProperty] private ObservableCollection<FinanceLedger> _selectedVendorTransactions = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditVendorCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteVendorCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddPaymentCommand))]
    private Vendor? _selectedVendor;

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
        _businessId = businessId;
        
        LoadVendorsCommand = new AsyncRelayCommand(LoadVendorsAsync);
        AddVendorCommand = new AsyncRelayCommand(AddVendorAsync);
        EditVendorCommand = new AsyncRelayCommand(EditVendorAsync, () => SelectedVendor != null);
        DeleteVendorCommand = new AsyncRelayCommand(DeleteVendorAsync, () => SelectedVendor != null);
        AddPaymentCommand = new AsyncRelayCommand(AddPaymentAsync, () => SelectedVendor != null);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
    }

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
                        await LoadVendorsAsync();
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
                        await LoadVendorsAsync();
                        SelectedVendor = result;
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
        if (SelectedVendor == null) return;

        bool confirmed = await ShowConfirmDeleteDialog();
        if (!confirmed) return;
        
        IsBusy = true;
        try
        {
            var success = await _vendorRepository.DeleteAsync(SelectedVendor.VendorID);
            if (success)
            {
                await LoadVendorsAsync();
                SelectedVendor = null;
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

    private async Task<bool> ShowConfirmDeleteDialog()
    {
        var dialog = new Views.ConfirmDeleteWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return await dialog.ShowDialog<bool>(desktop.MainWindow!);
        }
        return false;
    }
}
