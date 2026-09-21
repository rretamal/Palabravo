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
        await _viewModel.LoadAsync();
        // Loading the ranking also gives startup monetization reconciliation time
        // to resolve ownership before an ad request is allowed.
        MonetizationBanner.Attach(BannerHost, _monetization);
    }
}
