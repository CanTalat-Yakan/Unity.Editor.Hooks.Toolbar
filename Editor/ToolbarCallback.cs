#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEssentials
{
    public static class ToolbarCallback
    {
        private const int MaxSetupAttempts = 200;
        private const string LeftDockName = "CustomToolbarLeft";
        private const string RightDockName = "CustomToolbarRight";
        private const string MiddleContainerClass = "unity-overlay-container__middle-container";

        private static int setupAttempts;

        /// <summary>
        /// Callback for toolbar OnGUI method.
        /// </summary>
        public static Action OnToolbarGUI;
        public static Action OnToolbarGUILeft;
        public static Action OnToolbarGUIRight;

        static ToolbarCallback()
        {
            EditorApplication.update -= Initialize;
            EditorApplication.update += Initialize;
        }

        private static void Initialize()
        {
            setupAttempts++;

            var mainToolbarWindowType = typeof(Editor).Assembly.GetType("UnityEditor.MainToolbarWindow");
            if (mainToolbarWindowType == null)
            {
                TryAbort("MainToolbarWindow type not available yet.");
                return;
            }

            var toolbars = Resources.FindObjectsOfTypeAll(mainToolbarWindowType);
            if (toolbars.Length == 0)
            {
                TryAbort("Could not find MainToolbarWindow instance.");
                return;
            }

            var toolbarWindow = (EditorWindow)toolbars[0];
            var root = toolbarWindow.rootVisualElement;
            if (root == null)
            {
                TryAbort("MainToolbarWindow rootVisualElement is null.");
                return;
            }

            // Prevent duplicate containers after script/domain reload.
            if (root.Q(LeftDockName) != null || root.Q(RightDockName) != null)
            {
                EditorApplication.update -= Initialize;
                return;
            }

            var middleContainer = root.Q(className: MiddleContainerClass);
            if (middleContainer == null)
            {
                TryAbort("MainToolbarWindow middle container is not ready.");
                return;
            }

            var parentContainer = middleContainer.parent;
            if (parentContainer == null)
            {
                TryAbort("MainToolbarWindow parent container is null.");
                return;
            }

            var leftDock = CreateDock(LeftDockName, Justify.FlexEnd);
            var rightDock = CreateDock(RightDockName, Justify.FlexStart);

            var middleIndex = parentContainer.IndexOf(middleContainer);
            parentContainer.Insert(middleIndex, leftDock);
            parentContainer.Insert(middleIndex + 2, rightDock);

            leftDock.Add(new IMGUIContainer(() =>
            {
                OnToolbarGUI?.Invoke();
                OnToolbarGUILeft?.Invoke();
            }));

            rightDock.Add(new IMGUIContainer(() => OnToolbarGUIRight?.Invoke()));

            EditorApplication.update -= Initialize;
        }

        private static VisualElement CreateDock(string name, Justify justify)
        {
            var dock = new VisualElement { name = name };
            dock.style.flexGrow = 1;
            dock.style.flexBasis = 0;
            dock.style.flexDirection = FlexDirection.Row;
            dock.style.justifyContent = justify;
            dock.style.alignItems = Align.Center;
            return dock;
        }

        private static void TryAbort(string reason)
        {
            if (setupAttempts <= MaxSetupAttempts)
                return;

            Debug.LogWarning($"[CustomToolbar] {reason} Aborting toolbar callback setup.");
            EditorApplication.update -= Initialize;
        }
    }
}
#endif