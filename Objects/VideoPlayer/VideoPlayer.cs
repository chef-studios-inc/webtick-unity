using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace WebTick.Objects
{
    public class VideoPlayer : MonoBehaviour
    {
        public string url;
        public Renderer destination;
        private int textureId;
        private Texture2D texture;

#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void VideoPlayer_Create(string gameObject, string url);

        [DllImport("__Internal")]
        private static extern void VideoPlayer_Destroy(string gameObject);

        [DllImport("__Internal")]
        private static extern void VideoPlayer_Play(string gameObject);

        [DllImport("__Internal")]
        private static extern void VideoPlayer_SetVolume(string gameObject, float volume);

        [DllImport("__Internal")]
        private static extern void VideoPlayer_SetFPS(string gameObject, int FPS);
#endif

        void Start()
        {
#if UNITY_WEBGL
            VideoPlayer_Create(gameObject.name, url);
#endif
        }

        private void OnDestroy()
        {
            texture = null;
#if UNITY_WEBGL
            VideoPlayer_Destroy(gameObject.name);
#endif
        }

        [Preserve]
        private void OnVideoCreated(int texture)
        {
#if UNITY_WEBGL
            this.textureId = texture;
            // TODO width and height?
            var t = Texture2D.CreateExternalTexture(1, 1, TextureFormat.RGBA32, false, false, (IntPtr)texture);
            this.texture = t;
            destination.material.mainTexture = t;
            VideoPlayer_Play(gameObject.name);
            SetVolume(0);
            SetFPS(20);
#endif
        }

        public void SetVolume(float volume)
        {
#if UNITY_WEBGL
            VideoPlayer_SetVolume(gameObject.name, volume);
#endif
        }

        public void SetFPS(int fps)
        {
#if UNITY_WEBGL
            VideoPlayer_SetFPS(gameObject.name, fps);
#endif
        }
    }
}
