namespace BusinessSuite.UI.ViewModels;

/// <summary>Navigation payload for credit/debit note form screens.</summary>
public sealed class AmendmentFormArgs
{
    public int? NoteId { get; init; }
    public int? InvoiceId { get; init; }

    /// <summary>Dashboard navigation target when the form closes (e.g. Sales, CreditNotes).</summary>
    public string? ReturnTo { get; init; }
}
