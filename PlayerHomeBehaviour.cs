using Rocket.API;
using Rocket.Core;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Linq;
using UnityEngine;
using static ZaupHomeCommand.Main;

namespace ZaupHomeCommand
{
    public class PlayerHomeBehaviour : UnturnedPlayerComponent
    {
        public PlayerHomeBehaviour()
        {
        }

        public DateTime LastCalledHomeCommand { get; private set; }
        public Vector3 LastCalledHomePos { get; private set; }
        public Vector3 LastBedPos { get; private set; }
        public byte LastBedRot { get; private set; }

        double TimeToWait;

        bool WaitingForTeleport, AllowedToTeleport;

        void FixedUpdate()
        {
            if (inst.State != PluginState.Loaded)
                Destroy(this);

            if (!WaitingForTeleport)
                return;

            var msg = "";
            if (Player.Dead) // Abort teleport, player died.
                msg = NoTeleportDiedMsg;
            else if (conf.MovementRestriction && Vector3.Distance(Player.Position, LastCalledHomePos) > 0.1) // Abort teleport, player moved.
                msg = UnableMoveSinceMoveMsg;

            if (msg != "")
            {
                UnturnedChat.Say(Player, inst.Translate(msg, Player.CharacterName));
                WaitingForTeleport = false;
                AllowedToTeleport = false;
                return;
            }
            if (TimeToWait > 0 && (DateTime.Now - LastCalledHomeCommand).TotalSeconds < TimeToWait)
                return;
            AllowedToTeleport = true;
            Teleport();
        }

        public bool CheckHomeConditions(out Vector3 pos, out byte rot)
        {
            pos = Player.Position;
            rot = Player.Player.look.rot;
            try
            {
                if (!conf.Enabled)
                    throw new ArgumentException(DisabledMsg); // Disabled.
                if (Player.Stance == EPlayerStance.DRIVING || Player.Stance == EPlayerStance.SITTING)
                    throw new ArgumentException(NoVehicleMsg); // In the vehicle.
                if (Player.Player.animator.gesture == EPlayerGesture.ARREST_START && conf.DontAllowCuffed)
                    throw new ArgumentException(CuffedMsg); // Cuffed.
                if (!BarricadeManager.tryGetBed(Player.CSteamID, out pos, out rot))
                    throw new ArgumentException(NoBedMsg); // Bed not found.
                return true;
            }
            catch (ArgumentException ex) { UnturnedChat.Say(Player, inst.Translate(ex.Message, Player.CharacterName)); }
            return false;
        }

        public void TryTeleport()
        {
            if (!CheckHomeConditions(out var pos, out var rot)) return;
            LastBedPos = Vector3.up + pos;
            LastBedRot = rot;

            if (conf.TeleportWait)
            {
                LastCalledHomeCommand = DateTime.Now;
                LastCalledHomePos = Player.Position;

                if (!Player.IsAdmin)
                {
                    TimeToWait =
                        R.Permissions
                        .GetGroups(Player, false)
                        .Select(x => WaitGroups.TryGetValue(x.Id, out var time) ? time : -1)
                        .OrderBy(x => x)
                        .FirstOrDefault(x => x > 0);
                    // Take the lowest time.
                }
                else TimeToWait = conf.AdminWait;

                UnturnedChat.Say(Player, inst.Translate(
                    conf.MovementRestriction ? FoundBedWaitNoMoveMsg : FoundBedNowWaitMsg,
                    Player.CharacterName,
                    TimeToWait));
            }
            else
                AllowedToTeleport = true;
            WaitingForTeleport = true;
            Teleport();
        }

        void Teleport()
        {
            if (!AllowedToTeleport) return;

            UnturnedChat.Say(Player, inst.Translate(TeleportMsg, Player.CharacterName));
            Player.Player.teleportToLocationUnsafe(LastBedPos, LastBedRot); 
            AllowedToTeleport = false;
            WaitingForTeleport = false;
        }
    }
}