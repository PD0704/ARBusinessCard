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

    public static event Action<AppState> OnStateChanged;

    public AppState CurrentState { get; private set; } = AppState.Splash;
    private bool _isScanning = false;

    [Header("UI Panels")]
    [SerializeField] private GameObject splashPanel;
    [SerializeField] private GameObject authPanel;
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject scanPanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject profileSetupPanel;

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
        // Listen to AuthManager — not FirebaseManager — for navigation
        // AuthManager handles the session check and fires OnLoginSuccess when ready
        AuthManager.OnLoginSuccess += HandleLoginSuccess;
        AuthManager.OnLoggedOut += HandleLoggedOut;
        ProfileService.OnProfileFetched += HandleProfileFetched;

        GoToState(AppState.Splash);
    }

    void OnDestroy()
    {
        AuthManager.OnLoginSuccess -= HandleLoginSuccess;
        AuthManager.OnLoggedOut -= HandleLoggedOut;
        ProfileService.OnProfileFetched -= HandleProfileFetched;
    }

    public void GoToState(AppState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"App state: {newState}");

        splashPanel?.SetActive(false);
        authPanel?.SetActive(false);
        homePanel?.SetActive(false);
        scanPanel?.SetActive(false);
        profilePanel?.SetActive(false);
        profileSetupPanel?.SetActive(false);

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

    public void GoToHome()
    {
        _isScanning = false;
        GoToState(AppState.Home);
    }

    public void GoToScan()
    {
        _isScanning = true;
        GoToState(AppState.Scan);
    }

    public void GoToAuth() => GoToState(AppState.Auth);

    public void GoToProfileSetup()
    {
        _isScanning = false;
        GoToState(AppState.ProfileSetup);
    }

    public void GoBack()
    {
        switch (CurrentState)
        {
            case AppState.Scan:
            case AppState.Profile:
            case AppState.ProfileSetup:
                GoToHome();
                break;
            case AppState.Home:
                GoToAuth();
                break;
        }
    }

    /// <summary>
    /// Called by AuthManager after login OR session restore — profile is already fetched at this point
    /// </summary>
    private void HandleLoginSuccess(Firebase.Auth.FirebaseUser user)
    {
        Debug.Log($"AppStateManager: login success for {user.Email}, navigating to Home");
        GoToState(AppState.Home);
    }

    private void HandleLoggedOut()
    {
        GoToState(AppState.Auth);
    }

    private void HandleProfileFetched(UserProfile profile)
    {
        if (_isScanning)
        {
            _isScanning = false;
            GoToState(AppState.Profile);
        }
    }
}

public enum AppState
{
    Splash,
    Auth,
    Home,
    Scan,
    Profile,
    ProfileSetup
}