using Palabravo.ViewModels;
using Palabravo.Services.Monetization;
using Palabravo.Core.Monetization;

namespace Palabravo.Views;

public partial class RankingPage : ContentPage
{
    private readonly RankingViewModel _viewModel;
    private readonly IMonetizationService _monetization;

    public RankingPage(RankingViewModel viewModel, IMonetizationService monetization)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _monetization = monetization;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _monetization.StateChanged += RefreshBanner;
        await _viewModel.LoadAsync();
        MonetizationBanner.Attach(BannerHost, _monetization);
    }

    private void RefreshBanner() => MainThread.BeginInvokeOnMainThread(() =>
        MonetizationBanner.Attach(BannerHost, _monetization));

    protected override void OnDisappearing()
    {
        _monetization.StateChanged -= RefreshBanner;
        base.OnDisappearing();
    }
}
