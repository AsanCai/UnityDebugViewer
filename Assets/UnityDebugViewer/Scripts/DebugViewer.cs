#if UNITY_VERSION_1 || UNITY_VERSION_2 || UNITY_VERSION_3 || UNITY_VERSION_4
#warning UNITY_VERSION has been set manually
#elif UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
#define UNITY_VERSION_1
#elif UNITY_5_0 || UNITY_5_1 || UNITY_5_2
#define UNITY_VERSION_2
#else
#define UNITY_VERSION_3
#endif
#if UNITY_2018_3_OR_NEWER
#define UNITY_VERSION_4
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityDebugViewer
{
    [Serializable]
    public struct ViewerImage
    {
        public Texture2D clearImage;
        public Texture2D collapseImage;
        public Texture2D clearOnNewSceneImage;
        public Texture2D showTimeImage;
        public Texture2D showSceneImage;
        public Texture2D userImage;
        public Texture2D showMemoryImage;
        public Texture2D softwareImage;
        public Texture2D dateImage;
        public Texture2D showFpsImage;
        public Texture2D infoImage;
        public Texture2D saveLogsImage;
        public Texture2D searchImage;
        public Texture2D copyImage;
        public Texture2D copyAllImage;
        public Texture2D closeImage;

        public Texture2D buildFromImage;
        public Texture2D systemInfoImage;
        public Texture2D graphicsInfoImage;
        public Texture2D backImage;

        public Texture2D logImage;
        public Texture2D warningImage;
        public Texture2D errorImage;

        public Texture2D barImage;
        public Texture2D button_activeImage;
        public Texture2D even_logImage;
        public Texture2D odd_logImage;
        public Texture2D selectedImage;

        public GUISkin reporterScrollerSkin;
    }

    public class DebugViewer : MonoBehaviour
    {
        enum ReportView
        {
            None,
            Logs,
            Info,
            Snapshot,
        }
        enum DetailView
        {
            None,
            StackTrace,
            Graph,
        }

        private List<DebugSampleData> _samplerDataList = new List<DebugSampleData>();

        /// <summary>
        /// 所有log
        /// </summary>
        private List<DebugLogData> _allLogList = new List<DebugLogData>();
        /// <summary>
        /// 所有折叠起来的log
        /// </summary>
        private List<DebugLogData> _collapsedLogList = new List<DebugLogData>();
        /// <summary>
        /// 当前正在使用的log
        /// </summary>
        private List<DebugLogData> _currentLogList = new List<DebugLogData>();

        #region 公有字段
        [HideInInspector]
        public bool show = false;

        public ViewerImage images;
        public Vector2 size = new Vector2(32, 32);
        #endregion

        #region 全局私有字段
        private ReportView _currentView = ReportView.Logs;
        
        private GUIContent _clearContent;
        private GUIContent _collapseContent;
        private GUIContent _clearOnNewSceneContent;
        private GUIContent _showTimeContent;
        private GUIContent _showSceneContent;
        private GUIContent _userContent;
        private GUIContent _showMemoryContent;
        private GUIContent _softwareContent;
        private GUIContent _dateContent;
        private GUIContent _showFpsContent;

        private GUIContent _infoContent;
        private GUIContent _saveLogsContent;
        private GUIContent _searchContent;
        private GUIContent _copyContent;
        private GUIContent _copyAllContent;
        private GUIContent _closeContent;

        private GUIContent _buildFromContent;
        private GUIContent _systemInfoContent;
        private GUIContent _graphicsInfoContent;
        private GUIContent _backContent;

        private GUIContent _logContent;
        private GUIContent _warningContent;
        private GUIContent _errorContent;
        private GUIStyle _barStyle;
        private GUIStyle _buttonActiveStyle;

        private GUIStyle _nonStyle;
        private GUIStyle _lowerLeftFontStyle;
        private GUIStyle _backStyle;
        private GUIStyle _evenLogStyle;
        private GUIStyle _oddLogStyle;
        private GUIStyle _logButtonStyle;
        private GUIStyle _selectedLogStyle;
        private GUIStyle _selectedLogFontStyle;
        private GUIStyle _stackLabelStyle;
        private GUIStyle _scrollerStyle;
        private GUIStyle _searchStyle;
        private GUIStyle _sliderBackStyle;
        private GUIStyle sliderThumbStyle;
        private GUISkin _toolbarScrollerSkin;
        private GUISkin _logScrollerSkin;
        private GUISkin _graphScrollerSkin;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            
        }
        #endregion

        #region GUI绘制相关
        private void initializeStyle()
        {
            int paddingX = (int)(size.x * 0.2f);
            int paddingY = (int)(size.y * 0.2f);
            _nonStyle = new GUIStyle();
            _nonStyle.clipping = TextClipping.Clip;
            _nonStyle.border = new RectOffset(0, 0, 0, 0);
            _nonStyle.normal.background = null;
            _nonStyle.fontSize = (int)(size.y / 2);
            _nonStyle.alignment = TextAnchor.MiddleCenter;

            _lowerLeftFontStyle = new GUIStyle();
            _lowerLeftFontStyle.clipping = TextClipping.Clip;
            _lowerLeftFontStyle.border = new RectOffset(0, 0, 0, 0);
            _lowerLeftFontStyle.normal.background = null;
            _lowerLeftFontStyle.fontSize = (int)(size.y / 2);
            _lowerLeftFontStyle.fontStyle = FontStyle.Bold;
            _lowerLeftFontStyle.alignment = TextAnchor.LowerLeft;

            _barStyle = new GUIStyle();
            _barStyle.border = new RectOffset(1, 1, 1, 1);
            _barStyle.normal.background = images.barImage;
            _barStyle.active.background = images.button_activeImage;
            _barStyle.alignment = TextAnchor.MiddleCenter;
            _barStyle.margin = new RectOffset(1, 1, 1, 1);
            _barStyle.clipping = TextClipping.Clip;
            _barStyle.fontSize = (int)(size.y / 2);

            _buttonActiveStyle = new GUIStyle();
            _buttonActiveStyle.border = new RectOffset(1, 1, 1, 1);
            _buttonActiveStyle.normal.background = images.button_activeImage;
            _buttonActiveStyle.alignment = TextAnchor.MiddleCenter;
            _buttonActiveStyle.margin = new RectOffset(1, 1, 1, 1);
            _buttonActiveStyle.fontSize = (int)(size.y / 2);

            _backStyle = new GUIStyle();
            _backStyle.normal.background = images.even_logImage;
            _backStyle.clipping = TextClipping.Clip;
            _backStyle.fontSize = (int)(size.y / 2);

            _evenLogStyle = new GUIStyle();
            _evenLogStyle.normal.background = images.even_logImage;
            _evenLogStyle.fixedHeight = size.y;
            _evenLogStyle.clipping = TextClipping.Clip;
            _evenLogStyle.alignment = TextAnchor.UpperLeft;
            _evenLogStyle.imagePosition = ImagePosition.ImageLeft;
            _evenLogStyle.fontSize = (int)(size.y / 2);

            _oddLogStyle = new GUIStyle();
            _oddLogStyle.normal.background = images.odd_logImage;
            _oddLogStyle.fixedHeight = size.y;
            _oddLogStyle.clipping = TextClipping.Clip;
            _oddLogStyle.alignment = TextAnchor.UpperLeft;
            _oddLogStyle.imagePosition = ImagePosition.ImageLeft;
            _oddLogStyle.fontSize = (int)(size.y / 2);

            _logButtonStyle = new GUIStyle();
            _logButtonStyle.fixedHeight = size.y;
            _logButtonStyle.clipping = TextClipping.Clip;
            _logButtonStyle.alignment = TextAnchor.UpperLeft;
            _logButtonStyle.fontSize = (int)(size.y / 2);
            _logButtonStyle.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            _selectedLogStyle = new GUIStyle();
            _selectedLogStyle.normal.background = images.selectedImage;
            _selectedLogStyle.fixedHeight = size.y;
            _selectedLogStyle.clipping = TextClipping.Clip;
            _selectedLogStyle.alignment = TextAnchor.UpperLeft;
            _selectedLogStyle.normal.textColor = Color.white;
            _selectedLogStyle.fontSize = (int)(size.y / 2);

            _selectedLogFontStyle = new GUIStyle();
            _selectedLogFontStyle.normal.background = images.selectedImage;
            _selectedLogFontStyle.fixedHeight = size.y;
            _selectedLogFontStyle.clipping = TextClipping.Clip;
            _selectedLogFontStyle.alignment = TextAnchor.UpperLeft;
            _selectedLogFontStyle.normal.textColor = Color.white;
            _selectedLogFontStyle.fontSize = (int)(size.y / 2);
            _selectedLogFontStyle.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            _stackLabelStyle = new GUIStyle();
            _stackLabelStyle.wordWrap = true;
            _stackLabelStyle.fontSize = (int)(size.y / 2);
            _stackLabelStyle.padding = new RectOffset(paddingX, paddingX, paddingY, paddingY);

            _scrollerStyle = new GUIStyle();
            _scrollerStyle.normal.background = images.barImage;

            _searchStyle = new GUIStyle();
            _searchStyle.clipping = TextClipping.Clip;
            _searchStyle.alignment = TextAnchor.LowerCenter;
            _searchStyle.fontSize = (int)(size.y / 2);
            _searchStyle.wordWrap = true;


            _sliderBackStyle = new GUIStyle();
            _sliderBackStyle.normal.background = images.barImage;
            _sliderBackStyle.fixedHeight = size.y;
            _sliderBackStyle.border = new RectOffset(1, 1, 1, 1);

            sliderThumbStyle = new GUIStyle();
            sliderThumbStyle.normal.background = images.selectedImage;
            sliderThumbStyle.fixedWidth = size.x;

            GUISkin skin = images.reporterScrollerSkin;

            _toolbarScrollerSkin = (GUISkin)GameObject.Instantiate(skin);
            _toolbarScrollerSkin.verticalScrollbar.fixedWidth = 0f;
            _toolbarScrollerSkin.horizontalScrollbar.fixedHeight = 0f;
            _toolbarScrollerSkin.verticalScrollbarThumb.fixedWidth = 0f;
            _toolbarScrollerSkin.horizontalScrollbarThumb.fixedHeight = 0f;

            _logScrollerSkin = (GUISkin)GameObject.Instantiate(skin);
            _logScrollerSkin.verticalScrollbar.fixedWidth = size.x * 2f;
            _logScrollerSkin.horizontalScrollbar.fixedHeight = 0f;
            _logScrollerSkin.verticalScrollbarThumb.fixedWidth = size.x * 2f;
            _logScrollerSkin.horizontalScrollbarThumb.fixedHeight = 0f;

            _graphScrollerSkin = (GUISkin)GameObject.Instantiate(skin);
            _graphScrollerSkin.verticalScrollbar.fixedWidth = 0f;
            _graphScrollerSkin.horizontalScrollbar.fixedHeight = size.x * 2f;
            _graphScrollerSkin.verticalScrollbarThumb.fixedWidth = 0f;
            _graphScrollerSkin.horizontalScrollbarThumb.fixedHeight = size.x * 2f;
        }

        private Rect _screenRect = Rect.zero;
        private Rect _logRect = Rect.zero;
        private Rect _stackRect = Rect.zero;
        private Rect _detailRect = Rect.zero;

        private Vector2 _stackRectTopLeft = Vector2.zero;

        public void DrawView()
        {
            if (!show)
            {
                return;
            }

            _screenRect.x = 0;
            _screenRect.y = 0;
            _screenRect.width = Screen.width;
            _screenRect.height = Screen.height;

            _logRect.x = 0f;
            _logRect.y = size.y * 2f;
            _logRect.width = Screen.width;
            _logRect.height = Screen.height * 0.75f - size.y * 2f;

            _stackRectTopLeft.x = 0f;
            _stackRect.x = 0f;
            _stackRectTopLeft.y = Screen.height * 0.75f;
            _stackRect.y = Screen.height * 0.75f;
            _stackRect.width = Screen.width;
            _stackRect.height = Screen.height * 0.25f - size.y;

            _detailRect.x = 0f;
            _detailRect.y = Screen.height - size.y * 3;
            _detailRect.width = Screen.width;
            _detailRect.height = size.y * 3;

            if(_currentView == ReportView.Info)
            {

            }else if(_currentView == ReportView.Logs)
            {

            }
        }


        #endregion
    }
}
