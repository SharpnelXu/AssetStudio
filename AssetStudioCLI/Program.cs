using System;
using System.Linq;
using AssetStudioCLI.Extractor;

namespace AssetStudioCLI;

class Program
{
    private static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: AssetStudioCLI <mode> <additional_arguments>");
            Console.WriteLine("Modes:");
            Console.WriteLine("  list    - List assets in a file or directory");
            Console.WriteLine("  extract - Extract Sprite assets from a Nikke Catalog file");
            Console.WriteLine("Type 'AssetStudioCLI <mode>' for more information on a specific mode.");
            return;
        }
        
        var mode = args[0].ToLower();
        switch (mode)
        {
            case "list":
                AssetLister.Run(args.Skip(1).ToArray());
                return;
            case "extract":
                NikkeAssetExtractor.Run(args.Skip(1).ToArray());
                return;
        }
    }
}

