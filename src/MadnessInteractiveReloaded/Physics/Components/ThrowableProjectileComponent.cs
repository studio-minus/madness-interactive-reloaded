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
    public Entity TransformEntity;

    public readonly WorldSharpBox[] WorldSharpBoxCache;

    public ThrowableProjectileComponent(float damage, bool isHeavy, bool removeComponentOnDepletion, uint layerMask, IList<Rect> sharpHitboxes, Entity transformEntity, ComponentRef<CharacterComponent>? thrower)
    {
        Damage = damage;
        IsHeavy = isHeavy;
        RemoveComponentOnDepletion = removeComponentOnDepletion;
        LayerMask = layerMask;
        SharpHitboxes = sharpHitboxes;
        TransformEntity = transformEntity;

        WorldSharpBoxCache = new WorldSharpBox[sharpHitboxes.Count];
        Thrower = thrower;
    }

    public void UpdateWorldSharpBoxes(Scene scene)
    {
        var myTransform = scene.GetComponentFrom<TransformComponent>(TransformEntity);
        var s = myTransform.Scale;
        var offset = new Vector2(0.5f);
        //TODO Maak sneller AUB. Dit kan zo toch niet...
        for (int i = 0; i < SharpHitboxes.Count; i++)
        {
            var sharpBox = SharpHitboxes[i];
            var topLeft = Vector2.Transform(new Vector2(sharpBox.TopLeft.X / s.X, 1 - sharpBox.TopLeft.Y / s.Y) - offset, myTransform.LocalToWorldMatrix);
            var topRight = Vector2.Transform(new Vector2(sharpBox.TopRight.X / s.X, 1 - sharpBox.TopRight.Y / s.Y) - offset, myTransform.LocalToWorldMatrix);
            var bottomLeft = Vector2.Transform(new Vector2(sharpBox.BottomLeft.X / s.X, 1 - sharpBox.BottomLeft.Y / s.Y) - offset, myTransform.LocalToWorldMatrix);
            var bottomRight = Vector2.Transform(new Vector2(sharpBox.BottomRight.X / s.X, 1 - sharpBox.BottomRight.Y / s.Y) - offset, myTransform.LocalToWorldMatrix);

            WorldSharpBoxCache[i] = (topLeft, topRight, bottomLeft, bottomRight);
        }
    }
}
