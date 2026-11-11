using System.Text.Json.Serialization;

namespace MoneyTracker.Models;

// Money in; all data comes from Transaction
public class Income : Transaction
{
    public Income(string name, DateTime date, decimal amount, string? description)
        : base(name, date, amount, description)
    {
    }

    [JsonIgnore]
    public override TransactionType Type => TransactionType.Income;

    // Income adds money to the balance
    [JsonIgnore]
    public override decimal SignedAmount => Amount;
}
