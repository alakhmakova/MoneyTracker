using MoneyTracker.Models;
using MoneyTracker.Services;

namespace MoneyTracker.UI;

// Prints transactions and categories as tables: column widths, alignment, footer.
// Gets the rows to print as parameters; reads only category names and statistics from CategoryService
public class TablePrinter
{
    private const int ColumnPadding = 2; // extra spaces added to each column width
    private const int CategoryColumn = 4; // index of the Category column in the transactions table

    private readonly CategoryService _categoryService;

    public TablePrinter(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // Print transactions as a table; column width = longest value or header + padding.
    // Amount (column 3) is aligned to the right.
    // footer (optional): text under the table, aligned to its right edge (e.g. totals)
    // showCategory: false for incomes only - incomes have no category, so the column would always be empty
    public void DisplayTransactionsTable(List<Transaction> transactions, string? footer = null, bool showCategory = true)
    {
        string[] headers = { "Id", "Name", "Date", "Amount", "Category", "Description" };
        int[] rightAlignedColumns = { 3 }; // Category (4) is after Amount, so removing it doesn't move Amount
        List<string[]> rows = transactions.Select(ToRow).ToList();
        if (!showCategory)
        {
            headers = RemoveColumn(headers, CategoryColumn);
            rows = rows.Select(row => RemoveColumn(row, CategoryColumn)).ToList();
        }
        PrintTable(headers, rows, rightAlignedColumns, footer);
    }

    // Print categories as a table: Id, Name, Total, Count, % of the given expenses.
    // showUncategorized: add the last row "Uncategorized" (id 0) with the expenses without a category.
    // Total, Count and % (columns 2, 3, 4) are aligned to the right.
    // footer (optional): text under the table, aligned to its right edge (e.g. total of all expenses)
    public void DisplayCategoriesTable(List<Category> categories, List<Transaction> expenses, bool showUncategorized, string? footer = null)
    {
        string[] headers = { "Id", "Name", "Total", "Count", "%" };
        int[] rightAlignedColumns = { 2, 3, 4 };
        List<string[]> rows = categories.Select(c => ToRow(c, expenses)).ToList();
        if (showUncategorized)
        {
            rows.Add(new[]
            {
                "0",
                "Uncategorized",
                _categoryService.GetTotal(null, expenses).ToString("0.00"),
                _categoryService.GetNumberOfTransactions(null, expenses).ToString(),
                _categoryService.CalculatePercentage(null, expenses).ToString("P2"),
            });
        }
        PrintTable(headers, rows, rightAlignedColumns, footer);
    }

    // Common part of both tables: line, headers, line, rows, line, footer (if any)
    private static void PrintTable(string[] headers, List<string[]> rows, int[] rightAlignedColumns, string? footer)
    {
        int[] widths = GetColumnWidths(headers, rows);
        int tableWidth = widths.Sum() + widths.Length - 1; // + one space between columns

        InputHelper.ShowInfo(new string('-', tableWidth));
        InputHelper.ShowInfo(FormatRow(headers, widths, rightAlignedColumns));
        InputHelper.ShowInfo(new string('-', tableWidth));
        foreach (string[] row in rows)
        {
            Console.WriteLine(FormatRow(row, widths, rightAlignedColumns));
        }
        InputHelper.ShowInfo(new string('-', tableWidth));
        if (footer != null)
        {
            InputHelper.ShowInfo(footer.PadLeft(tableWidth));
        }
    }

    // Widest value in each column (header included) + padding
    private static int[] GetColumnWidths(string[] headers, List<string[]> rows)
    {
        int[] widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
        {
            int longest = headers[i].Length;
            foreach (string[] row in rows)
            {
                longest = Math.Max(longest, row[i].Length);
            }
            widths[i] = longest + ColumnPadding;
        }
        return widths;
    }

    // Pad each cell to its column width and join the cells with one space.
    // rightAlignedColumns: indexes of the columns aligned to the right (numbers: amount, total, %); the rest are aligned to the left.
    // Indexes start at 0. List only the columns that must be aligned to the right, not all columns.
    // Example: columns Id(0), Name(1), Date(2), Amount(3), Category(4), Description(5) -> new[] { 3 }: only Amount is aligned to the right.
    // Columns Id(0), Name(1), Total(2), Count(3), %(4) -> new[] { 2, 3, 4 }
    private static string FormatRow(string[] cells, int[] widths, int[] rightAlignedColumns)
    {
        string[] padded = new string[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            bool alignRight = rightAlignedColumns.Contains(i);
            // Right-aligned: the padding goes after the text too, so the next column doesn't stick to the number
            padded[i] = alignRight
                ? cells[i].PadLeft(widths[i] - ColumnPadding) + new string(' ', ColumnPadding)
                : cells[i].PadRight(widths[i]);
        }
        return string.Join(" ", padded);
    }

    // The same cells without the cell at this index (used to hide the Category column)
    private static string[] RemoveColumn(string[] cells, int index)
    {
        List<string> list = cells.ToList();
        list.RemoveAt(index);
        return list.ToArray();
    }

    // One category -> table cells as text; total, count and % are calculated from the given expenses
    private string[] ToRow(Category category, List<Transaction> expenses)
    {
        return new[]
        {
            category.Id.ToString(),
            category.Name,
            _categoryService.GetTotal(category.Id, expenses).ToString("0.00"),
            _categoryService.GetNumberOfTransactions(category.Id, expenses).ToString(),
            _categoryService.CalculatePercentage(category.Id, expenses).ToString("P2"), // 2 decimals: 14.29%; rounded parts may sum to 99.99% or 100.01%
        };
    }

    // One transaction -> table cells as text (ToRow(Category) is an overload of this method)
    private string[] ToRow(Transaction transaction)
    {
        return new[]
        {
            transaction.Id.ToString(),
            transaction.Name,
            transaction.Date.ToString("yyyy-MM-dd"),
            transaction.SignedAmount.ToString("0.00"), // 5.00 for an income, -5.00 for an expense
            GetCategoryName(transaction),
            transaction.Description ?? "", // no description -> empty cell
        };
    }

    // Text for the Category cell: "N/A" for incomes, "Uncategorized" for expenses without a category,
    // otherwise the category name
    private string GetCategoryName(Transaction transaction)
    {
        // "is Expense expense": if transaction is an Expense, put it into the variable expense (type Expense)
        if (transaction is Expense expense)
        {
            if (expense.CategoryId == null)
            {
                return "Uncategorized";
            }
            // CategoryId is int?, GetCategoryById needs int -> .Value
            return _categoryService.GetCategoryById(expense.CategoryId.Value).Name;
        }
        // Income: no category (not applicable)
        return "N/A";
    }
}
