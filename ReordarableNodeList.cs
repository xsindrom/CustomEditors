using System;
using System.Configuration;
using System.Configuration.Assemblies;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Utilities
{
    public class ReorderableNodeList<T> where T: ReordarableNode
    {
        #region static
        public static GUIStyle HeaderStyle = new GUIStyle() { fontStyle = FontStyle.Bold };
        public static Color32 DarkGreen = new Color32(16, 124, 16, 120);
        public static float standardSingleHeight = 0.0f;
        #endregion

        private SerializedObject serializedObject;
        private SerializedProperty serializedProperty;
        private ReorderableList list;

        private List<T> target = new List<T>();
        private List<Type> derivedTypes = new List<Type>();

        public ReorderableNodeList(List<T> target, SerializedObject serializedObject, SerializedProperty serializedProperty)
        {
            standardSingleHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            this.serializedObject = serializedObject;
            this.serializedProperty = serializedProperty;
            this.target = target;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                var types = assembly.GetTypes();
                for (int j = 0; j < types.Length; j++)
                {
                    var type = types[j];
                    if (typeof(T).IsAssignableFrom(type) && !derivedTypes.Contains(type) && !type.IsAbstract)
                    {
                        derivedTypes.Add(type);
                    }
                }
            }
            
            list = new ReorderableList(serializedObject, serializedProperty, true, true, true, true)
            {
                list = target,
                onAddDropdownCallback = OnAddDropdownCallback,
                onCanAddCallback = OnCanAddCallback,
                onCanRemoveCallback = OnCanRemoveCallback,
                onRemoveCallback = OnRemoveCallback,
                elementHeightCallback = OnElementHeightCallback,
                drawElementCallback = OnDrawElementCallback,
                drawElementBackgroundCallback = OnDrawElementBackGroundCallback,
                drawHeaderCallback = OnDrawHeaderCallback,
                onReorderCallbackWithDetails = OnReorderCallback,
                onSelectCallback = OnSelectCallback
            };
        }

        public void OnInspectorGUI()
        {
            serializedObject.Update();
            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
        #region ListCallbacks
        
        public bool OnCanAddCallback(ReorderableList list)
        {
            return derivedTypes.Count > 0;
        }

        public void AddNode(object obj)
        {
            var type = (Type)obj;
            var instance = ScriptableObject.CreateInstance(type);
            target.Add((T)instance);
            instance.name = serializedObject.targetObject.name + "_" + (list.list.Count - 1);

            var path = AssetDatabase.GetAssetPath(serializedObject.targetObject);
            AssetDatabase.AddObjectToAsset(instance, path);
            AssetDatabase.SaveAssets();
        }

        public void OnAddDropdownCallback(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();
            for(int i = 0; i < derivedTypes.Count; i++)
            {
                var derivedType = derivedTypes[i];
                menu.AddItem(new GUIContent(derivedType.Name), false, AddNode, derivedType);
            }
            menu.ShowAsContext();
            
        }
       
        public void OnReorderCallback(ReorderableList list, int oldIndex, int newIndex)
        {
            var newObj = target[newIndex];
            newObj.name = serializedObject.targetObject.name + "_" + newIndex;
            var oldObj = target[oldIndex];
            oldObj.name = serializedObject.targetObject.name + "_" + oldIndex;
            AssetDatabase.SaveAssets();
        }

        public void OnRenameCalblack()
        {
            for(int i = 0; i < serializedObject.targetObjects.Length; i++)
            {
                var targetObj = serializedObject.targetObjects[i];
                var targetProp = new SerializedObject(targetObj).FindProperty(serializedProperty.name);
                for(int j  = 0;  j <  targetProp.arraySize; j++)
                {
                    var pIt = targetProp.GetArrayElementAtIndex(j);
                    var pObj = pIt.objectReferenceValue;
                    pObj.name = targetObj.name + "_" + j;
                }
            }

            AssetDatabase.SaveAssets();
        }
      
        public bool OnCanRemoveCallback(ReorderableList list)
        {
            return derivedTypes.Count > 0;
        }

        public void OnSelectCallback(ReorderableList list)
        {
        }

        public void OnRemoveCallback(ReorderableList list)
        {
            var objectToRemove = target[list.index];
            UnityEngine.Object.DestroyImmediate(objectToRemove,true);
            target.RemoveAt(list.index);
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Draw

        public void OnDrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                                 list.list[index].GetType().Name,
                                 HeaderStyle);
            var height = standardSingleHeight;
            var previousHeight = height;

            var property = serializedProperty.GetArrayElementAtIndex(index);
            var obj = new SerializedObject(property.objectReferenceValue);

            var it = obj.GetIterator();
            it.NextVisible(true);
            while (it.NextVisible(false))
            {
                var itRect = new Rect(rect.x, rect.y + height, rect.width, previousHeight);
                //EditorGUI.DrawRect(itRect, Color.red);
                EditorGUI.PropertyField(itRect, it, it.isExpanded);
                var temp = height;
                height += it.hasChildren ? it.Copy().CountInProperty() * standardSingleHeight : standardSingleHeight;
                previousHeight = height - temp;
            }
            obj.ApplyModifiedProperties();
        }

        public void OnDrawElementBackGroundCallback(Rect rect,int index, bool isActive, bool isFocused)
        {
            if (isFocused)
            {
                EditorGUI.DrawRect(rect, DarkGreen);
            }
        }

        public void OnDrawHeaderCallback(Rect rect)
        {
            //EditorGUI.LabelField(rect, serializedProperty.name);

            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width / 2, rect.height), serializedProperty.name);
            if (GUI.Button(new Rect(rect.x + rect.width / 2, rect.y, rect.width/2, rect.height), "Rename childs"))
            {
                OnRenameCalblack();
            }
        }


        public float OnElementHeightCallback(int index)
        {
            var height = standardSingleHeight;
            var property = list.serializedProperty.GetArrayElementAtIndex(index);
            var obj = new SerializedObject(property.objectReferenceValue);

            var it = obj.GetIterator();
            it.NextVisible(true);
            
            while (it.NextVisible(false))
            {
                height += it.hasChildren ? it.Copy().CountInProperty() * standardSingleHeight : standardSingleHeight;
            }

            return height;
        }
        #endregion
    }
}