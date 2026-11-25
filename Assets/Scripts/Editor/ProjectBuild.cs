using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SlotMachine.EditorTools
{
    /// <summary>
    /// One-click (or one-command) entry points:
    ///   Slot Machine > Rebuild Assets And Scene   regenerates every derived asset, prefab and the scene
    ///   Slot Machine > Build WebGL                 builds the game into /Build/WebGL
    /// Headless:
    ///   Unity -batchmode -projectPath . -executeMethod SlotMachine.EditorTools.ProjectBuild.RebuildAll -quit
    ///   Unity -batchmode -projectPath . -buildTarget WebGL -executeMethod SlotMachine.EditorTools.ProjectBuild.BuildWebGL -quit
    /// </summary>
    public static class ProjectBuild
    {
        private const string WebGLOutput = "Build/WebGL";

        [MenuItem("Slot Machine/Rebuild Assets And Scene")]
        public static void RebuildAll()
        {
            SlotAssetBuilder.BuildAll();
            SlotSceneBuilder.BuildAll();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("[SlotMachine] Assets, prefabs and scene rebuilt.");
        }

        [MenuItem("Slot Machine/Build WebGL")]
        public static void BuildWebGL() => Build(WebGLOutput, BuildOptions.None);

        /// <summary>
        /// Development build with the QA shortcuts (force a line, empty the
        /// wallet) for testing rare outcomes. Written outside the repo's
        /// release build folder and never committed.
        /// </summary>
        [MenuItem("Slot Machine/Build WebGL (QA development build)")]
        public static void BuildWebGLDevelopment() => Build("Builds/WebGL-QA", BuildOptions.Development);

        private static void Build(string output, BuildOptions buildOptions)
        {
            ConfigurePlayer();
            ConfigureCodeSize(release: (buildOptions & BuildOptions.Development) == 0);
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = buildOptions,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log("[SlotMachine] WebGL build (" + output + ") " + summary.result + " in " + summary.totalTime +
                      ", " + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB");

            if (Application.isBatchMode)
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Vansh Srivastava";
            PlayerSettings.productName = "Retro Jackpot";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;

            // The game has its own loading screen; skipping the engine splash
            // (optional on every licence tier since Unity 6) gets players to the
            // reels about two seconds sooner.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            // Brotli makes the download about a fifth smaller than gzip. The
            // decompression fallback lets the loader unpack it in JavaScript, so
            // the build still runs from any static host (GitHub Pages, a local
            // server) without special Content-Encoding headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "PROJECT:RetroJackpot";
        }

        /// <summary>
        /// Release builds use Unity's recommended Web size settings: the
        /// download is mostly engine and game code, and these shrink it the
        /// most. QA builds keep the fast-compiling defaults.
        /// </summary>
        private static void ConfigureCodeSize(bool release)
        {
            var web = NamedBuildTarget.WebGL;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true;
            PlayerSettings.SetManagedStrippingLevel(web, release ? ManagedStrippingLevel.High : ManagedStrippingLevel.Medium);
            PlayerSettings.SetIl2CppCodeGeneration(web, release ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed);
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            // The core rules throw on invalid settings, so exceptions stay on
            // (explicitly thrown only) rather than None, which would freeze the game.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            // Stored per machine in Library/, so it's set here to travel with the project.
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = release
                ? UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO
                : UnityEditor.WebGL.WasmCodeOptimization.BuildTimes;
            AssetDatabase.SaveAssets();
            Debug.Log("[SlotMachine] Code size settings: stripping=" + PlayerSettings.GetManagedStrippingLevel(web) +
                      ", il2cpp=" + PlayerSettings.GetIl2CppCodeGeneration(web) +
                      ", wasm=" + UnityEditor.WebGL.UserBuildSettings.codeOptimization +
                      ", exceptions=" + PlayerSettings.WebGL.exceptionSupport);
        }
    }
}
