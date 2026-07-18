using Frent;
using Microsoft.Xna.Framework;
using ImGuiNET;
using Frent.Systems;
using System;
using Frent.Marshalling;
using Frent.Core;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace Paper.Core.Editor;
public class ImguiEditor
{
    public ImGuiRenderer Renderer => _imGuiRenderer;
    private readonly ImGuiRenderer _imGuiRenderer;
    private readonly Game _game;
    private readonly World _target;
    private readonly ComponentDrawer _componentDrawer;
    private ComponentID? _componentToRemove;

    private readonly Query _allNamedEntities;
    private readonly Query _allUnnamedEntities;

    private float _targetScaling = 1;
    public Entity SelectedEntity { get; set; }

    private Texture2D? _viewedTexture;
    private string _viewedTextureName = string.Empty;
    private bool _textureViewerOpen;
    private float _textureViewerZoom = 1f;

    private float _leftPanelWidth = 1000f;
    private float _splitterRatio = 0.45f;
    private const float SplitterThickness = 5f;
    private const double FpsSampleDuration = 0.5;
    private double _fpsSampleElapsed;
    private int _fpsSampleFrames;
    private double _displayedFps;
    private long _lastFpsTimestamp;

    private System.Numerics.Vector2 ButtonSize =>
        new System.Numerics.Vector2(_leftPanelWidth - ImGui.GetStyle().WindowPadding.X * 2f, 20 * _targetScaling);

    public ImguiEditor(Game game, World world)
    {
        _game = game;
        _target = world;
        _componentDrawer = new ComponentDrawer(this);
        _allNamedEntities = world.Query<EditorName>();
        _allUnnamedEntities = world.CreateQuery()
            .Without<EditorName>()
            .Build();

        _imGuiRenderer = new ImGuiRenderer(game);
        _imGuiRenderer.RebuildFontAtlas();
        UpdateScaling();
        game.Window.ClientSizeChanged += (s, e) => UpdateScaling();

        ComponentMetadata.RegisterBuiltinConverters(typeof(ImguiEditor).Assembly);
    }

    public void RegisterConverters(Assembly assembly)
    {
        ComponentMetadata.RegisterBuiltinConverters(assembly);
    }

    public void Draw(GameTime gameTime)
    {
        _imGuiRenderer.BeforeLayout(gameTime);
        DrawLeftPanel();
        DrawTextureViewer();
        DrawFpsCounter();
        _imGuiRenderer.AfterLayout();
    }

    private void DrawFpsCounter()
    {
        long timestamp = Stopwatch.GetTimestamp();
        if (_lastFpsTimestamp != 0)
        {
            double elapsed = (double)(timestamp - _lastFpsTimestamp) / Stopwatch.Frequency;
            _fpsSampleElapsed += elapsed;
            _fpsSampleFrames++;

            if (_displayedFps == 0 || _fpsSampleElapsed >= FpsSampleDuration)
            {
                _displayedFps = _fpsSampleFrames / _fpsSampleElapsed;
                _fpsSampleElapsed = 0;
                _fpsSampleFrames = 0;
            }
        }
        _lastFpsTimestamp = timestamp;

        System.Numerics.Vector2 displaySize = ImGui.GetIO().DisplaySize;
        ImGui.SetNextWindowPos(
            new System.Numerics.Vector2(displaySize.X - 10f, 10f),
            ImGuiCond.Always,
            new System.Numerics.Vector2(1f, 0f));
        ImGui.SetNextWindowBgAlpha(0.65f);

        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoInputs;

        if (ImGui.Begin("##FpsCounter", flags))
            ImGui.TextUnformatted($"{_displayedFps:0} FPS");
        ImGui.End();
    }

    public void OpenTextureViewer(Texture2D texture, string name)
    {
        ArgumentNullException.ThrowIfNull(texture);

        if (!ReferenceEquals(_viewedTexture, texture))
            _textureViewerZoom = 1f;

        _viewedTexture = texture;
        _viewedTextureName = string.IsNullOrWhiteSpace(name) ? texture.Name ?? "Texture" : name;
        _textureViewerOpen = true;
    }

    private void DrawTextureViewer()
    {
        if (!_textureViewerOpen || _viewedTexture is null)
            return;

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(800f, 600f), ImGuiCond.FirstUseEver);

        if (ImGui.Begin($"{_viewedTextureName}###TextureViewer", ref _textureViewerOpen))
        {
            Texture2D texture = _viewedTexture;
            if (texture.IsDisposed)
            {
                ImGui.TextDisabled("This texture is no longer available.");
            }
            else
            {
                ImGui.TextDisabled($"{texture.Width} x {texture.Height}");
                ImGui.SameLine();
                ImGui.TextDisabled($"{_textureViewerZoom * 100f:0}%");
                ImGui.SameLine();
                if (ImGui.SmallButton("Reset zoom"))
                    _textureViewerZoom = 1f;
                ImGui.Separator();

                const ImGuiWindowFlags canvasFlags = ImGuiWindowFlags.HorizontalScrollbar
                    | ImGuiWindowFlags.NoScrollWithMouse;
                if (ImGui.BeginChild(
                    "##TextureCanvas",
                    System.Numerics.Vector2.Zero,
                    ImGuiChildFlags.None,
                    canvasFlags))
                {
                    System.Numerics.Vector2 available = ImGui.GetContentRegionAvail();
                    float previousZoom = _textureViewerZoom;
                    float mouseWheel = ImGui.GetIO().MouseWheel;
                    if (ImGui.IsWindowHovered() && mouseWheel != 0f)
                    {
                        _textureViewerZoom = Math.Clamp(
                            _textureViewerZoom * MathF.Pow(1.2f, mouseWheel),
                            0.25f,
                            16f);
                    }

                    float fitScale = MathF.Min(available.X / texture.Width, available.Y / texture.Height);
                    float scale = fitScale * _textureViewerZoom;
                    if (scale > 0f)
                    {
                        System.Numerics.Vector2 imageSize = new(texture.Width * scale, texture.Height * scale);
                        System.Numerics.Vector2 cursor = ImGui.GetCursorPos();
                        ImGui.SetCursorPos(cursor + new System.Numerics.Vector2(
                            MathF.Max(0f, (available.X - imageSize.X) * 0.5f),
                            MathF.Max(0f, (available.Y - imageSize.Y) * 0.5f)));
                        ImGui.Image(_imGuiRenderer.BindTexture(texture), imageSize);
                    }

                    if (_textureViewerZoom != previousZoom)
                    {
                        float zoomRatio = _textureViewerZoom / previousZoom;
                        System.Numerics.Vector2 mouseInWindow = ImGui.GetIO().MousePos - ImGui.GetWindowPos();
                        ImGui.SetScrollX((ImGui.GetScrollX() + mouseInWindow.X) * zoomRatio - mouseInWindow.X);
                        ImGui.SetScrollY((ImGui.GetScrollY() + mouseInWindow.Y) * zoomRatio - mouseInWindow.Y);
                    }
                }
                ImGui.EndChild();
            }
        }
        ImGui.End();

        if (!_textureViewerOpen)
            _viewedTexture = null;
    }

    private void DrawLeftPanel()
    {
        var io = ImGui.GetIO();
        float screenHeight = io.DisplaySize.Y;
        float screenWidth = io.DisplaySize.X;

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Always);

        ImGui.SetNextWindowSizeConstraints(
            new System.Numerics.Vector2(120f, screenHeight),
            new System.Numerics.Vector2(screenWidth * 0.8f, screenHeight));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(_leftPanelWidth, screenHeight), ImGuiCond.FirstUseEver);

        var windowFlags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse;

        if (ImGui.Begin("##LeftPanel", windowFlags))
        {
            _leftPanelWidth = ImGui.GetWindowSize().X;

            var avail = ImGui.GetContentRegionAvail();
            float topHeight = MathF.Max(20f, avail.Y * _splitterRatio - SplitterThickness * 0.5f);
            float bottomHeight = MathF.Max(20f, avail.Y - topHeight - SplitterThickness);

            ImGui.BeginChild("##EntityList", new System.Numerics.Vector2(-1, topHeight));
            DrawEntityListContent();
            ImGui.EndChild();

            DrawHorizontalSplitter(avail.Y);

            ImGui.BeginChild("##EntityDetails", new System.Numerics.Vector2(-1, bottomHeight));
            DrawEntityDetailsContent();
            ImGui.EndChild();
        }
        ImGui.End();
    }

    private void DrawHorizontalSplitter(float totalAvailHeight)
    {
        var drawList = ImGui.GetWindowDrawList();
        var splitterPos = ImGui.GetCursorScreenPos();
        float width = ImGui.GetContentRegionAvail().X;

        ImGui.InvisibleButton("##HSplitter", new System.Numerics.Vector2(width, SplitterThickness));

        bool hovered = ImGui.IsItemHovered();
        bool active = ImGui.IsItemActive();

        if (hovered || active)
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);

        if (active)
        {
            float delta = ImGui.GetIO().MouseDelta.Y / totalAvailHeight;
            _splitterRatio = Math.Clamp(_splitterRatio + delta, 0.1f, 0.9f);
        }

        uint col = active
            ? ImGui.GetColorU32(ImGuiCol.SeparatorActive)
            : hovered
                ? ImGui.GetColorU32(ImGuiCol.SeparatorHovered)
                : ImGui.GetColorU32(ImGuiCol.Separator);

        drawList.AddRectFilled(
            splitterPos,
            new System.Numerics.Vector2(splitterPos.X + width, splitterPos.Y + SplitterThickness),
            col);
    }

    private void DrawEntityListContent()
    {
        ImGui.SeparatorText("Entities");

        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new System.Numerics.Vector2(0f, 0.5f));

        foreach (var (entity, name) in _allNamedEntities.EnumerateWithEntities<EditorName>())
        {
            if (ImGui.Button(name.Value.Name, ButtonSize))
                SelectedEntity = entity;
        }

        foreach (var entity in _allUnnamedEntities.EnumerateWithEntities())
        {
            int entityID = EntityMarshal.EntityID(entity);
            if (ImGui.Button($"{entityID}: [{string.Join(',', entity.ComponentTypes.Select(s => s.Type.Name))}] {(entity.TagTypes.Length > 0 ? $"Tags: [{string.Join(", ", entity.TagTypes.Select(t => t.Type.Name))}]" : "")}", ButtonSize))
                SelectedEntity = entity;
        }

        ImGui.PopStyleVar();
    }

    private void DrawEntityDetailsContent()
    {
        if (!SelectedEntity.IsAlive)
        {
            ImGui.SeparatorText("Inspector");
            ImGui.Spacing();
            ImGui.TextDisabled("No entity selected.");
            ImGui.TextDisabled("Click an entity to inspect it.");
            return;
        }

        ImGui.SeparatorText($"Entity {EntityMarshal.EntityID(SelectedEntity)}");

        if (ImGui.Button("Delete"))
        {
            SelectedEntity.Delete();
            return;
        }

        _componentToRemove = null;
        SelectedEntity.EnumerateComponents(_componentDrawer);

        if (_componentToRemove is ComponentID componentID && SelectedEntity.IsAlive)
            SelectedEntity.Remove(componentID);
    }

    private void DrawComponent<T>(ref T component)
    {
        ComponentID componentID = Component<T>.ID;
        ImGui.SeparatorText(componentID.Type.Name);
        ImGui.PushID(componentID.Type.FullName);
        if (ImGui.SmallButton("Remove component"))
            _componentToRemove = componentID;
        ImGui.PopID();

        if (_componentToRemove == componentID)
            return;

        EditorMember[] members = ComponentMetadata.GetComponentMembers(componentID);

        foreach (EditorMember member in members)
        {
            member.Initialize(component);
            ImGui.PushID(member.PositionalHash);
            if (member.IsReadOnly)
                ImGui.BeginDisabled();

            member.Converter.CallDisplay(SelectedEntity, componentID, member);

            if (member.IsReadOnly)
                ImGui.EndDisabled();
            ImGui.PopID();
            component = member.GetContainingValue<T>()!;
        }
    }

    private sealed class ComponentDrawer(ImguiEditor editor) : IGenericAction
    {
        public void Invoke<T>(ref T component)
        {
            editor.DrawComponent(ref component);
        }
    }

    private void UpdateScaling()
    {
        float scale = _game.Window.ClientBounds.Size.ToVector2().Length() / new Vector2(1920, 1080).Length();

        var styles = ImGui.GetStyle();

        // prev scaling * mul factor = new scaling
        // mul factor = new scaling / prev scaling
        styles.ScaleAllSizes(scale / _targetScaling);

        if (_targetScaling != scale)
        {
            //TODO: better scaling?
            ImGui.GetIO().FontGlobalScale = scale;
            _targetScaling = scale;
        }
    }
}
