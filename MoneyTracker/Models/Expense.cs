using System.Text.Json.Serialization;

namespace MoneyTracker.Models;

// Money out; common data comes from Transaction, plus the category (only expenses have categories)
public class Expense : Transaction
{
    // Id of the category; null = "Uncategorized" (no real category object, shown as "Uncategorized" in tables)
    // [JsonInclude]: private set is not in the constructor, so JSON needs permission to fill it
    [JsonInclude]
    public int? CategoryId { get; private set; }

    // A new expense is always "Uncategorized"; the category is set with SetCategory
    public Expense(string name, DateTime date, decimal amount, string? description)
        : base(name, date, amount, description)
    {
    }

    // null = Uncategorized. TransactionService checks that the category exists
    public void SetCategory(int? categoryId)
    {
        CategoryId = categoryId;
    }

    [JsonIgnore]
    public override TransactionType Type => TransactionType.Expense;

    // Expense takes money from the balance
    [JsonIgnore]
    public override decimal SignedAmount => -Amount;
}
