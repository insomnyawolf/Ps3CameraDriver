﻿using VirtualCameraCommon;

namespace Ps3CameraDriver;

public partial class Ps3CamDriver
{
    // PSMove output is in the following Bayer format (GRBG):
    //
    // G R G R G R
    // B G B G B G
    // G R G R G R
    // B G B G B G
    //
    // This is the normal Bayer pattern shifted left one place.


}

// from accord-net untill i can make my own implementation
// i really tried but looks like i am stupid...

public class BayerFilter
{
    private bool performDemosaicing = true;
    private int[,] bayerPattern = new int[2, 2]
    {
        { RGB.G, RGB.R },
        { RGB.B, RGB.G }
    };

    /// <summary>
    /// Specifies if demosaicing must be done or not.
    /// </summary>
    /// 
    /// <remarks><para>The property specifies if color demosaicing must be done or not.
    /// If the property is set to <see langword="false"/>, then pixels of the result color image
    /// are colored according to the <see cref="BayerPattern">Bayer pattern</see> used, i.e. every pixel
    /// of the source grayscale image is copied to corresponding color plane of the result image.
    /// If the property is set to <see langword="true"/>, then pixels of the result image
    /// are set to color, which is obtained by averaging color components from the 3x3 window - pixel
    /// itself plus 8 surrounding neighbors.</para>
    /// 
    /// <para>Default value is set to <see langword="true"/>.</para>
    /// </remarks>
    /// 
    public bool PerformDemosaicing
    {
        get { return performDemosaicing; }
        set { performDemosaicing = value; }
    }

    /// <summary>
    /// Specifies Bayer pattern used for decoding color image.
    /// </summary>
    /// 
    /// <remarks><para>The property specifies 2x2 array of RGB color indexes, which set the
    /// Bayer patter used for decoding color image.</para>
    /// 
    /// <para>By default the property is set to:
    /// <code>
    /// new int[2, 2] { { RGB.G, RGB.R }, { RGB.B, RGB.G } }
    /// </code>,
    /// which corresponds to
    /// <code lang="none">
    /// G R
    /// B G
    /// </code>
    /// pattern.
    /// </para>
    /// </remarks>
    /// 
    public int[,] BayerPattern
    {
        get { return bayerPattern; }
        set
        {
            bayerPattern = value;
        }
    }


    /// <summary>
    /// Process the filter on the specified image.
    /// </summary>
    /// 
    /// <param name="sourceData">Source image data.</param>
    /// <param name="destinationData">Destination image data.</param>
    /// 

    //public void DebayerGrey(int Width, int Height, Span<byte> input, Span<byte> output)
    public unsafe void ProcessFilter(VideoSize VideoSize, byte[] sourceData, uint srcStride, byte[] destinationData)
    {
        // get width and height
        uint width = VideoSize.Width;
        uint height = VideoSize.Height;

        uint widthM1 = width;
        uint heightM1 = height;

        uint srcOffset = 0;
        uint dstOffset = 0;

        uint srcIndex = 0;
        uint dstIndex = 0;

        // do the job
        fixed (byte* src = sourceData)
        {
            fixed (byte* dst = destinationData)
            {
                if (!performDemosaicing)
                {
                    // for each line
                    for (int y = 0; y < height; y++)
                    {
                        // for each pixel
                        for (int x = 0; x < width; x++)
                        {
                            // index + 0
                            dst[dstIndex + RGB.B] = 0;
                            // index + 1
                            dst[dstIndex + RGB.G] = 0;
                            // index + 2
                            dst[dstIndex + RGB.R] = 0;

                            var value = src[srcIndex];

                            dst[dstIndex + bayerPattern[y & 1, x & 1]] = value;

                            srcIndex++;
                            dstIndex += 3;
                        }
                        srcIndex += srcOffset;
                        dstIndex += dstOffset;
                    }

                    return;
                }

                Span<int> rgbValues = stackalloc int[3];
                Span<int> rgbCounters = stackalloc int[3];

                // for each line
                for (int y = 0; y < height; y++)
                {
                    // for each pixel
                    for (int x = 0; x < width; x++)
                    {
                        // 0
                        rgbValues[RGB.B] = 0;
                        rgbCounters[RGB.B] = 0;
                        // 1
                        rgbValues[RGB.G] = 0;
                        rgbCounters[RGB.G] = 0;
                        // 2
                        rgbValues[RGB.R] = 0;
                        rgbCounters[RGB.R] = 0;

                        int bayerIndex = bayerPattern[y & 1, x & 1];

                        rgbValues[bayerIndex] += src[srcIndex];
                        rgbCounters[bayerIndex]++;

                        if (x != 0)
                        {
                            bayerIndex = bayerPattern[y & 1, (x - 1) & 1];

                            rgbValues[bayerIndex] += src[srcIndex + -1];
                            rgbCounters[bayerIndex]++;
                        }

                        if (x != widthM1)
                        {
                            bayerIndex = bayerPattern[y & 1, (x + 1) & 1];

                            rgbValues[bayerIndex] += src[srcIndex + 1];
                            rgbCounters[bayerIndex]++;
                        }

                        if (y != 0)
                        {
                            bayerIndex = bayerPattern[(y - 1) & 1, x & 1];

                            rgbValues[bayerIndex] += src[srcIndex + -srcStride];
                            rgbCounters[bayerIndex]++;

                            if (x != 0)
                            {
                                bayerIndex = bayerPattern[(y - 1) & 1, (x - 1) & 1];

                                rgbValues[bayerIndex] += src[srcIndex + -srcStride - 1];
                                rgbCounters[bayerIndex]++;
                            }

                            if (x != widthM1)
                            {
                                bayerIndex = bayerPattern[(y - 1) & 1, (x + 1) & 1];

                                rgbValues[bayerIndex] += src[srcIndex + -srcStride + 1];
                                rgbCounters[bayerIndex]++;
                            }
                        }

                        if (y != heightM1)
                        {
                            bayerIndex = bayerPattern[(y + 1) & 1, x & 1];

                            rgbValues[bayerIndex] += src[srcIndex + srcStride];
                            rgbCounters[bayerIndex]++;

                            if (x != 0)
                            {
                                bayerIndex = bayerPattern[(y + 1) & 1, (x - 1) & 1];

                                rgbValues[bayerIndex] += src[srcIndex + srcStride - 1];
                                rgbCounters[bayerIndex]++;
                            }

                            if (x != widthM1)
                            {
                                bayerIndex = bayerPattern[(y + 1) & 1, (x + 1) & 1];

                                rgbValues[bayerIndex] += src[srcIndex + srcStride + 1];
                                rgbCounters[bayerIndex]++;
                            }
                        }

                        dst[dstIndex + RGB.R] = (byte)(rgbValues[RGB.R] / rgbCounters[RGB.R]);
                        dst[dstIndex + RGB.G] = (byte)(rgbValues[RGB.G] / rgbCounters[RGB.G]);
                        dst[dstIndex + RGB.B] = (byte)(rgbValues[RGB.B] / rgbCounters[RGB.B]);

                        srcIndex++;
                        dstIndex += 3;
                    }

                    srcIndex += srcOffset;
                    dstIndex += dstOffset;
                }
            }
        }
    }
}

[Serializable]
public struct RGB
{
    /// <summary>
    /// Index of red component.
    /// </summary>
    public const short R = 2;

    /// <summary>
    /// Index of green component.
    /// </summary>
    public const short G = 1;

    /// <summary>
    /// Index of blue component.
    /// </summary>
    public const short B = 0;

    /// <summary>
    /// Index of alpha component for ARGB images.
    /// </summary>
    public const short A = 3;

    /// <summary>
    /// Red component.
    /// </summary>
    public byte Red;

    /// <summary>
    /// Green component.
    /// </summary>
    public byte Green;

    /// <summary>
    /// Blue component.
    /// </summary>
    public byte Blue;

    /// <summary>
    /// Alpha component.
    /// </summary>
    public byte Alpha;

    /// <summary>
    /// <see cref="System.Drawing.Color">Color</see> value of the class.
    /// </summary>
    public System.Drawing.Color Color
    {
        get => System.Drawing.Color.FromArgb(Alpha, Red, Green, Blue);
        set
        {
            Red = value.R;
            Green = value.G;
            Blue = value.B;
            Alpha = value.A;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RGB"/> class.
    /// </summary>
    /// 
    /// <param name="red">Red component.</param>
    /// <param name="green">Green component.</param>
    /// <param name="blue">Blue component.</param>
    /// 
    public RGB(byte red, byte green, byte blue)
    {
        this.Red = red;
        this.Green = green;
        this.Blue = blue;
        this.Alpha = 255;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RGB"/> class.
    /// </summary>
    /// 
    /// <param name="red">Red component.</param>
    /// <param name="green">Green component.</param>
    /// <param name="blue">Blue component.</param>
    /// <param name="alpha">Alpha component.</param>
    /// 
    public RGB(byte red, byte green, byte blue, byte alpha)
    {
        this.Red = red;
        this.Green = green;
        this.Blue = blue;
        this.Alpha = alpha;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RGB"/> class.
    /// </summary>
    /// 
    /// <param name="color">Initialize from specified <see cref="System.Drawing.Color">color.</see></param>
    /// 
    public RGB(System.Drawing.Color color)
    {
        this.Red = color.R;
        this.Green = color.G;
        this.Blue = color.B;
        this.Alpha = color.A;
    }
}