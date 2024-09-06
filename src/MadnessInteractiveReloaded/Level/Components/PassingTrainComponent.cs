using System;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public class PassingTrainComponent : Component, IDisposable
{
    public float Speed = 0.5f;
    public float Time = 0;

    public AssetRef<Texture> LocomotiveTexture = Assets.Load<Texture>("textures/transportation_locomotive.qoi");
    public AssetRef<Texture> CarTexture = Assets.Load<Texture>("textures/transportation_wagon.qoi");

    public AssetRef<StreamAudioData> TrainLoopSound = Assets.Load<StreamAudioData>("sounds/train_loop.ogg");
    public AssetRef<FixedAudioData> TrainWhistleSound = Assets.Load<FixedAudioData>("sounds/train_whistle.wav");

    public void Dispose()
    {
    }
}
