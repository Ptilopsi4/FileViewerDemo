using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileViewerDemo.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace FileViewerDemo.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<ExplorerItem> Items { get; } = new();
    public ObservableCollection<string> Breadcrumbs { get; } = new();
    public ObservableCollection<DriveItem> Drives { get; } = new();

    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();
    private CancellationTokenSource? _loadCts;

    private string _currentPath = string.Empty;
    public string CurrentPath
    {
        get => _currentPath;
        private set => SetProperty(ref _currentPath, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    private bool _isGridView;
    public bool IsGridView
    {
        get => _isGridView;
        set => SetProperty(ref _isGridView, value);
    }

    private bool _canGoBack;
    public bool CanGoBack
    {
        get => _canGoBack;
        private set => SetProperty(ref _canGoBack, value);
    }

    private bool _canGoForward;
    public bool CanGoForward
    {
        get => _canGoForward;
        private set => SetProperty(ref _canGoForward, value);
    }

    private bool _canGoUp;
    public bool CanGoUp
    {
        get => _canGoUp;
        private set => SetProperty(ref _canGoUp, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync()
    {
        await LoadDrivesAsync();
        string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!Directory.Exists(defaultPath) && Drives.Count > 0)
        {
            defaultPath = Drives[0].Path;
        }

        if (Directory.Exists(defaultPath))
        {
            await NavigateToAsync(defaultPath, addToHistory: false);
        }
    }

    public async Task LoadDrivesAsync()
    {
        Drives.Clear();
        var driveItems = await Task.Run(() =>
        {
            var list = new List<DriveItem>();

            // Quick Access
            list.Add(new DriveItem { DisplayName = "快速访问", Path = "", IsHeader = true });
            
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (Directory.Exists(desktop))
            {
                list.Add(new DriveItem { DisplayName = "桌面", Path = desktop, Icon = Symbol.Home });
            }

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (Directory.Exists(documents))
            {
                list.Add(new DriveItem { DisplayName = "文档", Path = documents, Icon = Symbol.Document });
            }

            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (Directory.Exists(pictures))
            {
                list.Add(new DriveItem { DisplayName = "图片", Path = pictures, Icon = Symbol.Pictures });
            }

            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloads))
            {
                list.Add(new DriveItem { DisplayName = "下载", Path = downloads, Icon = Symbol.Download });
            }

            // Drives
            list.Add(new DriveItem { DisplayName = "此电脑", Path = "", IsHeader = true });

            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType == DriveType.NoRootDirectory)
                    {
                        continue;
                    }

                    string label = string.IsNullOrEmpty(drive.VolumeLabel) ? drive.Name : $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})";
                    list.Add(new DriveItem
                    {
                        DisplayName = label,
                        Path = drive.Name,
                        Icon = Symbol.Folder // Fallback to Folder as HardDrive symbol might not be available in this enum version
                    });
                }
                catch
                {
                }
            }

            list.Add(new DriveItem { DisplayName = "控件示例", Path = "SamplePage", Icon = Symbol.Library});

            return list;
        });

        foreach (var drive in driveItems)
        {
            Drives.Add(drive);
        }
    }

    public async Task<bool> NavigateToAsync(string path, bool addToHistory = true)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        try
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            IsLoading = true;
            if (addToHistory && !string.IsNullOrEmpty(CurrentPath))
            {
                _backStack.Push(CurrentPath);
                _forwardStack.Clear();
            }

            var items = await Task.Run(() => LoadItems(path));

            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            CurrentPath = path;
            UpdateBreadcrumbs(path);
            UpdateNavigationStates();

            _ = LoadThumbnailsAsync(items, token);

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (IOException)
        {
            return false;
        }
        catch (SystemException)
        {
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadThumbnailsAsync(List<ExplorerItem> items, CancellationToken token)
    {
        foreach (var item in items)
        {
            if (token.IsCancellationRequested) return;

            try
            {
                StorageItemThumbnail? thumbnail = null;
                if (item.IsFolder)
                {
                    var folder = await StorageFolder.GetFolderFromPathAsync(item.Path);
                    thumbnail = await folder.GetThumbnailAsync(ThumbnailMode.ListView, 64);
                }
                else
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.Path);
                    thumbnail = await file.GetThumbnailAsync(ThumbnailMode.ListView, 64);
                }

                if (thumbnail != null && !token.IsCancellationRequested)
                {
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(thumbnail);
                    item.Thumbnail = bitmap;
                }
            }
            catch
            {
                // Ignore errors (e.g. access denied, file not found)
            }
        }
    }

    private List<ExplorerItem> LoadItems(string path)
    {
        var result = new List<ExplorerItem>();
        var directory = new DirectoryInfo(path);
        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false
        };

        foreach (var dir in directory.EnumerateDirectories("*", options))
        {
            result.Add(new ExplorerItem
            {
                Name = dir.Name,
                Path = dir.FullName,
                IsFolder = true,
                DateModified = dir.LastWriteTime,
                Size = 0,
                TypeDescription = "文件夹"
            });
        }

        foreach (var file in directory.EnumerateFiles("*", options))
        {
            string type = string.IsNullOrWhiteSpace(file.Extension) ? "文件" : $"{file.Extension} 文件";
            result.Add(new ExplorerItem
            {
                Name = file.Name,
                Path = file.FullName,
                IsFolder = false,
                DateModified = file.LastWriteTime,
                Size = file.Length,
                TypeDescription = type
            });
        }

        return result
            .OrderByDescending(i => i.IsFolder)
            .ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<bool> GoBackAsync()
    {
        if (!_backStack.TryPop(out var path))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(CurrentPath))
        {
            _forwardStack.Push(CurrentPath);
        }

        return await NavigateToAsync(path, addToHistory: false);
    }

    public async Task<bool> GoForwardAsync()
    {
        if (!_forwardStack.TryPop(out var path))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(CurrentPath))
        {
            _backStack.Push(CurrentPath);
        }

        return await NavigateToAsync(path, addToHistory: false);
    }

    public async Task<bool> GoUpAsync()
    {
        if (string.IsNullOrEmpty(CurrentPath))
        {
            return false;
        }

        var parent = Directory.GetParent(CurrentPath);
        if (parent == null)
        {
            return false;
        }

        return await NavigateToAsync(parent.FullName);
    }

    public Task<bool> RefreshAsync()
    {
        return NavigateToAsync(CurrentPath, addToHistory: false);
    }

    public Task OpenFileAsync(string path)
    {
        return Task.Run(() =>
        {
            var startInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            Process.Start(startInfo);
        });
    }

    public string? BuildPathFromBreadcrumbIndex(int index)
    {
        if (index <= 0 || index >= Breadcrumbs.Count || Breadcrumbs.Count < 2)
        {
            return null;
        }

        var root = Breadcrumbs.ElementAtOrDefault(1);
        if (string.IsNullOrEmpty(root))
        {
            return null;
        }

        if (index == 1)
        {
            return EnsureTrailingSlash(root);
        }

        var builder = new StringBuilder();
        builder.Append(EnsureTrailingSlash(root).TrimEnd('\\'));
        for (int i = 2; i <= index && i < Breadcrumbs.Count; i++)
        {
            builder.Append('\\');
            builder.Append(Breadcrumbs[i]);
        }
        return EnsureTrailingSlash(builder.ToString());
    }

    private static string EnsureTrailingSlash(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }
        return path.EndsWith("\\", StringComparison.Ordinal) ? path : path + "\\";
    }

    private void UpdateBreadcrumbs(string path)
    {
        Breadcrumbs.Clear();
        Breadcrumbs.Add("此电脑");

        var root = Path.GetPathRoot(path)?.TrimEnd('\\');
        if (!string.IsNullOrEmpty(root))
        {
            Breadcrumbs.Add(root);
        }

        var relative = path.TrimEnd('\\');
        if (!string.IsNullOrEmpty(root) && relative.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            relative = relative[root.Length..];
        }

        var segments = relative.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            Breadcrumbs.Add(segment);
        }
    }

    private void UpdateNavigationStates()
    {
        CanGoBack = _backStack.Count > 0;
        CanGoForward = _forwardStack.Count > 0;
        CanGoUp = Directory.GetParent(CurrentPath) != null;
    }

    private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return;
        }
        storage = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
