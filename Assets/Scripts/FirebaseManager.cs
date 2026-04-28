using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private FirebaseApp _app;
    private FirebaseAuth _auth;
    private FirebaseFirestore _db;

    public bool IsInitialized { get; private set; }

    public static event Action OnFirebaseReady;
    public static event Action<string> OnFirebaseError;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeFirebase();
    }

    private async void InitializeFirebase()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                _app = FirebaseApp.DefaultInstance;
                _auth = FirebaseAuth.DefaultInstance;
                _db = FirebaseFirestore.DefaultInstance;
                IsInitialized = true;
                Debug.Log("Firebase initialized successfully");
                OnFirebaseReady?.Invoke();
            }
            else
            {
                string error = $"Firebase dependencies not available: {dependencyStatus}";
                Debug.LogError(error);
                OnFirebaseError?.Invoke(error);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Firebase initialization failed: {e.Message}");
            OnFirebaseError?.Invoke(e.Message);
        }
    }

    public async Task<UserProfile> FetchProfile(string uid)
    {
        if (!IsInitialized)
        {
            Debug.LogError("Firebase not initialized yet");
            return null;
        }

        try
        {
            DocumentReference docRef = _db.Collection("users").Document(uid);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                UserProfile profile = snapshot.ConvertTo<UserProfile>();
                profile.uid = uid;
                Debug.Log($"Profile fetched: {profile.name}");
                return profile;
            }
            else
            {
                Debug.LogWarning($"No profile found for uid: {uid}");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to fetch profile: {e.Message}");
            return null;
        }
    }

    public FirebaseAuth Auth => _auth;
    public FirebaseFirestore Database => _db;
}