using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using Xamasoft.JsonClassGenerator;
using JsonDictConvert;

namespace JsonToClass
{
    class Options
    {
        [Option('i', "input", Required = true, HelpText = "输入文件路径")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = false, HelpText = "输出文件路径")]
        public string OutputPath { get; set; }

        [Option('m', "multiple", Required = false, HelpText = "是否生成单一文件")]
        public bool Multiple { get; set; }

        [Usage(ApplicationAlias = "genclass")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("将json转换成c#类", new Options
                    {
                        InputPath = "c:/tmp/input",
                        OutputPath = "c:/tmp/output",
                        Multiple = true
                    })
                };
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(opts => RunOptionsAndReturnExitCode(opts));
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            if (!EnsureInput(opts, out string msg1))
            {
                WriteError(msg1);
                WriteError("程序已退出！");
                return;
            }

            if (!EnsureOutput(opts, out string msg2))
            {
                WriteError(msg2);
                WriteError("程序已退出！");
                return;
            }

            WriteWarn($"输入文件路径为：{opts.InputPath}");
            WriteWarn($"输出文件路径为：{opts.OutputPath}");

            Console.WriteLine("processing...");
            var count = 0;
            foreach (var file in GetFiles(opts.InputPath))
            {
                var content = File.ReadAllText(file);
                content = PreProcess(content);
                var gen = new JsonClassGenerator
                {
                    Namespace = "AutoGen",
                    InternalVisibility = false,
                    UseProperties = true,
                    TargetFolder = GetOutputFilePath(opts, file),
                    MainClass = "RootObject" + (count++),
                    UsePascalCase = true,
                    SingleFile = !opts.Multiple,
                    Example = content
                };
                using (var sw = new StringWriter())
                {
                    gen.OutputStream = sw;
                    gen.GenerateClasses();
                    sw.Flush();
                }

                Console.WriteLine("processed a file: " + file);
            }
            Console.WriteLine("done!");
        }

        private static string PreProcess(string content)
        {
            var json_dict = content.ToJsonDict();
            return content;
        }

        private static bool EnsureInput(Options options, out string msg)
        {
            msg = string.Empty;
            if (string.IsNullOrEmpty(options.InputPath))
            {
                msg = "输入文件路径为空";
                return false;
            }

            if (options.InputPath.IsRoot())
            {
                msg = $"输入文件路径为{options.InputPath}，你确定要扫描整个{options.InputPath}盘？";
                return false;
            }

            options.InputPath = options.InputPath.GetNormalized();

            return true;
        }

        private static bool EnsureOutput(Options options, out string msg)
        {
            msg = string.Empty;
            if (string.IsNullOrEmpty(options.OutputPath))
            {
                options.OutputPath = Path.Combine(Directory.GetCurrentDirectory());
                return true;
            }

            options.OutputPath = options.OutputPath.GetNormalized();
            return true;
        }

        private static string[] GetFiles(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                return Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
            else
                return new string[] { path };
        }

        private static string GetOutputFilePath(Options options, string inputFile)
        {
            var relative = GetRelativePath(options.InputPath, inputFile);
            var out_path = Path.Combine(
                options.OutputPath,
                relative);

            if (!Directory.Exists(out_path))
                Directory.CreateDirectory(out_path);

            return out_path;
        }

        private static string GetRelativePath(string baseInputPath, string inputFile)
        {
            var name = Path.GetFileName(inputFile);
            if (baseInputPath == inputFile)
                return string.Empty;

            return inputFile.Replace(baseInputPath, string.Empty).Replace(name, string.Empty);
        }

        private static void WriteError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        private static void WriteWarn(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ResetColor();
        }
    }
}
