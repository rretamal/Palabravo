using Palabravo.Services;
using Palabravo.ViewModels;

namespace Palabravo.Views;

public partial class GamePage : ContentPage, IQueryAttributable
{
    private readonly GameViewModel _viewModel;
    private readonly GameCoordinator _coordinator;
    private readonly CelebrationEffectsService _effects;
    private readonly Core.Services.GameplayActivity _activity;

    public GamePage(
        GameViewModel viewModel,
        GameCoordinator coordinator,
        CelebrationEffectsService effects, Core.Services.GameplayActivity activity)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _coordinator = coordinator;
        _effects = effects;
        _activity = activity;
        _viewModel.SubmissionFeedbackRequested += OnSubmissionFeedbackRequested;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var puzzleId = query.TryGetValue("puzzleId", out var puzzle) ? puzzle?.ToString() ?? "puzzle-01" : "puzzle-01";
        var mode = query.TryGetValue("mode", out var value) ? value?.ToString() ?? "Challenge" : "Challenge";
        var referred = query.TryGetValue("referred", out var referral) && referral is true;
        _ = _viewModel.LoadAsync(puzzleId, mode, referred);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _activity.Changed += OnActivityChanged;
        _activity.SetVisible(true);
        if (_coordinator.Engine.State is { IsFinished: false })
            _viewModel.RefreshFromEngine();
    }

    protected override void OnDisappearing()
    {
        _activity.SetVisible(false);
        _activity.Changed -= OnActivityChanged;
        base.OnDisappearing();
    }

    private void OnActivityChanged(bool active) => _viewModel.IsGameplayActive = active;

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnHelpClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(TutorialPage));

    private async void OnSubmissionFeedbackRequested(object? sender, SubmissionFeedbackEventArgs feedback)
    {
        CelebrationEffectsService.PerformHaptic(feedback.IsSuccess);

        if (!feedback.IsSuccess)
        {
            await ShakeAsync(WordsCollection);
            return;
        }

        if (feedback.IsFinal)
            _effects.PlayVictory();
        else
            _effects.PlayGroupFound();

        await Task.Yield();
        if (SolvedGroupsPanel.Children.LastOrDefault() is not VisualElement solvedGroup)
            return;

        solvedGroup.Opacity = 0;
        solvedGroup.Scale = 0.88;
        solvedGroup.TranslationY = 10;
        await Task.WhenAll(
            solvedGroup.FadeToAsync(1, 180, Easing.CubicOut),
            solvedGroup.ScaleToAsync(1, 280, Easing.SpringOut),
            solvedGroup.TranslateToAsync(0, 0, 220, Easing.CubicOut));
    }

    private static async Task ShakeAsync(VisualElement element)
    {
        await element.TranslateToAsync(-7, 0, 55, Easing.Linear);
        await element.TranslateToAsync(7, 0, 70, Easing.Linear);
        await element.TranslateToAsync(-4, 0, 60, Easing.Linear);
        await element.TranslateToAsync(0, 0, 55, Easing.Linear);
    }
}
