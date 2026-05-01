using System;
using UnityEngine;
using Vuforia;

/// <summary>
/// Manages Vuforia Cloud Recognition using Vuforia 11.x API.
/// Requires CloudRecoBehaviour on the same GameObject.
/// Fires OnUIDFound when a business card is matched in the cloud.
/// </summary>
public class VuforiaCloudManager : MonoBehaviour
{
    public static VuforiaCloudManager Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────
    [Header("Config")]
    [Tooltip("AppConfig ScriptableObject with Vuforia keys")]
    [SerializeField] private AppConfig appConfig;

    // ── Events ────────────────────────────────────────────────────
    // Fired when cloud recognition matches a card and returns a UID
    public static event Action<string> OnUIDFound;

    // Fired when no match is found
    public static event Action OnNoMatch;

    // ── State ─────────────────────────────────────────────────────
    private CloudRecoBehaviour _cloudReco;
    private bool _isScanning;
    private string _lastFoundUID = "";

    // ── Lifecycle ─────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Get CloudRecoBehaviour — must be on same GameObject
        _cloudReco = GetComponent<CloudRecoBehaviour>();

        if (_cloudReco == null)
        {
            Debug.LogError("CloudRecoBehaviour not found. " +
                "Add it to the same GameObject as VuforiaCloudManager.");
            return;
        }

        // Set keys from AppConfig
        if (appConfig != null)
        {
            _cloudReco.AccessKey = appConfig.vuforiaAccessKey;
            _cloudReco.SecretKey = appConfig.vuforiaSecretKey;
            Debug.Log("Vuforia Cloud keys set from AppConfig");
        }
        else
        {
            Debug.LogError("AppConfig not assigned to VuforiaCloudManager");
            return;
        }

        // Subscribe to cloud recognition events
        _cloudReco.RegisterOnInitializedEventHandler(OnCloudRecoInitialized);
        _cloudReco.RegisterOnNewSearchResultEventHandler(OnNewSearchResult);
        _cloudReco.RegisterOnStateChangedEventHandler(OnStateChanged);
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Start cloud recognition scanning.
    /// Call when entering AR scan mode.
    /// </summary>
    public void StartScanning()
    {
        if (_isScanning || _cloudReco == null) return;
        _isScanning = true;
        _lastFoundUID = "";
        _cloudReco.enabled = true;
        Debug.Log("Vuforia Cloud Recognition started");
    }

    /// <summary>
    /// Stop cloud recognition scanning.
    /// Call when a match is found or leaving AR mode.
    /// </summary>
    public void StopScanning()
    {
        if (!_isScanning || _cloudReco == null) return;
        _isScanning = false;
        _cloudReco.enabled = false;
        Debug.Log("Vuforia Cloud Recognition stopped");
    }

    // ── Private Handlers ─────────────────────────────────────────

    private void OnCloudRecoInitialized(CloudRecoBehaviour cloudReco)
    {
        Debug.Log("Vuforia Cloud Reco initialized successfully");
    }

    private void OnStateChanged(bool enabled)
    {
        Debug.Log($"Cloud Reco state changed, enabled: {enabled}");
    }

    private void OnNewSearchResult(CloudRecoBehaviour.CloudRecoSearchResult result)
    {
        if (result == null)
        {
            OnNoMatch?.Invoke();
            return;
        }

        string uid = result.MetaData;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("Card matched but metadata (UID) is empty");
            return;
        }

        if (uid == _lastFoundUID) return;
        _lastFoundUID = uid;

        Debug.Log($"Vuforia matched card — UID: {uid}");
        StopScanning();
        OnUIDFound?.Invoke(uid);
    }

    void OnDestroy()
    {
        if (_cloudReco != null)
        {
            _cloudReco.RegisterOnInitializedEventHandler(OnCloudRecoInitialized);
            _cloudReco.RegisterOnNewSearchResultEventHandler(OnNewSearchResult);
            _cloudReco.RegisterOnStateChangedEventHandler(OnStateChanged);
        }
    }
}