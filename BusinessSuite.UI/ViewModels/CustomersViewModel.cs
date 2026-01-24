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

namespace BusinessSuite.UI.ViewModels;

public partial class CustomersViewModel : ViewModelBase
{
    private readonly CustomerRepository _customerRepository;
    private readonly int _businessId;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCustomerCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCustomerCommand))]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    private List<Customer> _allCustomers = new();

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            Customers = new ObservableCollection<Customer>(_allCustomers);
        }
        else
        {
            var query = SearchQuery.ToLower();
            var filtered = _allCustomers.Where(c => 
                (c.CustomerName?.ToLower().Contains(query) ?? false) || 
                (c.GSTIN?.ToLower().Contains(query) ?? false))
                .ToList();
            Customers = new ObservableCollection<Customer>(filtered);
        }
    }

    [ObservableProperty]
    private bool _isBusy;

    public CustomersViewModel(int businessId)
    {
        var db = new AppDbContext();
        _customerRepository = new CustomerRepository(db);
        _businessId = businessId;
        
        LoadCustomersCommand = new AsyncRelayCommand(LoadCustomersAsync);
        AddCustomerCommand = new AsyncRelayCommand(AddCustomerAsync);
        EditCustomerCommand = new AsyncRelayCommand(EditCustomerAsync, () => SelectedCustomer != null);
        DeleteCustomerCommand = new AsyncRelayCommand(DeleteCustomerAsync, () => SelectedCustomer != null);
    }

    public IAsyncRelayCommand LoadCustomersCommand { get; }
    public IAsyncRelayCommand AddCustomerCommand { get; }
    public IAsyncRelayCommand EditCustomerCommand { get; }
    public IAsyncRelayCommand DeleteCustomerCommand { get; }

    private async Task LoadCustomersAsync()
    {
        IsBusy = true;
        try
        {
            var customers = await _customerRepository.GetAllAsync(_businessId);
            _allCustomers = customers.ToList();
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(ApplyFilter);
        }
        finally
        {
            IsBusy = false;
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
                        _allCustomers.Insert(0, result);
                        ApplyFilter();
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
                        var masterIndex = _allCustomers.FindIndex(c => c.CustomerID == result.CustomerID);
                        if (masterIndex >= 0) _allCustomers[masterIndex] = result;
                        
                        ApplyFilter();
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
                var customerToRemove = _allCustomers.FirstOrDefault(c => c.CustomerID == SelectedCustomer.CustomerID);
                if (customerToRemove != null) _allCustomers.Remove(customerToRemove);
                
                ApplyFilter();
                SelectedCustomer = null;
            }
        }
        finally
        {
            IsBusy = false;
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
