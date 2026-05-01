using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Anchors the AR profile overlay UI to a tracked image target.
/// Shows overlay when card is detected, hides when tracking is lost.
/// Attach to the ARTrackedImageManager's GameObject in the scene.
/// </summary>
public class AROverlayController : MonoBehaviour
{
    public static AROverlayController Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────
    [Header("AR Components")]
    [Tooltip("ARTrackedImageManager on the AR Session Origin")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;

    [Header("UI References")]
    [Tooltip("The world-space canvas prefab to anchor on the card")]
    [SerializeField] private GameObject overlayPrefab;

    [Tooltip("Offset from card surface to place the overlay")]
    [SerializeField] private Vector3 overlayOffset = new Vector3(0, 0.05f, 0);

    [Tooltip("Scale of the overlay relative to card size")]
    [SerializeField] private Vector3 overlayScale = new Vector3(0.001f, 0.001f, 0.001f);

    // ── Events ────────────────────────────────────────────────────
    // Fired when overlay becomes visible
    public static event Action OnOverlayShown;

    // Fired when overlay is hidden
    public static event Action OnOverlayHidden;

    // ── State ─────────────────────────────────────────────────────
    private GameObject _overlayInstance;
    private ProfileCardUI _profileCardUI;
    private bool _isVisible;

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
        // Listen for tracked image changes from AR Foundation
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;

        // Listen for profile fetch completion
        ProfileService.OnProfileFetched += HandleProfileFetched;
        ProfileService.OnProfileFetchFailed += HandleProfileFetchFailed;

        // Listen for Vuforia cloud match
        VuforiaCloudManager.OnUIDFound += HandleUIDFound;
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;

        ProfileService.OnProfileFetched -= HandleProfileFetched;
        ProfileService.OnProfileFetchFailed -= HandleProfileFetchFailed;
        VuforiaCloudManager.OnUIDFound -= HandleUIDFound;
    }

    // ── Public Methods ────────────────────────────────────────────

    /// <summary>
    /// Shows the overlay with the given profile data.
    /// Called after profile is successfully fetched.
    /// </summary>
    public void ShowOverlay(UserProfile profile)
    {
        if (_overlayInstance == null)
            CreateOverlay();

        _profileCardUI?.Populate(profile);
        _overlayInstance.SetActive(true);
        _isVisible = true;
        OnOverlayShown?.Invoke();
        Debug.Log("AR Overlay shown");
    }

    /// <summary>
    /// Hides the overlay.
    /// Called when tracking is lost or user dismisses it.
    /// </summary>
    public void HideOverlay()
    {
        if (_overlayInstance != null)
            _overlayInstance.SetActive(false);

        _isVisible = false;
        OnOverlayHidden?.Invoke();
        Debug.Log("AR Overlay hidden");
    }

    // ── Private Methods ───────────────────────────────────────────

    /// <summary>
    /// Creates the overlay instance from prefab if not already created.
    /// </summary>
    private void CreateOverlay()
    {
        if (overlayPrefab == null)
        {
            Debug.LogError("Overlay prefab not assigned to AROverlayController");
            return;
        }

        _overlayInstance = Instantiate(overlayPrefab);
        _overlayInstance.transform.localScale = overlayScale;
        _overlayInstance.SetActive(false);

        // Get ProfileCardUI from the instantiated prefab
        _profileCardUI = _overlayInstance.GetComponentInChildren<ProfileCardUI>();
    }

    /// <summary>
    /// Handles AR Foundation tracked image changes.
    /// Updates overlay position to follow the card in 3D space.
    /// </summary>
    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        // Handle newly detected cards
        foreach (var trackedImage in args.added)
        {
            UpdateOverlayTransform(trackedImage);
            if (_isVisible) ShowOverlay(ProfileService.Instance.CurrentProfile);
        }

        // Handle card position updates
        foreach (var trackedImage in args.updated)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                UpdateOverlayTransform(trackedImage);
            }
            else
            {
                // Card moved out of view
                HideOverlay();
            }
        }

        // Handle lost cards
        foreach (var trackedImage in args.removed)
        {
            HideOverlay();
        }
    }

    /// <summary>
    /// Positions and rotates the overlay to match the tracked card.
    /// </summary>
    private void UpdateOverlayTransform(ARTrackedImage trackedImage)
    {
        if (_overlayInstance == null) return;

        // Place overlay just above card surface
        _overlayInstance.transform.position =
            trackedImage.transform.position + overlayOffset;

        // Match card rotation so overlay faces same direction
        _overlayInstance.transform.rotation = trackedImage.transform.rotation;
    }

    /// <summary>
    /// When Vuforia finds a UID, fetch the profile — overlay will
    /// show automatically once profile is fetched.
    /// </summary>
    private void HandleUIDFound(string uid)
    {
        _ = ProfileService.Instance.FetchProfile(uid);
    }

    /// <summary>
    /// When profile is fetched successfully, show the overlay.
    /// </summary>
    private void HandleProfileFetched(UserProfile profile)
    {
        ShowOverlay(profile);
    }

    /// <summary>
    /// When profile fetch fails, log error and keep overlay hidden.
    /// </summary>
    private void HandleProfileFetchFailed(string error)
    {
        Debug.LogError($"Profile fetch failed: {error}");
        HideOverlay();
    }
}