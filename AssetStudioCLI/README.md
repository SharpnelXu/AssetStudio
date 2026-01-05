# AssetStudioCLI

A command-line tool for extracting and analyzing Unity asset file information using AssetStudio.

## Description

AssetStudioCLI is a console application that uses the AssetStudio library to read Unity asset files and generate detailed reports about their contents. It can process various Unity file formats including `.unity3d`, `.assetbundle`, and more.

## Usage

```bash
AssetStudioCLI <input_path> <output_directory> [options]
```

### Parameters

- `input_path`: Path to the Unity asset file or directory containing asset files to analyze
- `output_directory`: Directory where the output report(s) will be saved

### Options

- `--verbose` or `-v`: Show detailed information including object types, detailed object properties, and full metadata
- `--keep-hierarchy` or `-k`: Create separate output files for each asset file, maintaining directory structure (default: false)
- `--skip-invalid` or `-s`: Skip files starting with NKAB magic bytes (default: true) (this likely does not affect processing speed)

### Examples

```bash
# Basic usage - single file output
AssetStudioCLI myasset.unity3d C:\output

# Process entire directory with verbose output
### Single File Mode (Default)

By default, the tool generates a single `asset-info.txt` file in the output directory containing information from all processed files. Each file's data is separated with clear headers.

### Keep Hierarchy Mode

With `--keep-hierarchy`, the tool creates separate `<filename>_info.txt` files for each asset, maintaining the input directory structure in the output.

### Content

The output file(s) contain:

- **File Information**: Assets file name, original path, Unity version, platform
- **GameObject Hierarchy**: Tree structure of GameObjects with their components
- **Object Statistics** (verbose mode): Count of objects by type (Texture2D, GameObject, etc.)
- **Detailed Object Information** (verbose mode): 
# Process all files including NKAB files
AssetStudioCLI C:\assets C:\output --skip-invalid false

# Combine multiple options
AssetStudioCLI C:\assets C:\output --verbose --keep-hierarchy
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
```can process individual files or entire directories recursively
- Supports various file formats including bundles, web files, and compressed archives
- External file references in assets are automatically loaded if found in the same directory
- Files starting with NKAB magic bytes are skipped by default (can be disabled with `--skip-invalid false`)
- Files without valid assets are automatically skipped
- In verbose mode, detailed object information is limited to all objects to keep output comprehensiv

## Requirements

- .NET 8.0 Runtime
- AssetStudio library (included as project reference)

## Notes

- The tool automatically handles various file formats including bundles, web files, and compressed archives
- External file references in assets are automatically loaded if found in the same directory
- Large files are limited to showing the first 100 objects in detail to keep output manageable
