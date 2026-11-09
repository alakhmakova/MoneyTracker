namespace MoneyTracker.Exceptions;

// Thrown when data is invalid (empty name, future date, wrong amount...)
public class ValidationException : Exception
{
    public ValidationException()
        : base("Invalid data.") { }

    public ValidationException(string message)
        : base(message) { }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
