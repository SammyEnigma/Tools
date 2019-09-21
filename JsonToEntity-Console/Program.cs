using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JsonToEntity
{
    class Options
    {
        [Option('i', "input", Required = true, HelpText = "输入文件路径")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = false, HelpText = "输出文件路径")]
        public string OutputPath { get; set; }

        [Option('t', "template", Required = true, HelpText = "模板文件路径")]
        public string TemplateFile { get; set; }

        [Usage(ApplicationAlias = "jsonto")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example("将指定的c#类型映射到其它语言中", new Options
                    {
                        InputPath = "c:/tmp/input",
                        OutputPath = "c:/tmp/output" ,
                        TemplateFile = "c:/tmp/template/t1.cshtml"
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

            if (!EnsureTemplate(opts, out string msg3))
            {
                WriteError(msg3);
                WriteError("程序已退出！");
                return;
            }

            WriteWarn($"输入文件路径为：{opts.InputPath}");
            WriteWarn($"输出文件路径为：{opts.OutputPath}");
            WriteWarn($"模板文件为：{opts.TemplateFile}");

            var trans = new Transformer(opts.OutputPath, opts.TemplateFile);
            Console.WriteLine("processing...");
            foreach (var file in GetFiles(opts.InputPath).Where(
                p => !p.Contains("/bin") &&
                !p.Contains("/obj") &&
                !p.Contains("/debug") &&
                !p.Contains("/release")))
            {
                trans.Parse(opts.InputPath, file);
                Console.WriteLine("processed a file: " + file);
            }
            Console.WriteLine("done!");
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
                options.OutputPath = Path.Combine(Directory.GetCurrentDirectory());

            options.OutputPath = options.OutputPath.GetNormalized();
            return true;
        }

        private static bool EnsureTemplate(Options options, out string msg)
        {
            msg = string.Empty;
            if (string.IsNullOrEmpty(options.TemplateFile))
            {
                msg = "模板文件为空";
                return false;
            }

            options.TemplateFile = options.TemplateFile.GetNormalized();
            return true;
        }

        private static string[] GetFiles(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            else
                return new string[] { path };
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
