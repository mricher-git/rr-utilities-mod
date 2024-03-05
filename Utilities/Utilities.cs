using Character;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using GPUInstancer;
using HarmonyLib;
using Helpers;
using Map.Runtime;
using Model;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops.Definition;
using Model.OpsNew;
using RollingStock;
using RollingStock.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Track;
using UI.CarInspector;
using UI.Console.Commands;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityModManagerNet;
using Utilities.UMM;

namespace Utilities;

public class UtilitiesMod : MonoBehaviour
{
	public enum MapStates { MAINMENU, MAPLOADED, MAPUNLOADING }
	public static MapStates MapState { get; private set; } = MapStates.MAINMENU;

	// GUI vars
	private static readonly GUIStyle buttonStyle = new GUIStyle() { fontSize = 8 };
	private bool showGui = false;
	private Rect buttonRect = new Rect(0, 30, 20, 20);
	private Rect windowRect = new Rect(20, 30, 0, 0);
	private Vector2 scrollPosition;
	private Rect scrollRect;

	private Rect teleportRect;
	private int teleportLocation = -1;
	private bool teleportShow = false;
	private static string[] _teleportLocations;
	private static string[] teleportLocations
	{
		get
		{
			return _teleportLocations ?? SpawnPoint.All.Where(sp => sp.name.ToLower() != "ds").Select(sp => sp.name).OrderBy(sp => sp).ToArray();
		}
	}

	private Rect loadRect;
	private int loadIndex = -1;
	private bool loadShow = false;
	private static string[] _loadNames;
	private static string[] loadNames
	{
		get
		{
			return _loadNames ?? CarPrototypeLibrary.instance.opsLoads.Select(load => load.id).ToArray();
		}
	}


	internal Loader.UtilitiesModSettings Settings;

	public static UtilitiesMod Instance
	{
		get => Loader.Instance;
	}

	void Start()
	{
		Messenger.Default.Register<MapDidLoadEvent>(this, new Action<MapDidLoadEvent>(this.OnMapDidLoad));
		Messenger.Default.Register<MapWillUnloadEvent>(this, new Action<MapWillUnloadEvent>(this.OnMapWillUnload));

		if (StateManager.Shared.Storage != null)
		{
			OnMapDidLoad(new MapDidLoadEvent());
		}
	}

	void OnDestroy()
	{
		Messenger.Default.Unregister<MapDidLoadEvent>(this);
		Messenger.Default.Unregister<MapWillUnloadEvent>(this);

		if (MapState == MapStates.MAPLOADED)
		{
			OnMapWillUnload(new MapWillUnloadEvent());
		}
		MapState = MapStates.MAINMENU;
	}

	private void OnMapDidLoad(MapDidLoadEvent evt)
	{
		if (MapState == MapStates.MAPLOADED) return;
		MapState = MapStates.MAPLOADED;
		OnDistanceSettingsChanged();
		OnGraphicsSettingsChanged();
	}

	private void OnMapWillUnload(MapWillUnloadEvent evt)
	{
		MapState = MapStates.MAPUNLOADING;
	}

	public void OnGraphicsSettingsChanged()
	{
		if (MapState != MapStates.MAPLOADED) return;

		var pipelineAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
		if (pipelineAsset != null)
		{
			pipelineAsset.msaaSampleCount = (int)Settings.graphicsSettings.MSAA;
			pipelineAsset.renderScale = Settings.graphicsSettings.SSAA;
		}

		var cam = Camera.main.GetUniversalAdditionalCameraData();
		if (cam != null)
		{

			cam.antialiasing = Settings.graphicsSettings.PostProcessingAntiAliasing;
			cam.antialiasingQuality = Settings.graphicsSettings.PostProcessingAntiAliasingQuality;
		}

		QualitySettings.lodBias = Settings.graphicsSettings.lodBias;
		GPUInstancerAPI.SetLODBias(MapManager.Instance.sharedTreeManager, Settings.graphicsSettings.lodBiasTree);
		foreach (MapTerrain mapTerrain in MapManager.Instance._terrains.Values)
		{
			if (mapTerrain.detailManager != null)
			{
				GPUInstancerAPI.SetLODBias(mapTerrain.detailManager, Settings.graphicsSettings.lodBiasDetail);
			}
		}
	}

	public void OnDistanceSettingsChanged()
	{
		if (MapState != MapStates.MAPLOADED) return;

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

	void OnGUI()
	{
		if (MapState != MapStates.MAPLOADED)
		{
			showGui = false;
			return;
		}

		selectedCar = TrainController.Shared.SelectedCar;
		buttonRect = new Rect(0, UnityModManager.UI.Scale(30), UnityModManager.UI.Scale(20), UnityModManager.UI.Scale(20));
		if (GUI.Button(buttonRect, "U", new GUIStyle(GUI.skin.button) { fontSize = UnityModManager.UI.Scale(10), clipping = TextClipping.Overflow })) showGui = !showGui;

		if (showGui)
		{
			windowRect.x = UnityModManager.UI.Scale(20);
			windowRect.y = UnityModManager.UI.Scale(30);

			windowRect = GUILayout.Window(555, windowRect, Window, "Utilities", GUIStyle.none, GUILayout.Height(Screen.height - windowRect.y), GUILayout.Width(UnityModManager.UI.Scale(270) + GUI.skin.verticalScrollbar.fixedWidth));
			if (teleportShow)
			{
				if (teleportRect.width > 0)
				{
					teleportRect.x = windowRect.x + windowRect.width;
					teleportRect.y = windowRect.y + windowRect.height - teleportRect.height;
				}
				teleportRect = GUILayout.Window(556, teleportRect, TeleportWindow, "Select Teleport Location");
			}
			if (loadShow)
			{
				if (loadRect.width > 0)
				{
					loadRect.x = windowRect.x + windowRect.width;
					loadRect.y = windowRect.y + windowRect.height - loadRect.height;
				}
				loadRect = GUILayout.Window(557, loadRect, LoadWindow, "Select Load Type");
			}
		}
	}

	private Car? selectedCar;
	public static GUIStyle styleMiddleRight;
	public static GUIStyle styleMiddleCenter;
	private bool doOnce;
	void Window(int windowId)
	{
		if (!doOnce)
		{
			styleMiddleRight = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleRight };
			styleMiddleCenter = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
			doOnce = true;
		}

		var stateManager = StateManager.Shared;
		
		GUIStyle centeredLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

		scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUI.skin.window, GUILayout.ExpandHeight(false));
		using (new GUILayout.VerticalScope())
		{
			using (new GUILayout.VerticalScope("box"))
			{
				GUILayout.Label("Money", styleMiddleCenter, GUILayout.ExpandWidth(true));

				GUILayout.BeginHorizontal();
				GUILayout.Label("Money: ");
				GUILayout.Label(stateManager.GetBalance().ToString("C0"), styleMiddleRight);
				//GUILayout.Button("Set");
				GUILayout.EndHorizontal();

				if (StateManager.IsHost)
				{
					GUILayout.BeginHorizontal();
					if (GUILayout.Button("> $1k"))
						AddMoney(1000);
					if (GUILayout.Button("> $10k"))
						AddMoney(10000);
					if (GUILayout.Button("> $100k"))
						AddMoney(100000);
					if (GUILayout.Button("> $1M"))
						AddMoney(1000000);
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					if (GUILayout.Button("< $1k"))
						AddMoney(-1000);
					if (GUILayout.Button("< $10k"))
						AddMoney(-10000);
					if (GUILayout.Button("< $100k"))
						AddMoney(-100000);
					if (GUILayout.Button("< $1M"))
						AddMoney(-1000000);
					GUILayout.EndHorizontal();
				}
			}

			using (new GUILayout.VerticalScope("box"))
			{
				GUILayout.Label("Game Mode", styleMiddleCenter, GUILayout.ExpandWidth(true));

				GUILayout.BeginHorizontal();
				int gameMode = (int)stateManager.GameMode;
				GUILayout.FlexibleSpace();
				if (UnityModManager.UI.ToggleGroup(ref gameMode, Enum.GetNames(typeof(GameMode))))
					StateManager.ApplyLocal(new PropertyChange("_game", "mode", new IntPropertyValue((int)gameMode)));
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}

			using (new GUILayout.VerticalScope("box"))
			{
				GUILayout.Label("Time Controls", styleMiddleCenter, GUILayout.ExpandWidth(true));

				GUILayout.Label("Time: " + TimeWeather.TimeOfDayString);
				if (StateManager.CheckAuthorizedToSendMessage(default(WaitTime)))
				{
					GUILayout.BeginHorizontal();
					if (GUILayout.Button("Wait 1 Hour"))
					{
						StateManager.ApplyLocal(new WaitTime { Hours = 1f });
					}
					if (GUILayout.Button("    Sleep    "))
					{
						float currentHours = TimeWeather.Now.Hours;
						int interchangeServeHour = stateManager.Storage.InterchangeServeHour;
						float hours = ((currentHours < (float)interchangeServeHour) ? ((float)interchangeServeHour - currentHours) : (24f - currentHours + (float)interchangeServeHour));
						StateManager.ApplyLocal(new WaitTime { Hours = hours });
					}
					GUILayout.EndHorizontal();
				}
			}

			using (new GUILayout.VerticalScope("box"))
			{
				GUILayout.Label("Teleport Locations", styleMiddleCenter, GUILayout.ExpandWidth(true));

				if (GUILayout.Button("Teleport to Dispatch Station"))
				{
					new TeleportCommand().Execute(new string[] { "/tp", "ds" });
				}
				if (GUILayout.Button("Choose Location"))
				{
					teleportShow = !teleportShow;
					loadShow = false;
					teleportLocation = -1;
				}
			}

			using (new GUILayout.VerticalScope("box"))
			{
				GUILayout.Label("Weather Presets", styleMiddleCenter, GUILayout.ExpandWidth(true));

				GUILayout.BeginHorizontal();
				int selected = -1;
				string[] weather = TimeWeather.WeatherIdLookup.Keys.Select(key => char.ToUpper(key[0]) + key.Substring(1)).OrderBy(x => x).ToArray();
				selected = GUILayout.SelectionGrid(selected, weather, 2);
				if (selected != -1)
				{
					StateManager.ApplyLocal(new PropertyChange("_game", "weatherId", new IntPropertyValue(TimeWeather.WeatherIdLookup[weather[selected].ToLower()])));
				}
				GUILayout.EndHorizontal();
			}


			if (StateManager.IsSandbox && StateManager.IsHost)
			{
				GUILayout.Label("Sandbox shortcuts", styleMiddleCenter, GUILayout.ExpandWidth(true));
				using (new GUILayout.VerticalScope("box"))
				{
					GUILayout.Label("Train info", styleMiddleCenter, GUILayout.ExpandWidth(true));

					GUILayout.Label("Selected Loco/Car: " + (selectedCar ? CarInspector.TitleForCar(selectedCar) : "None"));

					if (selectedCar != null)
					{
						GUILayout.Label(CarInspector.SubtitleForCar(selectedCar));
						GUILayout.Label($"Speed: {(Mathf.Abs(selectedCar.velocity) * 2.23694f),4:N1} mph");

						int count = selectedCar.Definition.LoadSlots.Count;

						for (int i = 0; i < count; i++)
						{
							CarLoadInfo? loadInfo = selectedCar.GetLoadInfo(i);
							if (loadInfo != null)
							{
								CarLoadInfo value = loadInfo.Value;
								Load load = CarPrototypeLibrary.instance.LoadForId(value.LoadId);
								if (load != null)
									GUILayout.Label($"Load: {value.LoadString(load)}");
							}
						}
					}
				}
				if (selectedCar != null)
				{
					int loadSlots = selectedCar.Definition.LoadSlots.Count;
					using (new GUILayout.VerticalScope("box"))
					{

						GUILayout.Label("Repair", styleMiddleCenter, GUILayout.ExpandWidth(true));
						if (GUILayout.Button("Repair Car"))
						{
							selectedCar.SetCondition(1f);
						}
						if (GUILayout.Button("Repair Train"))
						{
							List<Car> list = TrainController.Shared.SelectedTrain.Where((Car car) => car.Definition.Archetype.IsFreight()).ToList<Car>();

							foreach (Car car in TrainController.Shared.SelectedTrain)
								car.SetCondition(1f);
						}
					}

					using (new GUILayout.VerticalScope("box"))
					{
						GUILayout.Label("Fill/Empty", styleMiddleCenter, GUILayout.ExpandWidth(true));

						GUILayout.BeginHorizontal();
						if (GUILayout.Button("Fill Car") && TrainController.Shared.SelectedCar)
						{
							for (int i = 0; i < loadSlots; i++)
							{
								LoadSlot loadSlot = selectedCar.Definition.LoadSlots[i];
								if (!string.IsNullOrEmpty(loadSlot.RequiredLoadIdentifier))
									selectedCar.SetLoadInfo(i, (new CarLoadInfo(loadSlot.RequiredLoadIdentifier, loadSlot.MaximumCapacity)));
							}
						}
						if (GUILayout.Button("Empty Car") && TrainController.Shared.SelectedCar)
						{
							for (int i = 0; i < loadSlots; i++)
							{
								selectedCar.SetLoadInfo(i, null);
							}
						}
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						if (GUILayout.Button("Fill Train") && TrainController.Shared.SelectedCar)
						{
							foreach (var car in TrainController.Shared.SelectedTrain.Where((Car car) => car.Definition.Archetype.IsFreight()).ToList<Car>())
							{
								for (int i = 0; i < loadSlots; i++)
								{
									LoadSlot loadSlot = car.Definition.LoadSlots[i];
									if (!string.IsNullOrEmpty(loadSlot.RequiredLoadIdentifier))
										car.SetLoadInfo(i, new CarLoadInfo?(new CarLoadInfo(loadSlot.RequiredLoadIdentifier, loadSlot.MaximumCapacity)));
								}
							}
						}
						if (GUILayout.Button("Empty Train") && TrainController.Shared.SelectedCar)
						{
							foreach (var car in TrainController.Shared.SelectedTrain.Where((Car car) => car.Definition.Archetype.IsFreight()).ToList<Car>())
							{
								for (int i = 0; i < loadSlots; i++)
								{
									car.SetLoadInfo(i, null);
								}
							}
						}
						GUILayout.EndHorizontal();
					}

					using (new GUILayout.VerticalScope("box"))
					{
						GUILayout.Label("Load/Unload", styleMiddleCenter, GUILayout.ExpandWidth(true));
						GUILayout.BeginHorizontal();
						GUILayout.Label("Load:", GUILayout.ExpandWidth(false));
						if (GUILayout.Button((loadIndex == -1 ? "Select Load" : loadNames[loadIndex])))
						{
							loadShow = !loadShow;
							teleportShow = false;
							loadIndex = -1;
						}
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						if (GUILayout.Button("Load Car") && TrainController.Shared.SelectedCar && loadIndex >= 0)
						{
							new UI.Console.SetLoadCommand().SetLoad(selectedCar, loadNames[loadIndex], new string[] { "/setload", selectedCar.id, loadNames[loadIndex], "100%" });
						}
						if (GUILayout.Button("Empty Car") && TrainController.Shared.SelectedCar)
						{
							for (int i = 0; i < loadSlots; i++)
							{
								selectedCar.SetLoadInfo(i, null);
							}
						}
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						if (GUILayout.Button("Load Train") && TrainController.Shared.SelectedCar && loadIndex >= 0)
						{
							foreach (var car in TrainController.Shared.SelectedTrain.Where((Car car) => car.Definition.Archetype.IsFreight()).ToList<Car>())
							{
								new UI.Console.SetLoadCommand().SetLoad(car, loadNames[loadIndex], new string[] { "/setload", car.id, loadNames[loadIndex], "100%" });
							}
						}
						if (GUILayout.Button("Empty Train") && TrainController.Shared.SelectedCar)
						{
							foreach (var car in TrainController.Shared.SelectedTrain.Where((Car car) => car.Definition.Archetype.IsFreight()).ToList<Car>())
							{
								for (int i = 0; i < loadSlots; i++)
								{
									car.SetLoadInfo(i, null);
								}
							}
						}
						GUILayout.EndHorizontal();
					}
				}
			}
		}

		if (Event.current.type == EventType.Repaint)
		{
			scrollRect = GUILayoutUtility.GetLastRect();
		}

		GUILayout.EndScrollView();
	}

	private void TeleportWindow(int windowId)
	{
		teleportLocation = GUILayout.SelectionGrid(teleportLocation, teleportLocations, 2);
		if (teleportLocation != -1)
		{
			teleportShow = false;
			new TeleportCommand().Execute(new string[] { "/tp", teleportLocations[teleportLocation] });
			teleportLocation = -1;
		}
	}

	private void LoadWindow(int windowId)
	{
		loadIndex = GUILayout.SelectionGrid(loadIndex, loadNames, 2);
		if (loadIndex != -1)
		{
			loadShow = false;
		}
	}

	private void AddMoney(int amount)
	{
		var sm = StateManager.Shared;
		sm.Balance = sm.Balance + amount;
		StateManager.Shared.SendFireEvent<BalanceDidChange>(default(BalanceDidChange));
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
			Track.Location? location = graph.LocationFromPoint(raycastHit.point, 1f);
			if (location != null)
			{
				Track.Location value = location.Value;
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

