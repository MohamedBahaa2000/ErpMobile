using ErpMobile.Pages;
using Microsoft.Maui.ApplicationModel;

namespace ErpMobile.Services;

public static class BarcodeScanCoordinator
{
    private static TaskCompletionSource<string?>? _pendingScan;

    public static async Task<string?> ScanAsync()
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

        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (Interlocked.CompareExchange(ref _pendingScan, completion, null) is not null)
        {
            throw new InvalidOperationException("A barcode scan is already in progress.");
        }

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
            Interlocked.CompareExchange(ref _pendingScan, null, completion);
        }
    }

    public static void Complete(string? barcode)
    {
        Volatile.Read(ref _pendingScan)?.TrySetResult(barcode);
    }
}
