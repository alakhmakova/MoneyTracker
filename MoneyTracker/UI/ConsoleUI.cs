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
                ShowStatistics();
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

    // ---------- TODO: implement the menu items below ----------

    // Above the menu: Balance / Incomes / Expenses with currency
    private void ShowStatistics()
    {
        // TODO: use _transactionService.CalculateBalance(), GetTotalIncome(), GetTotalExpenses()
        InputHelper.ShowInfo($"Balance: 0.00 {_currency} | Incomes: 0.00 {_currency} | Expenses: 0.00 {_currency}");
    }

    // 1: income or expense -> name -> date -> amount -> description (Enter - skip) -> only for expense: category (existing / new / Enter = Uncategorized)
    private void AddTransaction()
    {
        InputHelper.ShowWarning("Add Transaction: not implemented yet");
    }

    // 2: 1-All 2-Incomes 3-Expenses -> all or for a period -> sort field -> ascending/descending -> table.
    // Under the table: Incomes, Expenses and Balance for the shown list.
    // In the "All" table incomes and expenses are mixed: show the Type column (or "-" / red for expenses).
    private void ListTransactions()
    {
        // TODO: read 1-3; then call ListTransactions(null), (TransactionType.Income) or (TransactionType.Expense)
        InputHelper.ShowWarning("List Transactions: not implemented yet");
    }

    // type == null -> all transactions; otherwise only incomes or only expenses
    private void ListTransactions(TransactionType? type)
    {
        InputHelper.ShowWarning($"List {(type == null ? "All" : type.ToString())}: not implemented yet");
    }

    // 3: table -> id -> one scenario for all fields, each shown with its current value:
    //    "Name [ICA]: " ... Enter = keep current value, q = cancel.
    //    Fields: Name, Date, Amount, Description; Category only for expenses (id, or 0 = Uncategorized)
    private void EditTransaction()
    {
        InputHelper.ShowWarning("Edit Transaction: not implemented yet");
    }

    // 4: table -> id -> "will be deleted y/n"
    private void DeleteTransaction()
    {
        InputHelper.ShowWarning("Delete Transaction: not implemented yet");
    }

    // 5: category name (all categories are expense categories)
    private void CreateCategory()
    {
        InputHelper.ShowWarning("Create Category: not implemented yet");
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
            padded[i] = alignRight ? cells[i].PadLeft(widths[i]) : cells[i].PadRight(widths[i]);
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

    // ---------- TODO: tables ----------
    // Done: ToRow(Transaction), GetCategoryName.
    // TODO: rework the commented-out draft below into DisplayTransactionsTable, then delete the draft.
    //   The draft is the old asset table: the headers, FormatRow without the 3rd argument and the status colors still belong to assets.
    //   headers = { "Id", "Name", "Date", "Amount", "Category", "Description" }; FormatRow(..., new[] { 3 });
    //   under the table: Incomes, Expenses, Balance for the shown list; no status colors (maybe green incomes, red expenses).
    // TODO: ToRow(Category) - Id, Name from the category; Total, Count, % from CategoryService.
    // TODO: DisplayCategoriesTable(List<Category> items) - columns Id, Name, Total, Count, %; FormatRow(..., new[] { 2, 3, 4 });
    //   plus a separate "Uncategorized" row for expenses with CategoryId == null.
    /*
    // Print Transactions as a table; column width = longest value or header + padding
    // Rows are colored by end-of-life status: red = 3 months or less left, yellow = 6 months or less
    private void DisplayTransactionsTable(List<Transaction> Transactions)
    {
        string[] headers = { "Id", "Type", "Brand", "Model", "Office", "Purchase Date", "Price (USD)", "Local Price" };
        List<string[]> rows = Transactions.Select(ToRow).ToList(); // List<string[]> rows = categories.Select(c => ToRow(c, expenses)).ToList();
        int[] widths = GetColumnWidths(headers, rows);
        int tableWidth = widths.Sum() + widths.Length - 1; // + one space between columns

        InputHelper.ShowInfo(new string('-', tableWidth));
        InputHelper.ShowInfo(FormatRow(headers, widths));
        InputHelper.ShowInfo(new string('-', tableWidth));
        foreach (Transaction Transaction in Transactions)
        {
            // 1. Pick the row color by the Transaction's status today
            switch (Transaction.GetStatus(DateTime.Today))
            {
                case LifeStatus.Red:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LifeStatus.Yellow:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                    // LifeStatus.Normal: keep the default color
            }

            // 2. Print the row in that color
            Console.WriteLine(FormatRow(ToRow(Transaction), widths));

            // 3. Back to the default color for the next row
            Console.ResetColor();
        }
        InputHelper.ShowInfo(new string('-', tableWidth));
    }
    */
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
