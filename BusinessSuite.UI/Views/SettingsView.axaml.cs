using System;
using Avalonia.Controls;

namespace BusinessSuite.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void Unit_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            if (DataContext is ViewModels.SettingsViewModel vm)
            {
                vm.AddUnitCommand.Execute(null);
            }
        }
    }

    private async void OnBackupClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.SettingsViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Save Backup",
            DefaultExtension = "db",
            SuggestedFileName = $"BusinessSuite_Backup_{DateTime.Now:yyyyMMdd}.db"
        });

        if (file != null)
        {
            await vm.BackupDataAsync(file.Path.LocalPath);
        }
    }

    private async void OnRestoreClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.SettingsViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select Backup to Restore",
            AllowMultiple = false,
            FileTypeFilter = new[] { new Avalonia.Platform.Storage.FilePickerFileType("Database Files") { Patterns = new[] { "*.db" } } }
        });

        if (files.Count > 0)
        {
            await vm.RestoreDataAsync(files[0].Path.LocalPath);
        }
    }
}
