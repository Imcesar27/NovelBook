using NovelBook.Models;
using NovelBook.Services;
using System.Text;
using Microsoft.Maui.Controls;

namespace NovelBook.Views
{
    public partial class AddChapterPage : ContentPage
    {
        // Servicios
        private readonly NovelService _novelService;
        private readonly ImageService _imageService;
        private readonly DatabaseService _databaseService;

        // Datos
        private int _novelId;
        private Novel _novel;

        // Control de estado
        private bool _hasUnsavedChanges = false;
        private System.Timers.Timer _autoSaveTimer;
        private DateTime _lastSavedTime;

        public AddChapterPage(int novelId)
        {
            InitializeComponent();

            _novelId = novelId;

            // Inicializar servicios
            _databaseService = new DatabaseService();
            _novelService = new NovelService(_databaseService);
            _imageService = new ImageService(_databaseService);

            // Configurar auto-guardado cada 30 segundos para borradores
            _autoSaveTimer = new System.Timers.Timer(30000); // 30 segundos
            _autoSaveTimer.Elapsed += async (s, e) => await AutoSaveDraft();

            // Cargar información de la novela
            LoadNovelInfo();

            // Configurar eventos
            ContentEditor.TextChanged += OnContentTextChanged;
            ChapterTitleEntry.TextChanged += (s, e) => {
                _hasUnsavedChanges = true;
                UpdateAutoSaveLabel();
            };
            ChapterNumberEntry.TextChanged += (s, e) => {
                _hasUnsavedChanges = true;
                UpdateAutoSaveLabel();
            };
        }

        /// <summary>
        /// Carga la información de la novela
        /// </summary>
        private async void LoadNovelInfo()
        {
            try
            {
                LoadingIndicator.IsVisible = true;

                // Recargar la información de la novela para obtener el conteo actualizado
                _novel = await _novelService.GetNovelByIdAsync(_novelId);
                if (_novel != null)
                {
                    NovelTitle.Text = _novel.Title;
                    NovelCover.Source = await _imageService.GetCoverImageAsync(_novel.CoverImage);

                    // Sugerir número de capítulo siguiente basado en el conteo actual
                    int nextChapterNumber = _novel.ChapterCount + 1;
                    ChapterNumberEntry.Text = nextChapterNumber.ToString();
                    ChapterInfo.Text = $"Agregando capítulo {nextChapterNumber}";

                    // Solo cargar borrador si el número de capítulo coincide
                    LoadDraftIfValid(nextChapterNumber);
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo cargar la información de la novela", "OK");
                    await Navigation.PopAsync();
                }

                // Iniciar timer de auto-guardado
                _autoSaveTimer.Start();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar: {ex.Message}", "OK");
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
            UpdateAutoSaveLabel();

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
        /// Actualiza la etiqueta de auto-guardado con colores según el estado
        /// </summary>
        private void UpdateAutoSaveLabel()
        {
            if (_hasUnsavedChanges)
            {
                AutoSaveLabel.Text = "Cambios sin guardar";
                AutoSaveLabel.TextColor = Color.FromArgb("#FF6B6B"); // Rojo
            }
            else if (_lastSavedTime != default)
            {
                var timeSinceLastSave = DateTime.Now - _lastSavedTime;
                if (timeSinceLastSave.TotalMinutes < 1)
                {
                    AutoSaveLabel.Text = "Guardado hace momentos";
                }
                else if (timeSinceLastSave.TotalMinutes < 60)
                {
                    AutoSaveLabel.Text = $"Guardado hace {(int)timeSinceLastSave.TotalMinutes} min";
                }
                else
                {
                    AutoSaveLabel.Text = $"Guardado a las {_lastSavedTime:HH:mm}";
                }

                AutoSaveLabel.TextColor = Color.FromArgb("#4CAF50"); // Verde
            }
            else
            {
                AutoSaveLabel.Text = "Borrador automático";
                AutoSaveLabel.TextColor = Color.FromArgb("#B0B0B0"); // Gris
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

            string textToInsert = startTag + defaultText + endTag;

            if (cursorPosition >= 0 && cursorPosition <= currentText.Length)
            {
                ContentEditor.Text = currentText.Insert(cursorPosition, textToInsert);
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
        /// Actualiza la vista previa con formato básico
        /// </summary>
        private void UpdatePreview()
        {
            string content = ContentEditor.Text ?? "";

            // Por ahora, mostrar el texto tal cual
            // En una implementación completa, podrías convertir a HTML
            PreviewLabel.Text = content;
        }

        /// <summary>
        /// Auto-guarda el borrador
        /// </summary>
        private async Task AutoSaveDraft()
        {
            if (_hasUnsavedChanges)
            {
                await Device.InvokeOnMainThreadAsync(() =>
                {
                    SaveDraftLocal();
                    _hasUnsavedChanges = false;
                    _lastSavedTime = DateTime.Now;
                    UpdateAutoSaveLabel();
                });
            }
        }

        /// <summary>
        /// Guarda el borrador localmente
        /// </summary>
        private void SaveDraftLocal()
        {
            // Solo guardar si hay contenido
            if (!string.IsNullOrWhiteSpace(ContentEditor.Text) ||
                !string.IsNullOrWhiteSpace(ChapterTitleEntry.Text))
            {
                // Guardar en preferencias locales
                Preferences.Set($"draft_novel_{_novelId}_number", ChapterNumberEntry.Text);
                Preferences.Set($"draft_novel_{_novelId}_title", ChapterTitleEntry.Text ?? "");
                Preferences.Set($"draft_novel_{_novelId}_content", ContentEditor.Text ?? "");
                Preferences.Set($"draft_novel_{_novelId}_date", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            }
        }

        /// <summary>
        /// Carga el borrador si existe y es válido para el capítulo actual
        /// </summary>
        private void LoadDraftIfValid(int expectedChapterNumber)
        {
            if (Preferences.ContainsKey($"draft_novel_{_novelId}_content"))
            {
                // Verificar si el borrador es para el capítulo correcto
                string draftChapterNumber = Preferences.Get($"draft_novel_{_novelId}_number", "0");

                if (int.TryParse(draftChapterNumber, out int savedChapterNumber) &&
                    savedChapterNumber == expectedChapterNumber)
                {
                    // El borrador es para este capítulo, cargarlo
                    ChapterNumberEntry.Text = draftChapterNumber;
                    ChapterTitleEntry.Text = Preferences.Get($"draft_novel_{_novelId}_title", "");
                    ContentEditor.Text = Preferences.Get($"draft_novel_{_novelId}_content", "");

                    string draftDate = Preferences.Get($"draft_novel_{_novelId}_date", "");
                    if (!string.IsNullOrEmpty(draftDate))
                    {
                        AutoSaveLabel.Text = $"Borrador cargado ({draftDate})";
                        AutoSaveLabel.TextColor = Color.FromArgb("#4CAF50"); // Verde
                    }
                }
                else
                {
                    // El borrador es de un capítulo anterior, limpiarlo
                    ClearDraft();
                    AutoSaveLabel.Text = "Nuevo capítulo";
                    AutoSaveLabel.TextColor = Color.FromArgb("#B0B0B0"); // Gris
                }
            }
        }

        /// <summary>
        /// Limpia el borrador guardado
        /// </summary>
        private void ClearDraft()
        {
            Preferences.Remove($"draft_novel_{_novelId}_number");
            Preferences.Remove($"draft_novel_{_novelId}_title");
            Preferences.Remove($"draft_novel_{_novelId}_content");
            Preferences.Remove($"draft_novel_{_novelId}_date");
        }

        /// <summary>
        /// Guarda el capítulo como borrador
        /// </summary>
        private async void OnSaveDraftClicked(object sender, EventArgs e)
        {
            SaveDraftLocal();
            _hasUnsavedChanges = false;
            _lastSavedTime = DateTime.Now;
            UpdateAutoSaveLabel();
            await DisplayAlert("Éxito", "Borrador guardado correctamente", "OK");
        }

        /// <summary>
        /// Publica el capítulo
        /// </summary>
        private async void OnPublishClicked(object sender, EventArgs e)
        {
            // Validar datos
            if (!int.TryParse(ChapterNumberEntry.Text, out int chapterNumber))
            {
                await DisplayAlert("Error", "El número de capítulo debe ser válido", "OK");
                return;
            }

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

            // Confirmar publicación
            bool confirm = await DisplayAlert("Confirmar",
                "¿Estás seguro de publicar este capítulo?\nUna vez publicado no podrás cambiar el número de capítulo.",
                "Publicar", "Cancelar");

            if (!confirm) return;

            try
            {
                LoadingIndicator.IsVisible = true;
                PublishButton.IsEnabled = false;

                // Agregar el capítulo
                bool success = await _novelService.AddChapterAsync(
                    _novelId,
                    chapterNumber,
                    ChapterTitleEntry.Text.Trim(),
                    ContentEditor.Text
                );

                if (success)
                {
                    // Limpiar borrador
                    ClearDraft();

                    await DisplayAlert("Éxito", "Capítulo publicado correctamente", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error",
                        "No se pudo publicar el capítulo. Es posible que el número de capítulo ya exista.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al publicar: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                PublishButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Cancela la creación del capítulo
        /// </summary>
        private async void OnCancelClicked(object sender, EventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                bool confirm = await DisplayAlert("Cambios sin guardar",
                    "Tienes cambios sin guardar. ¿Deseas guardar como borrador antes de salir?",
                    "Guardar borrador", "Salir sin guardar");

                if (confirm)
                {
                    SaveDraftLocal();
                }
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
                        "Tienes cambios sin guardar. ¿Deseas guardar como borrador antes de salir?",
                        "Guardar borrador", "Salir sin guardar");

                    if (confirm)
                    {
                        SaveDraftLocal();
                    }

                    _autoSaveTimer?.Stop();
                    _autoSaveTimer?.Dispose();
                    await Navigation.PopAsync();
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

            // Guardar borrador si hay cambios
            if (_hasUnsavedChanges)
            {
                SaveDraftLocal();
            }

            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();
        }
    }
}