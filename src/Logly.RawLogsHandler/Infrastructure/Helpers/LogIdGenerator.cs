using System.Security.Cryptography;
using System.Text;

namespace Logly.RawLogsHandler.Infrastructure.Helpers;

public static class LogIdGenerator
{
    public static ulong GenerateStableUInt64(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToUInt64(hash, 0);
    }
}