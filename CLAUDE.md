# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build (Debug, x64 — default platform)
dotnet build

# Build specific platform (x86, x64, ARM64)
dotnet build -p:Platform=x64

# Run unpackaged (avoids MSIX packaging for faster dev iteration)
dotnet run

# Publish (ReadyToRun + trimmed for non-Debug)
dotnet publish -p:Platform=x64 -c Release
```

The project targets `net9.0-windows10.0.26100.0` with WinUI 3 and the Windows App SDK 1.6, and supports `win-x86`, `win-x64`, and `win-arm64` RIDs.

## Architecture

This is a **WinUI 3 desktop file explorer** app using the **MVVM** pattern (no DI container — viewmodels are constructed directly in views).

### Project structure

| Layer | Files | Purpose |
|-------|-------|---------|
| **Models** | `Models/ExplorerItem.cs` | `ExplorerItem` (file/folder display item with thumbnails), `DriveItem` (nav sidebar entry) |
| **ViewModels** | `ViewModels/MainViewModel.cs` | Core browsing logic: directory enumeration, back/forward/up navigation stacks, breadcrumb building, async thumbnail loading with cancellation |
| | `ViewModels/SettingsViewModel.cs` | Theme (Light/Dark/Default), show hidden files, show file extensions |
| **Views** | `MainWindow.xaml/.cs` | Shell: NavigationView sidebar, breadcrumb bar, toolbar buttons, ListView/GridView toggle |
| | `SamplePage.xaml/.cs` | Demo page showcasing `Files.App.Controls` components (Toolbar, BreadcrumbBar, StorageBar, ThemedIcon) |
| | `SettingsPage.xaml/.cs` | Settings UI bound to `SettingsViewModel` via compiled bindings |
| **Converters** | `Converters/` | `BoolToVisibility` and `InverseBoolToVisibility` — used to toggle between ListView (details) and GridView (tiles) |
| **Template selector** | `NavMenuItemTemplateSelector.cs` | Selects `NavigationViewItemHeader` for section headers vs `NavigationViewItem` for drive/quick-access entries |

### Key design decisions

- **Navigation history**: `MainViewModel` maintains `_backStack` and `_forwardStack` (Stack<string>). `NavigateToAsync(path, addToHistory: true)` pushes current path to back-stack before navigating. GoBack/GoForward pop from one stack and push current onto the other.
- **Breadcrumb construction**: `BuildPathFromBreadcrumbIndex(int index)` rebuilds a full path from breadcrumb segments. The first segment is always a label ("此电脑"), then the drive root, then relative path parts.
- **Async cancellation**: Each `NavigateToAsync` call cancels the previous `_loadCts` before starting. Thumbnail loading also respects the cancellation token — this prevents stale updates when the user navigates quickly.
- **Drive enumeration**: Runs on a thread-pool thread via `Task.Run` to avoid blocking the UI. Drives are presented as a flat list with "Quick Access" and "Drives" section headers. A `SamplePage` entry (Path = "SamplePage") is also injected.
- **Two view modes**: `IsGridView` property toggles visibility between ListView (`DetailsList`, details mode) and GridView (`TilesView`, icon mode) using the two boolean-to-visibility converters.
- **Settings are shared**: `SettingsViewModel` is created once in `App` and accessed by both `MainWindow` (for theme changes) and `SettingsPage` (for data-binding). Theme changes propagate via `PropertyChanged` subscription.

### External dependency

The solution references two projects from a sibling repo at `..\..\Repos\Files\`:
- `Files.App.Controls` — provides `Toolbar`, `BreadcrumbBar` (custom control), `StorageBar`, `ThemedIcon`, `SamplePanel` used in SamplePage
- `Files.Shared` — shared utilities

These are not NuGet packages but direct project references. The build will fail if the Files repo isn't present at the expected path.
