using System.Security.Cryptography;

namespace Merchant.Core.Storage;

/// <summary>
/// Holds the random key that encrypts this device's database. On Windows the key file is sealed
/// with DPAPI for the current user; elsewhere it is a file only the owner can read.
/// </summary>
public static class DeviceKeyStore
{
    public static string GetOrCreate(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        var file = Path.Combine(dataDir, "device.key");

        if (File.Exists(file))
        {
            var stored = File.ReadAllBytes(file);
            var key = OperatingSystem.IsWindows()
                ? ProtectedData.Unprotect(stored, null, DataProtectionScope.CurrentUser)
                : stored;
            return Convert.ToHexString(key);
        }

        var fresh = RandomNumberGenerator.GetBytes(32);
        var sealedKey = OperatingSystem.IsWindows()
            ? ProtectedData.Protect(fresh, null, DataProtectionScope.CurrentUser)
            : fresh;

        // CreateNew: a key file that already exists is never overwritten, because the database
        // it opens would be lost with it.
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var stream = new FileStream(file, options))
        {
            stream.Write(sealedKey);
            stream.Flush(flushToDisk: true);
        }
        return Convert.ToHexString(fresh);
    }
}
