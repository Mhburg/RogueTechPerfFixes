using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleTech;
using CustAmmoCategories;
using CustomUnits;
using Harmony;
using UnityEngine;

namespace RogueTechPerfFixes.HarmonyPatches
{
    using Building = BattleTech.Building;

    public static class H_TerraiWaterHelper
    {
        [HarmonyPatch(typeof(TerraiWaterHelper), nameof(TerraiWaterHelper.UpdateWaterHeight))]
        [HarmonyPatch(new[] { typeof(MapTerrainDataCell), typeof(int) })]
        public static class UpdateWaterHeight
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.CustomUnit;
            }

            public static bool Prefix(MapTerrainDataCell cell)
            {
                if (cell is MapTerrainDataCellEx eCell)
                {
                    return !eCell.waterLevelCached;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(TerraiWaterHelper), nameof(TerraiWaterHelper.UpdateWaterHeightRay))]
        public static class UpdateWaterHeightRay
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.CustomUnit;
            }

            public static bool Prefix(MapTerrainDataCellEx ecell)
            {
                ecell.UpdateWaterHeightRayNew();
                return false;
            }
        }

        public static void UpdateWaterHeightRayNew(this MapTerrainDataCellEx ecell)
        {
            Vector3 vector = ecell.WorldPos();
            Ray ray = new Ray(new Vector3(vector.x, 1000f, vector.z), Vector3.down);
            int layerMask = 1 << LayerMask.NameToLayer("Water");
            RaycastHit[] array = Physics.RaycastAll(ray, 2000f, layerMask, QueryTriggerInteraction.Collide);
            RTPFLogger.Debug?.Write("UpdateWaterHeightRay:" + vector + "\n");
            RTPFLogger.Debug?.Write("hits count:" + array.Length + "\n");
            float num = float.NaN;
            foreach (RaycastHit raycastHit in array)
            {
                if (float.IsNaN(num))
                    num = raycastHit.point.y;

                if (raycastHit.point.y > num)
                {
                    RTPFLogger.Error?.Write(string.Concat(new object[]
                    {
                        "hit pos:",
                        raycastHit.point,
                        " ",
                        raycastHit.collider.gameObject.name,
                        " layer:",
                        LayerMask.LayerToName(raycastHit.collider.gameObject.layer)
                    }));
                    num = raycastHit.point.y;
                }
            }
            if (!float.IsNaN(num))
            {
                num -= Core.Settings.waterFlatDepth;
                if (Mathf.Abs(ecell.terrainHeight - num) > Core.Settings.waterFlatDepth)
                {
                    ecell.realTerrainHeight = ecell.terrainHeight;
                    ecell.terrainHeight = num;
                    ecell.cachedHeight = num;
                    RTPFLogger.Debug?.Write(string.Concat(new object[]
                    {
                        "terrain height:",
                        ecell.realTerrainHeight,
                        " water surface height:",
                        ecell.terrainHeight
                    }));
                }
                ecell.waterLevelCached = true;
                if (ecell.cachedSteepness > Core.Settings.maxWaterSteepness)
                {
                    RTPFLogger.Debug?.Write("steppiness too high faltering:" + ecell.cachedSteepness + "\n");
                    if (ecell.cachedSteepness > Core.Settings.deepWaterSteepness)
                    {
                        RTPFLogger.Debug?.Write("steppiness too high mark as deep water:" + ecell.cachedSteepness + "\n");
                        ecell.AddTerrainMask(TerrainMaskFlags.DeepWater);
                    }
                    ecell.cachedSteepness = Core.Settings.maxWaterSteepness;
                    ecell.terrainSteepness = Core.Settings.maxWaterSteepness;
                }
                if (Mathf.Abs(ecell.realTerrainHeight - num) > Core.Settings.deepWaterDepth)
                {
                    RTPFLogger.Debug?.Write("real depth too high tie to deep water:" + Mathf.Abs(ecell.realTerrainHeight - num) + "\n");
                    ecell.AddTerrainMask(TerrainMaskFlags.DeepWater);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Building), "OnActorDestroyed")]
    public static class H_Building_OnActorDestroyed
    {
        public static bool Prepare()
        {
            return Mod.Settings.Patch.CustomUnit;
        }

        public static void Postfix(Building __instance)
        {
            List<MapEncounterLayerDataCell> cells = ObstructionGameLogic.GetObstructionFromBuilding(
                    __instance
                    , UnityGameInstance.BattleTechGame.Combat.ItemRegistry)
                .occupiedCells;

            foreach (var cell in cells)
            {
                if (cell.relatedTerrainCell is MapTerrainDataCellEx ex)
                {
                    ex.UpdateWaterHeight();
                }
            }
        }
    }

    [HarmonyPatch(typeof(DropshipGameLogic), "ShowDropshipBasedOnAnimationState")]
    public static class H_DropshipGameLogic
    {
        public static bool Prepare()
        {
            return Mod.Settings.Patch.CustomUnit;
        }

        public static void Postfix(DropshipGameLogic __instance)
        {
            if (!__instance.occupiedCells.Any())
                return;

            foreach (MapEncounterLayerDataCell cell in __instance.occupiedCells)
            {
                if (cell.relatedTerrainCell is MapTerrainDataCellEx ex)
                {
                    ex.UpdateWaterHeight();
                }
            }
        }
    }

    //[HarmonyPatch(typeof(CombatGameState))]
    //[HarmonyPatch("_Init")]
    public static class H_CombatGameState_Init
    {
        public static bool Prepare()
        {
            return Mod.Settings.Patch.CustomUnit;
        }

        public static void Postfix()
        {
            MapTerrainDataCell[,] cells = UnityGameInstance.BattleTechGame.Combat.MapMetaData.mapTerrainDataCells;
            int zIndex = cells.GetLength(0);
            int xIndex = cells.GetLength(1);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            stopwatch.Stop();

            stopwatch.Restart();

            Parallel.For(0, zIndex, (index) =>
            {
                for (int i = 0; i < xIndex; i++)
                {
                    cells[index, i].UpdateWaterHeight();
                }
            });

            stopwatch.Stop();
            RTPFLogger.Error?.Write($"Total time in building water grid: {stopwatch.Elapsed.TotalMilliseconds: 0000.0000}ms");
        }
    }
}
