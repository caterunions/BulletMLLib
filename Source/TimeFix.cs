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
        return Time.deltaTime * 60f;
      }
    }
  }
}
