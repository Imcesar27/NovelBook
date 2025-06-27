using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class NovelDetailPage : ContentPage
{
    // Servicios necesarios para interactuar con la base de datos
    private readonly NovelService _novelService;
    private readonly LibraryService _libraryService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Variables para almacenar datos de la novela actual
    private int _novelId;
    private Novel _novel;

    /// <summary>
    /// Constructor principal que recibe el ID de la novela a mostrar
    /// </summary>
    /// <param name="novelId">ID de la novela en la base de datos</param>
    public NovelDetailPage(int novelId)
    {
        InitializeComponent();

        _novelId = novelId;

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);
        _libraryService = new LibraryService(_databaseService, new AuthService(_databaseService));
        _imageService = new ImageService(_databaseService);

        // Cargar datos de forma asíncrona
        Task.Run(async () => await LoadNovelDetailsAsync());
    }

    /// <summary>
    /// ARREGLO 6: Se ejecuta cada vez que aparece la página para actualizar estado
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Recargar y verificar estado de biblioteca cada vez que aparece la página
        await CheckAndUpdateLibraryStatus();
    }

    /// <summary>
    /// Carga todos los detalles de la novela desde la base de datos
    /// ACTUALIZADO: Con verificación mejorada de biblioteca
    /// </summary>
    private async Task LoadNovelDetailsAsync()
    {
        try
        {
            // Obtener novela de la base de datos
            _novel = await _novelService.GetNovelByIdAsync(_novelId);

            if (_novel != null)
            {
                var coverImage = await _imageService.GetCoverImageAsync(_novel.CoverImage);

                // Actualizar UI en el hilo principal
                Device.BeginInvokeOnMainThread(() =>
                {
                    // Establecer datos para mostrar en la interfaz
                    BindingContext = new
                    {
                        Title = _novel.Title,
                        Author = _novel.Author,
                        CoverImageSource = coverImage,
                        Rating = _novel.Rating.ToString("F1"),
                        ChapterCount = _novel.ChapterCount.ToString(),
                        Status = GetStatusDisplay(_novel.Status),
                        StatusColor = GetStatusColor(_novel.Status),
                        Synopsis = _novel.Synopsis,
                        LastUpdated = _novel.UpdatedAt.ToString("dd/MM/yyyy"),
                        Views = "0" // TODO: Implementar sistema de vistas
                    };

                    LoadGenres();
                });

                // ARREGLO 5: Verificar estado de biblioteca y actualizar botón
                await CheckAndUpdateLibraryStatus();

                // Cargar lista de capítulos
                await LoadChaptersAsync();
            }
        }
        catch (Exception ex)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", "Error al cargar detalles: " + ex.Message, "OK");
            });
        }
    }

    /// <summary>
    /// ARREGLO 5: Verifica estado de biblioteca y actualiza botón - MEJORADO
    /// </summary>
    private async Task CheckAndUpdateLibraryStatus()
    {
        if (AuthService.CurrentUser != null)
        {
            try
            {
                var isInLibrary = await _libraryService.IsInLibraryAsync(_novelId);

                Device.BeginInvokeOnMainThread(() =>
                {
                    // Buscar todos los botones que podrían ser el de agregar
                    var addButton = FindAddToLibraryButton();

                    if (addButton != null)
                    {
                        UpdateButtonAppearance(addButton, isInLibrary);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando biblioteca: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ARREGLO 5: Busca el botón de agregar a biblioteca de forma más robusta
    /// </summary>
    private Button FindAddToLibraryButton()
    {
        // Método 1: Buscar por nombre
        try
        {
            var button = this.FindByName<Button>("AddToLibraryButton");
            if (button != null) return button;
        }
        catch { }

        // Método 2: Buscar en la grilla de botones de acción
        try
        {
            var actionGrid = this.Content.FindByName<Grid>("ActionButtonsGrid");
            if (actionGrid != null)
            {
                foreach (var child in actionGrid.Children)
                {
                    if (child is Button btn &&
                        (btn.Text.Contains("Agregar") || btn.Text.Contains("biblioteca")))
                    {
                        return btn;
                    }
                }
            }
        }
        catch { }

        // Método 3: Búsqueda recursiva por texto
        return FindButtonRecursively(this.Content);
    }

    /// <summary>
    /// ARREGLO 5: Búsqueda recursiva mejorada
    /// </summary>
    private Button FindButtonRecursively(IView element)
    {
        if (element is Button button)
        {
            if (button.Text != null &&
                (button.Text.Contains("Agregar") ||
                 button.Text.Contains("biblioteca") ||
                 button.Text.Contains("✓ En")))
            {
                return button;
            }
        }

        if (element is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                var result = FindButtonRecursively(child);
                if (result != null) return result;
            }
        }

        if (element is ContentView contentView && contentView.Content != null)
        {
            return FindButtonRecursively(contentView.Content);
        }

        if (element is ScrollView scrollView && scrollView.Content != null)
        {
            return FindButtonRecursively(scrollView.Content);
        }

        return null;
    }

    /// <summary>
    /// ARREGLO 5: Actualiza la apariencia del botón
    /// </summary>
    private void UpdateButtonAppearance(Button button, bool isInLibrary)
    {
        if (isInLibrary)
        {
            button.Text = "✓ En biblioteca";
            button.BackgroundColor = Color.FromArgb("#10B981");
            button.TextColor = Colors.White;
        }
        else
        {
            button.Text = "+ Agregar";
            button.BackgroundColor = Color.FromArgb("#2D2D2D");
            button.TextColor = Color.FromArgb("#FFFFFF");
        }
    }

    /// <summary>
    /// Convierte el estado interno a texto para mostrar
    /// </summary>
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

    /// <summary>
    /// Obtiene el color según el estado de la novela
    /// </summary>
    private string GetStatusColor(string status)
    {
        return status switch
        {
            "ongoing" => "#F59E0B",      // Naranja para en curso
            "completed" => "#10B981",    // Verde para completado
            "hiatus" => "#6B7280",       // Gris para en pausa
            _ => "#6B7280"
        };
    }

    /// <summary>
    /// Carga y muestra los géneros de la novela
    /// </summary>
    private void LoadGenres()
    {
        // Limpiar géneros anteriores
        GenresLayout.Children.Clear();

        // Cargar géneros reales de la novela
        if (_novel != null && _novel.Genres != null && _novel.Genres.Count > 0)
        {
            foreach (var genre in _novel.Genres)
            {
                var frame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#3D3D3D"),
                    CornerRadius = 15,
                    Padding = new Thickness(12, 6),
                    Margin = new Thickness(0, 0, 8, 8),
                    HasShadow = false
                };

                frame.Content = new Label
                {
                    Text = genre.Name,
                    TextColor = Colors.White,
                    FontSize = 12
                };

                GenresLayout.Children.Add(frame);
            }
        }
        else
        {
            // Si no hay géneros, mostrar mensaje
            var label = new Label
            {
                Text = "Sin géneros asignados",
                TextColor = Color.FromArgb("#808080"),
                FontSize = 12,
                FontAttributes = FontAttributes.Italic
            };
            GenresLayout.Children.Add(label);
        }
    }

    /// <summary>
    /// Carga la lista de capítulos de la novela
    /// </summary>
    private async Task LoadChaptersAsync()
    {
        try
        {
            // Obtener capítulos desde la base de datos
            var chapters = await _novelService.GetChaptersAsync(_novelId);

            // Transformar datos para mostrar en UI
            var chapterDisplay = chapters.Select(ch => new
            {
                Id = ch.Id,
                Title = $"Capítulo {ch.ChapterNumber}: {ch.Title}",
                Date = ch.CreatedAt.ToString("dd/MM/yyyy"),
                TitleColor = "#FFFFFF" // TODO: Implementar sistema de capítulos leídos
            }).ToList();

            // Actualizar UI en hilo principal
            Device.BeginInvokeOnMainThread(() =>
            {
                ChaptersList.ItemsSource = chapterDisplay;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando capítulos: {ex.Message}");
        }
    }

    /// <summary>
    /// Maneja el click en el botón de leer
    /// </summary>
    private async void OnReadClicked(object sender, EventArgs e)
    {
        if (_novel != null && _novel.ChapterCount > 0)
        {
            // Pasar información real al lector
            await Navigation.PushAsync(new ReaderPage(_novelId, 0, _novel.Title));
        }
        else
        {
            await DisplayAlert("Info", "Esta novela no tiene capítulos aún", "OK");
        }
    }

    /// <summary>
    /// ARREGLO 5: Maneja agregar/quitar de biblioteca - MEJORADO
    /// </summary>
    private async void OnAddToLibraryClicked(object sender, EventArgs e)
    {
        if (AuthService.CurrentUser == null)
        {
            await DisplayAlert("Info", "Debes iniciar sesión para agregar a tu biblioteca", "OK");
            return;
        }

        if (sender is Button button)
        {
            try
            {
                // Deshabilitar botón durante la operación
                button.IsEnabled = false;
                var originalText = button.Text;
                button.Text = "Procesando...";

                // Verificar estado actual en la base de datos
                bool isCurrentlyInLibrary = await _libraryService.IsInLibraryAsync(_novelId);
                bool success = false;

                if (!isCurrentlyInLibrary)
                {
                    // Agregar a biblioteca
                    success = await _libraryService.AddToLibraryAsync(_novelId);
                    if (success)
                    {
                        UpdateButtonAppearance(button, true);
                        await DisplayAlert("Éxito", "Novela agregada a tu biblioteca", "OK");
                    }
                    else
                    {
                        button.Text = originalText;
                        await DisplayAlert("Error", "No se pudo agregar a la biblioteca", "OK");
                    }
                }
                else
                {
                    // Confirmar eliminación
                    bool confirm = await DisplayAlert(
                        "Confirmar",
                        "¿Quitar esta novela de tu biblioteca?",
                        "Sí",
                        "No");

                    if (confirm)
                    {
                        success = await _libraryService.RemoveFromLibraryAsync(_novelId);
                        if (success)
                        {
                            UpdateButtonAppearance(button, false);
                            await DisplayAlert("Info", "Novela eliminada de tu biblioteca", "OK");
                        }
                        else
                        {
                            button.Text = originalText;
                            await DisplayAlert("Error", "No se pudo eliminar de la biblioteca", "OK");
                        }
                    }
                    else
                    {
                        // Usuario canceló, restaurar estado
                        button.Text = originalText;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en biblioteca: {ex.Message}");
                await DisplayAlert("Error", "Error al procesar la solicitud", "OK");

                // Restaurar estado del botón
                await CheckAndUpdateLibraryStatus();
            }
            finally
            {
                // Rehabilitar botón
                button.IsEnabled = true;
            }
        }
    }

    /// <summary>
    /// Métodos para cambiar entre tabs (Sinopsis, Capítulos, Detalles)
    /// </summary>
    private void OnSynopsisTabClicked(object sender, EventArgs e)
    {
        ShowTab("Synopsis");
    }

    private void OnChaptersTabClicked(object sender, EventArgs e)
    {
        ShowTab("Chapters");
    }

    private void OnDetailsTabClicked(object sender, EventArgs e)
    {
        ShowTab("Details");
    }

    /// <summary>
    /// Muestra el tab seleccionado y oculta los demás
    /// </summary>
    private void ShowTab(string tabName)
    {
        // Ocultar todos los contenidos
        SynopsisContent.IsVisible = false;
        ChaptersContent.IsVisible = false;
        DetailsContent.IsVisible = false;

        // Resetear colores de tabs
        SynopsisTab.TextColor = Color.FromArgb("#B0B0B0");
        ChaptersTab.TextColor = Color.FromArgb("#B0B0B0");
        DetailsTab.TextColor = Color.FromArgb("#B0B0B0");

        // Mostrar tab seleccionado
        switch (tabName)
        {
            case "Synopsis":
                SynopsisContent.IsVisible = true;
                SynopsisTab.TextColor = Color.FromArgb("#8B5CF6");
                break;
            case "Chapters":
                ChaptersContent.IsVisible = true;
                ChaptersTab.TextColor = Color.FromArgb("#8B5CF6");
                break;
            case "Details":
                DetailsContent.IsVisible = true;
                DetailsTab.TextColor = Color.FromArgb("#8B5CF6");
                break;
        }
    }

    /// <summary>
    /// Maneja el tap en un capítulo para abrirlo en el lector
    /// </summary>
    private async void OnChapterTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame)
        {
            // Usar un tipo anónimo en lugar de dynamic
            var chapter = frame.BindingContext;
            var chapterType = chapter.GetType();
            var idProperty = chapterType.GetProperty("Id");

            if (idProperty != null)
            {
                var chapterId = (int)idProperty.GetValue(chapter);
                await Navigation.PushAsync(new ReaderPage(_novelId, chapterId, _novel?.Title ?? ""));
            }
        }
    }

    // Constructor temporal para compatibilidad
    public NovelDetailPage() : this(1)
    {
    }
}