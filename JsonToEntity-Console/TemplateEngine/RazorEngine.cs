using JsonToEntity.Model;
using RazorLight;
using System.Collections.Generic;

namespace JsonToEntity.Core
{
    public class RazorEngine : TemplateEngine
    {
        private RazorLightEngine _engine;
        public RazorEngine(string template)
            : base(template)
        {
            _engine = new RazorLightEngineBuilder()
             .UseFilesystemProject(_template_path)
             .UseMemoryCachingProvider()
             .Build();
        }

        public override string GetOutFileExtension()
        {
            return "html"; // 也可以因为根据生成的语言来决定，比如这里可以返回"cs"
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
