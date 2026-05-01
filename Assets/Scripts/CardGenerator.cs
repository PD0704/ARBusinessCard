using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the business card UI template to a PNG image.
/// Works like GPay's QR share — generates a card image the
/// creator can download, share on WhatsApp, or print.
/// 
/// Flow:
/// 1. Populate CardTemplate prefab with profile data
/// 2. Generate QR and embed it in the card
/// 3. Render card UI to RenderTexture
/// 4. Convert RenderTexture to Texture2D
/// 5. Encode as PNG and save to device
/// 6. Open share sheet
/// 
/// Attach to the Managers GameObject in the scene.
/// </summary>
public class CardGenerator : MonoBehaviour
{
    public static CardGenerator Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────
    [Header("Card Template")]
    [Tooltip("The card UI prefab to render — matches PD business card design")]
    [SerializeField] private GameObject cardTemplatePrefab;

    [Tooltip("RawImage in the card template where QR code is displayed")]
    [SerializeField] private RawImage qrDisplayTarget;

    [Header("Render Settings")]
    [Tooltip("Width of the output card image in pixels")]
    [SerializeField] private int cardWidth = 1050;

    [Tooltip("Height of the output card image in pixels")]
    [SerializeField] private int cardHeight = 600;

    // ── Events ────────────────────────────────────────────────────
    // Fired when card image is successfully saved
    public static event Action<string> OnCardGenerated;

    // Fired when card generation fails
    public static event Action<string> OnCardGenerationFailed;

    // ── State ─────────────────────────────────────────────────────
    private GameObject _cardInstance;
    private RenderTexture _renderTexture;

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

    void OnDestroy()
    {
        CleanUp();
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Main entry point. Generates a card image for the given profile.
    /// Call from ProfileSetupUI when user taps "Generate Card".
    /// </summary>
    public void GenerateCard(UserProfile profile)
    {
        if (profile == null)
        {
            OnCardGenerationFailed?.Invoke("Profile is null");
            return;
        }

        if (cardTemplatePrefab == null)
        {
            OnCardGenerationFailed?.Invoke("Card template prefab not assigned");
            return;
        }

        Debug.Log($"Generating card for: {profile.name}");

        try
        {
            // Step 1 — Instantiate card template off-screen
            SetupCardTemplate(profile);

            // Step 2 — Generate and embed QR code
            EmbedQRCode(profile.uid);

            // Step 3 — Wait one frame for UI to render, then capture
            StartCoroutine(CaptureCardCoroutine(profile));
        }
        catch (Exception e)
        {
            Debug.LogError($"Card generation failed: {e.Message}");
            OnCardGenerationFailed?.Invoke(e.Message);
            CleanUp();
        }
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Instantiates the card template and populates it with profile data.
    /// Places it off-screen so it renders without being visible.
    /// </summary>
    private void SetupCardTemplate(UserProfile profile)
    {
        // Clean up any previous instance
        if (_cardInstance != null)
            Destroy(_cardInstance);

        // Instantiate off-screen at position far from camera
        _cardInstance = Instantiate(cardTemplatePrefab,
            new Vector3(9999, 9999, 9999), Quaternion.identity);

        // Populate card text fields
        var cardUI = _cardInstance.GetComponent<CardTemplateUI>();
        if (cardUI != null)
            cardUI.Populate(profile);
        else
            Debug.LogWarning("CardTemplateUI component not found on prefab");
    }

    /// <summary>
    /// Generates QR code for the user's UID and embeds it in the card.
    /// </summary>
    private void EmbedQRCode(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return;

        var qrGenerator = QRGenerator.Instance;
        if (qrGenerator == null)
        {
            Debug.LogWarning("QRGenerator not found — card will have no QR");
            return;
        }

        // Find QR display target inside card template
        var qrImage = _cardInstance.GetComponentInChildren<RawImage>();
        if (qrImage != null)
            qrGenerator.GenerateAndDisplay(uid, qrImage);
    }

    /// <summary>
    /// Waits one frame for UI to fully render, then captures to PNG.
    /// Coroutine needed because UI rendering happens at end of frame.
    /// </summary>
    private System.Collections.IEnumerator CaptureCardCoroutine(UserProfile profile)
    {
        // Wait for end of frame so UI is fully rendered
        yield return new WaitForEndOfFrame();

        try
        {
            // Step 4 — Render card UI to RenderTexture
            _renderTexture = new RenderTexture(cardWidth, cardHeight, 24);
            var tempCamera = CreateOffscreenCamera();
            tempCamera.targetTexture = _renderTexture;
            tempCamera.Render();

            // Step 5 — Read pixels from RenderTexture
            RenderTexture.active = _renderTexture;
            Texture2D cardTexture = new Texture2D(cardWidth, cardHeight,
                TextureFormat.RGB24, false);
            cardTexture.ReadPixels(new Rect(0, 0, cardWidth, cardHeight), 0, 0);
            cardTexture.Apply();
            RenderTexture.active = null;

            // Step 6 — Encode to PNG and save
            byte[] pngBytes = cardTexture.EncodeToPNG();
            string filePath = SaveCardImage(pngBytes, profile.name);

            // Step 7 — Share the image
            ShareCardImage(filePath);

            Debug.Log($"Card generated: {filePath}");
            OnCardGenerated?.Invoke(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Card capture failed: {e.Message}");
            OnCardGenerationFailed?.Invoke(e.Message);
        }
        finally
        {
            CleanUp();
        }
    }

    /// <summary>
    /// Creates a temporary camera pointing at the card template
    /// for off-screen rendering.
    /// </summary>
    private Camera CreateOffscreenCamera()
    {
        var camGO = new GameObject("OffscreenCamera");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.cullingMask = LayerMask.GetMask("UI");
        cam.transform.position = new Vector3(9999, 9999, 9998);
        return cam;
    }

    /// <summary>
    /// Saves PNG bytes to device persistent storage.
    /// Returns the file path for sharing.
    /// </summary>
    private string SaveCardImage(byte[] pngBytes, string profileName)
    {
        string sanitized = SanitizeFileName(profileName);
        string fileName = $"ARCard_{sanitized}.png";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(filePath, pngBytes);
        Debug.Log($"Card image saved: {filePath}");
        return filePath;
    }

    /// <summary>
    /// Opens Android share sheet with the card image.
    /// Allows sharing to WhatsApp, email, saving to gallery, etc.
    /// </summary>
    private void ShareCardImage(string filePath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass(
                "com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>(
                "currentActivity");

            AndroidJavaObject intent = new AndroidJavaObject(
                "android.content.Intent");
            intent.Call<AndroidJavaObject>("setAction",
                "android.intent.action.SEND");

            AndroidJavaObject uri = new AndroidJavaClass("android.net.Uri")
                .CallStatic<AndroidJavaObject>("parse", $"file://{filePath}");

            intent.Call<AndroidJavaObject>("setType", "image/png");
            intent.Call<AndroidJavaObject>("putExtra",
                "android.intent.extra.STREAM", uri);
            intent.Call<AndroidJavaObject>("addFlags", 1);

            AndroidJavaObject chooser = AndroidJavaClass
                .CallStatic<AndroidJavaObject>(
                "android.content.Intent",
                "createChooser", intent, "Share your AR Business Card");

            activity.Call("startActivity", chooser);
            Debug.Log("Share sheet opened");
        }
        catch (Exception e)
        {
            Debug.LogError($"Share failed: {e.Message}");
        }
#else
        // In editor — just log the path
        Debug.Log($"[Editor] Card would be shared from: {filePath}");
#endif
    }

    /// <summary>
    /// Removes invalid characters from file names.
    /// </summary>
    private string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "card";
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
        return name.Trim();
    }

    /// <summary>
    /// Destroys temporary objects created during generation.
    /// Always called in finally block to prevent memory leaks.
    /// </summary>
    private void CleanUp()
    {
        if (_cardInstance != null)
        {
            Destroy(_cardInstance);
            _cardInstance = null;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            _renderTexture = null;
        }
    }
}