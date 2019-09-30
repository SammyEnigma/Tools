using JsonToEntity.Model;
using RazorLight;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace JsonToEntity.Core
{
    public class RazorEngine : TemplateEngine
    {
        private RazorLightEngine _engine;
        public RazorEngine(string template)
            : base(template)
        {
            if (_template_path.IsRelativePath())
                _template_path = Path.GetFullPath(_template_path);

            _engine = new RazorLightEngineBuilder()
             .UseFilesystemProject(_template_path)
             .UseMemoryCachingProvider()
             .Build();
        }

        public override string ParseLangFromTemplate()
        {
            var content = File.ReadAllText(_template);
            var match = Regex.Match(content, "Language = \".+\";");
            if (match.Success)
                return match.Value.Split('"')[1];

            throw new System.InvalidOperationException("请指定模板文件的语言选项：@{ Language = \"c#\"; }");
        }

        public override string Render(List<ClassInfo> model)
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
