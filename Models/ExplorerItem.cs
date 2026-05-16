using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace FileViewerDemo.Models;

public class ExplorerItem : INotifyPropertyChanged
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public bool IsFolder { get; init; }
    public DateTimeOffset DateModified { get; init; }
    public long Size { get; init; }
    public string TypeDescription { get; init; } = string.Empty;

    public string SizeDisplay => IsFolder ? string.Empty : FormatSize(Size);
    public string DateDisplay => DateModified.ToString("g");
    public Symbol IconSymbol => IsFolder ? Symbol.Folder : Symbol.Document;

    private ImageSource? _thumbnail;
    public ImageSource? Thumbnail
    {
        get => _thumbnail;
        set
        {
            if (_thumbnail != value)
            {
                _thumbnail = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = size;
        int unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }
        return $"{value:0.#} {units[unitIndex]}";
    }
}

public class DriveItem
{
    public required string DisplayName { get; init; }
    public required string Path { get; init; }
    public Symbol Icon { get; init; } = Symbol.Folder;
    public bool IsHeader { get; init; }
}
