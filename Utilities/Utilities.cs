using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.State;
using HarmonyLib;
using Helpers;
using Model;
using RollingStock;
using RollingStock.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Track;
using UI.Menu;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Utilities.UMM;

namespace Utilities;

public class UtilitiesMod : MonoBehaviour
{
	// GUI vars
	private static readonly GUIStyle buttonStyle = new GUIStyle() { fontSize = 8 };
	private bool showGui = false;
	private Rect buttonRect = new Rect(0, 30, 20, 20);
	private Rect windowRect = new Rect(20, 30, 0, 0);
	private Vector2 scrollPosition;
	private Rect scrollRect;

	internal Loader.UtilitiesModSettings Settings;

	void Start()
	{
		Messenger.Default.Register<MapDidLoadEvent>(this, new Action<MapDidLoadEvent>(this.OnMapDidLoad));
		Messenger.Default.Register<MapWillUnloadEvent>(this, new Action<MapWillUnloadEvent>(this.OnMapWillUnload));

		if (StateManager.Shared.Storage != null) OnMapDidLoad(new MapDidLoadEvent());
	}

	private void OnMapDidLoad(MapDidLoadEvent evt)
	{
		OnSettingsChanged();
	}

	private void OnMapWillUnload(MapWillUnloadEvent evt)
	{

	}

	public void OnSettingsChanged()
	{
		var cam = Camera.main.GetUniversalAdditionalCameraData();
		if (cam != null)
		{
			cam.antialiasing = Settings.graphicsSettings.AntiAliasing;
			cam.antialiasingQuality = Settings.graphicsSettings.AntiAliasingQuality;
			if (Settings.graphicsSettings.AntiAliasing == AntialiasingMode.None)
			{
				Camera.main.allowMSAA = false;
			}
			else
			{
				Camera.main.allowMSAA = true;
			}
		}
		QualitySettings.lodBias = Settings.graphicsSettings.lodBias;

		foreach (var obj in Resources.FindObjectsOfTypeAll<SwitchStand>())
		{
			var collider = obj.GetComponentInChildren<CapsuleCollider>(true);
			collider.radius = Settings.distanceSettings.SwitchStandRadius;
			collider.height = 1.94f + (Settings.distanceSettings.SwitchStandRadius - 0.17f) * 2f;
		}

		foreach (var obj in Resources.FindObjectsOfTypeAll<FlarePickable>())
		{
			var collider = obj.GetComponentInChildren<CapsuleCollider>(true);
			collider.radius = Settings.distanceSettings.FlareRadius;
			collider.height = 0.4f + (Settings.distanceSettings.FlareRadius - 0.09f) * 2f;
		}

		foreach (var obj in Resources.FindObjectsOfTypeAll<CouplerPickable>())
		{
			var collider = obj.GetComponentInChildren<CapsuleCollider>(true);
			collider.radius = Settings.distanceSettings.CouplerRadius;
			collider.height = 0.53f + (Settings.distanceSettings.CouplerRadius - 0.21f) * 2f;
		}

		foreach (var obj in Resources.FindObjectsOfTypeAll<GladhandClickable>())
		{
			var collider = obj.GetComponentInChildren<CapsuleCollider>(true);
			collider.radius = Settings.distanceSettings.GladhandsRadius;
			collider.height = 0.53f + (Settings.distanceSettings.GladhandsRadius - 0.21f) * 2f;
		}

		foreach (var obj in Resources.FindObjectsOfTypeAll<StationAgent>())
		{
			var collider = obj.GetComponentInChildren<BoxCollider>(true);
			var delta = Settings.distanceSettings.StationRadius - 2.81f;
			collider.size = new Vector3(1f + delta, collider.size.y, 2.81f + delta);
		}

		foreach (var obj in Resources.FindObjectsOfTypeAll<KeyValuePickableToggle>())
		{
			var collider = obj.GetComponentInChildren<CapsuleCollider>(true);
			if (collider == null) continue;

			if (obj.displayTitle == "Coal Chute")
			{
				collider.radius = Settings.distanceSettings.CoalRadius;
				collider.height = 4.13f + (Settings.distanceSettings.CoalRadius - 0.3f) * 2f;
			}
			else if (obj.displayTitle == "Water Spout")
			{
				collider.radius = Settings.distanceSettings.WaterRadius;
				collider.height = 4.13f + (Settings.distanceSettings.WaterRadius - 0.3f) * 2f;
			}
			else if (obj.displayTitle == "Diesel Fueling Stand")
			{
				collider.radius = Settings.distanceSettings.DieselRadius;
				collider.height = 3f + (Settings.distanceSettings.DieselRadius - 0.1f) * 2f;
			}
		}
	}
}

[HarmonyPatch("SwitchStandClick", "MaxPickDistance", MethodType.Getter)]
public static class SwitchStandMaxDistancePatch
{
	public static bool Prefix(ref float __result)
	{
		__result = Loader.Settings.distanceSettings.SwitchStandDistance;
		
		return false;
	}
}

[HarmonyPatch("FlarePickable", "MaxPickDistance", MethodType.Getter)]
public static class FlareMaxDistancePatch
{
	public static bool Prefix(ref float __result)
	{
		__result = Loader.Settings.distanceSettings.FlareDistance;

		return false;
	}
}

[HarmonyPatch("CouplerPickable", "MaxPickDistance", MethodType.Getter)]
public static class CouplerMaxDistancePatch
{
	public static bool Prefix(ref float __result)
	{
		__result = Loader.Settings.distanceSettings.CouplerDistance;

		return false;
	}
}

[HarmonyPatch("GladhandClickable", "MaxPickDistance", MethodType.Getter)]
public static class GladhandsMaxDistancePatch
{
	public static bool Prefix(ref float __result)
	{
		__result = Loader.Settings.distanceSettings.GladhandsDistance;

		return false;
	}
}

[HarmonyPatch("StationAgent", "MaxPickDistance", MethodType.Getter)]
public static class StationAgentMaxDistancePatch
{
	public static bool Prefix(ref float __result)
	{
		__result = Loader.Settings.distanceSettings.StationDistance;

		return false;
	}
}

[HarmonyPatch("IndustryContentHoverable", "MaxPickDistance", MethodType.Getter)]
public static class IndustryMaxDistancePatch
{
	public static bool Prefix(ref float __result)
	{
		__result = Loader.Settings.distanceSettings.IndustryDistance;

		return false;
	}
}

[HarmonyPatch("KeyValuePickableToggle", "MaxPickDistance", MethodType.Getter)]
public static class CoalWaterDieselMaxDistancePatch
{
	public static bool Prefix(ref float __result, KeyValuePickableToggle __instance)
	{
		if (__instance.displayTitle == "Coal Chute")
		{
			__result = Loader.Settings.distanceSettings.CoalDistance;
			return false;
		}
		else if (__instance.displayTitle == "Water Spout")
		{
			__result = Loader.Settings.distanceSettings.WaterDistance;
			return false;
		}
		else if (__instance.displayTitle == "Diesel Fueling Stand")
		{
			__result = Loader.Settings.distanceSettings.DieselDistance;
			return false;
		}
		

		return true;
	}
}

[HarmonyPatch("ObjectPicker", "QueryTooltipInfo")]
public static class QueryToolDistancePatch
{
	public static bool Prefix(ref TooltipInfo __result, Ray ray)
	{
		RaycastHit raycastHit;
		if (!Physics.Raycast(ray, out raycastHit, Loader.Settings.distanceSettings.QueryDistance, (1 << Layers.Terrain) | (1 << Layers.Track)))
		{
			__result = TooltipInfo.Empty;
			return false;
		}
		if (raycastHit.collider.gameObject.layer == Layers.Track)
		{
			Graph graph = TrainController.Shared.graph;
			Location? location = graph.LocationFromPoint(raycastHit.point, 1f);
			if (location != null)
			{
				Location value = location.Value;
				float num = graph.CurvatureAtLocation(value, Graph.CurveQueryResolution.Interpolate);
				float num2 = Mathf.Abs(graph.GradeAtLocation(value));
				__result = new TooltipInfo("Track", string.Format("{0:F1}%, {1:F0} deg", num2, num));
				return false;
			}
		}

		__result = TooltipInfo.Empty;
		return false;
	}
}

