using MoneyTracker.Exceptions;
using MoneyTracker.Models;
using MoneyTracker.Services;

namespace MoneyTracker.UI;

// Console UI: menu, input, tables, colors; the only class that prints and catches errors
public class ConsoleUI
{
    // Logic lives in the services; readonly, so they are never replaced
    private readonly TransactionService _transactionService = new();
    private readonly CategoryService _categoryService = new();
    // Personal data: money.json is in .gitignore, so it never goes to GitHub
    private readonly MoneyFileStorage _storage = new("moneyTracker.json");

    // One currency for the whole app; null until the user enters it on the first start
    private string? _currency;

    private const int ColumnPadding = 2; // extra spaces added to each column width

    public void Run()
    {
        ShowHeader("MONEY TRACKER");
        try
        {
            // Load saved data on start
            LoadFromFile();
            if (_currency == null)
            {
                AskCurrency();
            }

            // Menu-based system: repeat until the user enters 0
            bool run = true;
            while (run)
            {
                Console.WriteLine();
                ShowStatistics(_transactionService.GetAllTransactions());
                Console.WriteLine("1-Add Transaction");
                Console.WriteLine("2-List Transactions (All/Incomes/Expenses)");
                Console.WriteLine("3-Edit Transaction");
                Console.WriteLine("4-Delete Transaction");
                Console.WriteLine("5-Create Category");
                Console.WriteLine("6-Add Transactions to the Category");
                Console.WriteLine("7-Display All Categories Statistics");
                Console.WriteLine("8-Display Statistics by Category");
                Console.WriteLine("0-Quit");

                string menuOption = InputHelper.ReadText("Select Option 0-8: ", "Menu option can't be empty");

                switch (menuOption)
                {
                    case "1":
                        AddTransaction();
                        SaveToFile();
                        break;
                    case "2":
                        ListTransactions();
                        break;
                    case "3":
                        EditTransaction();
                        SaveToFile();
                        break;
                    case "4":
                        DeleteTransaction();
                        SaveToFile();
                        break;
                    case "5":
                        CreateCategory();
                        SaveToFile();
                        break;
                    case "6":
                        AddTransactionsToCategory();
                        SaveToFile();
                        break;
                    case "7":
                        DisplayAllCategoriesStatistics();
                        break;
                    case "8":
                        DisplayStatisticsByCategory();
                        break;
                    case "0":
                        InputHelper.ShowInfo("Thank you for using Money Tracker");
                        run = false;
                        break;
                    default:
                        InputHelper.ShowError("Invalid menu option. Select 0-8.");
                        break;
                }
            }
        }
        // Console.ReadLine() returns null only when the input stream is closed:
        // Ctrl+Z then Enter (Windows) / Ctrl+D (Linux, macOS), or redirected input (dotnet run < input.txt) has ended
        catch (InputClosedException e)
        {
            InputHelper.ShowWarning(e.Message);
        }
    }

    // First start: ask for the currency (SEK, USD, EUR); the main menu is shown only after that
    private void AskCurrency()
    {
        _currency = InputHelper.ReadCurrency("Enter currency (e.g. SEK, USD, EUR): ", "Currency can't be empty", "Currency must be 3 letters, e.g. SEK");
        SaveToFile();
        InputHelper.ShowSuccess($"Currency {_currency} saved");
    }

    // 1: income or expense -> name -> date -> amount -> description (Enter - skip) -> only for expense: category (existing / new / Enter = Uncategorized)
    private void AddTransaction()
    {
        ShowHeader("Add transaction | Enter q at any step to cancel");
        try
        {
            while (true)
            {
                try
                {
                    int type = InputHelper.ReadNumberInRange("1-Income 2-Expense (q - back to menu): ", 1, 2, "Transaction type can't be empty", "Please enter 1 or 2");
                    string name = InputHelper.ReadTextOrCancel($"Enter name (max {Transaction.MaxNameLength} characters, q - cancel): ", "Transaction name can't be empty", Transaction.MaxNameLength, $"Transaction name can't be longer than {Transaction.MaxNameLength} characters.");
                    DateTime date = InputHelper.ReadDate("Enter date yyyy-MM-dd (q - cancel): ", "Transaction date can't be empty", "Date must be in the format yyyy-MM-dd and can't be in the future");
                    decimal amount = InputHelper.ReadAmount($"Enter amount in {_currency} (q - cancel): ", "Transaction amount can't be empty", "Amount must be greater than 0 with at most 2 decimals");
                    string? description = InputHelper.ReadOptionalText($"Enter description (max {Transaction.MaxDescriptionLength} characters, Enter - skip, q - cancel): ", Transaction.MaxDescriptionLength, $"Transaction description can't be longer than {Transaction.MaxDescriptionLength} characters.");

                    // Polymorphism: both are stored as Transaction
                    Transaction transaction = type == 1
                        ? new Income(name, date, amount, description)
                        : new Expense(name, date, amount, description);

                    _transactionService.AddTransaction(transaction);
                    ShowTransactionSuccess(transaction, "created");
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

    // Above the menu: Balance / Incomes / Expenses of the given transactions
    private void ShowStatistics(List<Transaction> items)
    {
        InputHelper.ShowInfo(GetTotalsText(items, null));
    }

    // 2: 1-All 2-Incomes 3-Expenses -> all or for a period -> sort field -> ascending/descending -> table.
    // Under the table: Incomes, Expenses and Balance for the shown list.
    // In the "All" table incomes and expenses are mixed: Amount is positive for incomes and negative for expenses.
    // Reads 1-3; then call ShowTransactionsByType(...).
    private void ListTransactions()
    {
        try
        {
            int choice = InputHelper.ReadNumberInRange("1-All 2-Incomes 3-Expenses (q - back to menu): ", 1, 3, "Choice can't be empty", "Please enter 1, 2 or 3");
            switch (choice)
            {
                case 1:
                    // Shows all transactions because type is null
                    ShowTransactionsByType(null);
                    break;
                case 2:
                    ShowTransactionsByType(TransactionType.Income);
                    break;
                case 3:
                    ShowTransactionsByType(TransactionType.Expense);
                    break;
            }
        }
        catch (UserCancelledException)
        {
            InputHelper.ShowInfo("Back to main menu");
        }
    }

    // type == null -> all transactions; otherwise only incomes or only expenses
    private void ShowTransactionsByType(TransactionType? type)
    {
        // TODO: period and sorting
        List<Transaction> transactions;
        if (type is TransactionType.Income)
        {
            transactions = _transactionService.GetAllIncomes();
        }
        else if (type is TransactionType.Expense)
        {
            transactions = _transactionService.GetAllExpenses();
        }
        else
        {
            transactions = _transactionService.GetAllTransactions();
        }

        if (transactions.Count == 0)
        {
            InputHelper.ShowWarning("No transactions yet. Choose 1 from the main menu to add a transaction");
            return;
        }
        DisplayTransactionsTable(transactions, GetTotalsText(transactions, type));
    }

    // Totals text: only "Total" for incomes or expenses; Balance / Incomes / Expenses for all (type == null).
    // Used under the transactions table and above the menu.
    private string GetTotalsText(List<Transaction> transactions, TransactionType? type)
    {
        if (type is TransactionType.Income)
        {
            return $"Total income: {_transactionService.GetTotalIncome(transactions):0.00} {_currency}";
        }
        if (type is TransactionType.Expense)
        {
            return $"Total expenses: {_transactionService.GetTotalExpenses(transactions):0.00} {_currency}";
        }
        decimal incomes = _transactionService.GetTotalIncome(transactions);
        decimal expenses = _transactionService.GetTotalExpenses(transactions);
        decimal balance = _transactionService.CalculateBalance(transactions);
        return $"Balance: {balance:0.00} {_currency} | Incomes: {incomes:0.00} {_currency} | Expenses: {expenses:0.00} {_currency}";
    }

    // 3: id -> show the transaction -> y/n -> each field with its current value:
    //    "Name [ICA]: " ... Enter = keep current value, q = cancel.
    //    Fields: Name, Date, Amount, Description.
    //    TODO: Category for expenses (id, or 0 = Uncategorized) when categories are ready.
    private void EditTransaction()
    {
        if (!_transactionService.HasTransactions)
        {
            InputHelper.ShowWarning("No transactions to edit yet. Add a transaction first.");
            return;
        }

        ShowHeader("Edit transaction | Enter q at any step to cancel");
        try
        {
            // Edit one transaction after another until the user enters q
            while (true)
            {
                try
                {
                    int id = InputHelper.ReadId("Enter transaction id to edit (q - back to menu): ", "Transaction id can't be empty", "Transaction id must be a whole number greater than 0");
                    Transaction transaction = _transactionService.GetTransactionById(id);
                    DisplayTransactionsTable(new List<Transaction> { transaction });
                    if (InputHelper.ReadYesNo("Edit this transaction? (y/n): ", "Answer can't be empty", "Please enter y or n"))
                    {
                        string name = InputHelper.ReadTextOrKeep($"Name [{transaction.Name}] (max {Transaction.MaxNameLength} characters, Enter - keep, q - cancel): ", transaction.Name, Transaction.MaxNameLength, $"Transaction name can't be longer than {Transaction.MaxNameLength} characters.");
                        DateTime date = InputHelper.ReadDateOrKeep($"Date [{transaction.Date:yyyy-MM-dd}] (yyyy-MM-dd, Enter - keep, q - cancel): ", transaction.Date, "Date must be in the format yyyy-MM-dd and can't be in the future");
                        decimal amount = InputHelper.ReadAmountOrKeep($"Amount [{transaction.Amount:0.00}] (Enter - keep, q - cancel): ", transaction.Amount, "Amount must be greater than 0 with at most 2 decimals");
                        string? description = InputHelper.ReadOptionalTextOrKeep($"Description [{transaction.Description}] (max {Transaction.MaxDescriptionLength} characters, Enter - keep, - to clear, q - cancel): ", transaction.Description, Transaction.MaxDescriptionLength, $"Transaction description can't be longer than {Transaction.MaxDescriptionLength} characters.");

                        _transactionService.UpdateTransaction(id, name, date, amount, description);
                        ShowTransactionSuccess(transaction, "edited");
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
    private void DeleteTransaction()
    {
        if (!_transactionService.HasTransactions)
        {
            InputHelper.ShowWarning("No transactions to remove yet. Add a transaction first.");
            return;
        }

        ShowHeader("Remove transaction | Enter q at any step to cancel");
        try
        {
            // Stop when there is nothing left to remove
            while (_transactionService.HasTransactions)
            {
                try
                {
                    int id = InputHelper.ReadId("Enter transaction id to remove (q - back to menu): ", "Transaction id can't be empty", "Transaction id must be a whole number greater than 0");
                    Transaction transaction = _transactionService.GetTransactionById(id);
                    DisplayTransactionsTable(new List<Transaction> { transaction });
                    if (InputHelper.ReadYesNo("Remove this transaction? (y/n): ", "Answer can't be empty", "Please enter y or n"))
                    {
                        _transactionService.DeleteTransaction(id);
                        ShowTransactionSuccess(transaction, "removed");
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

    // Success message after Add, Edit and Remove; action: "created", "edited" or "removed"
    private void ShowTransactionSuccess(Transaction transaction, string action)
    {
        string type = transaction.Type.ToString().ToLower(); // "income" or "expense"
        InputHelper.ShowSuccess($"The {type} \"{transaction.Name}\" with amount {transaction.Amount:0.00} {_currency} and date {transaction.Date:yyyy-MM-dd} was successfully {action}\n");
    }
    private void ShowCategorySuccess(Category category, string action)
    {
        InputHelper.ShowSuccess($"The category \"{category.Name}\" was successfully {action}\n");
    }

    // ---------- TODO: implement the menu items below ----------

    // 5: category name (all categories are expense categories)
    private void CreateCategory()
    {
        ShowHeader("Create category | Enter q to cancel");
        try
        {
            while (true)
            {
                try
                {
                    string name = InputHelper.ReadTextOrCancel($"Enter category name (max {Category.MaxNameLength} characters, q - cancel): ", "Category name can't be empty", Category.MaxNameLength, $"Category name can't be longer than {Category.MaxNameLength} characters.");
                    Category category = new(name);
                    _categoryService.AddCategory(category);
                    ShowCategorySuccess(category, "created");
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

    // 6: category -> several transaction ids at once
    private void AddTransactionsToCategory()
    {
        InputHelper.ShowWarning("Add Transactions to the Category: not implemented yet");
    }

    // 7: category table (total, count, percent of all expenses) + "Uncategorized" row
    private void DisplayAllCategoriesStatistics()
    {
        InputHelper.ShowWarning("All Categories Statistics: not implemented yet");
    }

    // 8: find a category by id or name -> its table
    private void DisplayStatisticsByCategory()
    {
        InputHelper.ShowWarning("Statistics by Category: not implemented yet");
    }

    // Text between two lines of the same length
    private void ShowHeader(string text)
    {
        InputHelper.ShowInfo(new string('-', text.Length));
        InputHelper.ShowInfo(text);
        InputHelper.ShowInfo(new string('-', text.Length));
    }

    // Widest value in each column (header included) + padding
    private static int[] GetColumnWidths(string[] headers, List<string[]> rows)
    {
        int[] widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
        {
            int longest = headers[i].Length;
            foreach (string[] row in rows)
            {
                longest = Math.Max(longest, row[i].Length);
            }
            widths[i] = longest + ColumnPadding;
        }
        return widths;
    }

    // Pad each cell to its column width and join the cells with one space.
    // rightAlignedColumns: indexes of the columns aligned to the right (numbers: amount, total, %); the rest are aligned to the left.
    // Indexes start at 0. List only the columns that must be aligned to the right, not all columns.
    // Example: columns Id(0), Name(1), Date(2), Amount(3), Category(4), Description(5) -> new[] { 3 }: only Amount is aligned to the right.
    // Columns Id(0), Name(1), Total(2), Count(3), %(4) -> new[] { 2, 3, 4 }
    private static string FormatRow(string[] cells, int[] widths, int[] rightAlignedColumns)
    {
        string[] padded = new string[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            bool alignRight = rightAlignedColumns.Contains(i);
            // Right-aligned: the padding goes after the text too, so the next column doesn't stick to the number
            padded[i] = alignRight
                ? cells[i].PadLeft(widths[i] - ColumnPadding) + new string(' ', ColumnPadding)
                : cells[i].PadRight(widths[i]);
        }
        return string.Join(" ", padded);
    }

    // Load all data from the file; on error start with empty lists
    private void LoadFromFile()
    {
        try
        {
            MoneyData data = _storage.Load();
            _transactionService.LoadData(data.Transactions, data.NextTransactionId);
            _categoryService.LoadData(data.Categories, data.NextCategoryId);
            if (data.Currency is null || !InputHelper.IsValidCurrency(data.Currency))
            {
                _currency = null;
            }
            else
            {
                _currency = data.Currency.ToUpper();
            }
            if (data.Transactions.Count > 0)
            {
                InputHelper.ShowSuccess($"{data.Transactions.Count} transactions loaded from file");
            }
            else
            {
                InputHelper.ShowSuccess("No saved transactions found. Starting with an empty list.");
            }
        }
        catch (DataFileException e) { InputHelper.ShowWarning(e.Message + " Starting with an empty list."); }
    }

    // Save all data; show an error if the file can't be written
    private void SaveToFile()
    {
        try
        {
            MoneyData data = new()
            {
                Currency = _currency,
                NextTransactionId = _transactionService.NextId,
                NextCategoryId = _categoryService.NextId,
                Transactions = _transactionService.GetAllTransactions(),
                Categories = _categoryService.GetAllCategories(),
            };
            _storage.Save(data);
        }
        catch (DataFileException e) { InputHelper.ShowError(e.Message); }
    }

    // ---------- Tables ----------
    // TODO: DisplayCategoriesTable(List<Category> items) - columns Id, Name, Total, Count, %; FormatRow(..., new[] { 2, 3, 4 });
    //   plus a separate "Uncategorized" row for expenses with CategoryId == null.

    // Print transactions as a table; column width = longest value or header + padding.
    // Amount (column 3) is aligned to the right.
    // footer (optional): text under the table, aligned to its right edge (e.g. totals)
    private void DisplayTransactionsTable(List<Transaction> transactions, string? footer = null)
    {
        string[] headers = { "Id", "Name", "Date", "Amount", "Category", "Description" };
        int[] rightAlignedColumns = { 3 };
        List<string[]> rows = transactions.Select(ToRow).ToList();
        int[] widths = GetColumnWidths(headers, rows);
        int tableWidth = widths.Sum() + widths.Length - 1; // + one space between columns

        InputHelper.ShowInfo(new string('-', tableWidth));
        InputHelper.ShowInfo(FormatRow(headers, widths, rightAlignedColumns));
        InputHelper.ShowInfo(new string('-', tableWidth));
        foreach (string[] row in rows)
        {
            Console.WriteLine(FormatRow(row, widths, rightAlignedColumns));
        }
        InputHelper.ShowInfo(new string('-', tableWidth));
        if (footer != null)
        {
            InputHelper.ShowInfo(footer.PadLeft(tableWidth));
        }
    }

    // One category -> table cells as text; total, count and % are calculated from the given expenses
    private string[] ToRow(Category category, List<Transaction> expenses)
    {
        return new[]
        {
            category.Id.ToString(),
            category.Name,
            _categoryService.GetTotal(category.Id, expenses).ToString("0.00"),
            _categoryService.GetNumberOfTransactions(category.Id, expenses).ToString(),
            _categoryService.CalculatePercentage(category.Id, expenses).ToString("P1"), // ToString("0.00") + " %"
        };
    }
    // One transaction -> table cells as text (ToRow(Category) is an overload of this method)
    private string[] ToRow(Transaction transaction)
    {
        return new[]
        {
            transaction.Id.ToString(),
            transaction.Name,
            transaction.Date.ToString("yyyy-MM-dd"),
            transaction.SignedAmount.ToString("0.00"), // 5.00 for an income, -5.00 for an expense
            GetCategoryName(transaction),
            transaction.Description ?? "", // no description -> empty cell
        };
    }

    // Text for the Category cell: "" for incomes, "Uncategorized" for expenses without a category,
    // otherwise the category name
    private string GetCategoryName(Transaction transaction)
    {
        // "is Expense expense": if transaction is an Expense, put it into the variable expense (type Expense)
        if (transaction is Expense expense)
        {
            if (expense.CategoryId == null)
            {
                return "Uncategorized";
            }
            // CategoryId is int?, GetCategoryById needs int -> .Value
            return _categoryService.GetCategoryById(expense.CategoryId.Value).Name;
        }
        // Income: no category
        return "";
    }
}
