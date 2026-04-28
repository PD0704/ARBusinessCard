using System;

[Serializable]
public class UserProfile
{
    public string uid;
    public string name;
    public string role;
    public string company;
    public string email;
    public string phone;
    public string address;
    public string linkedin;
    public string portfolio;
    public string pdfUrl;
    public string cardImageUrl;
    public string vuforiaTargetId;
    public string initials;
    public string initialsStyle;

    // Empty constructor required for Firestore deserialization
    public UserProfile() { }

    // Check if profile has minimum required data
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(uid) &&
               !string.IsNullOrEmpty(name);
    }

    // Returns display initials based on style preference
    public string GetDisplayInitials()
    {
        if (!string.IsNullOrEmpty(initials))
            return initials;

        if (string.IsNullOrEmpty(name))
            return "?";

        string[] parts = name.Split(' ');
        if (initialsStyle == "3" && parts.Length >= 3)
            return $"{parts[0][0]}{parts[1][0]}{parts[2][0]}";
        else if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[parts.Length - 1][0]}";
        else
            return parts[0][0].ToString();
    }
}