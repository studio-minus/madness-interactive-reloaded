using Walgelijk;
using Walgelijk.Onion;

namespace MIR;

public class AssetBrowserDialogSystem : Walgelijk.System
{
    public override void Update() //🎅
    {
        int i = 0;
        foreach (var item in Scene.GetAllComponentsOfType<AssetBrowserDialogComponent>())
        {
            bool open = true;
            Ui.Layout.Size(400, 500).Center().Resizable();
            Ui.Theme.OutlineColour(Colors.Red.WithAlpha(0.5f)).OutlineWidth(1).Once();
            Ui.StartDragWindow("Pick asset", ref open, identity: i);
            {
                Ui.Layout.FitContainer().Center();
                if (item.AssetBrowserControl.Start(ref item.Asset, item.FilterFunction))
                {
                    open = false;
                    item.SetAsset(item.Asset);
                }
            }
            Ui.End();

            if (!open)
                Scene.RemoveEntity(item.Entity);
        }
    }
}
