﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoGame.Tests.ContentPipeline
{
    class BitmapContentTests
    {
        // Needed to copy from an array of struct to a byte array
        static byte[] ToByteArray<T>(T[] source) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                byte[] destination = new byte[source.Length * Marshal.SizeOf(typeof(T))];
                Marshal.Copy(pointer, destination, 0, destination.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        static void Fill<T>(PixelBitmapContent<T> content, T color)
            where T : struct, IEquatable<T>
        {
            var src = Enumerable.Repeat(color, content.Width * content.Height).ToArray();
            var dest = ToByteArray(src);
            content.SetPixelData(dest);
        }

        void BitmapCopyFullNoResize<T>(T color1)
            where T: struct, IEquatable<T>
        {
            var b1 = new PixelBitmapContent<T>(8, 8);
            Fill(b1, color1);
            var b2 = new PixelBitmapContent<T>(8, 8);
            BitmapContent.Copy(b1, b2);

            for (var y = 0; y < b2.Height; y++)
                for (var x = 0; x < b2.Width; x++)
                    Assert.AreEqual(color1, b2.GetPixel(x, y));
        }

        void BitmapCopyFullResize<T>(T color1, IEqualityComparer<T> comparer)
            where T : struct, IEquatable<T>
        {
            var b1 = new PixelBitmapContent<T>(8, 8);
            Fill(b1, color1);
            var b2 = new PixelBitmapContent<T>(4, 4);
            BitmapContent.Copy(b1, b2);

            for (var y = 0; y < b2.Height; y++)
                for (var x = 0; x < b2.Width; x++)
                    Assert.That(color1, Is.EqualTo(b2.GetPixel(x, y)).Using(comparer));
        }

        void BitmapConvertFullNoResize<T, U>(T color1, U color2)
            where T : struct, IEquatable<T>
            where U: struct, IEquatable<U>
        {
            var b1 = new PixelBitmapContent<T>(8, 8);
            Fill(b1, color1);
            var b2 = new PixelBitmapContent<U>(8, 8);
            BitmapContent.Copy(b1, b2);

            for (var y = 0; y < b2.Height; y++)
                for (var x = 0; x < b2.Width; x++)
                    Assert.AreEqual(color2, b2.GetPixel(x, y));
        }

        void BitmapCompressFullNoResize<T>(T color1)
            where T : struct, IEquatable<T>
        {
            var b1 = new PixelBitmapContent<T>(8, 8);
            Fill(b1, color1);
            var b2 = new Dxt1BitmapContent(8, 8);
            BitmapContent.Copy(b1, b2);
        }

        void BitmapCompressFullResize<T>(T color1)
            where T : struct, IEquatable<T>
        {
            var b1 = new PixelBitmapContent<T>(16, 16);
            Fill(b1, color1);
            var b2 = new Dxt1BitmapContent(8, 8);
            BitmapContent.Copy(b1, b2);
        }

        void BitmapCopySameRegionNoResize<T>(T color1, T color2)
            where T : struct, IEquatable<T>
        {
            var b1 = new PixelBitmapContent<T>(8, 8);
            Fill(b1, color1);
            var b2 = new PixelBitmapContent<T>(8, 8);
            Fill(b2, color2);
            BitmapContent.Copy(b1, new Rectangle(0, 0, 4, 4), b2, new Rectangle(0, 0, 4, 4));

            for (var y = 0; y < b2.Height; y++)
                for (var x = 0; x < b2.Width; x++)
                    Assert.AreEqual(x < 4 && y < 4 ? color1 : color2, b2.GetPixel(x, y));
        }

        void BitmapCopyMoveRegionNoResize<T>(T color1, T color2)
            where T : struct, IEquatable<T>
        {
            var b1 = new PixelBitmapContent<T>(8, 8);
            Fill(b1, color1);
            var b2 = new PixelBitmapContent<T>(8, 8);
            Fill(b2, color2);
            BitmapContent.Copy(b1, new Rectangle(0, 0, 4, 4), b2, new Rectangle(4, 4, 4, 4));

            for (var y = 0; y < b2.Height; y++)
                for (var x = 0; x < b2.Width; x++)
                    Assert.AreEqual(x >= 4 && y >= 4 ? color1 : color2, b2.GetPixel(x, y));
        }

        void BitmapCopyRegionResize<T>(T color1, T color2, IEqualityComparer<T> comparer)
            where T : struct, IEquatable<T>
        {
            var b1 = new PixelBitmapContent<T>(8, 8);
            Fill(b1, color1);
            var b2 = new PixelBitmapContent<T>(8, 8);
            Fill(b2, color2);
            BitmapContent.Copy(b1, new Rectangle(0, 0, 4, 4), b2, new Rectangle(0, 0, 3, 6));

            for (var y = 0; y < b2.Height; y++)
                for (var x = 0; x < b2.Width; x++)
                    Assert.That(x < 3 && y < 6 ? color1 : color2, Is.EqualTo(b2.GetPixel(x, y)).Using(comparer));
        }

        [Test]
        public void BitmapCopyFullNoResize()
        {
#if !XNA
            BitmapCopyFullNoResize<byte>(56);
#endif
            BitmapCopyFullNoResize<float>(0.56f);
            BitmapCopyFullNoResize<Color>(Color.Red);
            BitmapCopyFullNoResize<Vector4>(new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
        }

        [Test]
        public void BitmapCopyFullResize()
        {
#if !XNA
            BitmapCopyFullResize<byte>(56, ByteComparer.Equal);
#endif
            BitmapCopyFullResize<float>(0.56f, FloatComparer.Epsilon);
            BitmapCopyFullResize<Color>(Color.Red, ColorComparer.Equal);
            BitmapCopyFullResize<Vector4>(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), Vector4Comparer.Epsilon);
        }

        [Test]
        public void BitmapConvertFullNoResize()
        {
#if !XNA
            BitmapConvertFullNoResize<byte, Color>(byte.MaxValue, Color.Red);
#endif
            BitmapConvertFullNoResize<float, Color>(1.0f, Color.Red);
            BitmapConvertFullNoResize<Color, Vector4>(Color.Red, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
            BitmapConvertFullNoResize<Vector4, Color>(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), Color.Red);
        }

        [Test]
#if DESKTOPGL
        [Ignore("NVidia native library could not be loaded on Mac build server.")]
#endif
        public void BitmapCompressFullNoResize()
        {
#if !XNA
            BitmapCompressFullNoResize<byte>(56);
#endif
            BitmapCompressFullNoResize<float>(0.56f);
            BitmapCompressFullNoResize<Color>(Color.Red);
            BitmapCompressFullNoResize<Vector4>(new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
        }

        [Test]
#if DESKTOPGL
        [Ignore("NVidia native library could not be loaded on Mac build server.")]
#endif
        public void BitmapCompressFullResize()
        {
#if !XNA
            BitmapCompressFullResize<byte>(56);
#endif
            BitmapCompressFullResize<float>(0.56f);
            BitmapCompressFullResize<Color>(Color.Red);
            BitmapCompressFullResize<Vector4>(new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
        }

        [Test]
        public void BitmapCopySameRegionNoResize()
        {
#if !XNA
            BitmapCopySameRegionNoResize<byte>(56, 48);
#endif
            BitmapCopySameRegionNoResize<float>(0.56f, 0.48f);
            BitmapCopySameRegionNoResize<Color>(Color.Red, Color.Blue);
            BitmapCopySameRegionNoResize<Vector4>(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
        }

        [Test]
        public void BitmapCopyMoveRegionNoResize()
        {
#if !XNA
            BitmapCopyMoveRegionNoResize<byte>(56, 48);
#endif
            BitmapCopyMoveRegionNoResize<float>(0.56f, 0.48f);
            BitmapCopyMoveRegionNoResize<Color>(Color.Red, Color.Blue);
            BitmapCopyMoveRegionNoResize<Vector4>(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
        }

        [Test]
        public void BitmapCopyRegionResize()
        {
#if !XNA
            BitmapCopyRegionResize<byte>(56, 48, ByteComparer.Equal);
#endif
            BitmapCopyRegionResize<float>(0.56f, 0.48f, FloatComparer.Epsilon);
            BitmapCopyRegionResize<Color>(Color.Red, Color.Blue, ColorComparer.Equal);
            BitmapCopyRegionResize<Vector4>(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), Vector4Comparer.Epsilon);
        }
    }
}
