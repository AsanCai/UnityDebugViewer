/// Copyright (C) 2020 AsanCai   
/// All rights reserved
/// Email: 969850420@qq.com


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

        private static Texture2D _errorIconSmallTexture;
        public static Texture2D errorIconSmallTexture
        {
            get
            {
                if (_errorIconSmallTexture == null)
                {
                    _errorIconSmallTexture = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
                }

                return _errorIconSmallTexture;
            }
        }

        private static Texture _warningIconSmallTexture;
        public static Texture warningIconSmallTexture
        {
            get
            {
                if (_warningIconSmallTexture == null)
                {
                    _warningIconSmallTexture = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
                }

                return _warningIconSmallTexture;
            }
        }

        private static Texture _infoIconSmallTexture;
        public static Texture infoIconSmallTexture
        {
            get
            {
                if (_infoIconSmallTexture == null)
                {
                    _infoIconSmallTexture = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;
                }

                return _infoIconSmallTexture;
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

        private static GUIStyle _oddLogBoxtyle;
        public static GUIStyle oddLogBoxtyle
        {
            get
            {
                if (_oddLogBoxtyle == null)
                {
                    _oddLogBoxtyle = new GUIStyle(oddEntryStyle);
                    _oddLogBoxtyle.wordWrap = true;
                    _oddLogBoxtyle.clipping = TextClipping.Clip;
                    _oddLogBoxtyle.padding = new RectOffset(35, 10, 5, 5);
                }

                return _oddLogBoxtyle;
            }
        }


        private static GUIStyle _evenLogBoxtyle;
        public static GUIStyle evenLogBoxtyle
        {
            get
            {
                if (_evenLogBoxtyle == null)
                {
                    _evenLogBoxtyle = new GUIStyle(evenEntryStyle);
                    _evenLogBoxtyle.wordWrap = true;
                    _evenLogBoxtyle.clipping = TextClipping.Clip;
                    _evenLogBoxtyle.padding = new RectOffset(35, 10, 5, 5);
                }

                return _evenLogBoxtyle;
            }
        }

        private static GUIStyle _selectedLogBoxStyle;
        public static GUIStyle selectedLogBoxStyle
        {
            get
            {
                if (_selectedLogBoxStyle == null)
                {
                    _selectedLogBoxStyle = new GUIStyle(selectedEntryStyle);
                    _selectedLogBoxStyle.wordWrap = true;
                    _selectedLogBoxStyle.clipping = TextClipping.Clip;
                    _selectedLogBoxStyle.padding = new RectOffset(35, 10, 5, 5);
                }

                return _selectedLogBoxStyle;
            }
        }

        private static GUIStyle _inactiveLogBoxStyle;
        public static GUIStyle inactiveLogBoxStyle
        {
            get
            {
                if (_inactiveLogBoxStyle == null)
                {
                    _inactiveLogBoxStyle = new GUIStyle(inactiveEntryStyle);
                    _inactiveLogBoxStyle.wordWrap = true;
                    _inactiveLogBoxStyle.clipping = TextClipping.Clip;
                    _inactiveLogBoxStyle.padding = new RectOffset(35, 10, 5, 5);
                }

                return _inactiveLogBoxStyle;
            }
        }

        private static GUIStyle _oddStackBoxStyle;
        public static GUIStyle oddStackBoxStyle
        {
            get
            {
                if (_oddStackBoxStyle == null)
                {
                    _oddStackBoxStyle = new GUIStyle(oddEntryStyle);
                    _oddStackBoxStyle.alignment = TextAnchor.MiddleLeft;
                    _oddStackBoxStyle.wordWrap = true;
                    _oddStackBoxStyle.padding = new RectOffset(10, 0, 0, 0);
                    _oddStackBoxStyle.richText = true;
                }

                return _oddStackBoxStyle;
            }
        }


        private static GUIStyle _evenStackBoxStyle;
        public static GUIStyle evenStackBoxStyle
        {
            get
            {
                if (_evenStackBoxStyle == null)
                {
                    _evenStackBoxStyle = new GUIStyle(evenEntryStyle);
                    _evenStackBoxStyle.alignment = TextAnchor.MiddleLeft;
                    _evenStackBoxStyle.wordWrap = true;
                    _evenStackBoxStyle.padding = new RectOffset(10, 0, 0, 0);
                    _evenStackBoxStyle.richText = true;
                }

                return _evenStackBoxStyle;
            }
        }

        private static GUIStyle _selectedStackBoxStyle;
        public static GUIStyle selectedStackBoxStyle
        {
            get
            {
                if (_selectedStackBoxStyle == null)
                {
                    _selectedStackBoxStyle = new GUIStyle(selectedEntryStyle);
                    _selectedStackBoxStyle.alignment = TextAnchor.MiddleLeft;
                    _selectedStackBoxStyle.wordWrap = true;
                    _selectedStackBoxStyle.padding = new RectOffset(10, 0, 0, 0);
                    _selectedStackBoxStyle.richText = true;
                }

                return _selectedStackBoxStyle;
            }
        }

        private static GUIStyle _inactiveStackBoxStyle;
        public static GUIStyle inactiveStackBoxStyle
        {
            get
            {
                if (_inactiveStackBoxStyle == null)
                {
                    _inactiveStackBoxStyle = new GUIStyle(inactiveEntryStyle);
                    _inactiveStackBoxStyle.alignment = TextAnchor.MiddleLeft;
                    _inactiveStackBoxStyle.wordWrap = true;
                    _inactiveStackBoxStyle.padding = new RectOffset(10, 0, 0, 0);
                    _inactiveStackBoxStyle.richText = true;
                }

                return _inactiveStackBoxStyle;
            }
        }

        private static GUIStyle _oddTreeRowStyle;
        public static GUIStyle oddTreeRowStyle
        {
            get
            {
                if (_oddTreeRowStyle == null)
                {
                    _oddTreeRowStyle = new GUIStyle(oddEntryStyle);
                    _oddTreeRowStyle.alignment = TextAnchor.MiddleLeft;
                    _oddTreeRowStyle.padding = new RectOffset(0, 0, 0, 0);
                }

                return _oddTreeRowStyle;
            }
        }


        private static GUIStyle _evenTreeRowStyle;
        public static GUIStyle evenTreeRowStyle
        {
            get
            {
                if (_evenTreeRowStyle == null)
                {
                    _evenTreeRowStyle = new GUIStyle(evenEntryStyle);
                    _evenTreeRowStyle.alignment = TextAnchor.MiddleLeft;
                    _evenTreeRowStyle.padding = new RectOffset(0, 0, 0, 0);
                }

                return _evenTreeRowStyle;
            }
        }

        private static GUIStyle _selectedTreeRowStyle;
        public static GUIStyle selectedTreeRowStyle
        {
            get
            {
                if (_selectedTreeRowStyle == null)
                {
                    _selectedTreeRowStyle = new GUIStyle(selectedEntryStyle);
                    _selectedTreeRowStyle.alignment = TextAnchor.MiddleLeft;
                    _selectedTreeRowStyle.padding = new RectOffset(0, 0, 0, 0);
                }

                return _selectedTreeRowStyle;
            }
        }

        private static GUIStyle _inactiveTreeRowStyle;
        public static GUIStyle inactiveTreeRowStyle
        {
            get
            {
                if (_inactiveTreeRowStyle == null)
                {
                    _inactiveTreeRowStyle = new GUIStyle(inactiveEntryStyle);
                    _inactiveTreeRowStyle.alignment = TextAnchor.MiddleLeft;
                    _inactiveTreeRowStyle.padding = new RectOffset(0, 0, 0, 0);
                }

                return _inactiveTreeRowStyle;
            }
        }

        private static GUIStyle _oddEntryStyle;
        private static GUIStyle oddEntryStyle
        {
            get
            {
                if (_oddEntryStyle == null)
                {
                    _oddEntryStyle = skin.GetStyle("CN EntryBackOdd");
                    if(_oddEntryStyle == null)
                    {
                        _oddEntryStyle = new GUIStyle();
                    }
                    _oddEntryStyle.richText = true;
                }

                return _oddEntryStyle;
            }
        }


        private static GUIStyle _evenEntryStyle;
        private static GUIStyle evenEntryStyle
        {
            get
            {
                if (_evenEntryStyle == null)
                {
                    _evenEntryStyle = skin.GetStyle("CN EntryBackEven");
                    if (_evenEntryStyle == null)
                    {
                        _evenEntryStyle = new GUIStyle();
                    }
                    _evenEntryStyle.richText = true;
                }

                return _evenEntryStyle;
            }
        }

        private static GUIStyle _selectedEntryStyle;
        private static GUIStyle selectedEntryStyle
        {
            get
            {
                if (_selectedEntryStyle == null)
                {
                    _selectedEntryStyle = skin.GetStyle("LODSliderRangeSelected");
                    if (_selectedEntryStyle == null)
                    {
                        _selectedEntryStyle = new GUIStyle();
                    }
                    _selectedEntryStyle.richText = true;
                }

                return _selectedEntryStyle;
            }
        }

        private static GUIStyle _inactiveEntryStyle;
        private static GUIStyle inactiveEntryStyle
        {
            get
            {
                if (_inactiveEntryStyle == null)
                {
                    if (_inactiveEntryStyle == null)
                    {
                        _inactiveEntryStyle = skin.GetStyle("LODBlackBox");
                    }
                    _inactiveEntryStyle.richText = true;
                }

                return _inactiveEntryStyle;
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