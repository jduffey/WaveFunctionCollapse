// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;
using WaveFunctionCollapse;

static class Program
{
    private static void Main(string[] args)
    {
        Stopwatch sw = Stopwatch.StartNew();

        const string outputDirectoryName = "output";
        Utils.PrepareOutputDirectory(outputDirectoryName);

        XDocument xdoc = XDocument.Load("samples.xml");
        var randomValueOverride = Utils.GetFlagValue(args, "--randomValueOverride");
        Random random = new Random();
        foreach (XElement xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
        {
            string name = xelem.Get<string>("name");
            Console.WriteLine($"Sample name: {name}");

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

            Model model;
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

            var maxScreenshots = randomValueOverride.HasValue ? 1 : xelem.Get("screenshots", 2);
            for (int i = 0; i < maxScreenshots; i++)
            {
                Console.WriteLine($" - Screenshot {i + 1}/{maxScreenshots}");
                var maxAttempts = randomValueOverride.HasValue ? 1 : 10;
                for (int k = 0; k < maxAttempts; k++)
                {
                    Console.Write($"  - Attempt {k + 1}/{maxAttempts} --> ");
                    if (AttemptToGenerateModel(randomValueOverride, random, model, xelem, outputDirectoryName, name)) break;
                }
            }
        }

        var elapsedMilliseconds = sw.ElapsedMilliseconds;
        Console.WriteLine($"time = {elapsedMilliseconds} ms for generating output");

        if (randomValueOverride is not null)
        {
            Utils.CalculateAndSaveHashes(outputDirectoryName, randomValueOverride.Value, elapsedMilliseconds);
        }
    }

    private static bool AttemptToGenerateModel(int? randomValueOverride, Random random, Model model, XElement xelem, string outputDirectoryName, string name)
    {
        int seed = randomValueOverride ?? random.Next();
        bool success = model.Run(seed, xelem.Get("limit", -1));
        if (success)
        {
            var filename = $"{outputDirectoryName}/{name} {seed}";
            var pngFilename = $"{filename}.png";
            model.Save(pngFilename);
            Console.Write($"DONE; wrote {pngFilename}");
            if (model is SimpleTiledModel stmodel && xelem.Get("textOutput", false))
            {
                var textFilename = $"{filename}.txt";
                File.WriteAllText(textFilename, stmodel.TextOutput());
                Console.Write($" & {textFilename}");
            }
            Console.WriteLine();
            return true;
        }

        Console.WriteLine("CONTRADICTION");
        return false;
    }
}