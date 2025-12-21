using gAPI.AutoApiClient.Configs;

namespace gAPI.AutoApiClient.Generators
{
    internal class ToFormFileAsyncExtentionGenerator : BaseGenerator
    {
        private readonly ClientConfig config;

        public ToFormFileAsyncExtentionGenerator(ClientConfig config)
        {
            this.config = config;

            Directory = config.Helpers_Destination.Directory;
            Namespace = config.Helpers_Destination.Namespace;

            Name = "ToFormFileAsyncExtention";
            FileName = $"{Name}.g.cs";
        }

        public void GenerateCode()
        {
            Code = $@"
using gAPI.Attributes;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.IO;
using System.Threading.Tasks;

#nullable enable
namespace {Namespace}
{{
    [IsToFormFileAsyncExtention]
    public static class ToFormFileAsyncExtention
    {{
        public static async Task<FormFile> ToFormFileAsync(this IBrowserFile browserFile)
        {{
            if (browserFile.Size > int.MaxValue)
                throw new Exception(""File too big (4gb???!)"");
            var size = Convert.ToInt32(browserFile.Size);
            using var source = browserFile.OpenReadStream(5 * 1024 * 1024);
            using var ms = new MemoryStream(size);
            await source.CopyToAsync(ms);
            return new FormFile(browserFile, ms.ToArray());
        }}
    }}
}}

";
        }
    }
}
