using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the creator profile setup form.
/// Populates fields from CurrentProfile when opened.
/// Saves to Firestore and refreshes CurrentProfile on save.
/// </summary>
public class ProfileSetupUI : MonoBehaviour
{
    public static ProfileSetupUI Instance { get; private set; }

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private TMP_InputField roleField;
    [SerializeField] private TMP_InputField companyField;
    [SerializeField] private TMP_InputField phoneField;
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField addressField;
    [SerializeField] private TMP_InputField linkedinField;
    [SerializeField] private TMP_InputField portfolioField;

    [Header("Initials Settings")]
    [SerializeField] private TMP_Dropdown initialsStyleDropdown;
    [SerializeField] private TextMeshProUGUI initialsPreviewText;

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button uploadPDFButton;
    [SerializeField] private Button generateCardButton;
    [SerializeField] private Button backButton;

    [Header("Feedback")]
    [SerializeField] private GameObject loadingSpinner;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private UserProfile _currentProfile;
    private bool _isSaving;

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
        nameField?.onValueChanged.AddListener(OnNameChanged);
        initialsStyleDropdown?.onValueChanged.AddListener(OnInitialsStyleChanged);

        // Try to populate immediately from CurrentProfile
        var profile = ProfileService.Instance?.CurrentProfile;
        if (profile != null)
        {
            Debug.Log($"ProfileSetupUI OnEnable — populating from CurrentProfile: {profile.name}");
            PopulateFields(profile);
        }
        else
        {
            Debug.Log("ProfileSetupUI OnEnable — no CurrentProfile, fetching...");
            FetchAndPopulate();
        }
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

    private async void FetchAndPopulate()
    {
        var uid = AuthManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        ShowFeedback("Loading...", true);
        await ProfileService.Instance.FetchProfile(uid);

        var profile = ProfileService.Instance.CurrentProfile;
        if (profile != null)
        {
            PopulateFields(profile);
            ShowFeedback("", false);
        }
        else
        {
            ShowFeedback("Could not load profile", false);
        }
    }

    private void PopulateFields(UserProfile profile)
    {
        _currentProfile = profile;
        if (nameField != null) nameField.text = profile.name ?? "";
        if (roleField != null) roleField.text = profile.role ?? "";
        if (companyField != null) companyField.text = profile.company ?? "";
        if (phoneField != null) phoneField.text = profile.phone ?? "";
        if (emailField != null) emailField.text = profile.email ?? "";
        if (addressField != null) addressField.text = profile.address ?? "";
        if (linkedinField != null) linkedinField.text = profile.linkedin ?? "";
        if (portfolioField != null) portfolioField.text = profile.portfolio ?? "";
        if (initialsStyleDropdown != null)
            initialsStyleDropdown.value = profile.initialsStyle == "3" ? 1 : 0;
        UpdateInitialsPreview();
        Debug.Log($"PopulateFields complete: {profile.name}");
    }

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
        ShowFeedback("Saving...", true);

        try
        {
            string uid = AuthManager.Instance.CurrentUser.UserId;
            string initialsStyle = initialsStyleDropdown?.value == 1 ? "3" : "2";
            string name = nameField?.text.Trim() ?? "";

            Debug.Log($"Saving profile — name: '{name}'");

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
            Debug.Log("Firestore update complete");

            // Clear cache so next fetch gets fresh data
            ProfileCache.Instance?.ClearProfile(uid);

            // Wait for Firestore to propagate
            await Task.Delay(500);

            // Fetch fresh profile
            await ProfileService.Instance.FetchProfile(uid);
            _currentProfile = ProfileService.Instance.CurrentProfile;

            Debug.Log($"After save — CurrentProfile: {_currentProfile?.name}");
            ShowFeedback("Profile saved!", true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
            ShowFeedback("Failed to save. Check connection.", false);
        }
        finally
        {
            _isSaving = false;
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
        }
    }

    private void OnUploadPDFClicked()
    {
        ShowFeedback("PDF upload coming soon", false);
    }

    private void OnGenerateCardClicked()
    {
        var profile = ProfileService.Instance?.CurrentProfile ?? _currentProfile;

        if (profile == null || string.IsNullOrEmpty(profile.name))
        {
            ShowFeedback("Please save your profile first", false);
            return;
        }

        Debug.Log($"Generating card for: {profile.name}");
        ShowFeedback("Generating card...", true);
        CardGenerator.Instance?.GenerateCard(profile);
    }

    private void OnBackClicked()
    {
        AppStateManager.Instance?.GoToHome();
    }

    private void OnNameChanged(string name) => UpdateInitialsPreview();
    private void OnInitialsStyleChanged(int index) => UpdateInitialsPreview();

    private void UpdateInitialsPreview()
    {
        if (initialsPreviewText == null) return;
        string name = nameField?.text ?? "";
        string style = initialsStyleDropdown?.value == 1 ? "3" : "2";
        initialsPreviewText.text = ComputeInitials(name, style);
    }

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

    private string ComputeInitials(string name) => ComputeInitials(name, "2");

    private void ShowFeedback(string message, bool success)
    {
        if (feedbackText == null) return;
        feedbackText.text = message;
        feedbackText.color = success
            ? new Color(0.18f, 0.8f, 0.44f)
            : new Color(0.9f, 0.3f, 0.3f);
        feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
}