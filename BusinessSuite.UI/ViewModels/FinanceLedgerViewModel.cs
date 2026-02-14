using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.UI.ViewModels;

public partial class FinanceLedgerViewModel : ViewModelBase
{
    private readonly LedgerService _ledgerService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<FinanceLedger> _transactions = new();
    [ObservableProperty] private decimal _totalCredit;
    [ObservableProperty] private decimal _totalDebit;
    [ObservableProperty] private decimal _netBalance;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 25;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private bool _isBusy;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public FinanceLedgerViewModel(IDbContextFactory<AppDbContext> dbFactory, int businessId)
    {
        _dbFactory = dbFactory;
        _ledgerService = new LedgerService(dbFactory);
        _businessId = businessId;

        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
    }

    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.FinanceLedgers.Where(l => l.BusinessId == _businessId);
            
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var txs = await query
                .OrderByDescending(l => l.TransactionDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var creditsList = await query.Where(t => t.Type == "Credit").Select(t => t.Amount).ToListAsync();
            var debitsList = await query.Where(t => t.Type == "Debit").Select(t => t.Amount).ToListAsync();
            
            decimal totalCredit = creditsList.Sum();
            decimal totalDebit = debitsList.Sum();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                Transactions = new ObservableCollection<FinanceLedger>(txs);
                TotalCredit = totalCredit;
                TotalDebit = totalDebit;
                NetBalance = totalDebit - totalCredit;
                
                OnPropertyChanged(nameof(HasPreviousPage));
                OnPropertyChanged(nameof(HasNextPage));
            });
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
            await LoadDataAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadDataAsync();
        }
    }
}
