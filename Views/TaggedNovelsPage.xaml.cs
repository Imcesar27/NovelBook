using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class TaggedNovelsPage : ContentPage
{
    private readonly TagService _tagService;
    private readonly NovelService _novelService;
    private readonly ImageService _imageService;
    private readonly DatabaseService _databaseService;
    private readonly string _tagName;

    public TaggedNovelsPage(string tagName)
    {
        InitializeComponent();

        _tagName = tagName;
        _databaseService = new DatabaseService();
        _tagService = new TagService(_databaseService);
        _novelService = new NovelService(_databaseService);
        _imageService = new ImageService(_databaseService);

        TagNameLabel.Text = tagName;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadNovelsAsync();
    }

    private async Task LoadNovelsAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            NovelsScrollView.IsVisible = false;
            EmptyLabel.IsVisible = false;

            // Obtener IDs de novelas con esta etiqueta
            var novelIds = await _tagService.GetNovelIdsByTagAsync(_tagName);

            if (novelIds.Count == 0)
            {
                EmptyLabel.IsVisible = true;
                NovelCountLabel.Text = "0 novelas";
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                return;
            }

            // Obtener detalles de las novelas
            var novels = new List<Novel>();
            foreach (var id in novelIds)
            {
                var novel = await _novelService.GetNovelByIdAsync(id);
                if (novel != null)
                {
                    novels.Add(novel);
                }
            }

            NovelCountLabel.Text = $"{novels.Count} novela(s)";

            // Mostrar novelas
            NovelsContainer.Children.Clear();
            foreach (var novel in novels)
            {
                var card = await CreateNovelCardAsync(novel);
                NovelsContainer.Children.Add(card);
            }

            NovelsScrollView.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando novelas: {ex.Message}");
            await DisplayAlert("Error", "No se pudieron cargar las novelas", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    /// <summary>
    /// Crea una tarjeta de novela con la imagen cargada correctamente
    /// </summary>
    private async Task<Frame> CreateNovelCardAsync(Novel novel)
    {
        var frame = new Frame
        {
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 0,
            HasShadow = false,
            Margin = new Thickness(5),
            WidthRequest = 160,
            HeightRequest = 260
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = 180 },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // Imagen de portada usando ImageService
        var coverSource = await _imageService.GetCoverImageAsync(novel.CoverImage);
        var coverImage = new Image
        {
            Source = coverSource,
            Aspect = Aspect.AspectFill,
            HeightRequest = 180
        };

        var imageFrame = new Frame
        {
            Padding = 0,
            CornerRadius = 10,
            IsClippedToBounds = true,
            HasShadow = false,
            Content = coverImage
        };

        grid.Children.Add(imageFrame);
        Grid.SetRow(imageFrame, 0);

        // Información de la novela
        var infoStack = new VerticalStackLayout
        {
            Padding = 8,
            Spacing = 2
        };

        infoStack.Children.Add(new Label
        {
            Text = novel.Title,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 2,
            TextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#1A1A1A")
                : Color.FromArgb("#FFFFFF")
        });

        infoStack.Children.Add(new Label
        {
            Text = novel.Author,
            FontSize = 10,
            TextColor = Color.FromArgb("#888888"),
            LineBreakMode = LineBreakMode.TailTruncation
        });

        var statsStack = new HorizontalStackLayout { Spacing = 8 };
        statsStack.Children.Add(new Label
        {
            Text = $"⭐ {novel.Rating:F1}",
            FontSize = 10,
            TextColor = Color.FromArgb("#FFD700")
        });
        statsStack.Children.Add(new Label
        {
            Text = $"📖 {novel.ChapterCount}",
            FontSize = 10,
            TextColor = Color.FromArgb("#4CAF50")
        });
        infoStack.Children.Add(statsStack);

        grid.Children.Add(infoStack);
        Grid.SetRow(infoStack, 1);

        frame.Content = grid;

        // Agregar tap para navegar
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await Navigation.PushAsync(new NovelDetailPage(novel.Id));
        };
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }
}