using NovelBook.Models;
using NovelBook.Services;
using System.Collections.ObjectModel;

namespace NovelBook.Views;

[QueryProperty(nameof(NovelId), "novelId")]
public partial class EditNovelPage : ContentPage
{
    private readonly NovelService _novelService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    private Novel _novel;
    private byte[] _newCoverImage;
    private string _newImageType;

    private readonly List<string> _availableGenres = new()
    {
        "Romance", "Fantasía", "Acción", "Drama", "Comedia",
        "Misterio", "Horror", "Ciencia Ficción", "Aventura", "Histórico"
    };

    private List<string> _selectedGenres = new();
    private ObservableCollection<ChapterDisplayItem> _chapters;

    private int _novelId;

    public int NovelId
    {
        get => _novelId;
        set
        {
            _novelId = value;
            _ = LoadNovelDataAsync();
        }
    }

    public EditNovelPage()
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);
        _imageService = new ImageService(_databaseService);
        _chapters = new ObservableCollection<ChapterDisplayItem>();

        ChaptersList.ItemsSource = _chapters;
    }

    private async Task LoadNovelDataAsync()
    {
        try
        {
            Device.BeginInvokeOnMainThread(() => LoadingIndicator.IsVisible = true);

            _novel = await _novelService.GetNovelByIdAsync(_novelId);

            if (_novel == null)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Error", "No se pudo cargar la novela", "OK");
                    await Shell.Current.GoToAsync("..");
                });
                return;
            }

            var chapters = await _novelService.GetChaptersAsync(_novelId);

            Device.BeginInvokeOnMainThread(async () =>
            {
                TitleEntry.Text = _novel.Title;
                AuthorEntry.Text = _novel.Author;
                SynopsisEditor.Text = _novel.Synopsis;

                StatusPicker.SelectedItem = _novel.Status switch
                {
                    "ongoing" => "En curso",
                    "completed" => "Completado",
                    "hiatus" => "En pausa",
                    _ => "En curso"
                };

                if (!string.IsNullOrEmpty(_novel.CoverImage))
                {
                    CoverImagePreview.Source = await _imageService.GetCoverImageAsync(_novel.CoverImage);
                    NoImageLabel.IsVisible = false;
                }
                else
                {
                    NoImageLabel.IsVisible = true;
                }

                LoadGenresButtons();
                LoadChapters(chapters);
                LoadingIndicator.IsVisible = false;
            });
        }
        catch (Exception ex)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                LoadingIndicator.IsVisible = false;
                await DisplayAlert("Error", $"Error al cargar: {ex.Message}", "OK");
            });
        }
    }

    private void LoadGenresButtons()
    {
        GenresLayout.Children.Clear();

        if (_novel.Genres != null)
            _selectedGenres = _novel.Genres.Select(g => g.Name).ToList();

        foreach (var genre in _availableGenres)
        {
            var button = new Button
            {
                Text = genre,
                BackgroundColor = _selectedGenres.Contains(genre)
                    ? Color.FromArgb("#8B5CF6")
                    : Color.FromArgb("#3D3D3D"),
                TextColor = Colors.White,
                CornerRadius = 15,
                Padding = new Thickness(15, 5),
                CommandParameter = genre
            };

            button.Clicked += OnGenreClicked;
            GenresLayout.Children.Add(button);
        }
    }

    private void LoadChapters(List<Chapter> chapters)
    {
        _chapters.Clear();

        foreach (var chapter in chapters)
        {
            _chapters.Add(new ChapterDisplayItem
            {
                Id = chapter.Id,
                Title = $"Capítulo {chapter.ChapterNumber}: {chapter.Title}",
                Info = $"Creado: {chapter.CreatedAt:dd/MM/yyyy}"
            });
        }

        ChapterCountLabel.Text = $"({chapters.Count} capítulos)";
    }

    private void OnGenreClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string genre)
        {
            if (_selectedGenres.Contains(genre))
            {
                _selectedGenres.Remove(genre);
                button.BackgroundColor = Color.FromArgb("#3D3D3D");
            }
            else
            {
                _selectedGenres.Add(genre);
                button.BackgroundColor = Color.FromArgb("#8B5CF6");
            }
        }
    }

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Selecciona una imagen de portada"
            });

            if (result != null)
            {
                var stream = await result.OpenReadAsync();
                if (stream.Length > 2 * 1024 * 1024)
                {
                    await DisplayAlert("Error", "La imagen no debe superar los 2MB", "OK");
                    return;
                }

                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                _newCoverImage = memoryStream.ToArray();
                _newImageType = result.ContentType;

                CoverImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(_newCoverImage));
                NoImageLabel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al seleccionar imagen: {ex.Message}", "OK");
        }
    }

    private void OnSynopsisTextChanged(object sender, TextChangedEventArgs e)
    {
        var length = e.NewTextValue?.Length ?? 0;
        CharCountLabel.Text = $"{length} / 2000 caracteres";

        if (length > 2000)
        {
            SynopsisEditor.Text = e.OldTextValue;
        }
    }

    private async void OnAddChapterClicked(object sender, EventArgs e)
    {
        if (_novel == null)
        {
            await DisplayAlert("Error", "Los datos de la novela aún se están cargando", "OK");
            return;
        }

        int nextChapterNumber = _chapters.Count + 1;
        await Navigation.PushAsync(new AddChapterPage(_novelId, _novel.Title, nextChapterNumber));
    }

    private async void OnEditChapterClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int chapterId)
        {
            await DisplayAlert("Info", "Función de edición de capítulo próximamente", "OK");
        }
    }

    private async void OnDeleteChapterClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int chapterId)
        {
            bool confirm = await DisplayAlert("Confirmar", "¿Eliminar este capítulo?", "Eliminar", "Cancelar");

            if (confirm)
            {
                try
                {
                    button.IsEnabled = false;

                    bool success = await _novelService.DeleteChapterAsync(chapterId);

                    if (success)
                    {
                        await DisplayAlert("Éxito", "Capítulo eliminado", "OK");

                        var chapters = await _novelService.GetChaptersAsync(_novelId);
                        LoadChapters(chapters);
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo eliminar el capítulo", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }
    }

    private async void OnSaveChangesClicked(object sender, EventArgs e)
    {
        try
        {
            // Mostrar indicador de carga
            LoadingIndicator.IsVisible = true;

            // Aquí va tu lógica de guardado existente
            // ...

            // Después de guardar exitosamente, navegar hacia atrás
            await DisplayAlert("Éxito", "Los cambios se guardaron correctamente", "OK");

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar: {ex.Message}");
            await DisplayAlert("Error", "No se pudieron guardar los cambios", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            // Preguntar si quiere descartar cambios
            bool confirm = await DisplayAlert(
                "Descartar cambios",
                "¿Estás seguro de que quieres descartar los cambios?",
                "Sí",
                "No");

            if (confirm)
            {
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cancelar: {ex.Message}");
            await DisplayAlert("Error", "No se pudo cancelar la operación", "OK");
        }
    }
}

public class ChapterDisplayItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Info { get; set; }
}
