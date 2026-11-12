using System.Data.Common;
using MoneyTracker.Exceptions;
using MoneyTracker.Models;

namespace MoneyTracker.Services;

// Holds the transaction list in memory and works with it: add, list, sort, search, edit, delete, totals.
// Does not print anything; errors are reported with exceptions.
public class TransactionService
{
    private readonly List<Transaction> _transactions = new();
    private readonly IdGenerator _ids = new();

    // true if the list has at least one transaction
    public bool HasTransactions => _transactions.Count > 0;

    // Next free id, to save in the file
    public int NextId => _ids.NextId;

    // Give the transaction the next id and add it to the list; throws ArgumentNullException if transaction is null
    public void AddTransaction(Transaction transaction)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }
        transaction.Id = _ids.GetNextId();
        _transactions.Add(transaction);
    }

    // Lists
    // All transactions sorted by id (also used for saving)
    public List<Transaction> GetAllTransactions()
    {
        return _transactions.OrderBy(t => t.Id).ToList();
    }
    // Only incomes / only expenses, sorted by id like GetAllTransactions.
    // Where (not OfType<Income>) keeps the type Transaction, so the result is List<Transaction> without Cast
    public List<Transaction> GetAllIncomes()
    {
        return _transactions.Where(t => t is Income).OrderBy(t => t.Id).ToList();
    }
    public List<Transaction> GetAllExpenses()
    {
        return _transactions.Where(t => t is Expense).OrderBy(t => t.Id).ToList();
    }

    // type == null -> all transactions; Income -> only incomes; Expense -> only expenses
    public List<Transaction> GetTransactionsByType(TransactionType? type)
    {
        if (type is TransactionType.Income)
        {
            return GetAllIncomes();
        }
        if (type is TransactionType.Expense)
        {
            return GetAllExpenses();
        }
        return GetAllTransactions();
    }

    // Throw TransactionNotFoundException if there is no transaction with this id
    public Transaction GetTransactionById(int id)
    {
        Transaction? transaction = _transactions.FirstOrDefault(t => t.Id == id); // null if not found
        if (transaction == null)
        {
            throw new TransactionNotFoundException($"Transaction with id {id} not found.");
        }
        return transaction;
    }

    // Remove the transaction with this id;
    // Throw TransactionNotFoundException if there is no transaction with this id
    public void DeleteTransaction(int id)
    {
        _transactions.Remove(GetTransactionById(id));
    }

    // Make all expenses of this category Uncategorized; called before the category is removed
    public void RemoveCategoryFromExpenses(int categoryId)
    {
        foreach (Expense expense in _transactions.OfType<Expense>().Where(e => e.CategoryId == categoryId))
        {
            expense.SetCategory(null);
        }
    }

    // Replaces the list with the loaded transactions and restores the id counter.
    // Throws ArgumentNullException if transactions is null
    public void LoadData(List<Transaction> transactions, int savedNextId)
    {
        if (transactions is null)
        {
            throw new ArgumentNullException(nameof(transactions));
        }
        _transactions.Clear(); // readonly forbids replacing the list, but its contents can still change
        _transactions.AddRange(transactions);
        int maxId = _transactions.Count > 0 ? _transactions.Max(t => t.Id) : 0;
        _ids.Restore(savedNextId, maxId);
    }

    // Totals and balance
    // Sum of the incomes in the given list
    public decimal GetTotalIncome(List<Transaction> items)
    {
        return items.OfType<Income>().Sum(i => i.SignedAmount);
    }

    // Sum of the expenses in the given list; negative, because SignedAmount of an expense is negative
    public decimal GetTotalExpenses(List<Transaction> items)
    {
        return items.OfType<Expense>().Sum(e => e.SignedAmount);
    }

    // Incomes minus expenses of the given list
    public decimal CalculateBalance(List<Transaction> items)
    {
        return items.Sum(i => i.SignedAmount);
    }

    // Edit: change the fields and return the updated transaction
    // (the category of an expense is changed with AddTransactionsToCategory, it works for one id too)
    public Transaction UpdateTransaction(int id, string name, DateTime date, decimal amount, string? description)
    {
        Transaction transaction = GetTransactionById(id);
        transaction.Update(name, date, amount, description);
        return transaction;
    }

    // Period: transactions from items with from <= Date <= to (both days included)
    public List<Transaction> FilterByPeriod(List<Transaction> items, DateTime from, DateTime to)
    {
        return items.Where(i => i.Date >= from && i.Date <= to).ToList();
    }

    // Period: date of the first and the last transaction in items (hint before the user enters the dates).
    // Throws ArgumentException if items is empty: without this check Min/Max would throw
    // InvalidOperationException ("Sequence contains no elements"), which doesn't say what went wrong
    public DateTime StartOfPeriod(List<Transaction> items)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("Transactions can't be empty", nameof(items));
        }
        return items.Min(i => i.Date);
    }

    public DateTime EndOfPeriod(List<Transaction> items)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("Transactions can't be empty", nameof(items));
        }
        return items.Max(i => i.Date);
    }

    // true if from <= to (the same date twice = one day).
    // Dates are NOT limited to StartOfPeriod..EndOfPeriod; empty result -> "No transactions for this period"
    public bool ValidateDateRange(DateTime from, DateTime to)
    {
        return from <= to;
    }

    // Puts the expenses with the given ids into the category; categoryId == null -> Uncategorized.
    // All ids are checked first, so the category is set for all expenses or for none of them.
    // If any expense id is not found, TransactionNotFoundException is thrown and nothing is changed
    public void AddTransactionsToCategory(List<int> ids, int? categoryId)
    {
        List<Expense> expenses = new();
        foreach (int id in ids)
        {
            Expense? expense = _transactions.OfType<Expense>().FirstOrDefault(e => e.Id == id);
            if (expense is null)
            {
                throw new TransactionNotFoundException($"Expense with id {id} not found.");
            }
            expenses.Add(expense);
        }
        foreach (Expense expense in expenses)
        {
            expense.SetCategory(categoryId);
        }
    }

    // Sort: take a list (all incomes, or incomes for a period...) and return it sorted.
    // descending = false -> A-Z / oldest first / smallest first; true -> the other way (teacher requirement).
    // ThenBy: order of the transactions with the same value (same date -> by id; same amount or name -> by date, oldest first)
    public List<Transaction> SortByDate(List<Transaction> items, bool descending)
    {
        if (descending)
        {
            return items.OrderByDescending(i => i.Date).ThenBy(i => i.Id).ToList();
        }
        return items.OrderBy(i => i.Date).ThenBy(i => i.Id).ToList();
    }

    // By Amount (always positive), not SignedAmount: "largest" means the biggest sum of money,
    // so in the expenses table the biggest expense (-8200.00) comes first, not the smallest one (-45.00)
    public List<Transaction> SortByAmount(List<Transaction> items, bool descending)
    {
        if (descending)
        {
            return items.OrderByDescending(i => i.Amount).ThenBy(i => i.Date).ToList();
        }
        return items.OrderBy(i => i.Amount).ThenBy(i => i.Date).ToList();
    }

    public List<Transaction> SortByName(List<Transaction> items, bool descending)
    {
        if (descending)
        {
            return items.OrderByDescending(i => i.Name).ThenBy(i => i.Date).ToList();
        }
        return items.OrderBy(i => i.Name).ThenBy(i => i.Date).ToList();
    }

    // Search: expenses of the category, sorted by date (oldest first); categoryId == null -> expenses without a category (Uncategorized).
    // Only expenses have categories. Returns List<Transaction>, so the result can be shown with TablePrinter.DisplayTransactionsTable
    public List<Transaction> SearchByCategory(int? categoryId)
    {
        return _transactions
            .OfType<Expense>()
            .Where(e => e.CategoryId == categoryId) // null == null -> true, so Uncategorized works without an extra check
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Id)
            .Cast<Transaction>()                    // List<Expense> can't be passed where List<Transaction> is expected
            .ToList();
    }
}
