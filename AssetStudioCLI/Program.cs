using System;
using System.Linq;

namespace AssetStudioCLI;

class Program
{
    private static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: AssetStudioCLI <mode> <additional arguments>");
            return;
        }
        
        var mode = args[0].ToLower();
        switch (mode)
        {
            case "list":
                AssetLister.Run(args.Skip(1).ToArray());
                return;
        }
    }
}

