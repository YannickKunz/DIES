using UnityEngine;

public struct PlatformInfo
{
    public GameObject platform;
    public string name;
    public string tag;
    public int layer;
    public Vector3 localPlayerPos; // Player's local position relative to the platform's transform

    // Optional: Add a method to check if the platform is valid
    public bool IsValid()
    {
        return platform != null;
    }
}

public static class WorldState
{
    public static PlatformInfo CurrentPlatform { get; private set; }
    public static event System.Action<PlatformInfo> OnPlatformChanged;

    private static PlatformInfo _previousPlatformInfo;

    public static void UpdateCurrentPlatform(PlatformInfo newPlatformInfo)
    {
        // Check if the essential platform reference has changed, or if name/tag/layer changed.
        // Comparing localPlayerPos here might be too frequent if only that changes.
        // The primary trigger for OnPlatformChanged should be a change in the platform itself.
        if (newPlatformInfo.platform != _previousPlatformInfo.platform ||
            (newPlatformInfo.platform != null && _previousPlatformInfo.platform != null && // only compare name/tag/layer if both are not null
             (newPlatformInfo.name != _previousPlatformInfo.name ||
              newPlatformInfo.tag != _previousPlatformInfo.tag ||
              newPlatformInfo.layer != _previousPlatformInfo.layer)))
        {
            CurrentPlatform = newPlatformInfo;
            _previousPlatformInfo = newPlatformInfo; // Update previous state
            OnPlatformChanged?.Invoke(newPlatformInfo);
            // Debug.Log($"WorldState: Player platform changed to {(newPlatformInfo.platform != null ? newPlatformInfo.platform.name : "None")}");
        }
        else if (newPlatformInfo.platform != null && _previousPlatformInfo.platform != null && newPlatformInfo.platform == _previousPlatformInfo.platform)
        {
            // If it's the same platform, still update CurrentPlatform in case localPlayerPos changed,
            // but don't necessarily fire the event unless localPlayerPos is critical for event listeners.
            CurrentPlatform = newPlatformInfo;
            _previousPlatformInfo = newPlatformInfo;
        }
        else if (newPlatformInfo.platform == null && _previousPlatformInfo.platform != null)
        {
            // Player left a platform
            CurrentPlatform = newPlatformInfo; // newPlatformInfo will be invalid (platform = null)
            _previousPlatformInfo = newPlatformInfo;
            OnPlatformChanged?.Invoke(newPlatformInfo);
            // Debug.Log("WorldState: Player left platform.");
        }
    }
}