# AssetStudioCLI

A command-line tool for extracting and analyzing Unity asset file information using AssetStudio.

## Description

AssetStudioCLI is a console application that uses the AssetStudio library to read Unity asset files and generate detailed reports about their contents. It can process various Unity file formats including `.unity3d`, `.assetbundle`, and more.

## Usage

```bash
AssetStudioCLI <input_file> <output_directory>
```

### Parameters

- `input_file`: Path to the Unity asset file to analyze
- `output_directory`: Directory where the output report will be saved

### Example

```bash
AssetStudioCLI myasset.unity3d C:\output
```

This will:
1. Load and parse `myasset.unity3d`
2. Create the output directory if it doesn't exist
3. Generate `asset_info.txt` in the output directory with detailed information

## Output

The tool generates a text file (`asset_info.txt`) containing:

- **File Information**: Unity version, platform, file paths
- **Object Statistics**: Count of objects by type (Texture2D, GameObject, etc.)
- **Detailed Object List**: Information about individual objects including:
  - Textures (size, format)
  - GameObjects (name, components)
  - Materials (name, shader)
  - Meshes (name, vertex count)
  - Audio clips, animations, sprites, and more

## Building

Build the solution in Visual Studio or using the .NET CLI:

```bash
dotnet build AssetStudioCLI.csproj -c Release
```

The executable will be generated in `bin/Release/net8.0/AssetStudioCLI.exe`

## Requirements

- .NET 8.0 Runtime
- AssetStudio library (included as project reference)

## Notes

- The tool automatically handles various file formats including bundles, web files, and compressed archives
- External file references in assets are automatically loaded if found in the same directory
- Large files are limited to showing the first 100 objects in detail to keep output manageable
