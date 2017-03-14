using Android.App;
using Android.OS;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace SP360Proxy
{
    [Activity(Label = "SP360Proxy", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public static string LogTag = "SP360Proxy";

        private HttpListener server;
        private List<HttpListenerResponse> connections;
        private WebResponse cameraResponse;
        private CancellationTokenSource cancellation;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.server = new HttpListener();
            this.server.Prefixes.Add("http://*:8080/");

            this.connections = new List<HttpListenerResponse>();

            var hasCamera = new AutoResetEvent(false);

            Task.Factory.StartNew(() =>
            {
                var cameraRequest = WebRequest.Create("http://172.16.0.254:9176") as HttpWebRequest;
                this.cameraResponse = cameraRequest.GetResponse();
                hasCamera.Set();
            });

            Task.Factory.StartNew(() =>
            {
                this.server.Start();
                while (true)
                {
                    if (this.cameraResponse == null)
                    {
                        hasCamera.WaitOne();
                    }
                    var context = this.server.GetContext();
                    var res = context.Response;
                    foreach (var key in this.cameraResponse.Headers.AllKeys)
                    {
                        res.Headers.Add(key, this.cameraResponse.Headers[key]);
                    }
                    this.connections.Add(res);
                    ResetStream();
                }
            });
        }

        private void CopyStream(Stream src, IEnumerable<Stream> dests)
        {
            var cancellationToken = this.cancellation.Token;
            src.CopyToAsync(new MultiOutputStream(dests, OnWriteError), 131072, cancellationToken);
            Log.Info(LogTag, "write started");
        }

        private void OnWriteError(IEnumerable<Tuple<Stream, Exception>> errors)
        {
            foreach (var error in errors)
            {
                Log.Warn(LogTag, Java.Lang.Throwable.FromException(error.Item2), "write error");
            }
            this.connections.RemoveAll(x => errors.Any(error => error.Item1 == x.OutputStream));
            ResetStream();
        }

        private void ResetStream()
        {
            if (this.cancellation != null)
            {
                Log.Info(LogTag, "write cancelled");
                this.cancellation.Cancel();
            }
            this.cancellation = new CancellationTokenSource();
            CopyStream(this.cameraResponse.GetResponseStream(), this.connections.Select(x => x.OutputStream));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (this.cameraResponse != null)
            {
                this.cameraResponse.Close();
            }
            foreach (var con in this.connections)
            {
                con.Close();
            }
            this.server.Close();
        }
    }
}
