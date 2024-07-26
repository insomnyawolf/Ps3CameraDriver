namespace VirtualCameraCommon;

public enum ColorFormat : int
{
    /// <summary>
    /// Output in Bayer. Destination buffer must be width * height bytes
    /// </summary>
    Bayer,
    /// <summary>
    /// Output in Grayscale. Destination buffer must be width * height bytes
    /// </summary>
    Gray,
    /// <summary>
    /// Output in RGB. Destination buffer must be width * height * 3 bytes
    /// </summary>
    RGB,
    /// <summary>
    /// Output in BGR. Destination buffer must be width * height * 3 bytes
    /// </summary>
    BGR,
#warning does alpha channel in a webcam matters?
    ///// <summary>
    ///// Output in RGBA. Destination buffer must be width * height * 4 bytes
    ///// </summary>
    //RGBA,
    ///// <summary>
    ///// Output in BGRA. Destination buffer must be width * height * 4 bytes
    ///// </summary>
    //BGRA,
};