using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;

namespace CNNWB.Common
{
    public class BitmapToImage : Image
    {
        public BitmapToImage(System.Drawing.Bitmap bitmap) : base()
        {
            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource destination = decoder.Frames[0];
            destination.Freeze();
            this.Source = destination;
        }
    }
}
