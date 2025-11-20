using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using DioRemoteControl.Common.Protocol;

namespace DioRemoteControl.Core
{
    /// <summary>
    /// WebSocket 클라이언트 클래스
    /// </summary>
    public class WebSocketClient
    {
        private WebSocket _ws;
        private readonly string _serverUrl;
        private bool _isConnected;
        private System.Timers.Timer _heartbeatTimer;

        // 이벤트
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<string> Error;

        public bool IsConnected => _isConnected && _ws?.ReadyState == WebSocketState.Open;

        public WebSocketClient(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        /// <summary>
        /// 서버에 연결
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                _ws = new WebSocket(_serverUrl);

                // 이벤트 핸들러 등록
                _ws.OnOpen += (sender, e) =>
                {
                    _isConnected = true;
                    StartHeartbeat();
                    Connected?.Invoke(this, EventArgs.Empty);
                };

                _ws.OnMessage += (sender, e) =>
                {
                    HandleMessage(e.Data);
                };

                _ws.OnError += (sender, e) =>
                {
                    Error?.Invoke(this, e.Message);
                };

                _ws.OnClose += (sender, e) =>
                {
                    _isConnected = false;
                    StopHeartbeat();
                    Disconnected?.Invoke(this, EventArgs.Empty);
                };

                // 비동기 연결
                await Task.Run(() => _ws.Connect());

                return IsConnected;
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, $"Connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 연결 종료
        /// </summary>
        public void Disconnect()
        {
            try
            {
                StopHeartbeat();
                _ws?.Close();
                _isConnected = false;
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, $"Disconnect error: {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지 전송
        /// </summary>
        public void Send(MessageBase message)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            try
            {
                var json = message.ToJson();
                _ws.Send(json);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, $"Send error: {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지 전송 (JSON 문자열)
        /// </summary>
        public void SendRaw(string json)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            try
            {
                _ws.Send(json);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, $"Send error: {ex.Message}");
            }
        }

        /// <summary>
        /// 수신된 메시지 처리
        /// </summary>
        private void HandleMessage(string data)
        {
            try
            {
                var json = JObject.Parse(data);
                var type = json["type"]?.ToString();

                if (string.IsNullOrEmpty(type))
                    return;

                MessageReceived?.Invoke(this, new MessageReceivedEventArgs
                {
                    Type = type,
                    Data = data,
                    Json = json
                });
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, $"Message handling error: {ex.Message}");
            }
        }

        /// <summary>
        /// Heartbeat 시작
        /// </summary>
        private void StartHeartbeat()
        {
            _heartbeatTimer = new System.Timers.Timer(30000); // 30초마다
            _heartbeatTimer.Elapsed += (sender, e) =>
            {
                if (IsConnected)
                {
                    try
                    {
                        Send(new HeartbeatMessage());
                    }
                    catch { }
                }
            };
            _heartbeatTimer.Start();
        }

        /// <summary>
        /// Heartbeat 중지
        /// </summary>
        private void StopHeartbeat()
        {
            _heartbeatTimer?.Stop();
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }
    }

    /// <summary>
    /// 메시지 수신 이벤트 인수
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Type { get; set; }
        public string Data { get; set; }
        public JObject Json { get; set; }
    }
}