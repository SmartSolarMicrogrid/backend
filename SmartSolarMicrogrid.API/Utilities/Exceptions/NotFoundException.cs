namespace SmartSolarMicrogrid.API.Utilities.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, string id)
        : base($"{entity} with id '{id}' was not found.") { }
}
