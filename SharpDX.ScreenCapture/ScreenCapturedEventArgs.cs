namespace SharpDX.ScreenCapture
{
    using SharpDX;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Runtime.CompilerServices;

    public class ScreenCapturedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a point that indicate a position outside the screen region.
        /// </summary>
        public static readonly Point InvalidPoint = new Point(-1, -1);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DataBox dataBox;

        /// <summary>
        /// Gets the captured screen region.
        /// </summary>
        public Rectangle Region { get; }

        /// <summary>
        /// Gets the x-coordinate of the captured screen relative the the full screen size.
        /// </summary>
        public int X => Region.X;

        /// <summary>
        /// Gets the y-coordinate of the captured screen relative the the full screen size.
        /// </summary>
        public int Y => Region.X;

        /// <summary>
        /// Gets the captured screen width.
        /// </summary>
        public int Width => Region.Width;

        /// <summary>
        /// Gets the captured screen height.
        /// </summary>
        public int Height => Region.Height;

        /// <summary>
        /// Create a new <see cref="ScreenCapturedEventArgs"/> instance.
        /// </summary>
        /// <param name="dataBox">The screen captured data box.</param>
        public ScreenCapturedEventArgs(DataBox dataBox)
        {
            this.dataBox = dataBox;

            Region = new Rectangle(
                0, 0, // x y
                dataBox.RowPitch / 4, // width
                dataBox.SlicePitch / dataBox.RowPitch // height
            );
        }

        /// <summary>
        /// Determines if the pixel position is inside the screen.
        /// </summary>
        /// <param name="position">The pixel position to test.</param>
        /// <returns></returns>
        public bool Contains(Point position)
        {
            return Contains(position.X, position.Y);
        }

        /// <summary>
        /// Determines if the pixel position is inside the screen.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel position to test.</param>
        /// <param name="y">The y-coordinate of the pixel position to test.</param>
        /// <returns></returns>
        public bool Contains(int x, int y)
        {
            return x >= 0 && y >= 0 && Region.Contains(x, y);
        }

        ///// <summary>
        ///// Gets a bitmap from the screen.
        ///// </summary>
        ///// <returns></returns>
        //public Bitmap GetBitmap()
        //{
        //    var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

        //    // copy pixels from screen capture Texture to GDI bitmap
        //    var mapDest = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
        //    var sourcePtr = dataBox.DataPointer;
        //    var destPtr = mapDest.Scan0;

        //    for (int y = 0; y < Height; y++)
        //    {
        //        // copy a single line 
        //        Utilities.CopyMemory(destPtr, sourcePtr, Width * 4);

        //        // advance the pointers
        //        sourcePtr = IntPtr.Add(sourcePtr, dataBox.RowPitch);
        //        destPtr = IntPtr.Add(destPtr, mapDest.Stride);
        //    }

        //    // release source and dest locks
        //    bitmap.UnlockBits(mapDest);

        //    return bitmap;
        //}

        /// <summary>
        /// Gets the color of the specified pixel the screen.
        /// </summary>
        /// <param name="point">The coordinates of the pixel to retrieve.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The x-coordinate is outside the screen region.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The y-coordinate is outside the screen region.</exception>
        public Color GetPixel(Point point)
        {
            return GetPixel(point.X, point.Y);
        }

        /// <summary>
        /// Gets the color of the specified pixel the screen.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel to retrieve.</param>
        /// <param name="y">The y-coordinate of the pixel to retrieve.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The x-coordinate is outside the screen region.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The y-coordinate is outside the screen region.</exception>
        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width)
                throw new ArgumentOutOfRangeException(nameof(x));

            if (y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(y));

            unsafe
            {
                byte* scan0 = (byte*)dataBox.DataPointer.ToPointer();

                int pixel = 4 * x + dataBox.RowPitch * y;

                byte B = scan0[pixel + 0];
                byte G = scan0[pixel + 1];
                byte R = scan0[pixel + 2];
                byte A = scan0[pixel + 3];

                return Color.FromArgb(R, G, B);
            }
        }

        /// <summary>
        /// Match a pixel to the given color.
        /// </summary>
        /// <param name="point">The pixel to match.</param>
        /// <param name="color">The color to match.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The x-coordinate is outside the screen region.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The y-coordinate is outside the screen region.</exception>
        public bool PixelMatch(Point point, Color color, int tolerance = 0)
        {
            return PixelMatch(point.X, point.Y, color.R, color.G, color.B, tolerance);
        }

        /// <summary>
        /// Match a pixel to the given color.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel to match.</param>
        /// <param name="y">The y-coordinate of the pixel to match.</param>
        /// <param name="color">The color to match.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The x-coordinate is outside the screen region.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The y-coordinate is outside the screen region.</exception>
        public bool PixelMatch(int x, int y, Color color, int tolerance = 0)
        {
            return PixelMatch(x, y, color.R, color.G, color.B, tolerance);
        }

        /// <summary>
        /// Match a pixel to the given color.
        /// </summary>
        /// <param name="point">The pixel to match.</param>
        /// <param name="r">The pixel Red value to match.</param>
        /// <param name="g">The pixel Green value to match.</param>
        /// <param name="b">The pixel Blue value to match.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The x-coordinate is outside the screen region.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The y-coordinate is outside the screen region.</exception>
        public bool PixelMatch(Point point, int r, int g, int b, int tolerance = 0)
        {
            return PixelMatch(point.X, point.Y, r, g, b, tolerance);
        }

        /// <summary>
        /// Match a pixel to the given color.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel to match.</param>
        /// <param name="y">The y-coordinate of the pixel to match.</param>
        /// <param name="r">The pixel Red value to match.</param>
        /// <param name="g">The pixel Green value to match.</param>
        /// <param name="b">The pixel Blue value to match.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The x-coordinate is outside the screen region.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The y-coordinate is outside the screen region.</exception>
        public bool PixelMatch(int x, int y, int r, int g, int b, int tolerance = 0)
        {
            var pixel = GetPixel(x, y);

            return ColorMatch(pixel.R, pixel.G, pixel.B, r, g, b, tolerance);
        }

        /// <summary>
        /// FInd a pixel in the screen.
        /// </summary>
        /// <param name="color">The pixel color to find.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        public Point FindPixel(Color color, int tolerance = 0)
        {
            return FindPixel(color.R, color.G, color.B, tolerance);
        }

        /// <summary>
        /// FInd a pixel in the specified screen region.
        /// </summary>
        /// <param name="color">The pixel color to find.</param>
        /// <param name="region">The region of the screen.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The region is outside the screen region.</exception>
        public Point FindPixel(Color color, Rectangle region, int tolerance = 0)
        {
            return FindPixel(color.R, color.G, color.B, region, tolerance);
        }

        /// <summary>
        /// FInd a pixel in the specified screen region.
        /// </summary>
        /// <param name="color">The pixel color to find.</param>
        /// <param name="x">The screen x-coordinate of the region.</param>
        /// <param name="y">The screen y-coordinate of the region.</param>
        /// <param name="w">The screen width of the region.</param>
        /// <param name="h">The screen height of the region.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The region is outside the screen region.</exception>
        public Point FindPixel(Color color, int x, int y, int w, int h, int tolerance = 0)
        {
            return FindPixel(color.R, color.G, color.B, x, y, w, h, tolerance);
        }

        /// <summary>
        /// FInd a pixel in the screen.
        /// </summary>
        /// <param name="r">The pixel Red value to find.</param>
        /// <param name="g">The pixel Green value to find.</param>
        /// <param name="b">The pixel Blue value to find.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        public Point FindPixel(int r, int g, int b, int tolerance = 0)
        {
            return FindPixel(r, g, b, X, Y, Width, Height, tolerance);
        }

        /// <summary>
        /// FInd a pixel in the specified screen region.
        /// </summary>
        /// <param name="r">The pixel Red value to find.</param>
        /// <param name="g">The pixel Green value to find.</param>
        /// <param name="b">The pixel Blue value to find.</param>
        /// <param name="region">The region of the screen.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The region is outside the screen region.</exception>
        public Point FindPixel(int r, int g, int b, Rectangle region, int tolerance = 0)
        {
            return FindPixel(r, g, b, region.X, region.Y, region.Width, region.Height, tolerance);
        }

        /// <summary>
        /// FInd a pixel in the specified screen region.
        /// </summary>
        /// <param name="r">The pixel Red value to find.</param>
        /// <param name="g">The pixel Green value to find.</param>
        /// <param name="b">The pixel Blue value to find.</param>
        /// <param name="x">The screen x-coordinate of the region.</param>
        /// <param name="y">The screen y-coordinate of the region.</param>
        /// <param name="w">The screen width of the region.</param>
        /// <param name="h">The screen height of the region.</param>
        /// <param name="tolerance">The color tolerance.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The region is outside the screen region.</exception>
        public Point FindPixel(int r, int g, int b, int x, int y, int w, int h, int tolerance = 0)
        {
            if (x < 0 || x >= Width)
                throw new ArgumentOutOfRangeException(nameof(x));

            if (y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(y));

            if (w < 0 || w > (Width - x))
                throw new ArgumentOutOfRangeException(nameof(w));

            if (h < 0 || h > (Height - y))
                throw new ArgumentOutOfRangeException(nameof(h));

            unsafe
            {
                byte* scan0 = (byte*)dataBox.DataPointer.ToPointer();

                for (int rx = 0; rx < w; rx++)
                {
                    for (int ry = 0; ry < h; ry++)
                    {
                        int pixel = 4 * (x + rx) + dataBox.RowPitch * (y + ry);

                        byte B = scan0[pixel + 0];
                        byte G = scan0[pixel + 1];
                        byte R = scan0[pixel + 2];

                        if (ColorMatch(r, g, b, R, G, B, tolerance))
                        {
                            return new Point(rx, ry);
                        }
                    }
                }
            }

            return InvalidPoint;
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private bool ColorMatch(int r1, int g1, int b1, int r2, int g2, int b2, int tolerance = 0)
        {
            return
                Math.Abs(r1 - r2) <= tolerance &&
                Math.Abs(g1 - g2) <= tolerance &&
                Math.Abs(b1 - b2) <= tolerance;
        }
    }
}
