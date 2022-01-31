using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ZaupHomeCommand
{
    public class HomeGroup
    {
        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute]
        public double Wait { get; set; }
    }
    public class Config : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public bool TeleportWait = false;
        public bool MovementRestriction = false;
        public bool DontAllowCuffed = false;
        public double AdminWait = 4f;
        public List<HomeGroup> WaitGroups = new List<HomeGroup>();

        public void LoadDefaults()
        {
            WaitGroups = new List<HomeGroup>() {
                new HomeGroup{Id = "default", Wait = 5},
                new HomeGroup{Id = "moderator", Wait = 3}
            };
        }
    }
}