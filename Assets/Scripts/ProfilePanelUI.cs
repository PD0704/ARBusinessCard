using UnityEngine;

/// <summary>
/// Listens for profile fetch events and populates
/// the flat profile screen (non-AR view).
/// Attach to ProfilePanel.
/// </summary>
public class ProfilePanelUI : MonoBehaviour
{
    [SerializeField] private ProfileCardUI profileCardUI;
    [SerializeField] private UnityEngine.UI.Button backButton;

    void OnEnable()
    {
        ProfileService.OnProfileFetched += HandleProfileFetched;
        backButton?.onClick.AddListener(OnBackClicked);
    }

    void OnDisable()
    {
        ProfileService.OnProfileFetched -= HandleProfileFetched;
        backButton?.onClick.RemoveListener(OnBackClicked);
    }

    private void HandleProfileFetched(UserProfile profile)
    {
        profileCardUI?.Populate(profile);
    }

    private void OnBackClicked()
    {
        AppStateManager.Instance?.GoToHome();
    }
}