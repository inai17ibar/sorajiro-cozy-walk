#include "Logic/CameraPolicy.h"
#include <cstdlib>
#include <iostream>
#include <limits>

static void Check(bool Condition, const char* Message)
{
    if (!Condition) { std::cerr << "FAIL: " << Message << '\n'; std::exit(1); }
}

int main()
{
    using namespace Sorajiro::CameraPolicy;
    const double NaN = std::numeric_limits<double>::quiet_NaN();
    Check(PitchAfterInput(-15, 10000, .2) == MaxPitch, "upward input clamps");
    Check(PitchAfterInput(-15, -10000, .2) == MinPitch, "downward input clamps");
    Check(PitchAfterInput(-15, 10, .2) == -13, "sensitivity preserves device delta");
    Check(PitchAfterInput(NaN, 0, .2) == DefaultPitch, "invalid current recovers");
    Check(PitchAfterInput(-15, NaN, .2) == -15, "invalid device delta ignored");
    Check(PitchAfterInput(-15, 10, -1) == -15, "invalid sensitivity ignored");
    Check(DistanceAfterScroll(120, 1) < 120, "positive scroll zooms in");
    Check(DistanceAfterScroll(120, -1) > 120, "negative scroll zooms out");
    Check(DistanceAfterScroll(120, 1e6) == MinDistance, "extreme wheel cannot cross near limit");
    Check(DistanceAfterScroll(120, -1e6) == MaxDistance, "extreme wheel cannot cross far limit");
    Check(DistanceAfterScroll(NaN, 0) == DefaultDistance, "invalid distance recovers");
    Check(DistanceAfterScroll(120, NaN) == 120, "invalid wheel ignored");
    Check(std::abs(DistanceAfterScroll(DistanceAfterScroll(120, 1), -1) - 120) < 1e-9,
          "wheel round trip does not drift away from limits");
    // Devices split deltas differently; frame count must not change the result.
    double Split = 120;
    for (int Frame = 0; Frame < 60; ++Frame) Split = DistanceAfterScroll(Split, 1.0 / 60);
    Check(std::abs(Split - DistanceAfterScroll(120, 1)) < 1e-9, "split scroll is frame independent");
    std::cout << "PASS: runtime CameraPolicy constraints (14 checks); not a UE movement/render test\n";
}
