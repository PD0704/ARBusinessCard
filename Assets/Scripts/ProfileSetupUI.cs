using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the creator profile setup form.
/// Lets users fill in their professional details which are
/// saved to Firestore and used to populate the AR overlay.
/// Attach to the ProfileSetupPanel in the scene.
/// </summary>
public class ProfileSetupUI : MonoBehaviour
{
    public static ProfileSetupUI Instance { get; private set; }

    // ── Inspector References — Input Fields ───────────────────────
    [Header("Input Fields")]
    [Tooltip("Full name input field")]
    [SerializeField] private TMP_InputField nameField;

    [Tooltip("Job role/title input field")]
    [SerializeField] private TMP_InputField roleField;

    [Tooltip("Company name input field")]
    [SerializeField] private TMP_InputField companyField;

    [Tooltip("Phone number input field")]
    [SerializeField] private TMP_InputField phoneField;

    [Tooltip("Email input field")]
    [SerializeField] private TMP_InputField emailField;

    [Tooltip("Physical address input field")]
    [SerializeField] private TMP_InputField addressField;

    [Tooltip("LinkedIn URL input field")]
    [SerializeField] private TMP_InputField linkedinField;

    [Tooltip("Portfolio URL input field")]
    [SerializeField] private TMP_InputField portfolioField;

    [Header("Initials Settings")]
    [Tooltip("Dropdown for choosing 2 or 3 initial style")]
    [SerializeField] private TMP_Dropdown initialsStyleDropdown;

    [Tooltip("Preview text showing computed initials")]
    [SerializeField] private TextMeshProUGUI initialsPreviewText;

    // ── Inspector References — Buttons ────────────────────────────
    [Header("Buttons")]
    [Tooltip("Save profile button")]
    [SerializeField] private Button saveButton;

    [Tooltip("Upload PDF resume button")]
    [SerializeField] private Button uploadPDFButton;

    [Tooltip("Generate card button — triggers CardGenerator")]
    [SerializeField] private Button generateCardButton;

    [Tooltip("Back button — returns to Home")]
    [SerializeField] private Button backButton;

    // ── Inspector References — Feedback ───────────────────────────
    [Header("Feedback")]
    [Tooltip("Loading spinner shown while saving")]
    [SerializeField] private GameObject loadingSpinner;

    [Tooltip("Success message shown after save")]
    [SerializeField] private TextMeshProUGUI feedbackText;

    // ── State ─────────────────────────────────────────────────────
    private UserProfile _currentProfile;
    private bool _isSaving;

    // ── Lifecycle ─────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        saveButton?.onClick.AddListener(OnSaveClicked);
        uploadPDFButton?.onClick.AddListener(OnUploadPDFClicked);
        generateCardButton?.onClick.AddListener(OnGenerateCardClicked);
        backButton?.onClick.AddListener(OnBackClicked);

        // Update initials preview when name changes
        nameField?.onValueChanged.AddListener(OnNameChanged);
        initialsStyleDropdown?.onValueChanged.AddListener(OnInitialsStyleChanged);

        // Load existing profile data into fields
        LoadCurrentProfile();
    }

    void OnDisable()
    {
        saveButton?.onClick.RemoveListener(OnSaveClicked);
        uploadPDFButton?.onClick.RemoveListener(OnUploadPDFClicked);
        generateCardButton?.onClick.RemoveListener(OnGenerateCardClicked);
        backButton?.onClick.RemoveListener(OnBackClicked);
        nameField?.onValueChanged.RemoveListener(OnNameChanged);
        initialsStyleDropdown?.onValueChanged.RemoveListener(OnInitialsStyleChanged);
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Loads current user profile data into all input fields.
    /// Called when the setup screen opens.
    /// </summary>
    public void LoadCurrentProfile()
    {
        var profile = ProfileService.Instance?.CurrentProfile;
        if (profile == null) return;

        _currentProfile = profile;

        if (nameField != null) nameField.text = profile.name;
        if (roleField != null) roleField.text = profile.role;
        if (companyField != null) companyField.text = profile.company;
        if (phoneField != null) phoneField.text = profile.phone;
        if (emailField != null) emailField.text = profile.email;
        if (addressField != null) addressField.text = profile.address;
        if (linkedinField != null) linkedinField.text = profile.linkedin;
        if (portfolioField != null) portfolioField.text = profile.portfolio;

        // Set initials style dropdown
        if (initialsStyleDropdown != null)
            initialsStyleDropdown.value = profile.initialsStyle == "3" ? 1 : 0;

        UpdateInitialsPreview();
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Saves all input field values to Firestore.
    /// Updates both the database and local cache.
    /// </summary>
    private async void OnSaveClicked()
    {
        if (_isSaving) return;
        if (AuthManager.Instance?.CurrentUser == null)
        {
            ShowFeedback("Please log in first", false);
            return;
        }

        _isSaving = true;
        if (loadingSpinner != null) loadingSpinner.SetActive(true);
        ShowFeedback("", false);

        try
        {
            string uid = AuthManager.Instance.CurrentUser.UserId;
            string initialsStyle = initialsStyleDropdown?.value == 1 ? "3" : "2";
            string name = nameField?.text.Trim() ?? "";

            var db = FirebaseManager.Instance.Database;
            var docRef = db.Collection("users").Document(uid);

            var updates = new System.Collections.Generic.Dictionary<string, object>
            {
                { "name", name },
                { "role", roleField?.text.Trim() ?? "" },
                { "company", companyField?.text.Trim() ?? "" },
                { "phone", phoneField?.text.Trim() ?? "" },
                { "email", emailField?.text.Trim() ?? "" },
                { "address", addressField?.text.Trim() ?? "" },
                { "linkedin", linkedinField?.text.Trim() ?? "" },
                { "portfolio", portfolioField?.text.Trim() ?? "" },
                { "initials", ComputeInitials(name, initialsStyle) },
                { "initialsStyle", initialsStyle },
                { "updatedAt", Firebase.Firestore.FieldValue.ServerTimestamp }
            };

            await docRef.UpdateAsync(updates);

            // Refresh local cache
            await ProfileService.Instance.FetchProfile(uid);

            ShowFeedback("Profile saved successfully", true);
            Debug.Log("Profile saved to Firestore");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save profile: {e.Message}");
            ShowFeedback("Failed to save. Check connection.", false);
        }
        finally
        {
            _isSaving = false;
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
        }
    }

    /// <summary>
    /// Opens device file picker to select a PDF resume.
    /// Uploads to Firebase Storage and saves download URL to profile.
    /// </summary>
    private void OnUploadPDFClicked()
    {
        // PDF upload via NativeFilePicker — implemented in Phase 7
        Debug.Log("PDF upload — coming in Phase 7");
        ShowFeedback("PDF upload coming soon", false);
    }

    /// <summary>
    /// Triggers card generation with current profile data.
    /// Saves profile first to ensure latest data is used.
    /// </summary>
    private void OnGenerateCardClicked()
    {
        // CardGenerator handles this — implemented next
        CardGenerator.Instance?.GenerateCard(
            ProfileService.Instance?.CurrentProfile);
    }

    /// <summary>
    /// Returns to Home screen.
    /// </summary>
    private void OnBackClicked()
    {
        AppStateManager.Instance?.GoToHome();
    }

    /// <summary>
    /// Updates initials preview text when name field changes.
    /// </summary>
    private void OnNameChanged(string name)
    {
        UpdateInitialsPreview();
    }

    /// <summary>
    /// Updates initials preview when style dropdown changes.
    /// </summary>
    private void OnInitialsStyleChanged(int index)
    {
        UpdateInitialsPreview();
    }

    /// <summary>
    /// Recomputes and displays initials based on current name and style.
    /// </summary>
    private void UpdateInitialsPreview()
    {
        if (initialsPreviewText == null) return;
        string name = nameField?.text ?? "";
        string style = initialsStyleDropdown?.value == 1 ? "3" : "2";
        initialsPreviewText.text = ComputeInitials(name, style);
    }

    /// <summary>
    /// Computes initials from name based on chosen style.
    /// Style 2 = first + last initial. Style 3 = first + middle + last.
    /// </summary>
    private string ComputeInitials(string name, string style)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        string[] parts = name.Trim().Split(' ');

        if (style == "3" && parts.Length >= 3)
            return $"{parts[0][0]}{parts[1][0]}{parts[2][0]}".ToUpper();
        else if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
        else
            return parts[0][0].ToString().ToUpper();
    }

    /// <summary>
    /// Shows feedback message to user.
    /// Green for success, red for error.
    /// </summary>
    private void ShowFeedback(string message, bool success)
    {
        if (feedbackText == null) return;
        feedbackText.text = message;
        feedbackText.color = success
            ? new Color(0.18f, 0.8f, 0.44f)
            : new Color(0.9f, 0.3f, 0.3f);
        feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    /// <summary>
    /// Computes initials using default style from profile.
    /// </summary>
    private string ComputeInitials(string name)
        => ComputeInitials(name, "2");
}