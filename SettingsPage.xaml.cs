using FileViewerDemo.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FileViewerDemo;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        // In a real app, you would resolve this from a DI container or a shared service
        ViewModel = ((App)Application.Current).SettingsViewModel;
    }
}
