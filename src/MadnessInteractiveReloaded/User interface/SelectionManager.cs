using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MIR;

/// <summary>
/// A UI element that can be selected.
/// </summary>
public interface ISelectable
{
    public int RaycastOrder { get; set; }
    public bool ContainsPoint(Vector2 point);
    public bool Disabled { get; }
}

/// <summary>
/// Generic ordered selectable object manager
/// </summary>
public class SelectionManager<T> where T : class, ISelectable
{
    public List<T> Selectables = new();
    private int[] selectionCache = Array.Empty<int>();

    public T? SelectedObject;
    public T? HoveringObject;

    public void UpdateState(Vector2 mousePosition, bool isMouseButtonPressed)
    {
        var allUnderMouse = GetAllIndicesAt(mousePosition);
        HoveringObject = null;
        for (int i = 0; i < Selectables.Count; i++)
        {
            var item = Selectables[i];
            if (item.Disabled)
                continue;

            if (item.ContainsPoint(mousePosition))
            {
                var indexInMouseCache = allUnderMouse.IndexOf(i);
                HoveringObject = item;
                if (isMouseButtonPressed)
                {
                    if (IsSelected(item) || (SelectedObject != null && Selectables.IndexOf(SelectedObject) > i))
                    {
                        if (allUnderMouse[^1] == i)
                        {
                            //if this is the last item in the hovering stack, select the topmost selectable
                            Select(Selectables[allUnderMouse[0]]);
                            return;
                        }
                        //this is already selected so let the selection fall through to the next selectable
                        continue;
                    }
                    else
                        Select(item);
                }

                return;
            }
        }

        if (isMouseButtonPressed)
            DeselectAll();
    }

    public ReadOnlySpan<int> GetAllIndicesAt(Vector2 pos)
    {
        int ii = 0;
        for (int i = 0; i < Selectables.Count; i++)
        {
            var item = Selectables[i];
            if (item.Disabled)
                continue;
            if (item.ContainsPoint(pos))
            {
                selectionCache[ii] = i;
                ii++;
            }
        }

        return selectionCache.AsSpan()[0..ii];
    }

    /// <summary>
    /// Should call every time the list changes (something added, something removed)
    /// </summary>
    public void UpdateOrder()
    {
        Selectables.Sort(static (a, b) => b.RaycastOrder - a.RaycastOrder);

        if (selectionCache.Length < Selectables.Count)
            selectionCache = new int[Selectables.Count];
    }

    public bool IsSelected(T? s) => s != null && SelectedObject == s;
    public bool IsHovering(T? s) => s != null && HoveringObject == s;

    public void PullToFront(T obj)
    {
        obj.RaycastOrder = Selectables.Max(static o => o.RaycastOrder) + 1;
        UpdateOrder();
    }

    public void Select(T obj)
    {
        SelectedObject = obj;
        // PullToFront(obj);
    }

    public void DeselectAll()
    {
        SelectedObject = null;
    }
}
