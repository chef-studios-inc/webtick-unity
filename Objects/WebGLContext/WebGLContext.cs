using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace WebTick.Objects
{
    public class WebGLContext : MonoBehaviour
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void WebGLContext_Initialize(string gameObject);

        [DllImport("__Internal")]
        private static extern void WebGLContext_GetCtxResponse(int req_id);

        [DllImport("__Internal")]
        private static extern void WebGLContext_GetTextureResponse(int req_id);

        [DllImport("__Internal")]
        private static extern void WebGLContext_DestroyTextureResponse(int req_id, int texture_id);
#endif

        private void Start()
        {
#if UNITY_WEBGL
            WebGLContext_Initialize(gameObject.name);
#endif
        }

        [Preserve]
        private void GetCtx(int req_id)
        {
#if UNITY_WEBGL
            WebGLContext_GetCtxResponse(req_id);
#endif
        }

        [Preserve]
        private void GetTexture(int req_id)
        {
#if UNITY_WEBGL
            WebGLContext_GetTextureResponse(req_id);
#endif
        }

        [Preserve]
        private void DestroyTexture(int req_id, int texture_id)
        {
#if UNITY_WEBGL
            WebGLContext_DestroyTextureResponse(req_id, texture_id);
#endif
        }
    }
}
