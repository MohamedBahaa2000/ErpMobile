using ErpMobile.Pages;
using Microsoft.Maui.ApplicationModel;

namespace ErpMobile.Services;

public static class BarcodeScanCoordinator
{
    private static TaskCompletionSource<string?>? _pendingScan;
    private static Func<string, Task<bool>>? _continuousHandler;

    /// <summary>يُستدعى عند كل مسح في وضع المسح المتصل: (عدد الأصناف المضافة، رسالة الحالة).</summary>
    public static event Action<int, string>? ProgressChanged;

    /// <summary>True عندما يكون الماسح في وضع المسح المتصل (أكثر من صنف).</summary>
    public static bool IsContinuous => _continuousHandler is not null;

    public static void ReportProgress(int count, string message) => ProgressChanged?.Invoke(count, message);

    /// <summary>مسح لمرة واحدة: يعيد قيمة الباركود أو null عند الإلغاء.</summary>
    public static async Task<string?> ScanAsync()
    {
        await EnsureCameraPermissionAsync();

        var completion = CreatePendingScan();
        try
        {
            await Shell.Current.GoToAsync(nameof(BarcodeScannerPage));
            return await completion.Task;
        }
        catch
        {
            completion.TrySetResult(null);
            throw;
        }
        finally
        {
            ReleasePendingScan(completion);
        }
    }

    /// <summary>
    /// مسح متصل: يبقى الماسح مفتوحاً ويستدعي <paramref name="onBarcode"/> مع كل باركود.
    /// يعيد الماسح للشاشة السابقة عند ضغط «تم/إغلاق» أو عندما تعيد المعالجة false.
    /// </summary>
    public static async Task<string?> ScanContinuousAsync(Func<string, Task<bool>> onBarcode)
    {
        await EnsureCameraPermissionAsync();

        var completion = CreatePendingScan();
        _continuousHandler = onBarcode;
        try
        {
            await Shell.Current.GoToAsync(nameof(BarcodeScannerPage));
            return await completion.Task;
        }
        catch
        {
            completion.TrySetResult(null);
            throw;
        }
        finally
        {
            _continuousHandler = null;
            ReleasePendingScan(completion);
        }
    }

    /// <summary>تُستدعى من شاشة الماسح؛ تعيد true للاستمرار في المسح.</summary>
    internal static async Task<bool> HandleDetectedAsync(string barcode)
    {
        Func<string, Task<bool>>? handler = _continuousHandler;
        if (handler is null)
        {
            return false;
        }

        try
        {
            return await handler(barcode);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void Complete(string? barcode) =>
        Volatile.Read(ref _pendingScan)?.TrySetResult(barcode);

    private static TaskCompletionSource<string?> CreatePendingScan()
    {
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (Interlocked.CompareExchange(ref _pendingScan, completion, null) is not null)
        {
            throw new InvalidOperationException("A barcode scan is already in progress.");
        }

        return completion;
    }

    private static void ReleasePendingScan(TaskCompletionSource<string?> completion) =>
        Interlocked.CompareExchange(ref _pendingScan, null, completion);

    private static async Task EnsureCameraPermissionAsync()
    {
        PermissionStatus permission = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (permission != PermissionStatus.Granted)
        {
            permission = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (permission != PermissionStatus.Granted)
        {
            throw new UnauthorizedAccessException("Camera permission was not granted.");
        }
    }
}
