using System.Runtime.InteropServices;
using System.Text.Json;
using ObjCRuntime;
using Palabravo.Core.Monetization;

namespace Palabravo.Services.Monetization;

public sealed class StoreKitAdapter : IPurchaseAdapter
{
    public event Action? Changed;
    private static StoreKitAdapter? observer;
    private static readonly StoreCallback UpdateCallback = Updated;
    public StoreKitAdapter()
    {
        if (!MonetizationSettings.Current.Enabled) return;
        observer = this;
        Listen(UpdateCallback);
    }
    [DllImport("__Internal", EntryPoint = "palabravo_store_listen")]
    private static extern void Listen(StoreCallback callback);
    [MonoPInvokeCallback(typeof(StoreCallback))]
    private static void Updated(nint context, nint json) => observer?.Changed?.Invoke();
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void StoreCallback(nint context, nint json);
    private static readonly StoreCallback Callback = Completed;
    [DllImport("__Internal", EntryPoint = "palabravo_store_request")]
    private static extern void Request(string operation, string argument, nint context, StoreCallback callback);

    [MonoPInvokeCallback(typeof(StoreCallback))]
    private static void Completed(nint context, nint json)
    {
        var handle = GCHandle.FromIntPtr(context);
        var completion = (TaskCompletionSource<JsonElement>)handle.Target!;
        try
        {
            var result = JsonSerializer.Deserialize<JsonElement>(Marshal.PtrToStringUTF8(json)!);
            if (result.TryGetProperty("error", out _)) completion.TrySetException(new IOException("App Store no disponible."));
            else completion.TrySetResult(result);
        }
        catch (Exception e) { completion.TrySetException(e); }
        finally { handle.Free(); }
    }

    private static Task<JsonElement> Call(string operation, string argument = "")
    {
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = GCHandle.Alloc(completion);
        try { Request(operation, argument, GCHandle.ToIntPtr(handle), Callback); }
        catch { handle.Free(); throw; }
        return completion.Task;
    }
    public async Task<StoreProduct?> GetProductAsync()
    {
        var result = await Call("product");
        return result.TryGetProperty("id", out _) ? result.Deserialize<StoreProduct>(Json) : null;
    }
    public async Task<PurchaseOutcome> PurchaseAsync()
    {
        var result = await Call("purchase");
        var status = Enum.Parse<PurchaseStatus>(result.GetProperty("status").GetString()!, true);
        return new(status, result.TryGetProperty("proof", out var proof) ? proof.Deserialize<PurchaseProof>(Json) : null);
    }
    public async Task<IReadOnlyList<PurchaseProof>> RestoreAsync(bool userInitiated = false) =>
        (await Call(userInitiated ? "restore" : "current")).GetProperty("proofs").Deserialize<List<PurchaseProof>>(Json)!;
    public async Task FinishAsync(PurchaseProof proof) => await Call("finish", proof.TransactionId);
}
