using Firebase.Firestore;
using System;

[FirestoreData]
[Serializable]
public class UserProfile
{
    [FirestoreProperty] public string uid { get; set; }
    [FirestoreProperty] public string name { get; set; }
    [FirestoreProperty] public string role { get; set; }
    [FirestoreProperty] public string company { get; set; }
    [FirestoreProperty] public string email { get; set; }
    [FirestoreProperty] public string phone { get; set; }
    [FirestoreProperty] public string address { get; set; }
    [FirestoreProperty] public string linkedin { get; set; }
    [FirestoreProperty] public string portfolio { get; set; }
    [FirestoreProperty] public string pdfUrl { get; set; }
    [FirestoreProperty] public string cardImageUrl { get; set; }
    [FirestoreProperty] public string vuforiaTargetId { get; set; }
    [FirestoreProperty] public string initials { get; set; }
    [FirestoreProperty] public string initialsStyle { get; set; }

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