using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using BusinessSuite.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class DebitNotesViewModel : ViewModelBase
{
    private readonly DebitNoteRepository _repository;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<DebitNote> debitNotes = new();
    [ObservableProperty] private DebitNote? selectedDebitNote;
    [ObservableProperty] private string searchQuery = string.Empty;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int totalPages;
    [ObservableProperty] private bool isBusy;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public event Action<AmendmentFormArgs?>? RequestDebitNoteForm;

    public DebitNotesViewModel(int businessId)
    {
        _businessId = businessId;
        var db = new AppDbContext();
        _repository = new DebitNoteRepository(db);
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            TotalCount = await _repository.GetCountAsync(_businessId, SearchQuery);
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var list = await _repository.GetPaginatedAsync(_businessId, CurrentPage, PageSize, SearchQuery);
            DebitNotes = new ObservableCollection<DebitNote>(list);
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (!HasPreviousPage) return;
        CurrentPage--;
        await LoadAsync();
    }

    [RelayCommand]
    private void AddDebitNote()
    {
        RequestDebitNoteForm?.Invoke(null);
    }

    [RelayCommand]
    private void EditDebitNote()
    {
        if (SelectedDebitNote == null) return;
        RequestDebitNoteForm?.Invoke(new AmendmentFormArgs { NoteId = SelectedDebitNote.DebitNoteID });
    }

    [RelayCommand]
    private async Task DeleteDebitNoteAsync()
    {
        if (SelectedDebitNote == null) return;

        if (!SelectedDebitNote.IsDraft)
        {
            SetStatusMessage("Only draft debit notes can be deleted.", "#B45309");
            return;
        }

        IsBusy = true;
        try
        {
            var success = await _repository.SoftDeleteAsync(
                SelectedDebitNote.DebitNoteID, "Deleted from list", AppState.Instance.GetCurrentUserId());

            if (success)
            {
                SetStatusMessage("Debit note deleted.", "#047857");
                await LoadAsync();
            }
            else
            {
                SetStatusMessage("Failed to delete debit note.", "#B45309");
            }
        }
        catch (Exception ex)
        {
            SetStatusMessage($"Delete failed: {ex.Message}", "#B45309");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
