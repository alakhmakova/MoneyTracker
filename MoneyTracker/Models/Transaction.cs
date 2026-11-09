using System.Text.Json.Serialization;

namespace MoneyTracker.Models;

// Base class for Income and Expense: common data.
// The JSON attributes save the subclass name in "$type", so the file knows whether to create an Income or an Expense.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Income), "Income")]
[JsonDerivedType(typeof(Expense), "Expense")]
public abstract class Transaction
{
    // "internal" can be read from anywhere, but set only inside this project (TransactionService assigns id)
    [JsonInclude]
    public int Id { get; internal set; }

    // Each child returns its own type
    public abstract TransactionType Type { get; }

    // "private set": can be changed only inside this class - in the constructor or in Update(),
    // so invalid data can't get in when creating or editing.

    // Short title (not empty, max length?)
    public string Name { get; private set; }

    // When the transaction took place (not in the future)
    public DateTime Date { get; private set; }

    // Always positive; Type says if it is money in or out (> 0, max 2 decimals)
    public decimal Amount { get; private set; }

    // Amount with a sign: Income +Amount, Expense -Amount (polymorphism: each child decides).
    // Balance = sum of all SignedAmount. Calculated, not saved in the file
    [JsonIgnore]
    public abstract decimal SignedAmount { get; }

    // Longer text, optional: null or empty is allowed (max length?)
    public string? Description { get; private set; }

    // Used when creating a transaction and when loading from the file.
    // Parameter names match the property names, so the JSON serializer uses this constructor when loading.
    // Throws ValidationException if the data is invalid
    protected Transaction(string name, DateTime date, decimal amount, string? description)
    {
        Validate(name, date, amount, description);
        Name = name.Trim();
        Date = date;
        Amount = amount;
        Description = description?.Trim();
    }

    // Edit: change all fields at once (the UI passes the current value for every field the user kept).
    // Throws ValidationException if the data is invalid; then nothing is changed
    public void Update(string name, DateTime date, decimal amount, string? description)
    {
        Validate(name, date, amount, description);
        Name = name.Trim();
        Date = date;
        Amount = amount;
        Description = description?.Trim();
    }

    // Checks the data for the constructor and Update; throws ValidationException with a clear message on the first error.
    // TODO: implement:
    //   name: not null/empty/whitespace; not longer than the max length (constant, e.g. 50)
    //   date: IsValidDate (not in the future)
    //   amount: IsValidAmount (> 0, at most 2 decimals)
    //   description: null or empty is allowed; not longer than the max length (constant, e.g. 200)
    private static void Validate(string name, DateTime date, decimal amount, string? description)
    {
    }

    // Compare date without time
    public static bool IsValidDate(DateTime date)
    {
        return date.Date <= DateTime.Today;
    }

    // Greater than 0 and at most 2 decimals
    public static bool IsValidAmount(decimal amount)
    {
        return amount > 0 && (Math.Round(amount, 2) == amount);
    }
}
