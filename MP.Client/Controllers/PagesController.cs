using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MP.Client.Common;
using MP.Client.Common.ClientMetadata;
using MP.Client.Contexts;
using System;
using System.Data;
using System.Threading.Tasks;

namespace MP.Client.Controllers
{
    public class PagesController : Controller
    {
        private const string INPUT_SCRIPT = "main.js";
        
        private IDbConnection _connection { get; }

        public PagesController(MainContext context)
        {
            _connection = context.Database.GetDbConnection();
        }

        [Route("/{*url}")]
        public async Task<IActionResult> Index()
        {
            string path = Request.Path.Value;
            if (path != null)
            {
                if (path.StartsWith("/games"))
                    return await CreateResultWithMetadata(new GameMetadataCreator());
                else if (path.StartsWith("/search"))
                    return await CreateResultWithMetadata(new SearchMetadataCreator());
            }

            return await CreateDefaultResult();
        }

        private async Task<IActionResult> CreateResultWithMetadata(IMetadataCreator metadataCreator)
        {
            Metadata metadata = metadataCreator.CreateMetadata(_connection, Request);
            if (metadata == null)
                return await CreateDefaultResult(metadata, 404);

            return await CreateDefaultResult(metadata);
        }

        private async Task<IActionResult> CreateDefaultResult(Metadata metadata = null, int statusCode = 200)
        {
            string url = GetUrl();
            var nodeJSService = HttpContext.RequestServices.GetRequiredService<INodeJSService>();

            ClientResult clientResult = await nodeJSService.InvokeFromFileAsync<ClientResult>(INPUT_SCRIPT,
                    args: new object[] { url, metadata });

            if (clientResult.StatusCode.HasValue)
                statusCode = clientResult.StatusCode.Value;
            
            if (!String.IsNullOrEmpty(clientResult.URL))
                return Redirect(clientResult.URL);

            return new ContentResult
            {
                Content = clientResult.RenderResult,
                StatusCode = statusCode,
                ContentType = "text/html"
            };
        }

        private string GetUrl() => Request.Path.Value + Request.QueryString.Value;
    }
}
