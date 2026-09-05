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
    // Ordered file-key -> ordered chunk list, in the order returned by the query (ORDER BY cfm.file_offset)
    var fileChunks = new Dictionary<string, List<ChunkInfo>>();

    var connectionString = new SqliteConnectionStringBuilder
    {
      DataSource = dbFile.FullName,
      Mode = SqliteOpenMode.ReadOnly
    }.ToString();

    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    var command = connection.CreateCommand();
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

      // var prefix = prefixes.FirstOrDefault(p => fileKey.StartsWith(p, StringComparison.Ordinal));
      // if (prefix == null) continue; // not one of the desired icon assets

      if (!fileChunks.TryGetValue(fileKey, out var list))
      {
        list = [];
        fileChunks[fileKey] = list;
      }

      list.Add(new ChunkInfo { FileOffset = fileOffset, ChunkHash = chunkHash });
    }

    if (options.StoreCatalogOutputPath != null)
    {
      Directory.CreateDirectory(options.StoreCatalogOutputPath);

      var catalogFilePath = Path.Combine(options.StoreCatalogOutputPath, "catalog.txt");
      await using var catalogWriter = new FileStream(catalogFilePath, FileMode.Create, FileAccess.Write);
      foreach (var fileKey in fileChunks.Keys) catalogWriter.Write(UTF8.GetBytes(fileKey + Environment.NewLine));
    }

    foreach (var (fileKey, chunks) in fileChunks)
    {
      FileChunks[fileKey] = new FileChunkInfo
      {
        FileKey = fileKey,
        Chunks = chunks.OrderBy(c => c.FileOffset).ToList()
      };
    }
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