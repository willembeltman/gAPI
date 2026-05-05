using gAPI.Core.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;

namespace gAPI.Core.Client;

public class ListDataSource<T, TKey>(
    IJSRuntime JS,
    Action StateHasChanged,
    Func<T, TKey> GetPrimaryKey,
    Func<int?, int?, string[]?, CancellationToken, Task<BaseListResponseT<T>>> List,
    Action<T>? SetForeignKey = null,
    Action<T>? AfterSaveAction = null,
    Action<T?>? AfterCancelAction = null,
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
    private DotNetObjectReference<ListDataSource<T, TKey>>? DotNetRef;
    private bool IsRegistrated = false;
    private CancellationTokenSource? LoadCts;
    private readonly Func<int?, int?, string[]?, CancellationToken, Task<BaseListResponseT<T>>> List = List;

    public List<ItemDataSource<T, TKey>> Items { get; } = [];
    public BaseListResponseT<T>? Response { get; private set; }
    public BaseResponse? StatusResponse { get; private set; }
    public string SentinelId { get; set; } = $"sentinel_{Guid.NewGuid()}";
    public int Take { get; set; } = 10;
    public bool HasMore { get; private set; } = true;
    public bool OrderByDirectionDesc { get; private set; } = false;
    public bool OrderByDirectionAsc => !OrderByDirectionDesc;
    public string OrderByColumn { get; private set; } = "";
    public bool ShowDropdown { get; set; } = false;
    public bool IsLoading { get; private set; } = false;

    public string[]? OrderBy =>
        string.IsNullOrEmpty(OrderByColumn)
            ? null
            : [$"{OrderByColumn} {(OrderByDirectionDesc ? "desc" : "asc")}"];

    public async Task InitialiseAsync()
    {
        await RegisterAsync();
        await LoadMoreAsync();
    }
    public async Task OnHandleFileSelected(ItemDataSource<T, TKey> item, InputFileChangeEventArgs e)
    {
        await item.HandleFileSelected(e);
    }
    public void OnCancelFileSelected(ItemDataSource<T, TKey> item)
    {
        item.CancelFileSelected();
    }
    public void OnHandleFileRemoved(ItemDataSource<T, TKey> item)
    {
        item.HandleFileRemoved();
    }
    public async Task HandleValidSubmit(ItemDataSource<T, TKey> item)
    {
        if (SetForeignKey != null) // SetForeignKey is optional
        {
            if (item.Model == null) throw new Exception("Model cannot be null.");
            SetForeignKey(item.Model);
        }
        await item.HandleValidSubmit();
    }
    public void ToggleDropdown()
    {
        ShowDropdown = !ShowDropdown;
    }
    public async Task CloseDropdown(ItemDataSource<T, TKey>? item)
    {
        if (item != null)
        {
            // Plaats gekozen item bovenaan
            Items.Remove(item);
            Items.Insert(0, item);
        }

        ShowDropdown = false;
        await UnRegisterAsync();
    }
    public async Task OrderByChanged(ChangeEventArgs e)
    {
        OrderByColumn = e.Value?.ToString() ?? "";
        Items.Clear();
        await LoadMoreAsync();
    }
    public async Task OrderByDirectionChanged(ChangeEventArgs e)
    {
        OrderByDirectionDesc = Convert.ToBoolean(e.Value);
        Items.Clear();
        await LoadMoreAsync();
    }
    public async Task OrderByColumnClicked(string columnName)
    {
        if (OrderByColumn != columnName)
        {
            OrderByColumn = columnName;
            OrderByDirectionDesc = false;
        }
        else if (!OrderByDirectionDesc)
        {
            OrderByDirectionDesc = true;
        }
        else
        {
            OrderByColumn = "";
        }
        Items.Clear();
        await LoadMoreAsync();
    }
    [JSInvokable]
    public async Task OnIntersect()
    {
        if (!IsLoading && HasMore)
            await LoadMoreAsync();
    }

    private async Task RegisterAsync()
    {
        DotNetRef ??= DotNetObjectReference.Create(this);
        if (!IsRegistrated)
        {
            await JS.InvokeVoidAsync("intersectionObserver.register", DotNetRef, SentinelId);
            IsRegistrated = true;
        }
    }
    private async Task UnRegisterAsync()
    {
        if (IsRegistrated)
        {
            await JS.InvokeVoidAsync("intersectionObserver.unregister", SentinelId);
            IsRegistrated = false;
        }
    }
    private async Task LoadMoreAsync()
    {
        LoadCts?.Cancel();
        LoadCts = new CancellationTokenSource();
        var token = LoadCts.Token;

        if (IsLoading || !HasMore) return;
        IsLoading = true;

        try
        {
            var newItems = ListItems(Items.Count, Take, OrderBy, token);
            var count = 0;

            await foreach (var item in newItems)
            {
                if (token.IsCancellationRequested) return;
                Items.Add(item);
                count++;
            }

            HasMore = count >= Take;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();

            // Fallback: als pagina korter is dan viewport, nog een keer proberen
            await JS.InvokeVoidAsync("intersectionObserver.ensureVisible", DotNetRef, SentinelId);
        }
    }
    private async IAsyncEnumerable<ItemDataSource<T, TKey>> ListItems(int? skip, int? take, string[]? orderBy, [EnumeratorCancellation] CancellationToken ct)
    {
        Response = await List(skip, take, orderBy, ct);
        StatusResponse = Response;
        if (Response?.Response == null)
        {
            yield break;
        }

        await foreach (var item in Response.Response)
        {
            yield return new ItemDataSource<T, TKey>(
                GetPrimaryKey: a => GetPrimaryKey(a),
                AfterSaveAction,
                AfterCancelAction,
                Create,
                Read,
                Update,
                Delete,
                FileUpdate,
                FileDelete)
            {
                Response = new BaseResponseT<T> { Response = item }
            };
        }
    }

    public async ValueTask DisposeAsync()
    {
        await UnRegisterAsync();
        if (LoadCts != null)
        {
            await LoadCts.CancelAsync();
            LoadCts.Dispose();
        }
        await Cts.CancelAsync();
        Cts.Dispose();
        GC.SuppressFinalize(this);
    }
}