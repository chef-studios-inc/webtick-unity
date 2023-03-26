using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace WebTick.Objects
{
    public class WebGLContext : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void WebGLContext_Initialize(string gameObject);

        [DllImport("__Internal")]
        private static extern void WebGLContext_GetCtxResponse(int req_id);

        [DllImport("__Internal")]
        private static extern void WebGLContext_GetTextureResponse(int req_id);

        [DllImport("__Internal")]
        private static extern void WebGLContext_DestroyTextureResponse(int req_id, int texture_id);

        private void Start()
        {
            WebGLContext_Initialize(gameObject.name);
        }

        [Preserve]
        private void GetCtx(int req_id)
        {
            WebGLContext_GetCtxResponse(req_id);
        }

        [Preserve]
        private void GetTexture(int req_id)
        {
            WebGLContext_GetTextureResponse(req_id);
        }

        [Preserve]
        private void DestroyTexture(int req_id, int texture_id)
        {
            WebGLContext_DestroyTextureResponse(req_id, texture_id);
        }
    }
}
