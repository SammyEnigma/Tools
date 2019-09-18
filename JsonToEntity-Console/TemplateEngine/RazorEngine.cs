using JsonToEntity.Model;
using RazorLight;
using System.Collections.Generic;
using System.IO;

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
            return "c#";
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
