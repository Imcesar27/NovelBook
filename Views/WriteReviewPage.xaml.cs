using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class WriteReviewPage : ContentPage
{
    // Servicios
    private readonly ReviewService _reviewService;
    private readonly DatabaseService _databaseService;

    // Datos
    private readonly int _novelId;
    private readonly Review _existingReview; // Si está editando
    private int _selectedRating = 0;

    // Referencias a los botones de estrellas
    private List<Button> _starButtons;

    /// <summary>
    /// Constructor para crear o editar una reseña
    /// </summary>
    /// <param name="novelId">ID de la novela</param>
    /// <param name="novelTitle">Título de la novela</param>
    /// <param name="existingReview">Reseña existente si está editando</param>
    public WriteReviewPage(int novelId, string novelTitle, Review existingReview = null)
    {
        InitializeComponent();

        _novelId = novelId;
        _existingReview = existingReview;

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _reviewService = new ReviewService(_databaseService);

        // Configurar UI
        NovelTitleLabel.Text = novelTitle;
        Title = existingReview != null ?
                LocalizationService.GetString("EditReviewTitle") :
                LocalizationService.GetString("WriteReviewTitle");
        SaveButton.Text = _existingReview != null ?
                LocalizationService.GetString("UpdateReview") :
                LocalizationService.GetString("PublishReview");

        // Lista de botones de estrellas para facilitar el manejo
        _starButtons = new List<Button> { Star1, Star2, Star3, Star4, Star5 };

        // Si está editando, cargar datos existentes
        if (_existingReview != null)
        {
            LoadExistingReview();
        }

        // Configurar el editor de comentarios
        CommentEditor.TextChanged += OnCommentTextChanged;
    }

    /// <summary>
    /// Carga los datos de una reseña existente
    /// </summary>
    private void LoadExistingReview()
    {
        // Establecer la calificación
        _selectedRating = _existingReview.Rating;
        UpdateStarDisplay();

        // Establecer el comentario
        CommentEditor.Text = _existingReview.Comment ?? "";
        UpdateCharCount();
    }

    /// <summary>
    /// Maneja el clic en una estrella
    /// </summary>
    private void OnStarClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string ratingStr)
        {
            if (int.TryParse(ratingStr, out int rating))
            {
                _selectedRating = rating;
                UpdateStarDisplay();
            }
        }
    }

    /// <summary>
    /// Actualiza la visualización de las estrellas
    /// </summary>
    private void UpdateStarDisplay()
    {
        // Actualizar las estrellas
        for (int i = 0; i < _starButtons.Count; i++)
        {
            _starButtons[i].Text = i < _selectedRating ? "★" : "☆";
        }

        // Actualizar descripción
        RatingDescriptionLabel.Text = _selectedRating switch
        {
            1 => LocalizationService.GetString("VeryBad"),
            2 => LocalizationService.GetString("Bad"),
            3 => LocalizationService.GetString("Regular"),
            4 => LocalizationService.GetString("Good"),
            5 => LocalizationService.GetString("Excellent"),
            _ => LocalizationService.GetString("TouchStarsToRate")
        };

        // Cambiar color del texto según si ha calificado
        RatingDescriptionLabel.TextColor = _selectedRating > 0 ?
            Colors.White : Color.FromArgb("#808080");

        // Habilitar botón de guardar si ha seleccionado calificación
        SaveButton.IsEnabled = _selectedRating > 0;
    }

    /// <summary>
    /// Maneja cambios en el texto del comentario
    /// </summary>
    private void OnCommentTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCharCount();
    }

    /// <summary>
    /// Actualiza el contador de caracteres
    /// </summary>
    private void UpdateCharCount()
    {
        int charCount = CommentEditor.Text?.Length ?? 0;
        CharCountLabel.Text = LocalizationService.GetString("Characters", charCount);

        // Cambiar color si se acerca al límite
        if (charCount > 900)
        {
            CharCountLabel.TextColor = Color.FromArgb("#FF5252");
        }
        else
        {
            CharCountLabel.TextColor = Color.FromArgb("#808080");
        }
    }

    /// <summary>
    /// Maneja el clic en cancelar
    /// </summary>
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            LocalizationService.GetString("CancelReview"),
            LocalizationService.GetString("ConfirmCancel"),
            LocalizationService.GetString("Yes"),
            LocalizationService.GetString("No"));

        if (confirm)
        {
            await Navigation.PopAsync();
        }
    }

    /// <summary>
    /// Maneja el clic en guardar
    /// </summary>
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validar que haya calificación
        if (_selectedRating == 0)
        {
            await DisplayAlert(
                 LocalizationService.GetString("Error"),
                 LocalizationService.GetString("MustSelectRating"),
                 LocalizationService.GetString("OK"));
            return;
        }

        // Deshabilitar botón mientras guarda
        SaveButton.IsEnabled = false;
        SaveButton.Text = LocalizationService.GetString("Saving");

        try
        {
            // Obtener el comentario (puede ser vacío)
            string comment = CommentEditor.Text?.Trim() ?? "";

            // Guardar la reseña
            bool success = await _reviewService.SaveReviewAsync(
                AuthService.CurrentUser.Id,
                _novelId,
                _selectedRating,
                comment
            );

            if (success)
            {
                await DisplayAlert(
                    LocalizationService.GetString("Success"),
                    _existingReview != null ?
                        LocalizationService.GetString("ReviewUpdated") :
                        LocalizationService.GetString("ReviewPublished"),
                    LocalizationService.GetString("OK"));

                // Volver a la página anterior
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert(
                    LocalizationService.GetString("Error"),
                    LocalizationService.GetString("ErrorSavingReview"),
                    LocalizationService.GetString("OK"));

                // Rehabilitar botón
                SaveButton.IsEnabled = true;
                SaveButton.Text = _existingReview != null ?
                    LocalizationService.GetString("UpdateReview") :
                    LocalizationService.GetString("PublishReview");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationService.GetString("Error"),
                LocalizationService.GetString("ErrorSaving", ex.Message),
                LocalizationService.GetString("OK"));

            // Rehabilitar botón
            SaveButton.IsEnabled = true;
            SaveButton.Text = _existingReview != null ?
                LocalizationService.GetString("UpdateReview") :
                LocalizationService.GetString("PublishReview");
        }
    }
}