
using UnityEngine;

namespace UberStrike.Unity.ArtTools
{
    public static class CmuneEditorStyles
    {
        private static GUIStyle _whiteLabel;
        public static GUIStyle WhiteLabel
        {
            get
            {
                if (_whiteLabel == null)
                {
                    _whiteLabel = new GUIStyle(GUI.skin.label);
                    _whiteLabel.wordWrap = true;
                    _whiteLabel.normal.textColor = Color.white;
                }
                return _whiteLabel;
            }
        }

        private static GUIStyle _grayLabel;
        public static GUIStyle GrayLabel
        {
            get
            {
                if (_grayLabel == null)
                {
                    _grayLabel = new GUIStyle(GUI.skin.label);
                    _grayLabel.wordWrap = true;
                    _grayLabel.normal.textColor = Color.gray;
                }
                return _grayLabel;
            }
        }

        private static GUIStyle _redLabel;
        public static GUIStyle RedLabel
        {
            get
            {
                if (_redLabel == null)
                {
                    _redLabel = new GUIStyle(GUI.skin.label);
                    _redLabel.normal.textColor = Color.red;
                }
                return _redLabel;
            }
        }
    }
}