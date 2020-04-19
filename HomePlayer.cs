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
        private bool _goingHome;
        private DateTime _lastCalledHomeCommand;
        private Vector3 _lastCalledHomePos;
        private bool _waitRestricted;
        private byte _waitTime;
        private bool _movementRestricted;
        private bool _canGoHome;
        private Vector3 _bedPos;
        private byte _bedRot;
        private UnturnedPlayer _player;

        public void Awake()
        {
            Rocket.Core.Logging.Logger.Log("Homeplayer is awake");
            Console.Write("Homeplayer is awake.");
        }
        protected override void Load()
        {
            _goingHome = false;
            _canGoHome = false;
        }
        public void GoHome(Vector3 bedPos, byte bedRot, UnturnedPlayer player)
        {
            Rocket.Core.Logging.Logger.Log("starting gohome");
            _waitRestricted = HomeCommand.Instance.Configuration.Instance.TeleportWait;
            _movementRestricted = HomeCommand.Instance.Configuration.Instance.MovementRestriction;
            _player = player;
            _bedPos = Vector3.up + bedPos + new Vector3(0f, 0.5f, 0f);
            _bedRot = bedRot;

            if (_waitRestricted)
            {
                // We have to wait to teleport now find out how long
                _lastCalledHomeCommand = DateTime.Now;
                if (HomeCommand.Instance.WaitGroups.ContainsKey("all"))
                {
                    HomeCommand.Instance.WaitGroups.TryGetValue("all", out _waitTime);
                }
                else
                {
                    if (player.IsAdmin && HomeCommand.Instance.WaitGroups.ContainsKey("admin"))
                    {
                        HomeCommand.Instance.WaitGroups.TryGetValue("admin", out _waitTime);
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
                        _waitTime = time2[0];
                    }
                }
                if (_movementRestricted)
                {
                    _lastCalledHomePos = this.transform.position;
                    UnturnedChat.Say(player, String.Format(HomeCommand.Instance.Configuration.Instance.FoundBedWaitNoMoveMsg, player.CharacterName, this._waitTime));
                }
                else
                {
                    UnturnedChat.Say(player, String.Format(HomeCommand.Instance.Configuration.Instance.FoundBedNowWaitMsg, player.CharacterName, _waitTime));
                }
            }
            else
            {
                this._canGoHome = true;
            }
            _goingHome = true;
            DoGoHome();
        }
        private void DoGoHome()
        {
            Rocket.Core.Logging.Logger.Log("starting dogohome");
            if (!_canGoHome) return;
            UnturnedChat.Say(_player, String.Format(HomeCommand.Instance.Configuration.Instance.TeleportMsg, _player.CharacterName));
            _player.Teleport(_bedPos, _bedRot);
            _canGoHome = false;
            _goingHome = false;
        }
        public void FixedUpdate()
        {
            if (!_goingHome) return;
            if (_player.Dead)
            {
                // Abort teleport, they died.
                UnturnedChat.Say(_player, string.Format(HomeCommand.Instance.Configuration.Instance.NoTeleportDiedMsg, _player.CharacterName));
                _goingHome = false;
                _canGoHome = false;
                return;
            }
            if (_movementRestricted)
            {
                if (Vector3.Distance(_player.Position, _lastCalledHomePos) > 0.1)
                {
                    // Abort teleport, they moved.
                    UnturnedChat.Say(_player, String.Format(HomeCommand.Instance.Configuration.Instance.UnableMoveSinceMoveMsg, _player.CharacterName));
                    _goingHome = false;
                    _canGoHome = false;
                    return;
                }
            }
            if (_waitRestricted)
            {
                if ((DateTime.Now - _lastCalledHomeCommand).TotalSeconds < _waitTime) return;
            }
            // We made it this far, we can go home.
            _canGoHome = true;
            DoGoHome();
        }
    }
}
