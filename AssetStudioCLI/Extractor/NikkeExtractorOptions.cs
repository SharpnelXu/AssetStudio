namespace AssetStudioCLI.Extractor;

public sealed record NikkeExtractorOptions
{
  public required string DbPath { get; init; }
  public required string ChunkPath { get; init; }
  public required string OutputDirectory { get; init; }
  public bool IsDryRun { get; init; }
  public string? PrefixFilePath { get; init; }
  public string? ManifestFilePath { get; init; }
  public string? StoreDbOutputPath { get; init; }
  public string? StoreCatalogOutputPath { get; init; }
}