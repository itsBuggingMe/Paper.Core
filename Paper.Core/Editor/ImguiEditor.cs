using Frent;
using Microsoft.Xna.Framework;
using ImGuiNET;
using System.Diagnostics;
using System.Security.AccessControl;
using Frent.Systems;
using System;
using Frent.Marshalling;
using Frent.Core;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace Paper.Core.Editor;
public class ImguiEditor
{
    private readonly ImGuiRenderer _imGuiRenderer;
    private readonly Game _game;
    private readonly World _target;

    private readonly Query _allNamedEntities;
    private readonly Query _allUnnamedEntities;

    private ComponentEditorInfo? _componentEditor;

    private float _targetScaling = 1;

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
        game.Window.ClientSizeChanged += (s, e) =>
        {
            UpdateScaling();
        };
    }

    public void Draw(GameTime gameTime)
    {
        _imGuiRenderer.BeforeLayout(gameTime);

        EntitiesWindow();
        ComponentEditorWindow();

        _imGuiRenderer.AfterLayout();
    }

    private void ComponentEditorWindow()
    {
        if (_componentEditor is not { } info)
            return;

        const string ComponentEditor = "Component Editor";

        if (ImGui.Begin(ComponentEditor))
        {
            ImGui.SeparatorText(info.Component.Type.Name);

            ComponentMeta metadata = ComponentMeta.ComponentMetaTable[info.Component];
            foreach (var fieldData in metadata.ComponentFields)
            {
                if (ComponentMeta.FieldModifierTable.TryGetValue(fieldData.Type, out var intf))
                {
                    ImGui.PushID(fieldData.Name);
                    intf.Entity = info.Target;
                    intf.FieldToModify = fieldData;
                    intf.UpdateUI();
                    ImGui.PopID();
                }
            }
        }
        ImGui.End();
    }

    private void EntitiesWindow()
    {
        if (!ImGui.Begin("Entities"))
        {
            ImGui.End();
            return;
        }

        foreach(var (entity, name) in _allNamedEntities.EnumerateWithEntities<EditorName>())
        {
            if(ImGui.CollapsingHeader(name.Value.Name))
            {
                DisplayEntityInfo(entity);
            }
        }


        const string Prefix = "ID: ";
        Span<char> nameBuffer = stackalloc char[16];
        Prefix.CopyTo(nameBuffer);

        foreach (var entity in _allUnnamedEntities.EnumerateWithEntities())
        {
            int entityID = EntityMarshal.EntityID(entity);
            entityID.TryFormat(nameBuffer[Prefix.Length..], out int charsWritten);

            if (ImGui.CollapsingHeader(nameBuffer[..(Prefix.Length + charsWritten)]))
            {
                DisplayEntityInfo(entity);
            }
        }

        ImGui.End();
    }

    private void DisplayEntityInfo(Entity entity)
    {
        foreach (var component in entity.ComponentTypes)
        {
            if (ImGui.Button(component.Type.Name))
            {
                _componentEditor = new ComponentEditorInfo(entity, component);
            }
        }

        if (entity.TagTypes.Length == 0)
            return;

        ImGui.SeparatorText("Tags");

        foreach (var item in entity.TagTypes)
            ImGui.Text(item.Type.Name);
    }

    private void UpdateScaling()
    {
        float scale = _game.Window.ClientBounds.Size.ToVector2().Length() / new Vector2(1920, 1080).Length() * 2;

        var styles = ImGui.GetStyle();

        // prev scaling * mul factor = new scaling
        // mul factor = new scaling / prev scaling
        styles.ScaleAllSizes(scale / _targetScaling);

        if(_targetScaling != scale)
        {
            //TODO: better scaling?
            ImGui.GetIO().FontGlobalScale = scale;
            _targetScaling = scale;
        }
    }

    private record struct ComponentEditorInfo(Entity Target, ComponentID Component);
}
