namespace NovelBook.Views;

public partial class StatsPage : ContentPage
{
    public StatsPage()
    {
        InitializeComponent();
    }

    private async void OnViewAllAchievements(object sender, EventArgs e)
    {
        await DisplayAlert("Logros", "Ver todos los logros desbloqueados", "OK");
    }
}