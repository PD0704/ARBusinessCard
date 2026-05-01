using System;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;

/// <summary>
/// Generates a QR code texture from a user's unique profile URL.
/// URL format: arcard.app/u/{uid}
/// The generated QR texture is used by CardGenerator to embed
/// into the downloadable business card image.
/// Attach to the Managers GameObject or any persistent GameObject.
/// </summary>
public class QRGenerator : MonoBehaviour
{
    public static QRGenerator Instance { get; private set; }

    // ── Inspector Settings ────────────────────────────────────────
    [Header("QR Settings")]
    [Tooltip("Size of the generated QR texture in pixels")]
    [SerializeField] private int qrSize = 256;

    [Tooltip("Base URL for profile links")]
    [SerializeField] private string baseUrl = "https://arcard.app/u/";

    [Tooltip("Quiet zone margin around QR in pixels — minimum 4 recommended")]
    [SerializeField] private int quietZone = 10;

    // ── Events ────────────────────────────────────────────────────
    // Fired when QR is successfully generated
    public static event Action<Texture2D> OnQRGenerated;

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

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Generates a QR code texture for the given user UID.
    /// Returns the texture and fires OnQRGenerated event.
    /// Call this before generating the card image.
    /// </summary>
    public Texture2D GenerateQR(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("Cannot generate QR — UID is empty");
            return null;
        }

        string url = $"{baseUrl}{uid}";
        Debug.Log($"Generating QR for: {url}");

        try
        {
            Texture2D qrTexture = GenerateQRTexture(url);
            OnQRGenerated?.Invoke(qrTexture);
            return qrTexture;
        }
        catch (Exception e)
        {
            Debug.LogError($"QR generation failed: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates QR and displays it on a RawImage component.
    /// Convenience method for showing QR in UI.
    /// </summary>
    public void GenerateAndDisplay(string uid, RawImage displayTarget)
    {
        Texture2D qr = GenerateQR(uid);
        if (qr != null && displayTarget != null)
            displayTarget.texture = qr;
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Core QR generation using ZXing.
    /// Creates a black and white Texture2D with the encoded URL.
    /// White background with black modules — standard QR format.
    /// </summary>
    private Texture2D GenerateQRTexture(string content)
    {
        // Configure ZXing QR writer
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width = qrSize,
                Height = qrSize,
                Margin = quietZone,
                // Error correction level H = 30% damage tolerance
                // Higher correction = larger QR but more scannable when worn
                ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.H
            }
        };

        var pixelData = writer.Write(content);

        // Create Unity texture from pixel data
        Texture2D texture = new Texture2D(qrSize, qrSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point; // Sharp pixels, no blur

        Color32[] pixels = new Color32[pixelData.Pixels.Length / 4];
        for (int i = 0; i < pixels.Length; i++)
        {
            // ZXing returns BGRA — convert to Unity's RGBA
            byte b = pixelData.Pixels[i * 4];
            byte g = pixelData.Pixels[i * 4 + 1];
            byte r = pixelData.Pixels[i * 4 + 2];
            byte a = pixelData.Pixels[i * 4 + 3];

            // Invert Y axis — Unity textures are bottom-up
            int x = i % qrSize;
            int y = qrSize - 1 - (i / qrSize);
            pixels[y * qrSize + x] = new Color32(r, g, b, a);
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Debug.Log($"QR texture generated: {qrSize}x{qrSize}");
        return texture;
    }
}