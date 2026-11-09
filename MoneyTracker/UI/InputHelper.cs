using System.Globalization;
using MoneyTracker.Exceptions;
using MoneyTracker.Models;

namespace MoneyTracker.UI;

// Reads and validates user input; prints colored messages
public static class InputHelper
{
    private const string DateFormat = "yyyy-MM-dd";

    // Ask until the user enters non-empty text
    public static string ReadText(string prompt, string emptyInputMessage)
    {
        while (true)
        {
            Console.Write(prompt);
            string? userInput = Console.ReadLine();

            // ReadLine() returns null when the input stream is closed (Ctrl+Z or redirected input has ended).
            if (userInput == null)
            {
                throw new InputClosedException();
            }

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                return userInput.Trim();
            }

            ShowError(emptyInputMessage);
        }
    }

    // Like ReadText, but "q" cancels the current operation
    public static string ReadTextOrCancel(string prompt, string emptyInputMessage)
    {
        string userInput = ReadText(prompt, emptyInputMessage);
        if (userInput.Equals("q", StringComparison.OrdinalIgnoreCase))
        {
            throw new UserCancelledException();
        }
        return userInput;
    }

    // Ask until the amount is valid (rules live in Transaction.IsValidAmount)
    public static decimal ReadAmount(string prompt, string emptyInputMessage, string invalidAmountMessage)
    {
        while (true)
        {
            string userInput = ReadTextOrCancel(prompt, emptyInputMessage);
            if (TryParseAmount(userInput, out decimal amount) && Transaction.IsValidAmount(amount))
            {
                return amount;
            }
            ShowError(invalidAmountMessage);
        }
    }

    // Read an amount with a dot or a comma as the decimal separator, on any computer:
    // "12.50" and "12,50" both give 12.50. Thousands separators are not allowed, so "1,234.50" fails.
    // Separate from ReadAmount, so it can be tested without the console.
    public static bool TryParseAmount(string input, out decimal amount)
    {
        string normalized = input.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
    }

    // Ask until the date has the format yyyy-MM-dd and is not in the future (Transaction.IsValidDate)
    //https://learn.microsoft.com/en-us/dotnet/api/system.datetime.tryparseexact?view=net-10.0#system-datetime-tryparseexact(system-string-system-string-system-iformatprovider-system-globalization-datetimestyles-system-datetime@)
    public static DateTime ReadDate(string prompt, string emptyInputMessage, string invalidDateMessage)
    {
        while (true)
        {
            string userInput = ReadTextOrCancel(prompt, emptyInputMessage);
            if (DateTime.TryParseExact(userInput, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
                && Transaction.IsValidDate(date))
            {
                return date;
            }
            ShowError(invalidDateMessage);
        }
    }

    // Ask until the user enters a whole number between min and max (menu and type choice)
    public static int ReadNumberInRange(string prompt, int min, int max, string emptyInputMessage, string invalidNumberMessage)
    {
        while (true)
        {
            string userInput = ReadTextOrCancel(prompt, emptyInputMessage);
            if (int.TryParse(userInput, out int number) && number >= min && number <= max)
            {
                return number;
            }
            ShowError(invalidNumberMessage);
        }
    }

    // Ask until the id is a whole number greater than 0
    public static int ReadId(string prompt, string emptyInputMessage, string invalidIdMessage)
    {
        while (true)
        {
            string userInput = ReadTextOrCancel(prompt, emptyInputMessage);
            if (int.TryParse(userInput, out int id) && id > 0)
            {
                return id;
            }
            ShowError(invalidIdMessage);
        }
    }

    // ---------- TODO: implement (used by Edit: Enter = keep the current value) ----------
    // Empty input returns currentValue; "q" cancels; otherwise the same checks as ReadText / ReadDate / ReadAmount
    public static string ReadTextOrKeep(string prompt, string currentValue) => throw new NotImplementedException();
    public static DateTime ReadDateOrKeep(string prompt, DateTime currentValue, string invalidDateMessage) => throw new NotImplementedException();
    public static decimal ReadAmountOrKeep(string prompt, decimal currentValue, string invalidAmountMessage) => throw new NotImplementedException();

    // Ask until the answer is y/yes (true) or n/no (false)
    public static bool ReadYesNo(string prompt, string emptyInputMessage, string invalidAnswerMessage)
    {
        while (true)
        {
            string answer = ReadText(prompt, emptyInputMessage).ToLower();
            if (answer == "y" || answer == "yes")
            {
                return true;
            }
            if (answer == "n" || answer == "no")
            {
                return false;
            }
            ShowError(invalidAnswerMessage);
        }
    }

    // Colored messages: red = error, green = success, magenta = info, yellow = warning
    public static void ShowError(string message) => WriteInColor(message, ConsoleColor.Red);

    public static void ShowSuccess(string message) => WriteInColor(message, ConsoleColor.Green);

    public static void ShowInfo(string message) => WriteInColor(message, ConsoleColor.Magenta);

    public static void ShowWarning(string message) => WriteInColor(message, ConsoleColor.Yellow);

    private static void WriteInColor(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
