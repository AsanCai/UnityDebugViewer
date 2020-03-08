using UnityEngine;
using UnityEditor;

namespace UnityDebugViewer
{
    public class TestCustomMode : UnityDebugViewerIntermediaryEditor
    {
        /// <summary>
        /// 模式的名称
        /// </summary>
        private const string MODE_NAME = "TestCustomMode";

        /// <summary>
        /// 标记初始化的入口
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeTestCustomMode()
        {
            /// 自定义模式的权重，用于决定其在下拉列表中的显示顺序
            int order = 10;

            /// 添加自定义的模式
            UnityDebugViewerEditorManager.RegisterMode<TestCustomMode>(MODE_NAME, order);
        }

        /// <summary>
        /// 在点击Clear按钮时被调用
        /// </summary>
        public override void Clear()
        {
            base.Clear();

            UnityDebugViewerLogger.Log("Clear", MODE_NAME);
        }

        /// <summary>
        /// 在下拉列表中选择当前的模式时被调用
        /// </summary>
        public override void OnGUI()
        {
            base.OnGUI();

            if (GUILayout.Button(new GUIContent("Add Log"), EditorStyles.toolbarButton))
            {
                UnityDebugViewerLogger.Log("Add Log", MODE_NAME);
            }
        }

        /// <summary>
        /// 在脚本开始编译时被调用
        /// </summary>
        public override void StartCompiling()
        {
            base.StartCompiling();

            UnityDebugViewerLogger.Log("StartCompiling", MODE_NAME);
        }
    }
}