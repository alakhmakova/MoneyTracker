using MoneyTracker.Data;
using MoneyTracker.Exceptions;
using MoneyTracker.Models;

namespace MoneyTracker.Services;

// Loads all data from the file into the services and saves it back; keeps the app currency.
// Does not print anything: file errors come as DataFileException, a wrong currency as ValidationException
public class MoneyDataService
{
    private readonly MoneyFileStorage _storage;
    private readonly TransactionService _transactionService;
    private readonly CategoryService _categoryService;

    // One currency for the whole app (SEK, USD, EUR); null until the user enters it on the first start
    public string? Currency { get; private set; }

    public MoneyDataService(MoneyFileStorage storage, TransactionService transactionService, CategoryService categoryService)
    {
        _storage = storage;
        _transactionService = transactionService;
        _categoryService = categoryService;
    }

    // Read the file and give transactions and categories to the services.
    // A missing or wrong currency in the file -> Currency = null (the user is asked again).
    // Throws DataFileException if the file can't be read
    public void Load()
    {
        MoneyData data = _storage.Load();
        _transactionService.LoadData(data.Transactions, data.NextTransactionId);
        _categoryService.LoadData(data.Categories, data.NextCategoryId);
        if (data.Currency is null || !IsValidCurrency(data.Currency))
        {
            Currency = null;
        }
        else
        {
            Currency = data.Currency.ToUpper();
        }
    }

    // Collect the data of both services and write it to the file.
    // Throws DataFileException if the file can't be written
    public void Save()
    {
        MoneyData data = new()
        {
            Currency = Currency,
            NextTransactionId = _transactionService.NextId,
            NextCategoryId = _categoryService.NextId,
            Transactions = _transactionService.GetAllTransactions(),
            Categories = _categoryService.GetAllCategories(),
        };
        _storage.Save(data);
    }

    // Set the currency in upper case ("sek" -> "SEK").
    // Throws ValidationException if it is not exactly 3 Latin letters
    public void SetCurrency(string currency)
    {
        if (!IsValidCurrency(currency))
        {
            throw new ValidationException("Currency must be 3 letters, e.g. SEK");
        }
        Currency = currency.ToUpper();
    }

    // true if the text is exactly 3 Latin letters (SEK, usd, Eur...)
    public static bool IsValidCurrency(string text)
    {
        return text.Length == 3 && text.All(char.IsAsciiLetter);
    }
}
