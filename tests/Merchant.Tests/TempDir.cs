namespace Merchant.Tests;

public sealed class TempDir : IDisposable
{
    public string Path { get; } =
        Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "merchant-tests", Guid.NewGuid().ToString("N"))).FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
