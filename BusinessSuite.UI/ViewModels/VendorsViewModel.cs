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

public partial class VendorsViewModel : ViewModelBase
{
    private readonly VendorRepository _vendorRepository;
    private readonly int _businessId;

    [ObservableProperty]
    private ObservableCollection<Vendor> _vendors = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditVendorCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteVendorCommand))]
    private Vendor? _selectedVendor;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    private List<Vendor> _allVendors = new();

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            Vendors = new ObservableCollection<Vendor>(_allVendors);
        }
        else
        {
            var query = SearchQuery.ToLower();
            var filtered = _allVendors.Where(v => 
                (v.VendorName?.ToLower().Contains(query) ?? false) || 
                (v.GSTIN?.ToLower().Contains(query) ?? false))
                .ToList();
            Vendors = new ObservableCollection<Vendor>(filtered);
        }
    }

    [ObservableProperty]
    private bool _isBusy;

    public VendorsViewModel(int businessId)
    {
        var db = new AppDbContext();
        _vendorRepository = new VendorRepository(db);
        _businessId = businessId;
        
        LoadVendorsCommand = new AsyncRelayCommand(LoadVendorsAsync);
        AddVendorCommand = new AsyncRelayCommand(AddVendorAsync);
        EditVendorCommand = new AsyncRelayCommand(EditVendorAsync, () => SelectedVendor != null);
        DeleteVendorCommand = new AsyncRelayCommand(DeleteVendorAsync, () => SelectedVendor != null);
    }

    public IAsyncRelayCommand LoadVendorsCommand { get; }
    public IAsyncRelayCommand AddVendorCommand { get; }
    public IAsyncRelayCommand EditVendorCommand { get; }
    public IAsyncRelayCommand DeleteVendorCommand { get; }

    private async Task LoadVendorsAsync()
    {
        IsBusy = true;
        try
        {
            var vendors = await _vendorRepository.GetAllAsync(_businessId);
            _allVendors = vendors.ToList();
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
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
                        _allVendors.Insert(0, result);
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
                        var masterIndex = _allVendors.FindIndex(v => v.VendorID == result.VendorID);
                        if (masterIndex >= 0) _allVendors[masterIndex] = result;
                        
                        ApplyFilter();
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
                var vendorToRemove = _allVendors.FirstOrDefault(v => v.VendorID == SelectedVendor.VendorID);
                if (vendorToRemove != null) _allVendors.Remove(vendorToRemove);
                
                ApplyFilter();
                SelectedVendor = null;
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
