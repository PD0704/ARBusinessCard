using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Scan screen UI.
/// Handles back button and QR/AR mode toggle.
/// Shows hint text based on current scan mode.
/// Attach to ScanPanel in the scene.
/// </summary>
public class ScanUI : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────
    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button modeToggleButton;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI modeToggleText;
    [SerializeField] private TextMeshProUGUI hintText;

    // ── Lifecycle ─────────────────────────────────────────────────

    void OnEnable()
    {
        backButton?.onClick.AddListener(OnBackClicked);
        modeToggleButton?.onClick.AddListener(OnModeToggleClicked);

        ScanModeController.OnScanModeChanged += HandleModeChanged;

        // Default to AR mode when screen opens
        ScanModeController.Instance?.ActivateARMode();
        UpdateModeUI(ScanMode.AR);
    }

    void OnDisable()
    {
        backButton?.onClick.RemoveListener(OnBackClicked);
        modeToggleButton?.onClick.RemoveListener(OnModeToggleClicked);

        ScanModeController.OnScanModeChanged -= HandleModeChanged;

        // Stop scanning when leaving screen
        ScanModeController.Instance?.DeactivateAll();
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Returns to Home screen and stops scanning.
    /// </summary>
    private void OnBackClicked()
    {
        ScanModeController.Instance?.DeactivateAll();
        AppStateManager.Instance?.GoToHome();
    }

    /// <summary>
    /// Toggles between QR and AR scan modes.
    /// </summary>
    private void OnModeToggleClicked()
    {
        var current = ScanModeController.Instance?.CurrentMode;

        if (current == ScanMode.AR)
            ScanModeController.Instance?.ActivateQRMode();
        else
            ScanModeController.Instance?.ActivateARMode();
    }

    /// <summary>
    /// Updates UI when scan mode changes.
    /// </summary>
    private void HandleModeChanged(ScanMode mode)
    {
        UpdateModeUI(mode);
    }

    /// <summary>
    /// Updates toggle button text and hint based on current mode.
    /// </summary>
    private void UpdateModeUI(ScanMode mode)
    {
        if (mode == ScanMode.AR)
        {
            if (modeToggleText != null)
                modeToggleText.text = "QR Mode";
            if (hintText != null)
                hintText.text = "Point camera at a business card";
        }
        else
        {
            if (modeToggleText != null)
                modeToggleText.text = "AR Mode";
            if (hintText != null)
                hintText.text = "Point camera at a QR code";
        }
    }
}