using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Home screen UI.
/// Shows welcome message with user's name.
/// Wires My Card, Scan, Edit Profile and Sign Out buttons.
/// Attach to HomePanel in the scene.
/// </summary>
public class HomeUI : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────
    [Header("UI Elements")]
    [Tooltip("Displays the logged in user's name")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("Welcome back label above name")]
    [SerializeField] private TextMeshProUGUI welcomeText;

    [Header("Buttons")]
    [SerializeField] private Button myCardButton;
    [SerializeField] private Button scanButton;
    [SerializeField] private Button editProfileButton;
    [SerializeField] private Button logoutButton;

    // ── Lifecycle ─────────────────────────────────────────────────

    void OnEnable()
    {
        myCardButton?.onClick.AddListener(OnMyCardClicked);
        scanButton?.onClick.AddListener(OnScanClicked);
        editProfileButton?.onClick.AddListener(OnEditProfileClicked);
        logoutButton?.onClick.AddListener(OnLogoutClicked);

        // Load user profile and update welcome message
        LoadUserName();
    }

    void OnDisable()
    {
        myCardButton?.onClick.RemoveListener(OnMyCardClicked);
        scanButton?.onClick.RemoveListener(OnScanClicked);
        editProfileButton?.onClick.RemoveListener(OnEditProfileClicked);
        logoutButton?.onClick.RemoveListener(OnLogoutClicked);
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Loads the current user's name and displays it.
    /// Fetches from Firebase if not cached.
    /// </summary>
    private async void LoadUserName()
    {
        var auth = AuthManager.Instance;
        if (auth?.CurrentUser == null) return;

        string uid = auth.CurrentUser.UserId;

        // Try cache first
        var cached = ProfileCache.Instance?.LoadProfile(uid);
        if (cached != null)
        {
            UpdateWelcome(cached.name);
            return;
        }

        // Fetch from Firebase
        var profile = await FirebaseManager.Instance.FetchProfile(uid);
        if (profile != null)
            UpdateWelcome(profile.name);
    }

    /// <summary>
    /// Updates the welcome text with the user's name.
    /// </summary>
    private void UpdateWelcome(string name)
    {
        if (nameText != null)
            nameText.text = name;
        if (welcomeText != null)
            welcomeText.text = "Welcome back";
    }

    /// <summary>
    /// Opens My Card screen — shows creator's own card and QR.
    /// </summary>
    private void OnMyCardClicked()
    {
        // Fetch own profile then generate card
        var uid = AuthManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;
        _ = ProfileService.Instance.FetchProfile(uid);
        AppStateManager.Instance?.GoToProfileSetup();
    }

    /// <summary>
    /// Opens scan screen in AR mode.
    /// </summary>
    private void OnScanClicked()
    {
        AppStateManager.Instance?.GoToScan();
    }

    /// <summary>
    /// Opens profile setup/edit screen.
    /// </summary>
    private void OnEditProfileClicked()
    {
        AppStateManager.Instance?.GoToProfileSetup();
    }

    /// <summary>
    /// Logs out the current user and returns to auth screen.
    /// </summary>
    private void OnLogoutClicked()
    {
        AuthManager.Instance?.Logout();
    }
}