using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Utilities;

public class InputSystemBlocker : MonoBehaviour
{
	private bool imguiControlActive = false;
	private InputDevice[] deactivedDevices = Array.Empty<InputDevice>();

	void Update()
	{
		if (!imguiControlActive && GUIUtility.keyboardControl != 0)
		{
			deactivedDevices = InputSystem.devices.Where(device => device.enabled).ToArray();
			foreach (var device in deactivedDevices)
			{
				InputSystem.DisableDevice(device);
			}
			imguiControlActive = true;
		}
		else if (imguiControlActive && GUIUtility.keyboardControl == 0)
		{
			foreach (var device in deactivedDevices)
			{
				InputSystem.EnableDevice(device);
			}
			imguiControlActive = false;
		}
	}

	void OnDestroy()
	{
		foreach (var device in deactivedDevices)
		{
			InputSystem.EnableDevice(device);
		}
		imguiControlActive = false;
		Instance = null;
	}

	public static GameObject? Instance { get; private set; }
	
	public static void CreateInputSystemBlocker(Transform? parent = null)
	{
		if (Instance != null) { return; }
		Instance = new GameObject("Input System Blocker", typeof(InputSystemBlocker));
		if (parent) Instance.transform.SetParent(parent, false);
		DontDestroyOnLoad(Instance);
	}
}
