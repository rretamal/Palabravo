using Palabravo.ViewModels;
using Palabravo.Core.Monetization;
using Palabravo.Services.Monetization;

namespace Palabravo.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    private readonly IMonetizationService monetization;

    public HomePage(HomeViewModel viewModel, IMonetizationService monetization)
    {
        InitializeComponent();
        this.monetization = monetization;
        BindingContext = _viewModel = viewModel;
    }

    private void RefreshBanner() => MainThread.BeginInvokeOnMainThread(() =>
        MonetizationBanner.Attach(BannerHost, monetization));

    protected override void OnDisappearing()
    {
        monetization.StateChanged -= RefreshBanner;
        base.OnDisappearing();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        monetization.StateChanged += RefreshBanner;
        await _viewModel.RefreshAsync();
        MonetizationBanner.Attach(BannerHost, monetization);
    }
}
