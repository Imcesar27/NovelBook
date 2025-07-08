using NovelBook.Services;

namespace NovelBook.Views;

/// <summary>
/// Página para mostrar la cola de descargas pendientes
/// Por ahora es una implementación básica que puede expandirse en el futuro
/// </summary>
public partial class DownloadQueuePage : ContentPage
{
    public DownloadQueuePage()
    {
        InitializeComponent();

        // Por ahora mostrar estado vacío
        ShowEmptyState();
    }

    /// <summary>
    /// Muestra el estado vacío cuando no hay descargas
    /// </summary>
    private void ShowEmptyState()
    {
        EmptyState.IsVisible = true;
        QueueList.IsVisible = false;
        QueueStatusLabel.Text = "No hay descargas en progreso";
        OverallProgress.IsVisible = false;
    }

    /// <summary>
    /// Método placeholder para cuando se implemente la cola de descargas
    /// </summary>
    private void LoadDownloadQueue()
    {
        // TODO: Implementar sistema de cola de descargas en el futuro
        // Por ahora solo mostrar estado vacío

        // Ejemplo de cómo se vería con datos:
        /*
        var queueItems = new List<object>
        {
            new
            {
                NovelTitle = "Overlord",
                ChapterInfo = "Capítulo 1-5",
                Status = "Descargando...",
                StatusColor = Color.FromArgb("#F59E0B"),
                Progress = 0.45
            }
        };
        
        if (queueItems.Any())
        {
            EmptyState.IsVisible = false;
            QueueList.IsVisible = true;
            QueueList.ItemsSource = queueItems;
            
            QueueStatusLabel.Text = $"{queueItems.Count} descargas en progreso";
            OverallProgress.IsVisible = true;
            OverallProgress.Progress = 0.45;
        }
        */
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadDownloadQueue();
    }
}