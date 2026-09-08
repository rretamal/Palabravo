using Palabravo.ViewModels;

namespace Palabravo.Views;

public partial class ChallengesPage : ContentPage
{
    private readonly ChallengesViewModel viewModel;

    public ChallengesPage(ChallengesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.RefreshAsync();
        await Task.Delay(50);

        if (viewModel.CurrentRankIndex > 0
            && RankSectionsHost.Children.ElementAtOrDefault(viewModel.CurrentRankIndex) is Element currentRank)
            await CaminoScroll.ScrollToAsync(currentRank, ScrollToPosition.Start, false);
    }

    private async void OnHelpClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(TutorialPage));
}

public sealed class RankPathView : GraphicsView
{
    public static readonly BindableProperty SectionProperty = BindableProperty.Create(
        nameof(Section),
        typeof(RankSectionViewModel),
        typeof(RankPathView),
        null,
        propertyChanged: OnSectionChanged);

    public RankSectionViewModel? Section
    {
        get => (RankSectionViewModel?)GetValue(SectionProperty);
        set => SetValue(SectionProperty, value);
    }

    private static void OnSectionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (RankPathView)bindable;
        view.Drawable = newValue is RankSectionViewModel section
            ? new RankPathDrawable(section)
            : null;
        view.Invalidate();
    }
}

internal sealed class RankPathDrawable(RankSectionViewModel section) : IDrawable
{
    private static readonly float[] VerticalPositions = [0.08f, 0.29f, 0.50f, 0.71f, 0.92f];

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        for (var index = 0; index < VerticalPositions.Length - 1; index++)
        {
            var start = Point(index, dirtyRect);
            var end = Point(index + 1, dirtyRect);
            DrawSegment(canvas, start, end, section.Nodes[index].IsCompleted);
        }
    }

    private static PointF Point(int index, RectF bounds) => new(
        bounds.Width * (index % 2 == 0 ? 0.20f : 0.80f),
        bounds.Height * VerticalPositions[index]);

    private void DrawSegment(ICanvas canvas, PointF start, PointF end, bool completed)
    {
        var controlX = (start.X + end.X) / 2;
        var path = new PathF();
        path.MoveTo(start);
        path.CurveTo(controlX, start.Y, controlX, end.Y, end.X, end.Y);
        canvas.StrokeColor = Color.FromArgb(completed ? section.Definition.AccentColor : "#DED7CF");
        canvas.StrokeSize = completed ? 6 : 5;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeDashPattern = completed ? null : [1, 2];
        canvas.DrawPath(path);
    }
}
