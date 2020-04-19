using System;
using System.Collections.Generic;

using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Plugins;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Steamworks;

namespace ZaupHomeCommand
{
    public class HomeCommand : RocketPlugin<HomeCommandConfiguration>
    {
        public Dictionary<string, byte> WaitGroups;
        public static HomeCommand Instance;

        protected override void Load()
        {
            HomeCommand.Instance = this;
            WaitGroups = new Dictionary<string, byte>();
            foreach (HomeGroup hg in this.Configuration.Instance.WaitGroups)
            {
                WaitGroups.Add(hg.Id, hg.Wait);
            }
        }
        // All we are doing here is checking the config to see if anything like restricted movement or time restriction is enforced.
        public static object[] CheckConfig(UnturnedPlayer player)
        {
            
            object[] returnv = { false, null, null };
            // First check if command is enabled.
            if (!Instance.Configuration.Instance.Enabled)
            {
                // Command disabled.
                UnturnedChat.Say(player, String.Format(Instance.Configuration.Instance.DisabledMsg, player.CharacterName));
                return returnv;
            }
            // It is enabled, but are they in a vehicle?
            if (player.Stance == EPlayerStance.DRIVING || player.Stance == EPlayerStance.SITTING)
            {
                // They are in a vehicle.
                UnturnedChat.Say(player, String.Format(Instance.Configuration.Instance.NoVehicleMsg, player.CharacterName));
                return returnv;
            }
            // They aren't in a vehicle, so check if they have a bed.    
            Vector3 bedPos;
            byte bedRot;
            if (!BarricadeManager.tryGetBed(player.CSteamID, out bedPos, out bedRot))
            {
                // Bed not found.
                UnturnedChat.Say(player, String.Format(Instance.Configuration.Instance.NoBedMsg, player.CharacterName));
                return returnv;
            }
            object[] returnv2 = { true, bedPos, bedRot };
            return returnv2;
        }
    }
}