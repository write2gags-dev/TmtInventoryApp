# Virtual Stock (Shymasteel) Implementation

## Overview
A virtual stock tracking system has been added to track Shymasteel company's stock. This virtual stock is automatically deducted when billing records are created.

## Key Features

### 1. Virtual Stock Management
- **Location**: Navigation menu → Virtual Stock
- **Purpose**: Track virtual stock maintained by Shymasteel company
- **Auto-deduction**: Stock is automatically deducted when billing is done

### 2. Database Tables
- **VirtualStock**: Stores available weight for each TMT variant
  - TmtVariantId
  - AvailableWeightKg (stored in database)
  - AvailableWeightTons (calculated property for display)
  - LastUpdated

- **VirtualStockTransaction**: Tracks all stock movements
  - TransactionType (Addition/Deduction/Adjustment)
  - WeightKg
  - ReferenceType (e.g., "BillingRecord", "ManualAdjustment")
  - ReferenceId
  - Notes
  - TransactionDate

### 3. Automatic Integration with Billing

#### When Creating Billing Record:
1. Billing record is saved
2. Virtual stock is automatically deducted for each billed item
3. Transaction is recorded with reference to billing record

#### When Editing Billing Record:
1. Old billing items are restored to virtual stock
2. Billing record is updated
3. New billing items are deducted from virtual stock
4. Both restoration and deduction transactions are recorded

#### When Deleting Billing Record:
1. Billed weight is restored to virtual stock
2. Transaction is recorded
3. Billing record is deleted

### 4. Manual Stock Adjustment
- Navigate to Virtual Stock → Click "Adjust" on any variant
- Enter new weight in KG
- Add notes explaining the adjustment
- System automatically calculates and records the difference

### 5. Transaction History
- View complete history of all stock movements
- See additions, deductions, and adjustments
- Track which billing records affected the stock
- Color-coded for easy identification:
  - Green: Additions
  - Red: Deductions
  - Yellow: Adjustments

### 6. Stock Status Indicators
- **Red row**: Negative stock (more billed than available)
- **Yellow row**: Zero stock
- **Normal row**: Positive stock available

### 7. Physical Stock Synchronization
- **Bundles**: Adjusting virtual stock bundles updates the physical stock bundles.
- **Weight**: Adjusting virtual stock weight updates the physical stock weight.
- Note: This ensures that corrections made to the Virtual Stock (Book Stock) are reflected in the Physical Stock inventory.

## Usage Instructions

### Initial Setup:
1. Navigate to Virtual Stock
2. Click "Initialize All Variants" to create entries for all TMT variants
3. Adjust each variant's stock to match actual Shymasteel stock levels

### Daily Operations:
- When you create a billing record, virtual stock is automatically deducted
- Monitor virtual stock levels regularly
- Adjust stock when Shymasteel updates their inventory
- View transaction history to audit stock movements

### Important Notes:
- Virtual stock can go negative (indicates over-billing)
- All transactions are logged for audit purposes
- Stock adjustments require notes for accountability
- Weight can be entered in Tons (billing) or KG (virtual stock)
