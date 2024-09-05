# Madness Interactive Reloaded

Game based on "Madness Interactive"

## Building the game
<!-- Keep this header at this line since the gettingstarted.md file expects it to be there. If you move it, move it there too. -->
- Assert you have these requirements
    - Windows 10 or 11 or Linux
    - git
    - [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
    - NuGet
- Clone this repository
- Open a terminal inside the repository directory
- Initialise the submodules
    ```shell
    git submodule init
    git submodule update --remote
    ```
 - **If you DON'T have Visual Studio**
    - Navigate to the `src\MadnessInteractiveReloaded` directory and build:
        ```shell
        cd src\MadnessInteractiveReloaded 
        dotnet build
        ```
    - Debug the game by typing `dotnet run` into the terminal        

- **If you DO have Visual Studio**
    - Navigate to `src\MadnessInteractiveReloaded` directory and open the `MIR.sln` solution using VS2022
    - Press F5 (or press the ▶ button)
    
