using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DdpClient
{
    public abstract class WebSocketAdapterBase
    {
        public abstract event EventHandler<EventArgs> Opened;
        public abstract event EventHandler<EventArgs> Closed;
        public abstract event EventHandler<Exception> Error;

        public virtual event EventHandler<DdpMessage> DdpMessage;

        public abstract void Connect(string url);

        public abstract Task ConnectAsync(string url);

        public abstract void Close();

        public abstract bool IsAlive();

        protected abstract void Send(string message);


        protected virtual void OnMessageReceived(string data)
        {
            var body = JObject.Parse(data);
            if (body["msg"] == null)
                return;
            var msg = body["msg"].ToObject<string>();
            DdpMessage?.Invoke(this, new DdpMessage(msg, data));
        }

        public virtual void SendJson(object body)
        {
            var data = JsonConvert.SerializeObject(body);
            Send(data);
        }
    }
}