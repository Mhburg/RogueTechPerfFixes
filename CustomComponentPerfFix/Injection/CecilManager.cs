using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
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

        public const string VanillaAssemblyName = "Assembly-CSharp.dll";

        public const string LocalVanillaAssemblyPath = @".\" + VanillaAssemblyName;

        public const string BackUpAssemblyName = "Assembly-CSharp.dll.PerfFix.orig";

        public const string CecilLog = @".\CecilLog.txt";

        public static string VanillaAssemblyDir { get; set; }

        public static string VanillaAssemblyFullPath { get; set; }

        public static List<IInjector> Injectors { get; } = new List<IInjector>();

        public static bool HasError { get; private set; }

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
                    if (assemblyName.Name == CecilManager.VanillaAssemblyName)
                    {
                        File.AppendAllText(CecilLog, "Loading: " + eventArgs.Name + "\n");
                        return Assembly.LoadFrom(CecilManager.LocalVanillaAssemblyPath);
                    }

                    string assemblyPath =
                        Path.Combine(VanillaAssemblyDir, assemblyName.Name + ".dll");
                    if (!File.Exists(assemblyPath))
                    {
                        File.AppendAllText(CecilLog, $"Can't find {assemblyPath}\n");
                        return Assembly.GetExecutingAssembly();
                    }

                    File.AppendAllText(CecilLog, "Loading: " + eventArgs.Name + "\n");
                    return Assembly.LoadFrom(assemblyPath);
                }
                catch (Exception e)
                {
                    File.AppendAllText(CecilLog, e + "\n");
                }

                File.AppendAllText(CecilLog, "Can't resolve assembly reference\n");
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

                    string bak = Path.Combine(VanillaAssemblyDir, BackUpAssemblyName);

                    // Restore the original dll if a backup exists.
                    if (File.Exists(bak))
                    {
                        File.Delete(VanillaAssemblyFullPath);
                        File.Move(bak, VanillaAssemblyFullPath);
                    }

                    // Make a backup for the game assembly
                    File.Copy(VanillaAssemblyFullPath, bak, true);

                    // Make a working copy in local
                    File.Copy(VanillaAssemblyFullPath, LocalVanillaAssemblyPath, true);

                    _assembly = AssemblyDefinition.ReadAssembly(bak, parameters);
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
                            if (type.Fields.Any(f => f.Name == RTPFVersion.Name))
                            {
                                _initialized = false;
                                File.AppendAllText(CecilLog, $"Already injected with version {RTPFVersion.Name}\n");
                                _assembly.Dispose();
                                File.Delete(bak);
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
                    HasError = true;
                    File.AppendAllText(CecilLog, e.ToString());
                }
                return;
            }

            HasError = true;
            File.AppendAllText(CecilLog, $"Incorrect file path: {VanillaAssemblyPath} to Assembly-CSharp.dll\n");
        }

        public static void Init()
        {
            if (!_initialized)
                return;

            File.AppendAllText(CecilLog, $"Start injecting...\n");

            try
            {
                Injectors.Add(new I_DesiredAuraReceptionState());
                Injectors.Add(new I_BTLight());
                Injectors.Add(new I_BTLightController());
                //Injectors.Add(new I_SortMoveCandidatesByInfMapNode());

                foreach (IInjector injector in Injectors)
                    injector.Inject(TypeTable, _assembly.MainModule);

                if (File.Exists(VanillaAssemblyFullPath))
                    File.Delete(VanillaAssemblyFullPath);

                _assembly.Write(VanillaAssemblyFullPath);
                _assembly.Dispose();
                File.AppendAllText(CecilLog, "All good here.");
            }
            catch (Exception e)
            {
                HasError = true;
                File.AppendAllText(CecilLog, e.ToString());
            }
        }
    }
}
