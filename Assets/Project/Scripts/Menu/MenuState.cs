// Assets/Project/Scripts/Menu/MenuState.cs
using UnityEngine;

namespace ResidentialHVAC.Menu
{
    /// <summary>
    /// Defines the different menu states/screens
    /// </summary>
    public enum MenuState
    {
        MainMenu,           // Initial screen with Start/Continue/Settings
        HubSelection,       // Shows The Hub access
        TrainingMode,       // Training mode options
        LoadCalculation,    // Load calculation rooms
        MaterialSelection,  // Material & equipment selection
        SystemInstallation, // System installation options
        Settings,           // Settings panel
        Paused              // In-game pause menu
    }
}