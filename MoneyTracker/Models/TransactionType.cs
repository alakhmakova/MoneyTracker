namespace MoneyTracker.Models;

// Is it money in or money out. Each child of Transaction (Income/Expense) returns its own
public enum TransactionType
{
    Income,
    Expense
}
