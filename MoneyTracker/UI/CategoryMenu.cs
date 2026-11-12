using MoneyTracker.Exceptions;
using MoneyTracker.Models;
using MoneyTracker.Services;

namespace MoneyTracker.UI;

// Menu items 5-9: create a category, add expenses to a category, category statistics, remove a category.
// Asks the user, calls CategoryService and TransactionService, shows the result in tables (TablePrinter) and messages (ConsoleMessages)
public class CategoryMenu
{
    private readonly TransactionService _transactionService;
    private readonly CategoryService _categoryService;
    private readonly TablePrinter _tablePrinter;
    private readonly ConsoleMessages _messages;

    public CategoryMenu(TransactionService transactionService, CategoryService categoryService, TablePrinter tablePrinter, ConsoleMessages messages)
    {
        _transactionService = transactionService;
        _categoryService = categoryService;
        _tablePrinter = tablePrinter;
        _messages = messages;
    }

    // 5: category name (all categories are expense categories)
    public void CreateCategory()
    {
        _messages.ShowHeader("Create category | Enter q to cancel");
        try
        {
            while (true)
            {
                try
                {
                    string name = InputHelper.ReadTextOrCancel($"Enter category name (max {Category.MaxNameLength} characters, q - cancel): ", "Category name can't be empty", Category.MaxNameLength, $"Category name can't be longer than {Category.MaxNameLength} characters.");
                    Category category = new(name);
                    _categoryService.AddCategory(category);
                    _messages.ShowCategorySuccess(category, "created");
                }
                // Safety net: the Category constructor has the final word on the rules
                catch (ValidationException e)
                {
                    InputHelper.ShowError(e.Message);
                }
            }
        }
        catch (UserCancelledException)
        {
            InputHelper.ShowInfo("Back to main menu");
        }
    }

    // 6: categories table -> category id (0 - Uncategorized) -> y/n -> expense ids "12, 15, 20" -> all of them get the category.
    // Unknown category or expense id -> message and ask again; q -> back to the main menu
    public void AddTransactionsToCategory()
    {
        if (!_categoryService.HasCategories)
        {
            InputHelper.ShowWarning("No categories yet. Create a category first (main menu 5).");
            return;
        }
        try
        {
            while (true)
            {
                // Uncategorized is shown: 0 moves the expenses out of their category
                _tablePrinter.DisplayCategoriesTable(_categoryService.GetAllCategories(), _transactionService.GetAllExpenses(), true);
                try
                {
                    string categoryName;
                    int? categoryId = InputHelper.ReadNumberInRange("Enter category id (0 - Uncategorized, q - back to menu): ", 0, int.MaxValue, "Category id can't be empty", "Category id must be a whole number");
                    if (categoryId > 0)
                    {
                        Category category = _categoryService.GetCategoryById(categoryId.Value);
                        categoryName = category.Name;
                    }
                    else
                    {
                        categoryId = null;
                        categoryName = "Uncategorized";
                    }
                    if (InputHelper.ReadYesNo($"Add expenses to the category \"{categoryName}\"? (y/n): ", "Answer can't be empty", "Please enter y or n"))
                    {
                        // Grouped by category, so it is easy to see which expenses are already in which category
                        _tablePrinter.DisplayTransactionsTable(_categoryService.SortByCategory(_transactionService.GetAllExpenses(), false));
                        while (true)
                        {
                            try
                            {
                                List<int> transactionIds = InputHelper.ReadIds("Enter expense ids separated by commas, e.g. 12, 15, 20 (q - cancel): ", "Expense ids list can't be empty", "Enter whole numbers greater than 0, separated by commas, e.g. 12, 15, 20");
                                _transactionService.AddTransactionsToCategory(transactionIds, categoryId);
                                InputHelper.ShowSuccess($"Expenses added to the category \"{categoryName}\": {transactionIds.Count}\n");
                                break;
                            }
                            catch (TransactionNotFoundException e)
                            {
                                InputHelper.ShowWarning(e.Message);
                            }
                        }
                    }
                    else
                    {
                        InputHelper.ShowWarning($"Expenses were not added to the category \"{categoryName}\"\n");
                    }
                }
                catch (CategoryNotFoundException e)
                {
                    InputHelper.ShowWarning(e.Message);
                }
            }
        }
        catch (UserCancelledException)
        {
            InputHelper.ShowInfo("Back to main menu");
        }
    }

    // 9: categories table -> id -> "Remove the category? Its expenses will become Uncategorized" y/n.
    // The expenses are not deleted: they only lose the category
    public void DeleteCategory()
    {
        if (!_categoryService.HasCategories)
        {
            InputHelper.ShowWarning("No categories to remove yet. Create a category first (main menu 5).");
            return;
        }

        _messages.ShowHeader("Remove category | Enter q at any step to cancel");
        try
        {
            // Stop when there is nothing left to remove
            while (_categoryService.HasCategories)
            {
                try
                {
                    // Uncategorized is not a real category, it can't be removed -> not shown
                    _tablePrinter.DisplayCategoriesTable(_categoryService.GetAllCategories(), _transactionService.GetAllExpenses(), false);
                    int id = InputHelper.ReadId("Enter category id to remove (q - back to menu): ", "Category id can't be empty", "Category id must be a whole number greater than 0");
                    Category category = _categoryService.GetCategoryById(id);
                    int expensesCount = _categoryService.GetNumberOfTransactions(id, _transactionService.GetAllExpenses());
                    if (InputHelper.ReadYesNo($"Remove the category \"{category.Name}\"? Its expenses ({expensesCount}) will become Uncategorized (y/n): ", "Answer can't be empty", "Please enter y or n"))
                    {
                        _transactionService.RemoveCategoryFromExpenses(id);
                        _categoryService.DeleteCategory(id);
                        _messages.ShowCategorySuccess(category, "removed");
                    }
                    else
                    {
                        InputHelper.ShowWarning("The category was not removed.\n");
                    }
                }
                catch (CategoryNotFoundException e)
                {
                    InputHelper.ShowWarning(e.Message + "\n"); // empty line before the table
                }
            }
            InputHelper.ShowWarning("No categories left to remove.");
        }
        catch (UserCancelledException)
        {
            InputHelper.ShowInfo("Back to main menu");
        }
    }

    // 7: table of all categories (total, count, percent of all expenses) + "Uncategorized" row;
    // under the table: total of all expenses
    public void DisplayAllCategoriesStatistics()
    {
        if (!_categoryService.HasCategories)
        {
            InputHelper.ShowWarning("No categories yet. Create a category first (main menu 5).");
            return;
        }

        _messages.ShowHeader("All categories statistics");
        List<Transaction> expenses = _transactionService.GetAllExpenses();
        string footer = _messages.GetTotalsText(expenses, TransactionType.Expense);
        _tablePrinter.DisplayCategoriesTable(_categoryService.GetAllCategories(), expenses, true, footer);
    }

    // 8: categories table -> category id (0 - Uncategorized) -> statistics row of this category (total, count, percent of all expenses)
    // -> its expenses sorted by date with the total under the table; repeat until q
    public void DisplayStatisticsByCategory()
    {
        if (!_categoryService.HasCategories)
        {
            InputHelper.ShowWarning("No categories yet. Create a category first (main menu 5).");
            return;
        }

        _messages.ShowHeader("Statistics by category | Enter q to cancel");
        try
        {
            while (true)
            {
                List<Transaction> allExpenses = _transactionService.GetAllExpenses();
                _tablePrinter.DisplayCategoriesTable(_categoryService.GetAllCategories(), allExpenses, true);
                try
                {
                    int id = InputHelper.ReadNumberInRange("Enter category id (0 - Uncategorized, q - back to menu): ", 0, int.MaxValue, "Category id can't be empty", "Category id must be a whole number");
                    int? categoryId = null; // 0 -> Uncategorized
                    List<Category> chosenCategory = new(); // stays empty for Uncategorized: its row is added by showUncategorized
                    if (id > 0)
                    {
                        Category category = _categoryService.GetCategoryById(id);
                        categoryId = category.Id;
                        chosenCategory.Add(category);
                    }

                    // One row: percent is calculated from all expenses
                    _tablePrinter.DisplayCategoriesTable(chosenCategory, allExpenses, id == 0);

                    List<Transaction> expenses = _transactionService.SearchByCategory(categoryId);
                    if (expenses.Count == 0)
                    {
                        InputHelper.ShowWarning("No expenses in this category\n");
                    }
                    else
                    {
                        string footer = _messages.GetTotalsText(expenses, TransactionType.Expense);
                        _tablePrinter.DisplayTransactionsTable(expenses, footer);
                    }
                }
                catch (CategoryNotFoundException e)
                {
                    InputHelper.ShowWarning(e.Message + "\n"); // empty line before the table
                }
            }
        }
        catch (UserCancelledException)
        {
            InputHelper.ShowInfo("Back to main menu");
        }
    }
}
