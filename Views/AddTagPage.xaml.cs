using NovelBook.Services;

namespace NovelBook.Views;

public partial class AddTagPage : ContentPage
{
    private readonly TagService _tagService;
    private readonly int _novelId;
    private readonly int _userId;
    private List<(string TagName, int Count)> _allTags = new();

    // Resultado para devolver a la página anterior
    public bool TagAdded { get; private set; } = false;
    public string ResultMessage { get; private set; } = "";

    public AddTagPage(int novelId, int userId)
    {
        InitializeComponent();

        _novelId = novelId;
        _userId = userId;
        _tagService = new TagService(new DatabaseService());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPopularTagsAsync();
    }

    /// <summary>
    /// Carga las etiquetas populares del sistema
    /// </summary>
    private async Task LoadPopularTagsAsync()
    {
        try
        {
            _allTags = await _tagService.GetPopularTagsAsync(50);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PopularTagsContainer.Children.Clear();

                if (_allTags.Count == 0)
                {
                    PopularTagsContainer.Children.Add(new Label
                    {
                        Text = "No hay etiquetas aún. ¡Sé el primero!",
                        TextColor = Color.FromArgb("#888888"),
                        FontSize = 12
                    });
                    return;
                }

                foreach (var tag in _allTags.Take(20))
                {
                    var tagView = CreateTagButton(tag.TagName, tag.Count);
                    PopularTagsContainer.Children.Add(tagView);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando etiquetas: {ex.Message}");
        }
    }

    /// <summary>
    /// Filtra sugerencias mientras el usuario escribe
    /// </summary>
    private void OnTagEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue?.Trim() ?? "";

        // Actualizar contador de caracteres
        CharCountLabel.Text = $"{text.Length}/25 caracteres";

        // Habilitar/deshabilitar botón
        AddButton.IsEnabled = text.Length >= 2;

        // Filtrar sugerencias
        FilterSuggestions(text);
    }

    /// <summary>
    /// Filtra las etiquetas según el texto escrito
    /// </summary>
    private void FilterSuggestions(string searchText)
    {
        SuggestionsContainer.Children.Clear();

        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
        {
            SuggestionsLabel.Text = "Sugerencias:";
            SuggestionsContainer.Children.Add(new Label
            {
                Text = "Escribe al menos 2 caracteres para ver sugerencias",
                TextColor = Color.FromArgb("#888888"),
                FontSize = 12
            });
            return;
        }

        var filtered = _allTags
            .Where(t => t.TagName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        if (filtered.Count == 0)
        {
            SuggestionsLabel.Text = "Sugerencias:";
            SuggestionsContainer.Children.Add(new Label
            {
                Text = $"No hay coincidencias. Se creará: \"{FormatTagName(searchText)}\"",
                TextColor = Color.FromArgb("#4CAF50"),
                FontSize = 12
            });
            return;
        }

        SuggestionsLabel.Text = $"Sugerencias ({filtered.Count}):";

        foreach (var tag in filtered)
        {
            var tagView = CreateTagButton(tag.TagName, tag.Count, isSelected:
                tag.TagName.Equals(searchText, StringComparison.OrdinalIgnoreCase));
            SuggestionsContainer.Children.Add(tagView);
        }
    }

    /// <summary>
    /// Crea un botón de etiqueta clickeable
    /// </summary>
    private Frame CreateTagButton(string tagName, int count, bool isSelected = false)
    {
        var bgColor = isSelected
            ? Color.FromArgb("#6200EE")
            : Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#E8E8E8")
                : Color.FromArgb("#3D3D3D");

        var textColor = isSelected
            ? Colors.White
            : Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#333333")
                : Color.FromArgb("#FFFFFF");

        var frame = new Frame
        {
            BackgroundColor = bgColor,
            CornerRadius = 15,
            Padding = new Thickness(12, 6),
            HasShadow = false,
            Margin = new Thickness(0, 0, 8, 8)
        };

        var stack = new HorizontalStackLayout { Spacing = 5 };

        stack.Children.Add(new Label
        {
            Text = tagName,
            TextColor = textColor,
            FontSize = 13,
            VerticalOptions = LayoutOptions.Center
        });

        if (count > 0)
        {
            stack.Children.Add(new Label
            {
                Text = $"({count})",
                TextColor = textColor,
                FontSize = 11,
                Opacity = 0.7,
                VerticalOptions = LayoutOptions.Center
            });
        }

        frame.Content = stack;

        // Al tocar, poner el texto en el Entry
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) =>
        {
            TagEntry.Text = tagName;
        };
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    /// <summary>
    /// Formatea el nombre de la etiqueta (primera letra mayúscula)
    /// </summary>
    private string FormatTagName(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        text = text.Trim();
        return char.ToUpper(text[0]) + text.Substring(1).ToLower();
    }

    /// <summary>
    /// Evento cancelar
    /// </summary>
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Evento agregar etiqueta
    /// </summary>
    private async void OnAddClicked(object sender, EventArgs e)
    {
        var tagName = TagEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(tagName) || tagName.Length < 2)
        {
            await DisplayAlert("Error", "La etiqueta debe tener al menos 2 caracteres", "OK");
            return;
        }

        try
        {
            AddButton.IsEnabled = false;
            AddButton.Text = "Agregando...";

            var (success, message) = await _tagService.AddTagAsync(_novelId, _userId, tagName);

            TagAdded = success;
            ResultMessage = message;

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo agregar: {ex.Message}", "OK");
            AddButton.IsEnabled = true;
            AddButton.Text = "Agregar";
        }
    }
}