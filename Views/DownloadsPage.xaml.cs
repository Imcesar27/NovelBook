namespace NovelBook.Views;

public partial class DownloadsPage : ContentPage
{
    private bool _isSelectionMode = false;
    private List<DownloadItem> _selectedItems = new List<DownloadItem>();

    public DownloadsPage()
    {
        InitializeComponent();
        LoadDownloads();
    }

    private void LoadDownloads()
    {
        var downloads = new List<DownloadItem>
        {
            new DownloadItem
            {
                Title = "Hidden Marriage",
                Author = "Jiong Jiong You Yao",
                CoverImage = "novel1.jpg",
                Size = "25 MB",
                ChapterCount = "150",
                DownloadDate = "Descargado hace 2 días"
            },
            new DownloadItem
            {
                Title = "Trial Marriage Husband",
                Author = "Passion Honey",
                CoverImage = "novel2.jpg",
                Size = "18 MB",
                ChapterCount = "100",
                DownloadDate = "Descargado hace 1 semana"
            },
            new DownloadItem
            {
                Title = "My Youth Began With Him",
                Author = "Baby Piggie",
                CoverImage = "novel5.jpg",
                Size = "45 MB",
                ChapterCount = "300",
                DownloadDate = "Descargado hace 3 días"
            }
        };

        DownloadsList.ItemsSource = downloads;
        EmptyState.IsVisible = downloads.Count == 0;
    }

    private void OnSelectClicked(object sender, EventArgs e)
    {
        _isSelectionMode = true;
        SelectionBar.IsVisible = true;
        // Implementar modo selección
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Ordenar por:", "Cancelar", null,
            "Nombre (A-Z)", "Nombre (Z-A)", "Tamaño (Mayor primero)",
            "Tamaño (Menor primero)", "Fecha de descarga");

        if (action != "Cancelar" && action != null)
        {
            // Implementar ordenamiento
            await DisplayAlert("Ordenar", $"Ordenando por: {action}", "OK");
        }
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Opciones:", "Cancelar", "Eliminar",
            "Ver detalles", "Actualizar capítulos");

        if (action == "Eliminar")
        {
            bool confirm = await DisplayAlert("Eliminar",
                "¿Estás seguro de que deseas eliminar esta descarga?", "Sí", "No");
            if (confirm)
            {
                // Implementar eliminación
            }
        }
        else if (action == "Ver detalles")
        {
            await Navigation.PushAsync(new NovelDetailPage());
        }
    }

    private async void OnDeleteSelectedClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Eliminar",
            $"¿Eliminar {_selectedItems.Count} elementos seleccionados?", "Sí", "No");

        if (confirm)
        {
            // Implementar eliminación múltiple
            OnCancelSelectionClicked(sender, e);
        }
    }

    private void OnCancelSelectionClicked(object sender, EventArgs e)
    {
        _isSelectionMode = false;
        _selectedItems.Clear();
        SelectionBar.IsVisible = false;
        SelectionLabel.Text = "0 seleccionados";
    }

    public class DownloadItem
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string CoverImage { get; set; }
        public string Size { get; set; }
        public string ChapterCount { get; set; }
        public string DownloadDate { get; set; }
    }
}