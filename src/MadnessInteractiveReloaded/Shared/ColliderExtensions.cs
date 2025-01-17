using System.Numerics;
using Walgelijk.Physics;

namespace MIR;

public static class ColliderExtensions
{
    public static bool OverlapsQuad(this ICollider coll, Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 contact)
    {
        if (getIntersect(coll, a, b, out contact))
            return true;      

        if (getIntersect(coll, b, c, out contact))
            return true;   

        if (getIntersect(coll, c, d, out contact))
            return true;   

        if (getIntersect(coll, d, a, out contact))
            return true;

        static bool getIntersect(ICollider coll, Vector2 from, Vector2 to, out Vector2 result)
        {
            var diff = to - from;
            var length = diff.Length();
            var dir = diff / length;

            var ray = new Geometry.Ray(from, dir);
            foreach (var intersect in coll.GetLineIntersections(ray))
            {
                if (Vector2.Distance(intersect, from) <= length)
                {
                    result = intersect;
                    return true;
                }
            }

            result = default;
            return false;
        }

        contact = default;
        return false;
    }
}