using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;

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
            var input = opts.InputPath;
            var output = opts.OutputPath;
            var template = opts.TemplateFile;

            Console.WriteLine($"输入文件路径为：{input}");
            EnsureOutput(ref output);
            Console.WriteLine($"输出文件路径为：{output}");
            Console.WriteLine($"模板文件为：{template}");

            var trans = new Transformer(output, template);
            Console.WriteLine("Processing...");
            foreach (var file in Directory.GetFiles(input, "*.cs"))
            {
                trans.Parse(file);
                Console.WriteLine("processed a file: " + file);
            }
            Console.WriteLine("Done!");
        }

        private static void EnsureOutput(ref string output)
        {
            if (string.IsNullOrEmpty(output))
                output = Path.Combine(Directory.GetCurrentDirectory());
        }
    }
}
