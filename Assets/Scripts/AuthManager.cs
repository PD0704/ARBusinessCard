using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Auth;

/// <summary>
/// Handles Firebase Authentication — email/password registration and login.
/// Single source of truth for current user session.
/// Other scripts access current user via AuthManager.Instance.CurrentUser.
/// Attach to the Managers GameObject in the scene.
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    // Fired when user successfully logs in or registers
    public static event Action<FirebaseUser> OnLoginSuccess;

    // Fired when login or registration fails
    public static event Action<string> OnLoginFailed;

    // Fired when user logs out
    public static event Action OnLoggedOut;

    // ── State ─────────────────────────────────────────────────────
    public FirebaseUser CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;

    // ── Lifecycle ─────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Check if user is already logged in from previous session
        FirebaseManager.OnFirebaseReady += CheckExistingSession;
    }

    void OnDestroy()
    {
        FirebaseManager.OnFirebaseReady -= CheckExistingSession;
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Registers a new user with email and password.
    /// On success creates Firestore profile document and fires OnLoginSuccess.
    /// </summary>
    public async Task Register(string email, string password, string name)
    {
        if (!ValidateInput(email, password)) return;

        try
        {
            var auth = FirebaseManager.Instance.Auth;
            var result = await auth.CreateUserWithEmailAndPasswordAsync(
                email, password);

            CurrentUser = result.User;
            Debug.Log($"User registered: {CurrentUser.Email}");

            // Create initial Firestore profile
            await CreateInitialProfile(CurrentUser.UserId, name, email);

            OnLoginSuccess?.Invoke(CurrentUser);
        }
        catch (Exception e)
        {
            Debug.LogError($"Registration failed: {e.Message}");
            OnLoginFailed?.Invoke(ParseAuthError(e.Message));
        }
    }

    /// <summary>
    /// Logs in an existing user with email and password.
    /// </summary>
    public async Task Login(string email, string password)
    {
        if (!ValidateInput(email, password)) return;

        try
        {
            var auth = FirebaseManager.Instance.Auth;
            var result = await auth.SignInWithEmailAndPasswordAsync(
                email, password);

            CurrentUser = result.User;
            Debug.Log($"User logged in: {CurrentUser.Email}");
            OnLoginSuccess?.Invoke(CurrentUser);
        }
        catch (Exception e)
        {
            Debug.LogError($"Login failed: {e.Message}");
            OnLoginFailed?.Invoke(ParseAuthError(e.Message));
        }
    }

    /// <summary>
    /// Logs out the current user and clears session.
    /// </summary>
    public void Logout()
    {
        FirebaseManager.Instance.Auth.SignOut();
        CurrentUser = null;
        ProfileService.Instance?.ClearCurrentProfile();
        Debug.Log("User logged out");
        OnLoggedOut?.Invoke();
        AppStateManager.Instance?.GoToAuth();
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Checks if a user session exists from a previous app launch.
    /// If yes, skip auth screen and go straight to Home.
    /// </summary>
    private void CheckExistingSession()
    {
        var auth = FirebaseManager.Instance.Auth;
        CurrentUser = auth.CurrentUser;

        if (CurrentUser != null)
        {
            Debug.Log($"Existing session found: {CurrentUser.Email}");
            OnLoginSuccess?.Invoke(CurrentUser);
        }
        else
        {
            Debug.Log("No existing session");
        }
    }

    /// <summary>
    /// Creates the initial Firestore profile document for a new user.
    /// Called automatically after successful registration.
    /// </summary>
    private async Task CreateInitialProfile(string uid, string name, string email)
    {
        try
        {
            var db = FirebaseManager.Instance.Database;
            var docRef = db.Collection("users").Document(uid);

            // Compute initials from name
            string initials = ComputeInitials(name);

            var profileData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "name", name },
                { "email", email },
                { "role", "" },
                { "company", "" },
                { "phone", "" },
                { "address", "" },
                { "linkedin", "" },
                { "portfolio", "" },
                { "pdfUrl", "" },
                { "cardImageUrl", "" },
                { "vuforiaTargetId", "" },
                { "initials", initials },
                { "initialsStyle", "2" },
                { "createdAt", Firebase.Firestore.FieldValue.ServerTimestamp },
                { "updatedAt", Firebase.Firestore.FieldValue.ServerTimestamp }
            };

            await docRef.SetAsync(profileData);
            Debug.Log($"Initial profile created for: {name}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create initial profile: {e.Message}");
        }
    }

    /// <summary>
    /// Computes display initials from full name.
    /// "Prahelika Dutta" → "PD"
    /// </summary>
    private string ComputeInitials(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        string[] parts = name.Split(' ');
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
        return parts[0][0].ToString().ToUpper();
    }

    /// <summary>
    /// Validates email and password before sending to Firebase.
    /// </summary>
    private bool ValidateInput(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            OnLoginFailed?.Invoke("Email and password cannot be empty");
            return false;
        }
        if (password.Length < 6)
        {
            OnLoginFailed?.Invoke("Password must be at least 6 characters");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Converts Firebase error messages to user-friendly strings.
    /// </summary>
    private string ParseAuthError(string error)
    {
        if (error.Contains("email-already-in-use"))
            return "An account with this email already exists";
        if (error.Contains("wrong-password"))
            return "Incorrect password";
        if (error.Contains("user-not-found"))
            return "No account found with this email";
        if (error.Contains("invalid-email"))
            return "Invalid email address";
        if (error.Contains("network-request-failed"))
            return "No internet connection";
        return "Something went wrong. Please try again";
    }
}