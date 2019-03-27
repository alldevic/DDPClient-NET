using System;
using DdpClient.Models;

namespace DdpClient
{
    public class DdpMethodHandler<T>
    {
        private readonly WebSocketAdapterBase _webSocketAdapterBase;
        private readonly Action<DetailedError, T> _callback;
        public string Id { get; set; }

        public DdpMethodHandler(WebSocketAdapterBase webSocketAdapterBase, Action<DetailedError, T> callback, string id)
        {
            _webSocketAdapterBase = webSocketAdapterBase;
            _callback = callback;
            _webSocketAdapterBase.DdpMessage += OnDdpMessage;

            Id = id;
        }

        private void OnDdpMessage(object sender, DdpMessage ddpMessage)
        {
            if (ddpMessage.Msg != "result" || ddpMessage.Body["id"].ToObject<string>() != Id)
            {
                return;
            }
            
            _webSocketAdapterBase.DdpMessage -= OnDdpMessage;
            var body = ddpMessage.Body;
            if (body["error"] == null)
                _callback(null, body["result"].ToObject<T>());
            else
                _callback(body["error"].ToObject<DetailedError>(), default(T));
        }
    }
}