using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Manages the AR Foundation session lifecycle.
/// Monitors tracking state and broadcasts changes to other scripts.
/// Attach this to the ARSession GameObject in the scene.
/// </summary>
public class ARSessionManager : MonoBehaviour
{
    public static ARSessionManager Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────
    [Header("AR Components")]
    [Tooltip("ARSession component — drag ARSession GameObject here")]
    [SerializeField] private ARSession arSession;

    [Tooltip("ARCameraManager on the AR Camera GameObject")]
    [SerializeField] private ARCameraManager arCameraManager;

    // ── Events ────────────────────────────────────────────────────
    // Fired when AR session becomes fully tracked
    public static event Action OnTrackingAcquired;

    // Fired when AR tracking is lost (card moved out of view etc.)
    public static event Action OnTrackingLost;

    // Fired when AR session state changes
    public static event Action<ARSessionState> OnSessionStateChanged;

    // ── State ─────────────────────────────────────────────────────
    public bool IsTracking { get; private set; }
    public ARSessionState CurrentState { get; private set; }

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

    void OnEnable()
    {
        // Subscribe to AR session state changes
        ARSession.stateChanged += HandleSessionStateChanged;
    }

    void OnDisable()
    {
        ARSession.stateChanged -= HandleSessionStateChanged;
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Resets the AR session — useful when switching back to AR mode
    /// after QR scanning or when tracking is persistently lost.
    /// </summary>
    public void ResetSession()
    {
        if (arSession != null)
        {
            arSession.Reset();
            Debug.Log("AR session reset");
        }
    }

    /// <summary>
    /// Returns true if device supports AR.
    /// Call before activating AR mode to show fallback UI if needed.
    /// </summary>
    public bool IsARSupported()
    {
        return ARSession.state != ARSessionState.Unsupported;
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Handles AR session state changes from ARFoundation.
    /// Maps session states to simplified tracking events.
    /// </summary>
    private void HandleSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        CurrentState = args.state;
        Debug.Log($"AR Session state: {args.state}");
        OnSessionStateChanged?.Invoke(args.state);

        switch (args.state)
        {
            case ARSessionState.SessionTracking:
                if (!IsTracking)
                {
                    IsTracking = true;
                    OnTrackingAcquired?.Invoke();
                }
                break;

            case ARSessionState.SessionInitializing:
                // Initializing — don't fire lost yet
                break;

            case ARSessionState.None:
            case ARSessionState.Unsupported:
            case ARSessionState.CheckingAvailability:
                if (IsTracking)
                {
                    IsTracking = false;
                    OnTrackingLost?.Invoke();
                }
                break;
        }
    }
}