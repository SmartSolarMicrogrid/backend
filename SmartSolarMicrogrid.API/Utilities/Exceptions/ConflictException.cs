namespace SmartSolarMicrogrid.API.Utilities.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
    public ConflictException(string field, string value)
        : base($"A record with {field} '{value}' already exists.") { }
}
