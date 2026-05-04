using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UIDocumentDesignSystem.BuildTools
{
    // Unity batchmode entry points for the showcase build orchestrator.
    // Paired with Tools/Build/Build-Showcase.ps1 (Windows). Every method:
    //   1. Reads CLI flags from Environment.GetCommandLineArgs.
    //   2. Performs the build / action.
    //   3. Writes a JSON report to -cliReportPath so the orchestrator can
    //      validate success without scraping the log.
    //   4. Calls EditorApplication.Exit(0 on success / 1 on failure).
    public static class BuildCli
    {
        const string SCENE_PATH = "Assets/Showcase/Showcase.unity";

        // -executeMethod UIDocumentDesignSystem.BuildTools.BuildCli.BuildWebGL
        public static void BuildWebGL()
        {
            var report = new BuildReportData();
            try
            {
                var args = ParseArgs();
                string buildDir   = args.Get("-cliBuildPath", "build/WebGL");
                string reportPath = args.Get("-cliReportPath", "Tools/Build/output/report-BuildWebGL.json");

                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                report.reportPath = reportPath;

                // Defensive — even though ProjectSettings already sets these,
                // a contributor editing the asset by hand could regress. Re-
                // assert at build time so the orchestrator output is stable
                // regardless of how the asset got modified.
                PlayerSettings.WebGL.compressionFormat   = WebGLCompressionFormat.Brotli;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.template            = "PROJECT:ShowcaseTemplate";

                var opts = new BuildPlayerOptions
                {
                    scenes           = new[] { SCENE_PATH },
                    locationPathName = buildDir,
                    target           = BuildTarget.WebGL,
                    targetGroup      = BuildTargetGroup.WebGL,
                    options          = BuildOptions.None,
                };

                Debug.Log($"[BuildCli] Building WebGL → {buildDir}");
                BuildReport result = BuildPipeline.BuildPlayer(opts);

                report.success     = result.summary.result == BuildResult.Succeeded;
                report.message     = result.summary.result.ToString();
                report.sizeBytes   = (long)result.summary.totalSize;
                report.durationSec = (float)result.summary.totalTime.TotalSeconds;
                report.indexPath   = Path.Combine(buildDir, "index.html");

                if (!report.success)
                {
                    foreach (var step in result.steps)
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            report.message += "\n  " + msg.content;
                    }
                }
            }
            catch (Exception ex)
            {
                report.success = false;
                report.message = "Exception: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace;
            }
            finally
            {
                WriteReport(report);
                EditorApplication.Exit(report.success ? 0 : 1);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        [Serializable]
        struct BuildReportData
        {
            public bool   success;
            public string message;
            public long   sizeBytes;
            public float  durationSec;
            public string indexPath;
            public string reportPath; // not serialized to disk, just held internally
        }

        static void WriteReport(BuildReportData data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.reportPath)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(data.reportPath));
                // Keep the file shape stable so PowerShell ConvertFrom-Json
                // can read every field even when the build aborted early.
                var json = $"{{\n" +
                           $"  \"success\": {(data.success ? "true" : "false")},\n" +
                           $"  \"message\": \"{Escape(data.message)}\",\n" +
                           $"  \"sizeBytes\": {data.sizeBytes},\n" +
                           $"  \"durationSec\": {data.durationSec.ToString(System.Globalization.CultureInfo.InvariantCulture)},\n" +
                           $"  \"indexPath\": \"{Escape(data.indexPath)}\"\n" +
                           $"}}\n";
                File.WriteAllText(data.reportPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[BuildCli] Failed to write report: " + ex.Message);
            }
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }

        class CliArgs
        {
            readonly string[] _argv;
            public CliArgs(string[] argv) { _argv = argv; }
            public string Get(string name, string fallback)
            {
                for (int i = 0; i < _argv.Length - 1; i++)
                    if (_argv[i] == name) return _argv[i + 1];
                return fallback;
            }
        }

        static CliArgs ParseArgs() => new CliArgs(Environment.GetCommandLineArgs());
    }
}
