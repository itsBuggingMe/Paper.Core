using Frent;
using Microsoft.Xna.Framework;
using ImGuiNET;
using Frent.Systems;
using System;
using Frent.Marshalling;
using Frent.Core;
using System.Linq;
using System.Collections.Generic;

namespace Paper.Core.Editor;
public class ImguiEditor
{
    public ImGuiRenderer Renderer => _imGuiRenderer;
    private readonly ImGuiRenderer _imGuiRenderer;
    private readonly Game _game;
    private readonly World _target;

    private readonly Query _allNamedEntities;
    private readonly Query _allUnnamedEntities;

    private float _targetScaling = 1;
    public Entity SelectedEntity { get; set; }

    private float _leftPanelWidth = 1000f;
    private float _splitterRatio = 0.45f;
    private const float SplitterThickness = 5f;

    private System.Numerics.Vector2 ButtonSize =>
        new System.Numerics.Vector2(_leftPanelWidth - ImGui.GetStyle().WindowPadding.X * 2f, 20 * _targetScaling);

    public ImguiEditor(Game game, World world, IEnumerable<IFieldModifer> fieldEditorTypes)
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

        foreach(var f in fieldEditorTypes)
            ComponentMeta.FieldModifierTable.Add(f.FieldType, f);
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
            if (ImGui.Button($"{entityID}, {string.Join(',', entity.ComponentTypes.Select(s => s.Type.Name))}", ButtonSize))
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

        foreach (var componentID in SelectedEntity.ComponentTypes)
        {
            ImGui.SeparatorText(componentID.Type.Name);
            var metadata = ComponentMeta.GetComponentMeta(componentID);

            foreach (var fieldData in metadata.ComponentFields)
            {
                if (ComponentMeta.GetFieldModifer(fieldData.Type) is { } intf)
                {
                    ImGui.PushID(fieldData.Name);
                    intf.Entity = SelectedEntity;
                    intf.FieldToModify = fieldData;
                    intf.UpdateUI();
                    ImGui.PopID();
                }
                else
                {
                    ImGui.Text($"<Missing Field Modifier For {fieldData.Type.Name}>");
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
