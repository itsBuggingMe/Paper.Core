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

    private float _targetScaling = 1;

    private Entity _selectedEntity;

    private System.Numerics.Vector2 ButtonSize => new System.Numerics.Vector2(SizePanelWidth, 20 * _targetScaling);
    private float SizePanelWidth = 512;

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
        EntityWindow();

        _imGuiRenderer.AfterLayout();
    }

    private void EntityWindow()
    {
        if (_selectedEntity.IsNull)
            return;

        
        if (ImGui.Begin($"Entity: {EntityMarshal.EntityID(_selectedEntity)}"))
        {
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
            if(ImGui.Button(name.Value.Name, ButtonSize))
            {
                _selectedEntity = entity;
            }
        }


        //const string Prefix = "ID: ";
        //Span<char> nameBuffer = stackalloc char[128];
        //Prefix.CopyTo(nameBuffer);

        foreach (var entity in _allUnnamedEntities.EnumerateWithEntities())
        {
            int entityID = EntityMarshal.EntityID(entity);
            //entityID.TryFormat(nameBuffer[Prefix.Length..], out int charsWritten);
            //nameBuffer[..(Prefix.Length + charsWritten)]
            if (ImGui.Button($"{entityID}, {string.Join(',', entity.ComponentTypes.Select(s => s.Type.Name))}", ButtonSize))
            {
                _selectedEntity = entity;
            }
        }

        ImGui.End();
    }

    private void UpdateScaling()
    {
        float scale = _game.Window.ClientBounds.Size.ToVector2().Length() / new Vector2(1920, 1080).Length();

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
}
