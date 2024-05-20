using System;
using System.IO;
using System.Xml.Linq;

namespace WaveFunctionCollapse;

public class ModelSynthesizer
{
    public static bool AttemptToGenerateModel(
        int? randomValueOverride,
        Random random,
        Model model,
        XElement xelem,
        string outputDirectoryName,
        string name
    )
    {
        int seed = randomValueOverride ?? random.Next();
        bool success = model.Run(seed, xelem.Get("limit", -1));
        if (success)
        {
            var filename = $"{outputDirectoryName}/{name} {seed}";
            var pngFilename = $"{filename}.png";
            SavePngFile(model, pngFilename);
            if (model is SimpleTiledModel stmodel && xelem.Get("textOutput", false))
            {
                SaveTxtFile(stmodel, filename);
            }
            Console.WriteLine();
            return true;
        }

        Console.WriteLine("CONTRADICTION");
        return false;
    }

    private static void SaveTxtFile(SimpleTiledModel stmodel, string filename)
    {
        var textFilename = $"{filename}.txt";
        File.WriteAllText(textFilename, stmodel.TextOutput());
        Console.Write($" & {textFilename}");
    }

    private static void SavePngFile(Model model, string pngFilename)
    {
        model.Save(pngFilename);
        Console.Write($"DONE; wrote {pngFilename}");
    }
}