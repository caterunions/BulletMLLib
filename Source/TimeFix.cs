using UnityEngine;

namespace BulletMLLib
{
  /// <summary>
  /// Remove the 60 FPS limit
  /// </summary>
  public static class TimeFix
  {
    /// <summary>
    /// Get a multiplier to transform a 60 FPS duration value into the current framerate value
    /// </summary>
    public static float Delta
    {
      get
      {
        if (Framerate != 60f)
        {
          return Time.deltaTime * 60f;
        }
        else
        {
          return 1f;
        }
      }
    }

    public static float Framerate
    {
      get
      {
        if (QualitySettings.vSyncCount == 0)
        {
          return Application.targetFrameRate;
        }
        else
        {
          if (Screen.currentResolution.refreshRate > 0)
          {
            return Screen.currentResolution.refreshRate;
          }
          return 60f;
        }
      }
    }
  }
}
