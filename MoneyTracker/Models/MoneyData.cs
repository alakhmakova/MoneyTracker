namespace MoneyTracker.Models;

// What is saved to the JSON file: currency, transactions, categories and the next free ids
public class MoneyData
{
    // One currency for the whole app (SEK, USD, EUR); null until the user enters it on the first start
    public string? Currency { get; set; }

    public int NextTransactionId { get; set; } = 1;
    public int NextCategoryId { get; set; } = 1;

    public List<Transaction> Transactions { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}
