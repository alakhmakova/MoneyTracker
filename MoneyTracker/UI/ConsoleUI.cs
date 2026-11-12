using MoneyTracker.Exceptions;
using MoneyTracker.Models;
using MoneyTracker.Services;

namespace MoneyTracker.UI;

// Console UI: start of the app (load, currency), main menu and save after changes.
// Menu items 1-4 are in TransactionMenu, 5-9 in CategoryMenu; shared texts in ConsoleMessages.
// Logic is in the services, loading/saving and the currency in MoneyDataService, tables in TablePrinter
public class ConsoleUI
{
    // All objects come from Program.cs; readonly, so they are never replaced
    private readonly TransactionService _transactionService;
    private readonly MoneyDataService _dataService;
    private readonly ConsoleMessages _messages;
    private readonly TransactionMenu _transactionMenu;
    private readonly CategoryMenu _categoryMenu;

    public ConsoleUI(TransactionService transactionService, MoneyDataService dataService, ConsoleMessages messages, TransactionMenu transactionMenu, CategoryMenu categoryMenu)
    {
        _transactionService = transactionService;
        _dataService = dataService;
        _messages = messages;
        _transactionMenu = transactionMenu;
        _categoryMenu = categoryMenu;
    }

    // Currency of the app, kept in MoneyDataService (short name for the texts below)
    private string? Currency => _dataService.Currency;

    public void Run()
    {
        _messages.ShowHeader("MONEY TRACKER");
        try
        {
            // Load saved data on start
            LoadFromFile();
            if (Currency == null)
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
                Console.WriteLine("9-Delete Category");
                Console.WriteLine("0-Quit");

                string menuOption = InputHelper.ReadText("Select Option 0-9: ", "Menu option can't be empty");

                switch (menuOption)
                {
                    case "1":
                        _transactionMenu.AddTransaction();
                        SaveToFile();
                        break;
                    case "2":
                        _transactionMenu.ListTransactions();
                        break;
                    case "3":
                        _transactionMenu.EditTransaction();
                        SaveToFile();
                        break;
                    case "4":
                        _transactionMenu.DeleteTransaction();
                        SaveToFile();
                        break;
                    case "5":
                        _categoryMenu.CreateCategory();
                        SaveToFile();
                        break;
                    case "6":
                        _categoryMenu.AddTransactionsToCategory();
                        SaveToFile();
                        break;
                    case "7":
                        _categoryMenu.DisplayAllCategoriesStatistics();
                        break;
                    case "8":
                        _categoryMenu.DisplayStatisticsByCategory();
                        break;
                    case "9":
                        _categoryMenu.DeleteCategory();
                        SaveToFile();
                        break;
                    case "0":
                        InputHelper.ShowInfo("Thank you for using Money Tracker");
                        run = false;
                        break;
                    default:
                        InputHelper.ShowError("Invalid menu option. Select 0-9.");
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

    // First start: ask for the currency (SEK, USD, EUR) until MoneyDataService accepts it; the main menu is shown only after that.
    // No "q" here: the currency is required
    private void AskCurrency()
    {
        while (true)
        {
            string currency = InputHelper.ReadText("Enter currency (e.g. SEK, USD, EUR): ", "Currency can't be empty"); // ReadText already trims
            try
            {
                _dataService.SetCurrency(currency);
                break;
            }
            catch (ValidationException e)
            {
                InputHelper.ShowError(e.Message);
            }
        }
        SaveToFile();
        InputHelper.ShowSuccess($"Currency {Currency} saved");
    }

    // Above the menu: Balance / Incomes / Expenses of the given transactions
    private void ShowStatistics(List<Transaction> items)
    {
        InputHelper.ShowInfo(_messages.GetTotalsText(items, null));
    }

    // Load all data from the file (MoneyDataService) and show how many transactions were loaded;
    // on error start with empty lists
    private void LoadFromFile()
    {
        try
        {
            _dataService.Load();
            int count = _transactionService.GetAllTransactions().Count;
            if (count > 0)
            {
                InputHelper.ShowSuccess($"{count} transactions loaded from file");
            }
            else
            {
                InputHelper.ShowSuccess("No saved transactions found. Starting with an empty list.");
            }
        }
        catch (DataFileException e) { InputHelper.ShowWarning(e.Message + " Starting with an empty list."); }
    }

    // Save all data (MoneyDataService); show an error if the file can't be written
    private void SaveToFile()
    {
        try
        {
            _dataService.Save();
        }
        catch (DataFileException e) { InputHelper.ShowError(e.Message); }
    }
}
