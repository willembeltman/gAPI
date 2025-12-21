using gAPI.AutoApiClient.Configs;

namespace gAPI.AutoApiClient.Generators
{
    internal class FormFileGenerator : BaseGenerator
    {
        private readonly ClientConfig config;

        public FormFileGenerator(ClientConfig config)
        {
            this.config = config;

            Directory = config.Helpers_Destination.Directory;
            Namespace = config.Helpers_Destination.Namespace;

            Name = "FormFile";
            FileName = $"{Name}.g.cs";
        }

        public void GenerateCode()
        {
            Code = $@"
using gAPI.Attributes;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

#nullable enable
namespace {Namespace}
{{
    [IsFormFile]
    public class FormFile : IFormFile
    {{
        public FormFile(IBrowserFile browserFile, byte[] content)
        {{
            Name = browserFile.Name;
            FileName = browserFile.Name;
            ContentType = browserFile.ContentType;
            Length = browserFile.Size;
            ContentDisposition = $""form-data; name=\""{{Name}}\""; filename=\""{{FileName}}\"""";
            Buffer = content;
        }}

        public IHeaderDictionary Headers => new HeaderDictionary();

        public string Name {{ get; }}
        public string FileName {{ get; }}
        public string ContentType {{ get; }}
        public long Length {{ get; }}
        public string ContentDisposition {{ get; }}
        public byte[] Buffer {{ get; }}

        public Stream OpenReadStream()
        {{
            return new MemoryStream(Buffer);
        }}

        public void CopyTo(Stream target)
        {{
            target.Write(Buffer, 0, Buffer.Length);
        }}

        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {{
            await target.WriteAsync(Buffer, 0, Buffer.Length);
        }}
    }}
}}

";
        }
    }
}
