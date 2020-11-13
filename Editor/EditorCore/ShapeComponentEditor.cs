﻿using System;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.UIElements;

#if UNITY_2020_2_OR_NEWER
using ToolManager = UnityEditor.EditorTools.ToolManager;
#else
using ToolManager = UnityEditor.EditorTools.EditorTools;
#endif

namespace UnityEditor.ProBuilder
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ShapeComponent))]
    class ShapeComponentEditor : Editor
    {
        IMGUIContainer m_ShapeField;

        SerializedProperty m_ShapeProperty;
        SerializedProperty m_ShapePropertiesProperty;

        int m_ActiveShapeIndex = 0;

        const string k_dialogTitle = "Shape reset";
        const string k_dialogText = "The current shape has been edited, you will loose all modifications.";

        bool HasMultipleShapeTypes
        {
            get
            {
                m_CurrentShapeType = null;
                foreach(var comp in targets)
                {
                    if(m_CurrentShapeType == null)
                        m_CurrentShapeType = ( (ShapeComponent) comp ).shape.GetType();
                    else if( m_CurrentShapeType != ( (ShapeComponent) comp ).shape.GetType() )
                        return true;
                }

                return false;
            }
        }

        Type m_CurrentShapeType;

        void OnEnable()
        {
            m_ShapeProperty = serializedObject.FindProperty("m_Shape");
            m_ShapePropertiesProperty = serializedObject.FindProperty("m_Properties");

            Undo.undoRedoPerformed += UndoRedoPerformedOnShapeEditor;
        }

        void UndoRedoPerformedOnShapeEditor()
        {
            foreach(var shapeComponent in targets)
                ((ShapeComponent)shapeComponent).Rebuild();
        }

        public override void OnInspectorGUI()
        {
            DrawShapeGUI();
            DrawShapeParametersGUI();
        }

        public void DrawShapeGUI(DrawShapeTool tool = null)
        {
            if(target == null || serializedObject == null)
                return;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            m_ActiveShapeIndex = HasMultipleShapeTypes ? -1 : Mathf.Max(-1, Array.IndexOf(EditorShapeUtility.availableShapeTypes, m_CurrentShapeType));

            m_ActiveShapeIndex = EditorGUILayout.Popup(m_ActiveShapeIndex, EditorShapeUtility.shapeTypes);

            if(EditorGUI.EndChangeCheck())
            {
                var type = EditorShapeUtility.availableShapeTypes[m_ActiveShapeIndex];
                foreach(var comp in targets)
                {
                    ShapeComponent shapeComponent = ( (ShapeComponent) comp );
                    Shape shape = shapeComponent.shape;
                    if(shape.GetType() != type)
                    {
                        if(tool != null)
                            DrawShapeTool.s_ActiveShapeIndex.value = m_ActiveShapeIndex;
                        UndoUtility.RegisterCompleteObjectUndo(shapeComponent, "Change Shape");
                        shapeComponent.SetShape(EditorShapeUtility.CreateShape(type));
                        ProBuilderEditor.Refresh();
                    }
                }
            }

            // if(shapeComp.edited)
            // {
            //     EditorGUILayout.BeginVertical();
            //     EditorGUILayout.HelpBox(
            //         L10n.Tr(
            //             "You have manually modified the Shape. Revert manual changes to access to procedural parameters"),
            //         MessageType.Info);
            //
            //     if(GUILayout.Button("Reset Shape"))
            //     {
            //         shapeComp.edited = false;
            //         shapeComp.Rebuild();
            //     }
            //
            //     EditorGUILayout.EndHorizontal();
            //
            //     GUI.enabled = false;
            // }
        }

        public void DrawShapeParametersGUI(DrawShapeTool tool = null)
        {
            if(target == null || serializedObject == null)
                return;

            serializedObject.Update ();

            foreach(var comp in targets)
            {
                ((ShapeComponent)comp).UpdateProperties();
            }

            EditorGUILayout.PropertyField(m_ShapePropertiesProperty, new GUIContent("Editing Box Properties"), true);

            if(!HasMultipleShapeTypes)
                EditorGUILayout.PropertyField(m_ShapeProperty, new GUIContent("Shape Properties"), true);

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach(var comp in targets)
                {
                    var shapeComponent = comp as ShapeComponent;
                    shapeComponent.UpdateComponent();
                    if(tool != null)
                        tool.SetBounds(shapeComponent.size);
                    EditorShapeUtility.SaveParams(shapeComponent.shape);
                    ProBuilderEditor.Refresh();
                }
            }

            GUI.enabled = true;
        }
    }
}
