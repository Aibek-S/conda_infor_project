using conda_infor_project.models;
using conda_infor_project.repository;

namespace conda_infor_project.services
{
    public class AuthService
    {
        private readonly AuthRepository _authRepository;
        public string? CurrentAccessToken { get; private set; }

        public AuthService()
        {
            _authRepository = new AuthRepository();
        }

        public async Task<User> RegisterAsync(string email, string password, string fullName, string role = "student")
        {
            try
            {
                Logger.LogInfo($"Starting registration for email: {email}");

                AuthResponse authResponse;
                try
                {
                    authResponse = await _authRepository.SignUpAsync(email, password);
                }
                catch (Exception signUpException) when (CanRecoverRegistrationByLogin(signUpException))
                {
                    Logger.LogWarning($"SignUp did not return a new account, trying login for existing auth user: {email}");
                    authResponse = await _authRepository.SignInAsync(email, password);
                }

                if (authResponse?.User?.Id == null)
                {
                    throw new Exception("Supabase не вернул id пользователя.");
                }

                Logger.LogInfo($"Auth account created with ID: {authResponse.User.Id}");
                AuthSession? session = authResponse.CurrentSession;
                CurrentAccessToken = session?.AccessToken;

                if (string.IsNullOrWhiteSpace(session?.AccessToken))
                {
                    Logger.LogWarning("Auth account was created without a session. Profile will be created after email confirmation/login.");
                    return CreateFallbackUser(authResponse.User.Id, email, fullName, role);
                }

                User? existingProfile = await _authRepository.GetUserByEmailAsync(email, session.AccessToken);
                if (existingProfile != null)
                {
                    Logger.LogInfo($"User profile already exists for: {email}");
                    return existingProfile;
                }

                User userProfile;
                try
                {
                    userProfile = await _authRepository.CreateUserProfileAsync(
                        authResponse.User.Id,
                        email,
                        fullName,
                        role,
                        session.AccessToken
                    );
                }
                catch (Exception profileException)
                {
                    Logger.LogWarning($"Auth account exists, but profile creation failed: {profileException.Message}");
                    return CreateFallbackUser(authResponse.User.Id, email, fullName, role);
                }

                Logger.LogInfo($"User profile created successfully for: {email}");
                return userProfile;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Registration failed for email: {email}", ex);
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<User> LoginAsync(string email, string password)
        {
            try
            {
                Logger.LogInfo($"Starting login for email: {email}");

                AuthResponse authResponse = await _authRepository.SignInAsync(email, password);
                AuthSession? session = authResponse.CurrentSession;

                if (session?.AccessToken == null)
                {
                    throw new Exception("Supabase не вернул токен сессии.");
                }

                Logger.LogInfo($"Authentication successful for email: {email}");
                CurrentAccessToken = session.AccessToken;

                User? userProfile = await _authRepository.GetUserByEmailAsync(email, session.AccessToken);

                if (userProfile == null)
                {
                    Logger.LogWarning($"User profile not found for email: {email}. Creating default user profile.");
                    AuthUser? authUser = authResponse.User;
                    string userId = authUser?.Id ?? Guid.NewGuid().ToString();
                    string fullName = authUser?.Email?.Split('@')[0] ?? email.Split('@')[0];

                    try
                    {
                        userProfile = await _authRepository.CreateUserProfileAsync(
                            userId,
                            email,
                            fullName,
                            "student",
                            session.AccessToken
                        );
                    }
                    catch (Exception profileException)
                    {
                        Logger.LogWarning($"Login succeeded, but profile creation failed: {profileException.Message}");
                        userProfile = CreateFallbackUser(userId, email, fullName, "student");
                    }
                }

                Logger.LogInfo($"User profile loaded for: {email}");
                return userProfile;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Login failed for email: {email}", ex);
                throw new Exception(ex.Message, ex);
            }
        }

        private static bool CanRecoverRegistrationByLogin(Exception exception)
        {
            string message = exception.Message;
            return message.Contains("already registered", StringComparison.OrdinalIgnoreCase)
                || message.Contains("already been registered", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Слишком много запросов", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Too many", StringComparison.OrdinalIgnoreCase);
        }

        private static User CreateFallbackUser(string userId, string email, string fullName, string role)
        {
            return new User
            {
                Id = userId,
                Email = email,
                Login = email,
                FullName = fullName,
                Role = role
            };
        }
    }
}

