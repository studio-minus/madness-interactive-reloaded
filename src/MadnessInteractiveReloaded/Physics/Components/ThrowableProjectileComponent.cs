using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// For things that can be thrown and hurt.
/// </summary>
[RequiresComponents(typeof(VelocityComponent))]
public class ThrowableProjectileComponent : Component
{
    /// <summary>
    /// How much damage this does to things when it hits them.
    /// </summary>
    public float Damage;

    /// <summary>
    /// Moves slower.
    /// </summary>
    public bool IsHeavy;

    /// <summary>
    /// The <see cref="CharacterComponent"/> responsible for having thrown this
    /// </summary>
    public ComponentRef<CharacterComponent>? Thrower;

    /// <summary>
    /// If this component should be removed when the 
    /// object settles after being thrown.
    /// </summary>
    public bool RemoveComponentOnDepletion = true;

    public uint LayerMask = uint.MaxValue;

    public IList<Rect> SharpHitboxes = Array.Empty<Rect>();

    /// <summary>
    /// The entity we use as reference for our transform data.
    /// </summary>
    public ComponentRef<TransformComponent> TransformEntity;

    public readonly WorldSharpBox[] WorldSharpBoxCache;

    public ThrowableProjectileComponent(float damage, bool isHeavy, bool removeComponentOnDepletion, uint layerMask, IList<Rect> sharpHitboxes, Entity transformEntity, ComponentRef<CharacterComponent>? thrower)
    {
        Damage = damage;
        IsHeavy = isHeavy;
        RemoveComponentOnDepletion = removeComponentOnDepletion;
        LayerMask = layerMask;
        SharpHitboxes = sharpHitboxes;
        TransformEntity = new(transformEntity);

        WorldSharpBoxCache = new WorldSharpBox[sharpHitboxes.Count];
        Thrower = thrower;
    }

    public void UpdateWorldSharpBoxes(Scene scene)
    {
        var transform = TransformEntity.Get(scene);
        var sR = new Vector2(1 / transform.Scale.X, 1 / transform.Scale.Y);
        var localToWorld = transform.LocalToWorldMatrix;

        for (int i = 0; i < SharpHitboxes.Count; i++)
        {
            var rect = SharpHitboxes[i].Expand(10);

            var s = rect.GetSize();
            var hS = transform.Scale * 0.5f;
            var p = new Vector2(rect.TopLeft.X - hS.X, (rect.TopLeft.Y + hS.Y) - s.Y);
            var r = new Rect(p.X, p.Y - s.Y, p.X + s.X, p.Y);

            var topLeft = r.TopLeft;
            var topRight = r.TopRight;
            var bottomLeft = r.BottomLeft;
            var bottomRight = r.BottomRight;   

            topLeft = Vector2.Transform(topLeft * sR, localToWorld);
            topRight = Vector2.Transform(topRight * sR, localToWorld);
            bottomLeft = Vector2.Transform(bottomLeft * sR, localToWorld);
            bottomRight = Vector2.Transform(bottomRight * sR, localToWorld);

            WorldSharpBoxCache[i] = (topLeft, topRight, bottomLeft, bottomRight);
        }
    }
}
