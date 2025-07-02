using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class PopularGenresPage : ContentPage
{
    // Servicios
    private readonly GenreService _genreService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Datos
    private List<PopularGenre> _popularGenres;

    public PopularGenresPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _genreService = new GenreService(_databaseService);
        _imageService = new ImageService(_databaseService);

        // Cargar datos
        LoadPopularGenres();
    }

    /// <summary>
    /// Carga los géneros populares
    /// </summary>
    private async void LoadPopularGenres()
    {
        try
        {
            // Obtener top 10 géneros más populares
            _popularGenres = await _genreService.GetPopularGenresAsync(10);

            // Ocultar indicador de carga
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;

            // Limpiar contenedor
            GenresContainer.Children.Clear();

            if (_popularGenres.Count == 0)
            {
                ShowNoGenresMessage();
                return;
            }

            // Crear tarjetas para cada género
            foreach (var genre in _popularGenres)
            {
                var genreCard = CreateGenreCard(genre);
                GenresContainer.Children.Add(genreCard);
            }
        }
        catch (Exception ex)
        {
            LoadingIndicator.IsVisible = false;
            await DisplayAlert("Error", "Error al cargar categorías: " + ex.Message, "OK");
        }
    }

    /// <summary>
    /// Crea una tarjeta visual para un género
    /// </summary>
    private Frame CreateGenreCard(PopularGenre genre)
    {
        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#1E1E1E"),
            CornerRadius = 15,
            Padding = 0,
            HasShadow = false,
            HeightRequest = 120
        };

        // Grid principal
        var mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = 15 },    // Barra de color
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        // Barra de color lateral
        var colorBar = new BoxView
        {
            Color = Color.FromArgb(genre.ThemeColor),
            VerticalOptions = LayoutOptions.Fill
        };
        mainGrid.Children.Add(colorBar);
        Grid.SetColumn(colorBar, 0);

        // Contenido principal
        var contentGrid = new Grid
        {
            Padding = 15,
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        // Header con ranking e icono
        var headerStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 10
        };

        // Badge de ranking
        if (genre.Rank <= 3)
        {
            var rankFrame = new Frame
            {
                BackgroundColor = Color.FromArgb(genre.Rank == 1 ? "#FFD700" :
                                               genre.Rank == 2 ? "#C0C0C0" : "#CD7F32"),
                CornerRadius = 15,
                Padding = new Thickness(10, 5),
                HasShadow = false
            };

            var rankLabel = new Label
            {
                Text = $"#{genre.Rank}",
                TextColor = Color.FromArgb("#121212"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 12
            };

            rankFrame.Content = rankLabel;
            headerStack.Children.Add(rankFrame);
        }

        // Nombre del género con icono
        var nameLabel = new Label
        {
            Text = $"{genre.Icon} {genre.Name}",
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            VerticalOptions = LayoutOptions.Center
        };
        headerStack.Children.Add(nameLabel);

        // Indicador de popularidad
        var popularityLabel = new Label
        {
            Text = genre.PopularityLevel,
            TextColor = Color.FromArgb("#FFD700"),
            FontSize = 12,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };
        headerStack.Children.Add(popularityLabel);

        contentGrid.Children.Add(headerStack);
        Grid.SetRow(headerStack, 0);

        // Estadísticas en el medio
        var statsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            VerticalOptions = LayoutOptions.Center
        };

        // Novelas
        var novelsStack = CreateStatStack("📚", genre.NovelCount.ToString(), "Novelas");
        statsGrid.Children.Add(novelsStack);
        Grid.SetColumn(novelsStack, 0);

        // Rating promedio
        var ratingStack = CreateStatStack("⭐", genre.AverageRating.ToString("F1"), "Rating");
        statsGrid.Children.Add(ratingStack);
        Grid.SetColumn(ratingStack, 1);

        // Usuarios activos
        var usersStack = CreateStatStack("👥", FormatNumber(genre.ActiveUsers), "Lectores");
        statsGrid.Children.Add(usersStack);
        Grid.SetColumn(usersStack, 2);

        contentGrid.Children.Add(statsGrid);
        Grid.SetRow(statsGrid, 1);

        // Footer con más estadísticas
        var footerStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 15
        };

        var chaptersLabel = new Label
        {
            Text = $"📖 {FormatNumber(genre.TotalChaptersRead)} capítulos leídos",
            TextColor = Color.FromArgb("#808080"),
            FontSize = 11
        };
        footerStack.Children.Add(chaptersLabel);

        var reviewsLabel = new Label
        {
            Text = $"💬 {FormatNumber(genre.TotalReviews)} reseñas",
            TextColor = Color.FromArgb("#808080"),
            FontSize = 11
        };
        footerStack.Children.Add(reviewsLabel);

        contentGrid.Children.Add(footerStack);
        Grid.SetRow(footerStack, 2);

        mainGrid.Children.Add(contentGrid);
        Grid.SetColumn(contentGrid, 1);

        frame.Content = mainGrid;

        // Agregar tap para ver novelas del género
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await Navigation.PushAsync(new GenreDetailPage(genre.Id, genre.Name));
        };
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    /// <summary>
    /// Crea un stack de estadística
    /// </summary>
    private StackLayout CreateStatStack(string icon, string value, string label)
    {
        var stack = new StackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 2
        };

        var valueLabel = new Label
        {
            Text = $"{icon} {value}",
            TextColor = Colors.White,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        };
        stack.Children.Add(valueLabel);

        var textLabel = new Label
        {
            Text = label,
            TextColor = Color.FromArgb("#808080"),
            FontSize = 10,
            HorizontalTextAlignment = TextAlignment.Center
        };
        stack.Children.Add(textLabel);

        return stack;
    }

    /// <summary>
    /// Formatea números grandes
    /// </summary>
    private string FormatNumber(int number)
    {
        if (number >= 1000000)
            return $"{number / 1000000.0:F1}M";
        else if (number >= 1000)
            return $"{number / 1000.0:F1}K";
        else
            return number.ToString();
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
            VerticalOptions = LayoutOptions.CenterAndExpand,
            FontSize = 16
        };
        GenresContainer.Children.Add(label);
    }

    /// <summary>
    /// Navega a la página de todos los géneros
    /// </summary>
    private async void OnViewAllGenresClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AllGenresPage());
    }
}