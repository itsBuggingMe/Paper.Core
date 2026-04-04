using Frent;
using Microsoft.Xna.Framework;
using ImGuiNET;
using Frent.Systems;
using System;
using Frent.Marshalling;
using Frent.Core;
using System.Linq;

namespace Paper.Core.Editor;
public class ImguiEditor
{
    private readonly ImGuiRenderer _imGuiRenderer;
    private readonly Game _game;
    private readonly World _target;

    private readonly Query _allNamedEntities;
    private readonly Query _allUnnamedEntities;

    private float _targetScaling = 1;
    private Entity _selectedEntity;

    private float _leftPanelWidth = 320f;
    private float _splitterRatio = 0.45f;
    private const float SplitterThickness = 5f;

    private System.Numerics.Vector2 ButtonSize =>
        new System.Numerics.Vector2(_leftPanelWidth - ImGui.GetStyle().WindowPadding.X * 2f, 20 * _targetScaling);

    public ImguiEditor(Game game, World world)
    {
        _game = game;
        _target = world;
        _allNamedEntities = world.Query<EditorName>();
        _allUnnamedEntities = world.CreateQuery()
            .Without<EditorName>()
            .Build();

        _imGuiRenderer = new ImGuiRenderer(game);
        _imGuiRenderer.RebuildFontAtlas();
        UpdateScaling();
        game.Window.ClientSizeChanged += (s, e) => UpdateScaling();
    }

    public void Draw(GameTime gameTime)
    {
        _imGuiRenderer.BeforeLayout(gameTime);
        DrawLeftPanel();
        _imGuiRenderer.AfterLayout();
    }

    private void DrawLeftPanel()
    {
        var io = ImGui.GetIO();
        float screenHeight = io.DisplaySize.Y;
        float screenWidth = io.DisplaySize.X;

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Always);
        // Lock height to full screen; allow user to drag the resize grip to change width only.
        ImGui.SetNextWindowSizeConstraints(
            new System.Numerics.Vector2(120f, screenHeight),
            new System.Numerics.Vector2(screenWidth * 0.8f, screenHeight));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(_leftPanelWidth, screenHeight), ImGuiCond.FirstUseEver);

        var windowFlags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoBringToDisplayOnFocus
            | ImGuiWindowFlags.NoSavedSettings;

        if (ImGui.Begin("##LeftPanel", windowFlags))
        {
            // Keep _leftPanelWidth in sync so ButtonSize stays accurate.
            _leftPanelWidth = ImGui.GetWindowSize().X;

            DrawResizeHint();

            var avail = ImGui.GetContentRegionAvail();
            float topHeight = MathF.Max(20f, avail.Y * _splitterRatio - SplitterThickness * 0.5f);
            float bottomHeight = MathF.Max(20f, avail.Y - topHeight - SplitterThickness);

            // ---- Entity list (top child) ----
            ImGui.BeginChild("##EntityList", new System.Numerics.Vector2(-1, topHeight));
            DrawEntityListContent();
            ImGui.EndChild();

            // ---- Horizontal splitter ----
            DrawHorizontalSplitter(avail.Y);

            // ---- Entity inspector (bottom child) ----
            ImGui.BeginChild("##EntityDetails", new System.Numerics.Vector2(-1, bottomHeight));
            DrawEntityDetailsContent();
            ImGui.EndChild();
        }
        ImGui.End();
    }

    private void DrawResizeHint()
    {
        // Draw a subtle line on the right edge of the panel to signal it is resizable.
        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();
        uint col = ImGui.GetColorU32(ImGuiCol.Separator);
        drawList.AddLine(
            new System.Numerics.Vector2(winPos.X + winSize.X - 1f, winPos.Y),
            new System.Numerics.Vector2(winPos.X + winSize.X - 1f, winPos.Y + winSize.Y),
            col, 2f);
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

        // Tint the splitter strip slightly when hot/active.
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

        foreach (var (entity, name) in _allNamedEntities.EnumerateWithEntities<EditorName>())
        {
            if (ImGui.Button(name.Value.Name, ButtonSize))
                _selectedEntity = entity;
        }

        foreach (var entity in _allUnnamedEntities.EnumerateWithEntities())
        {
            int entityID = EntityMarshal.EntityID(entity);
            if (ImGui.Button($"{entityID}, {string.Join(',', entity.ComponentTypes.Select(s => s.Type.Name))}", ButtonSize))
                _selectedEntity = entity;
        }
    }

    private void DrawEntityDetailsContent()
    {
        if (_selectedEntity.IsNull)
        {
            ImGui.SeparatorText("Inspector");
            ImGui.Spacing();
            ImGui.TextDisabled("No entity selected.");
            ImGui.TextDisabled("Click an entity above to inspect it.");
            return;
        }

        ImGui.SeparatorText($"Entity {EntityMarshal.EntityID(_selectedEntity)}");

        foreach (var componentID in _selectedEntity.ComponentTypes)
        {
            ImGui.SeparatorText(componentID.Type.Name);
            var metadata = ComponentMeta.GetComponentMeta(componentID);

            foreach (var fieldData in metadata.ComponentFields)
            {
                if (ComponentMeta.FieldModifierTable.TryGetValue(fieldData.Type, out var intf))
                {
                    ImGui.PushID(fieldData.Name);
                    intf.Entity = _selectedEntity;
                    intf.FieldToModify = fieldData;
                    intf.UpdateUI();
                    ImGui.PopID();
                }
                else
                {
                    ImGui.Text($"<Missing Field Modifier For {fieldData.Name}>");
                }
            }
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
