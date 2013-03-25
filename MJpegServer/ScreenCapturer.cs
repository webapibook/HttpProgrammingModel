using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MJpegServer
{
    public class ScreenCapturer
    {
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().Where(codec => codec.FormatID == format.Guid).First();
        }

        private static void GenerateImageBufferInto(Stream os)
        {
            using (var bitmap = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(
                        System.Windows.Forms.Screen.PrimaryScreen.Bounds.X, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Y,
                        0, 0,
                        System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

                    ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 5L);

                    bitmap.Save(os, jgpEncoder, encoderParams);
                }
            }
        }

        public static void GetEncodedBytesInto(Stream os)
        {
            GenerateImageBufferInto(os);
        }

        public static Stream GetEncodedByteStream()
        {
            var ms = new MemoryStream();
            GetEncodedBytesInto(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
