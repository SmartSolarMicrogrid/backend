using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace SmartSolarMicrogrid.API.Services;

public class QrService
{
    private const string Prefix = "SSMTS1";
    private readonly byte[] _key;

    public QrService(IConfiguration config)
    {
        var rawKey = config["Qr:Key"] ?? "c3NtdHMtc3VwZXItc2VjcmV0LXFyLXNpZ25pbmcta2V5LTIwMjY=";
        _key = Convert.FromBase64String(rawKey);
    }

    public string CreatePayload(string reservationId, int qrVersion)
    {
        var signature = Base64UrlEncode(Mac(reservationId, qrVersion));
        return $"{Prefix}.{reservationId}.{qrVersion}.{signature}";
    }

    public bool TryRead(string payload, out string reservationId, out int qrVersion)
    {
        reservationId = string.Empty;
        qrVersion = 0;

        var parts = payload.Split('.');
        if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[2], out qrVersion))
            return false;

        reservationId = parts[1];
        var expected = Encoding.ASCII.GetBytes(Base64UrlEncode(Mac(reservationId, qrVersion)));
        var actual = Encoding.ASCII.GetBytes(parts[3]);

        if (expected.Length != actual.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public string BackupCode(string reservationId, int qrVersion)
    {
        var hash = Mac(reservationId, qrVersion);
        var num = BinaryPrimitives.ReadUInt32BigEndian(hash) % 1_000_000;
        return num.ToString("D6");
    }

    private byte[] Mac(string reservationId, int qrVersion) =>
        HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes($"{reservationId}.{qrVersion}"));

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
