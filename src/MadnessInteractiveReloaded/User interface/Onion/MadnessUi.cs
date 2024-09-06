using MIR.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// MIR specific UI stuffs.
/// </summary>
public static class MadnessUi
{
    public static void AssetPicker(GlobalAssetId asset, Action<GlobalAssetId> action, Func<AssetMetadata, bool> filterFunction,
        int identity = 0, [CallerLineNumber] int site = 0, in string? title = null, in string? startPath = null)
    {
        var id = IdGen.Create(identity, site, 9275208);

        Ui.StartGroup(false, identity + site);
        {
            var h = Onion.Tree.CurrentNode!.GetInstance().Rects.ComputedGlobal.Height;

            Ui.Layout.Size(h, h);
            Ui.Image(Assets.Load<Texture>("textures/ui/icon/folder.png").Value, ImageContainmentMode.Stretch);

            var path = "None";
            if (Assets.TryFindFirst(asset.Internal, out var global) && Assets.TryGetPackage(global.External, out var p) && p.HasAsset(global.Internal))
                path = p.Metadata.Name + ':' + p.GetAssetPath(global.Internal);

            //var path = assetPickerPathState[id];
            Ui.Decorators.Tooltip(path);
            Ui.Layout.FitContainer(1, 1, false).Scale(-h, 0).Move(h, 0);
            Ui.Theme.Font(Fonts.CascadiaMono).FontSize(12).OutlineWidth(0).Once();
            if (LeftAlignedButton.Start(path))
            {
                var s = Game.Main.Scene;
                s.AttachComponent( s.CreateEntity(),
                   new AssetBrowserDialogComponent(
                       action,
                       filterFunction,
                       asset
                   ));
            }
        }
        Ui.End();
    }
}
