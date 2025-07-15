using Microsoft.Maui.Controls;
using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class ReaderPage : ContentPage
{
    // Servicios
    private readonly ChapterService _chapterService;
    private readonly DatabaseService _databaseService;

    // Variables de estado
    private bool _barsVisible = true;
    private double _scrollProgress = 0;

    // Datos del capítulo actual
    private int _novelId;
    private int _chapterId;
    private Chapter _currentChapter;
    private List<Chapter> _allChapters;
    private Novel _novel;

    // Progreso de lectura
    private double _lastScrollY = 0;
    private bool _isChapterCompleted = false;
    private bool _hasReachedEnd = false;



    /// <summary>
    /// Constructor para abrir un capítulo específico
    /// </summary>
    public ReaderPage(int novelId, int chapterId, string novelTitle = "")
    {
        InitializeComponent();

        _novelId = novelId;
        _chapterId = chapterId;
        _databaseService = new DatabaseService();
        _chapterService = new ChapterService(_databaseService);

        // Obtener el tema actual
        var currentTheme = Application.Current.RequestedTheme;

        // Configuración inicial del ViewModel
        BindingContext = new ReaderViewModel
        {
            NovelTitle = novelTitle,
            BackgroundColor = currentTheme == AppTheme.Light ?
            Color.FromArgb("#FFFFFF") : Color.FromArgb("#1A1A1A"),
            TextColor = currentTheme == AppTheme.Light ?
            Color.FromArgb("#1A1A1A") : Colors.White,
            FontSize = 16,
            FontFamily = "OpenSansRegular",
            LineHeight = 1.5,
            TextAlignment = TextAlignment.Justify
        };

        // Cargar capítulo de forma asíncrona
        Task.Run(async () => await LoadChapterAsync());
    }

    /// <summary>
    /// Constructor para continuar lectura (último capítulo leído)
    /// </summary>
    public ReaderPage(int novelId) : this(novelId, 0)
    {
        // El chapterId 0 indica que debe cargar el último leído
    }


    private async void OnContentScrolled(object sender, ScrolledEventArgs e)
    {
        _lastScrollY = e.ScrollY;

        // Calcular progreso
        var scrollView = sender as ScrollView;
        if (scrollView != null && scrollView.ContentSize.Height > 0)
        {
            // Calcular el progreso real considerando el viewport
            var contentHeight = scrollView.ContentSize.Height;
            var viewportHeight = scrollView.Height;
            var maxScroll = contentHeight - viewportHeight;

            if (maxScroll > 0)
            {
                _scrollProgress = e.ScrollY / maxScroll;
                _scrollProgress = Math.Max(0, Math.Min(1, _scrollProgress));
            }
            else
            {
                _scrollProgress = 1; // Si el contenido es más pequeño que el viewport
            }

            // Actualizar UI
            Device.BeginInvokeOnMainThread(async () =>
            {
                ReadingProgress.Progress = _scrollProgress;
                var percentage = (int)(_scrollProgress * 100);
                ProgressLabel.Text = $" | {percentage}%";

                // Marcar como completado cuando llegue al 95% o más
                if (percentage >= 95 && !_hasReachedEnd)
                {
                    _hasReachedEnd = true;
                    System.Diagnostics.Debug.WriteLine($"Capítulo alcanzó el final: {percentage}%");

                    // Guardar como completado
                    await SaveProgress(true);
                }
            });
        }
    }

    /// <summary>
    /// Carga el contenido del capítulo
    /// </summary>
    private async Task LoadChapterAsync()  
    {
        try
        {
            // Resetear flags
            _isChapterCompleted = false;
            _hasReachedEnd = false;

            _isChapterCompleted = false; // Resetear estado

            // Si chapterId es 0, buscar el último capítulo leído
            if (_chapterId == 0 && AuthService.CurrentUser != null)
            {
                var lastChapterId = await _chapterService.GetLastReadChapterAsync(
                    AuthService.CurrentUser.Id, _novelId);

                _chapterId = lastChapterId ?? 1; // Si no hay último leído, empezar por el primero
            }

            // Cargar todos los capítulos para navegación
            _allChapters = await _chapterService.GetChaptersForNovelAsync(_novelId);

            // Cargar el capítulo actual
            _currentChapter = await _chapterService.GetChapterAsync(_chapterId);

            if (_currentChapter != null)
            {
                // Actualizar UI en el hilo principal
                Device.BeginInvokeOnMainThread(() =>
                {
                    var viewModel = BindingContext as ReaderViewModel;
                    viewModel.ChapterTitle = $"{LocalizationService.GetString("Chapter")} {_currentChapter.ChapterNumber}: {_currentChapter.Title}";
                    viewModel.ChapterText = ProcessChapterContent(_currentChapter.Content);
                    viewModel.CurrentPage = $"Cap. {_currentChapter.ChapterNumber}";

                    // Actualizar navegación
                    UpdateNavigationButtons();
                });

                // Cargar progreso guardado si existe
                await LoadReadingProgress();
            }
            else
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert(
                        LocalizationService.GetString("Error"),
                        LocalizationService.GetString("ErrorLoadingChapter"),
                        LocalizationService.GetString("OK"));
                    await Navigation.PopAsync();
                });
            }
        }
        catch (Exception ex)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", $"Error al cargar: {ex.Message}", "OK");
            });
        }
    }

    /// <summary>
    /// Procesa el contenido del capítulo (convierte HTML básico)
    /// </summary>
    private string ProcessChapterContent(string content)
    {
        // Por ahora, remover tags HTML básicos
        content = content.Replace("<br/>", "\n")
                        .Replace("<hr/>", "\n────────────\n")
                        .Replace("<strong>", "")
                        .Replace("</strong>", "")
                        .Replace("<em>", "")
                        .Replace("</em>", "")
                        .Replace("<u>", "")
                        .Replace("</u>", "");

        return content;
    }

    /// <summary>
    /// Carga el progreso de lectura guardado
    /// </summary>
    private async Task LoadReadingProgress()
    {
        if (AuthService.CurrentUser == null) return;

        var progress = await _chapterService.GetReadingProgressAsync(
            AuthService.CurrentUser.Id, _chapterId);

        if (progress != null)
        {
            ReadingProgress.Progress = (double)progress.Progress / 100;
            _scrollProgress = (double)progress.Progress / 100;

            // Restaurar posición de scroll
            if (progress.LastPosition > 0)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(500); // Esperar a que se cargue el contenido
                    await ContentScrollView.ScrollToAsync(0, progress.LastPosition, false);
                });
            }
        }
    }

    /// <summary>
    /// Actualiza el estado de los botones de navegación
    /// </summary>
    private void UpdateNavigationButtons()
    {
        var currentIndex = _allChapters.FindIndex(c => c.Id == _chapterId);

        // Deshabilitar botón anterior si es el primer capítulo
        var prevButton = this.FindByName<Button>("PreviousButton");
        if (prevButton != null)
        {
            prevButton.IsEnabled = currentIndex > 0;
            prevButton.Opacity = currentIndex > 0 ? 1 : 0.5;
        }

        // Deshabilitar botón siguiente si es el último capítulo
        var nextButton = this.FindByName<Button>("NextButton");
        if (nextButton != null)
        {
            nextButton.IsEnabled = currentIndex < _allChapters.Count - 1;
            nextButton.Opacity = currentIndex < _allChapters.Count - 1 ? 1 : 0.5;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        try
        {
            // Guardar progreso antes de salir (si estás en el reader)
            if (this is ReaderPage)
            {
                await SaveProgress(_scrollProgress >= 0.95);

                if (AuthService.CurrentUser != null)
                {
                    var chapterService = new ChapterService(_databaseService);
                    await chapterService.SyncUserLibraryProgressAsync(AuthService.CurrentUser.Id, _novelId);
                }
            }

            // Verificar si podemos hacer pop
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
            else
            {
                // Si no hay stack, volver al tab anterior
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en navegación back: {ex.Message}");

            // Último recurso - volver a la biblioteca
            try
            {
                await Shell.Current.GoToAsync("//LibraryPage");
            }
            catch
            {
                // Si todo falla, solo cerrar la página actual
                if (Navigation.NavigationStack.Count > 1)
                {
                    Navigation.RemovePage(this);
                }
            }
        }
    }

    private void OnMenuClicked(object sender, EventArgs e)
    {
        SettingsPanel.IsVisible = !SettingsPanel.IsVisible;
    }

    private void OnContentTapped(object sender, EventArgs e)
    {
        _barsVisible = !_barsVisible;
        HeaderBar.IsVisible = _barsVisible;
        BottomBar.IsVisible = _barsVisible;
    }

    private async void OnPreviousChapterClicked(object sender, EventArgs e)
    {
        var currentIndex = _allChapters.FindIndex(c => c.Id == _chapterId);
        if (currentIndex > 0)
        {
            await SaveProgress();
            _chapterId = _allChapters[currentIndex - 1].Id;
            await LoadChapterAsync();
        }
    }

    private async void OnNextChapterClicked(object sender, EventArgs e)
    {
        var currentIndex = _allChapters.FindIndex(c => c.Id == _chapterId);
        if (currentIndex < _allChapters.Count - 1)
        {
            await SaveProgress(true); // Marcar capítulo actual como completado
            _chapterId = _allChapters[currentIndex + 1].Id;
            await LoadChapterAsync();
        }
        else
        {
            await SaveProgress(true);
            await DisplayAlert(
            LocalizationService.GetString("End"),
            LocalizationService.GetString("EndOfChapter"),
            LocalizationService.GetString("OK"));
        }
    }

    /// <summary>
    /// Guarda el progreso de lectura
    /// </summary>
    private async Task SaveProgress(bool isCompleted = false)
    {
        if (AuthService.CurrentUser == null) return;

        try
        {
            var progress = isCompleted ? 100 : _scrollProgress * 100;
            var scrollPosition = (int)_lastScrollY;

            await _chapterService.SaveReadingProgressAsync(
                AuthService.CurrentUser.Id,
                _chapterId,
                (decimal)progress,
                scrollPosition,
                isCompleted
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando progreso: {ex.Message}");
        }
    }

    // Métodos de configuración de lectura (igual que antes)
    private void OnFontSizeChanged(object sender, ValueChangedEventArgs e)
    {
        if (BindingContext is ReaderViewModel vm)
        {
            vm.FontSize = e.NewValue;
        }
    }

    private void OnLineHeightChanged(object sender, ValueChangedEventArgs e)
    {
        if (BindingContext is ReaderViewModel vm)
        {
            vm.LineHeight = e.NewValue;
        }
    }

    private void OnColorSelected(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.GestureRecognizers[0] is TapGestureRecognizer tap)
        {
            var colorHex = tap.CommandParameter as string;
            if (BindingContext is ReaderViewModel vm)
            {
                // Si selecciona blanco o negro, debe respetar el tema actual
                if (colorHex == "#FFFFFF" && Application.Current.RequestedTheme == AppTheme.Dark)
                {
                    // En modo oscuro, no permitir fondo blanco puro
                    vm.BackgroundColor = Color.FromArgb("#F5F5F5");
                    vm.TextColor = Color.FromArgb("#1A1A1A");
                }
                else if (colorHex == "#1A1A1A" && Application.Current.RequestedTheme == AppTheme.Light)
                {
                    // En modo claro, no permitir fondo negro puro
                    vm.BackgroundColor = Color.FromArgb("#2D2D2D");
                    vm.TextColor = Colors.White;
                }
                else
                {
                    vm.BackgroundColor = Color.FromArgb(colorHex);

                    // Ajustar color de texto según el fondo
                    vm.TextColor = colorHex == "#1A1A1A" ? Colors.White :
                                  colorHex == "#FFFFFF" ? Colors.Black :
                                  colorHex == "#F5E6D3" ? Color.FromArgb("#333333") :
                                  Color.FromArgb("#2E7D32");
                }
            }
        }
    }

    private void OnAlignmentChanged(object sender, EventArgs e)
    {
        if (sender is Button btn && BindingContext is ReaderViewModel vm)
        {
            vm.TextAlignment = btn.CommandParameter switch
            {
                "Start" => TextAlignment.Start,
                "End" => TextAlignment.End,
                _ => TextAlignment.Justify
            };

            // Actualizar visual de botones
            foreach (var child in (btn.Parent as HorizontalStackLayout).Children)
            {
                if (child is Button b)
                {
                    b.BackgroundColor = b == btn ?
                        Color.FromArgb("#8B5CF6") :
                        Color.FromArgb("#2D2D2D");
                    b.TextColor = b == btn ? Colors.White :
                        Color.FromArgb("#FFFFFF");
                }
            }
        }
    }

    private void OnFontChanged(object sender, EventArgs e)
    {
        if (sender is Button btn && BindingContext is ReaderViewModel vm)
        {
            vm.FontFamily = btn.CommandParameter as string;

            // Actualizar visual de botones
            foreach (var child in (btn.Parent as HorizontalStackLayout).Children)
            {
                if (child is Button b)
                {
                    b.BackgroundColor = b == btn ?
                        Color.FromArgb("#8B5CF6") :
                        Color.FromArgb("#2D2D2D");
                }
            }
        }
    }

    private void OnCloseSettingsClicked(object sender, EventArgs e)
    {
        SettingsPanel.IsVisible = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetNavBarIsVisible(this, false);

        // Actualizar colores si el tema cambió mientras estaba en otra página
        UpdateThemeColors();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Guardar progreso al salir
        Task.Run(async () =>
        {
            // Si el progreso es mayor al 95%, marcar como completado
            bool shouldComplete = _scrollProgress >= 0.95;
            await SaveProgress(shouldComplete);

            if (shouldComplete)
            {
                System.Diagnostics.Debug.WriteLine("Marcando capítulo como completado al salir");
            }
        });
    }

    private void UpdateThemeColors()
    {
        if (BindingContext is ReaderViewModel vm)
        {
            var currentTheme = Application.Current.RequestedTheme;

            // Solo actualizar si no se ha personalizado manualmente
            if (vm.BackgroundColor == Color.FromArgb("#1A1A1A") ||
                vm.BackgroundColor == Color.FromArgb("#FFFFFF"))
            {
                vm.BackgroundColor = currentTheme == AppTheme.Light ?
                    Color.FromArgb("#FFFFFF") : Color.FromArgb("#1A1A1A");
                vm.TextColor = currentTheme == AppTheme.Light ?
                    Color.FromArgb("#1A1A1A") : Colors.White;
            }
        }
    }

    /// <summary>
    /// ViewModel para el lector de novelas
    /// </summary>
    public class ReaderViewModel : BindableObject
    {
        private string _novelTitle;
        private string _chapterTitle;
        private string _currentPage;
        private string _chapterText;
        private Color _backgroundColor;
        private Color _textColor;
        private double _fontSize;
        private string _fontFamily;
        private double _lineHeight;
        private TextAlignment _textAlignment;

        public string NovelTitle
        {
            get => _novelTitle;
            set { _novelTitle = value; OnPropertyChanged(); }
        }

        public string ChapterTitle
        {
            get => _chapterTitle;
            set { _chapterTitle = value; OnPropertyChanged(); }
        }

        public string CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public string ChapterText
        {
            get => _chapterText;
            set { _chapterText = value; OnPropertyChanged(); }
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set { _backgroundColor = value; OnPropertyChanged(); }
        }

        public Color TextColor
        {
            get => _textColor;
            set { _textColor = value; OnPropertyChanged(); }
        }

        public double FontSize
        {
            get => _fontSize;
            set { _fontSize = value; OnPropertyChanged(); }
        }

        public string FontFamily
        {
            get => _fontFamily;
            set { _fontFamily = value; OnPropertyChanged(); }
        }

        public double LineHeight
        {
            get => _lineHeight;
            set { _lineHeight = value; OnPropertyChanged(); }
        }

        public TextAlignment TextAlignment
        {
            get => _textAlignment;
            set { _textAlignment = value; OnPropertyChanged(); }
        }
    }


}