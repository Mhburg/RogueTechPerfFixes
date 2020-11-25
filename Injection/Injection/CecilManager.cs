using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Injection.Injection;
using Mono.Cecil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace RogueTechPerfFixes.Injection
{
    public static class CecilManager
    {
        private static bool _initialized = false;

        private static AssemblyDefinition _assembly;

        public const string VanillaAssemblyPath = @"..\..\BattleTech_Data\Managed\Assembly-CSharp.dll";

        public const string VanillaAssemblyName = "Assembly-CSharp";

        private const string DOTween_NAME = "DOTween.dll";

        public const string BackUpAssemblyName = "Assembly-CSharp.dll.PerfFix.orig";

        public const string TempAssemblyName = VanillaAssemblyName + ".temp";

        public const string CecilLog = @".\CecilLog.txt";

        public static bool HasInjectionError { get; set; }

        public static string VanillaAssemblyDir { get; set; }

        public static string VanillaAssemblyFullPath { get; set; }

        public static string BackupAssemblyPath { get; set; }

        public static List<IInjector> Injectors { get; } = new List<IInjector>();

        public static Dictionary<string, TypeDefinition> TypeTable { get; } = new Dictionary<string, TypeDefinition>();

        static CecilManager()
        {
            VanillaAssemblyFullPath = Path.GetFullPath(VanillaAssemblyPath);
            VanillaAssemblyDir = Path.GetDirectoryName(VanillaAssemblyFullPath);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                try
                {
                    AssemblyName assemblyName = new AssemblyName(eventArgs.Name);
                    if (assemblyName.Name == VanillaAssemblyName)
                    {
                        WriteLog($"Loading {eventArgs.Name} from {BackupAssemblyPath}");
                        return Assembly.LoadFrom(BackupAssemblyPath);
                    }

                    string assemblyPath = Path.Combine(VanillaAssemblyDir, assemblyName.Name + ".dll");
                    if (!File.Exists(assemblyPath))
                    {
                        WriteLog($"Can't find {assemblyPath}\n");
                        return Assembly.GetExecutingAssembly();
                    }

                    WriteLog($"Loading {eventArgs.Name} from {assemblyPath}");
                    return Assembly.LoadFrom(assemblyPath);
                }
                catch (Exception e)
                {
                    WriteLog(e.ToString());
                }

                WriteLog("Can't resolve assembly reference");
                return null;
            };

            if (File.Exists(VanillaAssemblyPath))
            {
                if (File.Exists(CecilLog))
                    File.Delete(CecilLog);

                try
                {
                    ReaderParameters parameters = new ReaderParameters();
                    DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
                    resolver.AddSearchDirectory(VanillaAssemblyDir);
                    parameters.AssemblyResolver = resolver;
                    parameters.ReadWrite = true;

                    BackupAssemblyPath = Path.Combine(VanillaAssemblyDir, BackUpAssemblyName);
                    bool hasBackup = false;

                    // Restore the original dll if a backup exists.
                    if (File.Exists(BackupAssemblyPath))
                    {
                        hasBackup = true;
                        File.Delete(VanillaAssemblyFullPath);
                        File.Copy(BackupAssemblyPath, VanillaAssemblyFullPath, true);
                    }

                    // Make a backup for the game assembly
                    if (!hasBackup)
                        File.Copy(VanillaAssemblyFullPath, BackupAssemblyPath, true);

                    _assembly = AssemblyDefinition.ReadAssembly(VanillaAssemblyFullPath, parameters);
                    FieldDefinition RTPFVersion =
                        new FieldDefinition(
                            nameof(RTPFVersion) + Mod.Version.ToString().Replace('.', '_')
                            , FieldAttributes.Private
                            , _assembly.MainModule.ImportReference(typeof(string)));
                    TypeDefinition targetType = null;
                    foreach (TypeDefinition type in _assembly.MainModule.Types)
                    {
                        if (type.Name == nameof(UnityGameInstance))
                        {
                            targetType = type;
                            FieldDefinition field = type.Fields.FirstOrDefault(f => f.Name.StartsWith(nameof(RTPFVersion)));
                            if (field != null)
                            {
                                _initialized = false;

                                WriteLog($"Found assembly patched with version {field.Name}");
                                _assembly.Dispose();
                                return;
                            }
                        }

                        TypeTable.Add(type.FullName, type);
                    }

                    targetType.Fields.Add(RTPFVersion);
                    _initialized = true;
                }
                catch (Exception e)
                {
                    WriteLog(e.ToString());
                }

                return;
            }

            WriteLog($"Incorrect file path: {VanillaAssemblyPath} to Assembly-CSharp.dll");
        }

        public static void Init()
        {
            if (!_initialized)
                return;

            WriteLog($"Start injecting...");

            try
            {
                //Injectors.Add(new I_DesiredAuraReceptionState());

                Injectors.Add(new I_CombatAuraReticle());
                Injectors.Add(new I_BTLight());
                Injectors.Add(new I_BTLightController());

                // DataManager fixes
                Injectors.Add(new I_AmmunitionDefLoadRequest());

                //Injectors.Add(new I_DOTweenAnimation());
                //Injectors.Add(new I_ElementManager());
                //Injectors.Add(new I_SortMoveCandidatesByInfMapNode());

                foreach (IInjector injector in Injectors)
                {
                    injector.Inject(TypeTable, _assembly.MainModule);
                    WriteLog($"Injected {injector.GetType().Name}\n");
                }

                string tempFullPath = Path.Combine(VanillaAssemblyDir, TempAssemblyName);
                _assembly.Write();
                _assembly.Dispose();
                //File.Copy(tempFullPath, VanillaAssemblyFullPath, true);
                //File.Delete(tempFullPath);

                ReplaceDOTween();
                if (!HasInjectionError)
                    WriteLog("All good here.");
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
            }
        }

        public static bool RestoreVanillaAssembly()
        {
            // Restore the original dll if a backup exists.
            if (File.Exists(BackupAssemblyPath))
            {
                File.Delete(VanillaAssemblyFullPath);
                File.Copy(BackupAssemblyPath, VanillaAssemblyFullPath, true);
                return true;
            }

            return false;
        }

        public static void WriteLog(string message)
        {
            File.AppendAllText(CecilLog, $"[{DateTime.Now}] {message}\n");
        }

        public static void WriteError(string message)
        {
            HasInjectionError = true;
            File.AppendAllText(CecilLog, $"[{DateTime.Now}] {message}\n");
        }

        private static void ReplaceDOTween()
        {
            string newFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DOTween_NAME);
            string oldFile = Path.Combine(VanillaAssemblyDir, DOTween_NAME);

            AssemblyName oldName = AssemblyName.GetAssemblyName(oldFile);
            AssemblyName newName = AssemblyName.GetAssemblyName(newFile);
            if (oldName.Version == newName.Version)
            {
                WriteLog($"DOTween is up to date.");
                return;
            }

            if (!File.Exists(newFile))
            {
                WriteError($"Can't find {DOTween_NAME} at {newFile}.");
                return;
            }

            if (!File.Exists(oldFile))
            {
                WriteError($"Can't find {DOTween_NAME} at {oldFile}.");
                return;
            }

            try
            {
                if (oldName.Version == new Version("1.0.0.0"))
                    File.Copy(oldFile, oldFile + ".orig", true);

                File.Copy(newFile, oldFile, true);
                WriteLog($"DOTween updated to version {newName.Version}");
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
            }
        }
    }
}
