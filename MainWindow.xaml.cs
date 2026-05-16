using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileViewerDemo.Models;
using FileViewerDemo.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace FileViewerDemo
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            RootGrid.DataContext = ViewModel;
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            var settings = ((App)Application.Current).SettingsViewModel;
            settings.PropertyChanged += Settings_PropertyChanged;
            ApplyTheme(settings.CurrentTheme);
        }

        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.CurrentTheme))
            {
                if (sender is SettingsViewModel settings)
                {
                    ApplyTheme(settings.CurrentTheme);
                }
            }
        }

        private void ApplyTheme(ElementTheme theme)
        {
            if (Content is FrameworkElement root)
            {
                root.RequestedTheme = theme;
            }
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.InitializeAsync();
                SelectNavItemForPath(ViewModel.CurrentPath);
            }
            catch (UnauthorizedAccessException)
            {
                await ShowErrorAsync("无法访问该文件夹");
            }
        }

        private async void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ShellNavView.Header = "设置";
                ShellNavView.Content = new Frame { Content = new SettingsPage() };
                return;
            }

            // 检查是否选中了 SamplePage 导航项
            if (args.SelectedItem is DriveItem SampleDrive && SampleDrive.Path?.ToString() == "SamplePage")
            {
                ShellNavView.Header = "控件示例";
                ShellNavView.Content = new Frame { Content = new SamplePage() };
                return;
            }

            if (args.SelectedItem is DriveItem drive)
            {
                // Restore original content if navigating away from settings
                if (ShellNavView.Content is Frame)
                {
                    ShellNavView.Content = ShellContentGrid;
                    ShellNavView.Header = "此电脑";
                }
                
                await NavigateSafeAsync(drive.Path);
            }
        }

        private async void ItemsList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (DetailsList.SelectedItem is ExplorerItem item)
            {
                await HandleItemInvokeAsync(item);
            }
        }

        private async void ItemsGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (TilesView.SelectedItem is ExplorerItem item)
            {
                await HandleItemInvokeAsync(item);
            }
        }

        private async Task HandleItemInvokeAsync(ExplorerItem item)
        {
            try
            {
                if (item.IsFolder)
                {
                    await ViewModel.NavigateToAsync(item.Path);
                    SelectNavItemForPath(item.Path);
                }
                else
                {
                    await ViewModel.OpenFileAsync(item.Path);
                }
            }
            catch (UnauthorizedAccessException)
            {
                await ShowErrorAsync("无法访问该文件夹");
            }
            catch (Exception)
            {
                await ShowErrorAsync("无法打开该文件");
            }
        }

        private async void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            var target = ViewModel.BuildPathFromBreadcrumbIndex(args.Index);
            if (!string.IsNullOrEmpty(target))
            {
                await NavigateSafeAsync(target);
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            await NavigateWithHandling(ViewModel.GoBackAsync);
        }

        private async void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            await NavigateWithHandling(ViewModel.GoForwardAsync);
        }

        private async void UpButton_Click(object sender, RoutedEventArgs e)
        {
            await NavigateWithHandling(ViewModel.GoUpAsync);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await NavigateWithHandling(ViewModel.RefreshAsync);
        }

        private async Task NavigateWithHandling(Func<Task<bool>> navigationAction)
        {
            try
            {
                if (await navigationAction())
                {
                    SelectNavItemForPath(ViewModel.CurrentPath);
                }
            }
            catch (UnauthorizedAccessException)
            {
                await ShowErrorAsync("无法访问该文件夹");
            }
        }

        private async Task NavigateSafeAsync(string path)
        {
            try
            {
                if (await ViewModel.NavigateToAsync(path))
                {
                    SelectNavItemForPath(path);
                }
                else
                {
                    await ShowErrorAsync("路径不存在或无法访问");
                }
            }
            catch (UnauthorizedAccessException)
            {
                await ShowErrorAsync("无法访问该文件夹");
            }
        }

        private void SelectNavItemForPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var match = ViewModel.Drives.FirstOrDefault(d => 
                !string.IsNullOrEmpty(d.Path) && 
                path.StartsWith(d.Path, StringComparison.OrdinalIgnoreCase));
            
            if (match != null)
            {
                ShellNavView.SelectedItem = match;
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "提示",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
