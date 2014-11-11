
namespace Alanta.Client.Media.Jpeg
{
    /// <summary>
    /// Used by the JpegFrameEncoder and JpegFrameDecoder classes to define the values the two classes need to hold in common.
    /// </summary>
    /// <remarks>
    /// Normally, these values are written to and read from the JFIF headers.  But to streamline things, for our purposes, we've dropped the headers,
    /// and this is the easiest and simplest way to share these values.
    /// </remarks>
    public class FrameDefaults
    {
        public static readonly byte[] CompId = { 1, 2, 3 };
        public static readonly byte[] QtableNumber = { 0, 1, 1 };
        public static readonly byte[] DCtableNumber = { 0, 1, 1 };
        public static readonly byte[] ACtableNumber = { 0, 1, 1 };
        public static readonly byte[] HSampFactor = { 1, 1, 1 };
        public static readonly byte[] VSampFactor = { 1, 1, 1 };
        public const byte NumberOfComponents = 3;
    }
}
