namespace SmartSolarMicrogrid.API.Common.Errors;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string? detail = null)
        : base(detail ?? ErrorCatalog.Get(code).Title)
    {
        Code = code;
    }
}
