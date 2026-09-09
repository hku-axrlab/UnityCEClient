using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace nl.hku.axrlab
{
    public static class DefineSymbolsUtility
    {
        /// <summary>
        /// Adds a scripting define symbol for the current build target group.
        /// </summary>
        public static void AddDefineSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                Debug.LogError("Define symbol cannot be null or empty.");
                return;
            }

            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            // Get current defines
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);

            // Avoid duplicates
            if (!defines.Split(';').Contains(symbol))
            {
                defines = string.IsNullOrEmpty(defines) ? symbol : $"{defines};{symbol}";
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, defines);
                Debug.Log($"Added define symbol: {symbol}");
            }
            else
            {
                // Debug.Log($"Define symbol '{symbol}' already exists.");
            }
        }

        /// <summary>
        /// Removes a scripting define symbol for the current build target group.
        /// </summary>
        public static void RemoveDefineSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                Debug.LogError("Define symbol cannot be null or empty.");
                return;
            }

            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var definesList = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)
                                            .Split(';')
                                            .Where(d => d != symbol)
                                            .ToArray();

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, string.Join(";", definesList));
            Debug.Log($"Removed define symbol: {symbol}");
        }
    }
}