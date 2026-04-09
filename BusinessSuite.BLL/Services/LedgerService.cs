using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;

namespace BusinessSuite.BLL.Services;

public class LedgerService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public LedgerService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task RecordFinanceTransactionAsync(FinanceLedger transaction)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        db.FinanceLedgers.Add(transaction);
        await db.SaveChangesAsync();
    }

    public async Task<decimal> GetCustomerBalanceAsync(int businessId, int customerId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var credits = await db.FinanceLedgers
            .Where(l => l.BusinessId == businessId && l.RelatedEntity == "Customer" && l.RelatedEntityID == customerId && l.Type == "Credit")
            .Select(l => l.Amount)
            .ToListAsync();
        
        var debits = await db.FinanceLedgers
            .Where(l => l.BusinessId == businessId && l.RelatedEntity == "Customer" && l.RelatedEntityID == customerId && l.Type == "Debit")
            .Select(l => l.Amount)
            .ToListAsync();

        return debits.Sum() - credits.Sum(); // Positive means customer owes us
    }

    public async Task<decimal> GetVendorBalanceAsync(int businessId, int vendorId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var credits = await db.FinanceLedgers
            .Where(l => l.BusinessId == businessId && l.RelatedEntity == "Vendor" && l.RelatedEntityID == vendorId && l.Type == "Credit")
            .Select(l => l.Amount)
            .ToListAsync();
        
        var debits = await db.FinanceLedgers
            .Where(l => l.BusinessId == businessId && l.RelatedEntity == "Vendor" && l.RelatedEntityID == vendorId && l.Type == "Debit")
            .Select(l => l.Amount)
            .ToListAsync();

        return credits.Sum() - debits.Sum(); // Positive means we owe vendor
    }

    public async Task<List<Stock>> GetProductStockAsync(int productId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Stocks
            .Include(s => s.Warehouse)
            .Where(s => s.ProductID == productId)
            .ToListAsync();
    }

    public async Task<List<FinanceLedger>> GetCustomerTransactionsAsync(int businessId, int customerId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FinanceLedgers
            .Where(l => l.BusinessId == businessId && l.RelatedEntity == "Customer" && l.RelatedEntityID == customerId)
            .OrderByDescending(l => l.TransactionDate)
            .ToListAsync();
    }

    public async Task<List<FinanceLedger>> GetVendorTransactionsAsync(int businessId, int vendorId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FinanceLedgers
            .Where(l => l.BusinessId == businessId && l.RelatedEntity == "Vendor" && l.RelatedEntityID == vendorId)
            .OrderByDescending(l => l.TransactionDate)
            .ToListAsync();
    }

    public async Task<bool> TransferStockAsync(int businessId, int productId, int fromWarehouseId, int toWarehouseId, decimal quantity, string description)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var sourceStock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == productId && s.WarehouseID == fromWarehouseId);
            if (sourceStock == null || sourceStock.Quantity < quantity) return false;

            sourceStock.Quantity -= quantity;

            var destStock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == productId && s.WarehouseID == toWarehouseId);
            if (destStock == null)
            {
                destStock = new Stock { ProductID = productId, WarehouseID = toWarehouseId, Quantity = 0 };
                db.Stocks.Add(destStock);
            }
            destStock.Quantity += quantity;

            var stockTx = new StockTransaction
            {
                BusinessId = businessId,
                ProductID = productId,
                WarehouseID = fromWarehouseId,
                ToWarehouseID = toWarehouseId,
                Quantity = quantity,
                TransactionType = "Transfer",
                ReferenceType = "Transfer",
                TransactionDate = DateTime.Now,
                Description = description
            };
            db.StockTransactions.Add(stockTx);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task UpdateStockAdjustAsync(int businessId, int productId, int warehouseId, decimal newQuantity, string reason)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == productId && s.WarehouseID == warehouseId);
        decimal oldQty = 0;

        if (stock == null)
        {
            stock = new Stock { ProductID = productId, WarehouseID = warehouseId, Quantity = newQuantity };
            db.Stocks.Add(stock);
        }
        else
        {
            oldQty = stock.Quantity;
            stock.Quantity = newQuantity;
        }

        var stockTx = new StockTransaction
        {
            BusinessId = businessId,
            ProductID = productId,
            WarehouseID = warehouseId,
            Quantity = newQuantity - oldQty,
            TransactionType = "Adjustment",
            ReferenceType = "Adjustment",
            TransactionDate = DateTime.Now,
            Description = reason,
            PreviousQuantity = oldQty,
            NewQuantity = newQuantity
        };
        db.StockTransactions.Add(stockTx);

        await db.SaveChangesAsync();
    }

    public async Task ProcessInvoiceAsync(Invoice invoice, int warehouseId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // 1. Add/Update Invoice
            if (invoice.InvoiceID == 0)
            {
                db.Invoices.Add(invoice);
                await db.SaveChangesAsync();
            }
            else
            {
                var existingInvoice = await db.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID);

                if (existingInvoice == null)
                    throw new InvalidOperationException($"Invoice {invoice.InvoiceID} not found.");

                existingInvoice.BusinessID = invoice.BusinessID;
                existingInvoice.CustomerID = invoice.CustomerID;
                existingInvoice.InvoiceNumber = invoice.InvoiceNumber;
                existingInvoice.InvoiceDate = invoice.InvoiceDate;
                existingInvoice.DueDate = invoice.DueDate;
                existingInvoice.IsAutoRoundOff = invoice.IsAutoRoundOff;
                existingInvoice.TotalAmount = invoice.TotalAmount;
                existingInvoice.TotalTax = invoice.TotalTax;
                existingInvoice.Discount = invoice.Discount;
                existingInvoice.GrandTotal = invoice.GrandTotal;
                existingInvoice.TotalPaid = invoice.TotalPaid;
                existingInvoice.Notes = invoice.Notes;
                existingInvoice.DeliveryStatus = invoice.DeliveryStatus;
                existingInvoice.PaymentStatus = invoice.PaymentStatus;
                existingInvoice.IsItemLevelDiscount = invoice.IsItemLevelDiscount;
                existingInvoice.PaymentMethod = invoice.PaymentMethod;
                existingInvoice.PaymentTerms = invoice.PaymentTerms;
                existingInvoice.TermsAndConditions = invoice.TermsAndConditions;
                existingInvoice.PlaceOfSupply = invoice.PlaceOfSupply;
                existingInvoice.ReverseCharge = invoice.ReverseCharge;
                existingInvoice.RoundOff = invoice.RoundOff;
                existingInvoice.TotalCGST = invoice.TotalCGST;
                existingInvoice.TotalSGST = invoice.TotalSGST;
                existingInvoice.TotalIGST = invoice.TotalIGST;
                existingInvoice.ShippingCharges = invoice.ShippingCharges;

                db.InvoiceItems.RemoveRange(existingInvoice.Items);
                await db.SaveChangesAsync();

                foreach (var item in invoice.Items)
                {
                    item.InvoiceID = existingInvoice.InvoiceID;
                    db.InvoiceItems.Add(item);
                }

                await db.SaveChangesAsync();
            }

            // 2. Handle Status-Triggered Side Effects
            if (invoice.DeliveryStatus == "Cancelled")
            {
                await RevertInvoiceEffectsInternalAsync(db, invoice.InvoiceID);
            }
            else if (invoice.DeliveryStatus == "Shipped")
            {
                // Check if already processed to avoid duplicates
                bool alreadyShipped = await db.StockTransactions.AnyAsync(t => 
                    t.ReferenceType == "Invoice" && 
                    t.ReferenceID == invoice.InvoiceID && 
                    t.TransactionType == "Out");

                if (!alreadyShipped)
                {
                    // Record Finance Ledger (Customer Debit - Recognition of debt)
                    var ledger = new FinanceLedger
                    {
                        BusinessId = invoice.BusinessID,
                        TransactionDate = invoice.InvoiceDate,
                        Type = "Debit",
                        Amount = invoice.GrandTotal,
                        RelatedEntity = "Customer",
                        RelatedEntityID = invoice.CustomerID,
                        ReferenceType = "Invoice",
                        ReferenceID = invoice.InvoiceID,
                        Description = $"Invoice {invoice.InvoiceNumber} - Recognition"
                    };
                    db.FinanceLedgers.Add(ledger);

                    // Update Stock for each item
                    foreach (var item in invoice.Items)
                    {
                        var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == item.ProductID && s.WarehouseID == warehouseId);
                        decimal oldQty = stock?.Quantity ?? 0;
                        
                        if (stock == null)
                        {
                            stock = new Stock { ProductID = item.ProductID, WarehouseID = warehouseId, Quantity = -item.Quantity };
                            db.Stocks.Add(stock);
                        }
                        else
                        {
                            stock.Quantity -= item.Quantity;
                        }

                        db.StockTransactions.Add(new StockTransaction
                        {
                            BusinessId = invoice.BusinessID,
                            ProductID = item.ProductID,
                            WarehouseID = warehouseId,
                            Quantity = -item.Quantity,
                            TransactionType = "Out",
                            ReferenceType = "Invoice",
                            ReferenceID = invoice.InvoiceID,
                            TransactionNumber = invoice.InvoiceNumber,
                            TransactionDate = DateTime.Now,
                            PreviousQuantity = oldQty,
                            NewQuantity = oldQty - item.Quantity,
                            Description = $"Shipped via Invoice {invoice.InvoiceNumber}"
                        });
                    }
                }
            }
            else if (invoice.DeliveryStatus == "Returned")
            {
                // Verify it was previously shipped before allowing return? 
                // For now, just allow return if not already returned.
                bool alreadyReturned = await db.StockTransactions.AnyAsync(t => 
                    t.ReferenceType == "Invoice" && 
                    t.ReferenceID == invoice.InvoiceID && 
                    t.TransactionType == "In" && 
                    t.Description != null && t.Description.Contains("Return"));

                if (!alreadyReturned)
                {
                    // Record Finance Ledger (Customer Credit - Reversing the debt)
                    var ledger = new FinanceLedger
                    {
                        BusinessId = invoice.BusinessID,
                        TransactionDate = DateTime.Now,
                        Type = "Credit",
                        Amount = invoice.GrandTotal,
                        RelatedEntity = "Customer",
                        RelatedEntityID = invoice.CustomerID,
                        ReferenceType = "Sales Return",
                        ReferenceID = invoice.InvoiceID,
                        Description = $"Sales Return for Invoice {invoice.InvoiceNumber}"
                    };
                    db.FinanceLedgers.Add(ledger);

                    // Add Stock back
                    foreach (var item in invoice.Items)
                    {
                        var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == item.ProductID && s.WarehouseID == warehouseId);
                        decimal oldQty = stock?.Quantity ?? 0;
                        
                        if (stock == null)
                        {
                            stock = new Stock { ProductID = item.ProductID, WarehouseID = warehouseId, Quantity = item.Quantity };
                            db.Stocks.Add(stock);
                        }
                        else
                        {
                            stock.Quantity += item.Quantity;
                        }

                        db.StockTransactions.Add(new StockTransaction
                        {
                            BusinessId = invoice.BusinessID,
                            ProductID = item.ProductID,
                            WarehouseID = warehouseId,
                            Quantity = item.Quantity,
                            TransactionType = "In",
                            ReferenceType = "Sales Return",
                            ReferenceID = invoice.InvoiceID,
                            TransactionNumber = invoice.InvoiceNumber,
                            TransactionDate = DateTime.Now,
                            PreviousQuantity = oldQty,
                            NewQuantity = oldQty + item.Quantity,
                            Description = $"Returned via Invoice {invoice.InvoiceNumber}"
                        });
                    }
                }
            }

            // 3. Handle Payment Recognition (Enhanced Logic)
            if (invoice.TotalPaid > 0)
            {
                var existingPayment = await db.FinanceLedgers.FirstOrDefaultAsync(l => 
                    l.ReferenceType == "InvoicePayment" && 
                    l.ReferenceID == invoice.InvoiceID);

                if (existingPayment == null)
                {
                    db.FinanceLedgers.Add(new FinanceLedger
                    {
                        BusinessId = invoice.BusinessID,
                        TransactionDate = DateTime.Now,
                        Type = "Credit",
                        Amount = invoice.TotalPaid,
                        RelatedEntity = "Customer",
                        RelatedEntityID = invoice.CustomerID,
                        ReferenceType = "InvoicePayment",
                        ReferenceID = invoice.InvoiceID,
                        Description = $"Initial payment for Invoice {invoice.InvoiceNumber}"
                    });
                }
                else 
                {
                    existingPayment.Amount = invoice.TotalPaid;
                    existingPayment.RelatedEntityID = invoice.CustomerID; // Sync just in case
                    db.Entry(existingPayment).State = EntityState.Modified;
                }
            }
            else 
            {
                // Remove existing payment if TotalPaid became 0
                var existingPayment = await db.FinanceLedgers.FirstOrDefaultAsync(l => 
                    l.ReferenceType == "InvoicePayment" && 
                    l.ReferenceID == invoice.InvoiceID);
                if (existingPayment != null)
                {
                    db.FinanceLedgers.Remove(existingPayment);
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ProcessPurchaseOrderAsync(PurchaseOrder po, int warehouseId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // 1. Add/Update PO
            if (po.PurchaseOrderID == 0)
            {
                db.PurchaseOrders.Add(po);
                await db.SaveChangesAsync();
            }
            else
            {
                var existingPO = await db.PurchaseOrders
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.PurchaseOrderID == po.PurchaseOrderID);

                if (existingPO == null)
                    throw new InvalidOperationException($"Purchase order {po.PurchaseOrderID} not found.");

                existingPO.BusinessId = po.BusinessId;
                existingPO.VendorId = po.VendorId;
                existingPO.PONumber = po.PONumber;
                existingPO.PODate = po.PODate;
                existingPO.ExpectedDeliveryDate = po.ExpectedDeliveryDate;
                existingPO.IsAutoRoundOff = po.IsAutoRoundOff;
                existingPO.TotalAmount = po.TotalAmount;
                existingPO.TotalTax = po.TotalTax;
                existingPO.Discount = po.Discount;
                existingPO.GrandTotal = po.GrandTotal;
                existingPO.TotalPaid = po.TotalPaid;
                existingPO.Notes = po.Notes;
                existingPO.DeliveryStatus = po.DeliveryStatus;
                existingPO.PaymentStatus = po.PaymentStatus;
                existingPO.IsItemLevelDiscount = po.IsItemLevelDiscount;
                existingPO.PaymentMethod = po.PaymentMethod;
                existingPO.PaymentTerms = po.PaymentTerms;
                existingPO.TermsAndConditions = po.TermsAndConditions;
                existingPO.PlaceOfSupply = po.PlaceOfSupply;
                existingPO.ReverseCharge = po.ReverseCharge;
                existingPO.RoundOff = po.RoundOff;
                existingPO.TotalCGST = po.TotalCGST;
                existingPO.TotalSGST = po.TotalSGST;
                existingPO.TotalIGST = po.TotalIGST;
                existingPO.ShippingCharges = po.ShippingCharges;
                existingPO.VendorBillPath = po.VendorBillPath;

                db.PurchaseOrderItems.RemoveRange(existingPO.Items);
                await db.SaveChangesAsync();

                foreach (var item in po.Items)
                {
                    item.PurchaseOrderID = existingPO.PurchaseOrderID;
                    db.PurchaseOrderItems.Add(item);
                }

                await db.SaveChangesAsync();
            }

            // 2. Handle Status-Triggered Side Effects
            if (po.DeliveryStatus == "Cancelled")
            {
                await RevertPOEffectsInternalAsync(db, po.PurchaseOrderID);
            }
            else if (po.DeliveryStatus == "Received")
            {
                bool alreadyReceived = await db.StockTransactions.AnyAsync(t => 
                    t.ReferenceType == "PurchaseOrder" && 
                    t.ReferenceID == po.PurchaseOrderID && 
                    t.TransactionType == "In");

                if (!alreadyReceived)
                {
                    // Record Finance Ledger (Vendor Credit - Recognition of our debt to them)
                    var ledger = new FinanceLedger
                    {
                        BusinessId = po.BusinessId,
                        TransactionDate = po.PODate,
                        Type = "Credit",
                        Amount = po.GrandTotal,
                        RelatedEntity = "Vendor",
                        RelatedEntityID = po.VendorId,
                        ReferenceType = "PurchaseOrder",
                        ReferenceID = po.PurchaseOrderID,
                        Description = $"Purchase Order {po.PONumber} - Recognition"
                    };
                    db.FinanceLedgers.Add(ledger);

                    // Update Stock for each item
                    foreach (var item in po.Items)
                    {
                        var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == item.ProductID && s.WarehouseID == warehouseId);
                        decimal oldQty = stock?.Quantity ?? 0;

                        if (stock == null)
                        {
                            stock = new Stock { ProductID = item.ProductID, WarehouseID = warehouseId, Quantity = item.Quantity };
                            db.Stocks.Add(stock);
                        }
                        else
                        {
                            stock.Quantity += item.Quantity;
                        }

                        db.StockTransactions.Add(new StockTransaction
                        {
                            BusinessId = po.BusinessId,
                            ProductID = item.ProductID,
                            WarehouseID = warehouseId,
                            Quantity = item.Quantity,
                            TransactionType = "In",
                            ReferenceType = "PurchaseOrder",
                            ReferenceID = po.PurchaseOrderID,
                            TransactionNumber = po.PONumber,
                            TransactionDate = DateTime.Now,
                            PreviousQuantity = oldQty,
                            NewQuantity = oldQty + item.Quantity,
                            Description = $"Received via PO {po.PONumber}"
                        });
                    }
                }
            }
            else if (po.DeliveryStatus == "Returned-to-Vendor")
            {
                bool alreadyReturned = await db.StockTransactions.AnyAsync(t => 
                    t.ReferenceType == "PurchaseOrder" && 
                    t.ReferenceID == po.PurchaseOrderID && 
                    t.TransactionType == "Out" && 
                    t.Description != null && t.Description.Contains("Return"));

                if (!alreadyReturned)
                {
                    // Record Finance Ledger (Vendor Debit - Reversing the credit)
                    var ledger = new FinanceLedger
                    {
                        BusinessId = po.BusinessId,
                        TransactionDate = DateTime.Now,
                        Type = "Debit",
                        Amount = po.GrandTotal,
                        RelatedEntity = "Vendor",
                        RelatedEntityID = po.VendorId,
                        ReferenceType = "Purchase Return",
                        ReferenceID = po.PurchaseOrderID,
                        Description = $"Purchase Return for PO {po.PONumber}"
                    };
                    db.FinanceLedgers.Add(ledger);

                    // Remove Stock
                    foreach (var item in po.Items)
                    {
                        var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == item.ProductID && s.WarehouseID == warehouseId);
                        decimal oldQty = stock?.Quantity ?? 0;

                        if (stock == null)
                        {
                            stock = new Stock { ProductID = item.ProductID, WarehouseID = warehouseId, Quantity = -item.Quantity };
                            db.Stocks.Add(stock);
                        }
                        else
                        {
                            stock.Quantity -= item.Quantity;
                        }

                        db.StockTransactions.Add(new StockTransaction
                        {
                            BusinessId = po.BusinessId,
                            ProductID = item.ProductID,
                            WarehouseID = warehouseId,
                            Quantity = -item.Quantity,
                            TransactionType = "Out",
                            ReferenceType = "Purchase Return",
                            ReferenceID = po.PurchaseOrderID,
                            TransactionNumber = po.PONumber,
                            TransactionDate = DateTime.Now,
                            PreviousQuantity = oldQty,
                            NewQuantity = oldQty - item.Quantity,
                            Description = $"Returned-to-Vendor via PO {po.PONumber}"
                        });
                    }
                }
            }

            // 3. Handle Payment Recognition (Enhanced Logic)
            if (po.TotalPaid > 0)
            {
                var existingPayment = await db.FinanceLedgers.FirstOrDefaultAsync(l => 
                    l.ReferenceType == "POPayment" && 
                    l.ReferenceID == po.PurchaseOrderID);

                if (existingPayment == null)
                {
                    db.FinanceLedgers.Add(new FinanceLedger
                    {
                        BusinessId = po.BusinessId,
                        TransactionDate = DateTime.Now,
                        Type = "Debit",
                        Amount = po.TotalPaid,
                        RelatedEntity = "Vendor",
                        RelatedEntityID = po.VendorId,
                        ReferenceType = "POPayment",
                        ReferenceID = po.PurchaseOrderID,
                        Description = $"Initial payment for PO {po.PONumber}"
                    });
                }
                else 
                {
                    existingPayment.Amount = po.TotalPaid;
                    existingPayment.RelatedEntityID = po.VendorId;
                    db.Entry(existingPayment).State = EntityState.Modified;
                }
            }
            else 
            {
                // Remove existing payment if TotalPaid became 0
                var existingPayment = await db.FinanceLedgers.FirstOrDefaultAsync(l => 
                    l.ReferenceType == "POPayment" && 
                    l.ReferenceID == po.PurchaseOrderID);
                if (existingPayment != null)
                {
                    db.FinanceLedgers.Remove(existingPayment);
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ProcessPaymentAsync(int businessId, string entityType, int entityId, decimal amount, string method, string reference, string notes)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        
        // For Customer: Money Received = Credit (reduces their debt/debit)
        // For Vendor: Money Paid = Debit (reduces our debt/credit)
        string type = entityType == "Customer" ? "Credit" : "Debit";

        int? refId = null;
        if (int.TryParse(reference, out int id)) refId = id;

        var ledger = new FinanceLedger
        {
            BusinessId = businessId,
            TransactionDate = DateTime.Now,
            Type = type,
            Amount = amount,
            RelatedEntity = entityType,
            RelatedEntityID = entityId,
            ReferenceType = "Payment",
            ReferenceID = refId,
            Description = $"Payment via {method}. Ref: {reference}. {notes}"
        };

        db.FinanceLedgers.Add(ledger);
        await db.SaveChangesAsync();
    }

    public async Task<bool> AddProductWithStockAsync(Product product, int warehouseId, decimal initialQty)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            db.Products.Add(product);
            await db.SaveChangesAsync(); // Get ProductID

            if (initialQty != 0)
            {
                var stock = new Stock
                {
                    ProductID = product.ProductID,
                    WarehouseID = warehouseId,
                    Quantity = initialQty
                };
                db.Stocks.Add(stock);

                db.StockTransactions.Add(new StockTransaction
                {
                    BusinessId = product.BusinessID,
                    ProductID = product.ProductID,
                    WarehouseID = warehouseId,
                    Quantity = initialQty,
                    TransactionType = "In",
                    ReferenceType = "Opening Stock",
                    TransactionDate = DateTime.Now,
                    PreviousQuantity = 0,
                    NewQuantity = initialQty,
                    Description = "Initial Opening Stock"
                });
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<List<StockTransaction>> GetStockLedgerPaginatedAsync(int businessId, int page, int pageSize, int? productId = null, int? warehouseId = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.StockTransactions
            .Include(t => t.Product)
            .Include(t => t.Warehouse)
            .Include(t => t.ToWarehouse)
            .Where(t => t.BusinessId == businessId);

        if (productId.HasValue)
            query = query.Where(t => t.ProductID == productId.Value);
        
        if (warehouseId.HasValue)
            query = query.Where(t => t.WarehouseID == warehouseId.Value || t.ToWarehouseID == warehouseId.Value);

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetStockLedgerCountAsync(int businessId, int? productId = null, int? warehouseId = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.StockTransactions.Where(t => t.BusinessId == businessId);

        if (productId.HasValue)
            query = query.Where(t => t.ProductID == productId.Value);
        
        if (warehouseId.HasValue)
            query = query.Where(t => t.WarehouseID == warehouseId.Value || t.ToWarehouseID == warehouseId.Value);

        return await query.CountAsync();
    }

    public async Task<List<StockTransaction>> GetStockLedgerAsync(int businessId, int? productId = null, int? warehouseId = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.StockTransactions
            .Include(t => t.Product)
            .Include(t => t.Warehouse)
            .Include(t => t.ToWarehouse)
            .Where(t => t.BusinessId == businessId);

        if (productId.HasValue)
            query = query.Where(t => t.ProductID == productId.Value);
        
        if (warehouseId.HasValue)
            query = query.Where(t => t.WarehouseID == warehouseId.Value || t.ToWarehouseID == warehouseId.Value);

        return await query.OrderByDescending(t => t.TransactionDate).ToListAsync();
    }
    public async Task<bool> DeleteInvoiceWithReversalAsync(int invoiceId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await RevertInvoiceEffectsInternalAsync(db, invoiceId);
            
            var invoice = await db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);
            if (invoice != null)
            {
                db.InvoiceItems.RemoveRange(invoice.Items);
                db.Invoices.Remove(invoice);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> DeletePurchaseOrderWithReversalAsync(int poId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await RevertPOEffectsInternalAsync(db, poId);

            var po = await db.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.PurchaseOrderID == poId);
            if (po != null)
            {
                db.PurchaseOrderItems.RemoveRange(po.Items);
                db.PurchaseOrders.Remove(po);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    private async Task RevertInvoiceEffectsInternalAsync(AppDbContext db, int invoiceId)
    {
        // 1. Revert Stock
        var stockTxs = await db.StockTransactions.Where(t => t.ReferenceType == "Invoice" && t.ReferenceID == invoiceId).ToListAsync();
        foreach (var tx in stockTxs)
        {
            var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == tx.ProductID && s.WarehouseID == tx.WarehouseID);
            if (stock != null)
            {
                stock.Quantity -= tx.Quantity; // Reverse the movement (if tx.Quantity was negative, this adds it back)
            }
            db.StockTransactions.Remove(tx);
        }

        // 2. Revert Returns (if any)
        var returnTxs = await db.StockTransactions.Where(t => t.ReferenceType == "Sales Return" && t.ReferenceID == invoiceId).ToListAsync();
        foreach (var tx in returnTxs)
        {
            var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == tx.ProductID && s.WarehouseID == tx.WarehouseID);
            if (stock != null)
            {
                stock.Quantity -= tx.Quantity;
            }
            db.StockTransactions.Remove(tx);
        }

        // 3. Revert Finance Ledger Entries
        var financeEntries = await db.FinanceLedgers.Where(l => 
            (l.ReferenceType == "Invoice" || l.ReferenceType == "InvoicePayment" || l.ReferenceType == "Sales Return") && 
            l.ReferenceID == invoiceId).ToListAsync();
        
        db.FinanceLedgers.RemoveRange(financeEntries);
    }

    private async Task RevertPOEffectsInternalAsync(AppDbContext db, int poId)
    {
        // 1. Revert Stock
        var stockTxs = await db.StockTransactions.Where(t => t.ReferenceType == "PurchaseOrder" && t.ReferenceID == poId).ToListAsync();
        foreach (var tx in stockTxs)
        {
            var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == tx.ProductID && s.WarehouseID == tx.WarehouseID);
            if (stock != null)
            {
                stock.Quantity -= tx.Quantity;
            }
            db.StockTransactions.Remove(tx);
        }

        // 2. Revert Returns (if any)
        var returnTxs = await db.StockTransactions.Where(t => t.ReferenceType == "Purchase Return" && t.ReferenceID == poId).ToListAsync();
        foreach (var tx in returnTxs)
        {
            var stock = await db.Stocks.FirstOrDefaultAsync(s => s.ProductID == tx.ProductID && s.WarehouseID == tx.WarehouseID);
            if (stock != null)
            {
                stock.Quantity -= tx.Quantity;
            }
            db.StockTransactions.Remove(tx);
        }

        // 3. Revert Finance Ledger Entries
        var financeEntries = await db.FinanceLedgers.Where(l => 
            (l.ReferenceType == "PurchaseOrder" || l.ReferenceType == "POPayment" || l.ReferenceType == "Purchase Return") && 
            l.ReferenceID == poId).ToListAsync();

        db.FinanceLedgers.RemoveRange(financeEntries);
    }
}
