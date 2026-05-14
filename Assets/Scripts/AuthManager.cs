using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Auth;

/// <summary>
/// Handles Firebase Authentication.
/// IMPORTANT: OnLoginSuccess fires AFTER profile is fetched — 
/// so any subscriber (like AppStateManager) can safely access CurrentProfile.
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public static event Action<FirebaseUser> OnLoginSuccess;
    public static event Action<string> OnLoginFailed;
    public static event Action OnLoggedOut;

    public FirebaseUser CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;

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
        FirebaseManager.OnFirebaseReady += CheckExistingSession;
    }

    void OnDestroy()
    {
        FirebaseManager.OnFirebaseReady -= CheckExistingSession;
    }

    public async Task Register(string email, string password, string name)
    {
        if (!ValidateInput(email, password)) return;

        try
        {
            var auth = FirebaseManager.Instance.Auth;
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            CurrentUser = result.User;
            Debug.Log($"User registered: {CurrentUser.Email}");

            await CreateInitialProfile(CurrentUser.UserId, name, email);

            // Clear cache and fetch fresh profile
            ProfileCache.Instance?.ClearProfile(CurrentUser.UserId);
            await ProfileService.Instance.FetchProfile(CurrentUser.UserId);
            Debug.Log($"Profile after register: {ProfileService.Instance.CurrentProfile?.name}");

            // Fire AFTER profile is ready
            OnLoginSuccess?.Invoke(CurrentUser);
        }
        catch (Exception e)
        {
            Debug.LogError($"Registration failed: {e.Message}");
            OnLoginFailed?.Invoke(ParseAuthError(e.Message));
        }
    }

    public async Task Login(string email, string password)
    {
        if (!ValidateInput(email, password)) return;

        try
        {
            var auth = FirebaseManager.Instance.Auth;
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            CurrentUser = result.User;
            Debug.Log($"User logged in: {CurrentUser.Email}");

            // Clear cache and fetch fresh profile
            ProfileCache.Instance?.ClearProfile(CurrentUser.UserId);
            await ProfileService.Instance.FetchProfile(CurrentUser.UserId);
            Debug.Log($"Profile after login: {ProfileService.Instance.CurrentProfile?.name}");

            // Fire AFTER profile is ready
            OnLoginSuccess?.Invoke(CurrentUser);
        }
        catch (Exception e)
        {
            Debug.LogError($"Login failed: {e.Message}");
            OnLoginFailed?.Invoke(ParseAuthError(e.Message));
        }
    }

    public void Logout()
    {
        var uid = CurrentUser?.UserId;
        FirebaseManager.Instance.Auth.SignOut();
        CurrentUser = null;
        if (uid != null) ProfileCache.Instance?.ClearProfile(uid);
        ProfileService.Instance?.ClearCurrentProfile();
        Debug.Log("User logged out");
        OnLoggedOut?.Invoke();
    }

    private async void CheckExistingSession()
    {
        // Unsubscribe immediately — only run once
        FirebaseManager.OnFirebaseReady -= CheckExistingSession;

        Debug.Log("CheckExistingSession called");
        var auth = FirebaseManager.Instance.Auth;
        CurrentUser = auth.CurrentUser;

        if (CurrentUser != null)
        {
            Debug.Log($"Existing session: {CurrentUser.Email}");

            // Clear stale cache and fetch fresh from Firebase
            ProfileCache.Instance?.ClearProfile(CurrentUser.UserId);
            await ProfileService.Instance.FetchProfile(CurrentUser.UserId);
            Debug.Log($"Session profile: {ProfileService.Instance.CurrentProfile?.name ?? "null"}");

            // Fire AFTER profile is ready
            OnLoginSuccess?.Invoke(CurrentUser);
        }
        else
        {
            Debug.Log("No existing session — show auth");
            // No user — go to auth screen directly
            AppStateManager.Instance?.GoToAuth();
        }
    }

    private async Task CreateInitialProfile(string uid, string name, string email)
    {
        try
        {
            var db = FirebaseManager.Instance.Database;
            var docRef = db.Collection("users").Document(uid);
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

    private string ComputeInitials(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        string[] parts = name.Split(' ');
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
        return parts[0][0].ToString().ToUpper();
    }

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

    private string ParseAuthError(string error)
    {
        if (error.Contains("email-already-in-use")) return "An account with this email already exists";
        if (error.Contains("wrong-password")) return "Incorrect password";
        if (error.Contains("user-not-found")) return "No account found with this email";
        if (error.Contains("invalid-email")) return "Invalid email address";
        if (error.Contains("network-request-failed")) return "No internet connection";
        return "Something went wrong. Please try again";
    }
}