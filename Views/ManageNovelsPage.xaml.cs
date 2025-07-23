using NovelBook.Models;
using NovelBook.Services;
using System.Collections.ObjectModel;

namespace NovelBook.Views;

/// <summary>
/// Página para que los administradores gestionen las novelas (editar/eliminar)
/// </summary>
public partial class ManageNovelsPage : ContentPage
{
    // Servicios necesarios
    private readonly NovelService _novelService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Lista de novelas para mostrar
    private ObservableCollection<NovelDisplayItem> _novels;
    private List<Novel> _allNovels; // Lista completa para búsqueda

    public ManageNovelsPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);
        _imageService = new ImageService(_databaseService);

        // Inicializar colecciones
        _novels = new ObservableCollection<NovelDisplayItem>();
        _allNovels = new List<Novel>();

        // Configurar la vista
        NovelsCollection.ItemsSource = _novels;
    }

    /// <summary>
    /// Se ejecuta cuando aparece la página
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Deshabilitar navegación de Shell
        Shell.SetNavBarIsVisible(this, false);

        // Verificar que el usuario es administrador
        if (AuthService.CurrentUser == null || !AuthService.CurrentUser.IsAdmin)
        {
            await DisplayAlert(
                LocalizationService.GetString("AccessDenied"),
                LocalizationService.GetString("NoPermissions"),
                LocalizationService.GetString("OK"));
            await Navigation.PopAsync();
            return;
        }

        // Cargar las novelas
        await LoadNovelsAsync();
    }

    /// <summary>
    /// Carga todas las novelas de la base de datos
    /// </summary>
    private async Task LoadNovelsAsync()
    {
        try
        {
            // Mostrar indicador de carga
            LoadingIndicator.IsVisible = true;
            EmptyState.IsVisible = false;

            // Obtener todas las novelas
            _allNovels = await _novelService.GetAllNovelsAsync();

            // Actualizar estadísticas
            TotalNovelsLabel.Text = _allNovels.Count.ToString();
            ActiveNovelsLabel.Text = _allNovels.Count(n => n.Status != "paused").ToString();

            // Convertir a items de display
            await UpdateDisplayList(_allNovels);

            // Mostrar estado vacío si no hay novelas
            if (_allNovels.Count == 0)
            {
                EmptyState.IsVisible = true;
            }

           // Actualizar estadísticas
            UpdateStatistics();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationService.GetString("Error"),
                LocalizationService.GetString("ErrorLoadingNovels", ex.Message),
                LocalizationService.GetString("OK"));
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
        }
    }

    /// <summary>
    /// Actualiza la lista de novelas mostradas
    /// </summary>
    private async Task UpdateDisplayList(List<Novel> novels)
    {
        _novels.Clear();

        foreach (var novel in novels)
        {
            var displayItem = new NovelDisplayItem
            {
                Id = novel.Id,
                Title = novel.Title,
                Author = novel.Author ?? LocalizationService.GetString("NoAuthor"),
                CoverImageSource = await _imageService.GetCoverImageAsync(novel.CoverImage),
                ChapterCount = novel.ChapterCount,
                Status = novel.Status,
                StatusText = GetStatusText(novel.Status),
                StatusColor = GetStatusColor(novel.Status),
                UpdatedAtText = $"{LocalizationService.GetString("Updated")}{novel.UpdatedAt:dd/MM/yyyy}"
            };

            _novels.Add(displayItem);
        }
    }

    /// <summary>
    /// Obtiene el texto del estado en español
    /// </summary>
    private string GetStatusText(string status)
    {
        return status switch
        {
            "ongoing" => LocalizationService.GetString("OnGoing2"),
            "completed" => LocalizationService.GetString("CompletedStatus"),
            "hiatus" => LocalizationService.GetString("Hiatus2"),
            "cancelled" => LocalizationService.GetString("CancelledStatus"),
            _ => status
        };
    }

    /// <summary>
    /// Obtiene el color según el estado
    /// </summary>
    private Color GetStatusColor(string status)
    {
        return status switch
        {
            "ongoing" => Color.FromArgb("#F59E0B"),      // Naranja
            "completed" => Color.FromArgb("#10B981"),    // Verde
            "hiatus" => Color.FromArgb("#6B7280"),       // Gris
            "cancelled" => Color.FromArgb("#EF4444"),    // Rojo para cancelada
            _ => Color.FromArgb("#6B7280")
        };
    }

    /// <summary>
    /// Maneja la búsqueda de novelas
    /// </summary>
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Si no hay texto, mostrar todas
            await UpdateDisplayList(_allNovels);
        }
        else
        {
            // Filtrar por título o autor
            var filtered = _allNovels.Where(n =>
                n.Title.ToLower().Contains(searchText) ||
                (n.Author?.ToLower().Contains(searchText) ?? false)
            ).ToList();

            await UpdateDisplayList(filtered);
        }
    }

    /// <summary>
    /// Navega a la página de crear novela
    /// </summary>
    /// <summary>
    /// Navega a la página de crear novela
    /// </summary>
    private async void OnAddNovelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CreateNovelPage));
    }

    /// <summary>
    /// Navega a la página de gestión de géneros
    /// </summary>
    private async void OnManageGenresClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ManageGenresPage));
    }

    /// <summary>
    /// Navega a la página de editar novela
    /// </summary>
    private async void OnEditNovelClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int novelId)
        {
            // Se pasa el ID como parámetro de navegación
            await Shell.Current.GoToAsync($"{nameof(EditNovelPage)}?novelId={novelId}");
        }
    }


    /// <summary>
    /// Elimina una novela con confirmación
    /// </summary>
    private async void OnDeleteNovelClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int novelId)
        {
            // Buscar la novela
            var novel = _allNovels.FirstOrDefault(n => n.Id == novelId);
            if (novel == null) return;

            // Confirmar eliminación
            bool confirm = await DisplayAlert(
                    LocalizationService.GetString("ConfirmDeletion"),
                    LocalizationService.GetString("ConfirmDeleteNovel", novel.Title),
                    LocalizationService.GetString("Delete"),
                    LocalizationService.GetString("Cancel"));

            if (!confirm) return;

            try
            {
                // Deshabilitar el botón durante la operación
                button.IsEnabled = false;

                // Eliminar de la base de datos
                bool success = await _novelService.DeleteNovelAsync(novelId);

                if (success)
                {
                    await DisplayAlert(
                            LocalizationService.GetString("Success"),
                            LocalizationService.GetString("NovelDeletedSuccess"),
                            LocalizationService.GetString("OK")
                        );

                    // Recargar la lista
                    await LoadNovelsAsync();
                }
                else
                {
                    await DisplayAlert(
                            LocalizationService.GetString("Error"),
                            LocalizationService.GetString("ErrorDeletingNovel"),
                            LocalizationService.GetString("OK")
                        );
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                        LocalizationService.GetString("Error"),
                        LocalizationService.GetString("ErrorDeleting", ex.Message),
                        LocalizationService.GetString("OK")
                    );
            }
            finally
            {
                button.IsEnabled = true;
            }
        }
    }

    /// <summary>
    /// Maneja el botón de volver personalizado
    /// </summary>
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        try
        {
            // Intentar hacer pop de la página actual
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
            else
            {
                // Si no hay stack, volver usando Shell
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navegando hacia atrás: {ex.Message}");

            // Como último recurso, volver al tab de Más
            try
            {
                await Shell.Current.GoToAsync("//MoreTab");
            }
            catch
            {
                // Si todo falla, mostrar mensaje
                await DisplayAlert(
                        LocalizationService.GetString("Error"),
                        LocalizationService.GetString("ErrorNavigatingBack"),
                        LocalizationService.GetString("OK")
                    );
            }
        }

    }

    private void UpdateStatistics()
    {
        if (_novels == null) return;

        // Total de novelas
        TotalNovelsLabel.Text = _novels.Count.ToString();

        // Contar por estado
        int activeCount = _novels.Count(n => n.Status.ToLower() == "ongoing");
        int completedCount = _novels.Count(n => n.Status.ToLower() == "completed");
        int pausedCount = _novels.Count(n => n.Status.ToLower() == "hiatus");
        int cancelledCount = _novels.Count(n => n.Status.ToLower() == "cancelled");

        // Actualizar labels
        ActiveNovelsLabel.Text = activeCount.ToString();
        CompletedNovelsLabel.Text = completedCount.ToString();
        PausedNovelsLabel.Text = pausedCount.ToString();
        CancelledNovelsLabel.Text = cancelledCount.ToString();
    }
}


/// <summary>
/// Clase para mostrar novelas en la interfaz de gestión
/// </summary>
public class NovelDisplayItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public ImageSource CoverImageSource { get; set; }
    public int ChapterCount { get; set; }
    public string Status { get; set; }
    public string StatusText { get; set; }
    public Color StatusColor { get; set; }
    public string UpdatedAtText { get; set; }
}