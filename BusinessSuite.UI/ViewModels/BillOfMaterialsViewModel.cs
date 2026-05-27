using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using BusinessSuite.UI.Services;
using BusinessSuite.UI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class BillOfMaterialsViewModel : ViewModelBase
{
    private readonly BillOfMaterialsRepository _repository;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<BillOfMaterials> bomLines = new();
    [ObservableProperty] private BillOfMaterials? selectedBomLine;
    [ObservableProperty] private string searchQuery = string.Empty;
    [ObservableProperty] private bool isBusy;

    public BillOfMaterialsViewModel(int businessId)
    {
        _businessId = businessId;
        _repository = new BillOfMaterialsRepository(new AppDbContext());
    }

    partial void OnSearchQueryChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _repository.GetAllAsync(_businessId, SearchQuery);
            BomLines = new ObservableCollection<BillOfMaterials>(list);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        await ShowBomDialogAsync(null);
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (SelectedBomLine == null) return;
        await ShowBomDialogAsync(SelectedBomLine.BOM_ID);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedBomLine == null) return;

        IsBusy = true;
        try
        {
            var success = await _repository.DeactivateAsync(
                SelectedBomLine.BOM_ID, AppState.Instance.GetCurrentUserId());

            if (success)
            {
                SetStatusMessage("BOM line removed.", "#047857");
                await LoadAsync();
            }
            else
            {
                SetStatusMessage("Failed to remove BOM line.", "#B45309");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShowBomDialogAsync(int? bomId)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var vm = new BomFormViewModel(_businessId, bomId);
        var win = new BomFormWindow { DataContext = vm };
        var saved = await win.ShowDialog<bool?>(desktop.MainWindow!);
        if (saved == true)
            await LoadAsync();
    }
}
