using System;
using DdpClient;
using DdpClient.EJson;
using DdpClient.Models;
using DdpClient.Models.Client;
using DdpClient.Models.Server;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
// ReSharper disable DelegateSubtraction

namespace DDPClient.Tests
{
    [TestFixture]
    public class DdpConnectionTests
    {
        [SetUp]
        public void Setup()
        {
            _mock = new Mock<WebSocketAdapterBase>();
            _connection = new DdpConnection(_mock.Object);
        }

        private class TestClass : DdpDocument
        {
            [JsonProperty("data")]
            public int Data { get; set; }
        }

        private Mock<WebSocketAdapterBase> _mock;
        private DdpConnection _connection;

        [Test]
        public void ShouldHandleConnectSuccess()
        {
            _mock.Setup(websocket => websocket.Connect(It.IsAny<string>())).Callback(() => _mock.Raise(webSocket => webSocket.Opened += null, null, EventArgs.Empty));

            var wasRaised = false;
            EventHandler<EventArgs> handler = null;
            handler = (sender, args) =>
            {
                wasRaised = true;
                _connection.Open -= handler;
            };
            _connection.Open += handler;

            _connection.Connect("");

            Assert.IsTrue(wasRaised);
            _mock.Verify(webSocket => webSocket.Connect(It.IsAny<string>()));
            _mock.Verify(webSocket => webSocket.SendJson(It.IsAny<ConnectModel>()));
        }

        [Test]
        public void ShouldHandleDdpConnectFailed()
        {
            var version = "2";

            _mock.Setup(websocket => websocket.SendJson(It.IsAny<ConnectModel>()))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("failed", "{\"msg\":\"failed\", \"version\":\"" + version + "\"}")));

            var wasRaised = false;
            EventHandler<ConnectResponse> handler = null;
            handler = (sender, response) =>
            {
                wasRaised = true;
                Assert.IsNull(response.Session);
                Assert.AreEqual(version, response.Failed.Version);
                _connection.Connected -= handler;
            };
            _connection.Connected += handler;

            _mock.Raise(webSocket => webSocket.Opened += null, null, EventArgs.Empty);

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldHandleDdpConnectSuccess()
        {
            var session = "SomeSession";

            _mock.Setup(websocket => websocket.SendJson(It.IsAny<ConnectModel>()))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("connected", "{\"msg\":\"connected\", \"session\":\"" + session + "\"}")));

            var wasRaised = false;
            EventHandler<ConnectResponse> handler = null;
            handler = (sender, response) =>
            {
                wasRaised = true;
                Assert.IsNull(response.Failed);
                Assert.AreEqual(session, response.Session);
                _connection.Connected -= handler;
            };
            _connection.Connected += handler;

            _mock.Raise(webSocket => webSocket.Opened += null, null, EventArgs.Empty);

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldHandleMethod()
        {
            var id = "ShouldHandleMethod";
            var methodName = "MethodName";
            var parameter = 5;
            _connection.IdGenerator = () => id;

            _connection.CallMethod(methodName, parameter);

            _mock.Verify(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)));
        }

        [Test]
        public void ShouldHandleMethodDynamic()
        {
            var id = "ShouldHandleMethodDynamic";
            var methodName = "MethodName";
            var parameter = 5;
            _connection.IdGenerator = () => id;

            var response = new MethodResponse
            {
                Id = id,
                Error = null,
                Result = new JObject(new JProperty("someResult", 10))
            };

            _mock.Setup(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("result", JsonConvert.SerializeObject(response))));

            var wasCalled = false;
            Action<MethodResponse> callback = mResponse =>
            {
                wasCalled = true;
                Assert.IsNull(mResponse.Error);
                Assert.IsNotNull(mResponse.Result);
            };

            _connection.Call(methodName, callback, parameter);

            _mock.Verify(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)));
            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void ShouldHandleMethodFixedObject()
        {
            const string id = "ShouldHandleMethodFixedObject";
            const string methodName = "MethodName";
            const int parameter = 5;
            const int result = 10;

            _connection.IdGenerator = () => id;

            var mockResult = "{\"msg\":\"result\",\"id\":\"" + id + "\",\"result\":{\"data\": 10}}";

            _mock.Setup(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("result", mockResult)));

            var wasCalled = false;
            Action<DetailedError, TestClass> callback = (error, mResponse) =>
            {
                wasCalled = true;
                Assert.IsNull(error);
                Assert.AreEqual(result, mResponse.Data);
            };

            _connection.Call(methodName, callback, parameter);

            _mock.Verify(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)));
            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void ShouldHandleMethodFixedValue()
        {
            const string id = "ShouldHandleMethodFixedValue";
            const string methodName = "MethodName";
            const int parameter = 5;
            const int result = 10;

            _connection.IdGenerator = () => id;

            var mockResult = "{\"msg\":\"result\",\"id\":\"" + id + "\",\"result\": 10}";

            _mock.Setup(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)))
                .Callback(() => _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("result", mockResult)));

            var wasCalled = false;
            Action<DetailedError, int> callback = (error, mResponse) =>
            {
                wasCalled = true;
                Assert.IsNull(error);
                Assert.AreEqual(result, mResponse);
            };

            _connection.Call(methodName, callback, parameter);

            _mock.Verify(webSocket => webSocket.SendJson(It.Is<MethodModel>(model => model.Id == id)));
            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void ShouldHandlePingFromServerWithId()
        {
            var id = "SomeID";
            var ping = new PingModel
            {
                Id = id
            };

            var wasRaised = false;
            EventHandler<PingModel> handler = null;
            handler = delegate(object sender, PingModel pingMsg)
            {
                wasRaised = true;
                Assert.AreEqual(id, pingMsg.Id);
                _connection.Ping -= handler;
            };
            _connection.Ping += handler;

            _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("ping", JsonConvert.SerializeObject(ping)));
            _mock.Verify(socket => socket.SendJson(It.Is<PongModel>(pong => pong.Id == id)));

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldHandlePingFromServerWithoutId()
        {
            var ping = new PingModel();

            var wasRaised = false;
            EventHandler<PingModel> handler = null;
            handler = delegate(object sender, PingModel pingMsg)
            {
                wasRaised = true;
                Assert.IsNull(pingMsg.Id);
                _connection.Ping -= handler;
            };
            _connection.Ping += handler;

            _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("ping", JsonConvert.SerializeObject(ping)));
            _mock.Verify(socket => socket.SendJson(It.Is<PongModel>(pong => pong.Id == null)));

            Assert.IsTrue(wasRaised);
        }


        [Test]
        public void ShouldHandlePongFromServerWithId()
        {
            var id = "ShouldHandlePongFromServerWithId";

            var wasRaised = false;
            EventHandler<PongModel> handler = null;
            handler = delegate(object sender, PongModel pong)
            {
                wasRaised = true;
                Assert.AreEqual(id, pong.Id);
                _connection.Pong -= handler;
            };
            _connection.Pong += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("pong", "{\"msg\": \"pong\", \"id\": \"" + id + "\"}"));

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldHandlePongFromServerWithoutId()
        {
            var wasRaised = false;
            EventHandler<PongModel> handler = null;
            handler = delegate(object sender, PongModel pong)
            {
                wasRaised = true;
                Assert.IsNull(pong.Id);
                _connection.Pong -= handler;
            };
            _connection.Pong += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("pong", "{\"msg\": \"pong\"}"));

            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldLoginWithEmailSuccess()
        {
            const string methodId = "SomeRandomId";
            _connection.IdGenerator = () => methodId;

            var response = new MethodResponse
            {
                Id = methodId,
                Error = null,
                Result = new JObject(new JProperty("tokenExpires", JToken.FromObject(new DdpDate())), new JProperty("token", "SomeTokenId"))
            };
            _mock.Setup(websocket => websocket.SendJson(It.IsAny<MethodModel>()))
                .Callback(() => _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("result", JsonConvert.SerializeObject(response))));

            var email = "some@email.de";
            var password = "SecretPassword";

            var wasRaised = false;
            EventHandler<LoginResponse> handler = null;
            handler = delegate(object sender, LoginResponse loginResponse)
            {
                wasRaised = true;
                Assert.IsFalse(loginResponse.HasError());
                Assert.IsNotNull(loginResponse.Token);
                Assert.IsNotNull(loginResponse.TokenExpires);
                _connection.Login -= handler;
            };
            _connection.Login += handler;

            _connection.LoginWithEmail(email, password);

            _mock.Verify(websocket => websocket.SendJson(It.Is<MethodModel>(method =>
                method.Id == methodId &&
                method.Method == "login" &&
                method.Params.Length == 1 &&
                method.Params[0] is BasicLoginModel<EmailUser>)));
            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldLoginWithTokenSuccess()
        {
            var methodId = "SomeRandomId";
            _connection.IdGenerator = () => methodId;

            var response = new MethodResponse
            {
                Id = methodId,
                Error = null,
                Result = new JObject(new JProperty("tokenExpires", JToken.FromObject(new DdpDate())), new JProperty("token", "SomeTokenId"))
            };
            _mock.Setup(websocket => websocket.SendJson(It.IsAny<MethodModel>()))
                .Callback(() => _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("result", JsonConvert.SerializeObject(response))));

            var token = "SomeRandomToken";

            var wasRaised = false;
            EventHandler<LoginResponse> handler = null;
            handler = delegate(object sender, LoginResponse loginResponse)
            {
                wasRaised = true;
                Assert.IsFalse(loginResponse.HasError());
                Assert.IsNotNull(loginResponse.Token);
                Assert.IsNotNull(loginResponse.TokenExpires);
                _connection.Login -= handler;
            };
            _connection.Login += handler;

            _connection.LoginWithToken(token);

            _mock.Verify(websocket => websocket.SendJson(It.Is<MethodModel>(method =>
                method.Id == methodId &&
                method.Method == "login" &&
                method.Params.Length == 1 &&
                method.Params[0] is BasicTokenModel)));
            Assert.IsTrue(wasRaised);
        }

        [Test]
        public void ShouldLoginWithUsernameSuccess()
        {
            var methodId = "SomeRandomId";
            _connection.IdGenerator = () => methodId;

            var response = new MethodResponse
            {
                Id = methodId,
                Error = null,
                Result = new JObject(new JProperty("tokenExpires", JToken.FromObject(new DdpDate())), new JProperty("token", "SomeTokenId"))
            };
            _mock.Setup(websocket => websocket.SendJson(It.IsAny<MethodModel>()))
                .Callback(() => _mock.Raise(socket => socket.DdpMessage += null, null, new DdpMessage("result", JsonConvert.SerializeObject(response))));

            var username = "TestUser";
            var password = "SecretPassword";

            var wasRaised = false;
            EventHandler<LoginResponse> handler = null;
            handler = delegate(object sender, LoginResponse loginResponse)
            {
                wasRaised = true;
                Assert.IsFalse(loginResponse.HasError());
                Assert.IsNotNull(loginResponse.Token);
                Assert.IsNotNull(loginResponse.TokenExpires);
                _connection.Login -= handler;
            };
            _connection.Login += handler;

            _connection.LoginWithUsername(username, password);

            _mock.Verify(websocket => websocket.SendJson(It.Is<MethodModel>(method =>
                method.Id == methodId &&
                method.Method == "login" &&
                method.Params.Length == 1 &&
                method.Params[0] is BasicLoginModel<UsernameUser>)));
            Assert.IsTrue(wasRaised);
        }


        [Test]
        public void ShouldPingWithId()
        {
            var id = "SomeId";
            _connection.PingServer(id);

            _mock.Verify(websocket => websocket.SendJson(It.Is<PingModel>(ping => ping.Id == id)));
        }

        [Test]
        public void ShouldPingWithoutId()
        {
            _connection.PingServer();

            _mock.Verify(websocket => websocket.SendJson(It.Is<PingModel>(ping => ping.Id == null)));
        }

        [Test]
        public void ShouldSubscriberHandleAdded()
        {
            var id = "SomeRandomId";
            var data = 5;
            var collection = "tasks";

            var res = new TestClass
            {
                Id = id,
                Data = data
            };
            var mockResult = "{\"msg\":\"added\",\"id\":\"" + id + "\",\"collection\":\"" + collection + "\",\"fields\": " + JsonConvert.SerializeObject(res) + "}";

            var ddpSubscriber = _connection.GetSubscriber<TestClass>(collection);

            var wasRaised = false;
            EventHandler<SubAddedModel<TestClass>> handler = null;
            handler = delegate (object sender, SubAddedModel<TestClass> added)
            {
                wasRaised = true;
                Assert.AreEqual(id, added.Id);
                Assert.AreEqual(id, added.Object.Id);
                Assert.AreEqual(data, added.Object.Data);
                ddpSubscriber.Added -= handler;
            };
            ddpSubscriber.Added += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("added", mockResult));

            Assert.IsTrue(wasRaised);
            ddpSubscriber.Dispose();
        }

        [Test]
        public void ShouldSubscriberHandleChanged()
        {
            var id = "SomeRandomId";
            var data = 5;
            var collection = "tasks";

            var res = new TestClass
            {
                Id = id,
                Data = data
            };
            var mockResult = "{\"msg\":\"changed\",\"id\":\"" + id + "\",\"collection\":\"" + collection + "\",\"fields\": " + JsonConvert.SerializeObject(res) + "}";

            var ddpSubscriber = _connection.GetSubscriber<TestClass>(collection);

            var wasRaised = false;
            EventHandler<SubChangedModel<TestClass>> handler = null;
            handler = delegate (object sender, SubChangedModel<TestClass> changed)
            {
                wasRaised = true;
                Assert.AreEqual(id, changed.Id);
                Assert.AreEqual(id, changed.Object.Id);
                Assert.AreEqual(data, changed.Object.Data);
                ddpSubscriber.Changed -= handler;
            };
            ddpSubscriber.Changed += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("changed", mockResult));

            Assert.IsTrue(wasRaised);
            ddpSubscriber.Dispose();
        }

        [Test]
        public void ShouldSubscriberHandleRemoved()
        {
            var id = "SomeRandomId";
            var collection = "tasks";

            var mockResult = "{\"msg\":\"removed\",\"id\":\"" + id + "\",\"collection\":\"" + collection + "\"}";

            var ddpSubscriber = _connection.GetSubscriber<TestClass>(collection);

            var wasRaised = false;
            EventHandler<SubRemovedModel> handler = null;
            handler = delegate (object sender, SubRemovedModel changed)
            {
                wasRaised = true;
                Assert.AreEqual(id, changed.Id);
                ddpSubscriber.Removed -= handler;
            };
            ddpSubscriber.Removed += handler;

            _mock.Raise(webSocket => webSocket.DdpMessage += null, null, new DdpMessage("removed", mockResult));

            Assert.IsTrue(wasRaised);
            ddpSubscriber.Dispose();
        }
    }
}