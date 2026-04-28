using System;
using System.Threading;
using UnityEngine;
using ZXing;
using ZXing.QrCode;
using ZXing.Common;
/// <summary>
/// Scans camera feed for QR codes using ZXing.
/// Extracts user UID from scanned URL format: arcard.app/u/{uid}
/// Fires OnUIDScanned event when a valid UID is found.
/// Does not handle what happens after — other scripts listen to the event.
/// </summary>
public class QRScanner : MonoBehaviour
{
    // ── Inspector Settings ────────────────────────────────────────
    [Header("Scan Settings")]
    [Tooltip("How often to scan in seconds. Lower = faster but more CPU usage")]
    [SerializeField] private float scanInterval = 0.5f;

    [Tooltip("Width of camera texture sample for QR decoding")]
    [SerializeField] private int scanWidth = 256;

    [Tooltip("Height of camera texture sample for QR decoding")]
    [SerializeField] private int scanHeight = 256;

    // ── Events ────────────────────────────────────────────────────
    // Fired when a valid UID is extracted from a scanned QR code
    public static event Action<string> OnUIDScanned;

    // Fired when QR content is scanned but format is unrecognized
    public static event Action<string> OnInvalidQRScanned;

    // ── Private State ─────────────────────────────────────────────
    private WebCamTexture _camTexture;
    private BarcodeReader<ZXing.Common.BitMatrix> _reader;
    private float _scanTimer;
    private bool _isScanning;
    private string _lastScannedUID = "";

    // Expected URL prefix in QR codes
    private const string QR_URL_PREFIX = "arcard.app/u/";

    // ── Lifecycle ─────────────────────────────────────────────────

    void Start()
    {
        // Initialize ZXing barcode reader
        _reader = new BarcodeReader<ZXing.Common.BitMatrix>(null, null, null)
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };
    }

    void Update()
    {
        if (!_isScanning) return;

        // Throttle scanning to save CPU
        _scanTimer += Time.deltaTime;
        if (_scanTimer < scanInterval) return;
        _scanTimer = 0f;

        ScanFrame();
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Starts the camera and begins scanning for QR codes.
    /// Call this when entering QR scan mode.
    /// </summary>
    public void StartScanning()
    {
        if (_isScanning) return;

        // Request camera permission and start feed
        _camTexture = new WebCamTexture(scanWidth, scanHeight);
        _camTexture.Play();
        _isScanning = true;
        _lastScannedUID = "";
        Debug.Log("QR scanning started");
    }

    /// <summary>
    /// Stops the camera and scanning loop.
    /// Call this when leaving QR scan mode or a card is found.
    /// </summary>
    public void StopScanning()
    {
        if (!_isScanning) return;

        _isScanning = false;
        if (_camTexture != null && _camTexture.isPlaying)
        {
            _camTexture.Stop();
        }
        Debug.Log("QR scanning stopped");
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Grabs current camera frame and attempts QR decode.
    /// Runs on main thread — kept lightweight intentionally.
    /// </summary>
    private void ScanFrame()
    {
        if (_camTexture == null || !_camTexture.isPlaying) return;

        // Grab pixels from camera texture
        Color32[] pixels = _camTexture.GetPixels32();
        if (pixels == null || pixels.Length == 0) return;

        // Convert Color32 array to byte array for ZXing
        byte[] byteArray = new byte[pixels.Length * 4];
        for (int i = 0; i < pixels.Length; i++)
        {
            byteArray[i * 4] = pixels[i].r;
            byteArray[i * 4 + 1] = pixels[i].g;
            byteArray[i * 4 + 2] = pixels[i].b;
            byteArray[i * 4 + 3] = pixels[i].a;
        }

        // Attempt decode
        var result = _reader.Decode(byteArray, _camTexture.width,
            _camTexture.height, RGBLuminanceSource.BitmapFormat.RGBA32);

        if (result == null) return;

        string scannedText = result.Text;
        Debug.Log($"QR decoded: {scannedText}");

        // Extract UID from URL
        string uid = ExtractUID(scannedText);

        if (!string.IsNullOrEmpty(uid))
        {
            if (uid == _lastScannedUID) return;
            _lastScannedUID = uid;

            Debug.Log($"Valid UID found: {uid}");
            StopScanning();
            OnUIDScanned?.Invoke(uid);
        }
        else
        {
            OnInvalidQRScanned?.Invoke(scannedText);
        }
    }

    /// <summary>
    /// Extracts UID from URL format: arcard.app/u/{uid}
    /// Returns empty string if format doesn't match.
    /// </summary>
    private string ExtractUID(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";

        // Handle both http and https variants
        url = url.Replace("https://", "").Replace("http://", "");

        int prefixIndex = url.IndexOf(QR_URL_PREFIX,
            StringComparison.OrdinalIgnoreCase);

        if (prefixIndex < 0) return "";

        string uid = url.Substring(prefixIndex + QR_URL_PREFIX.Length).Trim();
        return string.IsNullOrEmpty(uid) ? "" : uid;
    }

    void OnDestroy()
    {
        StopScanning();
    }
}