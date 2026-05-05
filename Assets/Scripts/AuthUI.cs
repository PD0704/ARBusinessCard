using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Auth screen UI.
/// Wires login and register buttons to AuthManager.
/// Handles switching between login and register modes.
/// Attach to AuthPanel in the scene.
/// </summary>
public class AuthUI : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private TMP_InputField nameField;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("UI Elements")]
    [Tooltip("NameField is only visible during registration")]
    [SerializeField] private GameObject nameFieldContainer;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    // ── State ─────────────────────────────────────────────────────
    // Tracks whether we are in login or register mode
    private bool _isRegisterMode = false;

    // ── Lifecycle ─────────────────────────────────────────────────

    void OnEnable()
    {
        loginButton?.onClick.AddListener(OnLoginClicked);
        registerButton?.onClick.AddListener(OnRegisterClicked);

        AuthManager.OnLoginSuccess += HandleLoginSuccess;
        AuthManager.OnLoginFailed += HandleLoginFailed;

        // Start in login mode
        SetLoginMode();
    }

    void OnDisable()
    {
        loginButton?.onClick.RemoveListener(OnLoginClicked);
        registerButton?.onClick.RemoveListener(OnRegisterClicked);

        AuthManager.OnLoginSuccess -= HandleLoginSuccess;
        AuthManager.OnLoginFailed -= HandleLoginFailed;
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Handles login button click.
    /// In login mode — logs in.
    /// In register mode — registers.
    /// </summary>
    private async void OnLoginClicked()
    {
        ClearError();

        if (_isRegisterMode)
        {
            await AuthManager.Instance.Register(
                emailField.text,
                passwordField.text,
                nameField.text);
        }
        else
        {
            await AuthManager.Instance.Login(
                emailField.text,
                passwordField.text);
        }
    }

    /// <summary>
    /// Toggles between login and register mode.
    /// Shows/hides name field accordingly.
    /// </summary>
    private void OnRegisterClicked()
    {
        _isRegisterMode = !_isRegisterMode;

        if (_isRegisterMode)
            SetRegisterMode();
        else
            SetLoginMode();
    }

    /// <summary>
    /// Sets UI to login mode — hides name field.
    /// </summary>
    private void SetLoginMode()
    {
        _isRegisterMode = false;
        if (nameFieldContainer != null)
            nameFieldContainer.SetActive(false);
        if (titleText != null)
            titleText.text = "AR Business Card";
        if (subtitleText != null)
            subtitleText.text = "Sign in to continue";
        if (loginButton != null)
            loginButton.GetComponentInChildren<TextMeshProUGUI>().text = "Sign In";
        if (registerButton != null)
            registerButton.GetComponentInChildren<TextMeshProUGUI>().text = "Create Account";
        ClearError();
    }

    /// <summary>
    /// Sets UI to register mode — shows name field.
    /// </summary>
    private void SetRegisterMode()
    {
        _isRegisterMode = true;
        if (nameFieldContainer != null)
            nameFieldContainer.SetActive(true);
        if (titleText != null)
            titleText.text = "Create Account";
        if (subtitleText != null)
            subtitleText.text = "Join AR Business Card";
        if (loginButton != null)
            loginButton.GetComponentInChildren<TextMeshProUGUI>().text = "Register";
        if (registerButton != null)
            registerButton.GetComponentInChildren<TextMeshProUGUI>().text = "Back to Sign In";
        ClearError();
    }

    /// <summary>
    /// Navigates to Home on successful login.
    /// </summary>
    private void HandleLoginSuccess(Firebase.Auth.FirebaseUser user)
    {
        ClearError();
        AppStateManager.Instance?.GoToHome();
    }

    /// <summary>
    /// Shows error message on failed login.
    /// </summary>
    private void HandleLoginFailed(string error)
    {
        if (errorText != null)
        {
            errorText.text = error;
            errorText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Clears error message.
    /// </summary>
    private void ClearError()
    {
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }
}