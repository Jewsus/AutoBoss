using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace AutoBoss
{
    [ApiVersion(1, 23)]
    public class AutoBoss : TerrariaPlugin
    {
        public ABConfig configObj { get; set; }
        public Player[] Players { get; set; }
        internal static string ABConfigPath { get { return Path.Combine(TShock.SavePath, "AutoBossConfig.json"); } }
        private TShockAPI.DB.Region arenaregion = new TShockAPI.DB.Region();
        private DateTime LastCheck = DateTime.UtcNow;
        private DateTime OtherLastCheck = DateTime.UtcNow;
        private int BossTimer = 30;
        private List<NPC> bossList = new List<NPC>();
        private bool BossToggle = false;
        Random rndGen = new Random();
        public override string Name
        {
            get { return "AutoBoss"; }
        }
        public override string Author
        {
            get { return "Jewsus"; }
        }
        public override string Description
        {
            get { return "AutoBoss Plugin"; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
            base.Dispose(disposing);
        }
        public AutoBoss(Main game) : base(game)
        {
            Order = 1;
        }
        public void OnInitialize(EventArgs args)
        {
            configObj = new ABConfig();
            SetupConfig();
            Commands.ChatCommands.Add(new Command("autoboss", AutoBossToggle, "abtoggle"));
            Commands.ChatCommands.Add(new Command("autoboss", AutoBossReload, "abreload"));
            //Commands.ChatCommands.Add(new Command("autoboss", ABdebug, "absdebug"));
        }
        public void AutoBossToggle(CommandArgs args)
        {
            BossToggle = !BossToggle;
            if (BossToggle == true)
            {
                foreach (TShockAPI.DB.Region reg in TShock.Regions.ListAllRegions(Main.worldID.ToString()))
                {
                    if (reg.Name == "arena") { arenaregion = reg; }
                }
                if (arenaregion.Name != "arena") { TShock.Utils.Broadcast("Error: Region 'arena' is not defined.", Color.Red); BossToggle = false; }
            }
            args.Player.SendSuccessMessage("Boss battles now: " + ((BossToggle) ? "Enabled" : "Disabled"));
            BossTimer = configObj.BossTimer;
        }
        public void AutoBossReload(CommandArgs args)
        {
            SetupConfig();
            args.Player.SendSuccessMessage("AutoBoss config reloaded.");
        }
        /*  public void ABdebug(CommandArgs args)
          {
              args.Player.SendMessage("arena x: " + arenaregion.Area.X);

          }*/
        public void OnUpdate(EventArgs args)
        {
            if (BossToggle && ((DateTime.UtcNow - LastCheck).TotalSeconds >= 1))
            {
                LastCheck = DateTime.UtcNow;
                if (!Main.dayTime && BossTimer < 601)
                {
                    if (BossTimer == configObj.BossTimer)
                    {
                        if (configObj.BossText.Length > 1) { TShock.Utils.Broadcast(configObj.BossText, Color.Aquamarine); }
                    }
                    else if (BossTimer == 10)
                    {
                        if (configObj.BossText10s.Length > 1) { TShock.Utils.Broadcast(configObj.BossText10s, Color.Aquamarine); }
                    }
                    else if (BossTimer == 0)
                    {
                        if (configObj.BossText0s.Length > 1) { TShock.Utils.Broadcast(configObj.BossText0s, Color.Aquamarine); }
                        startBossBattle();
                    }
                    else if ((BossTimer < 0) && (BossTimer % 20 == 0))
                    {
                        bool bossActive = false;
                        for (int i = 0; i < bossList.Count; i++)
                        {
                            if (bossList[i].active) bossActive = true;
                        }
                        if (bossActive) spawnMinions();
                        else
                        {
                            if (configObj.BossDefeat.Length > 1) { TShock.Utils.Broadcast(configObj.BossDefeat, Color.Aquamarine); }
                            BossTimer = 601;
                            for (int i = 0; i < Main.npc.Length; i++)
                            {
                                if (Main.npc[i].active && (Main.npc[i].type == 70 || Main.npc[i].type == 72))
                                {
                                    TSPlayer.Server.StrikeNPC(i, 9999, 90f, 1);
                                }
                            }
                        }
                    }
                    BossTimer--;
                }
                else if (BossTimer != configObj.BossTimer && Main.dayTime)
                {
                    BossTimer = configObj.BossTimer;
                }
            }
        }
        private void startBossBattle()
        {
            NPC npc = new NPC();
            arenaregion = TShock.Regions.GetRegionByName("arena");
            int arenaX = arenaregion.Area.X + (arenaregion.Area.Width / 2);
            int arenaY = arenaregion.Area.Y + (arenaregion.Area.Height / 2);
            string broadcastString = "Boss selected:";
            BossSet bossSet = configObj.BossList[rndGen.Next(0, configObj.BossList.Count)];
            foreach (BossObj b in bossSet.bosses)
            {
                npc = TShock.Utils.GetNPCById(b.id);
                TSPlayer.Server.SpawnNPC(npc.type, npc.name, b.amt, arenaX, arenaY, 30, 30);
                broadcastString += " " + b.amt + "x " + npc.name + " +";
            }
            broadcastString = broadcastString.Remove(broadcastString.Length - 2);
            TShockAPI.TShock.Utils.Broadcast(broadcastString, Color.Aquamarine);
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].boss) bossList.Add(Main.npc[i]);
            }
        }
        private void spawnMinions()
        {
            //TODO: num of henchmen based on players in arena, spawn life when boss is at half health.
            NPC npc = new NPC();
            npc = TShock.Utils.GetNPCById(configObj.MinionsList[rndGen.Next(0, configObj.MinionsList.Length)]);
            arenaregion = TShock.Regions.GetRegionByName("arena");
            int arenaX = arenaregion.Area.X + (arenaregion.Area.Width / 2);
            int arenaY = arenaregion.Area.Y + (arenaregion.Area.Height / 2);
            int henchmenNumber = rndGen.Next(configObj.MinionsMinMax[0], configObj.MinionsMinMax[1] + 1);
            TSPlayer.Server.SpawnNPC(npc.type, npc.name, henchmenNumber, arenaX, arenaY, 30, 30);
            if (configObj.MinionsAnnounce) { TShock.Utils.Broadcast("Spawning Boss Minions: " + henchmenNumber + "x " + npc.name + "!", Color.SteelBlue); }
        }
        public void SetupConfig()
        {
            try
            {
                if (File.Exists(ABConfigPath))
                {
                    configObj = new ABConfig();
                    configObj = ABConfig.Read(ABConfigPath);
                    BossTimer = configObj.BossTimer;
                }
                else
                {
                    configObj.Write(ABConfigPath);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in AutoBoss config file");
                Console.ForegroundColor = ConsoleColor.Gray;
                TShock.Log.ConsoleError("AutoBoss Config Exception");
                TShock.Log.ConsoleError(ex.ToString());
            }
        }
    }
}
