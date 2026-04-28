using UnityEngine;
using System;

/// <summary>
/// Controls switching between two scan modes:
/// - QR Mode: uses ZXing to scan QR codes from camera
/// - AR Mode: uses Vuforia image tracking to detect business cards
/// Only one mode is active at a time.
/// UI buttons call ActivateQRMode() and ActivateARMode().
/// </summary>
public class ScanModeController : MonoBehaviour
{
    public static ScanModeController Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────
    [Header("Component References")]
    [Tooltip("QRScanner component in the scene")]
    [SerializeField] private QRScanner qrScanner;

    [Tooltip("Root GameObject of the AR camera/Vuforia setup")]
    [SerializeField] private GameObject arCameraRoot;

    [Tooltip("UI panel shown during QR scanning")]
    [SerializeField] private GameObject qrScanUI;

    [Tooltip("UI panel shown during AR scanning")]
    [SerializeField] private GameObject arScanUI;

    // ── Events ────────────────────────────────────────────────────
    // Fired whenever scan mode changes
    public static event Action<ScanMode> OnScanModeChanged;

    // ── State ─────────────────────────────────────────────────────
    public ScanMode CurrentMode { get; private set; } = ScanMode.None;

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
        // Listen for successful QR scan to auto-switch off QR mode
        QRScanner.OnUIDScanned += HandleUIDScanned;
    }

    void OnDisable()
    {
        QRScanner.OnUIDScanned -= HandleUIDScanned;
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Activates QR scanning mode.
    /// Disables AR camera, enables QR scanner and QR UI.
    /// </summary>
    public void ActivateQRMode()
    {
        if (CurrentMode == ScanMode.QR) return;

        DeactivateAll();
        CurrentMode = ScanMode.QR;

        if (arCameraRoot != null) arCameraRoot.SetActive(false);
        if (qrScanUI != null) qrScanUI.SetActive(true);
        if (qrScanner != null) qrScanner.StartScanning();

        Debug.Log("Scan mode: QR");
        OnScanModeChanged?.Invoke(ScanMode.QR);
    }

    /// <summary>
    /// Activates AR image tracking mode.
    /// Disables QR scanner, enables Vuforia AR camera and AR UI.
    /// </summary>
    public void ActivateARMode()
    {
        if (CurrentMode == ScanMode.AR) return;

        DeactivateAll();
        CurrentMode = ScanMode.AR;

        if (arCameraRoot != null) arCameraRoot.SetActive(true);
        if (arScanUI != null) arScanUI.SetActive(true);

        Debug.Log("Scan mode: AR");
        OnScanModeChanged?.Invoke(ScanMode.AR);
    }

    /// <summary>
    /// Deactivates all scanning — call when showing profile overlay
    /// or navigating away from scan screen.
    /// </summary>
    public void DeactivateAll()
    {
        if (qrScanner != null) qrScanner.StopScanning();
        if (arCameraRoot != null) arCameraRoot.SetActive(false);
        if (qrScanUI != null) qrScanUI.SetActive(false);
        if (arScanUI != null) arScanUI.SetActive(false);

        CurrentMode = ScanMode.None;
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// When QR scanner finds a UID, stop scanning and let
    /// ProfileService take over fetching the profile.
    /// </summary>
    private void HandleUIDScanned(string uid)
    {
        DeactivateAll();
        _ = ProfileService.Instance.FetchProfile(uid);
    }
}

/// <summary>
/// Represents which scanning mode is currently active.
/// </summary>
public enum ScanMode
{
    None,   // No scanning active
    QR,     // ZXing QR code scanning
    AR      // Vuforia image tracking
}