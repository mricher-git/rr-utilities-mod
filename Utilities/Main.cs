using System;
using System.Collections.Generic;
using System.Reflection;
using dnlib;
using HarmonyLib;
using Helpers;
using Model.AI;
using Track;
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

		var go = new GameObject("[UtilitiesMod]");
		Instance = go.AddComponent<UtilitiesMod>();
		UnityEngine.Object.DontDestroyOnLoad(go);
		Instance.Settings = Settings;


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
				HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
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
		}

		return true;
	}

	private static bool Unload(UnityModManager.ModEntry modEntry)
	{
		if (Instance != null) UnityEngine.Object.DestroyImmediate(Instance.gameObject);
		return true;
	}

	private static void OnGUI(UnityModManager.ModEntry modEntry)
	{
		Settings.Draw(ModEntry);
	}

	public class UtilitiesModSettings : UnityModManager.ModSettings, IDrawable
	{
		public bool DisableDamage;
		public bool UnlimitedResources;
		public bool DisableDerailment;
		public bool FreePurchases;

		[Draw("Graphics", Box = true)]
		public GraphicsSettings graphicsSettings = new GraphicsSettings();

		public class GraphicsSettings
		{
			[Draw("Anti-aliasing Mode")]
			public AntialiasingMode AntiAliasing = AntialiasingMode.FastApproximateAntialiasing;
			[Draw("Anti-aliasing Quality")]
			public AntialiasingQuality AntiAliasingQuality = AntialiasingQuality.High;
			[Draw("LOD Bias", Type = DrawType.Slider, Min = 1f, Max = 7f, Precision = 0)]
			public float lodBias = QualitySettings.lodBias;
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

			[Header("Gladhands")]
			[Draw("Click Distance (Default 30)", Type = DrawType.Slider, Min = 30f, Max = 150f, Precision = 0)]
			public float GladhandsDistance = 30f;
			[Draw("Click Radius (Default: 0.06)", Type = DrawType.Slider, Min = 0.06f, Max = 0.3f)]
			public float GladhandsRadius = 0.12f;

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
		}

		public override void Save(UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}

		public void OnChange()
		{
			Instance.OnSettingsChanged();
		}
	}
}
