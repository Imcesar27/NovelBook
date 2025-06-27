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

        // Configuración inicial del ViewModel
        BindingContext = new ReaderViewModel
        {
            NovelTitle = novelTitle,
            BackgroundColor = Color.FromArgb("#1A1A1A"),
            TextColor = Colors.White,
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


    private void OnContentScrolled(object sender, ScrolledEventArgs e)
    {
        _lastScrollY = e.ScrollY;

        // Calcular progreso
        var scrollView = sender as ScrollView;
        if (scrollView != null && scrollView.ContentSize.Height > 0)
        {
            _scrollProgress = e.ScrollY / (scrollView.ContentSize.Height - scrollView.Height);
            _scrollProgress = Math.Max(0, Math.Min(1, _scrollProgress)); // Clamp entre 0 y 1

            // Actualizar barra de progreso
            Device.BeginInvokeOnMainThread(() =>
            {
                ReadingProgress.Progress = _scrollProgress;
                var percentage = (int)(_scrollProgress * 100);
                ProgressLabel.Text = $" | {percentage}%";
            });
        }
    }

    /// <summary>
    /// Carga el contenido del capítulo
    /// </summary>
    private async Task LoadChapterAsync()  // Cambiar de void a Task
    {
        try
        {
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
                    viewModel.ChapterTitle = $"Capítulo {_currentChapter.ChapterNumber}: {_currentChapter.Title}";
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
                    await DisplayAlert("Error", "No se pudo cargar el capítulo", "OK");
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
            prevButton.Opacity = currentIndex > 0 ? 1.0 : 0.5;
        }

        // Deshabilitar botón siguiente si es el último capítulo
        var nextButton = this.FindByName<Button>("NextButton");
        if (nextButton != null)
        {
            nextButton.IsEnabled = currentIndex < _allChapters.Count - 1;
            nextButton.Opacity = currentIndex < _allChapters.Count - 1 ? 1.0 : 0.5;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // Guardar progreso antes de salir
        await SaveProgress();
        await Navigation.PopAsync();
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
            await DisplayAlert("Fin", "Has llegado al último capítulo disponible", "OK");
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
                vm.BackgroundColor = Color.FromArgb(colorHex);

                // Ajustar color de texto según el fondo
                vm.TextColor = colorHex == "#1A1A1A" ? Colors.White :
                              colorHex == "#FFFFFF" ? Colors.Black :
                              colorHex == "#F5E6D3" ? Color.FromArgb("#333333") :
                              Color.FromArgb("#2E7D32");
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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Guardar progreso al salir
        Task.Run(async () => await SaveProgress());
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