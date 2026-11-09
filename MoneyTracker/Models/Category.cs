using System.Text.Json.Serialization;

namespace MoneyTracker.Models;

// A group of expenses, created by the user (Food, Clothes...). Incomes have no categories.
// Each Expense keeps CategoryId.
public class Category
{
    // CategoryService assigns id
    [JsonInclude]
    public int Id { get; internal set; }

    // Not empty, max length; unique (ignoring case) is checked in CategoryService, because only it sees all categories
    public string Name { get; private set; }

    public Category(string name)
    {
        // TODO: throw ValidationException if the name is empty/whitespace or longer than the max length (e.g. 30)
        Name = name.Trim();
    }
}
