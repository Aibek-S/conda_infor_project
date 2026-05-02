/*
 * SUPABASE AUTHENTICATION IMPLEMENTATION GUIDE
 * ===========================================
 * 
 * This document explains the authentication system implemented for your WinForms application
 * using Supabase Auth and REST API.
 *
 * ARCHITECTURE OVERVIEW
 * =====================
 * 
 * UI Layer (Forms)
 *   ↓
 * Service Layer (AuthService)
 *   ↓
 * Repository Layer (AuthRepository)
 *   ↓
 * HTTP Client (DataBase.GetClient())
 *   ↓
 * Supabase API
 *
 *
 * SUPABASE ENDPOINTS USED
 * =======================
 * 
 * 1. /auth/v1/signup
 *    Method: POST
 *    Purpose: Create a new authentication account
 *    Payload: { email, password }
 *    Response: AuthResponse with user.id and session token
 *
 * 2. /auth/v1/token?grant_type=password
 *    Method: POST
 *    Purpose: Authenticate existing user and get session token
 *    Payload: { email, password }
 *    Response: AuthResponse with access_token and refresh_token
 *
 * 3. /rest/v1/profiles (POST)
 *    Method: POST
 *    Purpose: Insert user profile after successful registration
 *    Payload: { id, email, login, full_name, role }
 *    Response: Array with created profile
 *
 * 4. /rest/v1/profiles?email=eq.{email}&select=*
 *    Method: GET
 *    Purpose: Fetch user profile by email
 *    Response: Array with user profile
 *
 *
 * KEY COMPONENTS
 * ==============
 *
 * 1. DataBase.cs
 *    - Singleton HttpClient with base configuration
 *    - Sets default headers: apikey and Authorization Bearer token
 *    - Supabase URL and API key (anon public key) are already configured
 *
 * 2. AuthResponse.cs (Models)
 *    - AuthResponse: Wraps user data and session from Supabase Auth
 *    - AuthUser: Contains id, email, metadata from auth
 *    - AuthSession: Contains access_token, refresh_token, expires_in
 *    - AuthRequest: Email + password payload
 *    - AuthError: Error response structure from Supabase
 *
 * 3. User.cs (Models)
 *    - Updated to use Email instead of PasswordHash
 *    - Maps to the "profiles" table in Supabase
 *    - Properties: id, email, login, full_name, role
 *
 * 4. AuthRepository.cs
 *    - SignUpAsync(email, password): Creates auth account
 *    - SignInAsync(email, password): Authenticates user
 *    - CreateUserProfileAsync(...): Inserts into profiles table
 *    - GetUserByEmailAsync(email): Fetches user profile
 *
 * 5. AuthService.cs
 *    - RegisterAsync(email, password, fullName, role): Orchestrates signup + profile creation
 *    - LoginAsync(email, password): Orchestrates signin + profile fetch
 *    - Handles errors and throws meaningful exceptions
 *
 * 6. login.cs (Form)
 *    - Calls AuthService.LoginAsync()
 *    - Shows error/success messages
 *    - Disables button during request
 *    - TODO: Open main form on success
 *
 * 7. register.cs (Form)
 *    - Calls AuthService.RegisterAsync()
 *    - Validates email and password (min 6 chars)
 *    - Shows error/success messages
 *    - Redirects to login on success
 *
 *
 * REGISTRATION FLOW
 * =================
 * 
 * User enters: email, password, full name
 *   ↓
 * register.registerEnterButton_Click()
 *   ↓
 * AuthService.RegisterAsync()
 *   ├─ AuthRepository.SignUpAsync() → Supabase Auth
 *   │  └─ Returns: { user.id, session }
 *   │
 *   ├─ AuthRepository.CreateUserProfileAsync()
 *   │  └─ Inserts into /rest/v1/profiles with user.id
 *   │
 *   └─ Returns: User profile object
 *   ↓
 * Show success message
 * Navigate to login form
 *
 *
 * LOGIN FLOW
 * ==========
 * 
 * User enters: email, password
 *   ↓
 * login.loginEnterButton_Click()
 *   ↓
 * AuthService.LoginAsync()
 *   ├─ AuthRepository.SignInAsync() → Supabase Auth
 *   │  └─ Returns: { session.access_token }
 *   │
 *   ├─ AuthRepository.GetUserByEmailAsync()
 *   │  └─ Fetches from /rest/v1/profiles
 *   │
 *   └─ Returns: User profile object
 *   ↓
 * Show success message
 * TODO: Open main form and hide login form
 *
 *
 * ERROR HANDLING
 * ==============
 * 
 * Common errors from Supabase:
 * 
 * - "User already registered": Email already has an account
 * - "Invalid login credentials": Email or password is wrong (login only)
 * - "Invalid email format": Email doesn't match standard format
 * - "Password too weak": Password doesn't meet requirements
 * 
 * All errors are caught and displayed to the user in message boxes
 * without crashing the application.
 *
 *
 * JSON SERIALIZATION
 * ==================
 * 
 * [JsonPropertyName("...")] attributes map C# properties to JSON keys
 * PropertyNameCaseInsensitive = true allows matching despite case differences
 * 
 * Example:
 *   C# Property: public string FullName { get; set; }
 *   JSON Key: "full_name"
 *   Mapping: [JsonPropertyName("full_name")]
 *
 *
 * SECURITY NOTES
 * ==============
 * 
 * ✓ Passwords are hashed on Supabase backend
 * ✓ Never stored locally in plaintext
 * ✓ API key is public (anon key), suitable for client-side use
 * ✓ Session tokens expire after configured time
 * ✓ Use refresh_token to obtain new access_token when expired
 *
 * TODO for production:
 * - Implement refresh token handling
 * - Store session token for API calls
 * - Add password reset functionality
 * - Implement email verification
 * - Add 2FA support
 *
 *
 * TODO ITEMS IN CODE
 * ==================
 * 
 * 1. login.cs line ~30:
 *    TODO: Open main form and pass user info
 *    → Create MainForm class and pass User object
 *    → Example: MainForm mainForm = new MainForm(user);
 *
 * 2. register.cs line ~23:
 *    TODO: Update these with actual form controls for fullName and role
 *    → Add TextBox controls for full name
 *    → Add ComboBox control for role selection (admin, user, etc.)
 *    → Replace hardcoded values with actual control values
 *
 * 3. General:
 *    → Add token storage/caching for future API calls
 *    → Implement logout functionality
 *    → Add "Forgot Password" feature
 *    → Add email verification before login
 *
 *
 * TESTING
 * =======
 * 
 * 1. Test Registration:
 *    - Open register form
 *    - Enter new email, password (6+ chars), name
 *    - Click Register
 *    - Should show success and navigate to login
 *    - Verify user appears in Supabase "profiles" table
 *
 * 2. Test Login:
 *    - Open login form
 *    - Enter email and password from registration
 *    - Click Login
 *    - Should show success
 *    - TODO: Should open main form
 *
 * 3. Test Error Cases:
 *    - Register with invalid email format
 *    - Register with password < 6 characters
 *    - Login with non-existent email
 *    - Login with wrong password
 *    - Register with already-used email
 *
 *
 * CONFIGURATION
 * ==============
 * 
 * Supabase Project Details (in DataBase.cs):
 * - Url: "https://eqyuifxlyeolgonuaylb.supabase.co"
 * - ApiKey: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
 * 
 * These are already configured. For a new project:
 * 1. Go to https://supabase.com
 * 2. Create a new project
 * 3. Go to Project Settings → API
 * 4. Copy Project URL and anon public key
 * 5. Update DataBase.cs with new values
 *
 * Database Requirements:
 * - Table: "profiles"
 * - Columns: id (uuid, primary key), email, login, full_name, role
 * - Row-Level Security (RLS): Enable for security
 *
 */
