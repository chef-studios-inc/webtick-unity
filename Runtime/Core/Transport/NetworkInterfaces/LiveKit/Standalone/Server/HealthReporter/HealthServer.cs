using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace WebTick.Core.Server.HealthReporter
{
    public class HealthServer : MonoBehaviour
    {
        private Thread serverThread;
        private HttpListener listener;
        private uint port;
        public HealthInfo.Status status = HealthInfo.Status.Initializing;

        public void StartWithServerSettings(uint port)
        {
            Debug.LogFormat("Health Server starting with port: {0}", port);
            this.port = port;
            serverThread = new Thread(Listen);
            serverThread.Start();
        }

        private void Listen()
        {
            Debug.LogFormat("Health Server Listen");
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + port.ToString() + "/status/");
            listener.Start();

            foreach(string prefix in listener.Prefixes) {
                Debug.LogFormat("Listening on prefix: {0}", prefix);
            }

            while (true)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    var req = context.Request;
                    var resp = context.Response;

                    byte[] data = null;
                    if (status == HealthInfo.Status.Initializing)
                    {
                        data = GenerateResponse(resp, "initializing");
                    } else if(status == HealthInfo.Status.Running)
                    {
                        data = GenerateResponse(resp, "running");
                    } else if(status == HealthInfo.Status.Failed)
                    {
                        data = GenerateResponse(resp, "failed");
                    } else if(status == HealthInfo.Status.Complete)
                    {
                        data = GenerateResponse(resp, "complete");
                    } else
                    {
                        data = GenerateResponse(resp, "failed");
                    }

                    resp.OutputStream.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.Log(ex);
                }
            }
        }

        private byte[] GenerateResponse(HttpListenerResponse response, string payload)
        {
            byte[] data = Encoding.UTF8.GetBytes(payload);
            response.ContentType = "text/html";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.LongLength;
            return data;
        }

        private void OnDestroy()
        {
            if(serverThread != null)
            {
                serverThread.Abort();
                listener.Stop();
            }
        }
    }
}
