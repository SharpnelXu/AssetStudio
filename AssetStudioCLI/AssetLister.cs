using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AssetStudio;

namespace AssetStudioCLI;

public class AssetLister
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: AssetStudioCLI list <input_path> <output_directory> [--verbose] [--keep-hierarchy] [--skip-invalid]");
            Console.WriteLine("  input_path: File or directory to process");
            Console.WriteLine("  --verbose: Show detailed information (default: false)");
            Console.WriteLine("  --keep-hierarchy: Create separate files for each asset (default: false)");
            Console.WriteLine("  --skip-invalid (-s): Skip files starting with NKAB (default: true)");
            Console.WriteLine("Example: AssetStudioCLI myasset.unity3d C:\\output");
            Console.WriteLine("         AssetStudioCLI C:\\assets C:\\output --verbose");
            Console.WriteLine("         AssetStudioCLI C:\\assets C:\\output --keep-hierarchy");
            return;
        }
        
        string inputPath = args[0];
        string outputDir = args[1];
        bool verbose = args.Any(a => a == "--verbose" || a == "-v");
        bool keepHierarchy = args.Any(a => a == "--keep-hierarchy" || a == "-k");
        bool skipInvalid = !args.Any(a => a == "--skip-invalid" && args.Length > Array.IndexOf(args, a) + 1 && args[Array.IndexOf(args, a) + 1] == "false") && !args.Contains("-s=false");

        if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
        {
            Console.WriteLine($"Error: Input path '{inputPath}' does not exist.");
            return;
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
            Console.WriteLine($"Created output directory: {outputDir}");
        }

        Console.WriteLine("Starting processing... Mode: " + (keepHierarchy ? "Keep Hierarchy" : "Single File") + (verbose ? " with Verbose" : ""));

        try
        {
            List<string> filesToProcess = new List<string>();
            
            // Determine if input is a file or directory
            if (Directory.Exists(inputPath))
            {
                Console.WriteLine($"Scanning directory: {inputPath}");
                filesToProcess = Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories).ToList();
                Console.WriteLine($"Found {filesToProcess.Count} files to process.");
            }
            else
            {
                filesToProcess.Add(inputPath);
            }

            int processedCount = 0;
            int failedCount = 0;
            StreamWriter singleWriter = null;

            if (!keepHierarchy)
            {
                string singleOutputFile = Path.Combine(outputDir, "asset-info.txt");
                singleWriter = new StreamWriter(singleOutputFile, false, Encoding.UTF8);
                Console.WriteLine($"Writing all output to: {singleOutputFile}");
            }

            try
            {
                foreach (var file in filesToProcess)
                {
                    try
                    {
                        // Skip files starting with NKAB if skip-invalid is enabled
                        if (skipInvalid && IsNKABFile(file))
                        {
                            Console.WriteLine($"Skipping invalid file (NKAB): {file}");
                            continue;
                        }
                        
                        ProcessFile(file, outputDir, verbose, inputPath, keepHierarchy, singleWriter);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {file}: {ex.Message}");
                        failedCount++;
                    }
                }
            }
            finally
            {
                singleWriter?.Dispose();
            }

            Console.WriteLine();
            Console.WriteLine($"Processing complete!");
            Console.WriteLine($"Successfully processed: {processedCount}");
            Console.WriteLine($"Failed: {failedCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    static bool IsNKABFile(string filePath)
    {
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Length < 4)
                    return false;
                
                byte[] buffer = new byte[4];
                fs.Read(buffer, 0, 4);
                
                // Check if first 4 bytes are 'NKAB' (0x4E 0x4B 0x41 0x42)
                return buffer[0] == 0x4E && buffer[1] == 0x4B && buffer[2] == 0x41 && buffer[3] == 0x42;
            }
        }
        catch
        {
            return false;
        }
    }

    static void ProcessFile(string inputFile, string outputDir, bool verbose, string basePath, bool keepHierarchy, StreamWriter singleWriter)
    {
        // Create AssetsManager and load the file
        var manager = new AssetsManager();
        Console.WriteLine($"Loading file: {inputFile}");
        manager.LoadFiles(inputFile);

        // Skip if no assets were loaded
        if (manager.assetsFileList.Count == 0)
        {
            Console.WriteLine($"  Skipped (no assets found)");
            manager.Clear();
            return;
        }

        StreamWriter writer;
        string outputFile = null;

        if (keepHierarchy)
        {
            // Generate output file path based on input file structure
            string relativePath = Path.GetRelativePath(Path.GetDirectoryName(basePath) ?? basePath, inputFile);
            string outputFileName = Path.GetFileNameWithoutExtension(inputFile) + "_info.txt";
            string outputSubDir = Path.Combine(outputDir, Path.GetDirectoryName(relativePath) ?? "");
            
            if (!Directory.Exists(outputSubDir))
            {
                Directory.CreateDirectory(outputSubDir);
            }
            
            outputFile = Path.Combine(outputSubDir, outputFileName);
            writer = new StreamWriter(outputFile, false, Encoding.UTF8);
        }
        else
        {
            writer = singleWriter;
            writer.WriteLine("\n" + "=".PadRight(80, '='));
            writer.WriteLine($"FILE: {inputFile}");
            writer.WriteLine("=".PadRight(80, '=') + "\n");
        }

        try
        {
            if (verbose)
            {
                writer.WriteLine("=".PadRight(80, '='));
                writer.WriteLine("Asset Studio - File Information");
                writer.WriteLine("=".PadRight(80, '='));
                writer.WriteLine($"Input File: {inputFile}");
                writer.WriteLine($"Generated: {DateTime.Now}");
                writer.WriteLine("=".PadRight(80, '='));
                writer.WriteLine();

                // Print information about loaded assets files
                writer.WriteLine($"Total Assets Files Loaded: {manager.assetsFileList.Count}");
                writer.WriteLine();
            }

            foreach (var assetsFile in manager.assetsFileList)
            {
                writer.WriteLine($"Assets File: {assetsFile.fileName}");
                
                if (verbose)
                {
                    writer.WriteLine($"Original Path: {assetsFile.originalPath}");
                    writer.WriteLine($"Full Path: {assetsFile.fullName}");
                    writer.WriteLine($"Unity Version: {assetsFile.unityVersion}");
                    writer.WriteLine($"Version: {assetsFile.version}");
                    writer.WriteLine($"Platform: {assetsFile.m_TargetPlatform}");
                    writer.WriteLine($"Total Objects: {assetsFile.Objects.Count}");
                    writer.WriteLine();

                    // Group objects by type
                    var objectsByType = assetsFile.Objects
                        .GroupBy(o => o.type)
                        .OrderByDescending(g => g.Count());

                    writer.WriteLine("Object Types:");
                    foreach (var group in objectsByType)
                    {
                        writer.WriteLine($"  {group.Key,-30} Count: {group.Count()}");
                    }
                    writer.WriteLine();
                }
                writer.WriteLine();

                // Print GameObject hierarchy
                writer.WriteLine("GameObject Hierarchy:");
                writer.WriteLine();
                PrintGameObjectHierarchy(assetsFile, writer);
                writer.WriteLine();

                if (verbose)
                {
                    writer.WriteLine("Detailed Object Information:");
                    writer.WriteLine();

                    foreach (var obj in assetsFile.Objects)
                    {
                        writer.WriteLine($"  [{obj.type}] PathID: {obj.m_PathID}");
                        
                        // Add specific details based on object type
                        switch (obj)
                            {
                                case Texture2D texture:
                                    writer.WriteLine($"    Name: {texture.m_Name}");
                                    writer.WriteLine($"    Size: {texture.m_Width}x{texture.m_Height}");
                                    writer.WriteLine($"    Format: {texture.m_TextureFormat}");
                                    break;
                                case GameObject gameObj:
                                    writer.WriteLine($"    Name: {gameObj.m_Name}");
                                    writer.WriteLine($"    Components: {gameObj.m_Components.Length}");
                                    break;
                                case MonoBehaviour monoBehaviour:
                                    writer.WriteLine($"    Name: {monoBehaviour.m_Name}");
                                    if (monoBehaviour.m_Script.TryGet(out var script))
                                    {
                                        writer.WriteLine($"    Script: {script.m_ClassName}");
                                    }
                                    break;
                                case TextAsset textAsset:
                                    writer.WriteLine($"    Name: {textAsset.m_Name}");
                                    writer.WriteLine($"    Size: {textAsset.m_Script.Length} bytes");
                                    break;
                                case AudioClip audioClip:
                                    writer.WriteLine($"    Name: {audioClip.m_Name}");
                                    break;
                                case Mesh mesh:
                                    writer.WriteLine($"    Name: {mesh.m_Name}");
                                    writer.WriteLine($"    Vertices: {mesh.m_VertexCount}");
                                    break;
                                case Material material:
                                    writer.WriteLine($"    Name: {material.m_Name}");
                                    if (material.m_Shader.TryGet(out var shader))
                                    {
                                        writer.WriteLine($"    Shader: {shader.m_Name}");
                                    }
                                    break;
                                case Sprite sprite:
                                    writer.WriteLine($"    Name: {sprite.m_Name}");
                                    writer.WriteLine($"    Rect: x={sprite.m_Rect.x}, y={sprite.m_Rect.y}, w={sprite.m_Rect.width}, h={sprite.m_Rect.height}");
                                    break;
                                case AnimationClip animClip:
                                    writer.WriteLine($"    Name: {animClip.m_Name}");
                                    break;
                                default:
                                    if (obj is NamedObject namedObj && !string.IsNullOrEmpty(namedObj.m_Name))
                                    {
                                        writer.WriteLine($"    Name: {namedObj.m_Name}");
                                    }
                                    break;
                            }
                            writer.WriteLine();
                        }

                        if (assetsFile.Objects.Count > 100)
                        {
                            writer.WriteLine($"  ... and {assetsFile.Objects.Count - 100} more objects");
                            writer.WriteLine();
                    }
                }
            }

            if (verbose)
            {
                writer.WriteLine("=".PadRight(80, '='));
                writer.WriteLine("End of Report");
                writer.WriteLine("=".PadRight(80, '='));
            }

            if (keepHierarchy)
            {
                writer.Dispose();
                Console.WriteLine($"  Written to: {outputFile}");
            }
            
            Console.WriteLine($"  Assets files: {manager.assetsFileList.Count}, Total objects: {manager.assetsFileList.Sum(f => f.Objects.Count)}");

            // Cleanup
            manager.Clear();
        }
        finally
        {
            if (keepHierarchy && writer != null)
            {
                writer.Dispose();
            }
        }
    }

    

    static void PrintGameObjectHierarchy(SerializedFile assetsFile, StreamWriter writer)
    {
        // Find all GameObjects
        var gameObjects = assetsFile.Objects.OfType<GameObject>().ToList();
        
        if (gameObjects.Count == 0)
        {
            writer.WriteLine("  No GameObjects found in this file.");
            return;
        }

        // Find root GameObjects (those without a parent)
        var rootObjects = new List<GameObject>();
        var visitedTransforms = new HashSet<long>();

        foreach (var go in gameObjects)
        {
            if (go.m_Transform != null)
            {
                if (go.m_Transform.m_Father.IsNull || !go.m_Transform.m_Father.TryGet(out _))
                {
                    rootObjects.Add(go);
                }
            }
        }

        // Print each root and its children recursively
        foreach (var root in rootObjects)
        {
            PrintGameObjectRecursive(root, writer, 0, visitedTransforms);
        }

        // Print any orphaned objects that weren't visited
        var unvisitedObjects = gameObjects.Where(go => 
            go.m_Transform != null && !visitedTransforms.Contains(go.m_Transform.m_PathID)).ToList();
        
        if (unvisitedObjects.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("  Orphaned GameObjects (no valid parent chain):");
            foreach (var go in unvisitedObjects)
            {
                PrintGameObjectRecursive(go, writer, 0, visitedTransforms);
            }
        }
    }

    static void PrintGameObjectRecursive(GameObject gameObject, StreamWriter writer, int depth, HashSet<long> visitedTransforms)
    {
        if (gameObject.m_Transform == null)
            return;

        // Avoid infinite loops
        if (visitedTransforms.Contains(gameObject.m_Transform.m_PathID))
            return;
        
        visitedTransforms.Add(gameObject.m_Transform.m_PathID);

        string indent = new string(' ', depth * 2);
        string prefix = depth > 0 ? "└─ " : "";
        
        writer.Write($"{indent}{prefix}{gameObject.m_Name}");
        writer.Write($" [PathID: {gameObject.m_PathID}]");
        
        // Add component info
        var componentTypes = new List<string>();
        foreach (var component in gameObject.m_Components)
        {
            if (component.TryGet(out var comp))
            {
                if (comp.type != ClassIDType.Transform && comp.type != ClassIDType.RectTransform)
                {
                    componentTypes.Add(comp.type.ToString());
                }
            }
        }
        
        if (componentTypes.Count > 0)
        {
            writer.Write($" <{string.Join(", ", componentTypes)}>");
        }
        
        writer.WriteLine();

        // Print children
        if (gameObject.m_Transform.m_Children != null && gameObject.m_Transform.m_Children.Length > 0)
        {
            foreach (var childTransform in gameObject.m_Transform.m_Children)
            {
                if (childTransform.TryGet(out var transform))
                {
                    if (transform.m_GameObject.TryGet(out var childGameObject))
                    {
                        PrintGameObjectRecursive(childGameObject, writer, depth + 1, visitedTransforms);
                    }
                }
            }
        }
    }
}