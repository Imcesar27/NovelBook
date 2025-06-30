using NovelBook.Models;
using NovelBook.Services;
using System.Text;

namespace NovelBook.Views;

public partial class EditChapterPage : ContentPage
{
    // Servicios
    private readonly NovelService _novelService;
    private readonly ChapterService _chapterService;
    private readonly ImageService _imageService;
    private readonly DatabaseService _databaseService;

    // Datos del capítulo
    private int _chapterId;
    private Chapter _chapter;
    private Novel _novel;

    // Control de cambios
    private bool _hasUnsavedChanges = false;
    private DateTime _lastSavedTime;
    private System.Timers.Timer _autoSaveTimer;

    public EditChapterPage(int chapterId)
    {
        InitializeComponent();

        _chapterId = chapterId;

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);
        _chapterService = new ChapterService(_databaseService);
        _imageService = new ImageService(_databaseService);

        // Configurar auto-guardado cada 30 segundos
        _autoSaveTimer = new System.Timers.Timer(30000); // 30 segundos
        _autoSaveTimer.Elapsed += async (s, e) => await AutoSave();

        LoadChapterData();
    }

    /// <summary>
    /// Carga los datos del capítulo a editar
    /// </summary>
    private async void LoadChapterData()
    {
        try
        {
            LoadingIndicator.IsVisible = true;

            // Cargar datos del capítulo
            _chapter = await _chapterService.GetChapterAsync(_chapterId);
            if (_chapter == null)
            {
                await DisplayAlert("Error", "No se pudo cargar el capítulo", "OK");
                await Navigation.PopAsync();
                return;
            }

            // Cargar datos de la novela
            _novel = await _novelService.GetNovelByIdAsync(_chapter.NovelId);
            if (_novel != null)
            {
                NovelTitle.Text = _novel.Title;
                NovelCover.Source = await _imageService.GetCoverImageAsync(_novel.CoverImage);
            }

            // Configurar la UI con los datos del capítulo
            ChapterInfo.Text = $"Editando capítulo {_chapter.ChapterNumber}";
            ChapterNumberEntry.Text = _chapter.ChapterNumber.ToString();
            ChapterTitleEntry.Text = _chapter.Title;
            ContentEditor.Text = _chapter.Content ?? "";

            // Actualizar contadores
            UpdateWordAndCharCount();

            // Iniciar el timer de auto-guardado
            _autoSaveTimer.Start();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar el capítulo: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
        }
    }

    /// <summary>
    /// Maneja los cambios en el contenido
    /// </summary>
    private void OnContentTextChanged(object sender, TextChangedEventArgs e)
    {
        _hasUnsavedChanges = true;
        UpdateWordAndCharCount();
        UpdateLastSavedLabel();

        // Actualizar vista previa si está activa
        if (PreviewSwitch.IsToggled)
        {
            UpdatePreview();
        }
    }

    /// <summary>
    /// Actualiza los contadores de palabras y caracteres
    /// </summary>
    private void UpdateWordAndCharCount()
    {
        string content = ContentEditor.Text ?? "";

        // Contar caracteres
        int charCount = content.Length;
        CharCountLabel.Text = $"{charCount:N0} caracteres";

        // Contar palabras
        string[] words = content.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
        int wordCount = words.Length;
        WordCountLabel.Text = $"{wordCount:N0} palabras";
    }

    /// <summary>
    /// Actualiza la etiqueta de último guardado
    /// </summary>
    private void UpdateLastSavedLabel()
    {
        if (_hasUnsavedChanges)
        {
            LastSavedLabel.Text = "Cambios sin guardar";
            LastSavedLabel.TextColor = Color.FromArgb("#FF6B6B");
        }
        else if (_lastSavedTime != default)
        {
            var timeSinceLastSave = DateTime.Now - _lastSavedTime;
            if (timeSinceLastSave.TotalMinutes < 1)
                LastSavedLabel.Text = "Guardado hace momentos";
            else if (timeSinceLastSave.TotalMinutes < 60)
                LastSavedLabel.Text = $"Guardado hace {(int)timeSinceLastSave.TotalMinutes} min";
            else
                LastSavedLabel.Text = $"Guardado a las {_lastSavedTime:HH:mm}";

            LastSavedLabel.TextColor = Color.FromArgb("#4CAF50");
        }
    }

    #region Herramientas de formato

    /// <summary>
    /// Aplica formato de negrita al texto seleccionado
    /// </summary>
    private void OnBoldClicked(object sender, EventArgs e)
    {
        InsertFormatting("**", "**", "texto en negrita");
    }

    /// <summary>
    /// Aplica formato de cursiva al texto seleccionado
    /// </summary>
    private void OnItalicClicked(object sender, EventArgs e)
    {
        InsertFormatting("*", "*", "texto en cursiva");
    }

    /// <summary>
    /// Aplica formato de subrayado al texto seleccionado
    /// </summary>
    private void OnUnderlineClicked(object sender, EventArgs e)
    {
        InsertFormatting("<u>", "</u>", "texto subrayado");
    }

    /// <summary>
    /// Inserta alineación a la izquierda
    /// </summary>
    private void OnAlignLeftClicked(object sender, EventArgs e)
    {
        InsertAtCursor("\n<p align=\"left\">\n", "\n</p>\n");
    }

    /// <summary>
    /// Inserta alineación al centro
    /// </summary>
    private void OnAlignCenterClicked(object sender, EventArgs e)
    {
        InsertAtCursor("\n<p align=\"center\">\n", "\n</p>\n");
    }

    /// <summary>
    /// Inserta alineación a la derecha
    /// </summary>
    private void OnAlignRightClicked(object sender, EventArgs e)
    {
        InsertAtCursor("\n<p align=\"right\">\n", "\n</p>\n");
    }

    /// <summary>
    /// Inserta una línea divisora
    /// </summary>
    private void OnDividerClicked(object sender, EventArgs e)
    {
        InsertAtCursor("\n\n---\n\n", "");
    }

    /// <summary>
    /// Inserta comillas para diálogo
    /// </summary>
    private void OnDialogClicked(object sender, EventArgs e)
    {
        InsertFormatting("«", "»", "diálogo");
    }

    /// <summary>
    /// Inserta puntos suspensivos
    /// </summary>
    private void OnEllipsisClicked(object sender, EventArgs e)
    {
        InsertAtCursor("…", "");
    }

    /// <summary>
    /// Inserta salto de párrafo
    /// </summary>
    private void OnParagraphClicked(object sender, EventArgs e)
    {
        InsertAtCursor("\n\n", "");
    }

    /// <summary>
    /// Inserta formato para pensamientos
    /// </summary>
    private void OnThoughtClicked(object sender, EventArgs e)
    {
        InsertFormatting("_«", "»_", "pensamiento");
    }

    /// <summary>
    /// Inserta formato para acciones
    /// </summary>
    private void OnActionClicked(object sender, EventArgs e)
    {
        InsertFormatting("***", "***", "acción importante");
    }

    #endregion

    #region Métodos auxiliares de formato

    /// <summary>
    /// Inserta formato alrededor del texto seleccionado o en la posición del cursor
    /// </summary>
    private void InsertFormatting(string startTag, string endTag, string defaultText)
    {
        string currentText = ContentEditor.Text ?? "";
        int cursorPosition = ContentEditor.CursorPosition;

        // Por ahora, insertar en la posición del cursor
        // (MAUI no tiene soporte nativo para selección de texto en Editor)
        string textToInsert = startTag + defaultText + endTag;

        if (cursorPosition >= 0 && cursorPosition <= currentText.Length)
        {
            ContentEditor.Text = currentText.Insert(cursorPosition, textToInsert);
            // Posicionar el cursor después del texto insertado
            ContentEditor.CursorPosition = cursorPosition + startTag.Length + defaultText.Length;
        }
        else
        {
            ContentEditor.Text = currentText + textToInsert;
        }

        ContentEditor.Focus();
    }

    /// <summary>
    /// Inserta texto en la posición del cursor
    /// </summary>
    private void InsertAtCursor(string text, string endText = "")
    {
        string currentText = ContentEditor.Text ?? "";
        int cursorPosition = ContentEditor.CursorPosition;

        if (cursorPosition >= 0 && cursorPosition <= currentText.Length)
        {
            ContentEditor.Text = currentText.Insert(cursorPosition, text + endText);
            ContentEditor.CursorPosition = cursorPosition + text.Length;
        }
        else
        {
            ContentEditor.Text = currentText + text + endText;
        }

        ContentEditor.Focus();
    }

    #endregion

    /// <summary>
    /// Activa/desactiva la vista previa
    /// </summary>
    private void OnPreviewToggled(object sender, ToggledEventArgs e)
    {
        PreviewFrame.IsVisible = e.Value;
        if (e.Value)
        {
            UpdatePreview();
        }
    }

    /// <summary>
    /// Actualiza la vista previa con formato HTML básico
    /// </summary>
    private void UpdatePreview()
    {
        string content = ContentEditor.Text ?? "";

        // Convertir formato básico a HTML para la vista previa
        string htmlContent = content
            .Replace("***", "<strong><em>")
            .Replace("***", "</em></strong>")
            .Replace("**", "<strong>")
            .Replace("**", "</strong>")
            .Replace("*", "<em>")
            .Replace("*", "</em>")
            .Replace("_«", "<em>«")
            .Replace("»_", "»</em>")
            .Replace("\n\n", "<br/><br/>")
            .Replace("\n", "<br/>")
            .Replace("---", "<hr/>")
            .Replace("…", "…");

        PreviewLabel.Text = content; // MAUI no soporta HTML en Label por defecto
        // En una implementación real, podrías usar un WebView para mostrar HTML
    }

    /// <summary>
    /// Auto-guarda el capítulo como borrador
    /// </summary>
    private async Task AutoSave()
    {
        if (_hasUnsavedChanges)
        {
            await Device.InvokeOnMainThreadAsync(async () =>
            {
                await SaveChapter(false); // Guardar sin mostrar mensaje
            });
        }
    }

    /// <summary>
    /// Guarda el capítulo como borrador
    /// </summary>
    private async void OnSaveDraftClicked(object sender, EventArgs e)
    {
        await SaveChapter(true);
    }

    /// <summary>
    /// Actualiza el capítulo
    /// </summary>
    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        // Validar datos
        if (string.IsNullOrWhiteSpace(ChapterTitleEntry.Text))
        {
            await DisplayAlert("Error", "El título del capítulo es obligatorio", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ContentEditor.Text))
        {
            await DisplayAlert("Error", "El contenido del capítulo no puede estar vacío", "OK");
            return;
        }

        // Confirmar actualización
        bool confirm = await DisplayAlert("Confirmar",
            "¿Estás seguro de actualizar este capítulo?\nEsta acción no se puede deshacer.",
            "Actualizar", "Cancelar");

        if (confirm)
        {
            await SaveChapter(true);
            await Navigation.PopAsync();
        }
    }

    /// <summary>
    /// Guarda los cambios del capítulo
    /// </summary>
    private async Task SaveChapter(bool showMessage)
    {
        try
        {
            LoadingIndicator.IsVisible = true;

            // Actualizar el capítulo
            bool success = await _chapterService.UpdateChapterAsync(
                _chapterId,
                ChapterTitleEntry.Text.Trim(),
                ContentEditor.Text
            );

            if (success)
            {
                _hasUnsavedChanges = false;
                _lastSavedTime = DateTime.Now;
                UpdateLastSavedLabel();

                if (showMessage)
                {
                    await DisplayAlert("Éxito", "Capítulo guardado correctamente", "OK");
                }
            }
            else if (showMessage)
            {
                await DisplayAlert("Error", "No se pudo guardar el capítulo", "OK");
            }
        }
        catch (Exception ex)
        {
            if (showMessage)
            {
                await DisplayAlert("Error", $"Error al guardar: {ex.Message}", "OK");
            }
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
        }
    }

    /// <summary>
    /// Cancela la edición
    /// </summary>
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        if (_hasUnsavedChanges)
        {
            bool confirm = await DisplayAlert("Cambios sin guardar",
                "Tienes cambios sin guardar. ¿Deseas salir sin guardar?",
                "Salir", "Cancelar");

            if (!confirm)
                return;
        }

        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Dispose();
        await Navigation.PopAsync();
    }

    /// <summary>
    /// Maneja el botón de retroceso del dispositivo
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (_hasUnsavedChanges)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                bool confirm = await DisplayAlert("Cambios sin guardar",
                    "Tienes cambios sin guardar. ¿Deseas salir sin guardar?",
                    "Salir", "Cancelar");

                if (confirm)
                {
                    _autoSaveTimer?.Stop();
                    _autoSaveTimer?.Dispose();
                    await Navigation.PopAsync();
                }
            });
            return true; // Prevenir el retroceso automático
        }

        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Dispose();
        return base.OnBackButtonPressed();
    }

    /// <summary>
    /// Limpia recursos cuando la página se destruye
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Dispose();
    }
}