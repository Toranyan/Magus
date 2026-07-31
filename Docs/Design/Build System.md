# Build System

## Overview

The build system is divided into four layers:

```text
Build Scripts
        ↓
 Menu Wrapper Methods
        ↓
 Shared Build Pipeline
        ↓
 Unity Build Profile
        ↓
Unity Build Pipeline
```

Responsibilities:

| Layer | Responsibility |
|--------|----------------|
| Build Scripts | Entry point for local development and CI. Launch Unity in batch mode and execute the requested build method. |
| Menu Wrapper Methods | Expose build commands to the Unity Editor. Each wrapper simply forwards to the shared build pipeline. |
| Shared Build Pipeline | Locate the requested Unity Build Profile, invoke Unity's build API, log results, and return success or failure. |
| Unity Build Profile | Native Unity Build Profile asset containing all platform-specific build configuration. |
| Unity Build Pipeline | Performs the actual build. |

The build system should be data-driven. All platform-specific build configuration belongs in Unity Build Profiles. The shared build pipeline should not contain hardcoded platform settings.

---

# Code

## Location

`Magus/Scripts/Editor/MagusBuild.cs`

Adds build commands to the Unity menu and implements the shared build pipeline.

## Public Build Methods

These methods exist only as entry points for the Unity Editor menu and command-line builds.

- `MagusBuild.BuildAndroidDevelopment()`
- `MagusBuild.BuildAndroidRelease()`
- `MagusBuild.BuildIOSDevelopment()`
- `MagusBuild.BuildIOSRelease()`
- `MagusBuild.BuildWindowsDevelopment()`
- `MagusBuild.BuildWindowsRelease()`

Each method is a thin wrapper that forwards to the shared build pipeline.

Example:

```csharp
[MenuItem("Magus/Build/Android/Release")]
public static void BuildAndroidRelease()
{
    Build("android-release");
}
```

## Shared Build Pipeline

All build logic is implemented in a single internal method.

```csharp
private static BuildResult Build(string profileName)
```

The shared build pipeline performs the following steps:

1. Locate the requested Unity Build Profile.
2. Invoke Unity's Build Profile build API.
3. Log the build results.
4. Return success or failure.

The shared build pipeline should contain no platform-specific logic.

---

# Build Menu

```
Magus
└── Build
    ├── Android
    │   ├── Development
    │   └── Release
    ├── iOS
    │   ├── Development
    │   └── Release
    └── Windows
        ├── Development
        └── Release
```

Each menu item simply calls its corresponding wrapper method.

---

# Build Profiles

## Location

`Magus/Build/Profiles`

Naming convention:

```
[platform]-[development|release].asset
```

Examples:

- `android-development.asset`
- `android-release.asset`
- `ios-development.asset`
- `ios-release.asset`
- `windows-development.asset`
- `windows-release.asset`

Each Build Profile is a native Unity Build Profile asset.

Build Profiles are the single source of truth for build configuration and define everything required to build a platform/configuration pair, including:

- Target Platform
- Development / Release configuration
- Player Settings overrides
- Build Settings
- Scene configuration
- Platform-specific settings
- Output options

The shared build pipeline should treat Build Profiles as opaque configuration assets and should not duplicate or hardcode build settings.

---

# Build Output

Builds are written to:

```
Build/
└── Output
    ├── Android
    │   ├── Development
    │   └── Release
    ├── iOS
    │   ├── Development
    │   └── Release
    └── Windows
        ├── Development
        │
        └── Release
```

Examples:

```
Build/Output/Android/Development/
Build/Output/Android/Release/
Build/Output/Windows/Release/
```

Output filenames are determined by the active Build Profile.

---

# Build Scripts

## macOS / Linux

Location:

`Magus/BuildScripts/build.sh`

Usage:

```bash
./build.sh <platform> <configuration>
```

## Windows

Location:

`Magus/BuildScripts/build.bat`

Usage:

```bat
build.bat <platform> <configuration>
```

Arguments:

| Argument | Values |
|----------|--------|
| `platform` | `android`, `ios`, `windows` |
| `configuration` | `development`, `release` |

Examples:

```bash
./build.sh android development
./build.sh android release
./build.sh windows release
```

```bat
build.bat android development
build.bat ios release
build.bat windows release
```

## Responsibilities

Build scripts are responsible for:

1. Validating command-line arguments.
2. Mapping the arguments to the corresponding public build method.
3. Launching Unity in batch mode.
4. Executing the selected `MagusBuild` wrapper method.
5. Waiting for Unity to finish.
6. Returning Unity's exit code.

Example Unity invocation:

```bash
Unity \
    -batchmode \
    -quit \
    -projectPath "<Project>" \
    -executeMethod MagusBuild.BuildAndroidRelease
```

---

# Error Handling

Build failures should:

- Log a clear error message.
- Return a non-zero exit code.
- Cause CI builds to fail.
- Stop the build immediately.

Successful builds should log:

- Build Profile
- Platform
- Configuration
- Output location
- Build duration
- Build size (if available)

---

# Extensibility

The shared build pipeline should remain generic and independent of any specific platform.

To add support for a new platform:

1. Create a new Unity Build Profile.
2. Add two wrapper methods (Development and Release) that call the shared `Build()` method.
3. Register the new Unity menu items.
4. Update the build scripts to recognize the new platform.

No changes should be required to the shared build pipeline itself.