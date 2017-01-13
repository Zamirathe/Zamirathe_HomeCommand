using System;
using System.Collections.Generic;

using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;
using Rocket.API.Serialisation;

namespace ZaupHomeCommand
{
    public class HomePlayer : UnturnedPlayerComponent
    {
        private bool GoingHome;
        private DateTime LastCalledHomeCommand;
        private Vector3 LastCalledHomePos;
        private bool waitrestricted;
        private byte waittime;
        private bool movementrestricted;
        private bool cangohome;
        private Vector3 bedPos;
        private byte bedRot;
        private UnturnedPlayer p;

        public void Awake()
        {
            Rocket.Core.Logging.Logger.Log("Homeplayer is awake");
            Console.Write("Homeplayer is awake.");
        }
        protected override void Load()
        {
            this.GoingHome = false;
            this.cangohome = false;
        }
        public void GoHome(Vector3 bedPos, byte bedRot, UnturnedPlayer player)
        {
            Rocket.Core.Logging.Logger.Log("starting gohome");
            this.waitrestricted = HomeCommand.Instance.Configuration.Instance.TeleportWait;
            this.movementrestricted = HomeCommand.Instance.Configuration.Instance.MovementRestriction;
            this.p = player;
            this.bedPos = Vector3.up + bedPos;
            this.bedRot = bedRot;

            if (this.waitrestricted)
            {
                // We have to wait to teleport now find out how long
                this.LastCalledHomeCommand = DateTime.Now;
                if (HomeCommand.Instance.WaitGroups.ContainsKey("all"))
                {
                    HomeCommand.Instance.WaitGroups.TryGetValue("all", out this.waittime);
                }
                else
                {
                    if (player.IsAdmin && HomeCommand.Instance.WaitGroups.ContainsKey("admin"))
                    {
                        HomeCommand.Instance.WaitGroups.TryGetValue("admin", out this.waittime);
                    }
                    else
                    {
                        // Either not an admin or they don't get special wait restrictions.
                        List<RocketPermissionsGroup> hg = R.Permissions.GetGroups(player, true);
                        if (hg.Count <= 0)
                        {
                            Rocket.Core.Logging.Logger.Log("There was an error as a player has no groups!");
                        }
                        byte[] time2 = new byte[hg.Count];
                        for (byte g=0;g<hg.Count;g++)
                        {
                            
                            RocketPermissionsGroup hgr = hg[g];
                            HomeCommand.Instance.WaitGroups.TryGetValue(hgr.Id, out time2[g]);
                            if (time2[g] <= 0)
                            {
                                time2[g] = 60;
                            }
                        }
                        Array.Sort(time2);
                        // Take the lowest time.
                        this.waittime = time2[0];
                    }
                }
                if (this.movementrestricted)
                {
                    this.LastCalledHomePos = this.transform.position;
                    UnturnedChat.Say(player, String.Format(HomeCommand.Instance.Configuration.Instance.FoundBedWaitNoMoveMsg, player.CharacterName, this.waittime));
                }
                else
                {
                    UnturnedChat.Say(player, String.Format(HomeCommand.Instance.Configuration.Instance.FoundBedNowWaitMsg, player.CharacterName, this.waittime));
                }
            }
            else
            {
                this.cangohome = true;
            }
            this.GoingHome = true;
            this.DoGoHome();
        }
        private void DoGoHome()
        {
            Rocket.Core.Logging.Logger.Log("starting dogohome");
            if (!this.cangohome) return;
            UnturnedChat.Say(this.p, String.Format(HomeCommand.Instance.Configuration.Instance.TeleportMsg, this.p.CharacterName));
            this.p.Teleport(this.bedPos, this.bedRot);
            this.cangohome = false;
            this.GoingHome = false;
        }
        public void FixedUpdate()
        {
            if (!this.GoingHome) return;
            if (this.p.Dead)
            {
                // Abort teleport, they died.
                UnturnedChat.Say(this.p, String.Format(HomeCommand.Instance.Configuration.Instance.NoTeleportDiedMsg, this.p.CharacterName));
                this.GoingHome = false;
                this.cangohome = false;
                return;
            }
            if (this.movementrestricted)
            {
                if (Vector3.Distance(this.p.Position, this.LastCalledHomePos) > 0.1)
                {
                    // Abort teleport, they moved.
                    UnturnedChat.Say(this.p, String.Format(HomeCommand.Instance.Configuration.Instance.UnableMoveSinceMoveMsg, this.p.CharacterName));
                    this.GoingHome = false;
                    this.cangohome = false;
                    return;
                }
            }
            if (this.waitrestricted)
            {
                if ((DateTime.Now - this.LastCalledHomeCommand).TotalSeconds < this.waittime) return;
            }
            // We made it this far, we can go home.
            this.cangohome = true;
            this.DoGoHome();
        }
    }
}
