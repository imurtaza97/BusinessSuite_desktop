using BusinessSuite.DAL.Entities;

namespace BusinessSuite.UI.Services;

/// <summary>
/// Application state service for managing global application context
/// including currently logged-in user
/// </summary>
public class AppState
{
    private static AppState? _instance;
    private static readonly object _lock = new object();

    public User? CurrentUser { get; set; }
    public int CurrentBusinessId { get; set; }

    private AppState()
    {
    }

    /// <summary>
    /// Gets the singleton instance of AppState
    /// </summary>
    public static AppState Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new AppState();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Initialize the app state with current user and business
    /// </summary>
    public void Initialize(User user, int businessId)
    {
        CurrentUser = user;
        CurrentBusinessId = businessId;
    }

    /// <summary>
    /// Clear the app state (for logout)
    /// </summary>
    public void Clear()
    {
        CurrentUser = null;
        CurrentBusinessId = 0;
    }

    /// <summary>
    /// Get current user ID
    /// </summary>
    public int GetCurrentUserId()
    {
        return CurrentUser?.UserID ?? 0;
    }

    /// <summary>
    /// Get current user name
    /// </summary>
    public string GetCurrentUserName()
    {
        return CurrentUser?.FullName ?? CurrentUser?.UserName ?? "Unknown";
    }

    /// <summary>
    /// Check if user is logged in
    /// </summary>
    public bool IsLoggedIn()
    {
        return CurrentUser != null && CurrentUser.UserID > 0;
    }
}
