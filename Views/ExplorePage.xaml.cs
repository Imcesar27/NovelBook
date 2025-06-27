using NovelBook.Services;
using NovelBook.Models;

namespace NovelBook.Views;

public partial class ExplorePage : ContentPage
{
    private readonly NovelService _novelService;
    private readonly DatabaseService _databaseService;
    private readonly LibraryService _libraryService;
    private List<Novel> _allNovels = new List<Novel>();
    private readonly ImageService _imageService;
    public ExplorePage()
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);
        _libraryService = new LibraryService(_databaseService, new AuthService(_databaseService));
         _imageService = new ImageService(_databaseService);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadNovels();
    }

    private async Task LoadNovels()
    {
        try
        {
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;

            // Cargar novelas de la base de datos
            _allNovels = await _novelService.GetAllNovelsAsync();

            if (_allNovels.Count == 0)
            {
                await DisplayAlert("Info", "No hay novelas disponibles. Si eres administrador, puedes crear algunas desde el menú 'Más'.", "OK");
            }

            // Transformar para mostrar en UI
            var displayNovels = new List<NovelDisplay>();
            foreach (var novel in _allNovels)
            {
                bool isInLibrary = false;
                if (AuthService.CurrentUser != null)
                {
                    isInLibrary = await _libraryService.IsInLibraryAsync(novel.Id);
                }

                // Cargar imagen
                var coverImage = await _imageService.GetCoverImageAsync(novel.CoverImage);

                displayNovels.Add(new NovelDisplay
                {
                    Id = novel.Id,
                    Title = novel.Title,
                    Author = novel.Author,
                    CoverImageSource = coverImage, 
                    Status = GetStatusDisplay(novel.Status),
                    StatusColor = GetStatusColor(novel.Status),
                    ChapterCount = novel.ChapterCount.ToString(),
                    Rating = novel.Rating.ToString("F1"),
                    IsInLibrary = isInLibrary,
                    ShowAuthor = novel.Title.Length < 20 // Mostrar autor solo si el título es corto
                });
            }

            novelsCollection.ItemsSource = displayNovels;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al cargar novelas: " + ex.Message, "OK");
        }
        finally
        {
            loadingIndicator.IsVisible = false;
            loadingIndicator.IsRunning = false;
        }
    }

    private string GetStatusDisplay(string status)
    {
        return status switch
        {
            "ongoing" => "En curso",
            "completed" => "Completado",
            "hiatus" => "En pausa",
            _ => status
        };
    }

    private string GetStatusColor(string status)
    {
        return status switch
        {
            "ongoing" => "#F59E0B",
            "completed" => "#10B981",
            "hiatus" => "#6B7280",
            _ => "#6B7280"
        };
    }

    private async void OnNovelTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is NovelDisplay novel)
        {
            var detailPage = new NovelDetailPage(novel.Id);
            await Navigation.PushAsync(detailPage);
        }
    }

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Filtrar por:", "Cancelar", null,
            "Todos", "En curso", "Completados", "En pausa", "Mejor calificados");

        if (action != "Cancelar" && action != null)
        {
            await FilterNovels(action);
        }
    }

    private async Task FilterNovels(string filter)
    {
        try
        {
            var filteredNovels = filter switch
            {
                "En curso" => _allNovels.Where(n => n.Status == "ongoing").ToList(),
                "Completados" => _allNovels.Where(n => n.Status == "completed").ToList(),
                "En pausa" => _allNovels.Where(n => n.Status == "hiatus").ToList(),
                "Mejor calificados" => _allNovels.OrderByDescending(n => n.Rating).Take(10).ToList(),
                _ => _allNovels
            };

            // Actualizar la colección con las novelas filtradas
            var displayNovels = new List<NovelDisplay>();
            foreach (var novel in filteredNovels)
            {
                bool isInLibrary = false;
                if (AuthService.CurrentUser != null)
                {
                    isInLibrary = await _libraryService.IsInLibraryAsync(novel.Id);
                }

                displayNovels.Add(new NovelDisplay
                {
                    Id = novel.Id,
                    Title = novel.Title,
                    Author = novel.Author,
                    CoverImageSource = string.IsNullOrEmpty(novel.CoverImage) ? "novel_placeholder.jpg" : novel.CoverImage,
                    Status = GetStatusDisplay(novel.Status),
                    StatusColor = GetStatusColor(novel.Status),
                    ChapterCount = novel.ChapterCount.ToString(),
                    Rating = novel.Rating.ToString("F1"),
                    IsInLibrary = isInLibrary
                });
            }

            novelsCollection.ItemsSource = displayNovels;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al filtrar: " + ex.Message, "OK");
        }
    }

    // Clase para mostrar en UI
    public class NovelDisplay
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public ImageSource CoverImageSource { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string ChapterCount { get; set; }
        public string Rating { get; set; }
        public bool IsInLibrary { get; set; }
        public bool ShowAuthor { get; set; }
    }


}