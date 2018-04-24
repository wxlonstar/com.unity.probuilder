using UnityEngine;
using UnityEditor;
using UnityEditor.ProBuilder.UI;
using System.Collections.Generic;
using System.Linq;
using ProBuilder.Core;
using UnityEditor.ProBuilder;
using EditorGUILayout = UnityEditor.EditorGUILayout;
using EditorStyles = UnityEditor.EditorStyles;

namespace ProBuilder.Actions
{
	class SelectVertexColor : MenuAction
	{
		public override ToolbarGroup group { get { return ToolbarGroup.Selection; } }
		public override Texture2D icon { get { return IconUtility.GetIcon("Toolbar/Selection_SelectByVertexColor", IconSkin.Pro); } }
		public override TooltipContent tooltip { get { return _tooltip; } }
		GUIContent gc_restrictToSelection = new GUIContent("Current Selection", "Optionally restrict the matches to only those faces on currently selected objects.");

		static readonly TooltipContent _tooltip = new TooltipContent
		(
			"Select by Colors",
			"Selects all faces matching the selected vertex colors."
		);

		public override bool IsEnabled()
		{
			return 	ProBuilderEditor.instance != null &&
					ProBuilderEditor.instance.editLevel != EditLevel.Top &&
					selection != null &&
					selection.Length > 0 &&
					selection.Any(x => x.SelectedTriangleCount > 0);
		}

		public override bool IsHidden()
		{
			return 	editLevel != EditLevel.Geometry;
		}

		public override MenuActionState AltState()
		{
			if(	IsEnabled() && ProBuilderEditor.instance.editLevel == EditLevel.Geometry )
				return MenuActionState.VisibleAndEnabled;

			return MenuActionState.Visible;
		}

		public override void OnSettingsGUI()
		{
			GUILayout.Label("Select by Vertex Color Options", EditorStyles.boldLabel);

			bool restrictToSelection = PreferencesInternal.GetBool("pb_restrictSelectColorToCurrentSelection");

			EditorGUI.BeginChangeCheck();

			restrictToSelection = EditorGUILayout.Toggle(gc_restrictToSelection, restrictToSelection);

			if( EditorGUI.EndChangeCheck() )
				PreferencesInternal.SetBool("pb_restrictSelectColorToCurrentSelection", restrictToSelection);

			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Select Vertex Color"))
			{
				DoAction();
				SceneView.RepaintAll();
			}
		}

		public override pb_ActionResult DoAction()
		{
			UndoUtility.RecordSelection(selection, "Select Faces with Vertex Colors");

			HashSet<Color32> colors = new HashSet<Color32>();

			foreach(pb_Object pb in selection)
			{
				Color[] mesh_colors = pb.colors;

				if(mesh_colors == null || mesh_colors.Length != pb.vertexCount)
					continue;

				foreach(int i in pb.SelectedTriangles)
					colors.Add(mesh_colors[i]);
			}

			List<GameObject> newSelection = new List<GameObject>();
			bool selectionOnly = PreferencesInternal.GetBool("pb_restrictSelectColorToCurrentSelection");
			pb_Object[] pool = selectionOnly ? selection : Object.FindObjectsOfType<pb_Object>();

			foreach(pb_Object pb in pool)
			{
				Color[] mesh_colors = pb.colors;

				if(mesh_colors == null || mesh_colors.Length != pb.vertexCount)
					continue;

				List<pb_Face> matches = new List<pb_Face>();
				pb_Face[] faces = pb.faces;

				for(int i = 0; i < faces.Length; i++)
				{
					int[] tris = faces[i].distinctIndices;

					for(int n = 0; n < tris.Length; n++)
					{
						if( colors.Contains((Color32)mesh_colors[tris[n]]) )
						{
							matches.Add(faces[i]);
							break;
						}
					}
				}

				if(matches.Count > 0)
				{
					newSelection.Add(pb.gameObject);
					pb.SetSelectedFaces(matches);
				}
			}

			Selection.objects = newSelection.ToArray();

			ProBuilderEditor.Refresh();

			return new pb_ActionResult(Status.Success, "Select Faces with Vertex Colors");
		}
	}
}

