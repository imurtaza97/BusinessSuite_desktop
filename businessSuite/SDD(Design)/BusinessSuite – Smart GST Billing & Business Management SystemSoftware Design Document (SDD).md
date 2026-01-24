## **1. Introduction**

### **1.1 Purpose & Scope**
This Software Design Document (SDD) translates the approved Software Requirements Specification (SRS) into a comprehensive design model for the development team.  
It serves as a bridge between the **requirement analysis phase** and the **implementation phase**, ensuring that every feature defined in the SRS is technically designed and structurally represented.

The scope of this document covers the **architectural design**, **module-level specifications**, **database schema**, **interface layouts**, and **data flow definitions** necessary to implement the _BusinessSuite – Smart GST Billing & Business Management System_.  
It provides a unified reference for developers, testers, and project managers to understand how each component interacts to meet the stated requirements and constraints.

**Derived from:**
	SRS 1.1 Purpose
	SRS 1.4 Product Scope
### **1.2 Development Model**

The system will follow the **Waterfall Model with Incremental Delivery**, as established in the SRS.  
Each major phase—Requirements, Design, Implementation, Testing, and Deployment—will be sequentially executed, ensuring stability and traceability between phases.  
However, the **incremental delivery** approach allows partial modules (e.g., authentication, invoicing, reporting) to be developed and tested independently before full system integration.

This hybrid strategy enables:

- Early module validation and feedback,
    
- Reduced integration risk,
    
- Predictable scheduling and versioned releases.

**Derived from:**
	SRS 1.6 Development Approach
### **1.3 Glossary**

This document follows the terminology established in the SRS Glossary, extending it where necessary to include new technical design terms relevant to implementation.

**Inherited Terms (from SRS):**

- **GST, CGST, SGST, IGST:** Indian taxation components.
    
- **ERP:** Enterprise Resource Planning.
    
- **CRUD:** Create, Read, Update, Delete operations.
    
- **UI/UX:** User Interface / User Experience.
    
- **ORM:** Object Relational Mapper (Entity Framework Core).
    
- **SQLite:** Local database engine.
    

**Additional Terms (introduced in SDD):**

- **BCrypt:** A password-hashing function used for secure authentication.
    
- **Layered Architecture:** A design structure dividing presentation, business logic, and data access layers.
    
- **Incremental Delivery:** Deployment of system modules in stages for early feedback and validation.
    

**Derived from:**
	SRS 5.1 Glossary
## **2. System Architecture (High-Level Design - HLD)**

### **2.1 Architectural Overview**

The **BusinessSuite** system follows a **Three-Layer Architecture** to ensure modularity, maintainability, and clear separation of concerns.

#### **Layer 1: Presentation Layer (UI)**

- Built using **C# WinForms** for a responsive and intuitive desktop interface.
    
- Provides all user interaction screens, including Business Registration, Product Management, Invoicing, Purchase Orders, Reports, and Tax Configuration.
    
- Handles **input validation** (GSTIN, numeric fields, etc.) before passing data to the Business Logic Layer.

#### **Layer 2: Business Logic Layer (BLL)**

- Contains core logic for **authentication**, **tax computation**, **invoice numbering**, and **report generation**.
    
- Implements validation rules for Regular vs. Composition GST schemes.
    
- Acts as the intermediary between UI and DAL, ensuring all business rules are consistently applied.

#### **Layer 3: Data Access Layer (DAL)**

- Manages direct interaction with the local database (SQLite).
    
- Uses **Entity Framework Core (ORM)** for CRUD operations, schema migrations, and entity relationships.
    
- Responsible for maintaining data integrity and enforcing **referential constraints** between tables such as `Products`, `Invoices`, `Vendors`, and `Taxes`.
    

#### **Communication Flow**

UI Layer → BLL → DAL → Database  
Response data flows back in reverse order, maintaining encapsulation between modules.

 **Derived from:**
	SRS 3.1 _Overview_ and 4.1 _Database Requirements_ (Desktop App + Local DB Context)

### **2.2 Technology Stack**

| **Category**          | **Technology / Tool**            | **Purpose / Usage**                                              | **Derivation**          |
| --------------------- | -------------------------------- | ---------------------------------------------------------------- | ----------------------- |
| Programming Language  | **C# (.NET 8 Runtime)**          | Core development framework for Windows-based desktop application | SRS 3.2.1               |
| UI Framework          | **WinForms**                     | Presentation layer for user interaction and data entry           | SRS 3.3.1               |
| Database              | **SQLite**                       | Lightweight, file-based database engine for offline storage      | SRS 4.1.1               |
| ORM                   | **Entity Framework Core**        | Object-relational mapping for CRUD and schema management         | SRS 4.1.3               |
| Security Library      | **BCrypt.Net**                   | Password hashing for secure authentication                       | SRS 3.2.1, Dependencies |
| PDF / Reporting Tools | **iTextSharp**, **FastReport**   | Invoice generation and analytics reporting                       | SRS 3.3.3               |
| OS Environment        | **Windows 10 and above**         | Target platform for deployment                                   | SRS 3.3.2               |
| Optional Future APIs  | **RESTful Cloud Sync (planned)** | For online synchronization and updates                           | SRS 3.3.4               |
This technology combination ensures:

- **Performance** for local operations,
    
- **Scalability** for future cloud versions, and
    
- **Maintainability** through modern .NET tooling.

**Derived from:**
	SRS 3.2.1, 3.3.3, and 4.1.1–4.1.3 (_Dependencies and Environment Requirements_)
### **2.3 Component Diagram**
![[Component Diagram.png]]



The following represents a **high-level component view** of the system, depicting the interaction between the Presentation Layer, Business Logic Layer, and Data Access Layer.

This structure allows **independent module development** (incremental delivery) while maintaining strict data consistency through the centralized business logic layer.

**Derived from:**
	SRS 5.2 _Use Case Diagram_ (system modules and interactions).

## **3. Data Design (Low-Level Design - LLD)**

### **3.1 Entity–Relationship Diagram (ERD)**
The **Entity–Relationship Diagram (ERD)** represents the logical structure of the database, showing how data entities interrelate within the BusinessSuite system.  
It forms the foundation for the database schema and ensures consistency between application modules and their underlying data storage.
![[ERD.png]]#### **Entities and Relationships**

- **Businesses** → One-to-Many → **Users**
    
- **Businesses** → One-to-Many → **Products**, **Vendors**, **Customers**
    
- **Invoices** → One-to-Many → **InvoiceItems**
    
- **PurchaseOrders** → One-to-Many → **Vendors** and **Products**
    
- **Taxes** → One-to-Many → **Products**
    
- **Settings** → One-to-One → **Businesses**
Each relationship enforces **referential integrity** using `FOREIGN KEY` constraints, ensuring consistency between linked entities (e.g., deleting a Business will cascade to related Users, Products, and Transactions).

**Derived from:**
	SRS 4.1.2 Database Structure (Tentative Tables)
### **3.2 Database Schema Specification**

This section provides the final **table definitions** (names, columns, data types, constraints) for developers implementing the SQLite schema using **Entity Framework Core** migrations.
#### **Table: Businesses**

| Column Name  | Data Type | Constraints                      | Description                            |
| ------------ | --------- | -------------------------------- | -------------------------------------- |
| BusinessID   | INTEGER   | Primary Key, Auto Increment      | Unique ID for each registered business |
| BusinessName | TEXT(100) | NOT NULL                         | Registered business name               |
| GSTIN        | TEXT(15)  | UNIQUE                           | GST Identification Number              |
| Address      | TEXT(255) | NULL                             | Business address                       |
| ContactNo    | TEXT(15)  | NULL                             | Business contact number                |
| BusinessType | TEXT(20)  | CHECK (‘Regular’, ‘Composition’) | GST classification                     |
| CreatedAt    | DATETIME  | DEFAULT CURRENT_TIMESTAMP        | Record creation time                   |
#### **Table: Users**

| Column Name  | Data Type | Constraints                          | Description              |
| ------------ | --------- | ------------------------------------ | ------------------------ |
| UserID       | INTEGER   | Primary Key, Auto Increment          | Unique user ID           |
| BusinessID   | INTEGER   | Foreign Key → Businesses(BusinessID) | Associated business      |
| Username     | TEXT(50)  | UNIQUE                               | Login ID                 |
| PasswordHash | TEXT(255) | NOT NULL                             | Hashed password (BCrypt) |
| Role         | TEXT(20)  | DEFAULT 'Owner'                      | User role                |
| CreatedAt    | DATETIME  | DEFAULT CURRENT_TIMESTAMP            | Account creation date    |
#### **Table: Customers**

| Column Name     | Data Type | Constraints                              | Description                               |
| --------------- | --------- | ---------------------------------------- | ----------------------------------------- |
| CustomerID      | INTEGER   | **Primary Key, Auto Increment**          | Unique customer record ID                 |
| BusinessID      | INTEGER   | **Foreign Key → Businesses(BusinessID)** | Associated business                       |
| CustomerName    | TEXT(100) | **NOT NULL**                             | Full name or company name                 |
| GSTIN           | TEXT(15)  | UNIQUE NULL                              | GST Identification Number (if applicable) |
| ContactNo       | TEXT(15)  | NULL                                     | Customer’s phone number                   |
| Email           | TEXT(100) | NULL                                     | Customer’s email address                  |
| BillingAddress  | TEXT(255) | NULL                                     | Address used for billing                  |
| ShippingAddress | TEXT(255) | NULL                                     | Address used for shipping                 |
| State           | TEXT(50)  | NULL                                     | State for tax determination               |
| Country         | TEXT(50)  | DEFAULT 'India'                          | Country name                              |
| CreatedAt       | DATETIME  | DEFAULT CURRENT_TIMESTAMP                | Record creation date                      |
#### **Table: Vendors**

| Column Name | Data Type | Constraints                              | Description                  |
| ----------- | --------- | ---------------------------------------- | ---------------------------- |
| VendorID    | INTEGER   | **Primary Key, Auto Increment**          | Unique vendor record ID      |
| BusinessID  | INTEGER   | **Foreign Key → Businesses(BusinessID)** | Associated business          |
| VendorName  | TEXT(100) | **NOT NULL**                             | Supplier or vendor name      |
| GSTIN       | TEXT(15)  | UNIQUE NULL                              | Vendor GSTIN (if applicable) |
| ContactNo   | TEXT(15)  | NULL                                     | Vendor contact number        |
| Email       | TEXT(100) | NULL                                     | Vendor email                 |
| Address     | TEXT(255) | NULL                                     | Vendor address               |
| State       | TEXT(50)  | NULL                                     | State for tax purposes       |
| Country     | TEXT(50)  | DEFAULT 'India'                          | Country                      |
| CreatedAt   | DATETIME  | DEFAULT CURRENT_TIMESTAMP                | Record creation date         |
#### **Table: Settings**

|Column Name|Data Type|Constraints|Description|
|---|---|---|---|
|SettingID|INTEGER|**Primary Key, Auto Increment**|Unique settings ID|
|BusinessID|INTEGER|**Foreign Key → Businesses(BusinessID)**|Related business|
|Currency|TEXT(10)|DEFAULT 'INR'|Currency code (ISO 4217)|
|DateFormat|TEXT(20)|DEFAULT 'DD-MM-YYYY'|Preferred display format|
|TimeZone|TEXT(50)|DEFAULT 'Asia/Kolkata'|Application timezone|
|Theme|TEXT(20)|DEFAULT 'Light'|UI preference|
|BackupPath|TEXT(255)|NULL|User-defined backup directory|
|AutoBackupEnabled|BOOLEAN|DEFAULT 1|Enable daily backup|
|CreatedAt|DATETIME|DEFAULT CURRENT_TIMESTAMP|Record creation date|
#### **Table: PurchaseOrders**
| Column Name     | Data Type | Constraints                              | Description                  |
| --------------- | --------- | ---------------------------------------- | ---------------------------- |
| **PO_ID**       | INTEGER   | **Primary Key, Auto Increment**          | Unique PO number             |
| **BusinessID**  | INTEGER   | **Foreign Key → Businesses(BusinessID)** | Associated business          |
| **VendorID**    | INTEGER   | **Foreign Key → Vendors(VendorID)**      | Supplier                     |
| **PONumber**    | TEXT(50)  | UNIQUE                                   | Human-readable PO number     |
| **PODate**      | DATETIME  | DEFAULT CURRENT_TIMESTAMP                | Date created                 |
| **TotalAmount** | REAL      | DEFAULT 0                                | Aggregate of all item totals |
| **Status**      | TEXT(20)  | DEFAULT 'Pending'                        | Workflow status              |
| **Notes**       | TEXT(255) | NULL                                     | Optional remarks             |
| **CreatedAt**   | DATETIME  | DEFAULT CURRENT_TIMESTAMP                | Record timestamp             |
#### **Table: PurchaseOrderItems**

| Column Name   | Data Type | Constraints                             | Description           |
| ------------- | --------- | --------------------------------------- | --------------------- |
| **ItemID**    | INTEGER   | **Primary Key, Auto Increment**         | Line item ID          |
| **PO_ID**     | INTEGER   | **Foreign Key → PurchaseOrders(PO_ID)** | Linked purchase order |
| **ProductID** | INTEGER   | **Foreign Key → Products(ProductID)**   | Product reference     |
| **Quantity**  | INTEGER   | **NOT NULL, CHECK (Quantity > 0)**      | Quantity ordered      |
| **UnitPrice** | REAL      | **NOT NULL, CHECK (UnitPrice > 0)**     | Cost per item         |
| **TaxRate**   | REAL      | **NOT NULL**                            | GST rate              |
| **LineTotal** | REAL      | Computed (Quantity × UnitPrice)         | Total before tax      |
| **CreatedAt** | DATETIME  | DEFAULT CURRENT_TIMESTAMP               | Timestamp             |
#### **Table: Products**

| Column Name | Data Type | Constraints                 | Description             |
| ----------- | --------- | --------------------------- | ----------------------- |
| ProductID   | INTEGER   | Primary Key, Auto Increment | Product ID              |
| BusinessID  | INTEGER   | Foreign Key → Businesses    | Business ownership      |
| ProductName | TEXT(100) | NOT NULL                    | Product name            |
| SKU         | TEXT(30)  | UNIQUE                      | Stock Keeping Unit      |
| HSNCode     | TEXT(10)  | NULL                        | GST classification code |
| Category    | TEXT(50)  | NULL                        | Product category        |
| Price       | REAL      | NOT NULL                    | Selling price           |
| StockQty    | INTEGER   | DEFAULT 0                   | Available stock         |
| TaxRate     | REAL      | NOT NULL                    | GST rate                |
| CreatedAt   | DATETIME  | DEFAULT CURRENT_TIMESTAMP   | Added date              |
#### **Table: Invoices**

| Column Name   | Data Type | Constraints                 | Description           |
| ------------- | --------- | --------------------------- | --------------------- |
| InvoiceID     | INTEGER   | Primary Key, Auto Increment | Unique invoice number |
| BusinessID    | INTEGER   | Foreign Key → Businesses    | Associated business   |
| CustomerID    | INTEGER   | Foreign Key → Customers     | Linked customer       |
| InvoiceNumber | TEXT(50)  | UNIQUE                      | GST invoice number    |
| InvoiceDate   | DATETIME  | DEFAULT CURRENT_TIMESTAMP   | Invoice creation date |
| TotalAmount   | REAL      | NOT NULL                    | Gross invoice total   |
| TaxAmount     | REAL      | NOT NULL                    | GST portion           |
| NetAmount     | REAL      | NOT NULL                    | Amount after tax      |
| Status        | TEXT(20)  | DEFAULT 'Final'             | Draft/Final state     |
#### **Table: InvoiceItems**

|Column Name|Data Type|Constraints|Description|
|---|---|---|---|
|ItemID|INTEGER|Primary Key, Auto Increment|Line item ID|
|InvoiceID|INTEGER|Foreign Key → Invoices|Linked invoice|
|ProductID|INTEGER|Foreign Key → Products|Product reference|
|Quantity|INTEGER|NOT NULL|Quantity sold|
|UnitPrice|REAL|NOT NULL|Price per unit|
|TaxRate|REAL|NOT NULL|GST rate|
|SubTotal|REAL|Computed (Quantity * UnitPrice)|Line total before tax|
#### **Table: Taxes**

|Column Name|Data Type|Constraints|Description|
|---|---|---|---|
|TaxID|INTEGER|Primary Key, Auto Increment|Tax configuration ID|
|TaxName|TEXT(50)|UNIQUE|Name (e.g., CGST, SGST)|
|Rate|REAL|NOT NULL|Percentage rate|
|EffectiveFrom|DATETIME|NOT NULL|Start date for rate|
|Active|BOOLEAN|DEFAULT 1|Current applicability|


Each table enforces **foreign key integrity**, **unique constraints**, and **check conditions** for regulatory compliance with GST data standards.

**Derived from:**
	SRS 4.1.2–4.1.3 Database Requirements and Data Management
### **3.3 Data Security and Integrity**
#### **Transactional Control**
- **Entity Framework Core (EF Core)** is used as the ORM to manage database transactions.
    
- CRUD operations (Create, Read, Update, Delete) are wrapped in **transactional scopes** to ensure atomicity—either all operations succeed or none are applied.
    
- Referential integrity is maintained through **cascade updates/deletes** defined at the model level.
#### **Database Encryption**

- The SQLite database (`businesssuite.db`) will be stored as a **single encrypted file** using **AES-256 encryption** at the storage level.
    
- Encryption keys are **derived from the business credentials** and securely hashed using **BCrypt** to prevent exposure.
    
- Backup copies are also encrypted before export to comply with **data protection requirements**.
#### **Data Validation**

- EF Core enforces schema-level validation (lengths, nullability, foreign keys).
    
- Application-layer checks ensure **duplicate GSTIN**, **invalid tax rates**, and **broken relationships** are prevented before persistence.
#### **Integrity Maintenance**

- **Automatic backups** are generated at defined intervals and stored in user-specified directories.
    
- **Vacuum and re-indexing** routines are executed periodically to maintain query performance and database health.

**Derived from:**
	 SRS 4.1.3 _Data Management_ and 4.1.4 _Performance Expectation_
## **4. Detailed Module Design (LLD)**

### **4.1 Tax Calculation Logic**
**Objective:**  
Ensure 100% accurate GST computation and prevent compliance issues (Risk ID: **R2 – Incorrect GST Calculation**).

#### **Business Rule Summary:**
1. If **Seller State = Buyer State** → Apply **CGST + SGST**.
    
2. If **Seller State ≠ Buyer State** → Apply **IGST**.
    
3. Composition taxpayers → No GST applied (Bill of Supply).
    
4. Tax rounded to **2 decimal places**, total amount rounded to nearest rupee.

**Flowchart**
![[Flowchart.png]]
Pseudo-Code Example
if (business.Type == "Composition")
{
    cgst = sgst = igst = 0;
    invoice.Type = "Bill of Supply";
}
else
{
    if (seller.State == buyer.State)
    {
        cgst = taxRate / 2;
        sgst = taxRate / 2;
        igst = 0;
    }
    else
    {
        igst = taxRate;
        cgst = sgst = 0;
    }
    invoice.Type = "Tax Invoice";
}

invoice.TotalTax = Math.Round((cgst + sgst + igst) * subtotal / 100, 2);
invoice.TotalAmount = Math.Round(subtotal + invoice.TotalTax, 0);

**Derived from:**

- SRS 3.2.4.2 Functional Requirements: Tax Handling and Compliance
- SRS 4.4 Risk R2: Incorrect GST Calculation

### **4.2 User Authentication Module**

**Objective:**  
Provide secure login and credential management with hashed passwords, lockout after repeated failed attempts, and proper session control.

**Class Diagram**
![[Class Diagram.jpeg]]
**Authentication Flow(Sequence Diagram)**

User → AuthController: Submit LoginForm (username, password)
AuthController → AuthService: ValidateUser()
AuthService → Database: Retrieve User record
Database → AuthService: Return PasswordHash
AuthService → BCrypt: Verify(password, PasswordHash)
BCrypt → AuthService: True / False
AuthService → if failed:
      increment attemptCounter
      if attemptCounter >= 3 → LockAccount()
AuthService → if success:
      reset attemptCounter
      generate JWT/SessionToken
AuthService → AuthController: Login Success
AuthController → UI: Redirect to Dashboard

**Pseudo-Code snippet**
bool ValidateLogin(string username, string password)
{
    var user = db.Users.SingleOrDefault(u => u.Username == username);
    if (user == null) return false;

    if (user.IsLocked)
        throw new Exception("Account Locked");

    bool verified = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

    if (!verified)
    {
        user.FailedAttempts++;
        if (user.FailedAttempts >= 3)
        {
            user.IsLocked = true;
            db.SaveChanges();
            throw new Exception("Account locked after 3 failed attempts.");
        }
        db.SaveChanges();
        return false;
    }

    user.FailedAttempts = 0;
    db.SaveChanges();
    return true;
}
**Derived from:**
	SRS 3.2.1 _Business Registration and Login (Authentication)_
	Requirements for password hashing and 3-attempt lockout
### **4.3 Invoicing & Stock Update Flow**
**Objective:**  
Ensure that when an invoice is created, product stock updates immediately and tax calculations are applied consistently.

**Sequence Diagram (Textual Representation)**
![[Sequence.png]]
**Pseudo-Code**
public void CreateInvoice(InvoiceData data)
{
    decimal subtotal = data.Items.Sum(i => i.Quantity * i.UnitPrice);
    decimal totalTax = TaxCalculator.Calculate(data.BuyerState, data.SellerState, subtotal, data.TaxRate);

    var invoice = new Invoice
    {
        BusinessID = data.BusinessID,
        CustomerID = data.CustomerID,
        TotalAmount = subtotal + totalTax,
        TaxAmount = totalTax,
        CreatedAt = DateTime.Now
    };

    db.Invoices.Add(invoice);
    db.SaveChanges();

    foreach (var item in data.Items)
    {
        db.InvoiceItems.Add(new InvoiceItem
        {
            InvoiceID = invoice.InvoiceID,
            ProductID = item.ProductID,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        });

        // Update stock quantity
        var product = db.Products.Find(item.ProductID);
        product.StockQty -= item.Quantity;
    }

    db.SaveChanges();
}
#### **Postconditions**

- Each invoice is stored with its computed tax breakdown.
    
- Product stock quantity is decremented immediately.
    
- All actions occur within a **single EF Core transaction** to ensure atomicity.
    

 **Derived from:**
	  SRS 3.2.2.3 Product Management (Stock Handling)
	  SRS 3.2.4 Invoicing and Billing (GST Logic)
### **4.4 License Key Validation Module**

**Objective:**  
Prevent unauthorized installation or use of BusinessSuite by verifying license keys tied to the official distributor.

**Inputs:**

- User-entered License Key
    
- System Hardware ID (CPU, Disk Serial)
    

**Process Flow:**

1. Application startup triggers `CheckActivation()` in `LicenseService`.
    
2. If no valid activation found → open “License Activation Form”.
    
3. Validate key using internal algorithm or online service.
    
4. Generate activation signature: `SHA256(LicenseKey + HardwareID)`.
    
5. Store encrypted record in `Activation` table.
    

**Output:**  
Activation Status → Boolean (`Activated` / `Invalid`)

**Data Table:**

|Field|Type|Description|
|---|---|---|
|ActivationID|INTEGER|Primary Key|
|LicenseKeyHash|TEXT|Encrypted key hash|
|HardwareID|TEXT|Bound system ID|
|ActivatedAt|DATETIME|Timestamp of activation|
|IsValid|BOOLEAN|Activation status|

**Security:**

- AES-256 encryption used for LicenseKeyHash.
    
- License validation handled before loading main Dashboard.
    

**Derived From:**

- SRS 3.2.8 License Key and Activation System
    
- SRS 4.2.3 Licensing & IP
  
### **4.5 Initial Setup Wizard Module**

**Objective:**  
Guide the user through first-time business registration and configuration, enforcing single-business restriction.

**Flow Sequence:**

`App Start → Check if SetupComplete == false → Open SetupWizard → Collect Business Info → Save to DB → Set SetupComplete = true → Redirect to Login`

**Pseudo-code Example:**

`if (!db.Settings.Any(s => s.Key == "SetupComplete")) {     var setupForm = new SetupWizard();     setupForm.ShowDialog();     db.Settings.Add(new Setting { Key = "SetupComplete", Value = "true" });     db.SaveChanges(); }`

**Database Schema Update:**  
Add field in `Settings` table:

`ALTER TABLE Settings ADD COLUMN SetupComplete BOOLEAN DEFAULT 0;`

**Constraint (Business Table):**

`CREATE TRIGGER limit_single_business BEFORE INSERT ON Businesses WHEN (SELECT COUNT(*) FROM Businesses) >= 1 BEGIN     SELECT RAISE(ABORT, 'Only one business can be registered per installation'); END;`

**Derived From:**

- SRS 3.2.9 Initial Setup and Single Business Restriction
## **5. External Interface Design**

### **5.1 User Interface (UI) Components**
**Purpose:**  
List all core screens, dialogs, and controls required for WinForms implementation.

#### **Primary Application Windows**

1. **Login Screen**
    
    - Fields: Business ID, Password
        
    - Buttons: _Login_, _Forgot Password_
        
    - Features: Account lockout after 3 failed attempts
2. **Dashboard**
    
    - Menus: _Products_, _Invoices_, _Vendors_, _Customers_, _Reports_, _Settings_
        
    - Widgets: Sales Summary, Stock Alerts, Recent Invoices
3. **Product Management**
    
    - CRUD operations on products
        
    - Columns: ProductName, SKU, HSN, Category, TaxRate, Price, StockQty
        
    - Filters/Search bar
4. **Customer Management**
    
    - Add/Edit/Delete customer
        
    - Fields: Name, GSTIN, Contact, Billing/Shipping Address
5. **Vendor Management**
    
    - Similar to Customer module
        
    - Fields: VendorName, GSTIN, Contact, Address, State
6. **Invoice Generation**
    
    - Drop-downs: Customer, Products, TaxType
        
    - Buttons: _Add Item_, _Save Draft_, _Finalize & Print_
        
    - Auto-tax and total calculation
7. **Purchase Order Management**
    
    - Fields: Vendor, Product, Qty, TaxRate, Status
        
    - Actions: _Create PO_, _Mark Received_
8. **Reports and Analytics**
    
    - Tabs: _Sales_, _Purchase_, _Tax Summary_
        
    - Filters: Date Range, Customer/Vendor
        
    - Actions: _Generate_, _Export PDF_, _Export Excel_
9. **Settings**
    
    - Fields: Currency, DateFormat, Theme, BackupPath, AutoBackup toggle
10. **Backup/Restore Dialog**
    
    - Options: _Create Backup_, _Restore Database_
        
    - FilePicker control for .db files
**Derived from:**
	SRS 3.3.1 (User Interface Requirements)
### **5.2 Report Generation Design**
**Purpose:**  
Define the data fields, grouping, and calculations for all core reports to guide FastReport/iTextSharp implementation.
#### **Sales Report**

- **Source Tables:** Invoices, InvoiceItems, Customers
    
- **Columns:** InvoiceNo, Date, CustomerName, Subtotal, CGST, SGST, IGST, Total Amount, Status
    
- **Grouping:** Customer → Invoice Date
    
- **Summaries:** Total Invoices, Total Tax Collected, Total Revenue

| Field          | Source       | Calculation / Notes                             |
| -------------- | ------------ | ----------------------------------------------- |
| Invoice Number | Invoices     | Sequential GST invoice number                   |
| Date           | Invoices     | InvoiceDate (formatted per Settings.DateFormat) |
| Customer Name  | Customers    | Joined on CustomerID                            |
| Subtotal       | InvoiceItems | Σ(Quantity × UnitPrice)                         |
| CGST           | InvoiceItems | (TaxRate/2 × Subtotal)/100                      |
| SGST           | InvoiceItems | (TaxRate/2 × Subtotal)/100                      |
| IGST           | InvoiceItems | (TaxRate × Subtotal)/100 for interstate         |
| Total Amount   | Invoices     | NetAmount                                       |
| Status         | Invoices     | Final / Draft                                   |
#### **Purchase Report**

- **Source Tables:** PurchaseOrders, Vendors, Products
    
- **Columns:** PONumber, Date, Vendor, Product, Quantity, UnitPrice, Tax, Total, Status
    
- **Grouping:** Vendor → PO Date
    
- **Summaries:** Total Purchases, Total Tax Paid, Avg Cost/Vendor

|Field|Source|Calculation / Notes|
|---|---|---|
|PO Number|PurchaseOrders|Unique purchase order number|
|Vendor Name|Vendors|VendorID reference|
|Product|Products|ProductID reference|
|Quantity|PurchaseOrders|Ordered units|
|Unit Price|PurchaseOrders|Purchase cost per item|
|Tax|PurchaseOrders|TaxRate applied per item|
|Total|PurchaseOrders|(Quantity × UnitPrice) + Tax|
|Status|PurchaseOrders|Pending / Approved / Received|
#### **Tax Summary Report**

- **Source Tables:** Invoices, PurchaseOrders, Taxes
    
- **Columns:** TaxType, OutputTax (Sales), InputTax (Purchases), Net Payable Tax, Period
    
- **Grouping:** TaxType
    
- **Summaries:** Total Output Tax, Total Input Tax, Net Liability

|Field|Source|Calculation / Notes|
|---|---|---|
|Tax Type|Taxes|CGST / SGST / IGST|
|Output Tax|Invoices|Σ of TaxAmount (Sales)|
|Input Tax|PurchaseOrders|Σ of TaxAmount (Purchases)|
|Net Payable Tax|Derived|Output - Input|
|Period|User Input|Filter by Date Range|
#### **Export Options**

- PDF via **iTextSharp**
    
- Excel via **FastReport** or ClosedXML
    
- Expected Performance: ≤ 7 seconds/repor

**Derived from:**
	SRS 3.2.7.1 (Reports and Analytics Requirements)

## **6. Test and Quality Assurance**
### **6.1 Acceptance Criteria Mapping**

**Objective:**  
Define traceability between **SRS Functional Requirements (FRs)** and **System Test Cases (STCs)** to ensure that all major functions are verifiable and measurable during system validation.

The following matrix maps each **core FR (3.2.1–3.2.7)** from the SRS to its corresponding **test objective** and **acceptance criteria**.

| **SRS Ref** | **Functional Requirement**    | **Test Case ID** | **Test Objective / Description**                                                              | **Acceptance Criteria**                                                                        |
| ----------- | ----------------------------- | ---------------- | --------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| FR 3.2.1    | Business Registration & Login | STC-01           | Verify that a new business can register with valid GSTIN, business type, and contact details. | Registration completes successfully; BusinessID auto-generated; invalid GSTIN rejected.        |
| FR 3.2.1    | Authentication & Lockout      | STC-02           | Test login flow with valid/invalid credentials and 3-failed-attempt lockout mechanism.        | Login succeeds for valid user; account locks after 3 failed attempts; error message displayed. |
| FR 3.2.2    | Product Management            | STC-03           | Verify CRUD (Create, Read, Update, Delete) operations on product list.                        | All operations succeed; duplicate SKU/HSN entries prevented.                                   |
| FR 3.2.3    | Vendor & Customer Management  | STC-04           | Validate addition, search, and editing of vendors/customers.                                  | All entries persist correctly; duplicate GSTINs rejected.                                      |
| FR 3.2.4    | GST & Non-GST Invoicing       | STC-05           | Confirm invoice creation logic for Regular vs Composition businesses.                         | Invoice generated with correct tax logic; invoice number sequence validated.                   |
| FR 3.2.4.2  | Tax Handling & Compliance     | STC-06           | Test CGST/SGST/IGST logic and rounding accuracy.                                              | State comparison determines correct tax; tax values accurate within 0.01 tolerance.            |
| FR 3.2.5    | Purchase Order Management     | STC-07           | Ensure new PO can be created, updated, and linked to vendor and product.                      | PO created successfully; unique PONumber maintained; status transitions work.                  |
| FR 3.2.6    | Data Backup & Restore         | STC-08           | Validate data backup creation and restoration on local drive.                                 | Backup file successfully generated, encrypted, and restored without loss.                      |
| FR 3.2.7    | Reports & Analytics           | STC-09           | Validate report generation (Sales, Purchase, Tax Summary) for date ranges and filters.        | Reports generated in <7s; accurate totals; export to PDF/Excel successful.                     |
#### **Acceptance Validation Criteria**

- Each **STC must pass** with 100% compliance before acceptance sign-off.
    
- Tests will be executed on Windows 10 (base environment).
    
- Any failure in **Tax Logic** or **Data Integrity** tests blocks release until resolved.
    
- QA coverage tracked via **Requirement Traceability Matrix (RTM)** maintained in QA repository.

 **Derived from:**
	SRS 3.2.1–3.2.7 Functional Requirements

### **6.2 Unit Testing Focus**

**Objective:**  
Define critical modules requiring mandatory **unit test coverage** and specify tools and strategies for code-level verification.

#### **High-Priority Unit Testing Areas**
| **Module / Function**   | **Reason for Priority**     | **Testing Focus**                                                          | **Expected Coverage**     |
| ----------------------- | --------------------------- | -------------------------------------------------------------------------- | ------------------------- |
| Tax Calculation Engine  | Financial accuracy, Risk R2 | Validate CGST/SGST/IGST determination logic, rounding, and invoice totals. | ≥ 95% line coverage       |
| Authentication Service  | Security-sensitive          | Verify BCrypt hashing, password validation, and lockout thresholds.        | ≥ 90% branch coverage     |
| Invoice Creation Module | Transactional integrity     | Ensure atomic save of invoice + stock update.                              | ≥ 85% functional coverage |
| Database DAL Layer      | Data consistency            | Validate CRUD and constraint integrity through EF Core.                    | ≥ 80% coverage            |
| Backup/Restore          | Data recovery assurance     | Verify encrypted file export/import logic.                                 | ≥ 85% coverage            |
#### **Testing Tools**

- **xUnit / NUnit:** For .NET 8 unit testing framework.
    
- **Moq:** For mocking EF Core database contexts.
    
- **SQLite In-Memory DB:** For isolated, non-destructive database tests.
    
- **SonarQube / Coverlet:** For coverage analysis and code quality metrics.
    

#### **Sample Unit Test (Tax Calculation)**
[Fact]
public void ShouldApplyIGST_WhenBuyerAndSellerInDifferentStates()
{
    // Arrange
    var calc = new TaxCalculator();
    var subtotal = 1000m;
    var rate = 18m;

    // Act
    var result = calc.Calculate("Gujarat", "Maharashtra", subtotal, rate);

    // Assert
    Assert.Equal(180m, result.TotalTax); // IGST applied at 18%
    Assert.Equal(1180m, result.TotalAmount);
}
#### **Quality Gates**

- Builds must **fail** if coverage < 85%.
    
- All **tax logic tests** are treated as **regression blockers** due to their financial compliance nature (R2).
    
- **Peer code review** required before merge.

**Mitigates:**
	 **Risk R2 (Incorrect GST Calculation)** – validated through dedicated test coverage