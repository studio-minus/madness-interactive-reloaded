using MIR.Controls;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class AssetBrowserControl
{
    private string currentFilter = string.Empty;
    public string External = "base";
    public string Path = string.Empty;

    private Entry[]? folderCache;

    private struct Entry
    {
        public string Name;
        public AssetId? Asset;
        public string? FolderDestination;
    }

    [MemberNotNull(nameof(folderCache))]
    public void RefreshCache()
    {
        if (!Assets.TryGetPackage(External, out var package))
        {
            folderCache = [];
            return;
        }

        folderCache =
        [
            ..package.GetFoldersIn(Path).Select(i => new Entry{ 
                Name = $"{i}/", 
                FolderDestination = Path.TrimEnd('/') + '/' + i }),
            ..package.EnumerateFolder(Path).Select(i => {
                var m = package.GetMetadata(i);
                return new Entry
                {
                    Name = global::System.IO.Path.GetFileName(m.Path),
                    Asset = i
                };
            }),
        ];
    }

    public bool Start(ref GlobalAssetId asset, Func<AssetMetadata, bool> filterFunction, int identity = 0, [CallerLineNumber] int site = 0)
    {
        External ??= "base";
        currentFilter ??= string.Empty;
        bool r = false;

        if (folderCache == null)
            RefreshCache();

        Ui.StartGroup(identity: identity + site);
        {
            // top bar
            Ui.Layout.Height(40).FitWidth(false).Overflow(true, false);
            Ui.Theme.ForegroundColor(Colors.White.Brightness(0.15f)).Once();
            Ui.StartGroup(true);
            {
                Ui.Layout.Width(40).Square().StickLeft(false);
                if (Ui.ImageButton(Textures.UserInterface.MenuBack.Value, ImageContainmentMode.Stretch))
                {
                    if (Path.Contains('/'))
                    {
                        Path = Path[..Path.LastIndexOf('/')];
                        folderCache = null;
                    }
                }

                Ui.Layout.FitContainer(0.2f, 1).MinWidth(100).CenterVertical().StickLeft().Move(40, 0);
                if (Ui.StringInputBox(ref External, new("Package Id")))
                {
                    folderCache = null;
                    External = External.ToLowerInvariant();
                }

                Ui.Layout.FitContainer().Scale(-Onion.Tree.GetLastInstance().Rects.ComputedGlobal.Width - Onion.Theme.Base.Padding - 40, 0).CenterVertical().StickRight();
                if (Ui.StringInputBox(ref Path, new("/")))
                    folderCache = null;
            }
            Ui.End();

            if (folderCache != null && Assets.TryGetPackage(External, out var package))
            {
                // resource buttons
                Ui.Layout.FitContainer().Center().Scale(0, -50).StickBottom();
                Ui.StartScrollView();
                {
                    int i = 0;
                    foreach (var entry in folderCache)
                    {
                        if (entry.Asset.HasValue)
                        {
                            if (asset.Internal == entry.Asset)
                            {
                                Ui.Theme.Text(Colors.Red).Once();
                                Ui.Decorate(new CrosshairDecorator());
                            }
                            else if (!filterFunction(package.GetMetadata(entry.Asset.Value)))
                                continue;
                        }

                        Ui.Layout.FitWidth().Height(30).CenterHorizontal().Move(0, i * 30);
                        Ui.Theme.Font(Fonts.CascadiaMono).OutlineWidth(0).FontSize(14).Once();
                        if (LeftAlignedButton.Start(entry.Name, identity: i++))
                        {
                            if (entry.Asset.HasValue)
                            {
                                asset = new(package.Metadata.Id, entry.Asset.Value);
                                r = true;
                            }
                            else if (entry.FolderDestination != null)
                            {
                                folderCache = null;
                                Path = entry.FolderDestination;
                            }
                        }
                    }
                }
                Ui.End();
            }
        }
        Ui.End();
        return r;
    }
}
