# Build System

## Code

**Location**

`Magus/Scripts/Editor/MagusBuild.cs`

Adds build commands to the Unity menu and implements the build methods used by the build scripts.

### Available Methods

- `MagusBuild.BuildAndroidRelease`
- `MagusBuild.BuildAndroidDevelopment`
- `MagusBuild.BuildIOSRelease`
- `MagusBuild.BuildIOSDevelopment`
- `MagusBuild.BuildWindowsRelease`
- `MagusBuild.BuildWindowsDevelopment`

---

## Build Menu

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

---

## Build Profiles

**Location**

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

Each profile defines the Player Settings and build options for a specific platform and configuration.

---

## Build Scripts

### macOS / Linux

**Location**

`Magus/BuildScripts/build.sh`

Usage:

```bash
./build.sh <platform> <configuration>
```

### Windows

**Location**

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