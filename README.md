# HopDev.Maui.Controls

[![NuGet](https://img.shields.io/nuget/v/HopDev.Maui.Controls.svg)](https://www.nuget.org/packages/HopDev.Maui.Controls)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A .NET MAUI library that fixes real platform bugs at the WinUI3 boundary. Provides DPI-correct scrolling, a custom title bar, borderless window support, and a corner resize grip — all wired through shared platform services that solve the coordinate-space mismatches WinUI3 exposes on multi-monitor, mixed-DPI Windows setups.

> **Target framework:** .NET 10+ with MAUI workload
> **Platforms:** Windows (full feature set), macOS/iOS/Android (graceful no-op fallbacks)
> **Dependencies:** `Microsoft.Maui.Controls` only — no third-party packages

---

## The Problem

.NET MAUI on WinUI3 has a category of bugs rooted in **coordinate system mismatches** between Win32 (physical pixels), WinUI3 (its own internal scaling), and MAUI (logical device-independent pixels). These bugs are invisible at 100% display scaling on a single monitor, but break in production when users run 125%, 150%, or 200% scaling — especially across multiple monitors at different DPI.

Specific symptoms this library fixes:

- **Mouse wheel scrolling stops working** after moving a window between monitors at different scales, or sometimes randomly on any scaled display
- **Hit-testing is wrong at >100% DPI** — clicks land in the wrong place, scroll events go to the wrong panel, hover states are offset
- **`GetDpiForWindow` lies** — unpackaged MAUI apps without a `PerMonitorV2` manifest get virtualized coordinates from Win32 APIs, but `WH_MOUSE_LL` hooks always deliver physical coordinates. The mismatch breaks everything
- **WinUI3's `ExtendsContentIntoTitleBar`** doesn't give you the caption button metrics you need to lay out a custom title bar correctly, especially at non-100% DPI
- **Native scrollbars are too thin on high-DPI displays** — Windows renders 2-3px scrollbar tracks on 4K monitors that are nearly impossible to grab

---

## What's Included

### 1. SmartScrollPanel — Reliable Scroll with Customizable Scrollbar

A drop-in `ScrollView` replacement that **intercepts mouse wheel events before WinUI3 can misroute them**.

**Why the standard ScrollView breaks:** WinUI3 routes mouse input through internal `InputSite` threads that MAUI's ScrollView never sees. A standard ScrollView works for touch and trackpad, but mouse-wheel scrolling is unreliable or completely broken — especially after moving windows between monitors with different scaling.

**How SmartScrollPanel fixes it:**

1. Wraps a native MAUI `ScrollView` for layout and measurement
2. Installs a `WH_MOUSE_LL` hook to intercept wheel events before WinUI3 eats them
3. Uses WinUI3 `TransformToVisual` for DPI-safe hit-testing in physical screen coordinates
4. Monitors `XamlRoot.Changed` for DPI changes, saves scroll position ratio, forces native re-layout, then restores position
5. Overlays a **custom scrollbar thumb** — fully styleable width, color, corner radius — replacing the native scrollbar that becomes unusably thin on high-DPI displays

**Multiple panels on one page:** When several SmartScrollPanels are visible, the hook uses hit-testing to route wheel events to the correct panel. When panels are nested, the smallest (most specific) panel wins.

**XAML usage:**

```xml
xmlns:hd="clr-namespace:HopDev.Maui.Controls.Controls;assembly=HopDev.Maui.Controls"

<hd:SmartScrollPanel ScrollBarWidth="8"
                     ThumbColor="{DynamicResource TextMuted}"
                     ThumbCornerRadius="4"
                     ThumbMinHeight="40"
                     ScrollSensitivity="1.0">
    <VerticalStackLayout Spacing="8">
        <!-- your content -->
    </VerticalStackLayout>
</hd:SmartScrollPanel>
```

**Bindable properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Body` | `View` | null | The scrollable content (set via `ContentProperty`, so place content directly inside the tag) |
| `Orientation` | `ScrollOrientation` | Vertical | Scroll direction |
| `ScrollBarWidth` | `double` | 10.0 | Thumb track width in logical pixels |
| `ThumbColor` | `Color` | Gray | Scrollbar thumb color |
| `ThumbCornerRadius` | `double` | 5.0 | Thumb rounded corner radius |
| `ThumbMinHeight` | `double` | 30.0 | Minimum thumb size (prevents the thumb from becoming invisible on very long content) |
| `ScrollSensitivity` | `double` | 1.0 | Wheel scroll multiplier (>1 = faster, <1 = slower) |

**Public methods:**

| Method | Description |
|--------|-------------|
| `ScrollToTop()` | Animated scroll to top |
| `ScrollToBottom()` | Animated scroll to bottom |

**Events:**

| Event | Args | Description |
|-------|------|-------------|
| `Scrolled` | `SmartScrolledEventArgs` | Fires on every scroll position change. Args contain `ScrollX` and `ScrollY`. |

**Read-only state:** `ScrollY` and `ScrollX` properties expose the current scroll offset.

---

### 2. TitleBar — Custom Window Title Bar

A title bar control that replaces the standard Windows title bar with a three-zone layout: **Leading | Center | Trailing | [caption buttons]**.

Handles all platform plumbing — `ExtendsContentIntoTitleBar`, drag regions, caption button measurement, DPI-aware spacing, `InputNonClientPointerSource` interactive region registration — so you only think about content.

**Minimal usage:**

```xml
<hd:TitleBar Title="My App" HeightRequest="48"
             TitleBarBackground="{DynamicResource SurfaceColor}" />
```

**Full three-zone layout:**

```xml
<hd:TitleBar TitleBarBackground="{DynamicResource SurfaceBrush}" HeightRequest="48">

    <hd:TitleBar.LeadingContent>
        <Image Source="appicon.png" HeightRequest="20" />
    </hd:TitleBar.LeadingContent>

    <!-- Center content (default ContentProperty) -->
    <SearchBar Placeholder="Search..." hd:TitleBar.IsInteractive="True" />

    <hd:TitleBar.TrailingContent>
        <HorizontalStackLayout Spacing="4">
            <ImageButton Source="settings.png" hd:TitleBar.IsInteractive="True" />
        </HorizontalStackLayout>
    </hd:TitleBar.TrailingContent>

</hd:TitleBar>
```

**Key concepts:**

- **Drag regions:** The entire title bar is a drag region by default. Pointer events on the bar trigger window move/drag.
- **Interactive regions:** Controls inside the title bar that need to receive clicks (buttons, search bars, etc.) must be marked with `hd:TitleBar.IsInteractive="True"`. Common interactive types (`Button`, `Entry`, `SearchBar`, `Picker`, `CheckBox`, `Switch`, `Slider`) are auto-detected — you only need the attached property for custom controls.
- **Caption button spacer:** The control automatically measures the platform's minimize/maximize/close buttons and reserves space so your content doesn't overlap them. Uses exponential-backoff retry (100ms → 1600ms) to handle the timing gap when the `AppWindowTitleBar` isn't fully initialized on first render.

**Bindable properties:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LeadingContent` | `View` | null | Left zone (typically app icon or back button) |
| `CenterContent` | `View` | null | Center zone (search bar, title, tabs). This is the `ContentProperty` — content placed directly inside `<hd:TitleBar>` goes here. |
| `TrailingContent` | `View` | null | Right zone (settings, profile, actions) |
| `Title` | `string` | null | Fallback text title — shown only when `CenterContent` is null |
| `TitleFontSize` | `double` | 13.0 | Font size for fallback title |
| `TitleColor` | `Color` | null | Color for fallback title |
| `TitleBarBackground` | `Brush` | null | Background brush for the title bar |
| `ButtonHoverColor` | `Color` | null | Caption button hover background (null = platform default) |
| `ButtonForegroundColor` | `Color` | null | Caption button icon color |
| `ButtonPressedColor` | `Color` | null | Caption button pressed background |
| `AutoExtend` | `bool` | true | When true, automatically calls `ExtendContentIntoTitleBar` on attach. Set false to manage this yourself. |

**Attached property:**

| Property | Target | Description |
|----------|--------|-------------|
| `TitleBar.IsInteractive` | Any `View` | Marks a view inside the title bar as interactive (receives clicks instead of triggering window drag) |

**Read-only properties:**

| Property | Type | Description |
|----------|------|-------------|
| `CaptionButtonInsets` | `Thickness` | Current caption button dimensions in logical pixels. `.Right` = total width of min/max/close. `.Left` = any reserved left space. |
| `IsAttachedToWindow` | `bool` | True after the control has attached to the platform and metrics are available |

**Events:**

| Event | Description |
|-------|-------------|
| `AttachedToWindow` | Fires when the control has attached to the platform window and all metrics are available |

---

### 3. BorderlessWindowExtensions — Borderless Window Setup

A `MauiAppBuilder` extension that configures a fully borderless window where your app draws its own chrome.

**What it does:**

- Sets `ExtendsContentIntoTitleBar = true` on both WinUI3 and AppWindow levels
- Makes caption button backgrounds transparent
- Hides the system icon and menu
- Sets `PreferredHeightOption = Tall` for the modern Windows 11 look
- Pulls content up past MAUI's reserved 32px `AppTitleBarContainer` (uses the `-32` margin workaround for dotnet/maui#22894)
- Enables DWM dark mode border rendering (`DWMWA_USE_IMMERSIVE_DARK_MODE`)
- Auto-syncs the DWM border color when the app theme changes (light ↔ dark)
- Emits a runtime warning if the `PerMonitorV2` DPI manifest is missing

**Note on the 1px border:** The DWM window border at the top is intentionally kept. It provides a visual edge that helps users locate the window boundary, especially in dark mode where a borderless window blends into dark backgrounds. On Windows 11 this border is theme-colored.

**Registration:**

```csharp
// MauiProgram.cs
var builder = MauiApp.CreateBuilder();
builder
    .UseHopDevControls()
    .UseBorderlessWindow();  // Must come after UseHopDevControls()
```

**Requirements for consuming app:**

1. Set `Window.Title = ""` in `App.xaml.cs`
2. Optionally set `WindowCaptionBackground = Transparent` in `Platforms/Windows/App.xaml` (the extension does this at runtime as a fallback)
3. Include the `PerMonitorV2` DPI manifest (see PerMonitorV2 Manifest section below)

**Theme change API:**

```csharp
// Call when your app toggles between light/dark themes
BorderlessWindowExtensions.UpdateBorderColor(isDark: true);
```

---

### 4. ResizeGrip — Corner Resize for Borderless Windows

A bottom-right corner resize grip that hands off to native Windows resize on pointer down. Feels identical to grabbing the actual window corner — Windows handles the entire resize operation including cursor, rubber-band, and snap layout.

**How it works:** On `PointerPressed`, the control calls `ReleaseCapture()` + `SendMessage(hwnd, WM_NCLBUTTONDOWN, HTBOTTOMRIGHT)` which transfers the resize operation to the Windows window manager. No custom drag logic needed.

**Visual:** Six small dots in a triangular pattern (the standard resize grip pattern).

**Usage:**

Place it in a layout that positions it at the bottom-right corner of your window:

```xml
<Grid>
    <!-- Your app content -->
    <hd:TitleBar ... />
    <ContentView ... />

    <!-- Resize grip — floats at bottom-right -->
    <hd:ResizeGrip />
</Grid>
```

The control sets its own `HorizontalOptions="End"`, `VerticalOptions="End"`, 20x20 size, and 2px margin. No configuration needed.

---

## Platform Services (Advanced)

The controls above use three platform services internally. These are also available for direct use if you're building custom controls that need DPI-correct behavior.

Services are scoped **per-Window** via attached properties on `HopDevServices` — not DI singletons (breaks multi-window) and not transient (wastes resources).

### IWindowScaleService

Provides the **real** display scale factor, computed empirically using `AppWindow.Size.Width / Window.Width`. This is the only approach that works correctly under DPI virtualization.

```csharp
var scaleService = HopDevServices.GetScaleService(window);

double scale = scaleService.ScaleFactor;     // 1.0, 1.25, 1.5, 2.0, etc.
var logical = scaleService.ToLogical(physicalPoint);
var physical = scaleService.ToPhysical(logicalPoint);
nint hwnd = scaleService.WindowHandle;

scaleService.ScaleChanged += (s, e) => {
    // Window moved to a monitor with different DPI
};
```

### IWindowChromeService

Controls window chrome — title bar extension, drag regions, caption button metrics. Wraps `AppWindowTitleBar` and `InputNonClientPointerSource`.

```csharp
var chromeService = HopDevServices.GetChromeService(window);

chromeService.ExtendContentIntoTitleBar(true);
chromeService.SetDragRegion(myGrid);
chromeService.RegisterInteractiveRegion(mySearchBar);
chromeService.UnregisterInteractiveRegion(mySearchBar);

Thickness insets = chromeService.CaptionButtonInsets;
// insets.Right = total width of min/max/close buttons

chromeService.SetButtonColors(foreground, hoverBg, pressedBg);
```

### IPointerInterceptService

Centralized `WH_MOUSE_LL` hook with DPI-corrected coordinates. One hook per window (not per control).

```csharp
var pointerService = HopDevServices.GetPointerService(window);

pointerService.RegisterScrollRegion(myView, args => {
    // args.PhysicalPosition — raw screen pixels from the hook
    // args.LogicalPosition  — MAUI-compatible DPI-corrected coordinates
    // args.Delta            — wheel delta (positive = up, negative = down)
    // args.IsHorizontal     — true for horizontal wheel
    // args.Handled = true   — prevents further dispatch
});

pointerService.WheelEvent += (s, args) => {
    // Raw wheel events for the entire window
};
```

### Coordinate Types

Two value types prevent the category of bug where physical and logical coordinates are accidentally mixed:

```csharp
PhysicalPoint physical = new(1920.0, 540.0);  // actual screen pixels
LogicalPoint logical = new(1280.0, 360.0);     // MAUI DIPs at 150% scale

// Conversion
var logicalFromPhysical = scaleService.ToLogical(physical);
var physicalFromLogical = scaleService.ToPhysical(logical);

// Interop with MAUI
Point mauiPoint = logical.ToMauiPoint();
LogicalPoint fromMaui = LogicalPoint.FromMaui(mauiPoint);
```

---

## Setup

### Install

```
dotnet add package HopDev.Maui.Controls
```

### Register in MauiProgram.cs

```csharp
var builder = MauiApp.CreateBuilder();

builder.UseHopDevControls();
// For borderless windows, also add:
// builder.UseBorderlessWindow();

return builder.Build();
```

`UseHopDevControls()` wires `WindowHandler.Mapper` to auto-cleanup platform services (hooks, subscriptions) when windows are destroyed. The services themselves are created lazily — when a control that needs them first attaches to a window.

### PerMonitorV2 Manifest

**This is required** for any unpackaged MAUI app (`WindowsPackageType=None`) that uses this library on Windows. Without it, all DPI-dependent behavior is broken.

Create `Platforms/Windows/app.manifest`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="YourApp"/>
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">
        PerMonitorV2
      </dpiAwareness>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">
        true/pm
      </dpiAware>
    </windowsSettings>
  </application>
</assembly>
```

Reference it in your `.csproj`:

```xml
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">
    <ApplicationManifest>Platforms\Windows\app.manifest</ApplicationManifest>
</PropertyGroup>
```

**Why this matters:** `WH_MOUSE_LL` hooks always deliver physical screen coordinates. Without `PerMonitorV2`, `GetDpiForWindow` returns 96 (lies), `ClientToScreen` returns virtualized coordinates, and the mismatch makes all hit-testing fail on scaled displays. The library detects this at runtime and logs a warning, but cannot fix it — the manifest must be present at app startup.

**MSIX-packaged apps** (`WindowsPackageType=MSIX`) get `PerMonitorV2` automatically and don't need the manifest.

---

## Complete Example — Borderless App with TitleBar, ScrollPanel, and ResizeGrip

**MauiProgram.cs:**

```csharp
using HopDev.Maui.Controls.Extensions;

public static class MauiProgram
{
    public static MauiApp CreateMauiProgram()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseHopDevControls()
            .UseBorderlessWindow();

        return builder.Build();
    }
}
```

**MainPage.xaml:**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:hd="clr-namespace:HopDev.Maui.Controls.Controls;assembly=HopDev.Maui.Controls"
             x:Class="MyApp.MainPage">

    <Grid RowDefinitions="48,*">

        <!-- Row 0: Custom title bar -->
        <hd:TitleBar Grid.Row="0"
                     TitleBarBackground="{DynamicResource SurfaceColor}"
                     HeightRequest="48">
            <hd:TitleBar.LeadingContent>
                <HorizontalStackLayout Spacing="8" Padding="12,0">
                    <Image Source="appicon.png" HeightRequest="20" WidthRequest="20" />
                    <Label Text="My App" FontSize="13" VerticalOptions="Center" />
                </HorizontalStackLayout>
            </hd:TitleBar.LeadingContent>

            <SearchBar Placeholder="Search..."
                       hd:TitleBar.IsInteractive="True"
                       MaximumWidthRequest="400" />

            <hd:TitleBar.TrailingContent>
                <ImageButton Source="settings.png"
                             hd:TitleBar.IsInteractive="True"
                             HeightRequest="20" WidthRequest="20"
                             Margin="0,0,8,0" />
            </hd:TitleBar.TrailingContent>
        </hd:TitleBar>

        <!-- Row 1: Scrollable content with custom scrollbar -->
        <hd:SmartScrollPanel Grid.Row="1"
                             ScrollBarWidth="8"
                             ThumbColor="#64748B"
                             ThumbCornerRadius="4">
            <VerticalStackLayout Padding="24" Spacing="12">
                <Label Text="Welcome" FontSize="28" FontAttributes="Bold" />
                <Label Text="This content scrolls reliably at any DPI, on any monitor."
                       FontSize="16" />
                <!-- ... more content ... -->
            </VerticalStackLayout>
        </hd:SmartScrollPanel>

        <!-- Resize grip — floats at bottom-right -->
        <hd:ResizeGrip Grid.Row="1" />

    </Grid>
</ContentPage>
```

---

## Cross-Platform Behavior

| Feature | Windows | macOS / iOS / Android |
|---------|---------|----------------------|
| SmartScrollPanel wheel interception | `WH_MOUSE_LL` hook with DPI-correct hit-testing | Standard `ScrollView` behavior (no hook needed — these platforms handle wheel events correctly) |
| SmartScrollPanel custom thumb | Custom overlay thumb with drag support | Custom overlay thumb with drag support |
| TitleBar chrome integration | `AppWindowTitleBar` + `InputNonClientPointerSource` | No-op (standard platform title bar) |
| BorderlessWindow | DWM borderless with custom chrome | No-op |
| ResizeGrip | Native `WM_NCLBUTTONDOWN` resize | No-op (platform handles resize) |
| IWindowScaleService | Empirical DPI detection | Returns scale = 1.0 |
| IWindowChromeService | Full caption button metrics | No-op |
| IPointerInterceptService | Global `WH_MOUSE_LL` hook | No-op |

All controls compile and run on all platforms. Windows-specific behavior is behind `#if WINDOWS` conditionals with no-op fallbacks on other platforms.

---

## Troubleshooting

**Scrolling doesn't work at all:**
Ensure `UseHopDevControls()` is called in `MauiProgram.cs`. Check debug output for `[SmartScrollPanel] Hook ✓` — if you see `Hook ✗`, the hook failed to install.

**Scrolling works at 100% but breaks at 150%:**
The `PerMonitorV2` manifest is missing. Check debug output for the warning: `"Win32 DPI=96 but XamlRoot=1.50"`. See the PerMonitorV2 Manifest section.

**TitleBar caption spacer is 0px wide:**
The `AppWindowTitleBar` wasn't fully initialized when the TitleBar attached. The control retries automatically (up to 5 times with exponential backoff). Check debug output for `[TitleBar] Caption insets zero — retry`. If retries exhaust, the title bar may have attached before the native window was ready.

**Content overlaps caption buttons:**
Ensure you're using `<hd:TitleBar>` instead of manually calling `ExtendContentIntoTitleBar`. The TitleBar control automatically measures caption buttons and reserves space via an internal spacer column.

**Clicks don't work on controls inside the title bar:**
Add `hd:TitleBar.IsInteractive="True"` to any custom control. Standard types (Button, Entry, SearchBar, etc.) are auto-detected.

**Border is white in dark mode:**
Call `builder.UseBorderlessWindow()` — it enables `DWMWA_USE_IMMERSIVE_DARK_MODE` and auto-syncs on theme changes. If you toggle themes manually, call `BorderlessWindowExtensions.UpdateBorderColor(isDark)`.

---

## Project Structure

```
HopDev.Maui.Controls/
├── src/HopDev.Maui.Controls/
│   ├── Controls/
│   │   ├── TitleBar.cs                 — Custom title bar (three-zone, caption-aware)
│   │   ├── SmartScrollPanel.cs         — DPI-correct scrolling + custom thumb
│   │   ├── SmartScrolledEventArgs.cs   — Scroll event args
│   │   └── ResizeGrip.cs              — Corner resize grip
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs — UseHopDevControls() + service lifecycle
│   │   └── BorderlessWindowExtensions.cs  — UseBorderlessWindow() + DWM dark mode
│   └── Platform/
│       ├── Abstractions/               — IWindowScaleService, IWindowChromeService,
│       │                                 IPointerInterceptService, event args
│       ├── Types/                      — PhysicalPoint, LogicalPoint value types
│       ├── Windows/                    — Win32 implementations
│       ├── Mac/                        — (future)
│       ├── NoOp*.cs                    — Mobile/fallback implementations
│       └── HopDevServices.cs          — Per-Window attached property accessor
├── samples/HopDev.Maui.Controls.Sample/ — Standalone demo app
├── docs/                               — Architecture and design docs
├── pack-local.cmd / pack-local.sh      — Local NuGet testing scripts
└── nuget.consumer.config               — Template for consumer apps using local feed
```

---

## API Reference Summary

### Namespaces

| Namespace | Contents |
|-----------|----------|
| `HopDev.Maui.Controls.Controls` | `TitleBar`, `SmartScrollPanel`, `SmartScrolledEventArgs`, `ResizeGrip` |
| `HopDev.Maui.Controls.Extensions` | `ServiceCollectionExtensions` (`UseHopDevControls`), `BorderlessWindowExtensions` (`UseBorderlessWindow`) |
| `HopDev.Maui.Controls.Platform` | `HopDevServices` (per-Window service accessor) |
| `HopDev.Maui.Controls.Platform.Abstractions` | `IWindowScaleService`, `IWindowChromeService`, `IPointerInterceptService`, event args |
| `HopDev.Maui.Controls.Platform.Types` | `PhysicalPoint`, `LogicalPoint` |

### XAML Namespace

```xml
xmlns:hd="clr-namespace:HopDev.Maui.Controls.Controls;assembly=HopDev.Maui.Controls"
```

### Extension Methods

```csharp
using HopDev.Maui.Controls.Extensions;
```

| Method | Target | Description |
|--------|--------|-------------|
| `UseHopDevControls()` | `MauiAppBuilder` | Registers platform service lifecycle. Call once in `MauiProgram.cs`. |
| `UseBorderlessWindow()` | `MauiAppBuilder` | Configures borderless window chrome. Call after `UseHopDevControls()`. |
| `EnsureHopDevServicesAttached(Window)` | static | Force-attach services to a window. Called automatically by controls; use for manual service access. |
| `DetachHopDevServices(Window)` | static | Cleanup services for a window. Called automatically on window destroy. |
| `UpdateBorderColor(bool dark)` | static | Update DWM border for manual theme toggling. |

---

## License

[MIT](LICENSE)
