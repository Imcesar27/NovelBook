using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NovelBook.Models;
using System.Net;
using System.Web;

namespace NovelBook.Services
{
    /// <summary>
    /// Servicio que maneja la autenticación con GitHub usando OAuth
    /// Versión multiplataforma que funciona en Windows
    /// </summary>
    public class GitHubAuthService
    {
        private readonly DatabaseService _databaseService;
        private readonly AuthService _authService;
        private readonly HttpClient _httpClient;

        // Configuración de GitHub OAuth
        // IMPORTANTE: Reemplaza estos valores con los tuyos de GitHub
        private const string CLIENT_ID = "Ov23linqEVIDUY5FeDxf";
        private const string CLIENT_SECRET = "33a90dd49d15a874fc26f6e5958558f72be3bbc5";
        private const string REDIRECT_URI = "http://localhost:8080/callback";

        // URLs de GitHub OAuth
        private const string AUTHORIZE_URL = "https://github.com/login/oauth/authorize";
        private const string TOKEN_URL = "https://github.com/login/oauth/access_token";
        private const string USER_API_URL = "https://api.github.com/user";

        // Para el servidor local en Windows
        private HttpListener _httpListener;
        private string _authorizationCode;

        public GitHubAuthService(DatabaseService databaseService, AuthService authService)
        {
            _databaseService = databaseService;
            _authService = authService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "NovelBook-App");
        }

        /// <summary>
        /// Inicia el proceso de login con GitHub
        /// </summary>
        public async Task<(bool success, string message, User user)> LoginWithGitHubAsync()
        {
            try
            {
                string code = null;

                // Detectar plataforma y usar el método apropiado
#if WINDOWS
                code = await GetAuthorizationCodeWindows();
#else
                code = await GetAuthorizationCodeMobile();
#endif

                if (string.IsNullOrEmpty(code))
                {
                    return (false, "No se recibió código de autorización", null);
                }

                // Intercambiar código por token de acceso
                var tokenResponse = await ExchangeCodeForTokenAsync(code);
                if (string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return (false, "No se pudo obtener el token de acceso", null);
                }

                // Obtener información del usuario de GitHub
                var githubUser = await GetGitHubUserAsync(tokenResponse.AccessToken);
                if (githubUser == null)
                {
                    return (false, "No se pudo obtener la información del usuario", null);
                }

                // Guardar o actualizar usuario en la base de datos
                var user = await UpsertGitHubUserAsync(githubUser);
                if (user != null)
                {
                    // Establecer el usuario actual
                    _authService.SetCurrentUser(user);

                    // Opcionalmente guardar el token
                    if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        await SaveAuthTokenAsync(user.Id, "github", tokenResponse.AccessToken, null);
                    }

                    return (true, "Login exitoso", user);
                }
                else
                {
                    return (false, "Error al crear o actualizar el usuario", null);
                }
            }
            catch (TaskCanceledException)
            {
                return (false, "Login cancelado", null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en GitHub Login: {ex.Message}");
                return (false, "Error inesperado al iniciar sesión", null);
            }
        }

        /// <summary>
        /// Obtiene el código de autorización en Windows usando un servidor local
        /// </summary>
        private async Task<string> GetAuthorizationCodeWindows()
        {
            try
            {
                // Generar state para seguridad
                var state = Guid.NewGuid().ToString("N");

                // Construir URL de autorización
                var authUrl = $"{AUTHORIZE_URL}?client_id={CLIENT_ID}&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}&scope=user:email&state={state}";

                // Iniciar servidor local para capturar el callback
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add("http://localhost:8080/");
                _httpListener.Start();

                // Abrir navegador
                await Browser.Default.OpenAsync(authUrl, BrowserLaunchMode.SystemPreferred);

                // Esperar el callback
                var context = await _httpListener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                // Extraer el código de la URL
                var query = request.Url.Query;
                var queryParams = HttpUtility.ParseQueryString(query);
                _authorizationCode = queryParams["code"];

                // Enviar respuesta HTML al navegador
                string responseString = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>NovelBook - Login Exitoso</title>
                        <style>
                            body {
                                font-family: Arial, sans-serif;
                                display: flex;
                                justify-content: center;
                                align-items: center;
                                height: 100vh;
                                margin: 0;
                                background-color: #f0f0f0;
                            }
                            .container {
                                text-align: center;
                                padding: 2rem;
                                background: white;
                                border-radius: 10px;
                                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                            }
                            h1 { color: #28a745; }
                            p { color: #666; }
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h1>✓ Login Exitoso</h1>
                            <p>Puedes cerrar esta ventana y regresar a NovelBook</p>
                        </div>
                        <script>setTimeout(() => window.close(), 3000);</script>
                    </body>
                    </html>";

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                _httpListener.Stop();

                return _authorizationCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en GetAuthorizationCodeWindows: {ex.Message}");
                _httpListener?.Stop();
                return null;
            }
        }

        /// <summary>
        /// Obtiene el código de autorización en dispositivos móviles
        /// </summary>
        private async Task<string> GetAuthorizationCodeMobile()
        {
            try
            {
                var state = Guid.NewGuid().ToString("N");
                var mobileRedirectUri = "novelbook://oauth/github";
                var authUrl = $"{AUTHORIZE_URL}?client_id={CLIENT_ID}&redirect_uri={Uri.EscapeDataString(mobileRedirectUri)}&scope=user:email&state={state}";

                var authResult = await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(authUrl),
                    new Uri(mobileRedirectUri));

                if (authResult.Properties.TryGetValue("code", out var code))
                {
                    return code;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en GetAuthorizationCodeMobile: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Intercambia el código de autorización por un token de acceso
        /// </summary>
        private async Task<GitHubTokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                // Usar el redirect URI correcto según la plataforma
#if WINDOWS
                var currentRedirectUri = REDIRECT_URI;
#else
                var currentRedirectUri = "novelbook://oauth/github";
#endif

                var requestData = new
                {
                    client_id = CLIENT_ID,
                    client_secret = CLIENT_SECRET,
                    code = code,
                    redirect_uri = currentRedirectUri
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // GitHub requiere Accept header para recibir JSON
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.PostAsync(TOKEN_URL, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<GitHubTokenResponse>(responseContent);
                }

                System.Diagnostics.Debug.WriteLine($"Error al obtener token: {responseContent}");
                return new GitHubTokenResponse();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ExchangeCodeForToken: {ex.Message}");
                return new GitHubTokenResponse();
            }
        }

        /// <summary>
        /// Obtiene la información del usuario de GitHub
        /// </summary>
        private async Task<GitHubUser> GetGitHubUserAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.GetAsync(USER_API_URL);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<GitHubUser>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener usuario de GitHub: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Guarda o actualiza el usuario de GitHub en la base de datos
        /// </summary>
        private async Task<User> UpsertGitHubUserAsync(GitHubUser githubUser)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                using var command = new SqlCommand("sp_UpsertSocialLoginUser", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;

                // Usar el email o generar uno basado en el username si no está disponible
                var email = githubUser.Email ?? $"{githubUser.Login}@github.local";

                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Name", githubUser.Name ?? githubUser.Login);
                command.Parameters.AddWithValue("@Provider", "github");
                command.Parameters.AddWithValue("@ProviderId", githubUser.Id.ToString());
                command.Parameters.AddWithValue("@ProfilePictureUrl", (object)githubUser.AvatarUrl ?? DBNull.Value);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Email = reader.GetString(reader.GetOrdinal("email")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        ProfilePictureUrl = reader.IsDBNull(reader.GetOrdinal("profile_picture_url"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("profile_picture_url")),
                        AuthProvider = reader.IsDBNull(reader.GetOrdinal("auth_provider"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("auth_provider")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("last_login")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                        Role = reader.GetString(reader.GetOrdinal("role")),
                        GithubId = githubUser.Id.ToString()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar usuario de GitHub: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Guarda el token de autenticación (opcional)
        /// </summary>
        private async Task SaveAuthTokenAsync(int userId, string provider, string accessToken, string refreshToken)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Primero eliminar tokens anteriores
                var deleteQuery = "DELETE FROM user_auth_tokens WHERE user_id = @userId AND provider = @provider";
                using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@userId", userId);
                    deleteCommand.Parameters.AddWithValue("@provider", provider);
                    await deleteCommand.ExecuteNonQueryAsync();
                }

                // Insertar nuevo token
                var insertQuery = @"
                    INSERT INTO user_auth_tokens (user_id, provider, access_token, refresh_token)
                    VALUES (@userId, @provider, @accessToken, @refreshToken)";

                using var insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@userId", userId);
                insertCommand.Parameters.AddWithValue("@provider", provider);
                insertCommand.Parameters.AddWithValue("@accessToken", accessToken);
                insertCommand.Parameters.AddWithValue("@refreshToken", (object)refreshToken ?? DBNull.Value);

                await insertCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // No es crítico si falla guardar el token
                System.Diagnostics.Debug.WriteLine($"Error al guardar token: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Respuesta del token de GitHub
    /// </summary>
    public class GitHubTokenResponse
    {
        public string access_token { get; set; }
        public string AccessToken => access_token;
        public string token_type { get; set; }
        public string scope { get; set; }
    }

    /// <summary>
    /// Información del usuario de GitHub
    /// </summary>
    public class GitHubUser
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public string Bio { get; set; }
        public string Location { get; set; }
        public string Company { get; set; }
    }
}