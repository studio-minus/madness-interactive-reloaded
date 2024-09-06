using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor.Objects;

/// <summary>
/// Non-playable-character.
/// For spawning a specific character in a level.
/// </summary>
public class NPC : LevelObject
{
    /// <summary>
    /// What NPC to spawn.
    /// </summary>
    public NpcInstructions Instructions;

    public string? StartAnimation;

    private static readonly List<string> suggestions = [];

    public NPC(LevelEditorComponent editor, NpcInstructions npc) : base(editor)
    {
        Instructions = npc;
    }

    public override object Clone()
    {
        var cloned = new NpcInstructions(Instructions.Look, Instructions.Stats, Instructions.Faction, Instructions.Weapon, Instructions.Flipped, Instructions.Scale);
        cloned.BottomCenter = Instructions.BottomCenter;
        return new NPC(Editor, cloned);
    }

    public override bool ContainsPoint(Vector2 worldPoint)
    {
        return Vector2.Distance(Instructions.BottomCenter, worldPoint) < 32;
    }

    public override void ProcessInEditor(Scene scene, InputState input)
    {
        var instr = Instructions;
        ProcessDraggable(input);
        var isSelected = Editor.SelectionManager.SelectedObject == this;

        Draw.Order = RenderOrders.CharacterUpper;

        Draw.Texture = Textures.UserInterface.EditorNpcPlaceholder.Value;
        Draw.Colour = isSelected || Editor.SelectionManager.HoveringObject == this ? Colors.White : Colors.Red;
        var s = Draw.Texture.Size * MadnessConstants.BackgroundSizeRatio;
        if (Instructions.Flipped)
            s.X *= -1;

        s *= instr.Scale;

        Draw.Quad(instr.BottomCenter + new Vector2(-s.X / 2, s.Y), s);

        Draw.ResetTexture();
        Draw.Circle(instr.BottomCenter, new Vector2(32));

        Draw.FontSize = 18;
        Draw.Font = Fonts.Inter;
        Draw.Text(instr.Look, instr.BottomCenter + new Vector2(0, s.Y + 18 * 3), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
        Draw.Text(instr.Stats, instr.BottomCenter + new Vector2(0, s.Y + 18 * 2), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
        if (instr.Weapon != null)
            Draw.Text(instr.Weapon, instr.BottomCenter + new Vector2(0, s.Y + 18 * 1), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
    }

    public override void SpawnInGameScene(Scene scene)
    {
        WeaponInstructions? weapon = null;

        if (Instructions.Weapon != null && !Registries.Weapons.TryGet(Instructions.Weapon, out weapon))
            throw new Exception("Invalid weapon in level NPC list");
        if (!Registries.Looks.TryGet(Instructions.Look, out var look) || look == null)
            throw new Exception("Invalid look in level NPC list");
        if (!Registries.Stats.TryGet(Instructions.Stats, out var stats) || stats == null)
            throw new Exception("Invalid stats in level NPC list");  
        if (!Registries.Factions.TryGet(Instructions.Faction ?? "aahw", out var faction) || faction == null)
            throw new Exception("Invalid faction in level NPC list");

        var character = Prefabs.CreateEnemyWithWeapon(scene, Instructions.BottomCenter, weapon, stats, look, faction, true, Instructions.ScaleOverride ? Instructions.Scale : null);
        character.Name = Instructions.Name;

        if (Instructions.IsProgressionRequirement)
            scene.AttachComponent(character.Entity, new NeedsToDieComponent());

        if (StartAnimation != null && scene.TryGetComponentFrom<CharacterComponent>(character.Entity, out var cc) && Registries.Animations.TryGet(StartAnimation, out var s))
        {
            MadnessUtils.Delay(0, () =>
            {
                cc.PlayAnimation(s);
            });
        }
    }

    public override void ProcessPropertyUi()
    {
        int selectedIndex;

        Ui.Spacer(5);
        Ui.Label("Name");
        selectedIndex = Array.IndexOf(Editor.Looks, Instructions.Look);
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.StringInputBox(ref Instructions.Name, default))
            Editor.Dirty = true;
        
        Ui.Spacer(5);
        Ui.Label("Look");
        selectedIndex = Array.IndexOf(Editor.Looks, Instructions.Look);
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.Dropdown(Editor.Looks, ref selectedIndex))
        {
            Instructions.Look = Editor.Looks[selectedIndex];
            Editor.Dirty = true;
        }

        Ui.Spacer(5);
        Ui.Label("Stats");
        selectedIndex = Array.IndexOf(Editor.Stats, Instructions.Stats);
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.Dropdown(Editor.Stats, ref selectedIndex))
        {
            Instructions.Stats = Editor.Stats[selectedIndex];
            Editor.Dirty = true;
        }

        Ui.Spacer(5);
        Ui.Label("Faction");
        selectedIndex = Array.IndexOf(Editor.Factions, Instructions.Faction);
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.Dropdown(Editor.Factions, ref selectedIndex))
        {
            Instructions.Faction = Editor.Factions[selectedIndex];
            Editor.Dirty = true;
        }

        Ui.Spacer(5);
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.Checkbox(ref Instructions.IsProgressionRequirement, "Death is progression requirement"))
            Editor.Dirty = true;

        Ui.Spacer(5);
        bool hasWeapon = Instructions.Weapon != null;
        Ui.Layout.Height(32).FitWidth(false);
        if (Ui.Checkbox(ref hasWeapon, "Has weapon?"))
        {
            Instructions.Weapon = !hasWeapon ? Registries.Weapons.GetRandomKey() : null;
            Editor.Dirty = true;
        }

        if (Instructions.Weapon != null)
        {
            if (!Registries.Weapons.Has(Instructions.Weapon))
                Instructions.Weapon = Registries.Weapons.GetRandomKey();

            Ui.Label("Weapon");
            selectedIndex = Array.IndexOf(Editor.Weapons, Instructions.Weapon);
            Ui.Layout.Height(32).FitWidth(false);
            if (Ui.Dropdown(Editor.Weapons, ref selectedIndex))
            {
                Instructions.Weapon = Editor.Weapons[selectedIndex];
                Editor.Dirty = true;
            }
        }

        Ui.Spacer(5);
        Ui.Layout.FitWidth(false).Height(32);
        if (Ui.Checkbox(ref Instructions.Flipped, "Flipped"))
            Editor.Dirty = true;

        Ui.Spacer(5);
        Ui.Layout.FitWidth(false).Height(32);
        Ui.Checkbox(ref Instructions.ScaleOverride, "Scale override");
        if (Instructions.ScaleOverride)
        {
            Ui.Layout.FitWidth(false).Height(32);
            Ui.FloatInputBox(ref Instructions.Scale, (1, 3));
        }

        Ui.Spacer(5);
        bool a = StartAnimation != null;
        Ui.Layout.FitWidth().Height(32);
        if (Ui.Checkbox(ref a, "Spawn with animation"))
        {
            if (StartAnimation != null)
                StartAnimation = null;
            else
                StartAnimation = Registries.Animations.GetRandomKey();
        }

        if (StartAnimation != null)
        {
            Ui.Spacer(16);
            Ui.Label("Animation key");
            Ui.Layout.FitWidth(false).Height(32);
            if (Ui.StringInputBox(ref StartAnimation, TextBoxOptions.TextInput))
                FindSuggestions();

            if (!Registries.Animations.Has(StartAnimation))
            {
                Ui.Layout.FitWidth(false).Height(128).VerticalLayout();
                Ui.StartScrollView();
                {
                    int i = 0;
                    foreach (var item in suggestions)
                    {
                        Ui.Layout.FitWidth(false).Height(32);
                        if (Ui.Button(item, i++))
                            StartAnimation = item;
                    }
                }
                Ui.End();
            }
        }
    }

    private void FindSuggestions()
    {
        suggestions.Clear();
        suggestions.AddRange(Registries.Animations.GetAllKeys().OrderByDescending(a => GetSimilarity(a, StartAnimation)).Take(8));
    }

    private static float GetSimilarity(string str1, string str2)
    {
        str1 = str1.ToLower();
        str2 = str2.ToLower();
        int len1 = str1.Length;
        int len2 = str2.Length;
        if (len1 == 0 || len2 == 0) return 0;
        int matches = 0;
        for (int i = 0; i < len1; i++)
            if (str2.Contains(str1[i])) matches++;
        var ratio = (float)matches / str1.Length;
        if (len1 == len2 && str1 == str2) ratio = 1.0f;
        return ratio;
    }

    public override Vector2 GetPosition() => Instructions.BottomCenter;
    public override void SetPosition(Vector2 pos) => Instructions.BottomCenter = pos;
}
