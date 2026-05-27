using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Entities;

namespace BusinessSuite.UI.ViewModels;

/// <summary>Shared helpers to auto-fill amendment forms from a source sales invoice.</summary>
internal static class AmendmentInvoiceAutoFill
{
    public static void ApplySummary(Invoice? invoice, AmendmentInvoiceSummaryTarget target)
    {
        if (invoice == null)
        {
            target.HasDetails = false;
            target.InvoiceNumber = string.Empty;
            target.InvoiceDate = string.Empty;
            target.CustomerName = string.Empty;
            target.CustomerGstin = string.Empty;
            target.PlaceOfSupply = string.Empty;
            target.InvoiceTotal = string.Empty;
            target.PaymentStatus = string.Empty;
            target.LineItemCount = 0;
            return;
        }

        target.HasDetails = true;
        target.InvoiceNumber = invoice.InvoiceNumber;
        target.InvoiceDate = invoice.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        target.CustomerName = invoice.Customer?.CustomerName ?? "—";
        target.CustomerGstin = string.IsNullOrWhiteSpace(invoice.Customer?.GSTIN) ? "—" : invoice.Customer!.GSTIN!;
        target.PlaceOfSupply = string.IsNullOrWhiteSpace(invoice.PlaceOfSupply) ? "—" : invoice.PlaceOfSupply!;
        target.InvoiceTotal = invoice.GrandTotal.ToString("C2", CultureInfo.GetCultureInfo("en-IN"));
        target.PaymentStatus = invoice.PaymentStatus ?? "—";
        target.LineItemCount = invoice.Items?.Count ?? 0;
    }

    public static void PopulateCreditItems(
        Invoice invoice,
        ObservableCollection<CreditNoteItemViewModel> items,
        ObservableCollection<Product> products,
        IReadOnlyList<string> unitNames,
        IReadOnlyList<decimal> taxRates,
        Business business,
        bool isGstRegistered,
        Action<CreditNoteItemViewModel> attachHandler)
    {
        items.Clear();
        if (invoice.Items == null || !invoice.Items.Any())
            return;

        var customerState = invoice.Customer?.State ?? invoice.PlaceOfSupply;
        foreach (var line in invoice.Items)
        {
            var vm = MapCreditLine(line, products, unitNames, taxRates);
            vm.RecalculateLine(business, customerState, isGstRegistered);
            attachHandler(vm);
            items.Add(vm);
        }
    }

    public static void PopulateDebitItems(
        Invoice invoice,
        ObservableCollection<DebitNoteItemViewModel> items,
        ObservableCollection<Product> products,
        IReadOnlyList<string> unitNames,
        IReadOnlyList<decimal> taxRates,
        Business business,
        bool isGstRegistered,
        Action<DebitNoteItemViewModel> attachHandler)
    {
        items.Clear();
        if (invoice.Items == null || !invoice.Items.Any())
            return;

        var customerState = invoice.Customer?.State ?? invoice.PlaceOfSupply;
        foreach (var line in invoice.Items)
        {
            var vm = MapDebitLine(line, products, unitNames, taxRates);
            vm.RecalculateLine(business, customerState, isGstRegistered);
            attachHandler(vm);
            items.Add(vm);
        }
    }

    private static CreditNoteItemViewModel MapCreditLine(
        InvoiceItem line,
        ObservableCollection<Product> products,
        IReadOnlyList<string> unitNames,
        IReadOnlyList<decimal> taxRates)
    {
        var product = products.FirstOrDefault(p => p.ProductID == line.ProductID) ?? line.Product;
        var vm = new CreditNoteItemViewModel(products, unitNames.ToList(), taxRates.ToList())
        {
            OriginalInvoiceItemId = line.InvoiceItemID,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            HsnCode = line.HSNCode,
            Unit = line.Unit,
            ItemType = line.ItemType,
            TaxRate = line.TaxRate,
            TaxAmount = line.TaxAmount,
            CgstAmount = line.CGST_Amount,
            SgstAmount = line.SGST_Amount,
            IgstAmount = line.IGST_Amount,
            TotalAmount = line.TotalAmount
        };

        if (product != null)
            vm.SelectedProduct = product;

        return vm;
    }

    private static DebitNoteItemViewModel MapDebitLine(
        InvoiceItem line,
        ObservableCollection<Product> products,
        IReadOnlyList<string> unitNames,
        IReadOnlyList<decimal> taxRates)
    {
        var product = products.FirstOrDefault(p => p.ProductID == line.ProductID) ?? line.Product;
        var vm = new DebitNoteItemViewModel(products, unitNames.ToList(), taxRates.ToList())
        {
            OriginalInvoiceItemId = line.InvoiceItemID,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            HsnCode = line.HSNCode,
            Unit = line.Unit,
            ItemType = line.ItemType,
            TaxRate = line.TaxRate,
            TaxAmount = line.TaxAmount,
            CgstAmount = line.CGST_Amount,
            SgstAmount = line.SGST_Amount,
            IgstAmount = line.IGST_Amount,
            TotalAmount = line.TotalAmount
        };

        if (product != null)
            vm.SelectedProduct = product;

        return vm;
    }
}

internal sealed class AmendmentInvoiceSummaryTarget
{
    public bool HasDetails { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceDate { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerGstin { get; set; } = string.Empty;
    public string PlaceOfSupply { get; set; } = string.Empty;
    public string InvoiceTotal { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int LineItemCount { get; set; }
}
