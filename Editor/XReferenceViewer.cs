using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace XReferenceViewer.Editor
{
    public partial class XReferenceViewer : EditorWindow
    {
        static XReferenceViewer()
        {
            EditorApplication.projectWindowChanged += OnProjectChanged;
        }

        static void OnProjectChanged()
        {
            //wtodo:资源变化了
            Debug.Log("资源变化了");
        }

        private static List<Object> TargetObjects = new List<Object>();

        [MenuItem("Assets/XReferenceViewer", false, 0)]
        static void Open()
        {
            GetWindow<XReferenceViewer>();
        }

        [MenuItem("Assets/XReferenceViewer", true)]
        static bool OpenValidate()
        {
            TargetObjects.Clear();

            var valid = false;
            //wtodo:需要排除package路径
            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                TargetObjects.Add(obj);
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (File.Exists(path))
                {
                    valid = true;
                    break;
                }
                else if (Directory.Exists(path))
                {
                    valid = true;
                    break;
                }
            }

            return valid;
        }

        [SettingsProvider]
        static SettingsProvider CreateSettingProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/XReferenceViewer", SettingsScope.Project)
            {
                label = "XReferenceViewer",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, rootElement) =>
                {
                    // var settings = MyCustomSettings.GetSerializedSettings();
                    //
                    // // rootElement is a VisualElement. If you add any children to it, the OnGUI function
                    // // isn't called because the SettingsProvider uses the UIElements drawing framework.
                    // var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/settings_ui.uss");
                    // rootElement.styleSheets.Add(styleSheet);
                    // var title = new Label()
                    // {
                    //     text = "Custom UI Elements"
                    // };
                    // title.AddToClassList("title");
                    // rootElement.Add(title);
                    //
                    // var properties = new VisualElement()
                    // {
                    //     style =
                    //     {
                    //         flexDirection = FlexDirection.Column
                    //     }
                    // };
                    // properties.AddToClassList("property-list");
                    // rootElement.Add(properties);
                    //
                    // properties.Add(new PropertyField(settings.FindProperty("m_SomeString")));
                    // properties.Add(new PropertyField(settings.FindProperty("m_Number")));
                    //
                    // rootElement.Bind(settings);
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] {"Number", "Some String"})
            };

            return provider;
        }

        private NodeGraphView graphView;

        private void OnEnable()
        {
            graphView = new NodeGraphView()
            {
                style = {flexGrow = 1}
            };
            graphView.OnGraphViewReady = OnGraphViewReady;
            rootVisualElement.Add(graphView);
        }

        void OnGraphViewReady()
        {
            foreach (var target in TargetObjects)
            {
                var path = AssetDatabase.GetAssetPath(target);
                var ownerNode = new OwnerNode(path);
                graphView.AddElement(ownerNode);
                FillDependenceNode(ownerNode);
            }

            var timer = graphView.schedule.Execute(() => { graphView.FrameAll(); });
            timer.ExecuteLater(1L);
        }

        void FillDependenceNode(OwnerNode owner)
        {
            var assetDependencies = AssetDatabase.GetDependencies(owner.AssetPath, false);
            foreach (var dependence in assetDependencies)
            {
                var dependenceNode = new DependentNode(dependence);
                graphView.AddElement(dependenceNode);
                graphView.LinkNode(owner, dependenceNode);
            }
        }
    }
}