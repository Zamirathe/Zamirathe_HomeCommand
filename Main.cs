using Rocket.API.Collections;
using Rocket.Core;
using Rocket.Core.Assets;
using Rocket.Core.Commands;
using Rocket.Core.Plugins;
using Rocket.Core.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ZaupHomeCommand
{
    public class Main : RocketPlugin<Config>
    {
        public static Main Instance;
        internal static Main inst => Instance;
        internal static Config conf => Instance.Configuration.Instance;
        public static Dictionary<string, double> WaitGroups => conf.WaitGroups.ToDictionary(x => x.Id, x => x.Wait);

        protected override void Load()
        {
            Instance = this;
            var mappings =
                typeof(RocketCommandManager)
                .GetField("commandMappings", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(R.Commands) as XMLFileAsset<RocketCommands>;
            // set high priority for our command to overwrite rocketmod/home
            mappings.Instance.CommandMappings.FirstOrDefault(x => x.Class.StartsWith(typeof(CommandHome).FullName)).Priority = CommandPriority.High;
        }

        public const string
            FoundBedWaitNoMoveMsg = nameof(FoundBedWaitNoMoveMsg),
            UnableMoveSinceMoveMsg = nameof(UnableMoveSinceMoveMsg),
            NoTeleportDiedMsg = nameof(NoTeleportDiedMsg),
            DisabledMsg = nameof(DisabledMsg),
            NoBedMsg = nameof(NoBedMsg),
            NoVehicleMsg = nameof(NoVehicleMsg),
            TeleportMsg = nameof(TeleportMsg),
            CuffedMsg = nameof(CuffedMsg),
            FoundBedNowWaitMsg = nameof(FoundBedNowWaitMsg);

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { FoundBedWaitNoMoveMsg, "I have located your bed {0}, now don't move for {1} seconds while I prepare you for teleport." },
            { UnableMoveSinceMoveMsg, "I'm sorry {0}, but you moved. I am unable to teleport you." },
            { NoTeleportDiedMsg, "Sorry {0}, unable to finish home teleport as you died." },
            { TeleportMsg, "You were sent back to your bed."},
            { FoundBedNowWaitMsg, "I have located your bed {0}, please wait for {1} seconds to be teleported."},
            { DisabledMsg, "I'm sorry {0}, but the home command has been disabled."},
            { NoBedMsg, "I'm sorry {0}, but I could not find your bed."},
            { NoVehicleMsg, "I'm sorry {0}, but you can't be teleported while inside a vehicle."},
            { CuffedMsg, "I'm sorry {0}, but you can't be teleported while cuffed."},
        };
    }
}