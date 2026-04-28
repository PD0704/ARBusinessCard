using UnityEngine;

[CreateAssetMenu(fileName = "AppConfig", menuName = "ARCard/App Config")]
public class AppConfig : ScriptableObject
{
    [Header("Vuforia")]
    public string vuforiaLicenceKey;
    public string vuforiaAccessKey;
    public string vuforiaSecretKey;
    public string vuforiaTargetDatabaseName;

    [Header("Firebase")]
    public string firebaseProjectId;
}