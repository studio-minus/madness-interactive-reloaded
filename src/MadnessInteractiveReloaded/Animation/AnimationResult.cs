namespace MIR;

using System.Numerics;

/// <summary>
/// Used for applying animated transform data to animated things.
/// </summary>
public struct AnimationResult
{
    /// <summary>
    /// The position of the body.
    /// </summary>
    public Vector2 BodyPosition;

    /// <summary>
    /// The angle of the body.
    /// </summary>
    public float BodyRotation;

    /// <summary>
    /// The scale of the body.
    /// </summary>
    public Vector2 BodyScale;

    /// <summary>
    /// The position of the head.
    /// </summary>
    public Vector2 HeadPosition;

    /// <summary>
    /// The angle of the head.
    /// </summary>
    public float HeadRotation;

    /// <summary>
    /// The scale of the head.
    /// </summary>
    public Vector2 HeadScale;

    /// <summary>
    /// The position of hand1
    /// </summary>
    public Vector2 Hand1Position;
    
    /// <summary>
    /// The angle of hand1
    /// </summary>
    public float Hand1Rotation;

    /// <summary>
    /// The scale of hand1.
    /// </summary>
    public Vector2 Hand1Scale;

    /// <summary>
    /// The position of hand2
    /// </summary>
    public Vector2 Hand2Position;

    /// <summary>
    /// The angle of hand2.
    /// </summary>
    public float Hand2Rotation;

    /// <summary>
    /// The scale of hand2.
    /// </summary>
    public Vector2 Hand2Scale;

    /// <summary>
    /// What pose is hand1 in?
    /// </summary>
    public HandLook? Hand1Look;

    /// <summary>
    /// What pose is hand2 in?
    /// </summary>
    public HandLook? Hand2Look;
}
