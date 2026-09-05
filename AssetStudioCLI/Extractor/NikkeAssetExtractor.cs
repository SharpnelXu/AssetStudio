using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetStudio;
using AssetStudioCLI.Extractor.Decrypt;
using AssetStudioGUI;

namespace AssetStudioCLI.Extractor;

public static class NikkeAssetExtractor
{
  public static void Run(string[] args)
  {
    if (!TryParseArgs(args, out var options)) return;

    // TODO: actual extraction logic
    Console.WriteLine("Running Nikke Asset Extractor");
    Console.WriteLine($"dbPath: {options.DbPath}");
    Console.WriteLine($"chunkPath: {options.ChunkPath}");
    Console.WriteLine($"outputDirectory: {options.OutputDirectory}");
    Console.WriteLine($"isDryRun: {options.IsDryRun}");
    Console.WriteLine($"prefixFilePath: {options.PrefixFilePath ?? "(null)"}");
    Console.WriteLine($"storeAssetOutputPath: {options.StoreAssetPath ?? "(null)"}");
    Console.WriteLine($"storeDbOutputPath: {options.StoreDbOutputPath ?? "(null)"}");
    Console.WriteLine($"storeCatalogOutputPath: {options.StoreCatalogOutputPath ?? "(null)"}");
    try
    {
      Console.WriteLine("Decrypting Nikke database: " + options.DbPath);
      var dbFile = NikkeDbDecryptor.DecryptNikkeDatabase(options);
      Console.WriteLine("Reading chunks from DB");
      var chunkMapper = new ChunkMapper(options, dbFile);
      chunkMapper.MapChunks().Wait();
      Console.WriteLine("Mapped files: " + chunkMapper.FileChunks.Count);
      chunkMapper.ReadChunks();
      Console.WriteLine("Extracted assets: " + chunkMapper.ExtractedFiles.Count);
      if (options.IsDryRun)
      {
        Console.WriteLine("Dry run complete.");
        return;
      }
      
      ExtractSprites(chunkMapper.ExtractedFiles, options.OutputDirectory);
      Console.WriteLine("Extraction complete.");
    }
    finally
    {
      var tmpDirectory = Path.Combine(".", "tmp");
      if (Directory.Exists(tmpDirectory))
      {
        Console.WriteLine("Cleaning up temporary directory.");
        Directory.Delete(tmpDirectory, true);
      }
    }
  }

  public static void ExtractSprites(List<string> extractedFiles, string outputDirectory)
  {
    Directory.CreateDirectory(outputDirectory);
    var assetManager = new AssetsManager();
    foreach (var fileName in extractedFiles)
    {
      if (!File.Exists(fileName))
      {
        continue;
      }

      assetManager.Clear();
      assetManager.LoadFiles(fileName);
      var fileInfo = new FileInfo(fileName);
      var baseName = fileInfo.Name.Split('_')[0];

      foreach (var serializedFile in assetManager.assetsFileList)
      {
        foreach (var sprite in serializedFile.Objects.Where(it => it.type == ClassIDType.Sprite))
        {
          var assetItem = new AssetItem(sprite);
          assetItem.Text = ((NamedObject)sprite).m_Name;
          var outputPath = Path.Combine(outputDirectory, baseName, "Sprite");
          Exporter.ExportSprite(assetItem, outputPath);
        }
      }
    }
  }

  private static bool TryParseArgs(string[] args, out NikkeExtractorOptions options)
  {
    options = null!;

    if (args.Length < 3)
    {
      PrintUsage();
      return false;
    }

    // Required positional arguments
    var dbPath = args[0];
    var chunkPath = args[1];
    var outputDirectory = args[2];

    // Optional arguments
    var isDryRun = false;
    string? prefixFilePath = null;
    string? storeDbOutputPath = null;
    string? assetOutputPath = null;
    string? storeCatalogOutputPath = null;

    // Parse options starting from index 3
    for (var i = 3; i < args.Length; i++)
    {
      var arg = args[i];

      switch (arg)
      {
        case "--dry":
        case "-d":
          isDryRun = true;
          break;

        case "--prefix":
        case "-p":
          if (!TryReadOptionValue(args, ref i, arg, out prefixFilePath)) return false;
          break;

        case "--storeAsset":
        case "-a":
          if (!TryReadOptionValue(args, ref i, arg, out assetOutputPath)) return false;
          break;

        case "--storeDb":
        case "-b":
          if (!TryReadOptionValue(args, ref i, arg, out storeDbOutputPath)) return false;
          break;

        case "--storeCatalog":
        case "-c":
          if (!TryReadOptionValue(args, ref i, arg, out storeCatalogOutputPath)) return false;
          break;

        default:
          Console.WriteLine($"Error: Unknown option '{arg}'.");
          return false;
      }
    }

    options = new NikkeExtractorOptions
    {
      DbPath = dbPath,
      ChunkPath = chunkPath,
      OutputDirectory = outputDirectory,
      IsDryRun = isDryRun,
      PrefixFilePath = prefixFilePath,
      StoreAssetPath = assetOutputPath,
      StoreDbOutputPath = storeDbOutputPath,
      StoreCatalogOutputPath = storeCatalogOutputPath
    };

    return true;
  }

  private static bool TryReadOptionValue(string[] args, ref int index, string option, out string value)
  {
    value = string.Empty;

    if (index + 1 >= args.Length)
    {
      Console.WriteLine($"Error: Missing value for option '{option}'.");
      return false;
    }

    value = args[++index];
    return true;
  }

  private static void PrintUsage()
  {
    Console.WriteLine("Usage:");
    Console.WriteLine("  AssetStudioCLI extract <db_file_path> <chunk_path> <output_directory> [options]");
    Console.WriteLine();
    Console.WriteLine("Info:");
    Console.WriteLine("  This tool is for data extraction after the 2026 Sep 2nd update.");
    Console.WriteLine();
    Console.WriteLine("Required:");
    Console.WriteLine("  db_file_path      File path to Nikke `catalog.ndb`.");
    Console.WriteLine("  chunk_path        Path to Nikke `store.cdb` & `store.cdb.idx`.");
    Console.WriteLine("  output_directory  Directory where extracted assets are saved.");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --dry, -d");
    Console.WriteLine("      Run the extraction flow without writing asset files.");
    Console.WriteLine("      Useful with --storeCatalog to inspect available entries.");
    Console.WriteLine();
    Console.WriteLine("  --prefix, -p <prefix_file_path> <manifest_file_path>");
    Console.WriteLine("      Path to a file with asset prefixes to include (one prefix per line).");
    Console.WriteLine("      Example prefix: `icons-char-si(hd)_assets`.");
    Console.WriteLine();
    Console.WriteLine("  --storeAsset, -a <asset_output_path>");
    Console.WriteLine("      Save the extracted unity assets to this path.");
    Console.WriteLine();
    Console.WriteLine("  --storeDb, -b <decrypted_db_file_output_path>");
    Console.WriteLine("      Save the decrypted database to this path.");
    Console.WriteLine();
    Console.WriteLine("  --storeCatalog, -c <db_entries_output_path>");
    Console.WriteLine("      Save extracted database entries to this path.");
  }
}