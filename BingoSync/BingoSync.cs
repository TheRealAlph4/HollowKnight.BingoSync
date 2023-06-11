using Modding;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;
using BingoSync.CustomVariables;
using BingoSync.CustomVariables.Rando;
using UnityEngine;
using ItemChanger;

namespace BingoSync
{
    public class BingoSync : Mod, ILocalSettings<Settings>
    {
        new public string GetName() => "BingoSync";
        public override string GetVersion() => "0.0.0.2";

        const float fadeDuration = 0.2f;

        public override void Initialize()
        {
            // Check bingo objectives every frame
            ModHooks.HeroUpdateHook += HeroUpdate;

            // General
            ModHooks.SetPlayerBoolHook += UpdateBoolInternal;
            ModHooks.SetPlayerIntHook += UpdateIntInternal;

            // GeoSpent
            On.GeoCounter.TakeGeo += GeoSpent.UpdateGeoSpent;
            On.GeoCounter.Update += GeoSpent.UpdateGeoText;

            // Tolls
            On.GeoCounter.TakeGeo += Tolls.UpdateTolls;

            // Grubs
            ModHooks.SetPlayerIntHook += Grubs.CheckIfGrubWasSaved;

            // Myla
            ModHooks.SetPlayerIntHook += Myla.CheckIfMylaWasKilled;

            // Revek
            On.HeroController.NailParry += Revek.CheckParry;
            On.HeroController.EnterScene += Revek.EnterRoom;
            
            // Lifts
            ModHooks.SetPlayerBoolHook += Lifts.CheckIfLiftWasUsed;
            
            // Jiji
            ModHooks.SetPlayerBoolHook += Jiji.CheckIfKilledShadeInJijis;

            // Dialogue
            On.DialogueBox.StartConversation += Dialogue.StartConversation;

            // Hot Springs
            ModHooks.SetPlayerIntHook += HotSprings.CheckBath;

            // Hive Shard
            ModHooks.SetPlayerBoolHook += HiveShard.CheckIfHiveShardWasCollected;
        
            // Tram
            ModHooks.SetPlayerIntHook += Tram.CheckIfStationWasVisited;

            // Stalking Devouts
            ModHooks.OnReceiveDeathEventHook += StalkingDevouts.CheckIfStalkingDevoutWasKilled;

            // Giant Geo Egg
            ModHooks.HeroUpdateHook += GiantGeoEgg.CheckIfGiantGeoEggWasDestroyed;
            ModHooks.ColliderCreateHook += GiantGeoEgg.FindGiantGeoEggGameObject;

            // Rando
            AbstractItem.AfterGiveGlobal += Checks.Checked;

            // Menu
            On.UIManager.ContinueGame += ContinueGame;
            On.UIManager.StartNewGame += StartNewGame;
            On.UIManager.FadeInCanvasGroup += FadeIn;
            On.UIManager.FadeOutCanvasGroup += FadeOut;

            var _hook = new ILHook
            (
                typeof(DreamPlant).GetMethod("CheckOrbs", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                DreamTrees.TrackDreamTrees
            );

            MenuUI.Setup();
            BingoSyncClient.Setup(Log);
            BingoTracker.Setup(Log);
            BingoBoardUI.Setup(Log);
            RetryHelper.Setup(Log);
        }

        private IEnumerator FadeOut(On.UIManager.orig_FadeOutCanvasGroup orig, UIManager self, CanvasGroup cg)
        {
            if (cg.name == "MainMenuScreen")
            {
                MenuUI.layoutRoot.BeginFade(0, fadeDuration);
            }
            return orig(self, cg);
        }

        private IEnumerator FadeIn(On.UIManager.orig_FadeInCanvasGroup orig, UIManager self, CanvasGroup cg)
        {
            if (cg.name == "MainMenuScreen")
            {
                MenuUI.layoutRoot.BeginFade(1, fadeDuration);
            }
            return orig(self, cg);
        }

        private void ContinueGame(On.UIManager.orig_ContinueGame orig, UIManager self)
        {
            MenuUI.layoutRoot.BeginFade(0, fadeDuration);
            ConfigureBingoSync();
            orig(self);
        }

        private void StartNewGame(On.UIManager.orig_StartNewGame orig, UIManager self, bool permaDeath, bool bossRush)
        {
            MenuUI.layoutRoot.BeginFade(0, fadeDuration);
            ConfigureBingoSync();
            orig(self, permaDeath, bossRush);
        }

        private void ConfigureBingoSync()
        {
            BingoSyncClient.UpdateBoard();
        }

        private void HeroUpdate()
        {
            BingoTracker.ProcessBingo();
        }

        private bool UpdateBoolInternal(string name, bool orig)
        {
            PlayerData.instance.SetBoolInternal(name, orig);
            BingoTracker.UpdateBoolean(name, orig);
            return orig;
        }

        private int UpdateIntInternal(string name, int current)
        {
            var previous = PlayerData.instance.GetIntInternal(name);
            PlayerData.instance.SetIntInternal(name, current);
            BingoTracker.UpdateInteger(name, previous, current);
            return current;
        }

        public void OnLoadLocal(Settings s)
        {
            BingoTracker.settings = s;
        }

        public Settings OnSaveLocal()
        {
            return BingoTracker.settings;
        }
    }
}