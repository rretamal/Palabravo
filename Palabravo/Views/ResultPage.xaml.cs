using Palabravo.Services;
using Palabravo.ViewModels;

namespace Palabravo.Views;

public partial class ResultPage : ContentPage
{
    private readonly ResultViewModel _viewModel;
    private readonly CelebrationEffectsService _effects;
    private bool _hasAnimated;

    public ResultPage(ResultViewModel viewModel, CelebrationEffectsService effects)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _effects = effects;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
        if (_hasAnimated)
            return;

        _hasAnimated = true;
        await AnimateEntranceAsync();
    }

    private async Task AnimateEntranceAsync()
    {
        ResultEyebrow.Opacity = 0;
        ResultTitle.Opacity = 0;
        CelebrationImage.Opacity = 0;
        CelebrationImage.TranslationY = 18;
        CelebrationHalo.Scale = 0.45;
        CelebrationHalo.Opacity = 0;
        MedalIcon.Scale = 0.5;
        MedalIcon.Opacity = 0;
        MedalText.Opacity = 0;
        ResultStats.Opacity = 0;
        RankUpCard.Opacity = 0;
        RankUpCard.Scale = 0.88;
        ConfettiLeft.Opacity = ConfettiRight.Opacity = ConfettiBottom.Opacity = 0;

        await Task.WhenAll(
            ResultEyebrow.FadeToAsync(1, 180),
            ResultTitle.FadeToAsync(1, 260));

        await Task.WhenAll(
            CelebrationImage.FadeToAsync(1, 240, Easing.CubicOut),
            CelebrationImage.TranslateToAsync(0, 0, 360, Easing.SpringOut),
            CelebrationHalo.FadeToAsync(0.75, 250),
            CelebrationHalo.ScaleToAsync(1, 440, Easing.SpringOut));

        if (_viewModel.IsSuccess)
        {
            await Task.WhenAll(
                PopAsync(ConfettiLeft, -10, -8, 0),
                PopAsync(ConfettiRight, 8, -12, 55),
                PopAsync(ConfettiBottom, 8, 6, 110));
        }

        await Task.WhenAll(
            MedalIcon.FadeToAsync(1, 150),
            MedalIcon.ScaleToAsync(1, 360, Easing.SpringOut),
            MedalText.FadeToAsync(1, 240),
            ResultStats.FadeToAsync(1, 300));

        if (_viewModel.IsRankUp)
        {
            CelebrationEffectsService.PerformHaptic(true);
            _effects.PlayRankUp();
            await Task.WhenAll(
                RankUpCard.FadeToAsync(1, 180),
                RankUpCard.ScaleToAsync(1, 360, Easing.SpringOut));
        }
    }

    private static async Task PopAsync(VisualElement element, double x, double y, uint delay)
    {
        if (delay > 0)
            await Task.Delay((int)delay);

        element.Scale = 0.4;
        await Task.WhenAll(
            element.FadeToAsync(1, 130),
            element.ScaleToAsync(1, 260, Easing.SpringOut),
            element.TranslateToAsync(x, y, 260, Easing.CubicOut));
    }

    private async void OnChallengeFriendClicked(object? sender, EventArgs e)
    {
        string? shareText = null;
        try
        {
            shareText = await _viewModel.BuildShareTextAsync();
#if ANDROID
            if (ShareCard.Handler?.PlatformView is Android.Views.View nativeCard)
            {
                WinnerActions.IsVisible = false;
                try
                {
                    // Let the card reflow so action controls are not baked into the shared image.
                    await Task.Delay(50);
                    var screenshot = await Screenshot.Default.CaptureAsync(nativeCard);
                    if (screenshot is null)
                        throw new InvalidOperationException("No fue posible crear la tarjeta para compartir.");
                    var path = Path.Combine(FileSystem.CacheDirectory, "palabravo-reto.png");
                    await using (var output = File.Create(path))
                        await screenshot.CopyToAsync(output, ScreenshotFormat.Png, 100);

                    ShareBrandedCardOnAndroid(path, shareText);
                    return;
                }
                finally
                {
                    WinnerActions.IsVisible = _viewModel.IsSuccess;
                }
            }
#endif
            await _viewModel.ShareTextAsync(shareText);
        }
        catch
        {
            await _viewModel.ShareTextAsync(shareText);
        }
    }

#if ANDROID
    private static void ShareBrandedCardOnAndroid(string path, string text)
    {
        var context = Platform.AppContext;
        var file = new Java.IO.File(path);
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
            context,
            $"{context.PackageName}.fileProvider",
            file);
        using var intent = new Android.Content.Intent(Android.Content.Intent.ActionSend);
        intent.SetType("image/png");
        intent.PutExtra(Android.Content.Intent.ExtraStream, uri);
        intent.PutExtra(Android.Content.Intent.ExtraText, text);
        intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);

        var chooser = Android.Content.Intent.CreateChooser(intent, "Retar a un amigo")
            ?? throw new InvalidOperationException("No hay aplicaciones disponibles para compartir.");
        chooser.AddFlags(Android.Content.ActivityFlags.NewTask);
        context.StartActivity(chooser);
    }
#endif
}
