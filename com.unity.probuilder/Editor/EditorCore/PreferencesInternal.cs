using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.ProBuilder;

namespace UnityEditor.ProBuilder
{
	/// <summary>
	/// Where a preference is stored. Can be per-project or global.
	/// </summary>
	enum PreferenceLocation
	{
		/// <summary>
		/// Stored per-project.
		/// </summary>
		Project,

		/// <summary>
		/// Shared between all projects.
		/// </summary>
		Global
	};

	/// <summary>
	/// Manage ProBuilder preferences.
	/// </summary>
	[InitializeOnLoad]
	static class PreferencesInternal
	{
		const string k_PrefsAssetName = "ProBuilderPreferences.asset";

		static Dictionary<string, bool> s_BoolDefaults = new Dictionary<string, bool>()
		{
			{ PreferenceKeys.pbForceConvex, false },
			{ PreferenceKeys.pbManifoldEdgeExtrusion, false },
			{ PreferenceKeys.pbPBOSelectionOnly, false },
			{ PreferenceKeys.pbCloseShapeWindow, false },
			{ PreferenceKeys.pbGrowSelectionUsingAngle, false },
			{ PreferenceKeys.pbNormalizeUVsOnPlanarProjection, false },
			{ PreferenceKeys.pbDisableAutoUV2Generation, false },
			{ PreferenceKeys.pbShowSceneInfo, false },
			{ PreferenceKeys.pbEnableBackfaceSelection, false },
			{ PreferenceKeys.pbVertexPaletteDockable, false },
			{ PreferenceKeys.pbGrowSelectionAngleIterative, false },
			{ PreferenceKeys.pbIconGUI, false },
			{ PreferenceKeys.pbUniqueModeShortcuts, false },
			{ PreferenceKeys.pbShiftOnlyTooltips, false },
			{ PreferenceKeys.pbCollapseVertexToFirst, false },
			{ PreferenceKeys.pbEnableExperimental, false },
			{ PreferenceKeys.pbMeshesAreAssets, false },
			{ PreferenceKeys.pbSelectedFaceDither, true },
			{ PreferenceKeys.pbShowPreselectionHighlight, true },
		};

		static Dictionary<string, float> s_FloatDefaults = new Dictionary<string, float>()
		{
			{ PreferenceKeys.pbGrowSelectionAngle, 42f },
			{ PreferenceKeys.pbExtrudeDistance, .5f },
			{ PreferenceKeys.pbWeldDistance, .001f },
			{ PreferenceKeys.pbUVGridSnapValue, .125f },
			{ PreferenceKeys.pbUVWeldDistance, .01f },
			{ PreferenceKeys.pbBevelAmount, .05f },
			{ PreferenceKeys.pbVertexHandleSize, 3f },
			{ PreferenceKeys.pbLineHandleSize, 1f },
			{ PreferenceKeys.pbWireframeSize, .5f },
		};

		static Dictionary<string, int> s_IntDefaults = new Dictionary<string, int>()
		{
			{ PreferenceKeys.pbDefaultEditLevel, 0 },
			{ PreferenceKeys.pbDefaultSelectionMode, 0 },
			{ PreferenceKeys.pbHandleAlignment, 0 },
			{ PreferenceKeys.pbDefaultCollider, (int) ColliderType.MeshCollider },
			{ PreferenceKeys.pbVertexColorTool, (int) VertexColorTool.Painter },
			{ PreferenceKeys.pbToolbarLocation, (int) SceneToolbarLocation.UpperCenter },
			{ PreferenceKeys.pbDefaultEntity, (int) EntityType.Detail },
			{ PreferenceKeys.pbDragSelectMode, (int) SelectionModifierBehavior.Difference },
			{ PreferenceKeys.pbExtrudeMethod, (int) ExtrudeMethod.VertexNormal },
			{ PreferenceKeys.pbShadowCastingMode, (int) ShadowCastingMode.TwoSided },
		};

		static readonly Color k_ProBuilderWireframe = new Color(125f / 255f, 155f / 255f, 185f / 255f, 1f);
		static readonly Color k_ProBuilderSelected = new Color(0f, 210f / 255f, 239f / 255f, 1f);
		static readonly Color k_ProBuilderUnselected = new Color(44f / 255f, 44f / 255f, 44f / 255f, 1f);
		static readonly Color k_ProBuilderPreselection = new Color(179f / 255f, 246f / 255f, 255f / 255f, 1f);

		static Dictionary<string, Color> s_ColorDefaults = new Dictionary<string, Color>()
		{
			{ PreferenceKeys.pbSelectedFaceColor, k_ProBuilderSelected},
			{ PreferenceKeys.pbWireframeColor, k_ProBuilderWireframe},
			{ PreferenceKeys.pbUnselectedEdgeColor, k_ProBuilderUnselected},
			{ PreferenceKeys.pbSelectedEdgeColor, k_ProBuilderSelected},
			{ PreferenceKeys.pbUnselectedVertexColor, k_ProBuilderUnselected},
			{ PreferenceKeys.pbSelectedVertexColor, k_ProBuilderSelected},
			{ PreferenceKeys.pbPreselectionColor, k_ProBuilderPreselection },
		};

		static Dictionary<string, string> s_StringDefaults = new Dictionary<string, string>()
		{
		};

		static PreferencesInternal()
		{
			LoadPreferencesObject();
		}

		static PreferenceDictionary s_Preferences = null;

		static void LoadPreferencesObject()
		{
			string preferencesPath = FileUtility.GetLocalDataDirectory() + k_PrefsAssetName;

			// First try loading at the local files directory
			s_Preferences = AssetDatabase.LoadAssetAtPath<PreferenceDictionary>(preferencesPath);

			// If that fails, search the project for a compatible preference object
			if (s_Preferences == null)
				s_Preferences = FileUtility.FindAssetOfType<PreferenceDictionary>();

			// If that fails, create a new preferences object at the local data directory
			if (s_Preferences == null)
				s_Preferences = FileUtility.LoadRequired<PreferenceDictionary>(preferencesPath);
		}

		/// <summary>
		/// Access the project local preferences asset.
		/// </summary>
		public static PreferenceDictionary preferences
		{
			get
			{
				if (s_Preferences == null)
					LoadPreferencesObject();

				return s_Preferences;
			}
		}

		/**
		 *	Check if project or global preferences contains a key.
		 */
		public static bool HasKey(string key)
		{
			return (s_Preferences != null && s_Preferences.HasKey(key)) || EditorPrefs.HasKey(key);
		}

		/// <summary>
		/// Delete a key from both project and global preferences.
		/// </summary>
		/// <param name="key"></param>
		public static void DeleteKey(string key)
		{
			preferences.DeleteKey(key);
			EditorPrefs.DeleteKey(key);
		}

		/// <summary>
		/// Checks if pref key exists in library, and if so return the value.  If not, return the default value (true).
		/// </summary>
		/// <param name="pref"></param>
		/// <returns></returns>
		public static bool GetBool(string pref)
		{
			// Backwards compatibility reasons dictate that default bool value is true.
			if(s_BoolDefaults.ContainsKey(pref))
				return GetBool(pref, s_BoolDefaults[pref]);
			return GetBool(pref, true);
		}

		/// <summary>
		/// Get a preference bool value. Local preference has priority over EditorPref.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="fallback"></param>
		/// <returns></returns>
		public static bool GetBool(string key, bool fallback)
		{
			if(s_Preferences != null && preferences.HasKey<bool>(key))
				return preferences.GetBool(key, fallback);
			return EditorPrefs.GetBool(key, fallback);
		}

		/// <summary>
		/// Get float value that is stored in preferences, or it's default value.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static float GetFloat(string key)
		{
			if(s_FloatDefaults.ContainsKey(key))
				return GetFloat(key, s_FloatDefaults[key]);
			return GetFloat(key, 1f);
		}

		public static float GetFloat(string key, float fallback)
		{
			if(s_Preferences != null && preferences.HasKey<float>(key))
				return preferences.GetFloat(key, fallback);
			return EditorPrefs.GetFloat(key, fallback);
		}

		/// <summary>
		/// Get int value that is stored in preferences, or it's default value.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static int GetInt(string key)
		{
			if(s_IntDefaults.ContainsKey(key))
				return GetInt(key, s_IntDefaults[key]);
			return GetInt(key, 0);
		}

		public static int GetInt(string key, int fallback)
		{
			if(s_Preferences != null && preferences.HasKey<int>(key))
				return preferences.GetInt(key, fallback);
			return EditorPrefs.GetInt(key, fallback);
		}

		/// <summary>
		/// Get an enum value from the stored preferences (or it's default value).
		/// </summary>
		/// <param name="key"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetEnum<T>(string key) where T : struct, System.IConvertible
		{
			return (T) (object) GetInt(key);
		}

		/// <summary>
		/// Get Color value stored in preferences.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static Color GetColor(string key)
		{
			if(s_ColorDefaults.ContainsKey(key))
				return GetColor(key, s_ColorDefaults[key]);
			return GetColor(key, Color.white);
		}

		public static Color GetColor(string key, Color fallback)
		{
			if(s_Preferences != null && preferences.HasKey<Color>(key))
				return preferences.GetColor(key, fallback);
			InternalUtility.TryParseColor(EditorPrefs.GetString(key), ref fallback);
			return fallback;
		}

		/// <summary>
		/// Get the string value associated with this key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string GetString(string key)
		{
			if(s_StringDefaults.ContainsKey(key))
				return GetString(key, s_StringDefaults[key]);
			return GetString(key, string.Empty);
		}

		public static string GetString(string key, string fallback)
		{
			if(s_Preferences != null && preferences.HasKey<string>(key))
				return preferences.GetString(key, fallback);
			return EditorPrefs.GetString(key, fallback);
		}

		/// <summary>
		/// Get a material from preferences.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static Material GetMaterial(string key)
		{
			if(s_Preferences != null && preferences.HasKey<Material>(key))
				return preferences.GetMaterial(key);

			Material mat = null;

			switch(key)
			{
				case PreferenceKeys.pbDefaultMaterial:
					if(EditorPrefs.HasKey(key))
					{
						if(EditorPrefs.GetString(key) == "Default-Diffuse")
							return BuiltinMaterials.UnityDefaultDiffuse;

						mat = (Material) AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString(key), typeof(Material));
					}
					break;

				default:
					return BuiltinMaterials.DefaultMaterial;
			}

			if(!mat)
				mat = BuiltinMaterials.DefaultMaterial;

			return mat;
		}

		/// <summary>
		/// Retrieve stored shortcuts from preferences in an IEnumerable format.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<Shortcut> GetShortcuts()
		{
			return EditorPrefs.HasKey(PreferenceKeys.pbDefaultShortcuts)
				? Shortcut.ParseShortcuts(EditorPrefs.GetString(PreferenceKeys.pbDefaultShortcuts))
				: Shortcut.DefaultShortcuts();
		}

		/// <summary>
		/// Associate key with int value.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="location">Optional parameter stores preference in project settings (true) or global (false).</param>
		public static void SetInt(string key, int value, PreferenceLocation location = PreferenceLocation.Project)
		{
			if(location == PreferenceLocation.Project)
			{
				preferences.SetInt(key, value);
				UnityEditor.EditorUtility.SetDirty(preferences);
			}
			else
			{
				EditorPrefs.SetInt(key, value);
			}
		}

		/**
		 *	Associate key with float value.
		 *	Optional isLocal parameter stores preference in project settings (true) or global (false).
		 */
		public static void SetFloat(string key, float value, PreferenceLocation location = PreferenceLocation.Project)
		{
			if(location == PreferenceLocation.Project)
			{
				preferences.SetFloat(key, value);
				UnityEditor.EditorUtility.SetDirty(preferences);
			}
			else
			{
				EditorPrefs.SetFloat(key, value);
			}
		}

		/**
		 *	Associate key with bool value.
		 *	Optional isLocal parameter stores preference in project settings (true) or global (false).
		 */
		public static void SetBool(string key, bool value, PreferenceLocation location = PreferenceLocation.Project)
		{
			if(location == PreferenceLocation.Project)
			{
				preferences.SetBool(key, value);
				UnityEditor.EditorUtility.SetDirty(preferences);
			}
			else
			{
				EditorPrefs.SetBool(key, value);
			}
		}

		/**
		 *	Associate key with string value.
		 *	Optional isLocal parameter stores preference in project settings (true) or global (false).
		 */
		public static void SetString(string key, string value, PreferenceLocation location = PreferenceLocation.Project)
		{
			if(location == PreferenceLocation.Project)
			{
				preferences.SetString(key, value);
				UnityEditor.EditorUtility.SetDirty(preferences);
			}
			else
			{
				EditorPrefs.SetString(key, value);
			}
		}

		/**
		 *	Associate key with color value.
		 *	Optional isLocal parameter stores preference in project settings (true) or global (false).
		 */
		public static void SetColor(string key, Color value, PreferenceLocation location = PreferenceLocation.Project)
		{
			if(location == PreferenceLocation.Project)
			{
				preferences.SetColor(key, value);
				UnityEditor.EditorUtility.SetDirty(preferences);
			}
			else
			{
				EditorPrefs.SetString(key, value.ToString());
			}
		}

		/**
		 *	Associate key with material value.
		 *	Optional isLocal parameter stores preference in project settings (true) or global (false).
		 */
		public static void SetMaterial(string key, Material value, PreferenceLocation location = PreferenceLocation.Project)
		{
			if(location == PreferenceLocation.Project)
			{
				preferences.SetMaterial(key, value);
				UnityEditor.EditorUtility.SetDirty(preferences);
			}
			else
			{
				EditorPrefs.SetString(key, value != null ? AssetDatabase.GetAssetPath(value) : "");
			}
		}
	}
}