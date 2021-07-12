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
            p = GetComponent<Player>();
            up = UnturnedPlayer.FromPlayer(p);
        }

        public DateTime LastCalledHomeCommand { get; private set; }
        public Vector3 LastCalledHomePos { get; private set; }
        public Vector3 LastBedPos { get; private set; }
        public byte LastBedRot { get; private set; }

        double TimeToWait;

        bool WaitingForTeleport, AllowedToTeleport;

        readonly Player p;
        readonly UnturnedPlayer up;

        void FixedUpdate()
        {
            if (inst.State != PluginState.Loaded)
                Destroy(this);

            if (!WaitingForTeleport)
                return;

            var msg = "";
            if (up.Dead) // Abort teleport, player died.
                msg = NoTeleportDiedMsg;
            else if (conf.MovementRestriction && Vector3.Distance(up.Position, LastCalledHomePos) > 0.1) // Abort teleport, player moved.
                msg = UnableMoveSinceMoveMsg;

            if (msg != "")
            {
                UnturnedChat.Say(up, inst.Translate(msg, up.CharacterName));
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
            pos = up.Position;
            rot = up.Player.look.rot;
            try
            {
                if (!conf.Enabled)
                    throw new ArgumentException(DisabledMsg); // Disabled.
                if (up.Stance == EPlayerStance.DRIVING || up.Stance == EPlayerStance.SITTING)
                    throw new ArgumentException(NoVehicleMsg); // In the vehicle.
                if (p.animator.gesture == EPlayerGesture.ARREST_START && conf.DontAllowCuffed)
                    throw new ArgumentException(CuffedMsg); // Cuffed.
                if (!BarricadeManager.tryGetBed(up.CSteamID, out pos, out rot))
                    throw new ArgumentException(NoBedMsg); // Bed not found.
                return true;
            }
            catch (ArgumentException ex) { UnturnedChat.Say(up, inst.Translate(ex.Message, up.CharacterName)); }
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
                LastCalledHomePos = up.Position;

                if (!up.IsAdmin)
                {
                    TimeToWait =
                        R.Permissions
                        .GetGroups(up, false)
                        .Select(x => WaitGroups.TryGetValue(x.Id, out var time) ? time : -1)
                        .OrderBy(x => x)
                        .FirstOrDefault(x => x > 0);
                    // Take the lowest time.
                }
                else TimeToWait = conf.AdminWait;

                UnturnedChat.Say(up, inst.Translate(
                    conf.MovementRestriction ? FoundBedWaitNoMoveMsg : FoundBedNowWaitMsg,
                    up.CharacterName,
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

            UnturnedChat.Say(up, inst.Translate(TeleportMsg, up.CharacterName));
            p.teleportToLocationUnsafe(LastBedPos, LastBedRot); // Hello, Nelson, why did you make safe Teleport for UnturnedPlayer?
            AllowedToTeleport = false;
            WaitingForTeleport = false;
        }
    }
}