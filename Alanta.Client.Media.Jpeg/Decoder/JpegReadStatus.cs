
namespace Alanta.Client.Media.Jpeg.Decoder
{
    public enum Status
    {
        Success,
        EOF,
        MarkerFound
    }

    /// <summary>
    /// Holds the state of the current read/decode operation.
    /// </summary>
    /// <remarks>
    /// Tests show approximately equivalent performance between using a class and a struct.
    /// Further optimizations to try include:
    /// (1) Pass a struct by reference.
    /// (2) Only use one class/struct (JpegReadStatus), but include both ResultByte and ResultInt member fields.
    /// </remarks>
    public class JpegReadStatusByte
    {
        public Status Status;
        public byte Result;
        public JpegReadStatusInt ToInt()
        {
            var status = new JpegReadStatusInt();
            status.Status = Status;
            status.Result = Result;
            return status;
        }
        public static JpegReadStatusByte GetSuccess()
        {
            var status = new JpegReadStatusByte();
            status.Status = Status.Success;
            status.Result = 0;
            return status;
        }
    }

    public class JpegReadStatusInt
    {
        public Status Status;
        public int Result;
        public JpegReadStatusByte ToByte()
        {
            var status = new JpegReadStatusByte();
            status.Status = Status;
            status.Result = (byte)Result;
            return status;
        }
        public static JpegReadStatusInt GetSuccess()
        {
            var status = new JpegReadStatusInt();
            status.Status = Status.Success;
            status.Result = 0;
            return status;
        }
    }
}
