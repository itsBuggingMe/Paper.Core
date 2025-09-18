using System.Numerics;
using System;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Paper.Core;

public struct FastStack<T>(int capacity)
{
    private T[] _buffer = new T[capacity];
    private int _nextIndex;

    public readonly int Count => _nextIndex;
    public readonly ref T this[int index] => ref _buffer[index];

    public ref T PushRef() => ref MemoryHelper.GetValueOrResize(ref _buffer, _nextIndex++);
    public T Pop() => _buffer[--_nextIndex];
    public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_buffer), _nextIndex);
    public bool TryPop(out T value)
    {
        if (_nextIndex == 0)
        {
            value = default;
            return false;
        }

        value = Pop();
        return true;
    }

    [UnscopedRef]
    public ref FastStack<T> LazyInit()
    {
        if (_buffer is null)
            this = new(1);
        return ref this;
    }

    public void Remove(T item)
    {
        Span<T> items = AsSpan();
        for (int i = 0; i < items.Length; i++)
        {
            if (EqualityComparer<T>.Default.Equals(item, items[i]))
            {
                items[i] = Pop();
            }
        }
    }
}

public static class MemoryHelper
{
    public static ref T GetValueOrResize<T>(scoped ref T[] arr, int index)
    {
        var arrLoc = arr;
        if ((uint)index < (uint)arrLoc.Length)
            return ref arrLoc[index];
        return ref ResizeAndGet(ref arr, index);
    }

    private static ref T ResizeAndGet<T>(scoped ref T[] arr, int index)
    {
        int newSize = (int)BitOperations.RoundUpToPowerOf2((uint)(index + 1));
        Array.Resize(ref arr, newSize);
        return ref arr[index];
    }
}