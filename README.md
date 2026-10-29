# Orbit Starship Desktop

An unofficial Windows desktop companion with a 3D rocket, launch tower, flight animation, optional crew boarding, local mission statistics and scenario tests.

## Status

Preview source. The project compiles, but the latest 3D runtime and performance checks are pending. Windows Smart App Control may block an unsigned build. This repository does not include signing credentials or promise a trusted signature.

## Build

Windows with .NET Framework 4.8. Extract runtime-assets.zip in this folder, then run:

```powershell
./Build.ps1
```

Keep the resulting executable with models/ and effects/. No network connection is used by the application. Use the panel to launch, move the pad and view stats; right-click for scenario tests and Clear wreckage. Configuration and statistics are generated locally and are not included here. Wreckage persists for the current session.

The optional desktop-companion integration uses local named events; no companion photos or personal pet assets are included.

## Licensing

Project code is MIT licensed. Third-party model data is CC-BY-4.0; see THIRD-PARTY-ASSETS.md. This is not an official SpaceX product or engineering CAD.

## Privacy

See PRIVACY.md. This public copy omits saved settings, statistics, logs, personal reference images, compiled binaries, editable Blender files and original local history.
