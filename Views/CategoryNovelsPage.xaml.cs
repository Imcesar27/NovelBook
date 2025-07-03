using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class CategoryNovelsPage : ContentPage
{
    // Servicios
    private readonly CategoryService _categoryService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Datos
    private readonly UserCategory _category;
    private List<Novel> _novels;

    // Clase para mostrar novelas en la UI
    public class CategoryNovelDisplay
    {
        public int NovelId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public ImageSource CoverImageSource { get; set; }
    }

    /// <summary>
    /// Constructor que recibe la categoría a mostrar
    /// </summary>
    public CategoryNovelsPage(UserCategory category)
    {
        InitializeComponent();

        _category = category;
        Title = category.Name;

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _categoryService = new CategoryService(_databaseService);
        _imageService = new ImageService(_databaseService);

        // Configurar UI
        ConfigureUI();
    }

    /// <summary>
    /// Configura la interfaz con los datos de la categoría
    /// </summary>
    private void ConfigureUI()
    {
        CategoryNameLabel.Text = _category.Name;
        CategoryIconLabel.Text = _category.Icon;
        CategoryIconFrame.BackgroundColor = _category.GetMauiColor().WithAlpha(0.2f);

        if (!string.IsNullOrEmpty(_category.Description))
        {
            CategoryDescriptionLabel.Text = _category.Description;
            CategoryDescriptionLabel.IsVisible = true;
        }
    }

    /// <summary>
    /// Se ejecuta cuando aparece la página
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadNovelsAsync();
    }

    /// <summary>
    /// Carga las novelas de la categoría
    /// </summary>
    private async Task LoadNovelsAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            // Obtener novelas de la categoría
            _novels = await _categoryService.GetCategoryNovelsAsync(_category.Id);

            // Actualizar contador
            NovelCountLabel.Text = $"{_novels.Count} novela{(_novels.Count != 1 ? "s" : "")}";

            if (_novels.Count == 0)
            {
                EmptyStateFrame.IsVisible = true;
                NovelsCollection.IsVisible = false;
                AddNovelsFloatingButton.IsVisible = false;
            }
            else
            {
                EmptyStateFrame.IsVisible = false;
                NovelsCollection.IsVisible = true;
                AddNovelsFloatingButton.IsVisible = true;
                await DisplayNovelsAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al cargar novelas: " + ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Muestra las novelas en la colección
    /// </summary>
    private async Task DisplayNovelsAsync()
    {
        var displayItems = new List<CategoryNovelDisplay>();

        foreach (var novel in _novels)
        {
            var coverImage = await _imageService.GetCoverImageAsync(novel.CoverImage);

            displayItems.Add(new CategoryNovelDisplay
            {
                NovelId = novel.Id,
                Title = novel.Title,
                Author = novel.Author,
                CoverImageSource = coverImage
            });
        }

        NovelsCollection.ItemsSource = displayItems;
    }

    /// <summary>
    /// Navega a la página de agregar novelas
    /// </summary>
    private async void OnAddNovelsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddNovelsToCategoryPage(_category, async () =>
        {
            // Recargar cuando se agreguen novelas
            await LoadNovelsAsync();
        }));
    }

    /// <summary>
    /// Muestra opciones de la categoría
    /// </summary>
    private async void OnOptionsClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            "Opciones",
            "Cancelar",
            null,
            "Editar categoría",
            "Ordenar por título",
            "Ordenar por autor",
            "Ordenar por fecha agregada"
        );

        switch (action)
        {
            case "Editar categoría":
                await Navigation.PushModalAsync(new CreateCategoryPage(async (updatedCategory) =>
                {
                    // Actualizar UI con los nuevos datos
                    _category.Name = updatedCategory.Name;
                    _category.Description = updatedCategory.Description;
                    _category.Icon = updatedCategory.Icon;
                    _category.Color = updatedCategory.Color;

                    Title = _category.Name;
                    ConfigureUI();
                }, _category));
                break;

            case "Ordenar por título":
                _novels = _novels.OrderBy(n => n.Title).ToList();
                await DisplayNovelsAsync();
                break;

            case "Ordenar por autor":
                _novels = _novels.OrderBy(n => n.Author).ToList();
                await DisplayNovelsAsync();
                break;

            case "Ordenar por fecha agregada":
                // Ya vienen ordenadas por fecha desde la base de datos
                await LoadNovelsAsync();
                break;
        }
    }

    /// <summary>
    /// Navega al detalle de una novela
    /// </summary>
    private async void OnNovelTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is CategoryNovelDisplay novel)
        {
            await Navigation.PushAsync(new NovelDetailPage(novel.NovelId));
        }
    }

    /// <summary>
    /// Elimina una novela de la categoría
    /// </summary>
    private async void OnRemoveNovelClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int novelId)
        {
            var novel = _novels.FirstOrDefault(n => n.Id == novelId);
            if (novel == null) return;

            var confirm = await DisplayAlert(
                "Confirmar",
                $"¿Eliminar '{novel.Title}' de esta categoría?",
                "Eliminar",
                "Cancelar"
            );

            if (confirm)
            {
                var result = await _categoryService.RemoveNovelFromCategoryAsync(_category.Id, novelId);

                if (result.success)
                {
                    await LoadNovelsAsync();
                }
                else
                {
                    await DisplayAlert("Error", result.message, "OK");
                }
            }
        }
    }
}