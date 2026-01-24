## 1. Introduction
#### 1.1 Purpose

The purpose of this Software Requirements Specification (SRS) document is to describe the functional and non-functional requirements of **BusinessSuite**.

This document provides a detailed overview of the system’s features, operations, and constraints to ensure all stakeholders have a clear understanding of the system to be developed.  

The primary goal of **BusinessSuite** is to offer small and medium-sized businesses a comprehensive platform to manage business operations such as invoicing, product and vendor management, purchase orders, tax handling, and compliance with GST regulations in India.

#### 1.2 Intended Audience

- **Software Developers and Engineers:** To understand the system’s architecture, functional modules, and design expectations for implementation.
    
- **Project Managers and Analysts:** To plan, monitor, and ensure the software development aligns with business and regulatory goals.
    
- **Testers and Quality Assurance Team:** To prepare test cases and validate functionalities based on the defined requirements.
    
- **End Users (Business Owners / Accountants):** To gain an overview of the product features and capabilities.
    
- **Stakeholders and Investors:** To evaluate the system’s business scope, objectives, and potential market impact.

#### 1.3 Intended Use

The **BusinessSuite** application is designed for businesses to:

- Register and manage their business profiles.
    
- Handle **product, vendor, and customer data**.
    
- Generate **GST and Non-GST invoices** in compliance with Indian taxation laws.
    
- Manage **purchase orders**, tax calculations, and maintain records for accounting and filing.
    
- Support both **Regular GST** and **Composition GST** users.
    
- *(In future versions)* Provide secure login and data management with future scalability for **multi-user role-based access**.

#### 1.4 Product Scope

**BusinessSuite** aims to serve as a complete business management system that simplifies daily business operations and ensures compliance with GST rules.  
Key objectives include:

- Automate invoicing, tax calculations, and reporting.
    
- Maintain an organized record of sales, purchases, and inventory.
    
- Reduce manual errors in accounting and tax management.
    
- Offer flexibility for future features like **role-based access control**, **multi-branch management**, and **cloud synchronization**.  
    The system will initially be developed as a **desktop application** with a **local database** (SQLite or SQL Server Express), with the potential to expand into a cloud-enabled solution in future releases.

#### 1.5 Definitions and Acronyms

| Term / Acronym | Definition                                                                                           |
| -------------- | ---------------------------------------------------------------------------------------------------- |
| **GST**        | Goods and Services Tax – A unified indirect tax applicable in India.                                 |
| **CGST**       | Central Goods and Services Tax – Tax collected by the Central Government on intra-state supplies.    |
| **SGST**       | State Goods and Services Tax – Tax collected by the State Government on intra-state supplies.        |
| **IGST**       | Integrated Goods and Services Tax – Tax on inter-state supplies collected by the Central Government. |
| **ERP**        | Enterprise Resource Planning – A type of business management software.                               |
| **SRS**        | Software Requirements Specification.                                                                 |
| **UI**         | User Interface.                                                                                      |
| **CRUD**       | Create, Read, Update, Delete – Basic data manipulation operations.                                   |
| **SQLite**     | Lightweight relational database engine for local data storage.                                       |

#### 1.6 Development Approach
![[Waterfall.canvas]]
The development of the _BusinessSuite Desktop Application_ will follow a **Waterfall Model with Incremental Delivery**.  
This model ensures a structured and predictable process from requirement gathering through deployment, while allowing the delivery of functional modules (e.g., Authentication, Product Management, Invoicing, Reporting) in successive increments.

Each major phase—**Requirements, Design, Implementation, Testing, and Deployment**—will be completed and verified before the next begins.  
Incremental delivery enables early validation of completed modules without disrupting the overall project flow.

**Advantages of this approach:**

- Clear documentation and phase boundaries.
    
- Early feedback through incremental module delivery.
    
- Easier tracking of progress and quality assurance.
    
- Lower integration risk and predictable scheduling.
## 2. Overall Description

#### 2.1 User Needs

The **BusinessSuite** system is designed to address the core operational and compliance needs of small and medium-sized businesses in India that require efficient management of billing, inventory, and taxation.  
The application provides an intuitive desktop interface allowing users to perform day-to-day business activities without relying on external accounting or ERP systems.

##### Key User Needs

1. **Business Registration & Authentication**
    
    - Businesses should be able to register with their GST details and securely log in using a unique business ID and password.
        
2. **Product and Inventory Management**
    
    - Users need to easily add, edit, delete, and categorize products with details like HSN code, tax rate, stock quantity, and pricing.
        
3. **Vendor and Customer Management**
    
    - The system should allow maintaining records of suppliers and customers for quick access during invoice or purchase order creation.
        
4. **GST & Non-GST Invoicing**
    
    - Users must be able to generate both GST-compliant and non-GST invoices automatically applying correct tax logic (CGST, SGST, IGST) based on the business type and transaction type.
        
5. **Purchase Order Management**
    
    - Businesses should be able to create, modify, and track purchase orders with tax applicability.
        
6. **Tax Handling and Compliance**
    
    - The system should calculate taxes automatically and allow users to manage both **Regular** and **Composition** GST schemes.
        
7. **Data Storage and Backup**
    
    - Users require a reliable system to store their data locally with backup and restore functionality.
        
8. **Reports and Analytics**
    
    - Businesses need access to sales, purchase, and tax summary reports to support decision-making and filing requirements.
        
9. **Scalability for Future Enhancements**
    
    - The software should support future modules such as role-based access (staff management), multi-branch control, and cloud synchronization.

#### 2.2 Assumptions and Dependencies

##### Assumptions

1. Users have **basic computer knowledge** and familiarity with GST invoicing concepts.
    
2. The system will be installed on **Windows-based desktops or laptops**.
    
3. Internet access may not be required for day-to-day operations (local database support).
    
4. The initial version will use **SQLite** as the local database engine.
    
5. Each business will have a **unique Business ID** for registration and login.
    
6. Tax rates and rules will be **entered or updated manually** by the user if GST rates change (future versions may support auto-updates).
    
7. The application will follow the **Indian GST regulatory framework** and business requirements.
    

##### Dependencies

1. **.NET Framework / .NET 8 Runtime:** Required for running the WinForms application.
    
2. **Database Engine (SQLite):** Used for storing business data locally.
    
3. **Third-Party Libraries:**
    
    - **Entity Framework Core** for ORM (data access)
        
    - **PDF Generator (e.g., iTextSharp or QuestPDF)** for invoice creation
        
    - **Reporting Tool (e.g., FastReport / Crystal Reports)** for analytics and print layouts
        
4. **Windows OS Environment:** Application compatibility depends on supported Windows versions (Windows 10 and above).
    
5. **External Printers / PDF Drivers:** Needed for printing or exporting invoices and reports.
    
6. **Future Cloud Integration:** Optional dependency on APIs or web services for synchronization and online updates.
## 3. System Features and Requirements

#### 3.1 Overview

The **BusinessSuite** system provides a set of modules that collectively manage business operations such as product inventory, customer and vendor records, invoicing, purchase orders, and taxation.  
This section describes the **key system features**, their **functional and non-functional requirements**, and the **interfaces** that connect the system to external entities.

#### 3.2 System Features

##### 3.2.1 Business Registration and Login

**Description:**  
Allows a business to register its profile, set up a unique business ID, and log in securely.

**Functional Requirements:**

1. The system shall allow a business to register with details such as name, GSTIN, address, contact number, and business type (Regular / Composition).
    
2. The system shall generate a unique Business ID during registration.
    
3. The system shall authenticate users based on their Business ID and password.
    
4. The system shall validate GSTIN format for Regular GST users.
    
5. The system shall encrypt and securely store user passwords.

**Non-Functional Requirements:**

- Authentication must complete within **2 seconds** under normal system load.
    
- Passwords must be hashed using a **secure algorithm (e.g., BCrypt)**.
    
- The system shall lock a user account after **three failed login attempts**.

##### 3.2.2 Product Management

**Description:**  
Manages products and services offered by the business.

**Functional Requirements:**

1. The system shall allow users to **add, edit, delete, and view** products.
    
2. Each product shall include details such as name, SKU, HSN code, category, tax rate, price, and stock quantity.
    
3. The system shall update stock quantities automatically based on sales and purchase entries.
    
4. The system shall validate HSN codes for GST-registered businesses.

**Non-Functional Requirements:**

- CRUD operations must respond within **3 seconds**.
    
- Product data must persist in the local database even after application restarts.
    
- The interface should display at least **50 records per page** with smooth scrolling.

##### 3.2.3 Vendor and Customer Management

**Description:**  
Enables users to manage their supplier and customer details.

**Functional Requirements:**

1. The system shall allow adding, editing, and deleting vendor and customer records.
    
2. Each record shall store name, GSTIN (if applicable), contact info, and billing/shipping address.
    
3. The system shall support searching and filtering by name or GSTIN.

**Non-Functional Requirements:**

- Records should load within **2 seconds**.
    
- All contacts must be validated for duplicate GSTIN or mobile numbers.

##### 3.2.4 Invoicing and Billing (GST & Non-GST)

**Description:**  
Generates GST-compliant and non-GST invoices based on user type.

**Functional Requirements:**

1. The system shall support both **Tax Invoices (Regular GST)** and **Bill of Supply (Composition / Non-GST)**.
    
2. The system shall calculate **CGST, SGST, and IGST** automatically based on the buyer’s and seller’s states.
    
3. The system shall allow users to preview, print, and export invoices as **PDF**.
    
4. Each invoice shall include unique invoice numbering as per GST norms.
    
5. The system shall allow editing of draft invoices before finalization.

**Non-Functional Requirements:**

- Invoice generation and PDF export should complete within **5 seconds**.
    
- Generated invoices must be **digitally accurate and readable** in standard A4 format.
    
- The system must ensure data consistency between invoice and stock modules.

##### 3.2.5 Purchase Order Management

**Description:**  
Allows creation and management of purchase orders (PO) for procurement.

**Functional Requirements:**

1. The system shall allow users to create, view, and modify purchase orders.
    
2. The PO shall include vendor details, product items, quantities, and taxes.
    
3. The system shall generate a unique PO number automatically.
    
4. The system shall allow marking POs as “Pending,” “Approved,” or “Received.”

**Non-Functional Requirements:**

- System should display purchase order history within **3 seconds**.
    
- Each PO must be traceable and linked to vendor and product records.

##### 3.2.6 Tax Management

**Description:**  
Handles various tax configurations and ensures correct application during billing.

**Functional Requirements:**

1. The system shall support configuration of GST rates for each product or category.
    
2. The system shall handle multiple tax types (CGST, SGST, IGST, Cess).
    
3. The system shall automatically determine applicable tax type based on transaction type.
    
4. The system shall allow businesses to modify tax rates as per updated regulations.

**Non-Functional Requirements:**

- Tax calculations must execute with **100% accuracy**.
    
- All tax data must be stored securely for compliance purposes.

##### 3.2.7 Reports and Analytics

**Description:**  
Provides business insights and summaries.

**Functional Requirements:**

1. The system shall generate **Sales Reports**, **Purchase Reports**, and **Tax Summaries**.
    
2. Users shall be able to filter reports by **date range, customer, or product**.
    
3. Reports shall be exportable in **PDF or Excel** formats.

**Non-Functional Requirements:**

- Report generation should not exceed **7 seconds**.
    
- Data visualization should remain clear and legible in printed form.
  
##### 3.2.8 License Key and Activation System

**Description:**  
Ensures that the BusinessSuite software can only be activated using a valid security key distributed officially. This prevents unauthorized installations or copies.

**Functional Requirements:**

1. The system shall require entry of a license key during the first launch after installation.
    
2. The system shall verify the key using an internal checksum or online validation API.
    
3. Once validated, the key shall be bound to the system’s hardware ID (e.g., CPU + disk serial).
    
4. The application shall deny access to all modules until successful activation.
    
5. The license information shall be encrypted and stored securely in the local database.
    
6. If the license key is invalid or reused, the activation process shall fail with a clear error message.
**Non-Functional Requirements:**

- License validation must complete within 2 seconds.
    
- The key validation logic must use AES or RSA-based encryption for security.
    
- The system shall prevent any modifications to license data through database encryption.
##### 3.2.9 Initial Setup and Single Business Restriction

**Description:**  
During the first launch, the application guides the user through one-time business registration and configuration. Once completed, no additional business can be registered in the same installation.

**Functional Requirements:**

1. The application shall detect first-time usage (empty database) and trigger the Setup Wizard.
    
2. The Setup Wizard shall capture business information, GST details, and preferences.
    
3. The system shall mark `isSetupComplete = true` once configuration is done.
    
4. The system shall prevent adding multiple businesses in the same installation.
    
5. If the user attempts to register a second business, the system shall display a “Single Business Instance Only” warning.
**Non-Functional Requirements:**

- Setup should complete within 5 minutes of guided configuration.
    
- Data entered during setup shall be validated for correctness and stored securely.
    
- Once setup is completed, the wizard cannot reappear unless the database is reset.
#### 3.3 External Interface Requirements

##### 3.3.1 User Interface (UI)

- The application will use **C# WinForms** as the presentation layer.
    
- The design must be **user-friendly**, with consistent navigation and form layouts.
    
- UI will include **menus, data grids, dropdowns, and buttons** for core operations.
    
- All input fields must validate user input (e.g., GSTIN format, numeric fields).
    

##### 3.3.2 Hardware Interfaces

- **Minimum Requirements:**
    
    - Processor: Intel i3 or equivalent
        
    - RAM: 4 GB
        
    - Disk Space: 500 MB free
        
    - Display: 1366×768 resolution
        
    - OS: Windows 10 or later
        
    - Printer: Optional (for invoice printing)
        

##### 3.3.3 Software Interfaces

- **.NET Runtime:** .NET 6 / .NET 8 required for execution.
    
- **Database:** SQLite or SQL Server Express (local).
    
- **PDF/Reporting:** iTextSharp / FastReport for document generation.
    
- **Authentication Library:** BCrypt.Net for password hashing.
    

##### 3.3.4 Communication Interfaces

- In the initial version, **no internet connection is required** (local setup).
    
- Future versions may include:
    
    - RESTful API integration for cloud sync.
        
    - Email or SMS gateways for sending invoices and alerts.
## 4. Other Requirements

#### 4.1 Database Requirements

##### 4.1.1 Database Selection

The system will utilize **SQLite** as its primary database engine due to its **lightweight, file-based structure**, which is ideal for standalone desktop applications.  
SQLite requires no dedicated server installation and provides robust support for **transactions, indexing, and data integrity**, making it suitable for small and medium-sized business environments.

##### 4.1.2 Database Structure

The database will include the following primary tables (tentative):

| Table Name         | Description                                                                         |
| ------------------ | ----------------------------------------------------------------------------------- |
| **Businesses**     | Stores business registration details such as Business ID, GSTIN, name, and address. |
| **Users**          | Contains login credentials and user information.                                    |
| **Products**       | Stores product details such as SKU, HSN code, price, and tax rate.                  |
| **Customers**      | Contains customer contact and billing information.                                  |
| **Vendors**        | Contains supplier details for purchase and tax purposes.                            |
| **Invoices**       | Stores sales invoices, linked to customers and products.                            |
| **InvoiceItems**   | Holds product line items for each invoice.                                          |
| **PurchaseOrders** | Manages purchase transactions and vendor relationships.                             |
| **Taxes**          | Maintains GST and other applicable tax details.                                     |
| **Settings**       | Holds user preferences and system configuration.                                    |

##### 4.1.3 Data Management

- Data will be stored in a **single encrypted `.db` file** to ensure portability and security.
    
- Automatic **backup and restore** features will be provided to prevent data loss.
    
- SQLite constraints such as **FOREIGN KEY** and **UNIQUE** will be enforced to maintain referential integrity.
    
- Entity Framework Core will serve as the **Object Relational Mapper (ORM)** for database access and schema migrations.

- Data will be stored in a **single encrypted `.db` file** to ensure portability and security.
    
- Automatic **backup and restore** features will be provided to prevent data loss.
    
- SQLite constraints such as **FOREIGN KEY** and **UNIQUE** will be enforced to maintain referential integrity.
    
- Entity Framework Core will serve as the **Object Relational Mapper (ORM)** for database access and schema migrations.
##### 4.1.4 Performance Expectations

- Read and write operations should execute in under **100ms** under normal usage.
    
- Database file size should remain under **1 GB** for optimal performance.
    
- Periodic database vacuuming and indexing will be implemented to maintain speed.
#### 4.2 Legal and Regulatory Requirements

##### 4.2.1 GST Compliance

- The system must comply with the **Goods and Services Tax (GST)** regulations of India.
    
- All tax calculations, invoice formats, and numbering must align with the **Central Board of Indirect Taxes and Customs (CBIC)** guidelines.
    
- The system will clearly label invoices as **Tax Invoice** (for Regular GST users) or **Bill of Supply** (for Composition or Non-GST users).
    

##### 4.2.2 Data Protection

- The system shall not share business or customer data externally without user consent.
    
- All sensitive information such as passwords will be **hashed** before storage.
    
- Database backups will be stored in user-defined directories to comply with **data privacy** standards.
    

##### 4.2.3 Licensing and Intellectual Property

- The software will be distributed under a valid commercial or enterprise license.
    
- Any third-party libraries (e.g., iTextSharp, Entity Framework Core) will be used in accordance with their respective open-source or commercial licenses.
    
- The application includes a built-in license key activation system to prevent unauthorized use or distribution
#### 4.4 Risk Management (FMEA Matrix)

The following **Failure Mode and Effects Analysis (FMEA)** matrix identifies potential risks in the system, their impacts, likelihood, and mitigation strategies.

| **Risk ID** | **Risk Description**                     | **Effect / Impact**             | **Severity (S)** | **Likelihood (L)** | **Risk Priority (S×L)** | **Mitigation Strategy**                                            |
| ----------- | ---------------------------------------- | ------------------------------- | ---------------- | ------------------ | ----------------------- | ------------------------------------------------------------------ |
| R1          | Database file corruption                 | Loss of business data           | 9                | 3                  | 27                      | Implement auto-backup and recovery options                         |
| R2          | Incorrect GST calculation                | Financial and compliance issues | 8                | 4                  | 32                      | Validate tax logic with test data; maintain tax configuration file |
| R3          | Unauthorized access                      | Data theft or misuse            | 9                | 3                  | 27                      | Use password hashing (BCrypt), optional PIN-based login            |
| R4          | Application crash                        | User frustration, data loss     | 7                | 4                  | 28                      | Implement try-catch blocks, auto-save drafts                       |
| R5          | Hardware failure                         | Data inaccessibility            | 8                | 2                  | 16                      | Enable regular backups to external drives                          |
| R6          | Incorrect invoice numbering              | Regulatory non-compliance       | 6                | 3                  | 18                      | Validate sequence on each invoice generation                       |
| R7          | Software piracy                          | Revenue loss                    | 5                | 4                  | 20                      | Implement machine-based license validation                         |
| R8          | Performance degradation (large data)     | Slow UI and delays              | 6                | 4                  | 24                      | Optimize queries, apply indexing, and limit record loads           |
| R9          | User data deletion by mistake            | Loss of important records       | 7                | 2                  | 14                      | Add confirmation prompts and recycle bin feature                   |
| R10         | Version incompatibility (future updates) | System instability              | 5                | 3                  | 15                      | Maintain migration scripts and versioning control                  |
 **Severity Scale (S):** 1 = Minor, 10 = Critical  
 **Likelihood Scale (L):** 1 = Rare, 10 = Frequent

## 5. Appendices

#### 5.1 Glossary
| **Term / Acronym**                  | **Definition**                                                                                                                                                                 |     |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --- |
| **BusinessSuite**                   | The proposed C# WinForms desktop application designed for business management, GST billing, and tax compliance.                                                                |     |
| **GST**                             | Goods and Services Tax – India’s indirect tax system applied on goods and services.                                                                                            |     |
| **CGST**                            | Central Goods and Services Tax – Collected by the Central Government on intra-state transactions.                                                                              |     |
| **SGST**                            | State Goods and Services Tax – Collected by the State Government on intra-state transactions.                                                                                  |     |
| **IGST**                            | Integrated Goods and Services Tax – Applied on inter-state transactions.                                                                                                       |     |
| **Composition Scheme**              | A simplified GST scheme for small taxpayers with limited turnover, involving lower tax rates and simpler compliance.                                                           |     |
| **Tax Invoice**                     | Invoice format applicable to Regular GST taxpayers.                                                                                                                            |     |
| **Bill of Supply**                  | Invoice format applicable to Composition taxpayers or Non-GST entities.                                                                                                        |     |
| **SQLite**                          | Lightweight, serverless SQL database used for local storage.                                                                                                                   |     |
| **ORM**                             | Object-Relational Mapper – A programming technique for converting data between incompatible systems using object-oriented programming languages (e.g., Entity Framework Core). |     |
| **CRUD**                            | Create, Read, Update, Delete – The four basic operations for managing data.                                                                                                    |     |
| **FMEA**                            | Failure Mode and Effects Analysis – Risk management technique used to identify potential failures in a system.                                                                 |     |
| **UI/UX**                           | User Interface / User Experience – Design principles focused on usability and appearance of the software.                                                                      |     |
| **Entity Framework Core (EF Core)** | ORM framework used in .NET for interacting with relational databases.                                                                                                          |     |
| **PDF (Portable Document Format)**  | A file format used to present documents in a manner independent of application software and hardware.                                                                          |     |
| **API**                             | Application Programming Interface – Used for communication between software components (future use for cloud integration).                                                     |     |
#### 5.2 Use Cases and Diagrams

##### 5.2.1 Use Case Overview
The following primary use cases define the core interactions between the user (business owner or staff) and the **BusinessSuite** system.

| **Use Case ID** | **Use Case Name**       | **Actor(s)**           | **Description**                                                                 |
| --------------- | ----------------------- | ---------------------- | ------------------------------------------------------------------------------- |
| UC-01           | Business Registration   | Business Owner         | Register a new business with GST and contact details.                           |
| UC-02           | Login                   | Business Owner / Staff | Authenticate and access the system using Business ID and password.              |
| UC-03           | Manage Products         | Business Owner / Staff | Add, update, delete, and list products with tax details.                        |
| UC-04           | Manage Customers        | Business Owner / Staff | Add and maintain customer information.                                          |
| UC-05           | Create Invoice          | Business Owner / Staff | Generate GST or Non-GST invoices with tax calculation and print/export options. |
| UC-06           | Manage Purchase Orders  | Business Owner / Staff | Create and track purchase orders linked to vendors.                             |
| UC-07           | Manage Taxes            | Business Owner         | Configure and update applicable tax rates.                                      |
| UC-08           | Generate Reports        | Business Owner         | Generate and export sales, purchase, and tax summary reports.                   |
| UC-09           | Backup and Restore Data | Business Owner         | Export or import local database backups for safety.                             |
##### 5.2.2 Use Case Diagram 

![[Usecase.png]]

**Actors:**

- Business Owner (Primary User)
    
- Staff (Future enhancement)
    
- System (BusinessSuite Application)
    
- Database (SQLite)    

**Main Flow:**

	Business Owner --> [Login] --> [Dashboard]
	Dashboard --> [Manage Products]
	Dashboard --> [Manage Customers]
	Dashboard --> [Create Invoice]
	Dashboard --> [Purchase Orders]
	Dashboard --> [Reports]
	Dashboard --> [Settings / Tax Configurations]

**Description:**

1. The **Business Owner** logs into the system using Business ID and password.
    
2. Once authenticated, the **Dashboard** provides access to various modules.
    
3. Users can manage products, vendors, and customers.
    
4. When creating an invoice, the system automatically calculates applicable GST.
    
5. Users can print, export, or email invoices as PDFs.
    
6. Periodic reports can be generated for sales, purchases, and taxes.
    
7. Backup and restore features allow safe data management.
    

_(In your final documentation, this can be drawn using UML notation in tools like Draw.io or Visual Paradigm.)_

#### 5.3 To Be Determined (TBD) List

| **Item**                            | **Description**                                  | **TBD Decision Point**                      |
| ----------------------------------- | ------------------------------------------------ | ------------------------------------------- |
| **Multi-User Role Access**          | Staff and role-based access system               | Planned for Version 2.0                     |
| **Cloud Synchronization**           | Integration with web API for multi-location use  | Under evaluation                            |
| **E-Invoice and QR Code Support**   | Integration for GST e-invoicing compliance       | Pending government API access               |
| **Automatic GST Rate Update**       | Real-time GST rate fetch from government sources | Future enhancement                          |
| **Email/SMS Integration**           | Send invoices and payment reminders              | To be added post Phase 1 release            |
| **Payment Gateway Integration**     | Allow online payments for invoices               | To be integrated with Razorpay/Paytm API    |
| **Multi-Language Interface**        | Support for Indian regional languages            | Planned for future localization             |
| **License Key / Activation System** | Software protection and activation feature       | To be implemented before commercial release |
| **Mobile Companion App**            | Android/iOS app for basic invoice management     | Future development                          |
| **Cloud Backup**                    | Automatic daily cloud-based backup               | Future enhancement                          |
