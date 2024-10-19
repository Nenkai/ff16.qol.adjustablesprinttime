using ff16.qol.adjustablesprinttime.Template.Configuration;

using Reloaded.Mod.Interfaces.Structs;

using System.ComponentModel;

namespace ff16.qol.adjustablesprinttime.Configuration
{
    public class Config : Configurable<Config>
    {
        [DisplayName("Time before running")]
        [Description("Time to run in seconds.")]
        [DefaultValue(1.0f)]
        public float TimeToRun { get; set; } = 1.0f;
    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}
