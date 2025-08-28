using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Paper.Core.Batcher;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionColorTexture2D(Vector2 position, Color color, Vector2 textureCoord) : IVertexType
{
    public Vector2 Position = position;
    public Color Color = color;
    public Vector2 TextureCoordinate = textureCoord;

    public VertexDeclaration VertexDeclaration => _vertexDeclaration;
    private static readonly VertexDeclaration _vertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        );
}
