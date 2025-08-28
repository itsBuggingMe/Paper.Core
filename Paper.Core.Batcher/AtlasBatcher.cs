using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using STVector = System.Numerics.Vector2;
using STMatrix = System.Numerics.Matrix4x4;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System.Data;
using System.Buffers;


namespace Paper.Core.Batcher;

public class AtlasBatcher
{
    internal const int DefaultInitialSpriteCapacity = 42;
    internal const int DefaultAtlasSize = 512;
    internal const int VerticiesPerQuad = 4;
    internal const int IndiciesPerQuad = 6;
    internal const int MaxQuadsPerBatch = 65532;

    private static int _nextId = 0;

    internal readonly int Id = Interlocked.Increment(ref _nextId);

    public Texture2D _atlas;
    private SkylinePacker _packer;
    private HandleLookup[] _handleLookup = [];
    private Stack<(Texture2D Texture, Rectangle AtlasBounds, bool NeedsDispose)> _texturesToBuild = [];

    private GraphicsDevice _graphics;

    private bool _verticiesIndiciesDirty = false;
    private DynamicVertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private VertexPositionColorTexture2D[] _verticies;
    private Effect _spriteBatcher;
    private uint[] _indicies;
    private int _nextVertexIndex;

    public AtlasBatcher(
        GraphicsDevice graphicsDevice,
        ContentManager contentManager,
        int initalSpriteCapacity = DefaultInitialSpriteCapacity,
        int initalAtlasSize = DefaultAtlasSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initalAtlasSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initalSpriteCapacity);

        _graphics = graphicsDevice;

        _verticies = new VertexPositionColorTexture2D[initalSpriteCapacity * VerticiesPerQuad];
        _indicies = new uint[initalSpriteCapacity * IndiciesPerQuad];
        WriteIndicies(_indicies, 0, initalSpriteCapacity);
        _handleLookup = new HandleLookup[8];

        _packer = new SkylinePacker(initalAtlasSize, initalAtlasSize, Resize);
        _atlas = new Texture2D(graphicsDevice, initalAtlasSize, initalAtlasSize);

        _vertexBuffer = null!;
        _indexBuffer = null!;

        _spriteBatcher = contentManager.Load<Effect>("sprite_batcher");

        _verticiesIndiciesDirty = true;
    }

    public TextureHandle CreateHandle(Texture2D texture)
    {
        var result = texture.GetTextureHandle(this);

        ref HandleLookup coordToSet = ref TextureHelper.GetValueOrResize(ref _handleLookup, result.Value);

        if (coordToSet.IsDefault)
            InitalizeTextureCoordsFor(texture, ref coordToSet);

        return result;
    }

    public BatcherSprite Draw(Texture2D texture, Vector2 position) => Draw(CreateHandle(texture), position);

    public BatcherSprite Draw(TextureHandle handle, Vector2 position, Vector2 origin)
    {
        if (handle.BatcherId != Id)
            ThrowInvalidTextureHandle();

        HandleLookup[] coords = _handleLookup;
        int index = handle.Value;

        if (!((uint)index < (uint)coords.Length))
            ThrowUnreachableException();

        ref HandleLookup slot = ref coords[index];

        var verts = EnsureCapacity();

        _nextVertexIndex += VerticiesPerQuad;

        return new BatcherSprite(position, origin, _atlas.Width, _atlas.Height, slot.Bounds.Width, slot.Bounds.Height, verts)
            .Initalize(slot)
            .Tint(Color.White);
    }

    public BatcherSprite Draw(TextureHandle handle, Vector2 position) => Draw(handle, position, default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<VertexPositionColorTexture2D> EnsureCapacity()
    {
        int nextIndex = _nextVertexIndex;
        var verticies = _verticies;

        if (nextIndex + VerticiesPerQuad > verticies.Length)
            return Double();

        ref var firstVert = ref MemoryMarshal.GetArrayDataReference(verticies);
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref firstVert, nextIndex), VerticiesPerQuad);
    }

    private Span<VertexPositionColorTexture2D> Double()
    {
        _verticiesIndiciesDirty = true;

        int oldLength = _verticies.Length;
        int currentQuadCount = _verticies.Length / VerticiesPerQuad;

        Array.Resize(ref _verticies, currentQuadCount * VerticiesPerQuad * 2);
        Array.Resize(ref _indicies, currentQuadCount * IndiciesPerQuad * 2);

        WriteIndicies(_indicies, currentQuadCount, currentQuadCount);

        return _verticies.AsSpan(oldLength - VerticiesPerQuad, VerticiesPerQuad);
    }

    public void Submit(Matrix? view = null, Matrix? proj = null, BlendState? blendState = null, SamplerState? samplerState = null, DepthStencilState? depthStencilState = null, RasterizerState? rasterizerState = null)
    {
        BuildTextureAtlas();

        if (_nextVertexIndex == 0)
            return;

        if (_verticiesIndiciesDirty)
        {
            _indexBuffer = new IndexBuffer(_graphics, IndexElementSize.ThirtyTwoBits, _indicies.Length, BufferUsage.WriteOnly);
            _indexBuffer.SetData(_indicies);
            _vertexBuffer = new DynamicVertexBuffer(_graphics, default(VertexPositionColorTexture2D).VertexDeclaration, _verticies.Length, BufferUsage.WriteOnly);
            _verticiesIndiciesDirty = false;
        }

        // buffers
        _vertexBuffer.SetData(_verticies);
        _graphics.SetVertexBuffer(_vertexBuffer);
        _graphics.Indices = _indexBuffer;

        // states
        _graphics.BlendState = blendState ?? BlendState.AlphaBlend;
        _graphics.SamplerStates[0] = samplerState ?? SamplerState.PointClamp;
        _graphics.DepthStencilState = depthStencilState ?? DepthStencilState.None;
        _graphics.RasterizerState = RasterizerState.CullNone;

        // apply
        Viewport viewport = _graphics.Viewport;

        _spriteBatcher.Parameters["Atlas"].SetValue(_atlas);
        _spriteBatcher.Parameters["Transform"].SetValue(
            (view ?? Matrix.Identity) * 
            (proj ?? Matrix.CreateOrthographicOffCenter(viewport.X, viewport.Width, viewport.Height, viewport.Y, 0, 1))
            );

        foreach (EffectPass pass in _spriteBatcher.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2 * _nextVertexIndex / VerticiesPerQuad);
        }

        //reset
        _nextVertexIndex = 0;
    }

    private void BuildTextureAtlas()
    {
        while (_texturesToBuild.TryPop(out var res))
        {
            (Texture2D texture, Rectangle atlasBounds, bool dispose) = res;

            int size = texture.Width * texture.Height;
            Color[] colors = ArrayPool<Color>.Shared.Rent(size);

            texture.GetData(colors, 0, size);
            _atlas.SetData(0, atlasBounds, colors, 0, size);

            ArrayPool<Color>.Shared.Return(colors);

            if (dispose)
                texture.Dispose();
        }
    }

    private static void WriteIndicies(uint[] indicies, int quadStart, int quadCount)
    {
        uint baseIndex = (uint)(quadStart * VerticiesPerQuad);
        uint start = (uint)quadStart * IndiciesPerQuad;
        uint endIndexIndex = start + (uint)quadCount * IndiciesPerQuad;
        for (uint i = start; i < endIndexIndex; i += IndiciesPerQuad)
        {
            indicies[i + 0] = baseIndex + 0u;
            indicies[i + 1] = baseIndex + 1u;
            indicies[i + 2] = baseIndex + 2u;
            indicies[i + 3] = baseIndex + 1u;
            indicies[i + 4] = baseIndex + 3u;
            indicies[i + 5] = baseIndex + 2u;
            baseIndex += VerticiesPerQuad;
        }
    }

    private void InitalizeTextureCoordsFor(Texture2D texture2D, ref HandleLookup coords)
    {
        Point position = _packer.Pack(texture2D.Width, texture2D.Height);

        Rectangle atlasBounds = new(position, texture2D.Bounds.Size);

        _texturesToBuild.Push((texture2D, atlasBounds, false));

        coords.Bounds = atlasBounds;

        SetTextCoords(ref coords, Vector2.One / _atlas.Bounds.Size.ToVector2());
    }

    private Point Resize(Point value)
    {
        Point newSize = new(value.X * 2, value.Y * 2);

        _texturesToBuild.Push((_atlas, new Rectangle(0, 0, value.X, value.Y), true));

        _atlas = new Texture2D(_graphics, newSize.X, newSize.Y);

        var recep = Vector2.One / newSize.ToVector2();
        foreach (ref var coord in _handleLookup.AsSpan())
        {
            SetTextCoords(ref coord, recep);
        }

        return newSize;
    }

    private static void SetTextCoords(ref HandleLookup coords, Vector2 atlasSizeReciprical)
    {
        Vector2 position = coords.Bounds.Location.ToVector2();
        Vector2 brCorner = position + coords.Bounds.Size.ToVector2();

        position *= atlasSizeReciprical;
        brCorner *= atlasSizeReciprical;

        coords.TL = position;
        coords.BR = brCorner;

        coords.TR = new Vector2(brCorner.X, position.Y);
        coords.BL = new Vector2(position.X, brCorner.Y);
    }

    public ref struct BatcherSprite
    {
        private ref VertexPositionColorTexture2D _start;

        private STVector _origin;
        private float _atlasWidth;
        private float _atlasHeight;

        internal BatcherSprite(Vector2 position, Vector2 origin, int aWidth, int aHeight, int tWidth, int tHeight, Span<VertexPositionColorTexture2D> verticies)
        {
            Debug.Assert(verticies.Length == 4);
            _start = ref MemoryMarshal.GetReference(verticies);

            _atlasWidth = aWidth;
            _atlasHeight = aHeight;
            _origin = Unsafe.BitCast<Vector2, STVector>(origin);

            STVector pos = Unsafe.BitCast<Vector2, STVector>(position);

            TL = pos;
            TR = pos + new STVector(tWidth, 0);
            BL = pos + new STVector(0, tHeight);
            BR = pos + new STVector(tWidth, tHeight);
        }

        private ref STVector TL => ref Unsafe.As<Vector2, STVector>(ref _start.Position);
        private ref STVector TR => ref Unsafe.As<Vector2, STVector>(ref Unsafe.Add(ref _start, 1).Position);
        private ref STVector BL => ref Unsafe.As<Vector2, STVector>(ref Unsafe.Add(ref _start, 2).Position);
        private ref STVector BR => ref Unsafe.As<Vector2, STVector>(ref Unsafe.Add(ref _start, 3).Position);

        private ref VertexPositionColorTexture2D VTL => ref _start;
        private ref VertexPositionColorTexture2D VTR => ref Unsafe.Add(ref _start, 1);
        private ref VertexPositionColorTexture2D VBL => ref Unsafe.Add(ref _start, 2);
        private ref VertexPositionColorTexture2D VBR => ref Unsafe.Add(ref _start, 3);

        internal BatcherSprite Initalize(HandleLookup textCoord)
        {
            VTL.TextureCoordinate = textCoord.TL;
            VTR.TextureCoordinate = textCoord.TR;
            VBL.TextureCoordinate = textCoord.BL;
            VBR.TextureCoordinate = textCoord.BR;
            return this;
        }

        public BatcherSprite Rotate(float radians)
        {
            (float sin, float cos) = radians switch
            {
                0 => (0, 1),
                _ => (MathF.Sin(radians), MathF.Cos(radians))
            };

            Rotate(ref TL, sin, cos, _origin);
            Rotate(ref TR, sin, cos, _origin);
            Rotate(ref BL, sin, cos, _origin);
            Rotate(ref BR, sin, cos, _origin);

            return this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Rotate(ref STVector vec, float sin, float cos, STVector origin)
            {
                vec -= origin;
                float x = vec.X;
                vec.X = x * cos - vec.Y * sin;
                vec.Y = x * sin + vec.Y * cos;
                vec += origin;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BatcherSprite SetSource(Rectangle sourceRectangle)
        {
            Vector64<float> lower = Vector64.Create((float)sourceRectangle.X, sourceRectangle.Y);
            Vector64<float> higher = Vector64.Create((float)sourceRectangle.Width, sourceRectangle.Height);
            Vector128<float> corners = Vector128.Create(lower, lower + higher);

            corners /= Vector128.Create(_atlasWidth, _atlasHeight, _atlasWidth, _atlasHeight);
            //lx,ty,rx,by
            VTL.TextureCoordinate = Unsafe.BitCast<Vector64<float>, Vector2>(corners.GetLower());
            VBR.TextureCoordinate = Unsafe.BitCast<Vector64<float>, Vector2>(corners.GetUpper());

            corners = Vector128.Shuffle(corners, Vector128.Create(2, 1, 0, 3));

            //rx,ty,lx,by
            VTR.TextureCoordinate = Unsafe.BitCast<Vector64<float>, Vector2>(corners.GetLower());
            VBL.TextureCoordinate = Unsafe.BitCast<Vector64<float>, Vector2>(corners.GetUpper());

            return this;
        }

        public BatcherSprite Transform(Matrix matrix)
        {
            TL = STVector.Transform(TL, Unsafe.BitCast<Matrix, STMatrix>(matrix));
            TR = STVector.Transform(TR, Unsafe.BitCast<Matrix, STMatrix>(matrix));
            BL = STVector.Transform(BL, Unsafe.BitCast<Matrix, STMatrix>(matrix));
            BR = STVector.Transform(BR, Unsafe.BitCast<Matrix, STMatrix>(matrix));
            return this;
        }

        public BatcherSprite FlipVertically()
        {
            ref var a = ref VTL.TextureCoordinate.Y;
            ref var b = ref VBR.TextureCoordinate.Y;
            (a, b) = (b, a);
            return this;
        }

        public BatcherSprite FlipHorizontally()
        {
            ref var a = ref VTL.TextureCoordinate.X;
            ref var b = ref VBR.TextureCoordinate.X;
            (a, b) = (b, a);
            return this;
        }

        public BatcherSprite ApplyEffect(SpriteEffects effects)
        {
            if ((effects & SpriteEffects.FlipHorizontally) != 0)
                FlipHorizontally();
            if ((effects & SpriteEffects.FlipVertically) != 0)
                FlipVertically();
            return this;
        }

        public BatcherSprite Scale(Vector2 multipler)
        {
            if (Vector256.IsHardwareAccelerated)
            {
                //todo: check if jit opts this
                Vector256<float> pos = Vector256.Create(TL.X, TL.Y, TR.X, TR.Y, BL.X, BL.Y, BR.X, BR.Y);
                Vector256<float> origin = Vector256.Create(Unsafe.BitCast<STVector, long>(_origin)).AsSingle();

                pos -= origin;
                pos *= Vector256.Create(Unsafe.BitCast<Vector2, long>(multipler)).AsSingle();
                pos += origin;

                TL = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetLower().GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetLower().GetUpper());
                BL = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetUpper().GetLower());
                BR = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetUpper().GetUpper());
            }
            else
            {
                Vector128<float> lower = Vector128.Create(TL.X, TL.Y, TR.X, TR.Y);
                Vector128<float> upper = Vector128.Create(BL.X, BL.Y, BR.X, BR.Y);
                Vector128<float> origin = Vector128.Create(Unsafe.BitCast<STVector, long>(_origin)).AsSingle();

                lower -= origin;
                upper -= origin;

                Vector128<float> mul = Vector128.Create(Unsafe.BitCast<Vector2, long>(multipler)).AsSingle();
                lower *= mul;
                upper *= mul;

                lower += origin;
                upper -= origin;

                TL = Unsafe.BitCast<Vector64<float>, STVector>(lower.GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(lower.GetUpper());
                BL = Unsafe.BitCast<Vector64<float>, STVector>(upper.GetLower());
                BR = Unsafe.BitCast<Vector64<float>, STVector>(upper.GetUpper());
            }

            return this;
        }

        public BatcherSprite Scale(float multipler)
        {
            if (Vector256.IsHardwareAccelerated)
            {
                //todo: check if jit opts this
                Vector256<float> pos = Vector256.Create(TL.X, TL.Y, TR.X, TR.Y, BL.X, BL.Y, BR.X, BR.Y);
                Vector256<float> origin = Vector256.Create(Unsafe.BitCast<STVector, long>(_origin)).AsSingle();

                pos -= origin;
                pos *= Vector256.Create(multipler);
                pos += origin;

                TL = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetLower().GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetLower().GetUpper());
                BL = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetUpper().GetLower());
                BR = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetUpper().GetUpper());
            }
            else
            {
                Vector128<float> lower = Vector128.Create(TL.X, TL.Y, TR.X, TR.Y);
                Vector128<float> upper = Vector128.Create(BL.X, BL.Y, BR.X, BR.Y);
                Vector128<float> origin = Vector128.Create(Unsafe.BitCast<STVector, long>(_origin)).AsSingle();
                lower += origin;
                upper += origin;

                Vector128<float> mul = Vector128.Create(multipler);
                lower *= mul;
                upper *= mul;

                lower -= origin;
                upper -= origin;

                TL = Unsafe.BitCast<Vector64<float>, STVector>(lower.GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(lower.GetUpper());
                BL = Unsafe.BitCast<Vector64<float>, STVector>(upper.GetLower());
                BR = Unsafe.BitCast<Vector64<float>, STVector>(upper.GetUpper());
            }

            return this;
        }

        public BatcherSprite Tint(Color color)
        {
            VTL.Color = color;
            VTR.Color = color;
            VBL.Color = color;
            VBR.Color = color;
            return this;
        }
    }

    private static void ThrowInvalidTextureHandle()
    {
        throw new ArgumentException("Texture handle invalid.");
    }

    private static void ThrowUnreachableException()
    {
        throw new UnreachableException();
    }
}