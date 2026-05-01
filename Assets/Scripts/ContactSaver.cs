using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Saves a UserProfile as a vCard (.vcf) contact file on the device.
/// vCard format is universally supported by Android and iOS contacts apps.
/// On Android, writes to persistent storage then opens with intent.
/// </summary>
public class ContactSaver : MonoBehaviour
{
    public static ContactSaver Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    // Fired when contact is saved successfully
    public static event Action<string> OnContactSaved;

    // Fired when contact save fails
    public static event Action<string> OnContactSaveFailed;

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
    /// Saves the given profile as a vCard contact on the device.
    /// Creates a .vcf file and opens it with the device contacts app.
    /// </summary>
    public void SaveContact(UserProfile profile)
    {
        if (profile == null)
        {
            OnContactSaveFailed?.Invoke("Profile is null");
            return;
        }

        try
        {
            string vCard = BuildVCard(profile);
            string fileName = $"{SanitizeFileName(profile.name)}.vcf";
            string filePath = Path.Combine(
                Application.persistentDataPath, fileName);

            // Write vCard to device storage
            File.WriteAllText(filePath, vCard);
            Debug.Log($"vCard saved to: {filePath}");

            // Open with device contacts app
            OpenVCardOnDevice(filePath);

            OnContactSaved?.Invoke(profile.name);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save contact: {e.Message}");
            OnContactSaveFailed?.Invoke(e.Message);
        }
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Builds a vCard 3.0 string from the given profile.
    /// vCard 3.0 is the most widely supported version on Android.
    /// </summary>
    private string BuildVCard(UserProfile profile)
    {
        // Split name into first and last for vCard N field
        string[] nameParts = profile.name?.Split(' ') ?? new[] { "Unknown" };
        string firstName = nameParts[0];
        string lastName = nameParts.Length > 1
            ? string.Join(" ", nameParts, 1, nameParts.Length - 1)
            : "";

        string vCard = "BEGIN:VCARD\n";
        vCard += "VERSION:3.0\n";

        // Full name
        vCard += $"FN:{profile.name}\n";

        // Structured name: Last;First;Middle;Prefix;Suffix
        vCard += $"N:{lastName};{firstName};;;\n";

        // Organization / company
        if (!string.IsNullOrEmpty(profile.company))
            vCard += $"ORG:{profile.company}\n";

        // Job title / role
        if (!string.IsNullOrEmpty(profile.role))
            vCard += $"TITLE:{profile.role}\n";

        // Phone
        if (!string.IsNullOrEmpty(profile.phone))
            vCard += $"TEL;TYPE=CELL:{profile.phone}\n";

        // Email
        if (!string.IsNullOrEmpty(profile.email))
            vCard += $"EMAIL:{profile.email}\n";

        // Address
        if (!string.IsNullOrEmpty(profile.address))
            vCard += $"ADR;TYPE=HOME:;;{profile.address};;;;\n";

        // LinkedIn as URL
        if (!string.IsNullOrEmpty(profile.linkedin))
            vCard += $"URL;TYPE=LinkedIn:{profile.linkedin}\n";

        // Portfolio as URL
        if (!string.IsNullOrEmpty(profile.portfolio))
            vCard += $"URL;TYPE=Portfolio:{profile.portfolio}\n";

        // Note with app credit
        vCard += "NOTE:Shared via AR Business Card app\n";

        vCard += "END:VCARD\n";
        return vCard;
    }

    /// <summary>
    /// Opens the saved .vcf file using Android's intent system.
    /// This triggers the device contacts app to import the vCard.
    /// </summary>
    private void OpenVCardOnDevice(string filePath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Use Android intent to open .vcf file with contacts app
            AndroidJavaClass unityPlayer = new AndroidJavaClass(
                "com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>(
                "currentActivity");

            AndroidJavaObject intent = new AndroidJavaObject(
                "android.content.Intent");
            intent.Call<AndroidJavaObject>("setAction",
                "android.intent.action.VIEW");

            AndroidJavaObject uri = new AndroidJavaClass(
                "android.net.Uri").CallStatic<AndroidJavaObject>(
                "parse", $"file://{filePath}");

            intent.Call<AndroidJavaObject>("setDataAndType",
                uri, "text/x-vcard");
            intent.Call<AndroidJavaObject>("addFlags", 1);

            activity.Call("startActivity", intent);
            Debug.Log("vCard opened with contacts app");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to open vCard on device: {e.Message}");
        }
#else
        // In editor — just log the path for testing
        Debug.Log($"[Editor] vCard would open at: {filePath}");
#endif
    }

    /// <summary>
    /// Removes characters that are invalid in file names.
    /// </summary>
    private string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "contact";
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
        return name.Trim();
    }
}