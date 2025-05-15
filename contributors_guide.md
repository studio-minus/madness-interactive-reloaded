# Contributors guide

Thank you for considering making a contribution! Help of any size is very much appreciated. This guide should help you get started making your first contribution.

## Project goal and vision

**Madness Interactive Reloaded** (MIR) is a fan-game based on the original *Madness Interactive* (MI), a flash game from 2003. MI was a sidescrolling shoot-em-up where the player progresses through a series of levels. Levels were simple, static backgrounds during which enemies would randomly spawn with increasingly powerful weapons. MI was infamously difficult.

MIR started as a modernised remake of MI. A simple sidescrolling shooter that looked like it could be a clip from an episode. Much of the foundations were laid during this period, and the game suffered a violent scope expansion not long before it was meant to be complete. The campaign was extended, melee became a more complicated game mechanic, modding support was added, character creation became more flexible, the level editor was entirely rewritten, etc.

This increase in complexity led to foundational rewrites, but rewriting everything from scratch was too impractical. While some limitations still exist, we believe the codebase is in a good state and moving in the right direction.

As of today, the project vision is to be a platform for telling interactive Madness Combat stories. The provided campaign, Employee of the Month, serves as an example and an experiment to the community. MIR aims to be as faithful to the animated series as possible, intentionally leaving little room for our own interpretation. However, modded content is free to take any creative direction.

The goal of this project is to encourage the Madness community to create their own Madness games on top of MIR, and share this content around for others to remix and enjoy.

## Why open source?

MIR's central message encourages sharing, remixing, and creating Madness content. The project being open source helps players create mods, and allows for game forks that result in completely different games. This perfectly aligns with the project goal.

Due to the size of MIR's vision, and our team's thinly spread efforts (MIR generates no revenue), it is unfeasible to complete the game by ourselves. We simply need external help in order to continue. 

Additionally, Madness is owned by Krinkels. It's not our place to gatekeep a game that's based entirely on someone else's creation. 

## How to help

### 1. Development environment
Read the [README](README.md) to set up your development environment. Both Windows and Linux users are welcome.

### 2. Find an issue

Visit the [issue page](https://github.com/studio-minus/madness-interactive-reloaded/issues) and look for [issues labeled "help wanted"](https://github.com/studio-minus/madness-interactive-reloaded/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22help%20wanted%22). These are issues that are mostly self-contained and are good candidates for a single contributor to tackle. 

It's also possible to fix [bugs](https://github.com/studio-minus/madness-interactive-reloaded/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22bug%22), most of which are also self-contained.

It's best to involve maintainers if it's suspected that the implementation requires changes that touch core parts of the codebase.

### 3. Fork the repo

You may fork the repository and make a new branch from `dev` for your changes. Make focused changes - try to keep one PR per feature or fix.

### 4. Submit a pull request

Open a PR and write a clear description of the changes, linking to the relevant issue if possible. If your changes affect core systems, discuss it first to avoid wasted efforts. Active work is merged to the `dev` branch. 

It might take some time for a maintainer to discuss, review, and merge the PR, but they do eventually respond.

## Communication

Generally, if you need to talk to someone about something, talk to [mestiez](https://github.com/mestiez). Contact can be made through issues or PRs, [the Discord server](https://discord.gg/ezBxexVe4t), or [email](https://studiominus.nl/contact.html).

## Attribution

Contributors are listed in the game's credits! Let us know how you’d like to be acknowledged, if at all.

## Style guide

- We follow the style enforced by the `.editorconfig`. 
- No underscores for private members.
- Public members should ideally have an XML summary.