using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Merchant.Till.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType BackupFile = new("Merchant backup") { Patterns = ["*.merchantbackup"] };

    public MainWindow()
    {
        InitializeComponent();
    }

    public async Task<string?> PickBackupToSave()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save backup",
            SuggestedFileName = "merchant",
            DefaultExtension = "merchantbackup",
            FileTypeChoices = [BackupFile],
        });
        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickBackupToOpen()
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Restore backup",
            AllowMultiple = false,
            FileTypeFilter = [BackupFile],
        });
        return files.Count == 1 ? files[0].TryGetLocalPath() : null;
    }
}
