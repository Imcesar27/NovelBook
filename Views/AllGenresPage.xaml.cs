using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class AllGenresPage : ContentPage
{
    // Servicios
    private readonly GenreService _genreService;
    private readonly DatabaseService _databaseService;

    // Datos
    private List<Genre> _allGenres;
    private List<Genre> _filteredGenres;

    public AllGenresPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _genreService = new GenreService(_databaseService);

        // Cargar géneros
        LoadAllGenres();
    }

    /// <summary>
    /// Carga todos los géneros disponibles
    /// </summary>
    private async void LoadAllGenres()
    {
        try
        {
            // Obtener todos los géneros
            _allGenres = await _genreService.GetAllGenresAsync();

            // Ocultar indicador
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;

            if (_allGenres.Count == 0)
            {
                ShowNoGenresMessage();
                return;
            }

            // Mostrar todos inicialmente
            _filteredGenres = _allGenres;
            DisplayGenres();
        }
        catch (Exception ex)
        {
            LoadingIndicator.IsVisible = false;
            await DisplayAlert("Error", "Error al cargar géneros: " + ex.Message, "OK");
        }
    }

    /// <summary>
    /// Muestra los géneros en la UI
    /// </summary>
    private void DisplayGenres()
    {
        GenresContainer.Children.Clear();

        foreach (var genre in _filteredGenres)
        {
            var genreCard = CreateGenreCard(genre);
            GenresContainer.Children.Add(genreCard);
        }
    }

    /// <summary>
    /// Crea una tarjeta de género
    /// </summary>
    private Frame CreateGenreCard(Genre genre)
    {
        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#1E1E1E"),
            CornerRadius = 20,
            Padding = 15,
            HasShadow = false,
            WidthRequest = 150,
            HeightRequest = 100,
            Margin = 5
        };

        var stack = new StackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 10
        };

        // Icono del género
        var popularGenre = new PopularGenre { Name = genre.Name };
        var iconLabel = new Label
        {
            Text = popularGenre.Icon,
            FontSize = 30,
            HorizontalOptions = LayoutOptions.Center
        };
        stack.Children.Add(iconLabel);

        // Nombre del género
        var nameLabel = new Label
        {
            Text = genre.Name,
            TextColor = Colors.White,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        stack.Children.Add(nameLabel);

        frame.Content = stack;

        // Color de fondo temático con transparencia
        frame.BackgroundColor = Color.FromArgb(popularGenre.ThemeColor).WithAlpha(0.2f);

        // Agregar tap para navegar
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await Navigation.PushAsync(new GenreDetailPage(genre.Id, genre.Name));
        };
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    /// <summary>
    /// Muestra mensaje cuando no hay géneros
    /// </summary>
    private void ShowNoGenresMessage()
    {
        var label = new Label
        {
            Text = "No hay géneros disponibles",
            TextColor = Color.FromArgb("#808080"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontSize = 16
        };
        GenresContainer.Children.Add(label);
    }

    /// <summary>
    /// Maneja la búsqueda de géneros
    /// </summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            _filteredGenres = _allGenres;
        }
        else
        {
            _filteredGenres = _allGenres
                .Where(g => g.Name.ToLower().Contains(searchText))
                .ToList();
        }

        DisplayGenres();
    }
}