#pragma once

#include <algorithm>
#include <cmath>

// Used directly by the runtime character; no Unreal types or test-only substitute.
namespace Sorajiro::CameraPolicy
{
inline constexpr double MinPitch = -65.0;
inline constexpr double MaxPitch = 15.0;
inline constexpr double MinDistance = 55.0;
inline constexpr double MaxDistance = 240.0;
inline constexpr double DefaultDistance = 120.0;
inline constexpr double DefaultPitch = -15.0;

inline double PitchAfterInput(double Current, double MouseDelta, double Sensitivity)
{
    const double SafeCurrent = std::isfinite(Current) ? Current : DefaultPitch;
    if (!std::isfinite(MouseDelta) || !std::isfinite(Sensitivity) || Sensitivity < 0.0)
    {
        return std::clamp(SafeCurrent, MinPitch, MaxPitch);
    }
    return std::clamp(SafeCurrent + MouseDelta * Sensitivity, MinPitch, MaxPitch);
}

inline double DistanceAfterScroll(double Current, double Scroll)
{
    const double SafeCurrent = std::clamp(std::isfinite(Current) ? Current : DefaultDistance,
                                         MinDistance, MaxDistance);
    if (!std::isfinite(Scroll))
    {
        return SafeCurrent;
    }
    // Positive scroll brings the camera closer. Limit extreme device input.
    return std::clamp(SafeCurrent * std::exp(-0.12 * std::clamp(Scroll, -100.0, 100.0)),
                      MinDistance, MaxDistance);
}
}
