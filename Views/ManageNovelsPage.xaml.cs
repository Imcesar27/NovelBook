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
            await DisplayAlert("Acceso Denegado", "No tienes permisos para acceder a esta sección", "OK");
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
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar novelas: {ex.Message}", "OK");
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
                Author = novel.Author ?? "Sin autor",
                CoverImageSource = await _imageService.GetCoverImageAsync(novel.CoverImage),
                ChapterCount = novel.ChapterCount,
                Status = novel.Status,
                StatusText = GetStatusText(novel.Status),
                StatusColor = GetStatusColor(novel.Status),
                UpdatedAtText = $"Actualizado: {novel.UpdatedAt:dd/MM/yyyy}"
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
            "ongoing" => "En curso",
            "completed" => "Completado",
            "hiatus" => "En pausa",
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
                "Confirmar Eliminación",
                $"¿Estás seguro de eliminar '{novel.Title}'?\n\n" +
                "Esta acción eliminará también todos sus capítulos y no se puede deshacer.",
                "Eliminar",
                "Cancelar");

            if (!confirm) return;

            try
            {
                // Deshabilitar el botón durante la operación
                button.IsEnabled = false;

                // Eliminar de la base de datos
                bool success = await _novelService.DeleteNovelAsync(novelId);

                if (success)
                {
                    await DisplayAlert("Éxito", "Novela eliminada correctamente", "OK");

                    // Recargar la lista
                    await LoadNovelsAsync();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo eliminar la novela", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar: {ex.Message}", "OK");
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
        await Shell.Current.GoToAsync("..");

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