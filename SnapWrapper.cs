using System;
using System.Diagnostics;

public static class SnapWrapper
{
    public static void RunCommand(string command)
    {
        Console.Write("Running SNAP " + command.Split(' ')[0]);
        ProcessStartInfo ProcessInfo = new ProcessStartInfo("cmd.exe", "/c gpt " + command);
        ProcessInfo.CreateNoWindow = true;
        ProcessInfo.UseShellExecute = false;
        ProcessInfo.RedirectStandardError = true;

        Process process = new Process();
        process.StartInfo = ProcessInfo;

        process.ErrorDataReceived += (sender, args) => 
        {
            if (args == null || args.Data == null || args.Data.Contains("WARNING:") || args.Data.Contains("INFO:")) return;
            
            Console.Write(args.Data.Replace("\r", ""));     
        };

        process.Start();
        process.BeginErrorReadLine();
        process.WaitForExit();
        Console.Write("\n");
    }
}

