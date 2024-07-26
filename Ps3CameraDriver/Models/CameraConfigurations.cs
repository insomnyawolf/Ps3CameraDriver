using Ps3CameraDriver.Models;
using VirtualCameraCommon;

namespace Ps3CameraDriver;

public class CameraConfigurations
{
    public VideoSize VideoSize { get; init; }
    public IReadOnlyList<SensorConfiguration> SensorConfiguration { get; init; } = null!;
    public IReadOnlyList<Command> SensorStart { get; init; } = null!;
    public IReadOnlyList<Command> BridgeStart { get; init; } = null!;

    // _normalize_framerate()
    public SensorConfiguration GetSensorConfiguration(int framesPerSecond)
    {
        var candidates = SensorConfiguration;

        SensorConfiguration config = candidates[0];

        for (int i = 1; i < candidates.Count; i++)
        {
            var current = candidates[i];

            if (current.FramesPerSecond < framesPerSecond)
            {
                break;
            }

            config = current;
        }

        return config;
    }
}