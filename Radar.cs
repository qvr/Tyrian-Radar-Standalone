﻿using EFT;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Comfort.Common;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using LootItem = EFT.Interactive.LootItem;
using Debug = UnityEngine.Debug;
using System.Linq;
using static MineDirectional;

namespace Radar
{
    [BepInPlugin("Tyrian.Radar", "Radar", "1.1.1")]
    public class Radar : BaseUnityPlugin
    {
        private static GameWorld gameWorld;
        public static bool MapLoaded() => Singleton<GameWorld>.Instantiated;
        public static Player player;
        public static Radar instance;
        public static Dictionary<GameObject, HashSet<Material>> objectsMaterials = new Dictionary<GameObject, HashSet<Material>>();

        const string baseSettings = "Base Settings";
        const string advancedSettings = "Advanced Settings";
        const string colorSettings = "Color Settings";
        const string radarSettings = "Radar Settings";

        public static ConfigEntry<string> radarLanguage;
        public static ConfigEntry<bool> radarEnableConfig;
        public static ConfigEntry<bool> radarEnablePulseConfig;
        public static ConfigEntry<bool> radarEnableCorpseConfig;
        public static ConfigEntry<bool> radarEnableLootConfig;
        public static ConfigEntry<KeyboardShortcut> radarEnableShortCutConfig;
        public static ConfigEntry<KeyboardShortcut> radarEnableCorpseShortCutConfig;
        public static ConfigEntry<KeyboardShortcut> radarEnableLootShortCutConfig;
        public static bool enableSCDown = false;
        public static bool corpseSCDown = false;
        public static bool lootSCDown = false;

        public static ConfigEntry<float> radarSizeConfig;
        public static ConfigEntry<float> radarBlipSizeConfig;
        public static ConfigEntry<float> radarDistanceScaleConfig;
        public static ConfigEntry<float> radarYHeightThreshold;
        public static ConfigEntry<float> radarOffsetYConfig;
        public static ConfigEntry<float> radarOffsetXConfig;
        public static ConfigEntry<float> radarRangeConfig;
        public static ConfigEntry<float> radarScanInterval;
        public static ConfigEntry<float> radarLootThreshold;

        public static ConfigEntry<Color> bossBlipColor;
        public static ConfigEntry<Color> usecBlipColor;
        public static ConfigEntry<Color> bearBlipColor;
        public static ConfigEntry<Color> scavBlipColor;
        public static ConfigEntry<Color> corpseBlipColor;
        public static ConfigEntry<Color> lootBlipColor;
        public static ConfigEntry<Color> backgroundColor;


        public static ManualLogSource logger;

        public static Radar Instance
        {
            get { return instance; }
        }

        private void Awake()
        {
            logger = Logger;
            logger.LogInfo("Radar Plugin Enabled.");
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Add a custom configuration option for the Apply button
            radarLanguage = Config.Bind<string>(baseSettings, "Language", "EN",
                new ConfigDescription("Preferred language, if not available will tried English",
                new AcceptableValueList<string>("EN", "ZH")));

            radarEnableConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_enable"), true);
            radarEnableShortCutConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_enable_shortcut"), new KeyboardShortcut(KeyCode.F10));
            radarEnablePulseConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_pulse_enable"), true, Locales.GetTranslatedString("radar_pulse_enable_info"));

            radarEnableCorpseConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_corpse_enable"), false);
            radarEnableCorpseShortCutConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_corpse_shortcut"), new KeyboardShortcut(KeyCode.F11));
            radarEnableLootConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_loot_enable"), false);
            radarEnableLootShortCutConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_loot_shortcut"), new KeyboardShortcut(KeyCode.F9));

            radarSizeConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_hud_size"), 0.8f,
                new ConfigDescription(Locales.GetTranslatedString("radar_hud_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            radarBlipSizeConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_blip_size"), 0.7f,
                new ConfigDescription(Locales.GetTranslatedString("radar_blip_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            radarDistanceScaleConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_distance_scale"), 0.7f,
                new ConfigDescription(Locales.GetTranslatedString("radar_distance_scale_info"), new AcceptableValueRange<float>(0.1f, 2f)));
            radarYHeightThreshold = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_y_height_threshold"), 1f,
                new ConfigDescription(Locales.GetTranslatedString("radar_y_height_threshold_info"), new AcceptableValueRange<float>(1f, 4f)));
            radarOffsetXConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_x_position"), 0f,
                new ConfigDescription(Locales.GetTranslatedString("radar_x_position_info"), new AcceptableValueRange<float>(-4000f, 4000f)));
            radarOffsetYConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_y_position"), 0f,
                new ConfigDescription(Locales.GetTranslatedString("radar_y_position_info"), new AcceptableValueRange<float>(-4000f, 4000f)));
            radarRangeConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_range"), 128f,
                new ConfigDescription(Locales.GetTranslatedString("radar_range_info"), new AcceptableValueRange<float>(32f, 512f)));
            radarScanInterval = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_scan_interval"), 1f,
                new ConfigDescription(Locales.GetTranslatedString("radar_scan_interval_info"), new AcceptableValueRange<float>(0.1f, 30f)));
            radarLootThreshold = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_loot_threshold"), 30000f,
                new ConfigDescription(Locales.GetTranslatedString("radar_loot_threshold_info"), new AcceptableValueRange<float>(1000f, 100000f)));
            
            bossBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_boss_blip_color"), new Color(1f, 0f, 0f));
            scavBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_scav_blip_color"), new Color(0f, 1f, 0f));
            usecBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_usec_blip_color"), new Color(1f, 1f, 0f));
            bearBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_bear_blip_color"), new Color(1f, 0.5f, 0f));
            corpseBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_corpse_blip_color"), new Color(0.5f, 0.5f, 0.5f));
            lootBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_loot_blip_color"), new Color(0.9f, 0.5f, 0.5f));
            backgroundColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_background_blip_color"), new Color(0f, 0.7f, 0.85f));
        }

        private void Update()
        {
            if (!MapLoaded())
                return;

            gameWorld = Singleton<GameWorld>.Instance;
            player = gameWorld.MainPlayer;
            if (gameWorld == null || player == null)
                return;

            GameObject gamePlayerObject = player.gameObject;
            HaloRadar haloRadar = gamePlayerObject.GetComponent<HaloRadar>();

            // enable radar shortcut process
            if (!enableSCDown && radarEnableShortCutConfig.Value.IsDown())
            {
                radarEnableConfig.Value = !radarEnableConfig.Value;
                enableSCDown = true;
            }
            if (!radarEnableShortCutConfig.Value.IsDown())
            {
                enableSCDown = false;
            }

            // enable corpse shortcut process
            if (!corpseSCDown && radarEnableCorpseShortCutConfig.Value.IsDown())
            {
                radarEnableCorpseConfig.Value = !radarEnableCorpseConfig.Value;
                corpseSCDown = true;
            }
            if (!radarEnableCorpseShortCutConfig.Value.IsDown())
            {
                corpseSCDown = false;
            }

            // enable loot shortcut process
            if (!lootSCDown && radarEnableLootShortCutConfig.Value.IsDown())
            {
                radarEnableLootConfig.Value = !radarEnableLootConfig.Value;
                lootSCDown = true;
            }
            if (!radarEnableLootShortCutConfig.Value.IsDown())
            {
                lootSCDown = false;
            }

            if (radarEnableConfig.Value && haloRadar == null)
            {
                // Add the HaloRadar component if it doesn't exist.
                gamePlayerObject.AddComponent<HaloRadar>();
            }
            else if (!radarEnableConfig.Value && haloRadar != null)
            {
                // Remove the HaloRadar component if it exists.
                haloRadar.Destory();
                Destroy(haloRadar);
            }
        }
    }

    

    public class HaloRadar : MonoBehaviour
    {
        public static GameWorld gameWorld;
        public static Player player;
        public static Object RadarhudPrefab { get; private set; } 
        public static Object RadarBliphudPrefab { get; private set; }
        public static AssetBundle radarBundle;
        public static GameObject radarHud;
        public static GameObject radarBlipHud;
        public static GameObject playerCamera;

        public static RectTransform radarHudBlipBasePosition { get; private set; }
        public static RectTransform radarHudBasePosition { get; private set; }
        public static RectTransform radarHudPulse { get; private set; }
        public static RectTransform radarHudBlip { get; private set; }
        public static Image blipImage;
        public static Sprite EnemyBlip;
        public static Sprite EnemyBlipDown;
        public static Sprite EnemyBlipUp;
        public static Sprite EnemyBlipDead;
        public static Coroutine pulseCoroutine;
        public static float animationDuration = 1f;
        public static float pauseDuration = 4f;
        public static Vector3 radarScaleStart;
        public static float radarPositionYStart = 0f;
        public static float radarPositionXStart = 0f;
        public static bool MapLoaded() => Singleton<GameWorld>.Instantiated;

        public static float radarLastUpdateTime = 0;
        public float radarInterval = -1;

        public HashSet<int> enemyList = new HashSet<int>();
        public List<BlipPlayer> enemyCustomObject = new List<BlipPlayer>();

        public HashSet<string> lootList = new HashSet<string>();
        public List<BlipLoot> lootCustomObject = new List<BlipLoot>();

        private void Start()
        {
            // Create our prefabs from our bundles.
            if (RadarhudPrefab == null)
            {
                String haloRadarHUD = Path.Combine(Environment.CurrentDirectory, "BepInEx/plugins/radar/radarhud.bundle");
                if (!File.Exists(haloRadarHUD))
                    return;
                radarBundle = AssetBundle.LoadFromFile(haloRadarHUD);
                if (radarBundle == null)
                    return;
                RadarhudPrefab = radarBundle.LoadAsset("Assets/Examples/Halo Reach/Hud/RadarHUD.prefab");
                RadarBliphudPrefab = radarBundle.LoadAsset("Assets/Examples/Halo Reach/Hud/RadarBlipHUD.prefab");

                EnemyBlip = radarBundle.LoadAsset<Sprite>("EnemyBlip");
                EnemyBlipUp = radarBundle.LoadAsset<Sprite>("EnemyBlipUp");
                EnemyBlipDown = radarBundle.LoadAsset<Sprite>("EnemyBlipDown");
                EnemyBlipDead = radarBundle.LoadAsset<Sprite>("EnemyBlipDead");
            }
        }
        private void Update()
        {
            if (MapLoaded())
            {
                gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld.MainPlayer == null)
                {
                    return;
                }

                if (player == null)
                {
                    player = gameWorld.MainPlayer;
                }

                if (playerCamera == null)
                {
                    playerCamera = GameObject.Find("FPS Camera");
                    if (playerCamera == null)
                    {
                        return;
                    }
                }

                if (radarHud == null)
                {
                    var radarHudBase = Instantiate(RadarhudPrefab, playerCamera.transform.position, playerCamera.transform.rotation);
                    radarHud = radarHudBase as GameObject;
                    radarHud.transform.parent = playerCamera.transform;
                    radarHudBasePosition = radarHud.transform.Find("Radar") as RectTransform;
                    radarHudBlipBasePosition = radarHud.transform.Find("Radar/RadarBorder") as RectTransform;
                    radarHudBlipBasePosition.SetAsLastSibling();
                    radarHudPulse = radarHud.transform.Find("Radar/RadarPulse") as RectTransform;
                    radarScaleStart = radarHudBasePosition.localScale;
                    radarPositionYStart = radarHudBasePosition.position.y;
                    radarPositionXStart = radarHudBasePosition.position.x;
                    radarHudBasePosition.position = new Vector2(radarPositionXStart + Radar.radarOffsetXConfig.Value, radarPositionYStart + Radar.radarOffsetYConfig.Value);
                    radarHudBasePosition.localScale = new Vector2(radarScaleStart.x * Radar.radarSizeConfig.Value, radarScaleStart.y * Radar.radarSizeConfig.Value);

                    radarHudBlipBasePosition.GetComponent<Image>().color = Radar.backgroundColor.Value;
                    radarHudPulse.GetComponent<Image>().color = Radar.backgroundColor.Value;
                    radarHud.transform.Find("Radar/RadarBackground").GetComponent<Image>().color = Radar.backgroundColor.Value;

                    radarHud.SetActive(true);
                }

                radarHudBasePosition.position = new Vector2(radarPositionXStart + Radar.radarOffsetXConfig.Value, radarPositionYStart + Radar.radarOffsetYConfig.Value);
                radarHudBasePosition.localScale = new Vector2(radarScaleStart.x * Radar.radarSizeConfig.Value, radarScaleStart.y * Radar.radarSizeConfig.Value);
                radarHudBlipBasePosition.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, playerCamera.transform.eulerAngles.y);
                
                UpdateLoot();
                long rslt = UpdateActivePlayer();
                UpdateRadar(rslt != -1);

                if (radarInterval != Radar.radarScanInterval.Value)
                {
                    radarInterval = Radar.radarScanInterval.Value;
                    if (Radar.radarEnablePulseConfig.Value)
                    {
                        StartPulseAnimation();
                    }
                }
            }
        }

        public void Destory()
        {
            if (radarHud != null)
            {
                Destroy(radarHud);
            }
        }

        private void StartPulseAnimation()
        {
            // Stop any previous pulse coroutine
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            // Start the pulse coroutine
            pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        private IEnumerator PulseCoroutine()
        {
            float interval = Radar.radarScanInterval.Value;
            if (interval < 1)
            {
                interval = 1;
            }
            while (true)
            {
                // Rotate from 360 to 0 over the animation duration
                float t = 0f;
                while (t < 1.0f)
                {
                    t += Time.deltaTime / interval;
                    float angle = Mathf.Lerp(0f, 1f, 1 - t) * 360;

                    // Apply the scale to all axes
                    radarHudPulse.localEulerAngles = new Vector3(0, 0, angle);
                    yield return null;
                }
                // Pause for the specified duration
                // yield return new WaitForSeconds(interval);
            }
        }

        private long UpdateActivePlayer()
        {
            if (Time.time - radarLastUpdateTime < Radar.radarScanInterval.Value)
            {
                return -1;
            }
            else
            {
                radarLastUpdateTime = Time.time;
            }
            IEnumerable<Player> allPlayers = gameWorld.AllPlayersEverExisted;

            if (allPlayers.Count() == enemyList.Count + 1)
            {
                return -2;
            }

            foreach (Player enemyPlayer in allPlayers)
            {
                if (enemyPlayer == null || enemyPlayer == player)
                {
                    continue;
                }
                if (!enemyList.Contains(enemyPlayer.Id))
                {
                    enemyList.Add(enemyPlayer.Id);
                    enemyCustomObject.Add(new BlipPlayer(enemyPlayer));
                }
            }
            return 0;
        }

        private void UpdateLoot()
        {
            if (Time.time - radarLastUpdateTime < Radar.radarScanInterval.Value)
            {
                return;
            }

            if (!Radar.radarEnableLootConfig.Value)
            {
                if (lootList.Count > 0)
                {
                    lootList.Clear();
                    foreach (var loot in lootCustomObject)
                    {
                        loot.DestoryLoot();
                    }
                    lootCustomObject.Clear();
                }
                return;
            }

            HashSet<string> checkedLoot = new HashSet<string>();
            var allLoot = gameWorld.LootItems;
            foreach (LootItem loot in allLoot.GetValuesEnumerator())
            {
                checkedLoot.Add(loot.ItemId);
                if (!lootList.Contains(loot.ItemId))
                {
                    lootList.Add(loot.ItemId);
                    lootCustomObject.Add(new BlipLoot(loot));
                }
            }

            foreach (var item in lootCustomObject.Where(item => !checkedLoot.Contains(item.itemId)).ToList()) // ToList creates a copy to avoid modification during enumeration
            {
                item.DestoryLoot();
                lootList.Remove(item.itemId);
            }

            lootCustomObject.RemoveAll(item => !checkedLoot.Contains(item.itemId));
        }

        private void UpdateRadar(bool positionUpdate = true)
        {
            Target.setPlayerPosition(player.Transform.position);
            Target.setRadarRange(Radar.radarRangeConfig.Value);
            foreach (var obj in enemyCustomObject)
            {
                obj.Update(positionUpdate);
            }
            foreach (var obj in lootCustomObject)
            {
                obj.Update(positionUpdate);
            }
        }
    }
}