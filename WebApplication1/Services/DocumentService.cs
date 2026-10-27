using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Scriban;
using HoneyErpAPI.Models;

namespace HoneyErpAPI.Services
{
    public class DocumentService
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB limit
        private readonly IConfiguration _config;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(IConfiguration config, ILogger<DocumentService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string RenderDocument(object model, string html)
        {
            if (model == null || string.IsNullOrWhiteSpace(html)) return string.Empty;

            var template = Template.Parse(html);

            if (template.HasErrors)
            {
                throw new InvalidOperationException("Template parsing failed: " + string.Join("\n", template.Messages));
            }

            // memberRenamer ensures properties are matched exactly (PascalCase works as expected)
            return template.Render(model, member => member.Name);
        }

    }
}
