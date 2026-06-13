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

        bool isGstRegistered = invoice.Business?.IsGSTRegistered == true;
        bool isComposition = invoice.Business?.GstType == BusinessGstType.Composition;
        bool isInterState = isGstRegistered && !isComposition && invoice.TotalIGST > 0;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Verdana));

                page.Content().Column(column =>
                {
                    // --- CENTERED HEADING ---
                    string title = !isGstRegistered ? "INVOICE" : (isComposition ? "BILL OF SUPPLY" : "TAX INVOICE");
                    column.Item().AlignCenter().Text(title).FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);

                    if (isComposition)
                    {
                        column.Item().AlignCenter().Text("Composition taxable person, not eligible to collect tax on supplies").FontSize(7).Italic();
                    }

                    // --- FIRM NAME & HEADER SECTION ---
                    column.Item().Border(0.5f).Row(row =>
                    {
                        // Left Side: Business Branding
                        row.RelativeItem().BorderRight(0.5f).Padding(10).Column(c =>
                        {
                            c.Item().Text(invoice.Business?.BusinessName?.ToUpper() ?? "").FontSize(12).ExtraBold();
                                
                            c.Spacing(1); // Small gap between name and address

                            c.Item().MaxWidth(200).Text(invoice.Business?.Address ?? "");
                            c.Item().Text($"{invoice.Business?.State}, India.");

                            c.Item().Text(x => { 
                                x.Span("Phone: ").FontSize(9).SemiBold(); 
                                x.Span(invoice.Business?.ContactNo ?? "-").FontSize(9); 
                            });
                            
                            if (invoice.Business?.IsGSTRegistered == true)
                            {
                                c.Item().Text(x => { 
                                    x.Span("GSTIN: ").FontSize(9).SemiBold(); 
                                    x.Span(invoice.Business?.GSTIN ?? "").FontSize(9); 
                                });
                            }
                        });

                        // Right Side: Invoice Metadata (Shaded background for contrast)
                        row.RelativeItem().Padding(10).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Invoice No:").FontSize(9).SemiBold();
                                r.RelativeItem().AlignRight().Text(invoice.InvoiceNumber).FontSize(9).Bold();
                            });

                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Date:").FontSize(9).SemiBold();
                                r.RelativeItem().AlignRight().Text(invoice.InvoiceDate.ToString("dd MMM yyyy")).FontSize(9);
                            });

                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Due Date:").FontSize(9).SemiBold();
                                r.RelativeItem().AlignRight().Text(invoice.DueDate?.ToString("dd MMM yyyy") ?? "-").FontSize(9);
                            });

                            c.Item().PaddingTop(10).PaddingBottom(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                            
                            c.Item().PaddingTop(5).Text("Payment Terms").FontSize(8).SemiBold();
                            c.Item().Text(invoice.PaymentTerms ?? "Not Specified").FontSize(9).Italic();
                        });
                    });

                    // --- PARTIES SECTION ---
                    column.Item().BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Row(row =>
                    {
                        row.RelativeItem().Padding(5).Column(c =>
                        {
                            c.Item().Text("BILL TO").SemiBold().FontSize(7);

                            c.Spacing(1);

                            c.Item().PaddingLeft(4).Text(invoice.Customer?.CustomerName ?? "").SemiBold();
                            c.Item().PaddingLeft(4).MaxWidth(220).Text(invoice.Customer?.BillingAddress ?? "");
                            c.Item().PaddingLeft(4).Text($"{invoice.Customer?.State}, India.");
                            
                            // Only show GSTIN if customer is registered for GST
                            if (invoice.Customer?.GstTreatment != "Unregistered" && !string.IsNullOrWhiteSpace(invoice.Customer?.GSTIN))
                            {
                                c.Item().PaddingLeft(4).Text(x => { x.Span("GSTIN: ").SemiBold(); x.Span(invoice.Customer?.GSTIN ?? ""); });
                            }
                        });

                        row.RelativeItem().BorderLeft(0.5f).Padding(5).Column(c =>
                        {
                            c.Item().Text("SHIP TO").SemiBold().FontSize(7);

                            c.Spacing(1);

                            c.Item().PaddingLeft(4).Text(invoice.Customer?.ShippingAddress ?? invoice.Customer?.BillingAddress ?? "");
                            c.Item().PaddingLeft(4).Text(x => { x.Span("Place of Supply: ").SemiBold(); x.Span(invoice.PlaceOfSupply ?? ""); });
                        });
                    });

                    // --- ITEMS TABLE ---
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(30);
                            columns.ConstantColumn(30);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(35);
                            if (isGstRegistered && !isComposition)
                            {
                                columns.ConstantColumn(60); // Taxable Value
                                if (isInterState) columns.ConstantColumn(65);
                                else { columns.ConstantColumn(55); columns.ConstantColumn(55); }
                            }

                            columns.ConstantColumn(70);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Sr.");
                            header.Cell().Element(HeaderStyle).Text("Description of Goods");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("HSN/SAC");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("Qty");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("Unit");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Rate");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("Disc%");
                            if (isGstRegistered && !isComposition)
                            {
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Taxable");
                                if (isInterState) header.Cell().Element(HeaderStyle).AlignCenter().Text("IGST");
                                else
                                {
                                    header.Cell().Element(HeaderStyle).AlignCenter().Text("CGST");
                                    header.Cell().Element(HeaderStyle).AlignCenter().Text("SGST");
                                }
                            }
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Amount");

                            static IContainer HeaderStyle(IContainer container) =>
                                container.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Background(Colors.Grey.Lighten4).Padding(2).DefaultTextStyle(x => x.SemiBold().FontSize(7));
                        });

                        foreach (var (item, index) in invoice.Items.Select((v, i) => (v, i)))
                        {
                            decimal taxableValue = (item.Quantity * item.UnitPrice) - item.Discount;
                            decimal rowTotal = taxableValue + item.CGST_Amount + item.SGST_Amount + item.IGST_Amount;
                            decimal dPerc = (item.Quantity * item.UnitPrice) > 0 ? (item.Discount / (item.Quantity * item.UnitPrice) * 100) : 0;

                            table.Cell().Element(CellStyle).AlignCenter().Text((index + 1).ToString());
                            table.Cell().Element(CellStyle).Text(item.Product?.ProductName ?? "");
                            table.Cell().Element(CellStyle).AlignCenter().Text(item.HSNCode ?? "");
                            table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString("N0"));
                            table.Cell().Element(CellStyle).AlignCenter().Text(item.Unit ?? "");
                            table.Cell().Element(CellStyle).AlignRight().Text(item.UnitPrice.ToString("N2"));
                            table.Cell().Element(CellStyle).AlignCenter().Text(dPerc > 0 ? $"{dPerc:N1}%" : "-");
                            if (isGstRegistered && !isComposition)
                            {
                                table.Cell().Element(CellStyle).AlignRight().Text(taxableValue.ToString("N2"));
                                if (isInterState) table.Cell().Element(CellStyle).AlignCenter().Text($"{item.TaxRate}%\n{item.IGST_Amount:N2}");
                                else
                                {
                                    table.Cell().Element(CellStyle).AlignCenter().Text($"{item.TaxRate / 2}%\n{item.CGST_Amount:N2}");
                                    table.Cell().Element(CellStyle).AlignCenter().Text($"{item.TaxRate / 2}%\n{item.SGST_Amount:N2}");
                                }
                            }

                            table.Cell().Element(CellStyle).AlignRight().Text(rowTotal.ToString("N2"));

                            static IContainer CellStyle(IContainer container) => container.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(2);
                        }
                    });

                    // --- FOOTER SUMMARY SECTION ---
                    column.Item().BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Row(row =>
                    {
                        row.RelativeItem(6).Padding(5).Column(c =>
                        {
                            c.Item().Text(x =>
                            {
                                x.Span("Total in Words: ").SemiBold();
                                x.Span(NumberToWordsConverter.ConvertToWords(invoice.GrandTotal).ToUpper());
                            });

                            c.Item().PaddingTop(8).Text("BANK DETAILS").SemiBold().FontSize(7).Underline();
                            c.Item().Text(x => { x.Span("Bank: ").SemiBold(); x.Span(invoice.Business?.BankName ?? ""); });
                            c.Item().Text(x => { x.Span("Name: ").SemiBold(); x.Span(invoice.Business?.AccountName ?? ""); });
                            c.Item().Text(x => { x.Span("A/c No: ").SemiBold(); x.Span(invoice.Business?.AccountNumber ?? ""); });
                            c.Item().Text(x => { x.Span("IFSC: ").SemiBold(); x.Span(invoice.Business?.IFSC ?? ""); });
                        });

                        row.RelativeItem(4).BorderLeft(0.5f).Column(c =>
                        {
                            // Define the helper function for rows
                            void AddSummaryRow(string label, string value, bool isBold = false)
                            {
                                c.Item().BorderBottom(0.1f).PaddingHorizontal(5).PaddingVertical(1).Row(r =>
                                {
                                    r.RelativeItem().Text(label).Style(isBold ? TextStyle.Default.SemiBold() : TextStyle.Default);
                                    r.RelativeItem().AlignRight().Text(value).Style(isBold ? TextStyle.Default.SemiBold() : TextStyle.Default);
                                });
                            }

                            if (isGstRegistered && !isComposition)
                                AddSummaryRow("Sub Total", invoice.TotalAmount.ToString("N2"));

                            if (isGstRegistered && !isComposition)
                            {
                                if (isInterState)
                                    AddSummaryRow("Total IGST", invoice.TotalIGST.ToString("N2"));
                                else
                                {
                                    AddSummaryRow("Total CGST", invoice.TotalCGST.ToString("N2"));
                                    AddSummaryRow("Total SGST", invoice.TotalSGST.ToString("N2"));
                                }
                            }

                            if (invoice.Discount > 0)
                                AddSummaryRow("Discount", $"-{invoice.Discount:N2}");

                            AddSummaryRow("Shipping", invoice.ShippingCharges.ToString("N2"));
                            AddSummaryRow("Round Off", invoice.RoundOff.ToString("N2"));

                            // Grand Total Row
                            c.Item().Background(Colors.Grey.Lighten2).Padding(5).Row(r =>
                            {
                                r.RelativeItem().Text("Grand Total").FontSize(10).ExtraBold();
                                r.RelativeItem().AlignRight().Text($"₹ {invoice.GrandTotal:N2}").FontSize(10).ExtraBold();
                            });
                        });
                    });

                    // --- FINAL SIGNATORY SECTION ---
                    column.Item().BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Row(row =>
                    {
                        row.RelativeItem().Padding(5).Column(c =>
                        {
                            c.Item().Text("Terms & Conditions:").SemiBold().FontSize(7);
                            c.Item().Text(invoice.TermsAndConditions ?? "1. Subject to local jurisdiction.").FontSize(7);
                        });
                        row.RelativeItem().BorderLeft(0.5f).AlignRight().Padding(5).Column(c =>
                        {
                            c.Item().Text($"For {invoice.Business?.BusinessName}").SemiBold();
                            c.Item().PaddingTop(30).Text("Authorised Signatory").FontSize(8);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages();
                });
            });
        }).GeneratePdf(filePath);
    }
}