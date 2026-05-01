using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Populates the AR overlay UI cards with data from a UserProfile.
/// Attach this to the root of the AROverlayCanvas prefab.
/// All UI references are assigned via Inspector.
/// </summary>
public class ProfileCardUI : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────
    [Header("Identity Card")]
    [Tooltip("Displays user initials in the monogram circle")]
    [SerializeField] private TextMeshProUGUI initialsText;

    [Tooltip("Displays full name")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("Displays job role")]
    [SerializeField] private TextMeshProUGUI roleText;

    [Tooltip("Displays company name")]
    [SerializeField] private TextMeshProUGUI companyText;

    [Header("Contact Card")]
    [Tooltip("Displays phone number")]
    [SerializeField] private TextMeshProUGUI phoneText;

    [Tooltip("Displays email address")]
    [SerializeField] private TextMeshProUGUI emailText;

    [Tooltip("Displays physical address")]
    [SerializeField] private TextMeshProUGUI addressText;

    [Header("Action Buttons")]
    [Tooltip("Button to open LinkedIn profile")]
    [SerializeField] private Button linkedInButton;

    [Tooltip("Button to open portfolio URL")]
    [SerializeField] private Button portfolioButton;

    [Tooltip("Button to view resume PDF")]
    [SerializeField] private Button pdfButton;

    [Tooltip("Button to save contact to device")]
    [SerializeField] private Button saveContactButton;

    [Header("Optional")]
    [Tooltip("Loading spinner shown while profile is being fetched")]
    [SerializeField] private GameObject loadingSpinner;

    [Tooltip("Error message shown when profile fetch fails")]
    [SerializeField] private TextMeshProUGUI errorText;

    // ── State ─────────────────────────────────────────────────────
    private UserProfile _currentProfile;

    // ── Lifecycle ─────────────────────────────────────────────────

    void OnEnable()
    {
        // Wire up button listeners
        linkedInButton?.onClick.AddListener(OnLinkedInClicked);
        portfolioButton?.onClick.AddListener(OnPortfolioClicked);
        pdfButton?.onClick.AddListener(OnPDFClicked);
        saveContactButton?.onClick.AddListener(OnSaveContactClicked);
    }

    void OnDisable()
    {
        // Always remove listeners to prevent memory leaks
        linkedInButton?.onClick.RemoveListener(OnLinkedInClicked);
        portfolioButton?.onClick.RemoveListener(OnPortfolioClicked);
        pdfButton?.onClick.RemoveListener(OnPDFClicked);
        saveContactButton?.onClick.RemoveListener(OnSaveContactClicked);
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Populates all UI elements with data from the given profile.
    /// Call this after a profile is fetched from Firebase.
    /// </summary>
    public void Populate(UserProfile profile)
    {
        if (profile == null)
        {
            ShowError("Profile not found");
            return;
        }

        _currentProfile = profile;

        // Hide loading and error states
        if (loadingSpinner != null) loadingSpinner.SetActive(false);
        if (errorText != null) errorText.gameObject.SetActive(false);

        // Identity
        if (initialsText != null)
            initialsText.text = profile.GetDisplayInitials();

        if (nameText != null)
            nameText.text = profile.name;

        if (roleText != null)
            roleText.text = profile.role;

        if (companyText != null)
            companyText.text = profile.company;

        // Contact
        if (phoneText != null)
            phoneText.text = profile.phone;

        if (emailText != null)
            emailText.text = profile.email;

        if (addressText != null)
            addressText.text = profile.address;

        // Show/hide buttons based on available data
        linkedInButton?.gameObject.SetActive(!string.IsNullOrEmpty(profile.linkedin));
        portfolioButton?.gameObject.SetActive(!string.IsNullOrEmpty(profile.portfolio));
        pdfButton?.gameObject.SetActive(!string.IsNullOrEmpty(profile.pdfUrl));

        Debug.Log($"ProfileCardUI populated for: {profile.name}");
    }

    /// <summary>
    /// Shows loading spinner while profile is being fetched.
    /// Call this immediately after a card is scanned.
    /// </summary>
    public void ShowLoading()
    {
        if (loadingSpinner != null) loadingSpinner.SetActive(true);
        if (errorText != null) errorText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows error message when profile fetch fails.
    /// </summary>
    public void ShowError(string message)
    {
        if (loadingSpinner != null) loadingSpinner.SetActive(false);
        if (errorText != null)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = message;
        }
        Debug.LogWarning($"ProfileCardUI error: {message}");
    }

    // ── Button Handlers ───────────────────────────────────────────

    /// <summary>
    /// Opens LinkedIn profile URL in device browser.
    /// </summary>
    private void OnLinkedInClicked()
    {
        if (_currentProfile == null ||
            string.IsNullOrEmpty(_currentProfile.linkedin)) return;

        Application.OpenURL(_currentProfile.linkedin);
        Debug.Log($"Opening LinkedIn: {_currentProfile.linkedin}");
    }

    /// <summary>
    /// Opens portfolio URL in device browser.
    /// </summary>
    private void OnPortfolioClicked()
    {
        if (_currentProfile == null ||
            string.IsNullOrEmpty(_currentProfile.portfolio)) return;

        Application.OpenURL(_currentProfile.portfolio);
        Debug.Log($"Opening portfolio: {_currentProfile.portfolio}");
    }

    /// <summary>
    /// Opens resume PDF URL in device browser.
    /// Falls back to browser if in-app viewer unavailable.
    /// </summary>
    private void OnPDFClicked()
    {
        if (_currentProfile == null ||
            string.IsNullOrEmpty(_currentProfile.pdfUrl)) return;

        Application.OpenURL(_currentProfile.pdfUrl);
        Debug.Log($"Opening PDF: {_currentProfile.pdfUrl}");
    }

    /// <summary>
    /// Saves current profile as a vCard contact on the device.
    /// Delegates to ContactSaver script.
    /// </summary>
    private void OnSaveContactClicked()
    {
        if (_currentProfile == null) return;
        ContactSaver.Instance?.SaveContact(_currentProfile);
        Debug.Log($"Saving contact: {_currentProfile.name}");
    }
}