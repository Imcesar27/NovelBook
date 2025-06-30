using NovelBook.Models;
using NovelBook.Services;
using Microsoft.Maui.Controls;

namespace NovelBook.Views;

[QueryProperty(nameof(NovelId), "novelId")]
public partial class EditNovelPage : ContentPage
{
    // Servicios necesarios
    private readonly NovelService _novelService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Variables de estado
    private int _novelId;
    private Novel _currentNovel;
    private List<string> _selectedGenres = new List<string>();
    private List<string> _allGenres = new List<string>();
    private byte[] _selectedImageData;
    private string _selectedImageType;
    private bool _hasChanges = false;
    private List<Chapter> _chapters = new List<Chapter>();

    // Propiedad para recibir el ID por navegación
    public int NovelId
    {
        get => _novelId;
        set
        {
            _novelId = value;
            // Cargar datos cuando se establece el ID
            if (_novelId > 0)
            {
                _ = LoadNovelDataAsync();
            }
        }
    }

    public EditNovelPage()
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);
        _imageService = new ImageService(_databaseService);
    }

    /// <summary>
    /// Carga los datos de la novela para editar
    /// </summary>
    private async Task LoadNovelDataAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;

            // Cargar la novela
            _currentNovel = await _novelService.GetNovelByIdAsync(_novelId);

            if (_currentNovel == null)
            {
                await DisplayAlert("Error", "No se pudo cargar la novela", "OK");
                await Navigation.PopAsync();
                return;
            }

            // Cargar géneros disponibles
            await LoadAvailableGenres();

            // Cargar géneros de la novela
            await LoadNovelGenres();

            // Cargar capítulos
            await LoadChapters();

            // Cargar imagen
            await LoadNovelImage();

            // Actualizar UI
            Device.BeginInvokeOnMainThread(() =>
            {
                TitleEntry.Text = _currentNovel.Title;
                AuthorEntry.Text = _currentNovel.Author;
                SynopsisEditor.Text = _currentNovel.Synopsis;

                // Establecer el estado en el Picker
                switch (_currentNovel.Status.ToLower())
                {
                    case "ongoing":
                        StatusPicker.SelectedIndex = 0; // En curso
                        break;
                    case "completed":
                        StatusPicker.SelectedIndex = 1; // Completado
                        break;
                    case "hiatus":     // Cambiar de "paused" a "hiatus"
                    case "paused":     // Mantener compatibilidad por si acaso
                        StatusPicker.SelectedIndex = 2; // En pausa
                        break;
                    case "cancelled":
                        StatusPicker.SelectedIndex = 3; // Cancelada
                        break;
                    default:
                        StatusPicker.SelectedIndex = 0;
                        break;
                }

                UpdateGenresUI();
                UpdateChapterCount();
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar la novela: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
        }
    }

    /// <summary>
    /// Carga todos los géneros disponibles
    /// </summary>
    private async Task LoadAvailableGenres()
    {
        _allGenres = await _novelService.GetAllGenresAsync();
    }

    /// <summary>
    /// Carga los géneros asociados a la novela
    /// </summary>
    private async Task LoadNovelGenres()
    {
        _selectedGenres = await _novelService.GetNovelGenresAsync(_novelId);
    }

    /// <summary>
    /// Carga los capítulos de la novela
    /// </summary>
    private async Task LoadChapters()
    {
        _chapters = await _novelService.GetChaptersAsync(_novelId);

        Device.BeginInvokeOnMainThread(() =>
        {
            ChaptersList.ItemsSource = _chapters.Select(ch => new
            {
                Id = ch.Id,
                Title = $"Capítulo {ch.ChapterNumber}: {ch.Title}",
                ChapterNumber = ch.ChapterNumber
            }).ToList();
        });
    }

    /// <summary>
    /// Carga la imagen de la novela
    /// </summary>
    private async Task LoadNovelImage()
    {
        try
        {
            if (!string.IsNullOrEmpty(_currentNovel.CoverImage))
            {
                var imageSource = await _imageService.GetCoverImageAsync(_currentNovel.CoverImage);

                Device.BeginInvokeOnMainThread(() =>
                {
                    CoverImagePreview.Source = imageSource;
                    NoImageLabel.IsVisible = false;
                });
            }
            else
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    NoImageLabel.IsVisible = true;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando imagen: {ex.Message}");
        }
    }

    /// <summary>
    /// Actualiza la UI de géneros
    /// </summary>
    private void UpdateGenresUI()
    {
        GenresLayout.Children.Clear();

        foreach (var genre in _allGenres)
        {
            var chip = CreateGenreChip(genre, _selectedGenres.Contains(genre));
            GenresLayout.Children.Add(chip);
        }
    }

    /// <summary>
    /// Crea un chip visual para un género
    /// </summary>
    private Frame CreateGenreChip(string genre, bool isSelected)
    {
        var chip = new Frame
        {
            BackgroundColor = isSelected ?
                Color.FromArgb("#8B5CF6") :
                Color.FromArgb("#2D2D2D"),
            CornerRadius = 15,
            Padding = new Thickness(15, 8),
            HasShadow = false,
            BorderColor = Color.FromArgb("#8B5CF6"),
            Content = new Label
            {
                Text = genre,
                TextColor = isSelected ? Colors.White : Color.FromArgb("#B0B0B0"),
                FontSize = 14
            }
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnGenreChipTapped(genre, chip);
        chip.GestureRecognizers.Add(tapGesture);

        return chip;
    }

    /// <summary>
    /// Maneja el tap en un chip de género
    /// </summary>
    private void OnGenreChipTapped(string genre, Frame chip)
    {
        if (_selectedGenres.Contains(genre))
        {
            _selectedGenres.Remove(genre);
            chip.BackgroundColor = Color.FromArgb("#2D2D2D");
            (chip.Content as Label).TextColor = Color.FromArgb("#B0B0B0");
        }
        else
        {
            _selectedGenres.Add(genre);
            chip.BackgroundColor = Color.FromArgb("#8B5CF6");
            (chip.Content as Label).TextColor = Colors.White;
        }

        _hasChanges = true;
    }

    /// <summary>
    /// Actualiza el contador de capítulos
    /// </summary>
    private void UpdateChapterCount()
    {
        ChapterCountLabel.Text = $"({_chapters.Count} capítulos)";
    }

    /// <summary>
    /// Maneja el cambio de texto en la sinopsis
    /// </summary>
    private void OnSynopsisTextChanged(object sender, TextChangedEventArgs e)
    {
        var length = e.NewTextValue?.Length ?? 0;
        CharCountLabel.Text = $"{length} / 2000 caracteres";

        if (length > 2000)
        {
            CharCountLabel.TextColor = Color.FromArgb("#FF0000");
        }
        else
        {
            CharCountLabel.TextColor = Color.FromArgb("#999999");
        }

        _hasChanges = true;
    }

    /// <summary>
    /// Maneja la selección de imagen
    /// </summary>
    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Selecciona una imagen de portada"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                _selectedImageData = memoryStream.ToArray();
                _selectedImageType = result.ContentType;

                // Mostrar preview
                CoverImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(_selectedImageData));
                NoImageLabel.IsVisible = false;

                _hasChanges = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al seleccionar imagen: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Agrega un nuevo capítulo
    /// </summary>
    private async void OnAddChapterClicked(object sender, EventArgs e)
    {
        string chapterTitle = await DisplayPromptAsync(
            "Nuevo Capítulo",
            "Ingrese el título del capítulo:",
            placeholder: "Título del capítulo");

        if (!string.IsNullOrWhiteSpace(chapterTitle))
        {
            string chapterContent = await DisplayPromptAsync(
                "Contenido",
                "Ingrese el contenido del capítulo:",
                placeholder: "Contenido...");

            if (!string.IsNullOrWhiteSpace(chapterContent))
            {
                int nextChapterNumber = _chapters.Count > 0 ?
                    _chapters.Max(c => c.ChapterNumber) + 1 : 1;

                bool success = await _novelService.AddChapterAsync(
                    _novelId, nextChapterNumber, chapterTitle, chapterContent);

                if (success)
                {
                    await LoadChapters();
                    UpdateChapterCount();
                    await DisplayAlert("Éxito", "Capítulo agregado correctamente", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo agregar el capítulo", "OK");
                }
            }
        }
    }

    /// <summary>
    /// Edita un capítulo existente
    /// </summary>

    private async void OnEditChapterClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int chapterId)
        {
            // Navegar a la nueva página de edición de capítulos mejorada
            await Navigation.PushAsync(new EditChapterPage(chapterId));
        }
    }

    /// <summary>
    /// Actualiza un capítulo en la base de datos
    /// </summary>
    private async Task<bool> UpdateChapterAsync(int chapterId, string title, string content)
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            var query = @"UPDATE chapters 
                         SET title = @title, 
                             content = @content,
                             updated_at = GETDATE()
                         WHERE id = @id";

            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", chapterId);
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@content", content);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando capítulo: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Elimina un capítulo
    /// </summary>
    private async void OnDeleteChapterClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int chapterId)
        {
            bool confirm = await DisplayAlert(
                "Confirmar",
                "¿Estás seguro de eliminar este capítulo?",
                "Sí", "No");

            if (confirm)
            {
                try
                {
                    bool success = await DeleteChapterAsync(chapterId);

                    if (success)
                    {
                        await LoadChapters();
                        UpdateChapterCount();
                        await DisplayAlert("Éxito", "Capítulo eliminado correctamente", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo eliminar el capítulo", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error al eliminar: {ex.Message}", "OK");
                }
            }
        }
    }

    /// <summary>
    /// Elimina un capítulo de la base de datos
    /// </summary>
    private async Task<bool> DeleteChapterAsync(int chapterId)
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            // Primero obtener el número del capítulo y novel_id
            var getInfoQuery = "SELECT novel_id, chapter_number FROM chapters WHERE id = @id";
            using var getInfoCommand = new Microsoft.Data.SqlClient.SqlCommand(getInfoQuery, connection);
            getInfoCommand.Parameters.AddWithValue("@id", chapterId);

            int novelId = 0;
            int chapterNumber = 0;

            using (var reader = await getInfoCommand.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    novelId = reader.GetInt32(0);
                    chapterNumber = reader.GetInt32(1);
                }
            }

            // Eliminar el capítulo
            var deleteQuery = "DELETE FROM chapters WHERE id = @id";
            using var deleteCommand = new Microsoft.Data.SqlClient.SqlCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@id", chapterId);

            var rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                // Actualizar los números de los capítulos siguientes
                var updateQuery = @"UPDATE chapters 
                                   SET chapter_number = chapter_number - 1 
                                   WHERE novel_id = @novelId 
                                   AND chapter_number > @chapterNumber";

                using var updateCommand = new Microsoft.Data.SqlClient.SqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@novelId", novelId);
                updateCommand.Parameters.AddWithValue("@chapterNumber", chapterNumber);
                await updateCommand.ExecuteNonQueryAsync();

                // Actualizar el conteo en la tabla novels
                var updateNovelQuery = @"UPDATE novels 
                                        SET chapter_count = (SELECT COUNT(*) FROM chapters WHERE novel_id = @novelId),
                                            updated_at = GETDATE()
                                        WHERE id = @novelId";

                using var updateNovelCommand = new Microsoft.Data.SqlClient.SqlCommand(updateNovelQuery, connection);
                updateNovelCommand.Parameters.AddWithValue("@novelId", novelId);
                await updateNovelCommand.ExecuteNonQueryAsync();
            }

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando capítulo: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Maneja el botón cancelar
    /// </summary>
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            if (_hasChanges)
            {
                // Preguntar si quiere descartar cambios
                bool confirm = await DisplayAlert(
                    "Descartar cambios",
                    "¿Estás seguro de que quieres descartar los cambios?",
                    "Sí",
                    "No");

                if (!confirm) return;
            }

            // Navegar hacia atrás
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cancelar: {ex.Message}");
            await DisplayAlert("Error", "No se pudo cancelar la operación", "OK");
        }
    }

    /// <summary>
    /// Maneja el guardado de cambios
    /// </summary>
    private async void OnSaveChangesClicked(object sender, EventArgs e)
    {
        try
        {
            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(TitleEntry.Text))
            {
                await DisplayAlert("Error", "El título es requerido", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(AuthorEntry.Text))
            {
                await DisplayAlert("Error", "El autor es requerido", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(SynopsisEditor.Text))
            {
                await DisplayAlert("Error", "La sinopsis es requerida", "OK");
                return;
            }

            // Mostrar indicador de carga
            LoadingIndicator.IsVisible = true;

            // Obtener el estado seleccionado
            string status = StatusPicker.SelectedIndex switch
            {
                0 => "ongoing",    // En curso
                1 => "completed",  // Completado
                2 => "hiatus",     // En pausa (cambiar de "paused" a "hiatus")
                3 => "cancelled",  // Cancelada
                _ => "ongoing"
            };

            // Actualizar la novela
            bool success = await _novelService.UpdateNovelAsync(
                _novelId,
                TitleEntry.Text.Trim(),
                AuthorEntry.Text.Trim(),
                SynopsisEditor.Text.Trim(),
                status,
                _selectedGenres,
                _selectedImageData,
                _selectedImageType
            );

            if (success)
            {
                await DisplayAlert("Éxito", "Los cambios se guardaron correctamente", "OK");

                // IMPORTANTE: Recargar los datos para ver los cambios
                await LoadNovelDataAsync();

                // Resetear la bandera de cambios
                _hasChanges = false;

                // Navegar de vuelta a ManageNovelsPage
                if (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopAsync();
                }
                else
                {
                    await Shell.Current.GoToAsync("..");
                }
            }
            else
            {
                await DisplayAlert("Error", "No se pudieron guardar los cambios", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar: {ex.Message}");
            await DisplayAlert("Error", $"Error al guardar: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
        }
    }

    /// <summary>
    /// Se ejecuta cuando la página aparece
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Recargar datos cada vez que aparece la página
        await LoadNovelDataAsync();
    }
}