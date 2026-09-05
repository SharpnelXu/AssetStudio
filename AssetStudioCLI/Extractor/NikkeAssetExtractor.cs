using System;

namespace AssetStudioCLI.Extractor;

public class NikkeAssetExtractor
{
  public static void Run(string[] args)
  {
    if (args.Length < 3)
    {
      Console.WriteLine("Usage: AssetStudioCLI extract <db_path> <chunk_path> <output_directory>" +
                        " [--dry]" + 
                        " [--prefix <prefix_file_path>]" +
                        " [--manifest <manifest_file_path]" + 
                        " [--storeDb <decrypted_db_file_output_path]" +
                        " [--storeCatalog <db_entries_output_path]");
      Console.WriteLine("  db_path: The file path to the Nikke `catalog.ndb` file.");
      Console.WriteLine("  chunk_path: The file path to the Nikke `store.cdb` file.");
      Console.WriteLine("  output_directory: The directory where extracted assets will be saved.");
      Console.WriteLine("  --dry OR -d: Optional. If specified, the program will run through the flow but not attempt to output the assets. Useful for listing assets with --storeCatalog flag.");
      Console.WriteLine("  --prefix OR -p <prefix_file_path>: Optional. Path to a file containing a list of asset prefixes to filter (one asset prefix per line)." +
                        " E.g. `icons-char-si(hd)_assets`, if unsure use --dry --storeCatalog <db_entries_output_path> to list all assets first.");
      Console.WriteLine("  --manifest OR -m <manifest_file_path>: Optional. Path to a manifest file that contains previous versions of the assets." +
                        " If specified, the program will compare the current assets with the manifest and only extract new or updated assets.");
      Console.WriteLine("  --storeDb OR -b <decrypted_db_file_output_path>: Optional. Path to save the decrypted database file.");
      Console.WriteLine("  --storeCatalog OR -c <db_entries_output_path>: Optional. Path to save the extracted database entries.");
      return;
    }
    
    Console.WriteLine("Running Nikke Asset Extractor");
  }
}