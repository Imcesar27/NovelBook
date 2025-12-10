using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class NovelDetailPage : ContentPage
{
    // Servicios necesarios para interactuar con la base de datos
    private readonly NovelService _novelService;
    private readonly LibraryService _libraryService;
    private readonly TagService _tagService;
    private List<NovelTag> _novelTags = new();
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Variables para almacenar datos de la novela actual
    private int _novelId;
    private Novel _novel;

    // Variables para manejar el estado de los filtros
    private List<dynamic> _allChapters = new List<dynamic>();
    private string _currentChapterFilter = "Todos";

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
        _tagService = new TagService(_databaseService);



        // Cargar datos de forma asíncrona
        Task.Run(async () => await LoadNovelDetailsAsync());
    }


    /// <summary>
    /// Se ejecuta cada vez que aparece la página para actualizar estado
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Recargar estado cada vez que aparece la página
        if (_novel != null)
        {
            await CheckAndUpdateLibraryStatus();
            await UpdateReadButtonStatus();
            // Cargar etiquetas
            await LoadTagsAsync();
        }
    }

    /// <summary>
    /// Carga todos los detalles de la novela desde la base de datos
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
                var reviewCount = await GetReviewCountAsync(_novelId);

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
                        Views = "0", // TODO: Implementar sistema de vistas
                        ReviewCount = reviewCount
                    };

                    LoadGenres();
                });

                // Verificar estado de biblioteca
                await CheckAndUpdateLibraryStatus();

                // Actualizar estado del botón de lectura
                await UpdateReadButtonStatus();

                // Cargar lista de capítulos
                await LoadChaptersAsync();
            }
        }
        catch (Exception ex)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert(
                LocalizationService.GetString("Error"),
                $"{LocalizationService.GetString("ErrorLoadingDetails")}: {ex.Message}", // CAMBIO
                LocalizationService.GetString("OK"));
            });
        }
    }

    // Método auxiliar
    private async Task<int> GetReviewCountAsync(int novelId)
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM reviews WHERE novel_id = @novelId";
            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        catch
        {
            return 0;
        }

    }

    /// <summary>
    /// Actualiza el botón de lectura según el progreso del usuario
    /// </summary>
    /* private async Task UpdateReadButtonStatus()
     {
         try
         {
             if (AuthService.CurrentUser == null || _novel == null)
             {
                 // Usuario no logueado o novela no cargada
                 Device.BeginInvokeOnMainThread(() =>
                 {
                     var readButton = this.FindByName<Button>("ReadButton");
                     if (readButton != null)
                     {
                         readButton.Text = "Leer";
                     }
                 });
                 return;
             }

             // Obtener el progreso del usuario para esta novela
             var libraryService = new LibraryService(_databaseService, new AuthService(_databaseService));
             var userLibrary = await libraryService.GetUserLibraryAsync();
             var novelInLibrary = userLibrary.FirstOrDefault(x => x.NovelId == _novelId);

             Device.BeginInvokeOnMainThread(() =>
             {
                 var readButton = this.FindByName<Button>("ReadButton");
                 if (readButton != null)
                 {
                     if (novelInLibrary != null && novelInLibrary.LastReadChapter > 0)
                     {
                         // El usuario ya ha leído capítulos
                         if (novelInLibrary.LastReadChapter >= _novel.ChapterCount)
                         {
                             // Ha completado todos los capítulos
                             readButton.Text = "Releer";
                         }
                         else
                         {
                             // Tiene capítulos pendientes
                             int nextChapter = novelInLibrary.LastReadChapter + 1;
                             readButton.Text = $"Resumir Cap. {nextChapter}";
                         }
                     }
                     else
                     {
                         // No ha empezado a leer
                         readButton.Text = "Leer";
                     }
                 }
             });
         }
         catch (Exception ex)
         {
             System.Diagnostics.Debug.WriteLine($"Error actualizando botón de lectura: {ex.Message}");
         }
     }*/

    private async Task UpdateReadButtonStatus()
    {
        try
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var readButton = this.FindByName<Button>("ReadButton");
                if (readButton == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: No se encontró el botón ReadButton");
                    return;
                }
                readButton.Text = LocalizationService.GetString("Read2"); // CAMBIO
            });

            if (AuthService.CurrentUser == null || _novel == null) return;

            // Sincronizar progreso primero
            var chapterService = new ChapterService(_databaseService);
            await chapterService.SyncUserLibraryProgressAsync(AuthService.CurrentUser.Id, _novelId);

            // Obtener información actualizada
            var userLibrary = await _libraryService.GetUserLibraryAsync();
            var novelInLibrary = userLibrary.FirstOrDefault(x => x.NovelId == _novelId);

            // Obtener el progreso actual del usuario
            var currentProgress = await GetCurrentReadingProgress();

            Device.BeginInvokeOnMainThread(() =>
            {
                var readButton = this.FindByName<Button>("ReadButton");
                if (readButton != null)
                {
                    // Si hay progreso de lectura pero no está en biblioteca, agregarlo primero
                    if (novelInLibrary == null && currentProgress != null)
                    {
                        readButton.Text = LocalizationService.GetString("ContinueReading"); // CAMBIO
                    }
                    else if (novelInLibrary != null)
                    {
                        // Si completó todos los capítulos
                        if (novelInLibrary.LastReadChapter >= _novel.ChapterCount)
                        {
                            readButton.Text = LocalizationService.GetString("Reread"); // CAMBIO
                        }
                        // Si hay progreso parcial en algún capítulo
                        else if (currentProgress != null && !currentProgress.Value.IsCompleted)
                        {
                            readButton.Text = $"{LocalizationService.GetString("ContinueCap")} {currentProgress.Value.ChapterNumber}"; // CAMBIO
                        }
                        // Si completó algunos capítulos pero no todos
                        else if (novelInLibrary.LastReadChapter > 0)
                        {
                            int nextChapter = novelInLibrary.LastReadChapter + 1;
                            readButton.Text = $"{LocalizationService.GetString("ReadCap")} {nextChapter}"; // CAMBIO
                        }
                        else
                        {
                            readButton.Text = "Leer";
                        }
                    }
                    else
                    {
                        readButton.Text = LocalizationService.GetString("Read2"); // CAMBIO
                    }

                    System.Diagnostics.Debug.WriteLine($"Botón actualizado a: {readButton.Text}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando botón: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene el progreso de lectura actual del usuario
    /// </summary>
    private async Task<(int ChapterNumber, bool IsCompleted)?> GetCurrentReadingProgress()
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            // Buscar el último capítulo con progreso
            var query = @"SELECT TOP 1 c.chapter_number, rp.is_completed, rp.progress
                     FROM reading_progress rp
                     JOIN chapters c ON rp.chapter_id = c.id
                     WHERE rp.user_id = @userId AND c.novel_id = @novelId
                     ORDER BY c.chapter_number DESC";

            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            command.Parameters.AddWithValue("@novelId", _novelId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetInt32(0), reader.GetBoolean(1));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo progreso actual: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Verifica estado de biblioteca y actualiza botón
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
    /// Busca el botón de agregar a biblioteca
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
    /// Búsqueda recursiva
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
    /// Actualiza la apariencia del botón
    /// </summary>
private void UpdateButtonAppearance(Button button, bool isInLibrary)
{
    // Obtener colores según el tema actual
    var inactiveBackgroundColor = Application.Current.RequestedTheme == AppTheme.Light 
        ? (Color)Application.Current.Resources["BackgroundMediumLight"]
        : (Color)Application.Current.Resources["BackgroundMedium"];
    
    var inactiveTextColor = Application.Current.RequestedTheme == AppTheme.Light
        ? (Color)Application.Current.Resources["TextPrimaryLight"] 
        : (Color)Application.Current.Resources["TextPrimary"];

    if (isInLibrary)
    {
        button.Text = $"➖ {LocalizationService.GetString("RemoveFromLibrary")}";
        button.BackgroundColor = Color.FromArgb("#10B981"); // Verde - se mantiene igual
        button.TextColor = Colors.White;
    }
    else
    {
        button.Text = $"➕ {LocalizationService.GetString("AddToLibrary")}";
        button.BackgroundColor = inactiveBackgroundColor;
        button.TextColor = inactiveTextColor;
    }
}

    /// <summary>
    /// Convierte el estado interno a texto para mostrar
    /// </summary>
    private string GetStatusDisplay(string status)
    {
        return status switch
        {
            "ongoing" => LocalizationService.GetString("Ongoing"),
            "completed" => LocalizationService.GetString("Completed"),
            "hiatus" => LocalizationService.GetString("Hiatus"),
            "cancelled" => LocalizationService.GetString("Cancelled"),
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
            "cancelled" => "#EF4444",    // Rojo para cancelada
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
            // Obtener color de texto según el tema
            var genreTextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? (Color)Application.Current.Resources["TextPrimaryLight"]
                : (Color)Application.Current.Resources["TextPrimary"];

            foreach (var genre in _novel.Genres)
            {
                var frame = new Frame
                {
                    BackgroundColor = (Color)Application.Current.Resources["Primary"],
                    CornerRadius = 15,
                    Padding = new Thickness(12, 6),
                    HasShadow = false,
                    Margin = new Thickness(0, 0, 5, 5)
                };

                var label = new Label
                {
                    Text = genre.Name,
                    TextColor = Colors.White,
                    FontSize = 12
                };

                frame.Content = label;

                // Agregar tap para ver novelas del género
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    await Navigation.PushAsync(new GenreDetailPage(genre.Id, genre.Name));
                };
                frame.GestureRecognizers.Add(tapGesture);

                GenresLayout.Children.Add(frame);
            }
        }
        else
        {
            // Obtener color de texto según el tema
            var noGenresTextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? (Color)Application.Current.Resources["TextMutedLight"]
                : (Color)Application.Current.Resources["TextMuted"];

            // Replace the problematic line with the following code:
            var noGenresLabel = new Label
            {
                Text = LocalizationService.GetString("WithoutGenres"),
                TextColor = noGenresTextColor,
                FontSize = 14,
                FontAttributes = FontAttributes.Italic
            };
            GenresLayout.Children.Add(noGenresLabel);
        }
    }

    /// <summary>
    /// Carga la lista de capítulos de la novela
    /// </summary>
    private async Task LoadChaptersAsync()
    {
        try
        {
            var chapters = await _novelService.GetChaptersAsync(_novelId);

            // Obtener capítulos leídos si hay usuario logueado
            var readChapters = new HashSet<int>();
            if (AuthService.CurrentUser != null)
            {
                readChapters = await GetReadChaptersAsync();
            }

            // Transformar datos para mostrar en UI
            _allChapters = chapters.Select(ch => new
            {
                Id = ch.Id,
                Title = readChapters.Contains(ch.Id)
                    ? $"✓ {LocalizationService.GetString("Chapter")} {ch.ChapterNumber}: {ch.Title}"
                    : $"{LocalizationService.GetString("Chapter")} {ch.ChapterNumber}: {ch.Title}",
                Date = ch.CreatedAt.ToString("dd/MM/yyyy"),
                TextColor = readChapters.Contains(ch.Id)
                    ? (Application.Current.RequestedTheme == AppTheme.Light ? "#6B7280" : "#9CA3AF")
                    : (Application.Current.RequestedTheme == AppTheme.Light ? "#1F2937" : "#F3F4F6"),
                ChapterNumber = ch.ChapterNumber
            }).Cast<dynamic>().ToList();

            // Actualizar UI en hilo principal
            Device.BeginInvokeOnMainThread(() =>
            {
                ChaptersList.ItemsSource = _allChapters;
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
        try
        {
            if (_novel == null || _novel.ChapterCount == 0)
            {
                await DisplayAlert(
                LocalizationService.GetString("Info"),
                LocalizationService.GetString("NoChaptersYet"),
                LocalizationService.GetString("OK"));
                return;
            }

            // Obtener todos los capítulos
            var chapters = await _novelService.GetChaptersAsync(_novelId);
            if (chapters == null || chapters.Count == 0)
            {
                await DisplayAlert(
                LocalizationService.GetString("NoChaptersAvailable"),
                LocalizationService.GetString("NoChaptersAvailable"),
                LocalizationService.GetString("OK"));
                return;
            }

            // Ordenar por número de capítulo
            chapters = chapters.OrderBy(c => c.ChapterNumber).ToList();

            int chapterToRead = chapters.First().Id; // Por defecto el primero

            // Si hay usuario logueado, buscar su progreso
            if (AuthService.CurrentUser != null)
            {
                // Primero verificar si hay un capítulo con progreso parcial
                var currentProgress = await GetCurrentReadingProgress();

                if (currentProgress != null && !currentProgress.Value.IsCompleted)
                {
                    // Hay un capítulo a medio leer, continuar ese
                    var chapterInProgress = chapters.FirstOrDefault(c => c.ChapterNumber == currentProgress.Value.ChapterNumber);
                    if (chapterInProgress != null)
                    {
                        chapterToRead = chapterInProgress.Id;
                    }
                }
                else
                {
                    // No hay capítulo a medio leer, buscar el último completado
                    var userLibrary = await _libraryService.GetUserLibraryAsync();
                    var novelInLibrary = userLibrary.FirstOrDefault(x => x.NovelId == _novelId);

                    if (novelInLibrary != null && novelInLibrary.LastReadChapter > 0)
                    {
                        if (novelInLibrary.LastReadChapter >= _novel.ChapterCount)
                        {
                            // Ha completado todos, releer desde el primero
                            chapterToRead = chapters.First().Id;
                        }
                        else
                        {
                            // Continuar con el siguiente
                            var nextChapter = chapters.FirstOrDefault(c => c.ChapterNumber == novelInLibrary.LastReadChapter + 1);
                            if (nextChapter != null)
                            {
                                chapterToRead = nextChapter.Id;
                            }
                        }
                    }
                }
            }

            // Navegar al lector con el capítulo correcto
            await Navigation.PushAsync(new ReaderPage(_novelId, chapterToRead, _novel.Title));
        }
        catch (Exception ex)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("ChapterNotFound"),
            LocalizationService.GetString("OK"));
        }
    }

    // Método auxiliar para obtener el último capítulo leído
    private async Task<int> GetLastReadChapterAsync()
    {
        try
        {
            if (AuthService.CurrentUser == null) return 0;

            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT last_read_chapter 
                     FROM user_library 
                     WHERE user_id = @userId AND novel_id = @novelId";

            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            command.Parameters.AddWithValue("@novelId", _novelId);

            var result = await command.ExecuteScalarAsync();
            return result != DBNull.Value && result != null ? Convert.ToInt32(result) : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Maneja agregar/quitar de biblioteca
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
                        await DisplayAlert(
                            LocalizationService.GetString("Success"),
                            LocalizationService.GetString("AddedToLibrary"),
                            LocalizationService.GetString("OK"));
                    }
                    else
                    {
                        button.Text = originalText;
                        await DisplayAlert(
                            LocalizationService.GetString("Error"),
                            LocalizationService.GetString("ErrorAddingToLibrary"),
                            LocalizationService.GetString("OK"));
                    }
                }
                else
                {
                    // Confirmar eliminación
                    bool confirm = await DisplayAlert(
                        LocalizationService.GetString("Confirm"),
                        LocalizationService.GetString("ConfirmRemoveFromLibrary"),
                        LocalizationService.GetString("Yes"),
                        LocalizationService.GetString("No"));
                    if (confirm)
                    {
                        success = await _libraryService.RemoveFromLibraryAsync(_novelId);
                        if (success)
                        {
                            UpdateButtonAppearance(button, false);
                            await DisplayAlert(
                                LocalizationService.GetString("Info"),
                                LocalizationService.GetString("RemovedFromLibrary"),
                                LocalizationService.GetString("OK"));
                        }
                        else
                        {
                            button.Text = originalText;
                            await DisplayAlert(
                                LocalizationService.GetString("Error"),
                                LocalizationService.GetString("ErrorRemovingFromLibrary"),
                                LocalizationService.GetString("OK"));
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
                await DisplayAlert(
                        LocalizationService.GetString("Error"),
                        LocalizationService.GetString("ErrorProcessingRequest"),
                        LocalizationService.GetString("OK"));

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

        // Obtener colores según el tema actual
        var inactiveTextColor = Application.Current.RequestedTheme == AppTheme.Light
            ? (Color)Application.Current.Resources["TextSecondaryLight"]
            : (Color)Application.Current.Resources["TextSecondary"];

        // Resetear colores de tabs
        SynopsisTab.TextColor = inactiveTextColor;
        ChaptersTab.TextColor = inactiveTextColor;
        DetailsTab.TextColor = inactiveTextColor;

        // Mostrar tab seleccionado
        switch (tabName)
        {
            case "Synopsis":
                SynopsisContent.IsVisible = true;
                SynopsisTab.TextColor = (Color)Application.Current.Resources["Primary"];
                break;
            case "Chapters":
                ChaptersContent.IsVisible = true;
                ChaptersTab.TextColor = (Color)Application.Current.Resources["Primary"];
                break;
            case "Details":
                DetailsContent.IsVisible = true;
                DetailsTab.TextColor = (Color)Application.Current.Resources["Primary"];
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

    /// <summary>
    /// Maneja el filtrado de capítulos en la página de detalles
    /// </summary>
    private async void OnChapterFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            _currentChapterFilter = button.Text;

            // Actualizar visual de botones
            UpdateChapterFilterButtons(button);

            // Aplicar filtro
            await ApplyChapterFilter();
        }
    }

    /// <summary>
    /// Actualiza el estado visual de los botones de filtro
    /// </summary>
    private void UpdateChapterFilterButtons(Button selectedButton)
    {
        // Buscar el contenedor de botones de filtro
        var parent = selectedButton.Parent as HorizontalStackLayout;
        if (parent != null)
        {
            // Obtener colores según el tema actual
            var inactiveBackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                ? (Color)Application.Current.Resources["BackgroundMediumLight"]
                : (Color)Application.Current.Resources["BackgroundMedium"];

            var inactiveTextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? (Color)Application.Current.Resources["TextSecondaryLight"]
                : (Color)Application.Current.Resources["TextSecondary"];

            foreach (var child in parent.Children)
            {
                if (child is Button btn)
                {
                    if (btn == selectedButton)
                    {
                        btn.BackgroundColor = (Color)Application.Current.Resources["Primary"];
                        btn.TextColor = Colors.White;
                    }
                    else
                    {
                        btn.BackgroundColor = inactiveBackgroundColor;
                        btn.TextColor = inactiveTextColor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Aplica el filtro seleccionado a la lista de capítulos
    /// </summary>
    private async Task ApplyChapterFilter()
    {
        if (_allChapters.Count == 0) return;

        var filteredChapters = new List<dynamic>();

        var allText = LocalizationService.GetString("AllChapters");
        var unreadText = LocalizationService.GetString("Unread");
        var reverseText = LocalizationService.GetString("ReverseOrder");

        switch (_currentChapterFilter)
        {
            case var filter when filter == allText:
                filteredChapters = _allChapters;
                break;

            case var filter when filter == unreadText:
                // Obtener capítulos no leídos
                if (AuthService.CurrentUser != null)
                {
                    var readChapters = await GetReadChaptersAsync();
                    filteredChapters = _allChapters.Where(ch => !readChapters.Contains((int)ch.Id)).ToList();
                }
                else
                {
                    filteredChapters = _allChapters; // Si no hay usuario, mostrar todos
                }
                break;

            case var filter when filter == reverseText: // Invertir orden
                filteredChapters = _allChapters.AsEnumerable().Reverse().ToList();
                _allChapters = filteredChapters; // Actualizar la lista principal
                break;

            default:
                filteredChapters = _allChapters;
                break;
        }

        Device.BeginInvokeOnMainThread(() =>
        {
            ChaptersList.ItemsSource = filteredChapters;
        });
    }

    /// <summary>
    /// Obtiene los IDs de capítulos ya leídos por el usuario
    /// </summary>
    private async Task<HashSet<int>> GetReadChaptersAsync()
    {
        var readChapters = new HashSet<int>();

        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT DISTINCT chapter_id 
                     FROM reading_progress 
                     WHERE user_id = @userId 
                     AND chapter_id IN (SELECT id FROM chapters WHERE novel_id = @novelId)
                     AND is_completed = 1";

            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            command.Parameters.AddWithValue("@novelId", _novelId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                readChapters.Add(reader.GetInt32(0));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo capítulos leídos: {ex.Message}");
        }

        return readChapters;
    }

    /// <summary>
    /// Maneja la búsqueda de capítulos
    /// </summary>
    private void OnChapterSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Si no hay búsqueda, aplicar filtro actual
            _ = ApplyChapterFilter();
        }
        else
        {
            // Filtrar por texto
            var searchResults = _allChapters.Where(ch =>
                ch.Title.ToLower().Contains(searchText)).ToList();

            Device.BeginInvokeOnMainThread(() =>
            {
                ChaptersList.ItemsSource = searchResults;
            });
        }
    }

    /// <summary>
    /// Maneja el clic en el botón de reseñas
    /// </summary>
    private async void OnReviewsClicked(object sender, EventArgs e)
    {
        if (_novel != null)
        {
            // Navegar a la página de reseñas
            await Navigation.PushAsync(new ReviewsPage(_novelId, _novel.Title));
        }
    }

    /// <summary>
    /// Maneja el tap en el nombre del autor para navegar a sus novelas
    /// </summary>
    private async void OnAuthorTapped(object sender, EventArgs e)
    {
        if (_novel != null && !string.IsNullOrEmpty(_novel.Author))
        {
            await Navigation.PushAsync(new AuthorNovelsPage(_novel.Author));
        }
    }

    // Constructor temporal para compatibilidad
    public NovelDetailPage() : this(1)
    {
    }

    #region ========== SISTEMA DE ETIQUETAS ==========

    /// <summary>
    /// Carga las etiquetas de la novela
    /// </summary>
    private async Task LoadTagsAsync()
    {
        try
        {
            var currentUserId = AuthService.CurrentUser?.Id;
            _novelTags = await _tagService.GetTagsByNovelAsync(_novelId, currentUserId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TagsLayout.Children.Clear();

                if (_novelTags.Count == 0)
                {
                    NoTagsLabel.IsVisible = true;
                    return;
                }

                NoTagsLabel.IsVisible = false;

                foreach (var tag in _novelTags)
                {
                    var tagView = CreateTagView(tag);
                    TagsLayout.Children.Add(tagView);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando etiquetas: {ex.Message}");
        }
    }

    /// <summary>
    /// Crea la vista visual de una etiqueta
    /// </summary>
    private Frame CreateTagView(NovelTag tag)
    {
        var currentUserId = AuthService.CurrentUser?.Id ?? 0;
        var isAdmin = AuthService.CurrentUser?.Role == "admin";
        var canDelete = tag.CanBeDeletedByCreator(currentUserId) || isAdmin;

        // Color según si el usuario votó
        var bgColor = tag.UserHasVoted
            ? Color.FromArgb("#6200EE")  // Púrpura si votó
            : Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#E8E8E8")
                : Color.FromArgb("#3D3D3D");

        var textColor = tag.UserHasVoted
            ? Colors.White
            : Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#333333")
                : Color.FromArgb("#FFFFFF");

        var frame = new Frame
        {
            BackgroundColor = bgColor,
            CornerRadius = 15,
            Padding = new Thickness(10, 5),
            HasShadow = false,
            Margin = new Thickness(0, 0, 8, 8)
        };

        var stack = new HorizontalStackLayout { Spacing = 5 };

        // Nombre de la etiqueta
        var nameLabel = new Label
        {
            Text = tag.TagName,
            TextColor = textColor,
            FontSize = 12,
            VerticalOptions = LayoutOptions.Center
        };
        stack.Children.Add(nameLabel);

        // Contador de votos
        if (tag.VoteCount > 0)
        {
            var voteLabel = new Label
            {
                Text = $"({tag.VoteCount + 1})", // +1 incluye al creador
                TextColor = textColor,
                FontSize = 10,
                VerticalOptions = LayoutOptions.Center,
                Opacity = 0.7
            };
            stack.Children.Add(voteLabel);
        }

        // Botón eliminar (solo si puede)
        if (canDelete)
        {
            var deleteBtn = new Label
            {
                Text = "✕",
                TextColor = textColor,
                FontSize = 10,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            var deleteTap = new TapGestureRecognizer();
            deleteTap.Tapped += async (s, e) => await OnDeleteTagClicked(tag);
            deleteBtn.GestureRecognizers.Add(deleteTap);

            stack.Children.Add(deleteBtn);
        }

        frame.Content = stack;

        // Tap para votar o ver novelas con esta etiqueta
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) => await OnTagTapped(tag);
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    /// <summary>
    /// Evento al presionar agregar etiqueta
    /// </summary>
    private async void OnAddTagClicked(object sender, EventArgs e)
    {
        if (AuthService.CurrentUser == null)
        {
            await DisplayAlert(
                LocalizationService.GetString("Error"),
                LocalizationService.GetString("LoginRequired"),
                LocalizationService.GetString("OK"));
            return;
        }

        // Abrir página modal para agregar etiqueta
        var addTagPage = new AddTagPage(_novelId, AuthService.CurrentUser.Id);

        addTagPage.Disappearing += async (s, args) =>
        {
            if (addTagPage.TagAdded)
            {
                await DisplayAlert(
                    LocalizationService.GetString("Success"),
                    addTagPage.ResultMessage,
                    LocalizationService.GetString("OK"));

                await LoadTagsAsync();
            }
            else if (!string.IsNullOrEmpty(addTagPage.ResultMessage))
            {
                await DisplayAlert(
                    LocalizationService.GetString("Info"),
                    addTagPage.ResultMessage,
                    LocalizationService.GetString("OK"));
            }
        };

        await Navigation.PushModalAsync(new NavigationPage(addTagPage));
    }

    /// <summary>
    /// Evento al tocar una etiqueta (votar o ver novelas)
    /// </summary>
    private async Task OnTagTapped(NovelTag tag)
    {
        if (AuthService.CurrentUser == null)
        {
            // Si no está logueado, mostrar novelas con esta etiqueta
            await NavigateToTaggedNovels(tag.TagName);
            return;
        }

        var action = await DisplayActionSheet(
            $"Etiqueta: {tag.TagName}",
            LocalizationService.GetString("Cancel"),
            null,
            tag.UserHasVoted ? "Quitar mi voto" : "Votar esta etiqueta",
            "Ver novelas con esta etiqueta");

        if (action == null || action == LocalizationService.GetString("Cancel"))
            return;

        if (action.Contains("Votar") || action.Contains("Quitar"))
        {
            var voted = await _tagService.VoteTagAsync(tag.Id, AuthService.CurrentUser.Id);
            await DisplayAlert(
                LocalizationService.GetString("Success"),
                voted ? LocalizationService.GetString("VoteAdded") : LocalizationService.GetString("VoteRemoved"),
                LocalizationService.GetString("OK"));
            await LoadTagsAsync();
        }
        else if (action.Contains("Ver novelas"))
        {
            await NavigateToTaggedNovels(tag.TagName);
        }
    }

    /// <summary>
    /// Evento al eliminar una etiqueta
    /// </summary>
    private async Task OnDeleteTagClicked(NovelTag tag)
    {
        var confirm = await DisplayAlert(
            LocalizationService.GetString("DeleteTagConfirm"),
            $"¿Eliminar la etiqueta \"{tag.TagName}\"?",
            LocalizationService.GetString("Yes"),
            LocalizationService.GetString("No"));

        if (!confirm)
            return;

        var isAdmin = AuthService.CurrentUser?.Role == "admin";
        var (success, message) = await _tagService.DeleteTagAsync(
            tag.Id,
            AuthService.CurrentUser?.Id ?? 0,
            isAdmin);

        await DisplayAlert(
            success ? LocalizationService.GetString("Success") : LocalizationService.GetString("Error"),
            message,
            LocalizationService.GetString("OK"));

        if (success)
        {
            await LoadTagsAsync();
        }
    }

    /// <summary>
    /// Navega a la página de novelas con la etiqueta seleccionada
    /// </summary>
    private async Task NavigateToTaggedNovels(string tagName)
    {
        await Navigation.PushAsync(new TaggedNovelsPage(tagName));
    }

    #endregion
}