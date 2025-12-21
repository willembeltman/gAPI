using gAPI.AutoComponent.Interfaces;

namespace gAPI.AutoComponent.Generators.Helpers
{
    public class ItemDataSourceGenerator : BaseGenerator
    {
        public ItemDataSourceGenerator(
            ISharedReference baseResponseT,
            ISharedReference baseResponse,
            ISharedReference toFormFileAsync,
            string directory,
            string @namespace) : base(directory, @namespace)
        {
            BaseResponseT = baseResponseT;
            BaseResponse = baseResponse;
            ToFormFileAsync = toFormFileAsync;

            Name = "ItemDataSource";
            FileName = $"{Name}.g.cs";
        }

        public ISharedReference BaseResponseT { get; }
        public ISharedReference BaseResponse { get; }
        public ISharedReference ToFormFileAsync { get; }

        public void GenerateCode()
        {
            Reg(BaseResponseT);
            Reg(BaseResponse);
            Reg(ToFormFileAsync);
            Reg("gAPI.Storage");
            Reg("Microsoft.AspNetCore.Components.Forms");
            Reg("Microsoft.AspNetCore.Http");

            Code = $@"{GetNamespacesCode()}
#nullable enable
namespace {Namespace}
{{
    public class ItemDataSource<T, TKey> where T : class, new()
        where TKey : struct
    {{
        public ItemDataSource(
            Func<T, TKey?> GetPrimaryKey,
            Action<T>? AfterSaveAction,
            Action<T?>? AfterCancelAction,
            Func<T, Task<{BaseResponseT.Name}<T>>>? Create = null,
            Func<TKey, Task<{BaseResponseT.Name}<T>>>? Read = null,
            Func<T, Task<{BaseResponseT.Name}<T>>>? Update = null,
            Func<TKey, Task<{BaseResponseT.Name}<bool>>>? Delete = null,
            Func<TKey, IFormFile?, Task<{BaseResponseT.Name}<T>>>? FileUpdate = null,
            Func<TKey, Task<{BaseResponseT.Name}<bool>>>? FileDelete = null)
        {{
            this.GetPrimaryKey = GetPrimaryKey;
            this.AfterSaveAction = AfterSaveAction;
            this.AfterCancelAction = AfterCancelAction;
            this.Create = Create;
            this.Read = Read;
            this.Update = Update;
            this.Delete = Delete;
            this.FileUpdate = FileUpdate;
            this.FileDelete = FileDelete;
        }}

        private readonly Func<T, TKey?> GetPrimaryKey;
        private readonly Action<T>? AfterSaveAction;
        private readonly Action<T?>? AfterCancelAction;
        private readonly Func<T, Task<{BaseResponseT.Name}<T>>>? Create;
        private readonly Func<TKey, Task<{BaseResponseT.Name}<T>>>? Read;
        private readonly Func<T, Task<{BaseResponseT.Name}<T>>>? Update;
        private readonly Func<TKey, Task<{BaseResponseT.Name}<bool>>>? Delete;
        private readonly Func<TKey, IFormFile?, Task<{BaseResponseT.Name}<T>>>? FileUpdate;
        private readonly Func<TKey, Task<{BaseResponseT.Name}<bool>>>? FileDelete;

        public {BaseResponseT.Name}<T>? Response {{ get; set; }}
        public {BaseResponse.Name}? StatusResponse {{ get; set; }}
        public IFormFile? File {{ get; set; }}
        public bool DeleteFile {{ get; set; }}
        public bool NewModelFlag {{ get; set; }}
        public string? FileInputKey {{ get; set; }} = Guid.NewGuid().ToString();

        public T? Model => Response?.Response;

        public void NewModel()
        {{
            Response = new {BaseResponseT.Name}<T>
            {{
                Success = true,
                Response = new T()
            }};
            StatusResponse = Response;
            NewModelFlag = true;
        }}

        public async Task LoadModelAsync(TKey? key)
        {{
            if (key == null || Read == null)
            {{
                return;
            }}
            Response = await Read(key.Value);
            StatusResponse = Response;
            NewModelFlag = false;
        }}

        public async Task HandleFileSelected(InputFileChangeEventArgs e)
        {{
            File = await e.File.ToFormFileAsync();
        }}

        public void CancelFileSelected()
        {{
            File = null;
            FileInputKey = Guid.NewGuid().ToString(); // force rerender
        }}

        public void HandleFileRemoved()
        {{
            if (Model == null) throw new Exception(""Model is null."");
            if (Model is IStorageFileDto storageModel)
            {{
                storageModel.StorageFileUrl = null;
            }}
            File = null;
            FileInputKey = Guid.NewGuid().ToString(); // force rerender
            DeleteFile = true;
        }}

        public async Task HandleValidSubmit()
        {{
            if (Model == null) throw new Exception(""Model is null."");

            // Save Model
            var response = Response;
            if (NewModelFlag == true && Create != null)
            {{
                response = await Create(Model);
                StatusResponse = response;
            }}
            else if (NewModelFlag == false && Update != null)
            {{
                var id = GetPrimaryKey(Model);
                if (id == null) throw new Exception(""Cannot update without a primary key."");

                // Delete File
                if (DeleteFile)
                {{
                    if (Model is not IStorageFileDto) throw new Exception(""Cannot delete file on a model that does not implement IStorageFileDto."");
                    if (FileDelete == null) throw new Exception(""Cannot delete file without a FileDelete function."");
                    StatusResponse = await FileDelete(id.Value);
                    if (StatusResponse.Success == false)
                    {{
                        throw new Exception(""File deletion failed."");
                    }}
                }}
                response = await Update(Model);
                StatusResponse = response;
            }}
            else
            {{
                throw new Exception(""No Create or Update function provided."");
            }}

            if (StatusResponse.Success == false)
            {{
                throw new Exception(""Create or Update operation failed."");
            }}

            // Save File
            if (File != null)
            {{
                var id = GetPrimaryKey(Model); // In case of new model, get the new primary key
                if (Model is not IStorageFileDto) throw new Exception(""Cannot save a file on a model that does not implement IStorageFileDto."");
                if (id == null) throw new Exception(""Cannot save a file without a primary key."");
                if (FileUpdate == null) throw new Exception(""Cannot save a file without a FileUpdate function."");
                response = await FileUpdate(id.Value, File);
                StatusResponse = response;
                File = null; // Clear file after upload
            }}

            if (response.Success)
            {{
                Response = response;
                AfterSaveAction?.Invoke(Model); // AfterSaveAction is optional
                FileInputKey = Guid.NewGuid().ToString(); // force rerender
            }}
        }}

        public async Task HandleDelete()
        {{
            if (Model == null) throw new Exception(""Model is null."");
            if (Delete == null) throw new Exception(""Delete function is not provided."");
            var id = GetPrimaryKey(Model);
            if (id == null) throw new Exception(""Primary key is null."");

            // Delete File
            if (FileDelete != null) // FileDelete is optional
            {{
                StatusResponse = await FileDelete(id!.Value); // Callback for deletion is optional too, file doesn't have to exist
                if (StatusResponse.Success == false) throw new Exception(""File deletion failed."");
            }}

            // Delete Model
            StatusResponse = await Delete(id!.Value);
            if (StatusResponse.Success == false)
            {{
                AfterSaveAction?.Invoke(Model); // AfterSaveAction is optional
            }}
        }}

        public void Cancel()
        {{
            AfterCancelAction?.Invoke(Model); // AfterCancelAction is optional
        }}
    }}
}}
";
        }
    }
}
