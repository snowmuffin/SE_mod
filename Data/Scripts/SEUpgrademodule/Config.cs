using System;
using System.IO;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game;
using VRage.Utils;

namespace SEUpgrademodule
{
    public class Config
    {
        public static MyUpConfig Instance;

        public static void Load()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("SEUpgrademoduleConfig.xml", typeof(MyUpConfig)))
            {
                try
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("SEUpgrademoduleConfig.xml", typeof(MyUpConfig));
                    var xmlData = reader.ReadToEnd();
                    Instance = MyAPIGateway.Utilities.SerializeFromXML<MyUpConfig>(xmlData);
                    reader.Dispose();
                    MyLog.Default.WriteLine("SEUpgrademodule: found and loaded");
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("SEUpgrademodule: loading failed, generating new Config");
                }
            }

            if (Instance == null)
            {
                MyLog.Default.WriteLine("SEUpgrademodule: No Loot Config found, creating New");
                Instance = new MyUpConfig()
                {
                    SmallGridBasic = new Item() { Chance = 0.25f, MinAmount = 3, MaxAmount = 12 },
                    LargeGridBasic = new Item() { Chance = 0.2f, MinAmount = 6, MaxAmount = 25 },
                    SmallGridAdvanced = new Item() { Chance = 0.2f, MinAmount = 2, MaxAmount = 6 },
                    LargeGridAdvanced = new Item() { Chance = 0.2f, MinAmount = 2, MaxAmount = 6 },
                    NpcMultiplier  = new NpcMultiplier() {Attack = 2, Defence = 2, Power=2, Speed=1},
                    NpcOffset = new NpcOffset(){Attack = 1, Defence = 1, Power=1, Speed=1},
                    DisableGrindSubgridDamage = true,
                    ExcludeGrids = new List<string>() { "respawn","Respawn" }
                };
            }

            if(Instance.ExcludeGrids == null)
            {
                Instance.ExcludeGrids = new List<string>() { "respawn" };
            }
            if(Instance.DisableGrindSubgridDamage == null)
            {
                Instance.DisableGrindSubgridDamage = true;
            }

            Write();
        }

        public static void Write()
        {
            if (Instance == null) return;

            try
            {
                MyLog.Default.WriteLine("SEUpgrademodule: Serializing to XML... ");
                string xml = MyAPIGateway.Utilities.SerializeToXML<MyUpConfig>(Instance);
                MyLog.Default.WriteLine("SEUpgrademodule: Writing to disk... ");
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("SEUpgrademoduleConfig.xml", typeof(MyUpConfig));
                writer.Write(xml);
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("SEUpgrademodule: Error saving XML!" + e.StackTrace);
            }
        }
    }
}