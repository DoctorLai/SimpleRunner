/*
 Just a Simple Parallel Runner
 Author: https://steemit.com/@justyy
 
 Requires: 
   https://github.com/DoctorLai/SimpleCommandLineParametersReader
    
 More information:
   https://helloacm.com/just-a-simple-parallel-runner-in-c/
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CommandLineUtilities;

namespace SimpleRunner
{
    class Program
    {
        private static readonly Dictionary<string, string> Runners = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> RunnersParam = new Dictionary<string, string>();

        private static void Run(string runner, string param, int timeout = -1)
        {
            var startinfo = new ProcessStartInfo
            {
                FileName = runner,
                Arguments = param,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            Console.WriteLine("Process Starts: " + runner + " " + param);

            using (var p = Process.Start(startinfo))
            {
                while (!p.StandardOutput.EndOfStream)
                {
                    var line = p.StandardOutput.ReadLine();
                    Console.WriteLine(line);
                }
                if (timeout <= 0)
                {
                    p.WaitForExit();
                }
                else
                {
                    p.WaitForExit(timeout);
                }
            }

            Console.WriteLine("Process Stops: " + runner + " " + param);
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("The Simple Parallel Runner 30/09/2016");
                Console.WriteLine("https://helloacm.com");
                Console.WriteLine("Example: jobs=jobs.txt");
                Console.WriteLine("Example: jobs=jobs.txt stopifwarning=true");
                Console.WriteLine("Example: jobs=jobs.txt task1.txt task2.txt");
                Console.WriteLine("Example: a.vbs b.js timeout=1000");
                Console.WriteLine("Example: a.vbs b.js .vbs=wscript.exe .js=wscript.exe -js=/T:3");
                Console.WriteLine("Example: jobs=jobs.txt task.txt printwarning=false");
                return;
            }

            // job runner
            Runners[".vbs"] = "cscript.exe";
            Runners[".js"] = "cscript.exe";

            // job runner parameters
            RunnersParam[".vbs"] = "/NoLogo";
            RunnersParam[".js"] = "/NoLogo";

            var param = new CommandLineParametersReader(args);
            // default timeout
            var timeout = param.Get("timeout", "-1");
            var defaultTimeout = -1;
            var printwarning = param.Get("printwarning", "true");
            var stopifwarning = param.Get("stopifwarning", "false");
            if (!int.TryParse(timeout, out defaultTimeout))
            {
                if (printwarning == "true") Console.WriteLine("timeout not a valid integer = miliseconds");
                if (stopifwarning == "true")
                {
                    Console.WriteLine("stopifwarning = true");
                    return;
                }
            }
            var jobs = new List<string>();

            // get default runner paths
            foreach (var s in args)
            {
                var ss = s.Trim();
                if (string.IsNullOrWhiteSpace(ss)) continue;
                var line = ss.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (line.Length == 2)
                {
                    if (line[0].StartsWith("."))
                    {
                        Runners[line[0].ToLower()] = line[1];
                    }
                }
            }

            // get default runner parameters
            foreach (var s in args)
            {
                var ss = s.Trim();
                if (string.IsNullOrWhiteSpace(ss)) continue;
                var line = ss.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (line.Length == 2)
                {
                    if (line[0].StartsWith("-"))
                    {
                        var ext = line[0].ToLower();
                        ext = "." + ext.Substring(1);
                        if (RunnersParam.ContainsKey(ext))
                        {
                            var prev = RunnersParam[ext];
                            RunnersParam[ext] = prev + " " + line[1];
                        }
                        else
                        {
                            RunnersParam[ext] = line[1];
                        }
                    }
                }
            }

            foreach (var s in args)
            {
                var ss = s.Trim();
                if (string.IsNullOrWhiteSpace(ss)) continue;
                var line = ss.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (line.Length == 1)
                {
                    if (File.Exists(line[0]))
                    {
                        jobs.Add(line[0]);
                    }
                    else
                    {
                        if (printwarning == "true") Console.WriteLine("Warning: " + line[0] + " Not Found.");
                        if (stopifwarning == "true")
                        {
                            Console.WriteLine("stopifwarning = true");
                            return;
                        }
                    }
                }
                else
                {
                    line[0] = line[0].ToLower(CultureInfo.InvariantCulture);
                    if (line[0] == "jobs")
                    {
                        var jobsfile = line[1].Trim();
                        if (File.Exists(jobsfile))
                        {
                            var jobstxt = File.ReadAllText(jobsfile)
                                .Trim()
                                .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                            jobs.AddRange(jobstxt);
                        }
                        else
                        {
                            if (printwarning == "true") Console.WriteLine("Warning: " + line[1] + " Not Found.");
                            if (stopifwarning == "true")
                            {
                                Console.WriteLine("stopifwarning = true");
                                return;
                            }
                        }
                    }
                }
            }
            var goodjob = new List<string>();
            foreach (var job in jobs)
            {
                if (!File.Exists(job))
                {
                    if (printwarning == "true") Console.WriteLine("Warning: " + job + " Not Found.");
                    if (stopifwarning == "true")
                    {
                        Console.WriteLine("stopifwarning = true");
                        return;
                    }
                    continue;
                }
                goodjob.Add(job);
            }
            var tasks = new List<Task>();
            foreach (var job in goodjob)
            {
                var ext = Path.GetExtension(job);
                if (ext != null)
                {
                    ext = ext.ToLower(CultureInfo.InvariantCulture);
                    if (Runners.ContainsKey(ext))
                    {
                        var stask = RunnersParam.ContainsKey(ext) ? new Task(() => Run(Runners[ext], RunnersParam[ext] + " " + job)) :
                            new Task(() => Run(Runners[ext], job));
                        tasks.Add(stask);
                        stask.Start();
                        continue;
                    }
                }
                // runner not specified
                if (printwarning == "true")
                {
                    Console.WriteLine("Runner not Specified for " + job);
                    if (stopifwarning == "true")
                    {
                        return;
                    }
                }
            }
            foreach (var task in tasks)
            {
                if (defaultTimeout <= 0)
                {
                    task.Wait();
                }
                else
                {
                    task.Wait(defaultTimeout);
                }
            }
        }
    }
}
