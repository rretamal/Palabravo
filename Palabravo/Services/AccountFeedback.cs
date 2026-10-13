namespace Palabravo.Services;

internal static class AccountFeedback
{
    public static Task ShowAsync(string message) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
#if ANDROID
        Android.Widget.Toast.MakeText(Android.App.Application.Context, message,
            Android.Widget.ToastLength.Long)?.Show();
        await Task.CompletedTask;
#else
        await Shell.Current.DisplayAlertAsync("Cuenta", message, "Cerrar");
#endif
    });
}
