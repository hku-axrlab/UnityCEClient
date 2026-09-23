using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

[InitializeOnLoad]
public class UnityCEClient_FemtoBolt_Loader
{
    // Update this key to be unique for the specific plugin / package we are supporting (Optitrack, HU-CaveCamera, etc.)
    public const string ShownKey = "hku.axrlab.UnityCEClient-FemtoBolt";

    static UnityCEClient_FemtoBolt_Loader()
    {
        //if (SessionState.GetBool(ShownKey, false))
        //	return;

        EditorApplication.delayCall += RunSetup;
    }

    private static void RunSetup()
    {
        bool manifestChanged = EnsureDependencies();

        if (manifestChanged)
        {
            // Trigger UPM to resolve the new dependencies before showing the window
            Client.Resolve();
        }

        UnityCEClient_FemtoBolt_Window.ShowWindow(manifestChanged);
    }

    private static bool EnsureDependencies()
    {
        var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        var manifest = File.ReadAllText(manifestPath);

        // Add things here to ensure the manifest is updated
        var dependencies = new[] {
            ("","")
        };

        bool changed = false;

        foreach (var (id, url) in dependencies)
        {
            if (string.IsNullOrEmpty(id)) continue;

            // Only add if not already present — avoid overwriting pinned versions
            if (!manifest.Contains($"\"{id}\""))
            {
                // Insert into the dependencies block
                manifest = manifest.Replace(
                    "\"dependencies\": {",
                    $"\"dependencies\": {{\n    \"{id}\": \"{url}\","
                );
                changed = true;
            }
        }

        if (changed)
            File.WriteAllText(manifestPath, manifest);

        return changed;
    }
}