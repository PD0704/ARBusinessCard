using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Populates the business card template UI with profile data.
/// This script sits on the CardTemplate prefab which gets
/// instantiated off-screen, populated, rendered to PNG,
/// then destroyed — all handled by CardGenerator.
/// 
/// The card design matches the physical PD business card:
/// - Black background
/// - Monogram/initials on the left
/// - Vertical divider
/// - Name, role, contact info on the right
/// - QR code in the bottom right corner
/// </summary>
public class CardTemplateUI : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────
    [Header("Left Side")]
    [Tooltip("Large initials/monogram text — e.g. PD")]
    [SerializeField] private TextMeshProUGUI initialsText;

    [Header("Right Side")]
    [Tooltip("Full name text")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("Job role text")]
    [SerializeField] private TextMeshProUGUI roleText;

    [Tooltip("Company name text")]
    [SerializeField] private TextMeshProUGUI companyText;

    [Tooltip("Phone number text")]
    [SerializeField] private TextMeshProUGUI phoneText;

    [Tooltip("Email address text")]
    [SerializeField] private TextMeshProUGUI emailText;

    [Tooltip("Physical address text")]
    [SerializeField] private TextMeshProUGUI addressText;

    [Header("QR Code")]
    [Tooltip("RawImage where the generated QR texture is displayed")]
    [SerializeField] private RawImage qrCodeImage;

    [Header("Optional Branding")]
    [Tooltip("Small 'scan to view AR profile' label below QR")]
    [SerializeField] private TextMeshProUGUI scanPromptText;

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Populates all card fields with data from the given profile.
    /// Called by CardGenerator before rendering to PNG.
    /// </summary>
    public void Populate(UserProfile profile)
    {
        if (profile == null)
        {
            Debug.LogWarning("CardTemplateUI: profile is null");
            return;
        }

        // Left side — monogram
        if (initialsText != null)
            initialsText.text = profile.GetDisplayInitials();

        // Right side — identity
        if (nameText != null)
            nameText.text = profile.name;

        if (roleText != null)
            roleText.text = profile.role;

        if (companyText != null)
            companyText.text = profile.company;

        // Right side — contact
        if (phoneText != null)
            phoneText.text = profile.phone;

        if (emailText != null)
            emailText.text = profile.email;

        if (addressText != null)
            addressText.text = profile.address;

        // Scan prompt
        if (scanPromptText != null)
            scanPromptText.text = "scan to view AR profile";

        // QR is set separately by CardGenerator via QRGenerator
        // qrCodeImage is populated by QRGenerator.GenerateAndDisplay()

        Debug.Log($"CardTemplateUI populated for: {profile.name}");
    }

    /// <summary>
    /// Returns the RawImage component used for QR display.
    /// CardGenerator uses this to embed the QR texture.
    /// </summary>
    public RawImage GetQRTarget() => qrCodeImage;
}