using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityDebugViewer
{
    public struct UnityDebugViewerWindowConstant 
    {
        private static GUIStyle _collapsedNumLabelStyle;
        public static GUIStyle collapsedNumLabelStyle
        {
            get
            {
                if(_collapsedNumLabelStyle == null)
                {
                    _collapsedNumLabelStyle = GUI.skin.GetStyle("CN CountBadge");
                }

                return _collapsedNumLabelStyle;
            }
        }

        private static GUIStyle _errorIconStyle;
        public static GUIStyle errorIconStyle
        {
            get
            {
                if (_errorIconStyle == null)
                {
                    _errorIconStyle = GUI.skin.GetStyle("CN EntryErrorIcon");
                }

                return _errorIconStyle;
            }
        }

        private static GUIStyle _errorIconSmallStyle;
        public static GUIStyle errorIconSmallStyle
        {
            get
            {
                if (_errorIconSmallStyle == null)
                {
                    _errorIconSmallStyle = GUI.skin.GetStyle("CN EntryErrorIconSmall");
                }

                return _errorIconSmallStyle;
            }
        }

        private static GUIStyle _warningIconStyle;
        public static GUIStyle warningIconStyle
        {
            get
            {
                if (_warningIconStyle == null)
                {
                    _warningIconStyle = GUI.skin.GetStyle("CN EntryWarnIcon");
                }

                return _warningIconStyle;
            }
        }

        private static GUIStyle _warningIconSmallStyle;
        public static GUIStyle warningIconSmallStyle
        {
            get
            {
                if (_warningIconSmallStyle == null)
                {
                    _warningIconSmallStyle = GUI.skin.GetStyle("CN EntryWarnIconSmall");
                }

                return _warningIconSmallStyle;
            }
        }

        private static GUIStyle _infoIconStyle;
        public static GUIStyle infoIconStyle
        {
            get
            {
                if (_infoIconStyle == null)
                {
                    _infoIconStyle = GUI.skin.GetStyle("CN EntryInfoIcon");
                }

                return _infoIconStyle;
            }
        }

        private static GUIStyle _infoIconSmallStyle;
        public static GUIStyle infoIconSmallStyle
        {
            get
            {
                if (_infoIconSmallStyle == null)
                {
                    _infoIconSmallStyle = GUI.skin.GetStyle("CN EntryInfoIconSmall");
                }

                return _infoIconSmallStyle;
            }
        }

        private static Texture2D _bgLogBoxOdd;
        public static Texture2D boxLogBgOdd
        {
            get
            {
                if (_bgLogBoxOdd == null)
                {
                    _bgLogBoxOdd = GUI.skin.GetStyle("OL EntryBackOdd").normal.background;
                }

                return _bgLogBoxOdd;
            }
        }

        private static Texture2D _boxLogBgEven;
        public static Texture2D boxLogBgEven
        {
            get
            {
                if (_boxLogBgEven == null)
                {
                    _boxLogBgEven = GUI.skin.GetStyle("OL EntryBackEven").normal.background;
                }

                return _boxLogBgEven;
            }
        }

        private static Texture2D _boxLogBgSelected;
        public static Texture2D boxLogBgSelected
        {
            get
            {
                if (_boxLogBgSelected == null)
                {
                    _boxLogBgSelected = GUI.skin.GetStyle("OL SelectedRow").normal.background;
                }

                return _boxLogBgSelected;
            }
        }

        private static Texture2D _bgResizer;
        public static Texture2D bgResizer
        {
            get
            {
                if (_bgResizer == null)
                {
                    _bgResizer = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
                }

                return _bgResizer;
            }
        }

        private static Texture2D _bgTextArea;
        public static Texture2D bgTextArea
        {
            get
            {
                if (_bgTextArea == null)
                {
                    _bgTextArea = GUI.skin.GetStyle("ProjectBrowserIconAreaBg").normal.background;
                }

                return _bgTextArea;
            }
        }

        private static Texture2D _bgStackBoxOdd;
        public static Texture2D boxgStackBgOdd
        {
            get
            {
                if (_bgStackBoxOdd == null)
                {
                    _bgStackBoxOdd = GUI.skin.GetStyle("CN EntryBackOdd").normal.background;
                }

                return _bgStackBoxOdd;
            }
        }

        private static Texture2D _boxStackBgEven;
        public static Texture2D boxStackBgEven
        {
            get
            {
                if (_boxStackBgEven == null)
                {
                    _boxStackBgEven = GUI.skin.GetStyle("CN EntryBackEven").normal.background;
                }

                return _boxStackBgEven;
            }
        }

    }
}