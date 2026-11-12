using MoneyTracker.Exceptions;
using MoneyTracker.Models;
using MoneyTracker.Services;

namespace MoneyTracker.UI;

// Menu items 1-4: add, list (period and sorting), edit and remove transactions.
// Asks the user, calls TransactionService, shows the result in tables (TablePrinter) and messages (ConsoleMessages)
public class TransactionMenu
{
    private readonly TransactionService _transactionService;
    private readonly TablePrinter _tablePrinter;
    private readonly ConsoleMessages _messages;

    public TransactionMenu(TransactionService transactionService, TablePrinter tablePrinter, ConsoleMessages messages)
    {
        _transactionService = transactionService;
        _tablePrinter = tablePrinter;
        _messages = messages;
    }

    // Currency of the app (short name for the texts below)
    private string? Currency => _messages.Currency;

    // 1: income or expense -> name -> date -> amount -> description (Enter - skip) -> only for expense: category (existing / new / Enter = Uncategorized)
    public void AddTransaction()
    {
        _messages.ShowHeader("Add transaction | Enter q at any step to cancel");
        try
        {
            while (true)
            {
                try
                {
                    int type = InputHelper.ReadNumberInRange("1-Income 2-Expense (q - back to menu): ", 1, 2, "Transaction type can't be empty", "Please enter 1 or 2");
                    string name = InputHelper.ReadTextOrCancel($"Enter name (max {Transaction.MaxNameLength} characters, q - cancel): ", "Transaction name can't be empty", Transaction.MaxNameLength, $"Transaction name can't be longer than {Transaction.MaxNameLength} characters.");
                    DateTime date = InputHelper.ReadDate("Enter date yyyy-MM-dd (q - cancel): ", "Transaction date can't be empty", "Date must be in the format yyyy-MM-dd and can't be in the future");
                    decimal amount = InputHelper.ReadAmount($"Enter amount in {Currency} (q - cancel): ", "Transaction amount can't be empty", "Amount must be greater than 0 with at most 2 decimals");
                    string? description = InputHelper.ReadOptionalText($"Enter description (max {Transaction.MaxDescriptionLength} characters, Enter - skip, q - cancel): ", Transaction.MaxDescriptionLength, $"Transaction description can't be longer than {Transaction.MaxDescriptionLength} characters.");

                    // Polymorphism: both are stored as Transaction
                    Transaction transaction = type == 1
                        ? new Income(name, date, amount, description)
                        : new Expense(name, date, amount, description);

                    _transactionService.AddTransaction(transaction);
                    _messages.ShowTransactionSuccess(transaction, "created");
                }
                // Safety net: the Transaction constructor has the final word on the rules
                catch (ValidationException e)
                {
                    InputHelper.ShowError(e.Message);
                }
            }
        }
        catch (UserCancelledException)
        {
            InputHelper.ShowInfo("Back to main menu");
        }
    }

    // 2: 1-All 2-Incomes 3-Expenses -> all or for a period -> sort field -> ascending/descending -> table.
    // Under the table: Incomes, Expenses and Balance for the shown list.
    // In the "All" table incomes and expenses are mixed: Amount is positive for incomes and negative for expenses.
    // Reads 1-3; then call ShowTransactions(...).
    public void ListTransactions()
    {
        try
        {
            int choice = InputHelper.ReadNumberInRange("1-All 2-Incomes 3-Expenses (q - back to menu): ", 1, 3, "Choice can't be empty", "Please enter 1, 2 or 3");
            switch (choice)
            {
                case 1:
                    // Shows all transactions because type is null
                    ShowTransactions(null);
                    break;
                case 2:
                    ShowTransactions(TransactionType.Income);
                    break;
                case 3:
                    ShowTransactions(TransactionType.Expense);
                    break;
            }
        }
        catch (UserCancelledException)
        {
            InputHelper.ShowInfo("Back to main menu");
        }
    }

    // list of the chosen type -> 1-All time (sorted by id) or 2-Period (sorted by date, oldest first)
    // -> table with totals for the shown list 
    // -> the user can sort this table again by date, amount or name (q - back to menu).
    // type == null -> all transactions; otherwise only incomes or only expenses
    private void ShowTransactions(TransactionType? type)
    {
        List<Transaction> transactions = _transactionService.GetTransactionsByType(type);
        if (transactions.Count == 0)
        {
            InputHelper.ShowWarning("No transactions yet. Choose 1 from the main menu to add a transaction");
            return;
        }

        transactions = AskTimeRangeAndApply(transactions);
        if (transactions.Count == 0)
        {
            InputHelper.ShowWarning("No transactions for this period");
            return;
        }

        string footer = _messages.GetTotalsText(transactions, type);
        bool showCategory = type != TransactionType.Income; // incomes have no category -> no Category column
        _tablePrinter.DisplayTransactionsTable(transactions, footer, showCategory);

        // Sorting one transaction changes nothing
        if (transactions.Count > 1)
        {
            SortTransactionsByChosenOrder(transactions, footer, showCategory);
        }
    }

    // Ask 1-All time or 2-Period and return the list in its default order:
    // all time -> all given transactions sorted by id; period -> only the transactions of the period, sorted by date, oldest first.
    // The result can be empty (no transactions in the period)
    private List<Transaction> AskTimeRangeAndApply(List<Transaction> transactions)
    {
        int periodChoice = InputHelper.ReadNumberInRange("1-All time 2-Period (q - back to menu): ", 1, 2, "Choice can't be empty", "Please enter 1 or 2");
        if (periodChoice == 2)
        {
            List<Transaction> periodTransactions = FilterTransactionsByChosenPeriod(transactions);
            return _transactionService.SortByDate(periodTransactions, false);
        }
        return _transactionService.SortById(transactions, false);
    }

    // Ask the sort field (1-Date 2-Amount 3-Name) and the order (1-Ascending 2-Descending), show the table again;
    // repeat until the user enters q (UserCancelledException -> ListTransactions shows "Back to main menu").
    // footer and showCategory: the same as in the first table (sorting doesn't change the totals)
    private void SortTransactionsByChosenOrder(List<Transaction> transactions, string footer, bool showCategory)
    {
        while (true)
        {
            int field = InputHelper.ReadNumberInRange("Sort by 1-Date 2-Amount 3-Name (q - back to menu): ", 1, 3, "Choice can't be empty", "Please enter 1, 2 or 3");
            int order = InputHelper.ReadNumberInRange("1-Ascending (oldest, smallest, A-Z) 2-Descending (newest, largest, Z-A) (q - back to menu): ", 1, 2, "Choice can't be empty", "Please enter 1 or 2");
            bool descending = order == 2;
            if (field == 1)
            {
                transactions = _transactionService.SortByDate(transactions, descending);
            }
            else if (field == 2)
            {
                transactions = _transactionService.SortByAmount(transactions, descending);
            }
            else
            {
                transactions = _transactionService.SortByName(transactions, descending);
            }
            _tablePrinter.DisplayTransactionsTable(transactions, footer, showCategory);
        }
    }

    // Shows the first and the last date of the list, asks From and To (Enter in To = only the From day;
    // asks again while To is earlier than From)
    // and returns only the transactions of this period, both days included (the result can be empty)
    private List<Transaction> FilterTransactionsByChosenPeriod(List<Transaction> transactions)
    {
        // Hint: dates of the first and the last transaction in the list
        InputHelper.ShowInfo($"Available period is from {_transactionService.StartOfPeriod(transactions):yyyy-MM-dd} to {_transactionService.EndOfPeriod(transactions):yyyy-MM-dd}");
        DateTime from;
        DateTime to;
        // Ask both dates again while the end date is earlier than the start date
        while (true)
        {
            from = InputHelper.ReadDate("From yyyy-MM-dd (q - cancel): ", "Start date can't be empty", "Date must be in the format yyyy-MM-dd and can't be in the future");
            // Enter -> the same date as From: transactions of one day
            to = InputHelper.ReadDateOrKeep($"To yyyy-MM-dd (Enter - same as From {from:yyyy-MM-dd}, q - cancel): ", from, "Date must be in the format yyyy-MM-dd and can't be in the future");
            if (_transactionService.ValidateDateRange(from, to))
            {
                break;
            }
            InputHelper.ShowError("End date can't be earlier than start date");
        }

        return _transactionService.FilterByPeriod(transactions, from, to);
    }

    // 3: id -> show the transaction -> y/n -> each field with its current value:
    //    "Name [ICA]: " ... Enter = keep current value, q = cancel.
    //    Fields: Name, Date, Amount, Description.
    //    TODO: Category for expenses (id, or 0 = Uncategorized) when categories are ready.
    public void EditTransaction()
    {
        if (!_transactionService.HasTransactions)
        {
            InputHelper.ShowWarning("No transactions to edit yet. Add a transaction first.");
            return;
        }

        _messages.ShowHeader("Edit transaction | Enter q at any step to cancel");
        try
        {
            // Edit one transaction after another until the user enters q
            while (true)
            {
                try
                {
                    int id = InputHelper.ReadId("Enter transaction id to edit (q - back to menu): ", "Transaction id can't be empty", "Transaction id must be a whole number greater than 0");
                    Transaction transaction = _transactionService.GetTransactionById(id);
                    _tablePrinter.DisplayTransactionsTable(new List<Transaction> { transaction }, null, transaction is Expense); // no footer; Category column only for an expense
                    if (InputHelper.ReadYesNo("Edit this transaction? (y/n): ", "Answer can't be empty", "Please enter y or n"))
                    {
                        string name = InputHelper.ReadTextOrKeep($"Name [{transaction.Name}] (max {Transaction.MaxNameLength} characters, Enter - keep, q - cancel): ", transaction.Name, Transaction.MaxNameLength, $"Transaction name can't be longer than {Transaction.MaxNameLength} characters.");
                        DateTime date = InputHelper.ReadDateOrKeep($"Date [{transaction.Date:yyyy-MM-dd}] (yyyy-MM-dd, Enter - keep, q - cancel): ", transaction.Date, "Date must be in the format yyyy-MM-dd and can't be in the future");
                        decimal amount = InputHelper.ReadAmountOrKeep($"Amount [{transaction.Amount:0.00}] (Enter - keep, q - cancel): ", transaction.Amount, "Amount must be greater than 0 with at most 2 decimals");
                        string? description = InputHelper.ReadOptionalTextOrKeep($"Description [{transaction.Description}] (max {Transaction.MaxDescriptionLength} characters, Enter - keep, - to clear, q - cancel): ", transaction.Description, Transaction.MaxDescriptionLength, $"Transaction description can't be longer than {Transaction.MaxDescriptionLength} characters.");

                        _transactionService.UpdateTransaction(id, name, date, amount, description);
                        _messages.ShowTransactionSuccess(transaction, "edited");
                    }
                    else
                    {
                        InputHelper.ShowWarning("The transaction was not edited.\n");
                    }
                }
                catch (TransactionNotFoundException e)
                {
                    InputHelper.ShowWarning(e.Message);
                }
                // Safety net: the Transaction constructor and Update have the final word on the rules
                catch (ValidationException e)
                {
                    InputHelper.ShowError(e.Message);
                }
            }
        }
        catch (UserCancelledException)
        {
            InputHelper.ShowInfo("Back to main menu");
        }
    }

    // 4: id -> show the transaction -> "will be deleted y/n"
    public void DeleteTransaction()
    {
        if (!_transactionService.HasTransactions)
        {
            InputHelper.ShowWarning("No transactions to remove yet. Add a transaction first.");
            return;
        }

        _messages.ShowHeader("Remove transaction | Enter q at any step to cancel");
        try
        {
            // Stop when there is nothing left to remove
            while (_transactionService.HasTransactions)
            {
                try
                {
                    int id = InputHelper.ReadId("Enter transaction id to remove (q - back to menu): ", "Transaction id can't be empty", "Transaction id must be a whole number greater than 0");
                    Transaction transaction = _transactionService.GetTransactionById(id);
                    _tablePrinter.DisplayTransactionsTable(new List<Transaction> { transaction }, null, transaction is Expense); // no footer; Category column only for an expense
                    if (InputHelper.ReadYesNo("Remove this transaction? (y/n): ", "Answer can't be empty", "Please enter y or n"))
                    {
                        _transactionService.DeleteTransaction(id);
                        _messages.ShowTransactionSuccess(transaction, "removed");
                    }
                    else
                    {
                        InputHelper.ShowWarning("The transaction was not removed.\n");
                    }
                }
                catch (TransactionNotFoundException e)
                {
                    InputHelper.ShowWarning(e.Message);
                }
            }
            InputHelper.ShowWarning("No transactions left to remove.");
        }
        catch (UserCancelledException)
        {
            InputHelper.ShowInfo("Back to main menu");
        }
    }
}
