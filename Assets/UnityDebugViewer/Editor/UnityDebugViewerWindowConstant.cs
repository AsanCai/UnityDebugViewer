using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityDebugViewer
{
    public static class UnityDebugViewerWindowStyleUtility 
    {
        private static GUISkin _skin;
        public static GUISkin skin
        {
            get
            {
                if(_skin == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        _skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
                    }
                    else
                    {
                        _skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
                    }
                }

                return _skin;
            }
        }


        private static GUIStyle _logFullMessageAreaStyle;
        public static GUIStyle logFullMessageAreaStyle
        {
            get
            {
                if (_logFullMessageAreaStyle == null)
                {
                    _logFullMessageAreaStyle = skin.GetStyle("Wizard Box");
                    _logFullMessageAreaStyle.wordWrap = true;
                    _logFullMessageAreaStyle.alignment = TextAnchor.UpperLeft;
                    _logFullMessageAreaStyle.padding = new RectOffset(5, 0, 0, 0);
                }

                return _logFullMessageAreaStyle;
            }
        }

        private static GUIStyle _collapsedNumLabelStyle;
        public static GUIStyle collapsedNumLabelStyle
        {
            get
            {
                if(_collapsedNumLabelStyle == null)
                {
                    _collapsedNumLabelStyle = skin.GetStyle("CN CountBadge");
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
                    string name = EditorGUIUtility.isProSkin ? "CN EntryError" : "CN EntryErrorIcon";
                    if (_errorIconStyle == null)
                    {
                        _errorIconStyle = skin.GetStyle(name);
                    }
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
                    string name = EditorGUIUtility.isProSkin ? "CN EntryError" : "CN EntryErrorIconSmall";
                    if (_errorIconSmallStyle == null)
                    {
                        _errorIconSmallStyle = skin.GetStyle(name);
                    }
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
                    string name = EditorGUIUtility.isProSkin ? "CN EntryWarn" : "CN EntryWarnIcon";
                    
                    if (_warningIconStyle == null)
                    {
                        _warningIconStyle = skin.GetStyle(name);
                    }
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
                    string name = EditorGUIUtility.isProSkin ? "CN EntryWarn" : "CN EntryWarnIconSmall";
                    if (_warningIconSmallStyle == null)
                    {
                        _warningIconSmallStyle = skin.GetStyle(name);
                    }
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
                    string name = EditorGUIUtility.isProSkin ? "CN EntryInfo" : "CN EntryInfoIcon";
                    if(_infoIconStyle == null)
                    {
                        _infoIconStyle = skin.GetStyle(name);
                    }
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
                    string name = EditorGUIUtility.isProSkin ? "CN EntryInfo" : "CN EntryInfoIconSmall";
                    //string name = "CN EntryInfoIconSmall";

                    if (_infoIconSmallStyle == null)
                    {
                        _infoIconSmallStyle = skin.GetStyle(name);
                    }
                }

                return _infoIconSmallStyle;
            }
        }

        private static GUIStyle _toolbarSearchTextStyle;
        public static GUIStyle toolbarSearchTextStyle
        {
            get
            {
                if (_toolbarSearchTextStyle == null)
                {
                    _toolbarSearchTextStyle = GUI.skin.GetStyle("ToolbarSeachTextField");
                }

                return _toolbarSearchTextStyle;
            }
        }

        private static GUIStyle _toolbarCancelButtonStyle;
        public static GUIStyle toolbarCancelButtonStyle
        {
            get
            {
                if (_toolbarCancelButtonStyle == null)
                {
                    _toolbarCancelButtonStyle = GUI.skin.GetStyle("ToolbarSeachCancelButton");
                }

                return _toolbarCancelButtonStyle;
            }
        }

        private static Texture2D _bgLogBoxOdd;
        public static Texture2D boxBgOdd
        {
            get
            {
                if (_bgLogBoxOdd == null)
                {
                    var style = skin.GetStyle("OL EntryBackOdd");
                    _bgLogBoxOdd = style.normal.background;
                }

                return _bgLogBoxOdd;
            }
        }

        private static Texture2D _boxLogBgEven;
        public static Texture2D boxBgEven
        {
            get
            {
                if (_boxLogBgEven == null)
                {
                    _boxLogBgEven = skin.GetStyle("OL EntryBackEven").normal.background;
                }

                return _boxLogBgEven;
            }
        }

        private static Texture2D _boxLogBgSelected;
        public static Texture2D boxBgSelected
        {
            get
            {
                if (_boxLogBgSelected == null)
                {
                    _boxLogBgSelected = skin.GetStyle("OL SelectedRow").normal.background;
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
    }
}