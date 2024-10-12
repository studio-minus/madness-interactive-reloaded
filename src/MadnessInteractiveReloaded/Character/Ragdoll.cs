using System.Collections.Generic;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Static class for creating ragdolls.
/// </summary>
public static class Ragdoll
{
    //TODO wat the fuck man
    public static IEnumerable<Component> BuildDefaultRagdoll(Scene scene, CharacterComponent character)
    {
        var charPos = character.Positioning;
        var scale = charPos.Scale;

        var head = charPos.Head.Entity;
        var body = charPos.Body.Entity;

        var headTransform = scene.GetComponentFrom<TransformComponent>(head);
        var bodyTransform = scene.GetComponentFrom<TransformComponent>(body);
        var hand1Transform = scene.GetComponentFrom<TransformComponent>(charPos.Hands.First.Entity);
        var hand2Transform = scene.GetComponentFrom<TransformComponent>(charPos.Hands.Second.Entity);
        var flipped = charPos.IsFlipped;
        var fl = flipped ? -1 : 1;

        var impactStrength = 1;// Noise.GetValue(23.16f, -5913.34f, scene.Game.State.Time.SecondsSinceLoad) * 0.5f + 0.5f;

        //Create node on an existing entity
        VerletNodeComponent createNodeFor(Entity speedOrigin, Vector2 localPosition, TransformComponent transform, Entity entity, float radius, float mass = 1)
        {
            var node = new VerletNodeComponent(Vector2.Transform(localPosition, transform.LocalToWorldMatrix), 17 * scale)
            {
                Radius = radius,
                Mass = mass,
                Friction = 1,
            };

            //if (node.Position.Y < (Level.CurrentLevel?.FloorLevel ?? Level.DefaultFloorLevel))
            //    node.FloorOffset = node.Position.Y - (Level.CurrentLevel?.FloorLevel ?? Level.DefaultFloorLevel) - 10;
            //else
            // TODO mooier maken
            {
                if (scene.TryGetComponentFrom<ImpactOffsetComponent>(speedOrigin, out var impact))
                {
                    node.Acceleration += impact.TranslationOffset * (1.9f + (impactStrength));
                    if (node.Acceleration.Length() > 25)
                        node.Acceleration = Vector2.Normalize(node.Acceleration) * 25;
                }

                if (scene.TryGetComponentFrom<MeasuredVelocityComponent>(speedOrigin, out var measured))
                    node.Acceleration += measured.DeltaTranslation * scene.GetSystem<MeasuredVelocitySystem>().MeasureRate.Interval * 0.2f;
            }

            return scene.AttachComponent(entity, node);
        }

        //Create node with a new entity
        VerletNodeComponent createNode(Entity speedOrigin, Vector2 localPosition, TransformComponent transform, float radius, float mass = 1)
            => createNodeFor(speedOrigin, localPosition, transform, scene.CreateEntity(), radius, mass);

        var headNode1 = createNode(head, new Vector2(-.25f * fl, -0.35f), headTransform, 17 * scale);
        yield return headNode1;

        var headNode2 = createNode(head, new Vector2(-.25f * fl, 0.3f), headTransform, 17 * scale);
        yield return headNode2;

        var noseJoint = createNode(head, new Vector2(.4f * fl, 0), headTransform, 17 * scale);
        yield return noseJoint;

        yield return scene.AttachComponent(head, new VerletTransformComponent(charPos.Head.Entity, headTransform, headNode2, headNode1));

        var e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(headNode1, headNode2)); //connect all head nodes to form a triangle
        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(headNode2, noseJoint));
        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(noseJoint, headNode1));

        var neckNode = createNode(body, new Vector2(0, .3f), bodyTransform, 60 * scale, 2);
        yield return neckNode;

        var bodyNode1 = createNode(body, new Vector2(-.25f * fl, 0), bodyTransform, 17 * scale);
        yield return bodyNode1;

        var bodyNode2 = createNode(body, new Vector2(.3f * fl, 0), bodyTransform, 17 * scale);
        yield return bodyNode2;

        var bottomNode = createNode(body, new Vector2(0, -0.32f), bodyTransform, 70 * scale, 5);
        yield return bottomNode;

        yield return scene.AttachComponent(body, new VerletTransformComponent(body, bodyTransform, neckNode, bottomNode)); //link bodytransform to body nodes

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(neckNode, headNode2));  //connect head to neck

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(neckNode, noseJoint));

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(headNode1, neckNode, VerletLinkMode.MaxDistanceOnly) { /*TargetDistance = 50*/ }); //connect neck to head node with special joint

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(bodyNode2, noseJoint, VerletLinkMode.MinMaxDistance) { MinMaxDistance = new Vector2(107, 260) * scale }); //connect nose joint to body 1

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(bottomNode, noseJoint, VerletLinkMode.MinMaxDistance) { MinMaxDistance = new Vector2(300, 400) * scale }); //connect nose joint to body 2

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(bottomNode, noseJoint, VerletLinkMode.MinDistanceOnly) { TargetDistance = 230 * scale }); //nog een link to prevent opvouwen

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(bodyNode1, headNode2, VerletLinkMode.MinDistanceOnly) { TargetDistance = 190 * scale }); //NOG een link to prevent opvouwen

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(bodyNode1, bodyNode2)); //connect two sides

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(bodyNode1, neckNode));  //connect side nodes to neck

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(bodyNode2, neckNode));

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(bodyNode1, bottomNode)); //connect bottom to sides

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(neckNode, bottomNode)); //connect bottom to neck

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(bodyNode2, bottomNode));

        var hand1 = charPos.Hands.First.Entity;
        var hand2 = charPos.Hands.Second.Entity;

        var handNode1 = createNode(hand1, Vector2.Zero, hand1Transform, 25 * scale);
        yield return handNode1;

        var handNode2 = createNode(hand2, Vector2.Zero, hand2Transform, 25 * scale);
        yield return handNode2;

        yield return scene.AttachComponent(hand1, new VerletTransformComponent(
            hand1,
            scene.GetComponentFrom<TransformComponent>(hand1),
            neckNode,
            handNode1)
        { LocalRotationalOffset = -90 }); //link hands to hands

        yield return scene.AttachComponent(hand2, new VerletTransformComponent(
            hand2,
            scene.GetComponentFrom<TransformComponent>(hand2),
            neckNode,
            handNode2)
        { LocalRotationalOffset = -90 });

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(handNode1, neckNode, VerletLinkMode.MaxDistanceOnly) { TargetDistance = 280 * scale });  //connect neck to hands

        e = scene.CreateEntity();
        yield return scene.AttachComponent(e, new VerletLinkComponent(handNode2, neckNode, VerletLinkMode.MaxDistanceOnly) { TargetDistance = 280 * scale });
    }
}