using Microsoft.Xna.Framework.Graphics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Paper.Core.Batcher;

internal static class TextureHelper
{
    internal static TextureHandle GetTextureHandle(this Texture2D texture, AtlasBatcher atlasBatcher)
    {
        ref int x = ref GetTextureSortKey(texture);
        return new TextureHandle(x, atlasBatcher.Id);
    }

    internal static ref T GetValueOrResize<T>(ref T[] arr, int index)
    {
        if ((uint)index < (uint)arr.Length)
            return ref arr[index];
        return ref ResizeAndGet(ref arr, index);
    }

    private static ref T ResizeAndGet<T>(ref T[] arr, int index)
    {
        int newSize = (int)BitOperations.RoundUpToPowerOf2((uint)(index + 1));
        Array.Resize(ref arr, newSize);
        return ref arr[index];
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_sortingKey")]
    private static extern ref int GetTextureSortKey(Texture texture);
}
