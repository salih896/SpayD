#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameClient.EditorTools
{
    /// <summary>
    /// Yalnızca Unity Editör: oyun oynanırken Game penceresinin yakınlaştırmasını (Scale) 1x'te tutar.
    /// Fare tekerleği hem oyun kamerasını hem Game penceresini yakınlaştırıyordu; pencere yakınlaşınca
    /// kenarlar kesilip arayüz (seviye, kaynaklar, düğmeler) kaymış görünüyordu. Oyunun kendisini etkilemez.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewScaleLock
    {
        private static Type gameViewType;
        private static FieldInfo zoomAreaField, scaleField;
        private static double nextCheck;

        static GameViewScaleLock()
        {
            try
            {
                gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                zoomAreaField = gameViewType?.GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            catch (Exception) { gameViewType = null; }
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || gameViewType == null || zoomAreaField == null) return;
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 0.1;
            try
            {
                foreach (var w in Resources.FindObjectsOfTypeAll(gameViewType))
                {
                    var area = zoomAreaField.GetValue(w);
                    if (area == null) continue;
                    if (scaleField == null) scaleField = area.GetType().GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (scaleField == null) return;
                    var s = (Vector2)scaleField.GetValue(area);
                    if (Mathf.Abs(s.x - 1f) > 0.001f || Mathf.Abs(s.y - 1f) > 0.001f)
                    {
                        scaleField.SetValue(area, Vector2.one);
                        ((EditorWindow)w).Repaint();
                    }
                }
            }
            catch (Exception) { /* Unity sürümü farklıysa sessizce geç */ }
        }
    }
}
#endif
