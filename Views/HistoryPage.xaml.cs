namespace NovelBook.Views;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
        LoadHistory("Today");
    }

    private void LoadHistory(string filter)
    {
        // Datos de ejemplo
        var historyItems = new List<HistoryItem>
        {
            new HistoryItem
            {
                NovelTitle = "Hidden Marriage",
                ChapterTitle = "Capítulo 125: La revelación",
                CoverImage = "novel1.jpg",
                ReadingProgress = "100%",
                ReadingTime = "25 min",
                TimeAgo = "Hace 2h"
            },
            new HistoryItem
            {
                NovelTitle = "Trial Marriage Husband",
                ChapterTitle = "Capítulo 89: Nuevo comienzo",
                CoverImage = "novel2.jpg",
                ReadingProgress = "75%",
                ReadingTime = "15 min",
                TimeAgo = "Hace 4h"
            },
            new HistoryItem
            {
                NovelTitle = "Perfect Secret Love",
                ChapterTitle = "Capítulo 45: El secreto",
                CoverImage = "novel4.jpg",
                ReadingProgress = "100%",
                ReadingTime = "30 min",
                TimeAgo = "Hace 6h"
            }
        };

        HistoryList.ItemsSource = historyItems;
        EmptyState.IsVisible = historyItems.Count == 0;
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            // Actualizar apariencia de botones
            var parent = btn.Parent as HorizontalStackLayout;
            foreach (var child in parent.Children)
            {
                if (child is Button b)
                {
                    b.BackgroundColor = b == btn ?
                        Color.FromArgb("#8B5CF6") :
                        Color.FromArgb("#2D2D2D");
                    b.TextColor = b == btn ?
                        Colors.White :
                        Color.FromArgb("#B0B0B0");
                }
            }

            // Cargar historial según filtro
            LoadHistory(btn.CommandParameter as string);
        }
    }

    private async void OnHistoryItemTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is HistoryItem item)
        {
            // Necesitamos obtener el ID del capítulo, por ahora usar el primer capítulo
            // TODO: Guardar el chapter_id en HistoryItem
            await Navigation.PushAsync(new ReaderPage(1, 1, item.NovelTitle)); // IDs temporales
        }
    }

    private async void OnExploreClicked(object sender, EventArgs e)
    {
        // Cambiar a la pestaña de explorar
        await Shell.Current.GoToAsync("//ExplorePage");
    }

    public class HistoryItem
    {
        public string NovelTitle { get; set; }
        public string ChapterTitle { get; set; }
        public string CoverImage { get; set; }
        public string ReadingProgress { get; set; }
        public string ReadingTime { get; set; }
        public string TimeAgo { get; set; }
    }
}