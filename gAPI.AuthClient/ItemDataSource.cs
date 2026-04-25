using gAPI.Dtos;
using gAPI.Storage;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace gAPI.Core.Client;

public class ItemDataSource<T, TKey>(
    Func<T, TKey?> GetPrimaryKey,
    Action<T>? AfterSaveAction,
    Action<T?>? AfterCancelAction,
    Func<T, CancellationToken, Task<BaseResponseT<T>>>? Create = null,
    Func<TKey, CancellationToken, Task<BaseResponseT<T>>>? Read = null,
    Func<T, CancellationToken, Task<BaseResponseT<T>>>? Update = null,
    Func<TKey, CancellationToken, Task<BaseResponseT<bool>>>? Delete = null,
    Func<TKey, IFormFile?, CancellationToken, Task<BaseResponseT<T>>>? FileUpdate = null,
    Func<TKey, CancellationToken, Task<BaseResponseT<bool>>>? FileDelete = null) : IAsyncDisposable
    where T : class, new()
    where TKey : struct
{
    private readonly CancellationTokenSource Cts = new();

    public BaseResponseT<T>? Response { get; set; }
    public BaseResponse? StatusResponse { get; set; }
    public IFormFile? File { get; set; }
    public bool DeleteFile { get; set; }
    public bool NewModelFlag { get; set; }
    public string? FileInputKey { get; set; } = Guid.NewGuid().ToString();

    public T? Model => Response?.Response;

    public void NewModel()
    {
        Response = new BaseResponseT<T>
        {
            Success = true,
            Response = new T()
        };
        StatusResponse = Response;
        NewModelFlag = true;
    }

    public async Task LoadModelAsync(TKey? key)
    {
        if (key == null || Read == null)
        {
            return;
        }
        Response = await Read(key.Value, Cts.Token);
        StatusResponse = Response;
        NewModelFlag = false;
    }

    public async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        File = await e.File.ToFormFileAsync();
    }

    public void CancelFileSelected()
    {
        File = null;
        FileInputKey = Guid.NewGuid().ToString(); // force rerender
    }

    public void HandleFileRemoved()
    {
        if (Model == null) throw new Exception("Model is null.");
        if (Model is IStorageFileDto storageModel)
        {
            storageModel.StorageFileUrl = null;
        }
        File = null;
        FileInputKey = Guid.NewGuid().ToString(); // force rerender
        DeleteFile = true;
    }

    public async Task HandleValidSubmit()
    {
        if (Model == null) throw new Exception("Model is null.");

        // Save Model
        var response = Response;
        if (NewModelFlag == true && Create != null)
        {
            response = await Create(Model, Cts.Token);
            StatusResponse = response;
        }
        else if (NewModelFlag == false && Update != null)
        {
            var id = GetPrimaryKey(Model) ?? throw new Exception("Cannot update without a primary key.");

            // Delete File
            if (DeleteFile)
            {
                if (Model is not IStorageFileDto) throw new Exception("Cannot delete file on a model that does not implement IStorageFileDto.");
                if (FileDelete == null) throw new Exception("Cannot delete file without a FileDelete function.");
                StatusResponse = await FileDelete(id, Cts.Token);
                if (StatusResponse.Success == false)
                {
                    throw new Exception("File deletion failed.");
                }
            }
            response = await Update(Model, Cts.Token);
            StatusResponse = response;
        }
        else
        {
            throw new Exception("No Create or Update function provided.");
        }

        if (StatusResponse.Success == false)
        {
            throw new Exception("Create or Update operation failed.");
        }

        // Save File
        if (response.Response != null && File != null)
        {
            var id = GetPrimaryKey(response.Response); // In case of new model, get the new primary key
            if (Model is not IStorageFileDto) throw new Exception("Cannot save a file on a model that does not implement IStorageFileDto.");
            if (id == null) throw new Exception("Cannot save a file without a primary key.");
            if (FileUpdate == null) throw new Exception("Cannot save a file without a FileUpdate function.");
            response = await FileUpdate(id.Value, File, Cts.Token);
            StatusResponse = response;
            File = null; // Clear file after upload
        }

        if (response.Success)
        {
            Response = response;
            AfterSaveAction?.Invoke(Model); // AfterSaveAction is optional
            FileInputKey = Guid.NewGuid().ToString(); // force rerender
        }
    }

    public async Task HandleDelete()
    {
        if (Model == null) throw new Exception("Model is null.");
        if (Delete == null) throw new Exception("Delete function is not provided.");
        var id = GetPrimaryKey(Model) ?? throw new Exception("Primary key is null.");

        // Delete File
        if (FileDelete != null) // FileDelete is optional
        {
            StatusResponse = await FileDelete(id, Cts.Token); // Callback for deletion is optional too, file doesn't have to exist
            if (StatusResponse.Success == false) throw new Exception("File deletion failed.");
        }

        // Delete Model
        StatusResponse = await Delete(id, Cts.Token);
        if (StatusResponse.Success == false)
        {
            AfterSaveAction?.Invoke(Model); // AfterSaveAction is optional
        }
    }

    public void Cancel()
    {
        AfterCancelAction?.Invoke(Model); // AfterCancelAction is optional
    }

    public async ValueTask DisposeAsync()
    {
        await Cts.CancelAsync();
        Cts.Dispose();
        GC.SuppressFinalize(this);
    }
}