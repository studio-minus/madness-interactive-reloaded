# The Scrapeface bug;
This fork is aimed to fix the very infamous https://github.com/studio-minus/madness-interactive-reloaded/issues/38.

## The cause of the bug;
.NET 8.0.8 causes Vector2.Normalize() (and some other methods) on older CPU's that do not support SSE41 or SSE42 to not.
This may be fixed in the future with .NET 9, but for now here's this fork

# What the fork does;
The fork replaces all instances of Vector2.Normalize and other faulty (yet to be discovered) methods with custom Vector2.Normalize methods at minimum performance losses.

## How it accomplishes that;

The fork includes a Vector2 (MadnessVector2) class utility that can probably run on your kitchen microwave. It replaces 2 methods that would only work on newer CPUs with offbrand one. The methods in the MadnessVector2 class do not take advantage of Hardware Acceleration to ensure it doesn't cause issues.

## What it currently fixes;
This fork fixes all things related to positions, It fixes melee combat, head turning and rotating, special abilities,

## What it does NOT fix;
As of now it does not fix raycasting (might be related to Walgelijk), and ragdolls.
