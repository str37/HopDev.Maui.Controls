# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-03-23

### Added
- **TitleBar** control — custom title bar with three-zone layout (leading, center, trailing), interactive region passthrough via `InputNonClientPointerSource`, caption button awareness, and exponential-backoff retry for caption insets
- **SmartScrollPanel** control — drop-in `ScrollView` replacement with DPI-correct pointer wheel interception via `WH_MOUSE_LL`, proper multi-monitor support
- **ResizeGrip** control — corner resize grip for borderless windows
- **BorderlessWindowExtensions** — `UseBorderlessWindow()` extension method that removes WinUI3 chrome and auto-syncs DWM border color on theme changes
- **IWindowScaleService** — empirical DPI detection using `AppWindow.Size.Width / Window.Width` (never lies under DPI virtualization), `PhysicalPoint` ↔ `LogicalPoint` conversion
- **IWindowChromeService** — title bar extension, drag regions, caption button metrics, interactive region passthrough
- **IPointerInterceptService** — centralized `WH_MOUSE_LL` hook with DPI-correct hit-testing, single hook per window
- **NoOp implementations** for all platform services (mobile/fallback)
- **`PhysicalPoint` / `LogicalPoint`** value types to prevent DPI unit confusion
- **`UseHopDevControls()`** extension method for `MauiAppBuilder` registration
- Sample application with test pages for TitleBar, SmartScrollPanel, diagnostics, drag-drop, font debug, and icon reference
- Full documentation: architecture, phased plan, backlog, SmartScrollPanel guides, 4K multi-monitor investigation, WinUI scroll fix analysis
- GitHub Actions CI and release-to-NuGet workflows
- `pack-local.cmd` / `pack-local.sh` for local NuGet testing workflow
