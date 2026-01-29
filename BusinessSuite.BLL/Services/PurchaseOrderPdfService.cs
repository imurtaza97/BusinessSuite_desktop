using System;
using System.IO;
using System.Linq;
using BusinessSuite.DAL.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BusinessSuite.BLL.Services;

public class PurchaseOrderPdfService
{
    public void GeneratePO(PurchaseOrder po, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        bool isInterState = po.TotalIGST > 0;

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
                    column.Item().AlignCenter().Text("PURCHASE ORDER").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);

                    // --- FIRM NAME & HEADER SECTION ---
                    column.Item().Border(0.5f).Row(row =>
                    {
                        // Left Side: Business Branding
                        row.RelativeItem().BorderRight(0.5f).Padding(10).Column(c =>
                        {
                            c.Item().Text(po.Business?.BusinessName?.ToUpper() ?? "").FontSize(12).ExtraBold();
                                
                            c.Spacing(1);

                            c.Item().MaxWidth(200).Text(po.Business?.Address ?? "");
                            c.Item().Text($"{po.Business?.State}, India.");

                            c.Item().Text(x => { 
                                x.Span("Phone: ").FontSize(9).SemiBold(); 
                                x.Span(po.Business?.ContactNo ?? "-").FontSize(9); 
                            });
                            
                            c.Item().Text(x => { 
                                x.Span("GSTIN: ").FontSize(9).SemiBold(); 
                                x.Span(po.Business?.GSTIN ?? "").FontSize(9); 
                            });
                        });

                        // Right Side: PO Metadata
                        row.RelativeItem().Padding(10).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("PO Number:").FontSize(9).SemiBold();
                                r.RelativeItem().AlignRight().Text(po.PONumber).FontSize(9).Bold();
                            });

                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Date:").FontSize(9).SemiBold();
                                r.RelativeItem().AlignRight().Text(po.PODate.ToString("dd MMM yyyy")).FontSize(9);
                            });

                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Expected Deliv:").FontSize(9).SemiBold();
                                r.RelativeItem().AlignRight().Text(po.ExpectedDeliveryDate?.ToString("dd MMM yyyy") ?? "-").FontSize(9);
                            });

                            c.Item().PaddingTop(10).PaddingBottom(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                            
                            c.Item().PaddingTop(5).Text("Payment Terms").FontSize(8).SemiBold();
                            c.Item().Text(po.PaymentTerms ?? "Not Specified").FontSize(9).Italic();
                        });
                    });

                    // --- PARTIES SECTION ---
                    column.Item().BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Row(row =>
                    {
                        row.RelativeItem().Padding(5).Column(c =>
                        {
                            c.Item().Text("VENDOR").SemiBold().FontSize(7);

                            c.Spacing(1);

                            c.Item().PaddingLeft(4).Text(po.Vendor?.VendorName ?? "").SemiBold();
                            c.Item().PaddingLeft(4).MaxWidth(220).Text(po.Vendor?.Address ?? "");
                            c.Item().PaddingLeft(4).Text($"{po.Vendor?.State}, India.");
                            c.Item().PaddingLeft(4).Text(x => { x.Span("GSTIN: ").SemiBold(); x.Span(po.Vendor?.GSTIN ?? "Unregistered"); });
                        });

                        row.RelativeItem().BorderLeft(0.5f).Padding(5).Column(c =>
                        {
                            c.Item().Text("SHIP TO").SemiBold().FontSize(7);

                            c.Spacing(1);

                            c.Item().PaddingLeft(4).Text(po.Business?.BusinessName ?? "");
                            c.Item().PaddingLeft(4).Text(po.Business?.Address ?? "");
                            c.Item().PaddingLeft(4).Text(x => { x.Span("Place of Supply: ").SemiBold(); x.Span(po.PlaceOfSupply ?? ""); });
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
                            columns.ConstantColumn(60);

                            if (isInterState) columns.ConstantColumn(65);
                            else { columns.ConstantColumn(55); columns.ConstantColumn(55); }

                            columns.ConstantColumn(70);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Sr.");
                            header.Cell().Element(HeaderStyle).Text("Description of Goods");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("HSN");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("Qty");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("Unit");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Rate");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("Disc%");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Taxable");

                            if (isInterState) header.Cell().Element(HeaderStyle).AlignCenter().Text("IGST");
                            else
                            {
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("CGST");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("SGST");
                            }
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Amount");

                            static IContainer HeaderStyle(IContainer container) =>
                                container.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Background(Colors.Grey.Lighten4).Padding(2).DefaultTextStyle(x => x.SemiBold().FontSize(7));
                        });

                        foreach (var (item, index) in po.Items.Select((v, i) => (v, i)))
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
                            table.Cell().Element(CellStyle).AlignRight().Text(taxableValue.ToString("N2"));

                            if (isInterState) table.Cell().Element(CellStyle).AlignCenter().Text($"{item.TaxRate}%\n{item.IGST_Amount:N2}");
                            else
                            {
                                table.Cell().Element(CellStyle).AlignCenter().Text($"{item.TaxRate / 2}%\n{item.CGST_Amount:N2}");
                                table.Cell().Element(CellStyle).AlignCenter().Text($"{item.TaxRate / 2}%\n{item.SGST_Amount:N2}");
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
                                x.Span(NumberToWordsConverter.ConvertToWords(po.GrandTotal).ToUpper());
                            });
                        });

                        row.RelativeItem(4).BorderLeft(0.5f).Column(c =>
                        {
                            void AddSummaryRow(string label, string value, bool isBold = false)
                            {
                                c.Item().BorderBottom(0.1f).PaddingHorizontal(5).PaddingVertical(1).Row(r =>
                                {
                                    r.RelativeItem().Text(label).Style(isBold ? TextStyle.Default.SemiBold() : TextStyle.Default);
                                    r.RelativeItem().AlignRight().Text(value).Style(isBold ? TextStyle.Default.SemiBold() : TextStyle.Default);
                                });
                            }

                            AddSummaryRow("Sub Total", po.TotalAmount.ToString("N2"));

                            if (isInterState)
                                AddSummaryRow("Total IGST", po.TotalIGST.ToString("N2"));
                            else
                            {
                                AddSummaryRow("Total CGST", po.TotalCGST.ToString("N2"));
                                AddSummaryRow("Total SGST", po.TotalSGST.ToString("N2"));
                            }

                            if (po.Discount > 0)
                                AddSummaryRow("Discount", $"-{po.Discount:N2}");

                            AddSummaryRow("Shipping", po.ShippingCharges.ToString("N2"));
                            AddSummaryRow("Round Off", po.RoundOff.ToString("N2"));

                            c.Item().Background(Colors.Grey.Lighten2).Padding(5).Row(r =>
                            {
                                r.RelativeItem().Text("Grand Total").FontSize(10).ExtraBold();
                                r.RelativeItem().AlignRight().Text($"₹ {po.GrandTotal:N2}").FontSize(10).ExtraBold();
                            });
                        });
                    });

                    // --- FINAL SIGNATORY SECTION ---
                    column.Item().BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Row(row =>
                    {
                        row.RelativeItem().Padding(5).Column(c =>
                        {
                            c.Item().Text("Terms & Conditions:").SemiBold().FontSize(7);
                            c.Item().Text(po.TermsAndConditions ?? "1. Subject to local jurisdiction.").FontSize(7);
                        });
                        row.RelativeItem().BorderLeft(0.5f).AlignRight().Padding(5).Column(c =>
                        {
                            c.Item().Text($"For {po.Business?.BusinessName}").SemiBold();
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
