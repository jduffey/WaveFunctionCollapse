// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

static class Program
{
    private static void Main(string[] args)
    {
        Stopwatch sw = Stopwatch.StartNew();

        const string outputDirectoryName = "output";
        PrepareOutputDirectory(outputDirectoryName);

        XDocument xdoc = XDocument.Load("samples.xml");
        var randomValueOverride = GetFlagValue(args, "--randomValueOverride");
        Random random = new Random();
        foreach (XElement xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
        {
            Model model;
            string name = xelem.Get<string>("name");
            Console.WriteLine($"< {name}");

            bool isOverlapping = xelem.Name == "overlapping";
            int size = xelem.Get("size", isOverlapping ? 48 : 24);
            int width = xelem.Get("width", size);
            int height = xelem.Get("height", size);
            bool periodic = xelem.Get("periodic", false);
            string heuristicString = xelem.Get<string>("heuristic");
            var heuristic = heuristicString switch
            {
                "Scanline" => Model.Heuristic.Scanline,
                "MRV" => Model.Heuristic.MRV,
                _ => Model.Heuristic.Entropy
            };

            if (isOverlapping)
            {
                int N = xelem.Get("N", 3);
                bool periodicInput = xelem.Get("periodicInput", true);
                int symmetry = xelem.Get("symmetry", 8);
                bool ground = xelem.Get("ground", false);

                model = new OverlappingModel(name, N, width, height, periodicInput, periodic, symmetry, ground, heuristic);
            }
            else
            {
                string subset = xelem.Get<string>("subset");
                bool blackBackground = xelem.Get("blackBackground", false);

                model = new SimpleTiledModel(name, subset, width, height, periodic, blackBackground, heuristic);
            }

            for (int i = 0; i < xelem.Get("screenshots", 2); i++)
            {
                for (int k = 0; k < 10; k++)
                {
                    Console.Write("> ");
                    int seed = randomValueOverride ?? random.Next();
                    bool success = model.Run(seed, xelem.Get("limit", -1));
                    if (success)
                    {
                        Console.WriteLine("DONE");
                        model.Save($"{outputDirectoryName}/{name} {seed}.png");
                        if (model is SimpleTiledModel stmodel && xelem.Get("textOutput", false))
                        {
                            File.WriteAllText($"{outputDirectoryName}/{name} {seed}.txt", stmodel.TextOutput());
                        }
                        break;
                    }

                    Console.WriteLine("CONTRADICTION");
                }
            }
        }

        var elapsedMilliseconds = sw.ElapsedMilliseconds;
        Console.WriteLine($"time = {elapsedMilliseconds} ms for generating output");

        if (randomValueOverride is not null)
        {
            CalculateAndSaveHashes(outputDirectoryName, randomValueOverride.Value, elapsedMilliseconds);
        }
    }

    private static int? GetFlagValue(string[] args, string flagName)
    {
        int flagNameIndex = Array.IndexOf(args, flagName);

        if (flagNameIndex >= 0 &&
            flagNameIndex + 1 < args.Length &&
            int.TryParse(args[flagNameIndex + 1], out int flagValue))
        {
            return flagValue;
        }

        return null;
    }

    private static void CalculateAndSaveHashes(string directoryPath, int randomValueOverride, long elapsedMilliseconds)
    {
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine("Directory does not exist.");
            return;
        }

        string[] filePaths = Directory.GetFiles(directoryPath);
        Array.Sort(filePaths);

        StringBuilder sb = new StringBuilder();
        foreach (string filePath in filePaths)
        {
            string hash = ComputeSha256Hash(filePath);
            string fileName = Path.GetFileName(filePath);
            Console.WriteLine($"{fileName}: {hash}");
            sb.AppendLine($"{fileName}: {hash}");
        }

        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string outputFileName = $"{timestamp}_{randomValueOverride}.txt";

        sb.AppendLine();
        sb.AppendLine($"Elapsed Milliseconds: {elapsedMilliseconds}");
        sb.AppendLine($"Runtime Version: {Environment.Version}");
        sb.AppendLine($"Operating System: {Environment.OSVersion}");
        sb.AppendLine($"Processor Count: {Environment.ProcessorCount}");
        sb.AppendLine($"Processor Architecture: {RuntimeInformation.ProcessArchitecture}");

        File.WriteAllText(outputFileName, sb.ToString());
    }

    private static string ComputeSha256Hash(string filePath)
    {
        using SHA256 sha256 = SHA256.Create();
        using FileStream fs = File.OpenRead(filePath);
        byte[] hashBytes = sha256.ComputeHash(fs);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private static void PrepareOutputDirectory(string directoryName)
    {
        var directory = Directory.CreateDirectory(directoryName);
        foreach (var file in directory.GetFiles()) file.Delete();
    }
}