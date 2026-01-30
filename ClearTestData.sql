-- Clear All Test Data Script
-- This script removes all transactional data while preserving:
-- - TmtVariants (seed data)
-- - Users (login accounts)

-- Disable foreign key constraints temporarily
PRAGMA foreign_keys = OFF;

-- Delete in order to respect foreign key relationships
DELETE FROM ActivityLogs;
DELETE FROM PurchaseRecordItems;
DELETE FROM PurchaseRecords;
DELETE FROM VirtualStockTransactions;
DELETE FROM VirtualStocks;
DELETE FROM BillingRecordItems;
DELETE FROM BillingRecords;
DELETE FROM SalesOrderItems;
DELETE FROM SalesOrders;
DELETE FROM Dealers;
DELETE FROM OpeningDealerBalances;
DELETE FROM StockEntries;

-- Reset sequences
DELETE FROM sqlite_sequence WHERE name IN ('ActivityLogs', 'PurchaseRecords', 'PurchaseRecordItems', 'VirtualStocks', 'VirtualStockTransactions', 'BillingRecords', 'BillingRecordItems', 'SalesOrders', 'SalesOrderItems', 'StockEntries', 'OpeningDealerBalances', 'Dealers');

-- Re-enable foreign key constraints
PRAGMA foreign_keys = ON;

-- Vacuum to reclaim space
VACUUM;

SELECT 'Test data cleared successfully!' AS Result;
