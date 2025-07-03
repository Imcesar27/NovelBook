using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class CategoriesPage : ContentPage
{
    // Servicios necesarios
    private readonly CategoryService _categoryService;
    private readonly DatabaseService _databaseService;

    // Lista de categorías del usuario
    private List<UserCategory> _categories;

    /// <summary>
    /// Constructor de la página
    /// </summary>
    public CategoriesPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _categoryService = new CategoryService(_databaseService);
    }

    /// <summary>
    /// Se ejecuta cuando la página aparece
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Verificar si hay usuario logueado
        if (AuthService.CurrentUser == null)
        {
            await DisplayAlert("Aviso", "Debes iniciar sesión para ver tus categorías", "OK");
            await Navigation.PopAsync();
            return;
        }

        // Cargar las categorías
        await LoadCategoriesAsync();
    }

    /// <summary>
    /// Carga las categorías del usuario
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        try
        {
            // Mostrar indicador de carga
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            // Obtener categorías del servicio
            _categories = await _categoryService.GetUserCategoriesAsync();

            // Actualizar estadísticas
            UpdateStatistics();

            // Mostrar categorías o estado vacío
            if (_categories.Count == 0)
            {
                EmptyStateFrame.IsVisible = true;
                CategoriesContainer.IsVisible = false;
                AddCategoryButton.IsVisible = false;
            }
            else
            {
                EmptyStateFrame.IsVisible = false;
                CategoriesContainer.IsVisible = true;
                AddCategoryButton.IsVisible = true;
                DisplayCategories();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al cargar categorías: " + ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Actualiza las estadísticas en el header
    /// </summary>
    private void UpdateStatistics()
    {
        if (_categories != null)
        {
            var totalNovels = _categories.Sum(c => c.NovelCount);
            StatsLabel.Text = $"{_categories.Count} categoría{(_categories.Count != 1 ? "s" : "")} • " +
                              $"{totalNovels} novela{(totalNovels != 1 ? "s" : "")} organizada{(totalNovels != 1 ? "s" : "")}";
        }
    }

    /// <summary>
    /// Muestra las categorías en la interfaz
    /// </summary>
    private void DisplayCategories()
    {
        // Limpiar contenedor
        CategoriesContainer.Children.Clear();

        foreach (var category in _categories)
        {
            // Crear Frame para cada categoría
            var categoryFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#2D2D2D"),
                CornerRadius = 15,
                HasShadow = false,
                Padding = 0,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Grid principal
            var mainGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Padding = 15
            };

            // Ícono con color de fondo
            var iconFrame = new Frame
            {
                BackgroundColor = category.GetMauiColor().WithAlpha(0.2f),
                CornerRadius = 25,
                WidthRequest = 50,
                HeightRequest = 50,
                Padding = 0,
                HasShadow = false
            };

            var iconLabel = new Label
            {
                Text = category.Icon,
                FontSize = 24,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            iconFrame.Content = iconLabel;
            Grid.SetColumn(iconFrame, 0);
            mainGrid.Children.Add(iconFrame);

            // Información de la categoría
            var infoStack = new StackLayout
            {
                Spacing = 3,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(15, 0, 0, 0)
            };

            var nameLabel = new Label
            {
                Text = category.Name,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            var descriptionLabel = new Label
            {
                Text = string.IsNullOrEmpty(category.Description) ?
                       $"{category.NovelCount} novela{(category.NovelCount != 1 ? "s" : "")}" :
                       category.Description,
                FontSize = 14,
                TextColor = Color.FromArgb("#B0B0B0")
            };

            infoStack.Children.Add(nameLabel);
            infoStack.Children.Add(descriptionLabel);

            Grid.SetColumn(infoStack, 1);
            mainGrid.Children.Add(infoStack);

            // Botón de opciones
            var optionsButton = new Button
            {
                Text = "⋮",
                FontSize = 20,
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.White,
                WidthRequest = 40,
                HeightRequest = 40,
                VerticalOptions = LayoutOptions.Center
            };

            optionsButton.Clicked += async (s, e) => await ShowCategoryOptionsAsync(category);

            Grid.SetColumn(optionsButton, 2);
            mainGrid.Children.Add(optionsButton);

            categoryFrame.Content = mainGrid;

            // Agregar tap para ver novelas de la categoría
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await NavigateToCategoryNovelsAsync(category);
            categoryFrame.GestureRecognizers.Add(tapGesture);

            CategoriesContainer.Children.Add(categoryFrame);
        }
    }

    /// <summary>
    /// Muestra el diálogo para crear nueva categoría
    /// </summary>
    private async void OnAddCategoryClicked(object sender, EventArgs e)
    {
        // Crear página de diálogo para nueva categoría
        await Navigation.PushModalAsync(new CreateCategoryPage(async (category) =>
        {
            // Callback cuando se crea la categoría
            await LoadCategoriesAsync();
        }));
    }

    /// <summary>
    /// Muestra las opciones para una categoría (editar, eliminar)
    /// </summary>
    private async Task ShowCategoryOptionsAsync(UserCategory category)
    {
        var action = await DisplayActionSheet(
            category.Name,
            "Cancelar",
            "Eliminar",
            "Editar",
            "Ver novelas"
        );

        switch (action)
        {
            case "Editar":
                await Navigation.PushModalAsync(new CreateCategoryPage(async (updatedCategory) =>
                {
                    await LoadCategoriesAsync();
                }, category));
                break;

            case "Eliminar":
                var confirm = await DisplayAlert(
                    "Confirmar",
                    $"¿Eliminar la categoría '{category.Name}'?\n\nLas novelas no se eliminarán de tu biblioteca.",
                    "Eliminar",
                    "Cancelar"
                );

                if (confirm)
                {
                    var result = await _categoryService.DeleteCategoryAsync(category.Id);
                    if (result.success)
                    {
                        await DisplayAlert("Éxito", result.message, "OK");
                        await LoadCategoriesAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", result.message, "OK");
                    }
                }
                break;

            case "Ver novelas":
                await NavigateToCategoryNovelsAsync(category);
                break;
        }
    }

    /// <summary>
    /// Navega a la página de novelas de una categoría
    /// </summary>
    private async Task NavigateToCategoryNovelsAsync(UserCategory category)
    {
        await Navigation.PushAsync(new CategoryNovelsPage(category));
    }
}