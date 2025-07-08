using NovelBook.Services;
using Microsoft.Maui.Controls;

namespace NovelBook.Views;

/// <summary>
/// Página para gestionar las descargas de novelas offline
/// Permite ver, ordenar y eliminar las novelas descargadas
/// </summary>
public partial class DownloadsPage : ContentPage
{
    private readonly DownloadService _downloadService;
    private readonly ImageService _imageService;
    private readonly DatabaseService _databaseService;

    // Lista completa de descargas
    private List<DownloadedNovel> _allDownloads = new List<DownloadedNovel>();

    // Estado de selección
    private bool _isSelectionMode = false;
    private List<int> _selectedNovelIds = new List<int>();

    // Ordenamiento actual
    private string _currentSort = "recent";

    public DownloadsPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _downloadService = new DownloadService(_databaseService);
        _imageService = new ImageService(_databaseService);
    }

    /// <summary>
    /// Se ejecuta cuando la página aparece
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Solo cargar si hay usuario logueado
        if (AuthService.CurrentUser != null)
        {
            await LoadDownloadsAsync();
            await UpdateStorageInfo();
        }
        else
        {
            ShowEmptyState();
        }
    }

    #region Carga de Datos

    /// <summary>
    /// Carga la lista de descargas
    /// </summary>
    private async Task LoadDownloadsAsync()
    {
        try
        {
            // Mostrar indicador de carga
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            // Obtener descargas
            _allDownloads = await _downloadService.GetDownloadedNovelsAsync();

            if (_allDownloads.Any())
            {
                EmptyState.IsVisible = false;
                DownloadsList.IsVisible = true;

                // Aplicar ordenamiento actual
                ApplySort();

                // Crear vista de datos
                var downloadsView = new List<dynamic>();

                foreach (var novel in _allDownloads)
                {
                    var coverSource = string.IsNullOrEmpty(novel.CoverImage) ?
                        "novel_placeholder.jpg" :
                        novel.CoverImage.StartsWith("db://") ?
                            "novel_placeholder.jpg" :
                            novel.CoverImage;

                    downloadsView.Add(new
                    {
                        novel.Id,
                        novel.Title,
                        novel.Author,
                        CoverSource = coverSource,
                        ChaptersText = $"{novel.DownloadedChapters}/{novel.TotalChapters} capítulos",
                        SizeText = novel.FormattedSize,
                        ProgressValue = novel.ProgressPercentage,
                        IsComplete = novel.IsComplete,
                        IsSelected = _selectedNovelIds.Contains(novel.Id),
                        SelectionOpacity = _isSelectionMode ? 1.0 : 0.0
                    });
                }

                DownloadsList.ItemsSource = downloadsView;
            }
            else
            {
                ShowEmptyState();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar descargas: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Actualiza la información de almacenamiento
    /// </summary>
    private async Task UpdateStorageInfo()
    {
        try
        {
            var storageInfo = await _downloadService.GetStorageInfoAsync();

            // Actualizar UI
            StorageProgressBar.Progress = storageInfo.UsedPercentage;

            StorageLabel.FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span
                    {
                        Text = storageInfo.FormattedUsedSpace,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Span
                    {
                        Text = $" de {storageInfo.FormattedMaxSpace} usado",
                        TextColor = (Color)Application.Current.Resources["TextSecondary"]
                    }
                }
            };

            DownloadCountLabel.Text = $"{storageInfo.NovelCount} novelas descargadas";

            // Cambiar color de la barra según uso
            if (storageInfo.UsedPercentage > 0.9)
            {
                StorageProgressBar.ProgressColor = (Color)Application.Current.Resources["Error"];
            }
            else if (storageInfo.UsedPercentage > 0.7)
            {
                StorageProgressBar.ProgressColor = Colors.Orange;
            }
            else
            {
                StorageProgressBar.ProgressColor = (Color)Application.Current.Resources["Primary"];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando info de almacenamiento: {ex.Message}");
        }
    }

    #endregion

    #region Eventos de UI

    /// <summary>
    /// Maneja el tap en una novela descargada
    /// </summary>
    private async void OnNovelTapped(object sender, EventArgs e)
    {
        if (_isSelectionMode)
        {
            // En modo selección, toggle la selección
            var frame = sender as Frame;
            var novel = frame?.BindingContext as dynamic;
            if (novel != null)
            {
                ToggleSelection(novel.Id);
            }
        }
        else
        {
            // En modo normal, abrir la novela
            var frame = sender as Frame;
            var novel = frame?.BindingContext as dynamic;
            if (novel != null)
            {
                await Shell.Current.GoToAsync($"NovelDetailPage?novelId={novel.Id}");
            }
        }
    }

    /// <summary>
    /// Activa el modo de selección
    /// </summary>
    private void OnSelectClicked(object sender, EventArgs e)
    {
        _isSelectionMode = true;
        _selectedNovelIds.Clear();

        // Mostrar barra de selección
        SelectionBar.IsVisible = true;
        SelectButton.Text = "Cancelar";
        SelectButton.Clicked -= OnSelectClicked;
        SelectButton.Clicked += OnCancelSelectionClicked;

        // Actualizar vista
        UpdateSelectionUI();
    }

    /// <summary>
    /// Cancela el modo de selección
    /// </summary>
    private void OnCancelSelectionClicked(object sender, EventArgs e)
    {
        ExitSelectionMode();
    }

    /// <summary>
    /// Muestra el menú de ordenamiento
    /// </summary>
    private async void OnSortClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            "Ordenar por",
            "Cancelar",
            null,
            "Más reciente",
            "Más antiguo",
            "Nombre (A-Z)",
            "Nombre (Z-A)",
            "Tamaño (Mayor)",
            "Tamaño (Menor)",
            "Progreso");

        switch (action)
        {
            case "Más reciente":
                _currentSort = "recent";
                break;
            case "Más antiguo":
                _currentSort = "oldest";
                break;
            case "Nombre (A-Z)":
                _currentSort = "name_asc";
                break;
            case "Nombre (Z-A)":
                _currentSort = "name_desc";
                break;
            case "Tamaño (Mayor)":
                _currentSort = "size_desc";
                break;
            case "Tamaño (Menor)":
                _currentSort = "size_asc";
                break;
            case "Progreso":
                _currentSort = "progress";
                break;
        }

        if (!string.IsNullOrEmpty(action) && action != "Cancelar")
        {
            await LoadDownloadsAsync();
        }
    }

    /// <summary>
    /// Elimina las descargas seleccionadas
    /// </summary>
    private async void OnDeleteSelectedClicked(object sender, EventArgs e)
    {
        if (!_selectedNovelIds.Any())
        {
            await DisplayAlert("Información", "No hay novelas seleccionadas", "OK");
            return;
        }

        var confirm = await DisplayAlert(
            "Confirmar eliminación",
            $"¿Eliminar {_selectedNovelIds.Count} novela(s) descargada(s)?",
            "Eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                // Mostrar indicador de progreso
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                int deleted = 0;
                foreach (var novelId in _selectedNovelIds)
                {
                    if (await _downloadService.DeleteNovelDownloadsAsync(novelId))
                    {
                        deleted++;
                    }
                }

                // Salir del modo selección
                ExitSelectionMode();

                // Recargar lista
                await LoadDownloadsAsync();
                await UpdateStorageInfo();

                // Mostrar resultado
                await DisplayAlert(
                    "Completado",
                    $"{deleted} novela(s) eliminada(s) correctamente",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }
    }

    /// <summary>
    /// Muestra el menú de opciones para una novela
    /// </summary>
    private async void OnMoreOptionsClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var novel = button?.BindingContext as dynamic;
        if (novel == null) return;

        var action = await DisplayActionSheet(
            novel.Title,
            "Cancelar",
            "Eliminar descarga",
            "Ver detalles",
            novel.IsComplete ? null : "Descargar capítulos faltantes");

        switch (action)
        {
            case "Ver detalles":
                await Shell.Current.GoToAsync($"NovelDetailPage?novelId={novel.Id}");
                break;

            case "Eliminar descarga":
                var confirm = await DisplayAlert(
                    "Confirmar",
                    $"¿Eliminar la descarga de '{novel.Title}'?",
                    "Eliminar",
                    "Cancelar");

                if (confirm)
                {
                    if (await _downloadService.DeleteNovelDownloadsAsync(novel.Id))
                    {
                        await LoadDownloadsAsync();
                        await UpdateStorageInfo();
                    }
                }
                break;

            case "Descargar capítulos faltantes":
                await DownloadMissingChapters(novel.Id);
                break;
        }
    }

    #endregion

    #region Métodos de Ayuda

    /// <summary>
    /// Aplica el ordenamiento actual a la lista
    /// </summary>
    private void ApplySort()
    {
        switch (_currentSort)
        {
            case "recent":
                _allDownloads = _allDownloads.OrderByDescending(n => n.LastDownload).ToList();
                break;
            case "oldest":
                _allDownloads = _allDownloads.OrderBy(n => n.LastDownload).ToList();
                break;
            case "name_asc":
                _allDownloads = _allDownloads.OrderBy(n => n.Title).ToList();
                break;
            case "name_desc":
                _allDownloads = _allDownloads.OrderByDescending(n => n.Title).ToList();
                break;
            case "size_desc":
                _allDownloads = _allDownloads.OrderByDescending(n => n.TotalSize).ToList();
                break;
            case "size_asc":
                _allDownloads = _allDownloads.OrderBy(n => n.TotalSize).ToList();
                break;
            case "progress":
                _allDownloads = _allDownloads.OrderBy(n => n.ProgressPercentage).ToList();
                break;
        }
    }

    /// <summary>
    /// Alterna la selección de una novela
    /// </summary>
    private void ToggleSelection(int novelId)
    {
        if (_selectedNovelIds.Contains(novelId))
        {
            _selectedNovelIds.Remove(novelId);
        }
        else
        {
            _selectedNovelIds.Add(novelId);
        }

        UpdateSelectionUI();
    }

    /// <summary>
    /// Actualiza la UI de selección
    /// </summary>
    private async void UpdateSelectionUI()
    {
        SelectionLabel.Text = $"{_selectedNovelIds.Count} seleccionados";

        // Recargar la lista para actualizar los checkboxes
        await LoadDownloadsAsync();
    }

    /// <summary>
    /// Sale del modo de selección
    /// </summary>
    private void ExitSelectionMode()
    {
        _isSelectionMode = false;
        _selectedNovelIds.Clear();

        // Ocultar barra de selección
        SelectionBar.IsVisible = false;
        SelectButton.Text = "Seleccionar";
        SelectButton.Clicked -= OnCancelSelectionClicked;
        SelectButton.Clicked += OnSelectClicked;

        // Actualizar vista
        _ = LoadDownloadsAsync();
    }

    /// <summary>
    /// Muestra el estado vacío
    /// </summary>
    private void ShowEmptyState()
    {
        DownloadsList.IsVisible = false;
        EmptyState.IsVisible = true;
    }

    /// <summary>
    /// Descarga los capítulos faltantes de una novela
    /// </summary>
    private async Task DownloadMissingChapters(int novelId)
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var result = await _downloadService.DownloadNovelAsync(novelId);

            await DisplayAlert(
                "Descarga completada",
                $"Descargados: {result.downloaded} capítulos\nFallidos: {result.failed}",
                "OK");

            // Recargar lista
            await LoadDownloadsAsync();
            await UpdateStorageInfo();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al descargar: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Navega a la página de exploración
    /// </summary>
    private async void OnExploreClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ExploreTab");
    }

    #endregion
}