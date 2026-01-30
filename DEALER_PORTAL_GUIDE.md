# Dealer Portal Module - Setup & Usage Guide

## Overview

The Dealer Portal is a comprehensive module within the TMT Inventory App that enables dealers to manage their own inventory, end customers, sales, and credit tracking. This is separate from the distributor-dealer relationship and focuses on dealer-to-end-customer operations.

## Features

### 1. **Dealer Authentication**
- Separate login system for dealers
- Secure password hashing (SHA-256)
- Session-based authentication
- Each dealer has their own isolated portal

### 2. **Product & Brand Management**
- **Product Categories**: Cement, Asbestos, TMT Bars
- **Multiple Brands**: Each category supports multiple brands
  - Cement: UltraTech, ACC, Ambuja, Shree Cement, Birla Shakti
  - Asbestos: Everest, Visaka, Ramco
  - TMT: TATA Tiscon, JSW NeoSteel, Kamdhenu, SAIL, Jindal Panther

### 3. **Inventory Management**
- Track stock for each product/brand combination
- Add stock with reference numbers
- Adjust stock levels
- Set minimum stock levels for alerts
- View transaction history
- Automatic stock deduction on sales

### 4. **End Customer Management**
- Add and manage end customers
- Track customer details (contact, address, GSTIN)
- Set credit limits per customer
- Track current outstanding amounts
- Customer ledger with full transaction history

### 5. **Sales & Invoicing**
- Create sales invoices for end customers
- Multi-item invoices with different products
- Automatic inventory deduction
- Track payment status (Pending, Partial, Paid)
- Record payments against invoices
- Support multiple payment methods (Cash, Cheque, Bank Transfer, UPI, Card)

### 6. **Credit Tracking**
- Real-time outstanding balance tracking
- Payment history
- Customer-wise credit analysis
- Automatic updates on sales and payments

### 7. **Dashboard & Reports**
- Key metrics at a glance
- Recent sales overview
- Pending payments list
- Low stock alerts
- Inventory value tracking

## Database Schema

### New Tables Created

1. **DealerUsers** - Authentication for dealer portal
2. **ProductCategories** - Product types (Cement, Asbestos, TMT)
3. **ProductBrands** - Brands within each category
4. **DealerInventoryItems** - Dealer's inventory stock
5. **DealerInventoryTransactions** - All inventory movements
6. **EndCustomers** - Dealer's customers
7. **CustomerSales** - Sales to end customers
8. **CustomerSaleItems** - Line items in sales
9. **CustomerPayments** - Payment records

## Setup Instructions

### Step 1: Create Database Migration

Run the following command in the Package Manager Console:

```powershell
Add-Migration AddDealerPortalTables
Update-Database
```

### Step 2: Seed Initial Data

You need to register the seeder service and run it. Add this to your `Program.cs`:

```csharp
using TmtInventoryApp.Services;

// After builder.Services configuration
builder.Services.AddScoped<DealerPortalSeeder>();

// After app.Build() and before app.Run()
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DealerPortalSeeder>();
    await seeder.SeedAsync();
}
```

### Step 3: Enable Session Support

Add session support in `Program.cs` if not already present:

```csharp
// Add before builder.Build()
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add after app.Build() and before app.UseAuthorization()
app.UseSession();
```

### Step 4: Create Dealer User Accounts

You can create dealer user accounts programmatically or via a setup script. Here's an example:

```csharp
// In a controller or setup script
var seeder = new DealerPortalSeeder(context);

// Create a dealer user for dealer ID 1
await seeder.CreateDealerUserAsync(
    dealerId: 1,
    username: "dealer1",
    password: "password123"
);
```

## Usage Guide

### For Distributors (Admin)

1. **Create Dealer Accounts**: Use the existing dealer management system
2. **Create Portal Access**: Use the seeder service to create dealer user accounts
3. **Share Credentials**: Provide dealers with their username and password

### For Dealers

#### 1. Login
- Navigate to: `/DealerPortal/DealerAuth/Login`
- Enter username and password
- Access the dealer dashboard

#### 2. Manage Inventory

**Add Stock:**
1. Go to "Manage Inventory" → "Add Stock"
2. Select product brand
3. Enter quantity and reference number
4. Submit to add stock

**Adjust Stock:**
1. Go to "Manage Inventory"
2. Click "Adjust" on any item
3. Enter new quantity
4. System creates adjustment transaction

**Set Alerts:**
1. Go to "Manage Inventory"
2. Click "Edit" on any item
3. Set minimum stock level and unit price
4. Save changes

#### 3. Manage Customers

**Add Customer:**
1. Go to "Manage Customers" → "Create"
2. Fill in customer details
3. Set credit limit (optional)
4. Submit

**View Customer Ledger:**
1. Go to "Manage Customers"
2. Click "Ledger" on any customer
3. View all sales and payments with running balance

#### 4. Create Sales

**New Sale:**
1. Go to "Sales & Invoices" → "Create"
2. Select customer
3. Add items (product, quantity, price)
4. System checks stock availability
5. Submit to create sale
6. Inventory automatically deducted
7. Customer outstanding updated

**Record Payment:**
1. Go to "Sales & Invoices"
2. Click "Record Payment" on any sale
3. Enter payment amount and method
4. Submit to record payment
5. Outstanding automatically updated

#### 5. Dashboard Insights

The dashboard shows:
- Total active customers
- Total outstanding amount
- Total inventory value
- Low stock alerts
- Recent sales
- Pending payments

## API Endpoints

### Authentication
- `GET /DealerPortal/DealerAuth/Login` - Login page
- `POST /DealerPortal/DealerAuth/Login` - Login action
- `GET /DealerPortal/DealerAuth/Logout` - Logout

### Dashboard
- `GET /DealerPortal/DealerDashboard` - Main dashboard

### Inventory
- `GET /DealerPortal/DealerInventory` - List inventory
- `GET /DealerPortal/DealerInventory/AddStock` - Add stock form
- `POST /DealerPortal/DealerInventory/AddStock` - Add stock action
- `GET /DealerPortal/DealerInventory/AdjustStock/{id}` - Adjust stock form
- `POST /DealerPortal/DealerInventory/AdjustStock/{id}` - Adjust stock action
- `GET /DealerPortal/DealerInventory/Transactions/{id}` - View transactions

### Customers
- `GET /DealerPortal/Customer` - List customers
- `GET /DealerPortal/Customer/Create` - Create customer form
- `POST /DealerPortal/Customer/Create` - Create customer action
- `GET /DealerPortal/Customer/Edit/{id}` - Edit customer form
- `POST /DealerPortal/Customer/Edit/{id}` - Edit customer action
- `GET /DealerPortal/Customer/Details/{id}` - Customer details
- `GET /DealerPortal/Customer/Ledger/{id}` - Customer ledger

### Sales
- `GET /DealerPortal/Sales` - List sales
- `GET /DealerPortal/Sales/Create` - Create sale form
- `POST /DealerPortal/Sales/Create` - Create sale action
- `GET /DealerPortal/Sales/Details/{id}` - Sale details
- `GET /DealerPortal/Sales/RecordPayment/{id}` - Record payment form
- `POST /DealerPortal/Sales/RecordPayment/{id}` - Record payment action

### Products
- `GET /DealerPortal/ProductManagement` - List categories
- `GET /DealerPortal/ProductManagement/Brands/{categoryId}` - List brands

## Security Features

1. **Session-based Authentication**: Dealers must login to access portal
2. **Data Isolation**: Each dealer can only see their own data
3. **Password Hashing**: Passwords stored as SHA-256 hashes
4. **CSRF Protection**: Anti-forgery tokens on all forms
5. **Authorization Checks**: All actions verify dealer ownership

## Customization

### Adding New Product Categories

```csharp
var category = new ProductCategory
{
    Name = "New Category",
    Description = "Description",
    UnitOfMeasurement = "Units",
    IsActive = true
};
_context.ProductCategories.Add(category);
await _context.SaveChangesAsync();
```

### Adding New Brands

```csharp
var brand = new ProductBrand
{
    Name = "New Brand",
    ProductCategoryId = categoryId,
    IsActive = true
};
_context.ProductBrands.Add(brand);
await _context.SaveChangesAsync();
```

## Troubleshooting

### Issue: Cannot login
- Verify dealer user account exists
- Check username/password are correct
- Ensure session is enabled in Program.cs

### Issue: Stock not deducting
- Verify inventory item exists for the product brand
- Check dealer has sufficient stock
- Review transaction logs

### Issue: Outstanding not updating
- Check customer sale was created successfully
- Verify payment amount is correct
- Review customer ledger for discrepancies

## Future Enhancements

Potential features to add:
1. SMS/Email notifications for low stock
2. Bulk import of sales data
3. Advanced reporting and analytics
4. Mobile-responsive design improvements
5. Export to Excel/PDF
6. Multi-currency support
7. Tax calculation (GST)
8. Barcode scanning for inventory
9. Customer loyalty programs
10. Integration with accounting software

## Support

For issues or questions, contact the development team or refer to the main application documentation.
