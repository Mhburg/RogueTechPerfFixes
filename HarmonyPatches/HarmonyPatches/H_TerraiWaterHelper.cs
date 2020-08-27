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
                return false;
            }
        }

        public static void UpdateWaterHeightNew(this MapTerrainDataCell cell, bool hasWater, int level = 0)
        {
            if (Core.Settings.fixWaterHeight && level <= TerraiWaterHelper.MAX_LEVEL)
            {
                if (!((level == 0) ? (hasWater = cell.HasWater()) : hasWater))
                {
                    return;
                }
                MapTerrainDataCellEx mapTerrainDataCellEx = cell as MapTerrainDataCellEx;
                if (mapTerrainDataCellEx != null && !mapTerrainDataCellEx.waterLevelCached)
                {
                    mapTerrainDataCellEx.UpdateWaterHeightRayNew();
                    int x = mapTerrainDataCellEx.x;
                    int y = mapTerrainDataCellEx.y;
                    int num = mapTerrainDataCellEx.mapMetaData.mapTerrainDataCells.GetLength(0) - 1;
                    int num2 = mapTerrainDataCellEx.mapMetaData.mapTerrainDataCells.GetLength(1) - 1;
                    if (x > 0 && y > 0)
                    {
                        mapTerrainDataCellEx.mapMetaData.mapTerrainDataCells[x - 1, y - 1].UpdateWaterHeightNew(hasWater, level + 1);
                    }
                    if (x > 0)
                    {
                        mapTerrainDataCellEx.mapMetaData.mapTerrainDataCells[x - 1, y].UpdateWaterHeightNew(hasWater, level + 1);
                    }
                    if (x < num && y < num2)
                    {
                        mapTerrainDataCellEx.mapMetaData.mapTerrainDataCells[x + 1, y + 1].UpdateWaterHeightNew(hasWater, level + 1);
                    }
                    if (y < num2)
                    {
                        mapTerrainDataCellEx.mapMetaData.mapTerrainDataCells[x, y + 1].UpdateWaterHeightNew(hasWater, level + 1);
                    }
                    if (x < num)
                    {
                        mapTerrainDataCellEx.mapMetaData.mapTerrainDataCells[x + 1, y].UpdateWaterHeightNew(hasWater, level + 1);
                    }
                    if (x < num && y > 0)
                    {
                        mapTerrainDataCellEx.mapMetaData.mapTerrainDataCells[x + 1, y - 1].UpdateWaterHeightNew(hasWater, level + 1);
                    }
                    if (x > 0 && y < num2)
                    {
                        mapTerrainDataCellEx.mapMetaData.mapTerrainDataCells[x - 1, y + 1].UpdateWaterHeightNew(hasWater, level + 1);
                    }
                }
            }
        }

        private static void UpdateWaterHeightRayNew(this MapTerrainDataCellEx ecell)
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
                if (float.IsNaN(num) || raycastHit.point.y > num)
                {
                    RTPFLogger.Debug?.Write(string.Concat(new object[]
                    {
                        "hit pos:",
                        raycastHit.point,
                        " ",
                        raycastHit.collider.gameObject.name,
                        " layer:",
                        LayerMask.LayerToName(raycastHit.collider.gameObject.layer),
                        "\n"
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
                        ecell.terrainHeight,
                        "\n"
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
            Vector3 pos = __instance.CurrentPosition;
            if (UnityGameInstance.BattleTechGame.Combat.MapMetaData.GetCellAt(pos) is MapTerrainDataCellEx ex)
            {
                ex.UpdateWaterHeightNew(false);
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
                    ex.UpdateWaterHeightNew(false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(CombatGameState))]
    [HarmonyPatch("_Init")]
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
                    cells[index, i].UpdateWaterHeightNew(false);
                }
            });

            stopwatch.Stop();
            RTPFLogger.Error?.Write($"Total time in building water grid: {stopwatch.Elapsed.TotalMilliseconds: 0000.0000}ms");
        }
    }
}
