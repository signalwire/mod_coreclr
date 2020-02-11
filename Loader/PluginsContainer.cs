﻿using PluginInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LoaderModule
{
    public static class PluginsContainer
    {
        public static List<PluginLoadContext> pluginLoadContexts = new List<PluginLoadContext>();


        public static bool LoadPlugin(string pluginPath)
        {
            var pluginLocation = Path.GetFullPath(pluginPath.Replace('\\', Path.DirectorySeparatorChar));
            var loadContext = new PluginLoadContext(pluginLocation);
            var aname = new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation));
            var loadedAssembly = loadContext.LoadFromAssemblyName(aname);
            if (loadedAssembly != null)
            {
                BuildDispatchers(loadedAssembly, loadContext);
            }
            if (loadContext.Dispatchers.Count == 0)
            {
                return false;
            }
            pluginLoadContexts.Add(loadContext);
            Console.WriteLine($"LoadedPlugin - {pluginLocation}");
            Console.WriteLine($"APIs: {string.Join(',', loadContext.Dispatchers.SelectMany(d => d.GetApiNames()))}");
            Console.WriteLine($"DPApps: {string.Join(',', loadContext.Dispatchers.SelectMany(d => d.GetDPNames()))}");
            return true;
        }

        public static void LoadPluginsFromSubDirs(string path)
        {
            var dirs = Directory.GetDirectories(path);
            foreach (var d in dirs)
            {
                foreach (var f in Directory.GetFiles(d))
                {
                    var filename = Path.GetFileName(f);
                    if (filename == Path.GetFileName(d) + ".dll")
                    {
                        LoadPlugin(f);
                    }
                }
            }
        }

        public static bool DispatchAPI(string cmd)
        {
            var args = cmd.Trim().Split(" ".ToCharArray());
            var dispatcher = pluginLoadContexts.SelectMany(c => c.Dispatchers).FirstOrDefault(d => d.GetApiNames().Contains(args[0]));
            if (dispatcher == null)
            {
                return false;
            }
            dispatcher.DispatchAPI();
            return true;
        }

        public static bool DispatchDialPlanApp(string appArgs)
        {
            var args = appArgs.Trim().Split(" ".ToCharArray());
            var dispatcher = pluginLoadContexts.SelectMany(c => c.Dispatchers).FirstOrDefault(d => d.GetDPNames().Contains(args[0]));
            if (dispatcher == null)
            {
                return false;
            }
            dispatcher.DispatchDialPlan();
            return true;
        }

        private static void BuildDispatchers(Assembly assembly, PluginLoadContext context)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPluginDispatcher).IsAssignableFrom(type))
                {
                    IPluginDispatcher result = Activator.CreateInstance(type) as IPluginDispatcher;
                    if (result != null)
                    {
                        context.Dispatchers.Add(result);
                    }
                }
            }
        }

    }
}