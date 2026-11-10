using MoneyTracker.Exceptions;
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

    public const int MaxNameLength = 35;

    public Category(string name)
    {
        if (String.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Category name can't be empty.");
        }
        if (name.Trim().Length > MaxNameLength)
        {
            throw new ValidationException($"Category name can't be longer than {MaxNameLength} characters.");
        }
        Name = name.Trim();
    }
}
