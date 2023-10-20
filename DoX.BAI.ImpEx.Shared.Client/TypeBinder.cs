using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace DoX.BAI.ImpEx.Shared
{
    public static class TypeBinder
    {

        public static Type GetRemotingTypeByInterface(Type interfaceType)
        {
            return GetTypeByInterfaceInternal(interfaceType, true);
        }

        public static Type GetTypeByInterface(Type interfaceType)
        {
            return GetTypeByInterfaceInternal(interfaceType, false);
        }

        private static Type GetTypeByInterfaceInternal(Type interfaceType, Boolean remotingType)
        {
            Type t = null;
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            List<Assembly> assemblies = new List<Assembly>();

            foreach (FileInfo fi in di.GetFiles("*.exe"))
            {
                if (!fi.Name.Contains(".vshost.exe"))
                {
                    try
                    {
                        assemblies.Add(Assembly.LoadFrom(fi.FullName));
                        Debug.WriteLine("Assembly hinzugefügt: " + fi.FullName);
                    }
                    catch (Exception) { }
                }
            }

            foreach (FileInfo fi in di.GetFiles("*.dll"))
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(fi.FullName));
                    Debug.WriteLine("Assembly hinzugefügt: " + fi.FullName);
                }
                catch (Exception) { }                
            }

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type includedType in assembly.GetExportedTypes())
                {
                    // --- 1. Klasse suchen, welche die Schnittstelle implementiert

                    if (includedType.IsClass && (includedType.GetInterface(interfaceType.Name) != null))
                    {
                        if (remotingType)
                        {
                            if (includedType.IsSubclassOf(typeof(MarshalByRefObject)))
                                t = includedType;
                        }
                        else
                        {
                            if (!includedType.IsSubclassOf(typeof(MarshalByRefObject)))
                                t = includedType;
                        }

                        if (t != null)
                        {
                            Debug.WriteLine("Schnittstelle " + interfaceType.Name + " in Assembly " + assembly.FullName + " gefunden");
                            break;
                        }
                    }
                }
                if (t != null) break;
            }

            return t;
        }
    }
}
