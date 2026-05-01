using System;
using UnityEngine;

/// <summary>
/// Global state machine for the app.
/// Controls navigation between: Splash → Auth → Home → Scan → Profile
/// All scene transitions and UI panel switches go through here.
/// Single source of truth for what the user is currently seeing.
/// </summary>
public class AppStateManager : MonoBehaviour
{
    public static AppStateManager Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    // Fired whenever app state changes — UI panels listen to this
    public static event Action<AppState> OnStateChanged;

    // ── State ─────────────────────────────────────────────────────
    public AppState CurrentState { get; private set; } = AppState.Splash;

    // ── Inspector References ──────────────────────────────────────
    [Header("UI Panels")]
    [Tooltip("Splash screen panel")]
    [SerializeField] private GameObject splashPanel;

    [Tooltip("Login / registration panel")]
    [SerializeField] private GameObject authPanel;

    [Tooltip("Home screen panel — My Card + Scan buttons")]
    [SerializeField] private GameObject homePanel;

    [Tooltip("Scan screen panel — QR and AR scan modes")]
    [SerializeField] private GameObject scanPanel;

    [Tooltip("Profile overlay panel — shown after successful scan")]
    [SerializeField] private GameObject profilePanel;

    [Tooltip("Profile setup panel — for creators filling their info")]
    [SerializeField] private GameObject profileSetupPanel;

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
        // Listen for Firebase ready to decide initial state
        FirebaseManager.OnFirebaseReady += HandleFirebaseReady;
        FirebaseManager.OnFirebaseError += HandleFirebaseError;

        // Listen for profile fetch to navigate to profile view
        ProfileService.OnProfileFetched += HandleProfileFetched;

        // Start at splash
        GoToState(AppState.Splash);
    }

    void OnDestroy()
    {
        FirebaseManager.OnFirebaseReady -= HandleFirebaseReady;
        FirebaseManager.OnFirebaseError -= HandleFirebaseError;
        ProfileService.OnProfileFetched -= HandleProfileFetched;
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Navigate to a new app state.
    /// Activates the correct panel and deactivates all others.
    /// </summary>
    public void GoToState(AppState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"App state: {newState}");

        // Deactivate all panels first
        splashPanel?.SetActive(false);
        authPanel?.SetActive(false);
        homePanel?.SetActive(false);
        scanPanel?.SetActive(false);
        profilePanel?.SetActive(false);
        profileSetupPanel?.SetActive(false);

        // Activate the correct panel
        switch (newState)
        {
            case AppState.Splash:
                splashPanel?.SetActive(true);
                break;

            case AppState.Auth:
                authPanel?.SetActive(true);
                break;

            case AppState.Home:
                homePanel?.SetActive(true);
                break;

            case AppState.Scan:
                scanPanel?.SetActive(true);
                // Default to AR mode when entering scan
                ScanModeController.Instance?.ActivateARMode();
                break;

            case AppState.Profile:
                profilePanel?.SetActive(true);
                break;

            case AppState.ProfileSetup:
                profileSetupPanel?.SetActive(true);
                break;
        }

        OnStateChanged?.Invoke(newState);
    }

    // Convenience methods for UI buttons to call directly
    public void GoToHome() => GoToState(AppState.Home);
    public void GoToScan() => GoToState(AppState.Scan);
    public void GoToAuth() => GoToState(AppState.Auth);
    public void GoToProfileSetup() => GoToState(AppState.ProfileSetup);

    /// <summary>
    /// Called when back button is pressed.
    /// Returns to previous logical screen.
    /// </summary>
    public void GoBack()
    {
        switch (CurrentState)
        {
            case AppState.Scan:
            case AppState.Profile:
            case AppState.ProfileSetup:
                GoToState(AppState.Home);
                break;
            case AppState.Home:
                GoToState(AppState.Auth);
                break;
            default:
                break;
        }
    }

    // ── Private Handlers ─────────────────────────────────────────

    /// <summary>
    /// When Firebase is ready, check if user is already logged in.
    /// If yes → Home. If no → Auth.
    /// </summary>
    private void HandleFirebaseReady()
    {
        var auth = FirebaseManager.Instance.Auth;
        if (auth.CurrentUser != null)
        {
            Debug.Log($"User already logged in: {auth.CurrentUser.Email}");
            GoToState(AppState.Home);
        }
        else
        {
            GoToState(AppState.Auth);
        }
    }

    /// <summary>
    /// When Firebase fails to initialize, stay on splash and log error.
    /// In production this would show an error UI.
    /// </summary>
    private void HandleFirebaseError(string error)
    {
        Debug.LogError($"Firebase error, staying on splash: {error}");
    }

    /// <summary>
    /// When a profile is fetched after scanning, navigate to profile view.
    /// </summary>
    private void HandleProfileFetched(UserProfile profile)
    {
        GoToState(AppState.Profile);
    }
}

/// <summary>
/// All possible app states.
/// Each state corresponds to a UI pan