using Palabravo.ViewModels;

namespace Palabravo.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    private async void OnDeleteAccountClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.IsAccountIdle)
            return;

        var continueDeletion = await DisplayAlertAsync(
            "Eliminar cuenta y datos",
            "Se eliminarán tu cuenta de Palabravo, puntajes, vinculaciones y progreso local.",
            "Continuar",
            "Cancelar");
        if (!continueDeletion)
            return;

        var confirmed = await DisplayAlertAsync(
            "¿Confirmas la eliminación?",
            "Esta acción no se puede deshacer. Tu cuenta general de Google Play Games no será eliminada.",
            "Sí, eliminar",
            "Volver");
        if (!confirmed)
            return;

        var result = await _viewModel.DeleteAccountAsync();
        var message = result.IsSuccess && !string.IsNullOrWhiteSpace(result.RequestId)
            ? $"{result.Message}\n\nReferencia: {result.RequestId}"
            : result.Message;
        await DisplayAlertAsync(
            result.IsSuccess ? "Solicitud recibida" : "No se pudo eliminar",
            message,
            "Cerrar");

        if (result.IsSuccess)
            await Shell.Current.GoToAsync("//home");
    }
}
