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
            // Load config xml
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
                // Create default values
                Instance = new MyUpConfig()
                {
                    // Stage 1: Basic (기본)
                    SmallGridBasic = new Item() { Chance = 0.25f, MinAmount = 3, MaxAmount = 12 },
                    LargeGridBasic = new Item() { Chance = 0.2f, MinAmount = 6, MaxAmount = 25 },

                    // Stage 2: Advanced (고급)
                    SmallGridAdvanced = new Item() { Chance = 0.2f, MinAmount = 2, MaxAmount = 11 },
                    LargeGridAdvanced = new Item() { Chance = 0.2f, MinAmount = 5, MaxAmount = 13 },


                    DisableGrindSubgridDamage = true,
                    ExcludeGrids = new List<string>() { "respawn" }
                };
            }


            // Updates
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