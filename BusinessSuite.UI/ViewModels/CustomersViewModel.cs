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

public partial class CustomersViewModel : ViewModelBase
{
    private readonly CustomerRepository _customerRepository;
    private readonly LedgerService _ledgerService;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private decimal _selectedCustomerBalance;
    [ObservableProperty] private ObservableCollection<FinanceLedger> _selectedCustomerTransactions = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCustomerCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCustomerCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddPaymentCommand))]
    private Customer? _selectedCustomer;

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value != null)
        {
            _ = LoadCustomerDetailsAsync(value.CustomerID);
        }
        else
        {
            SelectedCustomerBalance = 0;
            SelectedCustomerTransactions.Clear();
        }
    }

    private async Task LoadCustomerDetailsAsync(int customerId)
    {
        SelectedCustomerBalance = await _ledgerService.GetCustomerBalanceAsync(_businessId, customerId);
        var transactions = await _ledgerService.GetCustomerTransactionsAsync(_businessId, customerId);
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
            SelectedCustomerTransactions = new ObservableCollection<FinanceLedger>(transactions);
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
        _ = LoadCustomersAsync();
    }

    [ObservableProperty]
    private bool _isBusy;

    public CustomersViewModel(int businessId, LedgerService ledgerService)
    {
        var db = new AppDbContext();
        _customerRepository = new CustomerRepository(db);
        _ledgerService = ledgerService;
        _businessId = businessId;
        
        LoadCustomersCommand = new AsyncRelayCommand(LoadCustomersAsync);
        AddCustomerCommand = new AsyncRelayCommand(AddCustomerAsync);
        EditCustomerCommand = new AsyncRelayCommand(EditCustomerAsync, () => SelectedCustomer != null);
        DeleteCustomerCommand = new AsyncRelayCommand(DeleteCustomerAsync, () => SelectedCustomer != null);
        AddPaymentCommand = new AsyncRelayCommand(AddPaymentAsync, () => SelectedCustomer != null);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
    }

    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }

    public IAsyncRelayCommand AddPaymentCommand { get; }

    public IAsyncRelayCommand LoadCustomersCommand { get; }
    public IAsyncRelayCommand AddCustomerCommand { get; }
    public IAsyncRelayCommand EditCustomerCommand { get; }
    public IAsyncRelayCommand DeleteCustomerCommand { get; }

    private async Task LoadCustomersAsync()
    {
        IsBusy = true;
        try
        {
            TotalCount = await _customerRepository.GetCountAsync(_businessId, SearchQuery);
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var customers = await _customerRepository.GetPaginatedAsync(_businessId, CurrentPage, PageSize, SearchQuery);
            Customers = new ObservableCollection<Customer>(customers);

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
            await LoadCustomersAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadCustomersAsync();
        }
    }

    private async Task AddCustomerAsync()
    {
        var vm = new CustomerFormViewModel(_businessId);
        var dialog = new Views.CustomerFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<Customer?>(desktop.MainWindow!);
            if (result != null)
            {
                IsBusy = true;
                try
                {
                    var success = await _customerRepository.AddAsync(result);
                    if (success)
                    {
                        await LoadCustomersAsync();
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    private async Task EditCustomerAsync()
    {
        if (SelectedCustomer == null) return;
        
        var vm = new CustomerFormViewModel(_businessId, SelectedCustomer);
        var dialog = new Views.CustomerFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<Customer?>(desktop.MainWindow!);
            if (result != null)
            {
                result.CustomerID = SelectedCustomer.CustomerID;
                
                IsBusy = true;
                try
                {
                    var success = await _customerRepository.UpdateAsync(result);
                    if (success)
                    {
                        await LoadCustomersAsync();
                        SelectedCustomer = result;
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        bool confirmed = await ShowConfirmDeleteDialog();
        if (!confirmed) return;
        
        IsBusy = true;
        try
        {
            var success = await _customerRepository.DeleteAsync(SelectedCustomer.CustomerID);
            if (success)
            {
                await LoadCustomersAsync();
                SelectedCustomer = null;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddPaymentAsync()
    {
        if (SelectedCustomer == null) return;
        
        var vm = new PaymentFormViewModel(_businessId, "Customer", SelectedCustomer.CustomerID, SelectedCustomer.CustomerName, _ledgerService);
        var dialog = new Views.PaymentFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<bool>(desktop.MainWindow!);
            if (result)
            {
                await LoadCustomerDetailsAsync(SelectedCustomer.CustomerID);
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
