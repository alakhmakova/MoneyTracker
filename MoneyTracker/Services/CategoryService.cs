using MoneyTracker.Exceptions;
using MoneyTracker.Models;

namespace MoneyTracker.Services;

// Holds the category list: add, list, delete, statistics, sorting transactions by category.
// Statistics are calculated from the list of expenses passed to each method
// Does not print anything; errors are reported with exceptions.
public class CategoryService
{
    private readonly List<Category> _categories = new();
    private readonly IdGenerator _ids = new();

    // Next free id, to save in the file
    public int NextId => _ids.NextId;
    public bool HasCategories => _categories.Count > 0;

    // Give the category the next id and add it to the list; 
    // throws ArgumentNullException if category is null
    // throws ValidationException if category already exists
    public void AddCategory(Category category)
    {
        if (category is null)
        {
            throw new ArgumentNullException(nameof(category));
        }
        if (_categories.Any(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ValidationException($"Category \"{category.Name}\" already exists.");
        }
        category.Id = _ids.GetNextId();
        _categories.Add(category);
    }

    // All categories sorted by id (also used for saving)
    public List<Category> GetAllCategories()
    {
        return _categories.OrderBy(c => c.Id).ToList();
    }

    // Throw CategoryNotFoundException if there is no category with this id
    public Category GetCategoryById(int id)
    {
        Category? category = _categories.FirstOrDefault(c => c.Id == id); // null if not found
        if (category == null)
        {
            throw new CategoryNotFoundException($"Category with id {id} not found.");
        }
        return category;
    }

    // Remove the category with this id;
    // Throw CategoryNotFoundException if there is no category with this id
    public void DeleteCategory(int id)
    {
        _categories.Remove(GetCategoryById(id));
    }

    // Replaces the list with the loaded categories and restores the id counter.
    // Throws ArgumentNullException if categories is null
    public void LoadData(List<Category> categories, int savedNextId)
    {
        if (categories is null)
        {
            throw new ArgumentNullException(nameof(categories));
        }
        _categories.Clear();
        _categories.AddRange(categories);
        int maxId = _categories.Count > 0 ? _categories.Max(c => c.Id) : 0;
        _ids.Restore(savedNextId, maxId);
    }

    // Statistics for the category table
    // Sum of the expenses in the given category; categoryId == null -> "Uncategorized"
    public decimal GetTotal(int? categoryId, List<Transaction> expenses)
    {
        return expenses
            .OfType<Expense>()                      // keep only Expense objects, already typed as Expense
            .Where(e => e.CategoryId == categoryId) // only this category
            .Sum(e => e.Amount);
    }
    // Number of expenses in the given category; categoryId == null -> "Uncategorized".
    public int GetNumberOfTransactions(int? categoryId, List<Transaction> expenses)
    {
        return expenses
            .OfType<Expense>()                      // keep only Expense objects, already typed as Expense
            .Count(e => e.CategoryId == categoryId); // only this category
    }

    // Share of the category in all given expenses: 0.3333 -> shown as "33.33%" with ToString("P2").
    // Returns 0 if the expenses total is 0 (nothing to divide by)
    public decimal CalculatePercentage(int? categoryId, List<Transaction> expenses)
    {
        decimal allExpensesTotal = expenses.OfType<Expense>().Sum(e => e.Amount); // incomes are not counted, like in GetTotal
        if (allExpensesTotal <= 0)
        {
            return 0;
        }
        return GetTotal(categoryId, expenses) / allExpensesTotal;
    }

    // Sort by category name (A-Z, or Z-A if descending), "Uncategorized" for expenses without a category;
    // inside one category: by date, oldest first.
    // Incomes have no category: they always come after the expenses, sorted by id
    public List<Transaction> SortByCategory(List<Transaction> items, bool descending)
    {
        IEnumerable<Expense> expenses = items.OfType<Expense>();
        IEnumerable<Income> incomes = items.OfType<Income>().OrderBy(i => i.Id);

        IOrderedEnumerable<Expense> sortedExpenses = descending
            ? expenses.OrderByDescending(e => GetCategoryName(e.CategoryId))
            : expenses.OrderBy(e => GetCategoryName(e.CategoryId));

        List<Transaction> sorted = new();
        sorted.AddRange(sortedExpenses.ThenBy(e => e.Date));
        sorted.AddRange(incomes);
        return sorted;
    }

    // Category name for sorting; categoryId == null (or a category that no longer exists) -> "Uncategorized"
    private string GetCategoryName(int? categoryId)
    {
        Category? category = _categories.FirstOrDefault(c => c.Id == categoryId);
        return category?.Name ?? "Uncategorized";
    }
}
