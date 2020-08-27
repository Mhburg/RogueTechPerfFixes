using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using UnityEngine;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(PathNodeGrid), nameof(PathNodeGrid.FindBlockerBetween))]
    public static class H_FindBlockerBetween
    {
        public static volatile int availableThreads = Mod.MaxConcurrency;
        private static Stopwatch _stopwatch = new Stopwatch();
        public static double Time = 0;
        public static long Counter = 0;
        public static double Slowest = 0;
        public static double Fastest = 0;

        private static void Prefix()
        {
            _stopwatch.Restart();
        }

        private static bool NotPrefix(
            ref bool __result, Vector3 from, Vector3 to, MapMetaData ___mapMetaData, CombatGameState ___combat, List<Point> ___fbbLine,
            float ___maxGrade, float ___cellDelta, float ___cellDeltaDiag, Point ___incrementZ, Point ___incrementX, Point ___decrementX,
            Point ___decrementZ)
        {
            _stopwatch.Start();
            Counter++;
            ___mapMetaData = ___combat.MapMetaData;
            Point index = ___mapMetaData.GetIndex(from);
            Point index2 = ___mapMetaData.GetIndex(to);
            if (!___mapMetaData.IsWithinBounds(index) || !___mapMetaData.IsWithinBounds(index))
            {
                return true;
            }
            ___fbbLine = BresenhamLineUtil.BresenhamLine(index, index2);
            if (___fbbLine.Count < 3)
            {
                return false;
            }

            float cachedHeight = ___mapMetaData.GetCellAt(___fbbLine[___fbbLine.Count - 1]).cachedHeight;
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                availableThreads = Mod.MaxConcurrency;
                Task<bool>[] tasks = new Task<bool>[3];
                CancellationToken token = cts.Token;

                for (int i = -1; i <= 1; i++)
                {
                    while (availableThreads <= 0)
                    {
                    }

                    Interlocked.Decrement(ref availableThreads);
                    int k = i;
                    tasks[i + 1] = Task.Run(() =>
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (token.IsCancellationRequested)
                                return false;

                            if (k == 0 && j == 0)
                            {
                                continue;
                            }

                            int num = ___fbbLine[___fbbLine.Count - 1].X + k;
                            int num2 = ___fbbLine[___fbbLine.Count - 1].Z + j;
                            if (___mapMetaData.IsWithinBounds(num, num2))
                            {
                                float cachedHeight2 = ___mapMetaData.GetCellAt(num, num2).cachedHeight;
                                if (PathNodeGrid.CheckForBlocker(cachedHeight, cachedHeight2, ___maxGrade,
                                    (k != 0 && j != 0) ? ___cellDeltaDiag : ___cellDelta))
                                {
                                    Interlocked.Increment(ref availableThreads);
                                    cts.Cancel();
                                    return true;
                                }
                            }
                        }

                        Interlocked.Increment(ref availableThreads);
                        return false;
                    });
                }

                if (token.IsCancellationRequested)
                {
                    __result = true;
                    PrintExceptions(tasks);
                    return false;
                }

                foreach (Task<bool> t in tasks)
                {
                    // busy wait
                    while (!t.IsCompleted)
                    {
                    }
                }

                PrintExceptions(tasks);
                if (token.IsCancellationRequested)
                {
                    __result = true;
                    return false;
                }
            }

            availableThreads = Mod.MaxConcurrency;

            float fromHeight = ___mapMetaData.GetCellAt(___fbbLine[0]).cachedHeight;

            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                CancellationToken token = cts.Token;
                Task<bool>[] tasks = new Task<bool>[___fbbLine.Count - 1];

                for (int k = 1; k < ___fbbLine.Count; k++)
                {
                    while (availableThreads <= 0)
                    {
                    }

                    Interlocked.Decrement(ref availableThreads);
                    int i = k - 1;
                    tasks[i] = Task.Run(() =>
                    {
                        cachedHeight = ___mapMetaData.GetCellAt(___fbbLine[i + 1]).cachedHeight;
                        if (___fbbLine[i + 1].Z == ___fbbLine[i].Z)
                        {
                            if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight, ___maxGrade, ___cellDelta))
                            {
                                cts.Cancel();
                                Interlocked.Increment(ref availableThreads);
                                return true;
                            }
                            float cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___incrementZ).cachedHeight;
                            if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                            {
                                cts.Cancel();
                                Interlocked.Increment(ref availableThreads);
                                return true;
                            }
                            cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___decrementZ).cachedHeight;
                            if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                            {
                                cts.Cancel();
                                Interlocked.Increment(ref availableThreads);
                                return true;
                            }
                        }
                        else if (___fbbLine[i + 1].X == ___fbbLine[i].X)
                        {
                            if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight, ___maxGrade, ___cellDelta))
                            {
                                cts.Cancel();
                                Interlocked.Increment(ref availableThreads);
                                return true;
                            }
                            float cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___decrementX).cachedHeight;
                            if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                            {
                                cts.Cancel();
                                Interlocked.Increment(ref availableThreads);
                                return true;
                            }
                            cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___incrementX).cachedHeight;
                            if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                            {
                                cts.Cancel();
                                Interlocked.Increment(ref availableThreads);
                                return true;
                            }
                        }
                        else
                        {
                            if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight, ___maxGrade, ___cellDeltaDiag))
                            {
                                cts.Cancel();
                                Interlocked.Increment(ref availableThreads);
                                return true;
                            }
                            if (___fbbLine[i + 1].X > ___fbbLine[i].X)
                            {
                                float cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___incrementX).cachedHeight;
                                if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                                {
                                    cts.Cancel();
                                    Interlocked.Increment(ref availableThreads);
                                    return true;
                                }
                                if (___fbbLine[i + 1].Z > ___fbbLine[i].Z)
                                {
                                    cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___incrementZ).cachedHeight;
                                    if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                                    {
                                        cts.Cancel();
                                        Interlocked.Increment(ref availableThreads);
                                        return true;
                                    }
                                }
                                else if (___fbbLine[i + 1].Z < ___fbbLine[i].Z)
                                {
                                    cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___decrementZ).cachedHeight;
                                    if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                                    {
                                        cts.Cancel();
                                        Interlocked.Increment(ref availableThreads);
                                        return true;
                                    }
                                }
                            }
                            else if (___fbbLine[i + 1].X < ___fbbLine[i].X)
                            {
                                float cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___decrementX).cachedHeight;
                                if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                                {
                                    cts.Cancel();
                                    Interlocked.Increment(ref availableThreads);
                                    return true;
                                }
                                if (___fbbLine[i + 1].Z > ___fbbLine[i].Z)
                                {
                                    cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___incrementZ).cachedHeight;
                                    if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                                    {
                                        cts.Cancel();
                                        Interlocked.Increment(ref availableThreads);
                                        return true;
                                    }
                                }
                                else if (___fbbLine[i + 1].Z < ___fbbLine[i].Z)
                                {
                                    cachedHeight2 = ___mapMetaData.GetCellAt(___fbbLine[i] + ___decrementZ).cachedHeight;
                                    if (PathNodeGrid.CheckForBlocker(fromHeight, cachedHeight2, ___maxGrade, ___cellDelta))
                                    {
                                        cts.Cancel();
                                        Interlocked.Increment(ref availableThreads);
                                        return true;
                                    }
                                }
                            }
                        }

                        fromHeight = cachedHeight;
                        Interlocked.Increment(ref availableThreads);
                        return false;
                    });

                    if (token.IsCancellationRequested)
                    {
                        PrintExceptions(tasks);
                        __result = true;
                        return false;
                    }
                }

                if (token.IsCancellationRequested)
                {
                    PrintExceptions(tasks);
                    __result = true;
                    return false;
                }

                foreach (Task<bool> t in tasks)
                {
                    // busy wait
                    while (!t.IsCompleted)
                    {
                        if (token.IsCancellationRequested)
                        {
                            PrintExceptions(tasks);
                            __result = true;
                            return false;
                        }
                    }
                }
                PrintExceptions(tasks);
            }

            __result = false;
            return false;

            void PrintExceptions(Task<bool>[] tasks)
            {
                List<Task<bool>> faultyTasks = tasks.Where(t => t?.IsFaulted ?? false).ToList();
                if (faultyTasks.Any())
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (Task task in faultyTasks)
                    {
                        stringBuilder.AppendLine(task.Exception.Flatten().ToString());
                    }

                    Utils.Logger.LogError($"{Utils.LOG_HEADER}" + stringBuilder);
                }
            }
        }

        private static void Postfix()
        {
            _stopwatch.Stop();
            double time = _stopwatch.Elapsed.TotalMilliseconds;
            Time += time;
            if (time > Slowest)
                Slowest = time;
            else if (time < Fastest)
                Fastest = time;
        }
    }
}
