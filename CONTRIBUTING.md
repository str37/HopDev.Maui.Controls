# Contributing to HopDev.Maui.Controls

Thanks for your interest in contributing! This document covers the essentials.

## Getting Started

1. Fork the repo and clone locally
2. Ensure you have .NET 10 SDK + MAUI workload installed:
   ```
   dotnet workload install maui
   ```
3. Open `HopDev.Maui.Controls.sln` in Visual Studio 2022+ or Rider
4. Build and run the sample app to verify your setup:
   ```
   dotnet build samples/HopDev.Maui.Controls.Sample/HopDev.Maui.Controls.Sample.csproj
   ```

## Development Workflow

1. Create a branch from `main`
2. Make your changes in `src/HopDev.Maui.Controls/`
3. Update the sample app if adding new controls or APIs
4. Update documentation in `docs/` if applicable
5. Add an entry to `CHANGELOG.md` under `[Unreleased]`
6. Submit a pull request

## Architecture Notes

This library provides an anti-corruption layer between MAUI (logical pixels, cross-platform) and WinUI3 (physical pixels, HWND, DPI virtualization). Key principles:

- **Platform services are per-Window** — scoped via attached properties on `HopDevServices`, never DI singletons
- **NoOp fallbacks** — every platform service has a no-op implementation for unsupported platforms
- **DPI correctness** — all coordinate math uses `PhysicalPoint`/`LogicalPoint` value types to prevent unit confusion
- **Single hook** — `IPointerInterceptService` provides one `WH_MOUSE_LL` hook per window (not per control)

See [`docs/architecture.md`](docs/architecture.md) for the full design.

## Testing on Scaled Displays

DPI bugs are the primary reason this library exists. When testing:

- Test at 100%, 150%, and 200% scale
- Test with multi-monitor setups at different scales if possible
- Ensure the `PerMonitorV2` manifest is present (see sample app)
- Use the Diagnostics page in the sample app to verify coordinate conversions

## Code Style

- Follow existing patterns in the codebase
- XML doc comments on all public types and members
- `Nullable` enabled everywhere
- No `#region` blocks

## Reporting Issues

- **Bugs:** Use the Bug Report template — include display scale and monitor setup
- **Features:** Use the Feature Request template
- **Questions:** Open a Discussion

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
