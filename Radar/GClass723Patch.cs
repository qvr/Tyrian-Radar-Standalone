using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using UnityEngine;
using EFT.Interactive;

namespace Radar
{
    internal class GClass723PatchAdd : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass723<int, LootItem>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        static void PostFix(int key, LootItem value)
        {
            //Debug.LogError($"Added called with key {key} and value {value}");
            var radar = InRaidRadarManager._radarGo.GetComponent<HaloRadar>();
            radar.AddLoot(value, true, key);
        }
    }
    internal class GClass723PatchRemove : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass723<int, LootItem>).GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        static void PreFix(int key)
        {
            //Debug.LogError($"Remove Called with key {key}");
            var radar = InRaidRadarManager._radarGo.GetComponent<HaloRadar>();
            radar.RemoveLoot(key);
        }
    }
}
