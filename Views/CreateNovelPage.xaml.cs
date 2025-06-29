using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class CreateNovelPage : ContentPage
{
    private readonly NovelService _novelService;
    private readonly DatabaseService _databaseService;
    private List<string> _selectedGenres = new List<string>();
    private byte[] _coverImageData;
    private string _coverImageType;

    public CreateNovelPage()
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);

        StatusPicker.SelectedIndex = 0;

        SynopsisEditor.TextChanged += OnSynopsisTextChanged;
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

    private void OnGenreClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            var genre = button.Text;

            if (_selectedGenres.Contains(genre))
            {
                _selectedGenres.Remove(genre);
                button.BackgroundColor = Color.FromArgb("#3D3D3D");
                button.TextColor = Color.FromArgb("#B0B0B0");
            }
            else
            {
                _selectedGenres.Add(genre);
                button.BackgroundColor = Color.FromArgb("#8B5CF6");
                button.TextColor = Colors.White;
            }
        }
    }

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona una imagen de portada",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                // Verificar tamaño
                var stream = await result.OpenReadAsync();
                if (stream.Length > 2 * 1024 * 1024) // 2MB
                {
                    await DisplayAlert("Error", "La imagen no debe superar 2MB", "OK");
                    return;
                }

                // Leer imagen
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    _coverImageData = memoryStream.ToArray();
                    _coverImageType = result.ContentType;
                }

                // Mostrar preview
                CoverImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(_coverImageData));
                CoverImagePreview.IsVisible = true;
                NoImageLabel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar la imagen", "OK");
        }
    }

    // Métodos para formato de texto en sinopsis
    private void OnBoldClicked(object sender, EventArgs e)
    {
        InsertHtmlTag(SynopsisEditor, "strong");
    }

    private void OnItalicClicked(object sender, EventArgs e)
    {
        InsertHtmlTag(SynopsisEditor, "em");
    }

    private void OnUnderlineClicked(object sender, EventArgs e)
    {
        InsertHtmlTag(SynopsisEditor, "u");
    }

    // Métodos para formato de texto en capítulo
    private void OnChapterBoldClicked(object sender, EventArgs e)
    {
        InsertHtmlTag(ChapterContentEditor, "strong");
    }

    private void OnChapterItalicClicked(object sender, EventArgs e)
    {
        InsertHtmlTag(ChapterContentEditor, "em");
    }

    private void OnChapterUnderlineClicked(object sender, EventArgs e)
    {
        InsertHtmlTag(ChapterContentEditor, "u");
    }

    private void OnParagraphClicked(object sender, EventArgs e)
    {
        ChapterContentEditor.Text += "\n\n";
    }

    private void InsertHtmlTag(Editor editor, string tag)
    {
        var text = editor.Text ?? "";
        var cursorPosition = editor.CursorPosition;

        // Si hay texto seleccionado, envolverlo
        // Por ahora, solo agregar las etiquetas
        var before = text.Substring(0, cursorPosition);
        var after = text.Substring(cursorPosition);

        editor.Text = $"{before}<{tag}></{tag}>{after}";
        editor.CursorPosition = cursorPosition + tag.Length + 2;
    }

    private async void OnCreateNovelClicked(object sender, EventArgs e)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlert("Error", "El título es obligatorio", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(AuthorEntry.Text))
        {
            await DisplayAlert("Error", "El autor es obligatorio", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(SynopsisEditor.Text))
        {
            await DisplayAlert("Error", "La sinopsis es obligatoria", "OK");
            return;
        }

        var createButton = sender as Button;
        createButton.IsEnabled = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            // Mapear estado
            var statusMap = new Dictionary<string, string>
            {
                { "En curso", "ongoing" },
                { "Completado", "completed" },
                { "En pausa", "hiatus" }
            };

            var status = statusMap[StatusPicker.SelectedItem.ToString()];

            // Crear la novela
            var success = await _novelService.CreateNovelAsync(
                TitleEntry.Text,
                AuthorEntry.Text,
                SynopsisEditor.Text,
                status,
                _selectedGenres,
                _coverImageData,
                _coverImageType,
                ChapterTitleEntry.Text,
                ChapterContentEditor.Text
            );

            if (success)
            {
                await DisplayAlert("Éxito", "Novela creada exitosamente", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo crear la novela", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            createButton.IsEnabled = true;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Se ejecuta cuando aparece la página
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Deshabilitar navegación de Shell
        Shell.SetNavBarIsVisible(this, false);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Cancelar", "¿Deseas cancelar la creación de la novela?", "Sí", "No");
        if (answer)
        {
            await Navigation.PopAsync();
        }
    }
}