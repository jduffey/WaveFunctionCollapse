using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WaveFunctionCollapse;

public static class Utils
{
    public static int? GetFlagValue(string[] args, string flagName)
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

    public static void PrepareOutputDirectory(string directoryName)
    {
        var directory = Directory.CreateDirectory(directoryName);
        foreach (var file in directory.GetFiles()) file.Delete();
    }

    public static void CalculateAndSaveHashes(string directoryPath, int randomValueOverride, long elapsedMilliseconds)
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
}