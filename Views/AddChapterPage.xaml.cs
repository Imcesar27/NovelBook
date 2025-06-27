using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class AddChapterPage : ContentPage
{
    private readonly NovelService _novelService;
    private readonly DatabaseService _databaseService;
    private int _novelId;
    private int _nextChapterNumber;

    public AddChapterPage(int novelId, string novelTitle, int currentChapterCount)
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);

        _novelId = novelId;
        _nextChapterNumber = currentChapterCount + 1;

        // Configurar información de la novela
        NovelTitle.Text = novelTitle;
        ChapterInfo.Text = $"Agregando capítulo {_nextChapterNumber}";
        ChapterNumberEntry.Text = _nextChapterNumber.ToString();

        // Configurar eventos
        ContentEditor.TextChanged += OnContentTextChanged;
    }

    private void OnContentTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue ?? "";
        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var charCount = text.Length;

        WordCountLabel.Text = $"{wordCount} palabras";
        CharCountLabel.Text = $"{charCount} caracteres";

        // Actualizar vista previa si está activa
        if (PreviewSwitch.IsToggled)
        {
            UpdatePreview();
        }
    }

    private void OnBoldClicked(object sender, EventArgs e)
    {
        InsertHtmlTag("strong");
    }

    private void OnItalicClicked(object sender, EventArgs e)
    {
        InsertHtmlTag("em");
    }

    private void OnUnderlineClicked(object sender, EventArgs e)
    {
        InsertHtmlTag("u");
    }

    private void OnParagraphClicked(object sender, EventArgs e)
    {
        ContentEditor.Text += "\n\n";
    }

    private void OnDividerClicked(object sender, EventArgs e)
    {
        ContentEditor.Text += "\n<hr/>\n";
    }

    private void OnDialogClicked(object sender, EventArgs e)
    {
        var cursorPosition = ContentEditor.CursorPosition;
        var text = ContentEditor.Text ?? "";
        var before = text.Substring(0, cursorPosition);
        var after = text.Substring(cursorPosition);

        ContentEditor.Text = $"{before}«»{after}";
        ContentEditor.CursorPosition = cursorPosition + 1;
    }

    private void InsertHtmlTag(string tag)
    {
        var text = ContentEditor.Text ?? "";
        var cursorPosition = ContentEditor.CursorPosition;

        var before = text.Substring(0, cursorPosition);
        var after = text.Substring(cursorPosition);

        ContentEditor.Text = $"{before}<{tag}></{tag}>{after}";
        ContentEditor.CursorPosition = cursorPosition + tag.Length + 2;
    }

    private void OnPreviewToggled(object sender, ToggledEventArgs e)
    {
        PreviewFrame.IsVisible = e.Value;
        if (e.Value)
        {
            UpdatePreview();
        }
    }

    private void UpdatePreview()
    {
        var content = ContentEditor.Text ?? "";

        // Convertir saltos de línea simples a <br/>
        content = content.Replace("\n", "<br/>");

        // Crear FormattedString para mostrar HTML básico
        var formattedString = new FormattedString();

        // Por ahora mostrar el texto plano
        // En una implementación completa, parsearíamos el HTML
        formattedString.Spans.Add(new Span { Text = content.Replace("<br/>", "\n") });

        PreviewLabel.FormattedText = formattedString;
    }

    private async void OnPublishClicked(object sender, EventArgs e)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(ChapterTitleEntry.Text))
        {
            await DisplayAlert("Error", "El título del capítulo es obligatorio", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ContentEditor.Text))
        {
            await DisplayAlert("Error", "El contenido del capítulo es obligatorio", "OK");
            return;
        }

        if (!int.TryParse(ChapterNumberEntry.Text, out int chapterNumber) || chapterNumber < 1)
        {
            await DisplayAlert("Error", "Número de capítulo inválido", "OK");
            return;
        }

        var publishButton = sender as Button;
        publishButton.IsEnabled = false;
        publishButton.Text = "Publicando...";

        try
        {
            var success = await _novelService.AddChapterAsync(
                _novelId,
                chapterNumber,
                ChapterTitleEntry.Text,
                ContentEditor.Text
            );

            if (success)
            {
                await DisplayAlert("Éxito", "Capítulo publicado exitosamente", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo publicar el capítulo", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            publishButton.IsEnabled = true;
            publishButton.Text = "Publicar Capítulo";
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Confirmar", "¿Deseas cancelar? Se perderá el contenido", "Sí", "No");
        if (confirm)
        {
            await Navigation.PopAsync();
        }
    }
}