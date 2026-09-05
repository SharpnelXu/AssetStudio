# AssetStudioCLI

A command-line tool for extracting and analyzing Unity asset file information using AssetStudio.


## Usage

```bash
AssetStudioCLI <mode>
```

### Modes
- `list`: Lists all assets in the specified Unity asset file.
- `extract`: Extracts Sprites from the specified Nikke DB.

Type `AssetStudioCLI <mode>` for more information on a specific mode.

## List Mode

Not maintained for now.

## Extract Mode

```
Usage:
  AssetStudioCLI extract <db_file_path> <chunk_path> <output_directory> [options]

Info:
  This tool is for data extraction after the 2026 Sep 2nd update.

Required:
  db_file_path      File path to Nikke `catalog.ndb`.
  chunk_path        Path to Nikke `store.cdb` & `store.cdb.idx`.
  output_directory  Directory where extracted assets are saved.

Options:
  --dry, -d
      Run the extraction flow without writing asset files.
      Useful with --storeCatalog to inspect available entries.

  --prefix, -p <prefix_file_path>
      Path to a file with asset prefixes to include and output folder of that prefix (csv file).
      Example prefix: `icons-char-si(hd)_assets,nikke`.

  --storeAsset, -a <asset_output_path>
      Save the extracted unity assets to this path.

  --storeDb, -b <decrypted_db_file_output_path>
      Save the decrypted database to this path.

  --storeCatalog, -c <db_entries_output_path>
      Save extracted database entries to this path.
```

For after the 2026/09/02 update. ONLY extract Sprites.

**NOTE:** If you don't provide a prefix file with `-p` the tool may run for a very long time since each catalog has
a lot of assets. Consider running with `-d -c <catalog_output_dir>` to generate a readable catalog for the prefix file
first, then use `-p <prefix_file>` to select what Sprites to extract.

Sample prefix file format (CSV): `prefix,output_dir`
```
icons-jukebox_album(hd)_assets,jukebox
icons-emblem(hd),emblem
icons-squad(hd),squad
```

This prefix file will extract all assets with the prefix `icons-jukebox_album(hd)_assets` to the output directory in `jukebox`, and so on.

Sample usage to just list catalogs:
```
AssetStudioCLI extract D:\GAMES\Nikke\NIKKE\Unity\com_proximabeta_NIKKE\com.shiftup.patch\dp\catalog.ndb D:\GAMES\Nikke\NIKKE\Unity\com_proximabeta_NIKKE\com.shiftup.patch\dp\chunk\ D:\github\AssetStudio\AssetStudioCLI\Output\dp -c D:\github\AssetStudio\AssetStudioCLI\Catalog\ -d
```

Sample usage with prefix file:
```
AssetStudioCLI extract D:\GAMES\Nikke\NIKKE\Unity\com_proximabeta_NIKKE\com.shiftup.patch\dp\catalog.ndb D:\GAMES\Nikke\NIKKE\Unity\com_proximabeta_NIKKE\com.shiftup.patch\dp\chunk\ D:\github\AssetStudio\AssetStudioCLI\Output\dp -c D:\github\AssetStudio\AssetStudioCLI\Catalog\ -p D:\github\AssetStudio\AssetStudioCLI\Prefix\icons.csv
```