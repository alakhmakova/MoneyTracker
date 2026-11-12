using MoneyTracker.Models;
using MoneyTracker.Services;

namespace MoneyTracker.UI;

// Texts used by several UI classes: header, success messages after create/edit/remove, totals text.
// One place, so ConsoleUI, TransactionMenu and CategoryMenu don't have their own copies
public class ConsoleMessages
{
    private readonly TransactionService _transactionService;
    private readonly MoneyDataService _dataService;

    public ConsoleMessages(TransactionService transactionService, MoneyDataService dataService)
    {
        _transactionService = transactionService;
        _dataService = dataService;
    }

    // Currency of the app, kept in MoneyDataService
    public string? Currency => _dataService.Currency;

    // Totals text: only "Total" for incomes or expenses; Balance / Incomes / Expenses for all (type == null).
    // Used under the transactions table and above the menu.
    public string GetTotalsText(List<Transaction> transactions, TransactionType? type)
    {
        if (type is TransactionType.Income)
        {
            return $"Total income: {_transactionService.GetTotalIncome(transactions):0.00} {Currency}";
        }
        if (type is TransactionType.Expense)
        {
            return $"Total expenses: {_transactionService.GetTotalExpenses(transactions):0.00} {Currency}";
        }
        decimal incomes = _transactionService.GetTotalIncome(transactions);
        decimal expenses = _transactionService.GetTotalExpenses(transactions);
        decimal balance = _transactionService.CalculateBalance(transactions);
        return $"Balance: {balance:0.00} {Currency} | Incomes: {incomes:0.00} {Currency} | Expenses: {expenses:0.00} {Currency}";
    }

    // Success message after Add, Edit and Remove; action: "created", "edited" or "removed"
    public void ShowTransactionSuccess(Transaction transaction, string action)
    {
        string type = transaction.Type.ToString().ToLower(); // "income" or "expense"
        InputHelper.ShowSuccess($"The {type} \"{transaction.Name}\" with amount {transaction.Amount:0.00} {Currency} and date {transaction.Date:yyyy-MM-dd} was successfully {action}\n");
    }
    public void ShowCategorySuccess(Category category, string action)
    {
        InputHelper.ShowSuccess($"The category \"{category.Name}\" was successfully {action}\n");
    }

    // Text between two lines of the same length
    public void ShowHeader(string text)
    {
        InputHelper.ShowInfo(new string('-', text.Length));
        InputHelper.ShowInfo(text);
        InputHelper.ShowInfo(new string('-', text.Length));
    }
}
