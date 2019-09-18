using JsonToEntity.Model;
using System.Collections.Generic;
using System.IO;

namespace JsonToEntity.Core
{
    public abstract class TemplateEngine
    {
        protected string _template_path;
        protected string _template_file;
        public TemplateEngine(string template)
        {
            _template_path = template.Substring(0, template.LastIndexOf('/'));
            _template_file = Path.GetFileName(template);
        }

        public abstract string ParseLangFromTemplate();
        public abstract string Render(List<ClassInfo> model);
    }
}
