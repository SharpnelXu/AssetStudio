using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using static System.Text.Encoding;
using Convert = System.Convert;

namespace AssetStudioCLI.Extractor.Decrypt;

public class ChunkMapper(NikkeExtractorOptions options, FileInfo dbFile)
{
  private readonly NikkeExtractorOptions options = options;
  private readonly FileInfo dbFile = dbFile;
  public readonly Dictionary<string, FileChunkInfo> FileChunks = new(); // chunks ordered by file offset
  public readonly List<string> ExtractedFiles = new();

  private const string NDB_QUERY_COMMAND = """
                                           SELECT 
                                               f.key,
                                               c.hash,
                                               cfm.file_offset
                                           FROM 
                                               files_chunktype f 
                                           JOIN
                                               chunk_file_map cfm ON f.file_id = cfm.file_id
                                           JOIN 
                                               chunks c ON cfm.chunk_id = c.chunk_id
                                           ORDER BY
                                               cfm.file_offset;
                                           """;

  public async Task MapChunks()
  {
    var prefixes = new List<string>();
    if (options.PrefixFilePath != null && File.Exists(options.PrefixFilePath))
      prefixes = (await File.ReadAllLinesAsync(options.PrefixFilePath))
        .Select(line => line.Trim())
        .Where(line => !string.IsNullOrEmpty(line))
        .ToList();

    // Ordered file-key -> ordered chunk list, in the order returned by the query (ORDER BY cfm.file_offset)
    var fileChunks = new Dictionary<string, List<ChunkInfo>>();

    var connectionString = new SqliteConnectionStringBuilder
    {
      DataSource = dbFile.FullName,
      Mode = SqliteOpenMode.ReadOnly,
      Pooling = false // avoid pooled native handle keeping dbFile locked after Close/Dispose
    }.ToString();

    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = NDB_QUERY_COMMAND;

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      var fileKey = reader.GetString(0);
      var chunkHash = ToHexString(reader.GetValue(1));
      // This is the chunk's byte offset within the reconstructed file (chunk_file_map.file_offset),
      // NOT the global chunk_id. Chunks are deduplicated/shared across files, so a chunk's global
      // chunk_id does not indicate its position within any particular file - only file_offset does.
      var fileOffset = Convert.ToInt64(reader.GetValue(2));

      var prefix = prefixes.FirstOrDefault(p => fileKey.StartsWith(p, StringComparison.Ordinal));
      if (prefixes.Count > 0 && prefix == null && options.StoreCatalogOutputPath == null)
        continue; // not one of the desired icon assets and don't need to list

      // var prefix = prefixes.FirstOrDefault(p => fileKey.StartsWith(p, StringComparison.Ordinal));
      // if (prefix == null) continue; // not one of the desired icon assets

      if (!fileChunks.TryGetValue(fileKey, out var list))
      {
        list = [];
        fileChunks[fileKey] = list;
      }

      list.Add(new ChunkInfo { FileOffset = fileOffset, ChunkHash = chunkHash });
    }

    connection.Close();

    if (options.StoreCatalogOutputPath != null)
    {
      Directory.CreateDirectory(options.StoreCatalogOutputPath);

      var catalogFilePath = Path.Combine(options.StoreCatalogOutputPath, "catalog.txt");
      await using var catalogWriter = new FileStream(catalogFilePath, FileMode.Create, FileAccess.Write);
      foreach (var fileKey in fileChunks.Keys) catalogWriter.Write(UTF8.GetBytes(fileKey + Environment.NewLine));
    }

    foreach (var (fileKey, chunks) in fileChunks)
    {
      var prefix = prefixes.FirstOrDefault(p => fileKey.StartsWith(p, StringComparison.Ordinal));
      if (prefixes.Count > 0 && prefix == null)
        continue;
      
      FileChunks[fileKey] = new FileChunkInfo
      {
        FileKey = fileKey,
        Chunks = chunks.OrderBy(c => c.FileOffset).ToList()
      };
    }
  }

  /**
   * returns the number of successfully extracted assets
   */
  public void ReadChunks()
  {
    var idxPath = Path.Combine(options.ChunkPath, "store.cdb.idx");
    var cdbPath = Path.Combine(options.ChunkPath, "store.cdb");

    if (!File.Exists(idxPath))
      throw new FileNotFoundException("store.cdb.idx not found in the specified chunk path.");

    if (!File.Exists(cdbPath))
      throw new FileNotFoundException("store.cdb not found in the specified chunk path.");

    // Gather the set of chunk hashes we care about
    var wantedHashes = new HashSet<string>(
      FileChunks.Values.SelectMany(f => f.Chunks).Select(c => c.ChunkHash),
      StringComparer.OrdinalIgnoreCase);
    var chunkLocations = new Dictionary<string, (long Offset, uint Size)>(StringComparer.OrdinalIgnoreCase);

    using (var idxStream = File.OpenRead(idxPath))
    using (var idxReader = new BinaryReader(idxStream))
    {
      var magic = new string(idxReader.ReadChars(4));
      if (magic != "CIDX") throw new InvalidDataException("Invalid CIDX file.");

      var version = idxReader.ReadUInt32();
      if (version != 1) throw new InvalidDataException($"Unsupported CIDX version: {version}");

      var entryCount = idxReader.ReadUInt32();

      for (var i = 0; i < entryCount; i++)
      {
        var hash = idxReader.ReadBytes(16);
        var offset = idxReader.ReadUInt64();
        var size = idxReader.ReadUInt32();

        var hexHash = Convert.ToHexString(hash).ToLowerInvariant();
        if (wantedHashes.Contains(hexHash)) chunkLocations[hexHash] = ((long)offset, size);
      }
    }
    
    var assetPath = options.StoreAssetPath ?? Path.Combine(".", "tmp");
    Directory.CreateDirectory(assetPath);

    using var cdbStream = File.OpenRead(cdbPath);
    using var decompressor = new ZstdSharp.Decompressor();

    foreach (var fileChunkInfo in FileChunks.Values)
    {
      using var outputStream = new MemoryStream();
      var allChunksFound = true;

      foreach (var chunk in fileChunkInfo.Chunks.OrderBy(c => c.FileOffset))
      {
        if (!chunkLocations.TryGetValue(chunk.ChunkHash, out var location))
        {
          Console.WriteLine($"- Missing chunk {chunk.ChunkHash} for {fileChunkInfo.FileKey}");
          allChunksFound = false;
          break;
        }

        cdbStream.Seek(location.Offset, SeekOrigin.Begin);
        var compressed = new byte[location.Size];
        var totalRead = 0;
        while (totalRead < compressed.Length)
        {
          var read = cdbStream.Read(compressed, totalRead, compressed.Length - totalRead);
          if (read <= 0) break;
          totalRead += read;
        }

        var decompressed = decompressor.Unwrap(compressed).ToArray();
        outputStream.Write(decompressed, 0, decompressed.Length);
      }

      if (!allChunksFound) continue;

      var safeFileName = SanitizeFileName(fileChunkInfo.FileKey);
      var destFile = Path.Combine(assetPath, safeFileName);
      File.WriteAllBytes(destFile, outputStream.ToArray());
      
      ExtractedFiles.Add(destFile);
    }
  }

  private static string SanitizeFileName(string fileName)
  {
    foreach (var c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, '_');
    return fileName;
  }

  private static string ToHexString(object? value)
  {
    return value switch
    {
      byte[] bytes => Convert.ToHexString(bytes).ToLowerInvariant().Replace("-", ""),
      null => string.Empty,
      _ => value.ToString() ?? string.Empty
    };
  }

  public class ChunkInfo
  {
    public long FileOffset { get; set; }
    public string ChunkHash { get; set; } = "";
  }

  public class FileChunkInfo
  {
    public string FileKey { get; set; } = "";
    public List<ChunkInfo> Chunks { get; set; } = new();
  }
}