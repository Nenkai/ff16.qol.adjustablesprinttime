using ff16.qol.adjustablesprinttime.Configuration;
using ff16.qol.adjustablesprinttime.Template;

using Reloaded.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.Sigscan.Definitions.Structs;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

using System.Diagnostics;
using Reloaded.Memory.Interfaces;

namespace ff16.qol.adjustablesprinttime;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private static IStartupScanner? _startupScanner = null!;

    private nint _imageBase;
    private nint _timeAddr;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;


#if DEBUG
        Debugger.Launch();
#endif

        _logger.WriteLine($"[{_modConfig.ModId}] Initializing..", _logger.ColorGreen);

        var startupScannerController = _modLoader.GetController<IStartupScanner>();
        if (startupScannerController == null || !startupScannerController.TryGetTarget(out _startupScanner))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Unable to get IStartupScanner", _logger.ColorRed);
            return;
        }

        _imageBase = Process.GetCurrentProcess().MainModule!.BaseAddress;
        var memory = Reloaded.Memory.Memory.Instance;

        // Find data we can overwrite, 'incorrect length check' from zlib is just fine
        // Need space to change a vcomiss instruction
        SigScan("69 6E 63 6F 72 72 65 63 74 20 6C 65 6E 67 74 68 20 63 68 65 63 6B", "", address =>
        {
            _timeAddr = address;
            ApplyTime();
        });

        SigScan("C5 F8 2F 05 ?? ?? ?? ?? 72 ?? 83 C9 ?? 89 4E", "", instAddr => 
        {
            // vcomiss xmm0, cs:value,
            // ^ value is 5.0
            _logger.WriteLine($"[{_modConfig.ModId}] Found time check address: 0x{instAddr:X8}", _logger.ColorGreen);

            nint afterInstAddr = instAddr - _imageBase + 8;
            byte[] addr = BitConverter.GetBytes((int)((_timeAddr - _imageBase) - afterInstAddr));
            Reloaded.Memory.Memory.Instance.SafeWrite((nuint)instAddr + 4, addr);
        });
    }

    private unsafe void ApplyTime()
    {
        _logger.WriteLine($"[{_modConfig.ModId}] Applying time to run: {_configuration.TimeToRun}", _logger.ColorGreen);
        Reloaded.Memory.Memory.Instance.SafeWrite((nuint)_timeAddr, BitConverter.GetBytes(_configuration.TimeToRun));
    }

    private void SigScan(string pattern, string name, Action<nint> action)
    {
        _startupScanner?.AddMainModuleScan(pattern, result =>
        {
            if (!result.Found)
            {
                return;
            }
            action(_imageBase + result.Offset);
        });
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");

        ApplyTime();
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}