namespace SmartSolarMicrogrid.API.Utilities.Exceptions;

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
