using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using JsonToEntity.Business;
using JsonToEntity.Model;
using JsonToEntity.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using RazorLight;
using FieldInfo = JsonToEntity.Model.FieldInfo;

namespace JsonToEntity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private static ICSharpEntityRepository _csharpBusiness;
        public ValuesController(ICSharpEntityRepository CSharpBusiness) {
            _csharpBusiness = CSharpBusiness;
        }
        
        [HttpGet("csharp")]
        // POST api/values/csharp
        public ActionResult CSharpParse() {
            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = new ParseEntity().CSharpParseEntity(_csharpBusiness)
            };
        }
    }
}
