﻿using System;
using System.Collections.Generic;
using System.Linq;
#if DEBUG
using System.Reflection;
using System.Text;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Utilities;

namespace CharacterAccessory
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency("marco.kkapi", "1.17")]
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "3.0")]
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories", "1.0.9")]
	public partial class CharacterAccessory : BaseUnityPlugin
	{
		public const string GUID = "madevil.kk.ca";
#if DEBUG
		public const string Name = "Character Accessory (Debug Build)";
#else
		public const string Name = "Character Accessory";
#endif
		public const string Version = "1.3.1.0";

		internal static new ManualLogSource Logger;
		internal static CharacterAccessory Instance;
		internal static Dictionary<string, Harmony> HooksInstance = new Dictionary<string, Harmony>();

		internal static ConfigEntry<bool> CfgMakerMasterSwitch { get; set; }
		internal static ConfigEntry<bool> CfgDebugMode { get; set; }
		internal static ConfigEntry<bool> CfgStudioFallbackReload { get; set; }
		internal static ConfigEntry<bool> CfgMAHookUpdateStudioUI { get; set; }

		internal const int PluginDataVersion = 3;
		internal static List<string> SupportList = new List<string>();
		internal static List<string> CordNames = new List<string>();

		private void Awake()
		{
			Logger = base.Logger;
			Instance = this;

			CfgMakerMasterSwitch = Config.Bind("Maker", "Master Switch", true, new ConfigDescription("A quick switch on the sidebar that templary disable the function", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
			CfgStudioFallbackReload = Config.Bind("Studio", "Fallback Reload Mode", false, new ConfigDescription("Enable this if some plugins are having visual problem", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
			CfgDebugMode = Config.Bind("Debug", "Debug Mode", false, new ConfigDescription("Showing debug messages in LogWarning level", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
			CfgMAHookUpdateStudioUI = Config.Bind("Hook", "MoreAccessories UpdateStudioUI", true, new ConfigDescription("Performance tweak, disable it if having issue on studio chara state panel update", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

			if (Application.dataPath.EndsWith("CharaStudio_Data"))
				CharaStudio.Running = true;
		}

		private void Start()
		{
			CordNames = Enum.GetNames(typeof(ChaFileDefine.CoordinateType)).ToList();
			CharacterApi.RegisterExtraBehaviour<CharacterAccessoryController>(GUID);
			HooksInstance["General"] = Harmony.CreateAndPatchAll(typeof(Hooks));

			MoreAccessoriesSupport.Init();
#if DEBUG
			MoreOutfitsSupport.Init();
#endif
			HairAccessoryCustomizerSupport.Init();
			MaterialEditorSupport.Init();
			MaterialRouterSupport.Init();
			AccStateSyncSupport.Init();
			DynamicBoneEditorSupport.Init();
			AAAPKSupport.Init();
			CumOnOverSupport.Init();

			if (CharaStudio.Running)
			{
				HooksInstance["Studio"] = Harmony.CreateAndPatchAll(typeof(HooksStudio));
				SceneManager.sceneLoaded += CharaStudio.StartupCheck;
			}
			else
			{
				MakerAPI.MakerBaseLoaded += (object sender, RegisterCustomControlsEvent ev) =>
				{
					HooksInstance["Maker"] = Harmony.CreateAndPatchAll(typeof(HooksMaker));
#if DEBUG
					MoreOutfitsSupport.MakerInit();
#endif
					{
						BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("ClothingStateMenu", out PluginInfo _pluginInfo);
						if (_pluginInfo?.Instance != null)
							HooksInstance["Maker"].Patch(_pluginInfo.Instance.GetType().GetMethod("OnGUI", AccessTools.all), prefix: new HarmonyMethod(typeof(HooksMaker), nameof(HooksMaker.DuringLoading_Prefix)));
					}
				};

				MakerAPI.MakerExiting += (object sender, EventArgs ev) =>
				{
					HooksInstance["Maker"].UnpatchAll(HooksInstance["Maker"].Id);
					HooksInstance["Maker"] = null;

					MakerDropdownRef = null;
					MakerToggleEnable = null;
					MakerToggleAutoCopyToBlank = null;
					SidebarToggleEnable = null;
				};

				MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
			}
		}

		internal static void DebugMsg(LogLevel LogLevel, string LogMsg)
		{
			if (CfgDebugMode.Value)
				Logger.Log(LogLevel, LogMsg);
			else
				Logger.Log(LogLevel.Debug, LogMsg);
		}
#if DEBUG
		internal static string DisplayObjectInfo(object o)
		{
			StringBuilder sb = new StringBuilder();

			// Include the type of the object
			Type type = o.GetType();
			sb.Append("Type: " + type.Name);

			// Include information for each Field
			sb.Append("\r\n\r\nFields:");
			FieldInfo[] fi = type.GetFields();
			if (fi.Length > 0)
			{
				foreach (FieldInfo f in fi)
				{
					sb.Append("\r\n " + f.ToString() + " = " + f.GetValue(o));
				}
			}
			else
				sb.Append("\r\n None");

			// Include information for each Property
			sb.Append("\r\n\r\nProperties:");
			PropertyInfo[] pi = type.GetProperties();
			if (pi.Length > 0)
			{
				foreach (PropertyInfo p in pi)
				{
					sb.Append("\r\n " + p.ToString() + " = " + p.GetValue(o, null));
				}
			}
			else
				sb.Append("\r\n None");

			return sb.ToString();
		}
#endif
	}
}
