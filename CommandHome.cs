using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace ZaupHomeCommand
{
    public class CommandHome : IRocketCommand
    {
        public string Name => "home";

        public string Help => "Teleports you to your bed if you have one.";

        public string Syntax => Name;

        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>();

        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public void Execute(IRocketPlayer caller, string[] bed)
        {
            var up = (UnturnedPlayer)caller;
            up.Player.gameObject.GetOrAddComponent<PlayerHomeBehaviour>().TryTeleport();
        }
    }
}