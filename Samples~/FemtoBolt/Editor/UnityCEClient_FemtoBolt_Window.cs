using nl.hku.axrlab;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class UnityCEClient_FemtoBolt_Window : EditorWindow
{
    private const string PACKAGE_NAME = "Azure Kinect and Femto Bolt Examples for Unity"; // Update for informational purposes

    // For package manager dependencies
    private bool _installingDependencies;

    // For downloadable dependencies
    private static string _downloadUrl = "";    // Insert URL here for downloadable .unityPackage, if it's a Package Manager package, see the other class and add it there
    private static string _manualUrl = "https://assetstore.unity.com/packages/tools/integration/azure-kinect-and-femto-bolt-examples-for-unity-149700";    // Insert URL here for manual download (in case of failure)
    private static string _manualUrlVersion = "1.21.1";
    private const string ImportDoneKey = "hku.axrlab.UnityCEClient-FemtoBolt-external"; // Update this name to match the one in the Loader class + "-external" (or any similar post-fix)
    private string _errorMessage = null;
    private bool _importDone => EditorPrefs.GetBool(ImportDoneKey, false);

    public static void ShowWindow(bool installingDependencies)
    {
        var window = GetWindow<UnityCEClient_FemtoBolt_Window>(true, "FemtoBolt Sample Setup", true);
        window._installingDependencies = installingDependencies;
        window.minSize = window.maxSize = new Vector2(480, 280);

        var main = EditorGUIUtility.GetMainWindowPosition();
        window.position = new Rect(
            main.x + (main.width - 480) / 2f,
            main.y + (main.height - 280) / 2f,
            480, 280
        );

        if (!NamespaceExists("com.rfilkov.kinect"))
        {
            window._errorMessage = "Required com.rfilkov.kinect namespace not found, please follow above install instructions.";
        }
        else
        {
            EditorPrefs.SetBool(ImportDoneKey, true);
            SessionState.SetBool(UnityCEClient_FemtoBolt_Loader.ShownKey, true);
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(16);

        if (_installingDependencies)
        {
            EditorGUILayout.HelpBox(
                "Required dependencies are being checked and may be added to your manifest. " +
                "Unity will recompile if anything is added and installation is complete." +
                "You may close this window when Unity stops doing things for several seconds.",
                MessageType.Info
            );

            GUILayout.Space(8);
        }

        // Add additional stuff here
        if (_importDone)
        {
            // Done state
            EditorGUILayout.HelpBox($"{PACKAGE_NAME} library has been detected." +
                                    $"You can close this window.", MessageType.None);

            DefineSymbolsUtility.AddDefineSymbol("FEMTO_SAMPLE_IMPORTED");
        }
        else
        {
            // Not yet done
            EditorGUILayout.HelpBox(
                $"This sample requires the {PACKAGE_NAME} package to be downloaded & installed." +
                $"You can either purchase or (for students) request free access here : {_manualUrl}",
                MessageType.Info
            );

            GUILayout.Space(8);

            if (_errorMessage != null)
            {
                GUILayout.Space(4);
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
            }
        }

        GUILayout.FlexibleSpace();
    }

    private static bool NamespaceExists(string desiredNamespace)
    {
        foreach (Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (System.Type type in assembly.GetTypes())
            {
                if (type.Namespace == desiredNamespace)
                    return true;
            }
        }
        return false;
    }
}