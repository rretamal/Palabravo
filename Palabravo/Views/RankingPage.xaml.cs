using Palabravo.ViewModels;

namespace Palabravo.Views;

public partial class RankingPage : ContentPage
{
    private readonly RankingViewModel _viewModel;

    public RankingPage(RankingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
