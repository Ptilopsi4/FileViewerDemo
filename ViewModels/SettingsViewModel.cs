using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace FileViewerDemo.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private ElementTheme _currentTheme = ElementTheme.Default;
    public ElementTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThemeIndex));
            }
        }
    }

    public int ThemeIndex
    {
        get => _currentTheme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };
        set
        {
            CurrentTheme = value switch
            {
                0 => ElementTheme.Light,
                1 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }

    private bool _showHiddenFiles;
    public bool ShowHiddenFiles
    {
        get => _showHiddenFiles;
        set
        {
            if (_showHiddenFiles != value)
            {
                _showHiddenFiles = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showFileExtensions;
    public bool ShowFileExtensions
    {
        get => _showFileExtensions;
        set
        {
            if (_showFileExtensions != value)
            {
                _showFileExtensions = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
