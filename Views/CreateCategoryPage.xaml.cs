using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class CreateCategoryPage : ContentPage
{
    // Servicios
    private readonly CategoryService _categoryService;
    private readonly DatabaseService _databaseService;

    // Callback cuando se guarda
    private readonly Action<UserCategory> _onSaveCallback;

    // Categoría a editar (null si es nueva)
    private readonly UserCategory _categoryToEdit;

    // Estado actual
    private string _selectedIcon = "📁";
    private string _selectedColor = "#2196F3";

    // Opciones disponibles
    private readonly string[] _availableIcons = new[]
    {
        "📁", "📚", "⭐", "❤️", "🔖", "📖", "🎯", "🌟",
        "💎", "🔥", "🌈", "🎨", "🎭", "🎪", "🎬", "🎮",
        "🏆", "🎓", "💡", "🔮", "🌙", "☀️", "🌸", "🍀"
    };

    private readonly string[] _availableColors = new[]
    {
        "#2196F3", "#4CAF50", "#FF9800", "#E91E63", "#9C27B0",
        "#673AB7", "#3F51B5", "#00BCD4", "#009688", "#8BC34A",
        "#FFC107", "#FF5722", "#795548", "#607D8B", "#F44336"
    };

    /// <summary>
    /// Constructor para crear o editar categoría
    /// </summary>
    public CreateCategoryPage(Action<UserCategory> onSaveCallback, UserCategory categoryToEdit = null)
    {
        InitializeComponent();

        _onSaveCallback = onSaveCallback;
        _categoryToEdit = categoryToEdit;

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _categoryService = new CategoryService(_databaseService);

        // Configurar página según si es edición o creación
        if (_categoryToEdit != null)
        {
            Title = "Editar Categoría";
            TitleLabel.Text = "Editar categoría";
            SaveButton.Text = "Guardar cambios";
            LoadCategoryData();
        }
        else
        {
            SaveButton.Text = "Crear categoría";
        }

        // Configurar eventos
        NameEntry.TextChanged += OnNameChanged;
        DescriptionEditor.TextChanged += OnDescriptionChanged;

        // Cargar opciones de íconos y colores
        LoadIconOptions();
        LoadColorOptions();

        // Actualizar vista previa inicial
        UpdatePreview();
    }

    /// <summary>
    /// Carga los datos de la categoría a editar
    /// </summary>
    private void LoadCategoryData()
    {
        if (_categoryToEdit != null)
        {
            NameEntry.Text = _categoryToEdit.Name;
            DescriptionEditor.Text = _categoryToEdit.Description;
            _selectedIcon = _categoryToEdit.Icon;
            _selectedColor = _categoryToEdit.Color;
        }
    }

    /// <summary>
    /// Carga las opciones de íconos
    /// </summary>
    private void LoadIconOptions()
    {
        foreach (var icon in _availableIcons)
        {
            var iconFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#3D3D3D"),
                CornerRadius = 20,
                WidthRequest = 40,
                HeightRequest = 40,
                Padding = 0,
                HasShadow = false,
                Margin = new Thickness(5)
            };

            var iconLabel = new Label
            {
                Text = icon,
                FontSize = 20,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            iconFrame.Content = iconLabel;

            // Marcar como seleccionado si corresponde
            if (icon == _selectedIcon)
            {
                iconFrame.BorderColor = Color.FromArgb("#2196F3");
                iconFrame.BackgroundColor = Color.FromArgb("#2196F3").WithAlpha(0.2f);
            }

            // Agregar evento de tap
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => SelectIcon(icon, iconFrame);
            iconFrame.GestureRecognizers.Add(tapGesture);

            IconsLayout.Children.Add(iconFrame);
        }
    }

    /// <summary>
    /// Carga las opciones de colores
    /// </summary>
    private void LoadColorOptions()
    {
        foreach (var color in _availableColors)
        {
            var colorFrame = new Frame
            {
                BackgroundColor = Color.FromArgb(color),
                CornerRadius = 20,
                WidthRequest = 40,
                HeightRequest = 40,
                Padding = 0,
                HasShadow = false,
                Margin = new Thickness(5)
            };

            // Marcar como seleccionado si corresponde
            if (color == _selectedColor)
            {
                colorFrame.BorderColor = Colors.White;

                // Agregar check mark
                var checkLabel = new Label
                {
                    Text = "✓",
                    TextColor = Colors.White,
                    FontSize = 16,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
                colorFrame.Content = checkLabel;
            }

            // Agregar evento de tap
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => SelectColor(color, colorFrame);
            colorFrame.GestureRecognizers.Add(tapGesture);

            ColorsLayout.Children.Add(colorFrame);
        }
    }

    /// <summary>
    /// Selecciona un ícono
    /// </summary>
    private void SelectIcon(string icon, Frame selectedFrame)
    {
        // Desmarcar todos los íconos
        foreach (Frame frame in IconsLayout.Children)
        {
            frame.BorderColor = Colors.Transparent;
            frame.BackgroundColor = Color.FromArgb("#3D3D3D");
        }

        // Marcar el seleccionado
        _selectedIcon = icon;
        selectedFrame.BorderColor = Color.FromArgb("#2196F3");
        selectedFrame.BackgroundColor = Color.FromArgb("#2196F3").WithAlpha(0.2f);

        UpdatePreview();
    }

    /// <summary>
    /// Selecciona un color
    /// </summary>
    private void SelectColor(string color, Frame selectedFrame)
    {
        // Desmarcar todos los colores
        foreach (Frame frame in ColorsLayout.Children)
        {
            frame.BorderColor = Colors.Transparent;
            frame.Content = null;
        }

        // Marcar el seleccionado
        _selectedColor = color;
        selectedFrame.BorderColor = Colors.White;

        var checkLabel = new Label
        {
            Text = "✓",
            TextColor = Colors.White,
            FontSize = 16,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        selectedFrame.Content = checkLabel;

        UpdatePreview();
    }

    /// <summary>
    /// Actualiza la vista previa
    /// </summary>
    private void UpdatePreview()
    {
        PreviewIconLabel.Text = _selectedIcon;
        PreviewIconFrame.BackgroundColor = Color.FromArgb(_selectedColor).WithAlpha(0.2f);

        PreviewNameLabel.Text = string.IsNullOrWhiteSpace(NameEntry.Text) ?
                                "Nueva categoría" : NameEntry.Text;

        PreviewDescriptionLabel.Text = string.IsNullOrWhiteSpace(DescriptionEditor.Text) ?
                                       "Sin descripción" : DescriptionEditor.Text;
    }

    /// <summary>
    /// Evento cuando cambia el nombre
    /// </summary>
    private void OnNameChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    /// <summary>
    /// Evento cuando cambia la descripción
    /// </summary>
    private void OnDescriptionChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    /// <summary>
    /// Cancelar y cerrar
    /// </summary>
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Guardar categoría
    /// </summary>
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validar nombre
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlert("Error", "El nombre es obligatorio", "OK");
            return;
        }

        if (NameEntry.Text.Length < 3)
        {
            await DisplayAlert("Error", "El nombre debe tener al menos 3 caracteres", "OK");
            return;
        }

        try
        {
            var category = new UserCategory
            {
                Name = NameEntry.Text.Trim(),
                Description = DescriptionEditor.Text?.Trim() ?? "",
                Icon = _selectedIcon,
                Color = _selectedColor
            };

            if (_categoryToEdit != null)
            {
                // Editar categoría existente
                category.Id = _categoryToEdit.Id;
                var result = await _categoryService.UpdateCategoryAsync(category);

                if (result.success)
                {
                    _onSaveCallback?.Invoke(category);
                    await Navigation.PopModalAsync();
                }
                else
                {
                    await DisplayAlert("Error", result.message, "OK");
                }
            }
            else
            {
                // Crear nueva categoría
                var result = await _categoryService.CreateCategoryAsync(category);

                if (result.success)
                {
                    category.Id = result.categoryId;
                    _onSaveCallback?.Invoke(category);
                    await Navigation.PopModalAsync();
                }
                else
                {
                    await DisplayAlert("Error", result.message, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al guardar: " + ex.Message, "OK");
        }
    }
}