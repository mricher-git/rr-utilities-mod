using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityModManagerNet;

namespace Utilities.UMM;

#if DEBUG
[EnableReloading]
#endif
public static class Loader
{
	public static UnityModManager.ModEntry ModEntry { get; private set; }
	public static Harmony HarmonyInstance { get; private set; }
	public static UtilitiesMod Instance { get; private set; }

	internal static UtilitiesModSettings Settings;

	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		if (ModEntry != null || Instance != null)
		{
			modEntry.Logger.Warning("Utilities is already loaded!");
			return false;
		}

		ModEntry = modEntry;
		Settings = UnityModManager.ModSettings.Load<UtilitiesModSettings>(modEntry);
		ModEntry.OnUnload = Unload;
		ModEntry.OnToggle = OnToggle;
		ModEntry.OnGUI = OnGUI;
		ModEntry.OnSaveGUI = Settings.Save;

		HarmonyInstance = new Harmony(modEntry.Info.Id);
		//Harmony.DEBUG = true;
		return true;
	}

	public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
	{
		if (value)
		{
			try
			{
				var go = new GameObject("[UtilitiesMod]");
				Instance = go.AddComponent<UtilitiesMod>();
				UnityEngine.Object.DontDestroyOnLoad(go);
				Instance.Settings = Settings;
				HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

				InputSystemBlocker.CreateInputSystemBlocker();

			}
			catch (Exception ex)
			{
				modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
				HarmonyInstance?.UnpatchAll(modEntry.Info.Id);
				return false;
			}
		}
		else
		{
			HarmonyInstance.UnpatchAll(modEntry.Info.Id);
			if (Instance != null) UnityEngine.Object.DestroyImmediate(Instance.gameObject);
			Instance = null;
		}

		return true;
	}

	private static bool Unload(UnityModManager.ModEntry modEntry)
	{
		if (InputSystemBlocker.Instance != null)
			UnityEngine.Object.DestroyImmediate(InputSystemBlocker.Instance);
		return true;
	}

	private static bool showGraphics = true;
	private static bool showDistance = true;
	private static float orig_SSAA;
	private static void OnGUI(UnityModManager.ModEntry modEntry)
	{
		using (new GUILayout.VerticalScope())
		{
			orig_SSAA = Settings.graphicsSettings.SSAA;
			var showGraphics = Settings.showGraphics;
			var showDistance = Settings.showDistance;
			using (new GUILayout.HorizontalScope())
			{
				GUILayout.Label("Graphics Settings", GUILayout.ExpandWidth(false));
				if (GUILayout.Button(Settings.showGraphics ? "Hide" : "Show", GUILayout.ExpandWidth(false)))
				{
					Settings.showGraphics = !Settings.showGraphics;
				}
			}
			if (showGraphics)
			{
				using (new GUILayout.VerticalScope("box"))
				{
					UnityModManager.UI.DrawFields(ref Settings.graphicsSettings, modEntry, DrawFieldMask.OnlyDrawAttr, Settings.OnGraphicsSettingsChanged);
				}
			}
			using (new GUILayout.HorizontalScope())
			{
				GUILayout.Label("Distance Interaction Settings", GUILayout.ExpandWidth(false));
				if (GUILayout.Button(Settings.showDistance ? "Hide" : "Show", GUILayout.ExpandWidth(false)))
				{
					Settings.showDistance = !Settings.showDistance;
				}
			}
			if (showDistance)
				using (new GUILayout.VerticalScope("box"))
				{
					UnityModManager.UI.DrawFields(ref Settings.distanceSettings, modEntry, DrawFieldMask.OnlyDrawAttr, Settings.OnDistanceChange);
				}
		}
		//Settings.Draw(ModEntry);
	}

	public class UtilitiesModSettings : UnityModManager.ModSettings//, IDrawable
	{
		public bool DisableDamage;
		public bool UnlimitedResources;
		public bool DisableDerailment;
		public bool FreePurchases;
		public bool showGraphics = true;
		public bool showDistance = true;

		[Draw("Graphics Settings", Box = true, Collapsible = true)]
		public GraphicsSettings graphicsSettings = new GraphicsSettings();

		public class GraphicsSettings
		{
			[Header("Anti-aliasing")]
			[Draw("Super-Sampling Anti-Aliasing (SSAA) <i><b>high impact</b></i>",Type = DrawType.Slider, Min = 1, Max = 2)]
			public float SSAA = 1f;
			[Draw("Multi-Sampling Anti-Aliasing (MSAA)")]
			public MsaaQuality MSAA = (MsaaQuality)((QualitySettings.renderPipeline as UniversalRenderPipelineAsset)?.msaaSampleCount ?? 2);
			[Draw("Post Processing Anti-aliasing Mode (FXAA/SMAA")]
			public AntialiasingMode PostProcessingAntiAliasing = AntialiasingMode.FastApproximateAntialiasing;
			[Draw("Post Processing Anti-aliasing Quality")]
			public AntialiasingQuality PostProcessingAntiAliasingQuality = AntialiasingQuality.High;

			[Header("LOD")]
			[Draw("LOD Bias", Type = DrawType.Slider, Min = 1f, Max = 10f, Precision = 0)]
			public float lodBias = QualitySettings.lodBias;
			[Draw("Foliage LOD Bias", Type = DrawType.Slider, Min = 1f, Max = 10f, Precision = 0)]
			public float lodBiasFoliage = QualitySettings.lodBias;
		}

		[Draw("Distance Interaction Settings", Box = true, Collapsible = true)]
		public DistanceSettings distanceSettings = new DistanceSettings();

		public class DistanceSettings
		{
			[Header("Switch Stand")]
			[Draw("Click Distance (Default 100)", Type = DrawType.Slider, Min = 100f, Max = 500f, Precision = 0)]
			public float SwitchStandDistance = 300f;
			[Draw("Click Radius (Default 0.17)", Type = DrawType.Slider, Min = 0.17f, Max = 0.5f)]
			public float SwitchStandRadius = 0.34f;


			[Header("Flare / Fusee")]
			[Draw("Click Distance (Default 100)", Type = DrawType.Slider, Min = 100f, Max = 500f, Precision = 0)]
			public float FlareDistance = 300f;
			[Draw("Click Radius (Default: 0.09)", Type = DrawType.Slider, Min = 0.09f, Max = 0.5f)]
			public float FlareRadius = 0.27f;

			[Header("Coupler")]
			[Draw("Click Distance (Default 50)", Type = DrawType.Slider, Min = 50f, Max = 250f, Precision = 0)]
			public float CouplerDistance = 250f;
			[Draw("Click Radius (Default: 0.21)", Type = DrawType.Slider, Min = 0.21f, Max = 0.5f)]
			public float CouplerRadius = 0.21f;

			[Header("Gladhands / Hose")]
			[Draw("Click Distance (Default 30)", Type = DrawType.Slider, Min = 30f, Max = 150f, Precision = 0)]
			public float GladhandsDistance = 30f;
			[Draw("Click Radius (Default: 0.06)", Type = DrawType.Slider, Min = 0.06f, Max = 0.3f)]
			public float GladhandsRadius = 0.12f;
			[Draw("Hose Render Distance (Default: 25)", Type = DrawType.Slider, Min = 25f, Max = 75f, Precision = 0)]
			public float HoseRenderDistance = 25f;

			[Header("Station Info")]
			[Draw("Click Distance (Default 50)", Type = DrawType.Slider, Min = 50f, Max = 500f, Precision = 0)]
			public float StationDistance = 250f;
			[Draw("Click Radius (Default 2.81)", Type = DrawType.Slider, Min = 2.81, Max = 6f)]
			public float StationRadius = 4.81f;

			[Header("Industry / Loader Info")]
			[Draw("Hover Distance (Default 50)", Type = DrawType.Slider, Min = 50f, Max = 500f, Precision = 0)]
			public float IndustryDistance = 250f;

			[Header("Coal Loader")]
			[Draw("Click Distance (Default 50)", Type = DrawType.Slider, Min = 50f, Max = 250f, Precision = 0)]
			public float CoalDistance = 150f;
			[Draw("Click Radius (Default: 0.3)", Type = DrawType.Slider, Min = 0.3f, Max = 1.2f)]
			public float CoalRadius = 0.6f;

			[Header("Water Spout")]
			[Draw("Click Distance (Default 50)", Type = DrawType.Slider, Min = 50f, Max = 250f, Precision = 0)]
			public float WaterDistance = 150f;
			[Draw("Click Radius (Default: 0.3)", Type = DrawType.Slider, Min = 0.3f, Max = 1.2f)]
			public float WaterRadius = 0.6f;

			[Header("Diesel Stand")]
			[Draw("Click Distance (Default 50)", Type = DrawType.Slider, Min = 50f, Max = 250f, Precision = 0)]
			public float DieselDistance = 150f;
			[Draw("Click Radius (Default: 0.1)", Type = DrawType.Slider, Min = 0.1f, Max = 1.2f)]
			public float DieselRadius = 0.6f;

			[Header("Query Tool")]
			[Draw("Distance (Default 100)", Type = DrawType.Slider, Min = 100, Max = 1500f, Precision = 0)]
			public float QueryDistance = 1500f;

			[Header("Roundhouse Stall Doors")]
			[Draw("Distance (Default 10)", Type = DrawType.Slider, Min = 10, Max = 500f, Precision = 0)]
			public float StallDoorsDistance = 250f;
		}

		public override void Save(UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}


		public void OnGraphicsSettingsChanged()
		{
			if (orig_SSAA != graphicsSettings.SSAA)
			{
				graphicsSettings.SSAA = (float)(Math.Round(graphicsSettings.SSAA * 2, MidpointRounding.AwayFromZero) / 2);
				if (orig_SSAA == graphicsSettings.SSAA) return;
			}

			Instance.OnGraphicsSettingsChanged();
		}

		public void OnDistanceChange()
		{
			Instance.OnDistanceSettingsChanged();
		}

		/*
		public void OnChange()
		{
			if (orig_SSAA != graphicsSettings.SSAA)
			{
				graphicsSettings.SSAA = (float)(Math.Round(graphicsSettings.SSAA * 2, MidpointRounding.AwayFromZero) / 2);
				if (orig_SSAA == graphicsSettings.SSAA) return;
			}

			Instance.OnDistanceSettingsChanged();
		}
		*/
	}

	public static void Log(string str)
	{
		ModEntry?.Logger.Log(str);
	}

	public static void LogDebug(string str)
	{
#if DEBUG
		ModEntry?.Logger.Log(str);
#endif
	}
}
