namespace Paper.Core.Batcher;
public readonly struct TextureHandle : IEquatable<TextureHandle>
{
    internal readonly int Value;
    internal readonly int BatcherId;
    internal TextureHandle(int value, int batcherId) => (Value, BatcherId) = (value, batcherId);
    public override int GetHashCode() => Value.GetHashCode();
    public bool Equals(TextureHandle other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is TextureHandle other && Equals(other);
    public static bool operator ==(TextureHandle left, TextureHandle right) => left.Equals(right);
    public static bool operator !=(TextureHandle left, TextureHandle right) => !left.Equals(right);
}
