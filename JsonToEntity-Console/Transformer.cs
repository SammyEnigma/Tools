using JsonDictConvert;
using JsonToEntity.Core;
using JsonToEntity.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using FieldInfo = JsonToEntity.Model.FieldInfo;
using JsonDict = System.Collections.Generic.Dictionary<string, object>;

namespace JsonToEntity
{
    public class Transformer
    {
        private string _commentTemplate = @"@using JsonToEntity.Model
@using RazorLight
@inherits TemplatePage<List<ClassInfo>>
@{
    DisableEncoding = true;
}
@{ foreach (var us in Model[0].Using)
{ @(us.NewLine()) }
@(string.Empty.NewLine())
}
namespace @Model[0].Namespace
{
@foreach (var classInfo in Model)
{
    foreach (var com in classInfo.CommentStr)
    {
        @(com.Indent().NewLine())
    }
    @((""public partial class "" + classInfo.Name).Indent().NewLine())
    @(""{"".Indent().NewLine())
    foreach (var field in classInfo.Fields)
    {
        foreach (var com in field.CommentStr)
        {
            @(com.Indent(8).NewLine())
        }
        foreach (var att in field.AttributeStr)
        {
            @(att.Indent(8).NewLine())
        }
        @($""public {@field.TypeStr} {@field.Name} {{ set; get; }}"".Indent(8).NewLine())
    }
    @(""}"".Indent().NewLine().NewLine())
}
}";

        private string _output;
        private string _commentPath;
        private string _lang;
        private TemplateEngine _engine;
        private RazorLightEngine _commentEngine;
        private ITypeMapper _typeMapper;
        private ICommentFormatter _commentFormatter;
        private Dictionary<string, JsonDict> _backupComments;

        public Transformer(string output, string template, string commentPath = "")
        {
            _output = output;
            _commentPath = commentPath;
            _backupComments = new Dictionary<string, JsonDict>();
            _engine = new RazorEngine(template);
            _commentEngine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build();
            _lang = _engine.ParseLangFromTemplate();
            SetTypeMapper();
            SetCommentFormatter();
        }

        public void Parse(string baseInputPath, string inputFile)
        {
            // 加载文件
            var content = File.ReadAllText(inputFile);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var coreLocation = typeof(object).GetTypeInfo().Assembly.Location;

            // 编译dll
            var refPaths = new[] {
                coreLocation,
                Path.Combine(new FileInfo(coreLocation).DirectoryName, "netstandard.dll"),
                Path.Combine(new FileInfo(coreLocation).DirectoryName, "System.Runtime.dll"),
                typeof(JsonSerializer).GetTypeInfo().Assembly.Location
            };
            MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
            var compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var semModel = compilation.GetSemanticModel(tree);

            var list = ParseClassInfo(root, compilation, semModel);
            // 处理备份注释信息逻辑
            if (!string.IsNullOrEmpty(_commentPath))
                DoWithComment(inputFile, content, list);

            var parsed = RenderClass(list);
            Output(baseInputPath, inputFile, parsed);
        }

        private void DoWithComment(string inputFile, string content, List<ClassInfo> list)
        {
            var comment_file = Path.Combine(
                    _output,
                    $"{Path.GetFileNameWithoutExtension(inputFile)}_{CalculateMD5Hash(inputFile)}.comment");
            if (content.IndexOf("/// <summary>") > 0)
            {
                // 被解析文件存在summary
                // 无论之前是否存在comment备份文件，总是以该cs文件的summary为准

                // 确定输出comment文件路径
                // 输出comment文件
                var comments = JsonConvert.SerializeObject(list, Formatting.Indented);
                File.WriteAllText(comment_file, comments);
            }
            else
            {
                // 如果被解析cs文件中没有summary
                // 判断是否之前已经存在comment备份文件
                if (File.Exists(comment_file))
                {
                    // 读取之前备份的comment文件
                    var comments_str = File.ReadAllText(comment_file);
                    JObject[] json_arr = null;
                    using (var sr = new StringReader(comments_str))
                    using (var reader = new JsonTextReader(sr))
                    {
                        var json = JToken.ReadFrom(reader);
                        if (json is JArray)
                            json_arr = ((JArray)json).Cast<JObject>().ToArray();
                    }

                    foreach (var item in json_arr)
                    {
                        var dict = item.ToObject<JsonDict>();
                        _backupComments.Add(item.GetValue("Name").ToString(), dict);
                    }

                    foreach (var cinfo in list)
                    {
                        JsonDict cdict = null;
                        if (_backupComments.TryGetValue(cinfo.Name, out cdict))
                            cinfo.RawComment = cdict.GetString("RawComment");
                        foreach (var finfo in cinfo.Fields)
                        {
                            if (cdict != null)
                            {
                                var tmp = cdict.GetList<JsonDict>("Fields")
                                    .FirstOrDefault(p => p.GetString("Name") == finfo.Name);
                                if (tmp != null)
                                    finfo.RawComment = tmp.GetString("RawComment");
                            }
                        }
                    }

                    PreProcess(list);
                    var parsed = string.Empty;
                    var cacheResult = _commentEngine.TemplateCache.RetrieveTemplate("comment_key");
                    if (cacheResult.Success)
                        parsed = _commentEngine.RenderTemplateAsync(cacheResult.Template.TemplatePageFactory(), list).Result;
                    else
                        parsed = _commentEngine.CompileRenderAsync("comment_key", _commentTemplate, list).Result;
                    File.WriteAllBytes(inputFile, Encoding.UTF8.GetBytes(parsed));
                }
                else
                {
                    // 没有summary且之前也不存在comment备份文件的情况
                    // 什么也不做
                }
            }
        }

        private List<ClassInfo> ParseClassInfo(CompilationUnitSyntax root, CSharpCompilation compilation, SemanticModel semModel)
        {
            // 解析信息（只包含基本名称、注释信息）
            var list = new List<ClassInfo>();
            var us = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(p => p.ToString()).ToList();
            var ns = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
            var ns_symbol = semModel.GetDeclaredSymbol(ns) as INamespaceSymbol;
            foreach (var item in GetAllTypes(compilation.GetCompilationNamespace(ns_symbol)))
            {
                var cinfo = new ClassInfo();
                cinfo.Using = us;
                cinfo.Name = item.Name;
                JsonDict cdict = null;
                if (_backupComments.TryGetValue(cinfo.Name, out cdict))
                    cinfo.RawComment = cdict.GetString("RawComment");
                else
                    cinfo.RawComment = item.GetDocumentationCommentXml();
                cinfo.Fields = new List<FieldInfo>();
                foreach (var member in item.GetMembers().Where(p => p.Kind == SymbolKind.Property))
                {
                    var field = new FieldInfo();
                    field.Name = member.Name;
                    if (cdict != null)
                    {
                        var tmp = cdict.GetList<JsonDict>("Fields")
                            .FirstOrDefault(p => p.GetString("Name") == field.Name);
                        if (tmp != null)
                            field.RawComment = tmp.GetString("RawComment");
                        else
                            field.RawComment = member.GetDocumentationCommentXml();
                    }
                    else
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
                        cinfo.FullName = type.FullName;
                        cinfo.Namespace = type.Namespace;
                        foreach (var prop in type.GetProperties())
                        {
                            var finfo = cinfo.Fields.Find(p => p.Name == prop.Name);
                            finfo.RawType = prop.PropertyType;
                            finfo.AttributeStr = GetAttributeStr(prop, out var rawname);
                            finfo.RawName = rawname;
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

            return list;
        }

        private List<string> GetAttributeStr(PropertyInfo pinfo, out string rawPropName)
        {
            rawPropName = string.Empty;
            var ret = new List<string>();
            var arrs = pinfo.GetCustomAttributes();
            foreach (var item in arrs)
            {
                if (item is JsonPropertyAttribute)
                {
                    rawPropName = ((JsonPropertyAttribute)item).PropertyName;
                    ret.Add($"[JsonProperty(\"{rawPropName}\")]");
                }
                // 可能还会有其它需要的attribute，在此添加即可
            }

            return ret;
        }

        public string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
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
            return _engine.Render(info);
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
                    finfo.TypeStr = _typeMapper.MapType(finfo);
            }
        }

        private void FormatComment(List<ClassInfo> list)
        {
            foreach (var cinfo in list)
            {
                cinfo.CommentStr = _commentFormatter.Format(cinfo.RawComment);
                foreach (var finfo in cinfo.Fields)
                    finfo.CommentStr = _commentFormatter.Format(finfo.RawComment);
            }
        }

        private void SetTypeMapper()
        {
            switch (_lang)
            {
                case "c#":
                case "csharp":
                    {
                        _typeMapper = new CSharpTypeMapper();
                    }
                    break;
                default: throw new NotSupportedException($"暂不支持的语言类型：{_lang}");
            }
        }

        private void SetCommentFormatter()
        {
            switch (_lang)
            {
                case "c#":
                case "csharp":
                    {
                        _commentFormatter = new CSharpCommentFormatter();
                    }
                    break;
                default: throw new NotSupportedException($"暂不支持的语言类型：{_lang}");
            }
        }

        private string GetOutFileExtension()
        {
            switch (_lang)
            {
                case "c#":
                case "csharp":
                    {
                        return "cs";
                    }
                default: return string.Empty;
            }
        }

        private void Output(string baseInputPath, string inputFile, string content)
        {
            var relative = GetRelativePath(baseInputPath, inputFile);
            var name = Path.GetFileNameWithoutExtension(inputFile);
            var extension = GetOutFileExtension();

            var out_path = Path.Combine(
                _output,
                relative);
            var out_file = Path.Combine(
                out_path,
                $"{name}_out.{extension}");

            if (!Directory.Exists(out_path))
                Directory.CreateDirectory(out_path);

            File.WriteAllBytes(out_file, Encoding.UTF8.GetBytes(content));
        }

        private string GetRelativePath(string baseInputPath, string inputFile)
        {
            var name = Path.GetFileName(inputFile);
            if (baseInputPath == inputFile)
                return string.Empty;

            return inputFile.Replace(baseInputPath, string.Empty).Replace(name, string.Empty);
        }
    }
}
