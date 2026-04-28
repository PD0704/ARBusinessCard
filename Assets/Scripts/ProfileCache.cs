using UnityEngine;

public class ProfileCache : MonoBehaviour
{
    public static ProfileCache Instance { get; private set; }

    private const string CACHE_KEY = "cached_profile_";
    private const string CACHE_TIME_KEY = "cached_profile_time_";
    private const int CACHE_EXPIRY_HOURS = 24;

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

    public void SaveProfile(UserProfile profile)
    {
        if (profile == null) return;

        string json = JsonUtility.ToJson(profile);
        PlayerPrefs.SetString(CACHE_KEY + profile.uid, json);
        PlayerPrefs.SetString(CACHE_TIME_KEY + profile.uid,
            System.DateTime.UtcNow.ToString());
        PlayerPrefs.Save();
        Debug.Log($"Profile cached for uid: {profile.uid}");
    }

    public UserProfile LoadProfile(string uid)
    {
        string key = CACHE_KEY + uid;
        string timeKey = CACHE_TIME_KEY + uid;

        if (!PlayerPrefs.HasKey(key))
            return null;

        // Check if cache has expired
        string timeStr = PlayerPrefs.GetString(timeKey);
        if (System.DateTime.TryParse(timeStr, out System.DateTime cacheTime))
        {
            double hoursElapsed = (System.DateTime.UtcNow - cacheTime).TotalHours;
            if (hoursElapsed > CACHE_EXPIRY_HOURS)
            {
                Debug.Log("Cache expired, clearing");
                ClearProfile(uid);
                return null;
            }
        }

        string json = PlayerPrefs.GetString(key);
        UserProfile profile = JsonUtility.FromJson<UserProfile>(json);
        Debug.Log($"Profile loaded from cache: {profile.name}");
        return profile;
    }

    public bool HasCache(string uid)
    {
        return PlayerPrefs.HasKey(CACHE_KEY + uid);
    }

    public void ClearProfile(string uid)
    {
        PlayerPrefs.DeleteKey(CACHE_KEY + uid);
        PlayerPrefs.DeleteKey(CACHE_TIME_KEY + uid);
        PlayerPrefs.Save();
    }

    public void ClearAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("All cache cleared");
    }
}