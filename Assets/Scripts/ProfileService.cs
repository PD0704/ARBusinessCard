using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Orchestrates profile fetching logic.
/// Priority: Cache → Firebase → Error
/// All other scripts request profiles through this service.
/// </summary>
public class ProfileService : MonoBehaviour
{
    public static ProfileService Instance { get; private set; }

    // Broadcast when a profile is successfully fetched
    public static event Action<UserProfile> OnProfileFetched;

    // Broadcast when fetch fails completely
    public static event Action<string> OnProfileFetchFailed;

    // Currently loaded profile
    public UserProfile CurrentProfile { get; private set; }

    void Awake()
    {
        // Singleton pattern — only one ProfileService exists at a time
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Main entry point. Call this with a UID whenever a card is scanned.
    /// Checks cache first, then Firebase if cache is empty or expired.
    /// </summary>
    public async Task FetchProfile(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            OnProfileFetchFailed?.Invoke("UID is empty");
            return;
        }

        Debug.Log($"Fetching profile for uid: {uid}");

        // Step 1 — check cache first (works offline)
        UserProfile cached = ProfileCache.Instance.LoadProfile(uid);
        if (cached != null)
        {
            Debug.Log("Profile loaded from cache");
            CurrentProfile = cached;
            OnProfileFetched?.Invoke(cached);
            return;
        }

        // Step 2 — cache miss, fetch from Firebase
        if (!FirebaseManager.Instance.IsInitialized)
        {
            OnProfileFetchFailed?.Invoke("Firebase not ready");
            return;
        }

        UserProfile profile = await FirebaseManager.Instance.FetchProfile(uid);

        // Step 3 — save to cache and broadcast result
        if (profile != null)
        {
            ProfileCache.Instance.SaveProfile(profile);
            CurrentProfile = profile;
            OnProfileFetched?.Invoke(profile);
        }
        else
        {
            OnProfileFetchFailed?.Invoke($"No profile found for uid: {uid}");
        }
    }

    /// <summary>
    /// Clears the current profile and its cache.
    /// Call this when user logs out or scans a new card.
    /// </summary>
    public void ClearCurrentProfile()
    {
        if (CurrentProfile != null)
        {
            ProfileCache.Instance.ClearProfile(CurrentProfile.uid);
            CurrentProfile = null;
        }
    }
}