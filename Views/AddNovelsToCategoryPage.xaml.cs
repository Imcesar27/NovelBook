using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class AddNovelsToCategoryPage : ContentPage
{
    // Servicios
    private readonly LibraryService _libraryService;
    private readonly CategoryService _categoryService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Datos
    private readonly UserCategory _category;
    private readonly Action _onNovelsAdded;
    private List<UserLibraryItem> _allLibraryItems;
    private List<UserLibraryItem> _filteredItems;
    private Dictionary<int, CheckBox> _checkboxes = new Dictionary<int, CheckBox>();
    private HashSet<int> _existingNovelIds;
    private HashSet<int> _selectedNovelIds = new HashSet<int>();

    /// <summary>
    /// Constructor
    /// </summary>
    public AddNovelsToCategoryPage(UserCategory category, Action onNovelsAdded)
    {
        InitializeComponent();

        _category = category;
        _onNovelsAdded = onNovelsAdded;
        Title = $"Agregar a {category.Name}";

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _categoryService = new CategoryService(_databaseService);
        _libraryService = new LibraryService(_databaseService, new AuthService(_databaseService));
        _imageService = new ImageService(_databaseService);
    }

    /// <summary>
    /// Se ejecuta cuando aparece la página
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLibraryNovelsAsync();
    }

    /// <summary>
    /// Carga las novelas de la biblioteca del usuario
    /// </summary>
    private async Task LoadLibraryNovelsAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            // Obtener novelas que ya están en la categoría
            var categoryNovels = await _categoryService.GetCategoryNovelsAsync(_category.Id);
            _existingNovelIds = new HashSet<int>(categoryNovels.Select(n => n.Id));

            // Obtener todas las novelas de la biblioteca
            _allLibraryItems = await _libraryService.GetUserLibraryAsync();

            if (_allLibraryItems.Count == 0)
            {
                EmptyStateFrame.IsVisible = true;
                NovelsContainer.IsVisible = false;
            }
            else
            {
                EmptyStateFrame.IsVisible = false;
                NovelsContainer.IsVisible = true;
                _filteredItems = _allLibraryItems;
                await DisplayNovelsAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al cargar biblioteca: " + ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Muestra las novelas en la interfaz
    /// </summary>
    private async Task DisplayNovelsAsync()
    {
        NovelsContainer.Children.Clear();
        _checkboxes.Clear();

        foreach (var item in _filteredItems)
        {
            var novelFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#2D2D2D"),
                CornerRadius = 10,
                Padding = 10,
                HasShadow = false,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var mainGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = 60 },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            // Checkbox
            var checkBox = new CheckBox
            {
                Color = Color.FromArgb("#8B5CF6"),
                IsChecked = _selectedNovelIds.Contains(item.Novel.Id),
                IsEnabled = !_existingNovelIds.Contains(item.Novel.Id)
            };

            checkBox.CheckedChanged += (s, e) => OnCheckboxChanged(item.Novel.Id, e.Value);
            _checkboxes[item.Novel.Id] = checkBox;

            Grid.SetColumn(checkBox, 0);
            mainGrid.Children.Add(checkBox);

            // Imagen de portada
            var coverImage = await _imageService.GetCoverImageAsync(item.Novel.CoverImage);
            var imageFrame = new Frame
            {
                CornerRadius = 5,
                Padding = 0,
                HasShadow = false,
                WidthRequest = 50,
                HeightRequest = 70,
                Margin = new Thickness(10, 0, 10, 0)
            };

            var image = new Image
            {
                Source = coverImage,
                Aspect = Aspect.AspectFill
            };

            imageFrame.Content = image;
            Grid.SetColumn(imageFrame, 1);
            mainGrid.Children.Add(imageFrame);

            // Información de la novela
            var infoStack = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Spacing = 3
            };

            var titleLabel = new Label
            {
                Text = item.Novel.Title,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            var authorLabel = new Label
            {
                Text = item.Novel.Author,
                FontSize = 14,
                TextColor = Color.FromArgb("#B0B0B0")
            };

            infoStack.Children.Add(titleLabel);
            infoStack.Children.Add(authorLabel);

            Grid.SetColumn(infoStack, 2);
            mainGrid.Children.Add(infoStack);

            // Etiqueta si ya está en la categoría
            if (_existingNovelIds.Contains(item.Novel.Id))
            {
                var addedLabel = new Label
                {
                    Text = "Ya agregada",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#4CAF50"),
                    VerticalOptions = LayoutOptions.Center
                };

                Grid.SetColumn(addedLabel, 3);
                mainGrid.Children.Add(addedLabel);

                // Hacer el frame semi-transparente
                novelFrame.Opacity = 0.6;
            }

            novelFrame.Content = mainGrid;
            NovelsContainer.Children.Add(novelFrame);
        }
    }

    /// <summary>
    /// Maneja el cambio de estado de un checkbox
    /// </summary>
    private void OnCheckboxChanged(int novelId, bool isChecked)
    {
        if (isChecked)
        {
            _selectedNovelIds.Add(novelId);
        }
        else
        {
            _selectedNovelIds.Remove(novelId);
        }

        UpdateSelectedCount();
    }

    /// <summary>
    /// Actualiza el contador de seleccionadas
    /// </summary>
    private void UpdateSelectedCount()
    {
        var count = _selectedNovelIds.Count;
        SelectedCountLabel.Text = $"{count} novela{(count != 1 ? "s" : "")} seleccionada{(count != 1 ? "s" : "")}";
        AddSelectedButton.IsVisible = count > 0;
    }

    /// <summary>
    /// Maneja la búsqueda
    /// </summary>
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            _filteredItems = _allLibraryItems;
        }
        else
        {
            _filteredItems = _allLibraryItems.Where(item =>
                item.Novel.Title.ToLower().Contains(searchText) ||
                item.Novel.Author.ToLower().Contains(searchText)
            ).ToList();
        }

        await DisplayNovelsAsync();
    }

    /// <summary>
    /// Agrega las novelas seleccionadas a la categoría
    /// </summary>
    private async void OnAddSelectedClicked(object sender, EventArgs e)
    {
        if (_selectedNovelIds.Count == 0) return;

        try
        {
            AddSelectedButton.IsEnabled = false;
            var successCount = 0;
            var errors = new List<string>();

            foreach (var novelId in _selectedNovelIds)
            {
                var result = await _categoryService.AddNovelToCategoryAsync(_category.Id, novelId);
                if (result.success)
                {
                    successCount++;
                }
                else
                {
                    errors.Add(result.message);
                }
            }

            if (successCount > 0)
            {
                await DisplayAlert(
                    "Éxito",
                    $"{successCount} novela{(successCount != 1 ? "s" : "")} agregada{(successCount != 1 ? "s" : "")} a la categoría",
                    "OK"
                );

                _onNovelsAdded?.Invoke();
                await Navigation.PopAsync();
            }
            else if (errors.Count > 0)
            {
                await DisplayAlert("Error", string.Join("\n", errors), "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al agregar novelas: " + ex.Message, "OK");
        }
        finally
        {
            AddSelectedButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Cierra la página sin agregar nada
    /// </summary>
    private async void OnDoneClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}