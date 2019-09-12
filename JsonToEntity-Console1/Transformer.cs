using JsonToEntity.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using RazorLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using FieldInfo = JsonToEntity.Model.FieldInfo;

namespace JsonToEntity
{
    public class Transformer
    {
        private string _output;
        private string _template_path;
        private string _template_file;
        private RazorLightEngine _engine;
        private Dictionary<Type, string> _csharpTypeMap;
        public Transformer(string output, string template)
        {
            _output = output;
            _template_path = template.Substring(0, template.LastIndexOf('/'));
            _template_file = Path.GetFileName(template);
            _engine = new RazorLightEngineBuilder()
              .UseFilesystemProject(_template_path)
              .UseMemoryCachingProvider()
              .Build();

            _csharpTypeMap = new Dictionary<Type, string>
            {
                [typeof(byte)] = "byte",
                [typeof(sbyte)] = "sbyte",
                [typeof(short)] = "short",
                [typeof(ushort)] = "unshort",
                [typeof(int)] = "int",
                [typeof(uint)] = "uint",
                [typeof(long)] = "long",
                [typeof(ulong)] = "ulong",
                [typeof(float)] = "float",
                [typeof(double)] = "double",
                [typeof(decimal)] = "decimal",
                [typeof(bool)] = "bool",
                [typeof(string)] = "string",
                [typeof(char)] = "char",
                [typeof(Guid)] = "Guid",
                [typeof(DateTime)] = "DateTime",
                [typeof(DateTimeOffset)] = "DateTimeOffset",
                [typeof(TimeSpan)] = "TimeSpan",
                [typeof(object)] = "object"
            };
        }

        public void Parse(string inputFile)
        {
            // 加载文件
            var content = File.ReadAllText(inputFile);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = (CompilationUnitSyntax)tree.GetRoot();

            // 编译dll
            var refPaths = new[] {
                typeof(object).GetTypeInfo().Assembly.Location
            };
            MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
            var compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var semModel = compilation.GetSemanticModel(tree);

            // 解析信息（只包含基本名称、注释信息）
            var list = new List<ClassInfo>();
            var ns = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
            var ns_symbol = semModel.GetDeclaredSymbol(ns) as INamespaceSymbol;
            foreach (var item in GetAllTypes(compilation.GetCompilationNamespace(ns_symbol)))
            {
                var cinfo = new ClassInfo();
                cinfo.Name = item.Name;
                cinfo.RawComment = item.GetDocumentationCommentXml();
                cinfo.Fields = new List<FieldInfo>();
                foreach (var member in item.GetMembers().Where(p => p.Kind == SymbolKind.Property))
                {
                    var field = new FieldInfo();
                    field.Name = member.Name;
                    field.RawComment = member.GetDocumentationCommentXml();
                    cinfo.Fields.Add(field);
                }
                list.Add(cinfo);
            }

            // 再次解析，回填类型信息
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    foreach (var type in assembly.ExportedTypes)
                    {
                        var cinfo = list.Find(p => p.Name == type.Name);
                        foreach (var prop in type.GetProperties())
                        {
                            var finfo = cinfo.Fields.Find(p => p.Name == prop.Name);
                            finfo.RawType = prop.PropertyType;
                        }
                    }
                }
                else
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);
                    var err = new StringBuilder();
                    foreach (Diagnostic diagnostic in failures)
                        err.AppendLine($"\t{diagnostic.Id}: {diagnostic.GetMessage()}");
                    throw new Exception(err.ToString());
                }
            }

            var parsed = RenderClass(list);
            var out_name = _get_out_name();
            using (FileStream fs = new FileStream(out_name, FileMode.CreateNew))
            {
                var bytes = Encoding.UTF8.GetBytes(parsed);
                fs.Write(bytes, 0, bytes.Length);
            }

            string _get_out_name()
            {
                return Path.Combine(
                    _output,
                    Path.GetFileNameWithoutExtension(inputFile) + "_out.html");
            }
        }

        private IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol @namespace)
        {
            foreach (var type in @namespace.GetTypeMembers())
                foreach (var nestedType in GetNestedTypes(type))
                    yield return nestedType;

            foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
                foreach (var type in GetAllTypes(nestedNamespace))
                    yield return type;
        }

        private IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
        {
            yield return type;
            foreach (var nestedType in type.GetTypeMembers()
                .SelectMany(nestedType => GetNestedTypes(nestedType)))
                yield return nestedType;
        }

        private string RenderClass(List<ClassInfo> info)
        {
            PreProcess(info);
            var html = InnerRender(info);
            return html;
        }

        private void PreProcess(List<ClassInfo> list)
        {
            MapType(list);
            FormatComment(list);
        }

        private void MapType(List<ClassInfo> list)
        {
            foreach (var cinfo in list)
            {
                foreach (var finfo in cinfo.Fields)
                {
                    if (_csharpTypeMap.ContainsKey(finfo.RawType))
                    {
                        finfo.TypeStr = _csharpTypeMap[finfo.RawType];
                    }
                    else
                    {
                        finfo.TypeStr = finfo.RawType.Name;
                    }
                }
            }
        }

        private void FormatComment(List<ClassInfo> list)
        {
            foreach (var cinfo in list)
            {
                cinfo.CommentStr = _format(cinfo.RawComment);
                foreach (var finfo in cinfo.Fields)
                    finfo.CommentStr = _format(finfo.RawComment);
            }

            List<string> _format(string raw)
            {
                var ret = new List<string>();
                var arrs = raw.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 1; i < arrs.Length - 1; i++)
                    ret.Add(arrs[i]);

                return ret;
            }
        }

        private string InnerRender(List<ClassInfo> model)
        {
            var result = string.Empty;
            var cacheResult = _engine.TemplateCache.RetrieveTemplate(_template_file);
            if (cacheResult.Success)
                result = _engine.RenderTemplateAsync(cacheResult.Template.TemplatePageFactory(), model).Result;
            else
                result = _engine.CompileRenderAsync(_template_file, model).Result;

            return result;
        }
    }
}
