using System;
using System.IO;
using System.Linq;
using BusinessSuite.DAL.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BusinessSuite.BLL.Services;

public class InvoicePdfService
{
    public void GenerateInvoice(Invoice invoice, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Verdana));

                // Table structure to mimic the Ramada reference
                page.Content().Border(1).Column(column =>
                {
                    // Title
                    column.Item().AlignCenter().PaddingVertical(5).Text("Tax Invoice").FontSize(11).ExtraBold();

                    // Header Info Section
                    column.Item().BorderTop(1).Row(row =>
                    {
                        row.RelativeItem().Padding(5).Column(c =>
                        {
                            c.Item().Text(x => { x.Span("GSTIN No. : ").SemiBold(); x.Span(invoice.Business?.GSTIN ?? ""); });
                            c.Item().Text(x => { x.Span("Tax is Payable on Reverse Charge? ").SemiBold(); x.Span(invoice.ReverseCharge ? "Yes" : "No"); });
                            c.Item().Text(x => { x.Span("Invoice No.: ").SemiBold(); x.Span(invoice.InvoiceNumber); });
                            c.Item().Text(x => { x.Span("Invoice Dt.: ").SemiBold(); x.Span(invoice.InvoiceDate.ToString("dd/MM/yyyy")); });
                        });

                        row.RelativeItem().BorderLeft(1).Padding(5).Column(c =>
                        {
                            c.Item().Text(x => { x.Span("Place of Supply: ").SemiBold(); x.Span(invoice.PlaceOfSupply ?? ""); });
                            c.Item().Text(x => { x.Span("Payment Terms : ").SemiBold(); x.Span(invoice.PaymentTerms ?? "0"); });
                            c.Item().Text(x => { x.Span("Due Date : ").SemiBold(); x.Span(invoice.DueDate?.ToString("dd/MM/yyyy") ?? invoice.InvoiceDate.AddDays(30).ToString("dd/MM/yyyy")); });
                        });
                    });

                    // Address Section
                    column.Item().BorderTop(1).Row(row =>
                    {
                        row.RelativeItem().Padding(5).Column(c =>
                        {
                            c.Item().Text("Billing Address:").SemiBold().FontSize(10);
                            c.Item().Text(invoice.Customer?.CustomerName ?? "").FontSize(11).SemiBold();
                            c.Item().Text(invoice.Customer?.BillingAddress ?? "");
                            c.Item().Text($"{invoice.Customer?.State}, India");
                            c.Item().Text(x => { x.Span("GSTIN No.: ").SemiBold(); x.Span(invoice.Customer?.GSTIN ?? ""); });
                            if (!string.IsNullOrEmpty(invoice.Customer?.GstTreatment))
                                c.Item().Text(x => { x.Span("GST Treatment: ").SemiBold(); x.Span(invoice.Customer?.GstTreatment); });
                        });

                        row.RelativeItem().BorderLeft(1).Padding(5).Column(c =>
                        {
                            c.Item().Text("Shipping Address:").SemiBold().FontSize(10);
                            c.Item().Text(invoice.Customer?.CustomerName ?? "").FontSize(11).SemiBold();
                            c.Item().Text(invoice.Customer?.ShippingAddress ?? "Same as Billing Address");
                            c.Item().Text($"{invoice.Customer?.State}, India");
                        });
                    });

                    // Main Items Table
                    column.Item().BorderTop(1).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25); // Sr No
                            columns.RelativeColumn(3);  // Description
                            columns.ConstantColumn(60); // HSN
                            columns.ConstantColumn(40); // Qty
                            columns.ConstantColumn(40); // UOM
                            columns.ConstantColumn(60); // Rate
                            columns.ConstantColumn(40); // Disc %
                            columns.ConstantColumn(60); // Disc Amt
                            columns.ConstantColumn(80); // Taxable Value
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Sr.\nNo.");
                            header.Cell().Element(CellStyle).Text("Description of goods");
                            header.Cell().Element(CellStyle).AlignCenter().Text("HSN Code");
                            header.Cell().Element(CellStyle).AlignCenter().Text("Qty");
                            header.Cell().Element(CellStyle).AlignCenter().Text("UOM");
                            header.Cell().Element(CellStyle).AlignRight().Text("Rate");
                            header.Cell().Element(CellStyle).AlignCenter().Text("Disc\n%");
                            header.Cell().Element(CellStyle).AlignRight().Text("Discount");
                            header.Cell().Element(CellStyle).AlignRight().Text("Taxable\nValue");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderLeft(1).Padding(2).AlignCenter().DefaultTextStyle(x => x.SemiBold());
                            }
                        });

                        int sr = 1;
                        foreach (var item in invoice.Items)
                        {
                            decimal taxableValue = (item.Quantity * item.UnitPrice) - item.Discount;
                            table.Cell().Element(ItemCellStyle).Text(sr.ToString());
                            table.Cell().Element(ItemCellStyle).AlignLeft().Text(item.Product?.ProductName ?? "");
                            table.Cell().Element(ItemCellStyle).AlignCenter().Text(item.HSNCode ?? "");
                            table.Cell().Element(ItemCellStyle).AlignCenter().Text(item.Quantity.ToString("N2"));
                            table.Cell().Element(ItemCellStyle).AlignCenter().Text(item.UOM ?? "");
                            table.Cell().Element(ItemCellStyle).AlignRight().Text(item.UnitPrice.ToString("N2"));
                            table.Cell().Element(ItemCellStyle).AlignCenter().Text(item.Discount > 0 ? ((item.Discount / (item.Quantity * item.UnitPrice)) * 100).ToString("N2") : "0");
                            table.Cell().Element(ItemCellStyle).AlignRight().Text(item.Discount.ToString("N2"));
                            table.Cell().Element(ItemCellStyle).AlignRight().Text(taxableValue.ToString("N2"));
                            sr++;

                            static IContainer ItemCellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderLeft(1).Padding(2).DefaultTextStyle(x => x.FontSize(8));
                            }
                        }
                    });

                    // Summary Section
                    column.Item().Row(row =>
                    {
                        // Tax Summary Table (Grouped by HSN)
                        row.RelativeItem(2).Padding(5).Column(c =>
                        {
                            c.Item().Text("Tax Summary :").SemiBold();
                            c.Item().Table(summTable =>
                            {
                                summTable.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(); // HSN
                                    cols.RelativeColumn(); // Taxable
                                    cols.RelativeColumn(2); // CGST
                                    cols.RelativeColumn(2); // SGST
                                    cols.RelativeColumn(2); // IGST
                                });

                                summTable.Header(h =>
                                {
                                    h.Cell().Element(SStyle).Text("HSN");
                                    h.Cell().Element(SStyle).Text("Taxable");
                                    h.Cell().Element(SStyle).Text("CGST");
                                    h.Cell().Element(SStyle).Text("SGST");
                                    h.Cell().Element(SStyle).Text("IGST");
                                    
                                    static IContainer SStyle(IContainer container) => container.BorderBottom(1).Padding(2).DefaultTextStyle(x => x.SemiBold().FontSize(7));
                                });

                                var groups = invoice.Items.GroupBy(i => i.HSNCode ?? "OTHER");
                                foreach (var group in groups)
                                {
                                    decimal taxable = group.Sum(i => (i.Quantity * i.UnitPrice) - i.Discount);
                                    decimal cgst = group.Sum(i => i.CGST_Amount);
                                    decimal sgst = group.Sum(i => i.SGST_Amount);
                                    decimal igst = group.Sum(i => i.IGST_Amount);
                                    decimal rate = group.First().TaxRate;

                                    summTable.Cell().Element(VStyle).Text(group.Key);
                                    summTable.Cell().Element(VStyle).Text(taxable.ToString("N2"));
                                    summTable.Cell().Element(VStyle).Text($"{rate/2}% | {cgst:N2}");
                                    summTable.Cell().Element(VStyle).Text($"{rate/2}% | {sgst:N2}");
                                    summTable.Cell().Element(VStyle).Text($"{rate}% | {igst:N2}");

                                    static IContainer VStyle(IContainer container) => container.Padding(2).DefaultTextStyle(x => x.FontSize(7));
                                }
                            });

                            c.Item().PaddingTop(10).Text(x => {
                                x.Span("RUPEES ").SemiBold();
                                x.Span(NumberToWordsConverter.ConvertToWords(invoice.GrandTotal).ToUpper());
                            });
                        });

                        // Final Totals
                        row.RelativeItem().BorderLeft(1).Padding(5).Column(c =>
                        {
                            c.Item().Row(r => { r.RelativeItem().Text("Sub Total:"); r.RelativeItem().AlignRight().Text(invoice.TotalAmount.ToString("N2")); });
                            if (invoice.TotalCGST > 0)
                            {
                                c.Item().Row(r => { r.RelativeItem().Text("CGST:"); r.RelativeItem().AlignRight().Text(invoice.TotalCGST.ToString("N2")); });
                                c.Item().Row(r => { r.RelativeItem().Text("SGST:"); r.RelativeItem().AlignRight().Text(invoice.TotalSGST.ToString("N2")); });
                            }
                            if (invoice.TotalIGST > 0)
                            {
                                c.Item().Row(r => { r.RelativeItem().Text("IGST:"); r.RelativeItem().AlignRight().Text(invoice.TotalIGST.ToString("N2")); });
                            }

                            c.Item().Row(r => { r.RelativeItem().Text("Discount:"); r.RelativeItem().AlignRight().Text(invoice.Discount.ToString("N2")); });
                            c.Item().Row(r => { r.RelativeItem().Text("Total Tax:"); r.RelativeItem().AlignRight().Text(invoice.TotalTax.ToString("N2")); });
                            c.Item().Row(r => { r.RelativeItem().Text("Shipping:"); r.RelativeItem().AlignRight().Text(invoice.ShippingCharges.ToString("N2")); });
                            c.Item().Row(r => { r.RelativeItem().Text("Round Off:"); r.RelativeItem().AlignRight().Text(invoice.RoundOff.ToString("N2")); });
                            c.Item().BorderTop(1).PaddingTop(5).Row(r => 
                            { 
                                r.RelativeItem().Text("Invoice Total:").ExtraBold(); 
                                r.RelativeItem().AlignRight().Text(invoice.GrandTotal.ToString("N2")).ExtraBold(); 
                            });
                        });
                    });

                    // Bank Details Section
                    column.Item().BorderTop(1).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(x => { x.Span("Bank Details : ").SemiBold(); x.Span(invoice.Business?.BankName ?? ""); });
                            c.Item().Text(x => { x.Span("A/c No.: ").SemiBold(); x.Span(invoice.Business?.AccountNumber ?? ""); });
                            c.Item().Text(x => { x.Span("IFS Code: ").SemiBold(); x.Span(invoice.Business?.IFSC ?? ""); });
                        });
                    });

                    // Terms and Signatory
                    column.Item().BorderTop(1).Row(row =>
                    {
                        row.RelativeItem().Padding(5).Column(c =>
                        {
                            c.Item().Text("TERM AND CONDITION :").SemiBold();
                            c.Item().Text(invoice.TermsAndConditions ?? "(1) Subject to local Jurisdiction.");
                        });

                        row.RelativeItem().BorderLeft(1).Padding(5).Column(c =>
                        {
                            c.Item().AlignCenter().Text($"For, {invoice.Business?.BusinessName ?? "KUTBI TRADERS"}").SemiBold();
                            c.Item().PaddingTop(30).AlignCenter().Text("Authorised Signatory");
                            c.Item().PaddingTop(5).AlignCenter().Text("Certified that the Particulars given above are true and correct").FontSize(7).Italic();
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(filePath);
    }
}
