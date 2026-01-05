using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using FixedMathSharp;
using FixedMathSharp.Utility;
using Microsoft.Xna.Framework.Graphics;
using SoftFloat;
using Steamworks;
using Steamworks.Data;
using Stride.Core.Mathematics;

namespace NFMWorld.Mad;

public static class Extensions
{
    extension(VertexBuffer vertexBuffer)
    {
        [Conditional("DEBUG")]
        private void ErrorCheck<T>(
            ReadOnlySpan<T> data,
            int startIndex,
            int elementCount,
            int vertexStride
        ) where T : struct {
            if (data.IsEmpty)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if ((startIndex + elementCount > data.Length) || elementCount <= 0)
            {
                throw new InvalidOperationException(
                    "The array specified in the data parameter is not the correct size for the amount of data requested."
                );
            }
            if (	elementCount > 1 &&
                    (elementCount * vertexStride) > (vertexBuffer.VertexCount * vertexBuffer.VertexDeclaration.VertexStride)	)
            {
                throw new InvalidOperationException(
                    "The vertex stride is larger than the vertex buffer."
                );
            }

            int elementSizeInBytes = Unsafe.SizeOf<T>();
            if (vertexStride == 0)
            {
                vertexStride = elementSizeInBytes;
            }
            if (vertexStride < elementSizeInBytes)
            {
                throw new ArgumentOutOfRangeException(
                    $"The vertex stride must be greater than or equal to the size of the specified data ({elementSizeInBytes})."
                );
            }
        }
        
        public unsafe void SetDataEXT<T>(ReadOnlySpan<T> data, SetDataOptions options = SetDataOptions.None)
            where T : unmanaged
        {
            vertexBuffer.ErrorCheck(data, 0, data.Length, Unsafe.SizeOf<T>());

            fixed (T* ptr = data)
            {
                vertexBuffer.SetDataPointerEXT(0, (IntPtr)ptr, data.AsBytes().Length, options);
            }
        }

        public unsafe void SetDataEXT<T>(List<T> data, SetDataOptions options = SetDataOptions.None)
            where T : unmanaged
        {
            vertexBuffer.SetDataEXT(CollectionsMarshal.AsSpan(data), options);
        }
    }

    extension(IndexBuffer indexBuffer)
    {
        [Conditional("DEBUG")]
        private void ErrorCheck<T>(
            ReadOnlySpan<T> data,
            int startIndex,
            int elementCount
        ) where T : struct {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (data.Length < (startIndex + elementCount))
            {
                throw new InvalidOperationException("The array specified in the data parameter is not the correct size for the amount of data requested.");
            }
        }

        public unsafe void SetDataEXT<T>(ReadOnlySpan<T> data, SetDataOptions options = SetDataOptions.None)
            where T : unmanaged
        {
            indexBuffer.ErrorCheck<T>(data, 0, data.Length);

            fixed (T* ptr = data)
            {
                indexBuffer.SetDataPointerEXT(0, (IntPtr)ptr, data.AsBytes().Length, options);
            }
        }
        
        public unsafe void SetDataEXT<T>(List<T> data, SetDataOptions options = SetDataOptions.None)
            where T : unmanaged
        {
            indexBuffer.SetDataEXT(CollectionsMarshal.AsSpan(data), options);
        }
    }

    extension(Connection connection)
    {
        public unsafe Result SendMessage<T>(Span<T> data, SendType sendType = SendType.Reliable)
            where T : unmanaged
        {
            fixed (T* ptr = data)
                return connection.SendMessage((IntPtr) ptr, data.AsBytes().Length, sendType);
        }

        public unsafe Result SendMessage<T>(ReadOnlySpan<T> data, SendType sendType = SendType.Reliable)
            where T : unmanaged
        {
            fixed (T* ptr = data)
                return connection.SendMessage((IntPtr) ptr, data.AsBytes().Length, sendType);
        }
    }

    extension(RectangleF rectangle)
    {
        public bool Contains(Vector2 vec) => rectangle.Contains(vec.X, vec.Y);
    }
}