using MoneyTracker.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoneyTracker.Models;

namespace MoneyTracker.Services;

// Saves all data to a JSON file and loads it back; turns file errors into DataFileException
public class MoneyFileStorage
{
    private readonly string _filePath;
    // Readable JSON: line breaks and indentation; enums as text ("Income", not 0)
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public MoneyFileStorage(string filePath)
    {
        _filePath = filePath;
    }

    // Write all data to the file (serialization)
    public void Save(MoneyData data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (IOException e) { throw new DataFileException("Could not save data.", e); }
        catch (UnauthorizedAccessException e) { throw new DataFileException("Could not save data. Access denied.", e); }
    }

    // Read all data from the file (deserialization)
    public MoneyData Load()
    {
        try
        {
            if (!File.Exists(_filePath)) // first run: no file yet
            {
                return new();
            }

            string fromFile = File.ReadAllText(_filePath);
            MoneyData? loaded = JsonSerializer.Deserialize<MoneyData>(fromFile, _jsonOptions);
            return loaded ?? new(); // "null" in the file -> empty data
        }
        catch (JsonException e) { throw new DataFileException("Could not load data. The data file is corrupted.", e); }
        catch (IOException e) { throw new DataFileException("Could not load data.", e); }
        catch (UnauthorizedAccessException e) { throw new DataFileException("Could not load data. Access denied.", e); }
        catch (ValidationException e) { throw new DataFileException("Could not load data. The file contains invalid data.", e); }
    }
}
