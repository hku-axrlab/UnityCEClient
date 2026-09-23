using System.IO;
using UnityEditor;
using UnityEngine;
using System.Net;
using System.Threading.Tasks;

public class UnityCEClient_Optitrack_Window : EditorWindow
{
	private const string PACKAGE_NAME = "Optitrack"; // Update for informational purposes

	// For package manager dependencies
	private bool _installingDependencies;

	// For downloadable dependencies
	private static string _downloadUrl = "https://d2mzlempwep3hb.cloudfront.net/Plugins/Unity/OptiTrack_Unity_Plugin_1.6.1.unitypackage";    // Insert URL here for downloadable .unityPackage, if it's a Package Manager package, see the other class and add it there
	private static string _manualUrl = "https://www.optitrack.com/support/downloads";    // Insert URL here for manual download (in case of failure)
	private static string _manualUrlVersion = "1.6.1";
	private bool _isDownloading = false;
	private const string ImportDoneKey = "hku.axrlab.UnityCEClient-Optitrack-external";	// Update this name to match the one in the Loader class + "-external" (or any similar post-fix)
	private string _errorMessage = null;
	private bool _importDone => EditorPrefs.GetBool(ImportDoneKey, false);

	public static void ShowWindow(bool installingDependencies)
	{
		var window = GetWindow<UnityCEClient_Optitrack_Window>(true, "Optitrack Sample Setup", true);
		window._installingDependencies = installingDependencies;
		window.minSize = window.maxSize = new Vector2(480, 280);

		var main = EditorGUIUtility.GetMainWindowPosition();
		window.position = new Rect(
			main.x + (main.width - 480) / 2f,
			main.y + (main.height - 280) / 2f,
			480, 280
		);
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
			EditorGUILayout.HelpBox($"{PACKAGE_NAME} library has been imported.", MessageType.None);
			if (GUILayout.Button("Re-import"))
				StartDownload();
		}
		else
		{
			// Not yet done
			EditorGUILayout.HelpBox(
				$"This sample required the {PACKAGE_NAME} package to be downloaded & installed." +
				"Please press the below button to start this process, and allow it to finish." +
				$"If this doesn't work, you can add it yourself from the following URL ({_manualUrlVersion}): {_manualUrl}",
				MessageType.Info
			);

			GUILayout.Space(8);

			EditorGUI.BeginDisabledGroup(_isDownloading);
			if (GUILayout.Button(_isDownloading ? "Downloading..." : $"Download & Import {PACKAGE_NAME}"))
				StartDownload();
			EditorGUI.EndDisabledGroup();

			if (_errorMessage != null)
			{
				GUILayout.Space(4);
				EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
			}
		}

		GUILayout.FlexibleSpace();
	}

	private void StartDownload()
	{
		_isDownloading = true;
		_errorMessage = null;
		Repaint();

		UnityCEClient_Optitrack_Window.DownloadAndImportAsync(success =>
		{
			_isDownloading = false;

			if (success)
			{
				EditorPrefs.SetBool(ImportDoneKey, true);
				_errorMessage = null;
			}
			else
			{
				_errorMessage = "Download failed. Check the Console for details, then try again.";
			}

			Repaint();
		});
	}

	public static async void DownloadAndImportAsync(System.Action<bool> onComplete = null)
	{
		var tempPath = Path.Combine(Path.GetTempPath(), $"{ImportDoneKey}.unitypackage");

		EditorUtility.DisplayProgressBar("Downloading Package", "Downloading...", -1f); // -1 = indeterminate

		bool success = await Task.Run(() =>
		{
			try
			{
				new WebClient().DownloadFile(_downloadUrl, tempPath);
				return true;
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[YourSample] Download failed: {e.Message}");
				return false;
			}
		});

		EditorUtility.ClearProgressBar();

		if (success)
			AssetDatabase.ImportPackage(tempPath, interactive: true);

		EditorApplication.delayCall += () =>
		{
			if (File.Exists(tempPath)) File.Delete(tempPath);
		};

		onComplete?.Invoke(success);
	}
}