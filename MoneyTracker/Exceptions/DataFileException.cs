namespace MoneyTracker.Exceptions;

// Thrown when the data file can't be read or written (missing access, corrupted JSON, invalid data)
public class DataFileException : Exception
{
    public DataFileException()
        : base("Data file error.") { }

    public DataFileException(string message)
        : base(message) { }

    public DataFileException(string message, Exception innerException)
        : base(message, innerException) { }
}
