using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DioRemoteControl.Common.Protocol
{
    /// <summary>
    /// 모든 메시지의 기본 클래스
    /// </summary>
    public abstract class MessageBase
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        [JsonProperty("encrypted")]
        public bool Encrypted { get; set; } = false;

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static T FromJson<T>(string json) where T : MessageBase
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }

    /// <summary>
    /// 상담원 등록 메시지
    /// </summary>
    public class RegisterAgentMessage : MessageBase
    {
        public RegisterAgentMessage()
        {
            Type = "register_agent";
        }

        [JsonProperty("agent_id")]
        public string AgentId { get; set; }

        [JsonProperty("agent_name")]
        public string AgentName { get; set; }
    }

    /// <summary>
    /// 고객 연결 메시지
    /// </summary>
    public class ConnectClientMessage : MessageBase
    {
        public ConnectClientMessage()
        {
            Type = "connect_client";
        }

        [JsonProperty("auth_code")]
        public string AuthCode { get; set; }

        [JsonProperty("client_name")]
        public string ClientName { get; set; }

        [JsonProperty("client_info")]
        public string ClientInfo { get; set; }
    }

    /// <summary>
    /// 인증번호 수신 메시지
    /// </summary>
    public class AuthCodeMessage : MessageBase
    {
        [JsonProperty("auth_code")]
        public string AuthCode { get; set; }

        [JsonProperty("session_id")]
        public int SessionId { get; set; }
    }

    /// <summary>
    /// 화면 데이터 메시지
    /// </summary>
    public class ScreenDataMessage : MessageBase
    {
        public ScreenDataMessage()
        {
            Type = "screen_data";
        }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        [JsonProperty("screen_data")]
        public string ScreenData { get; set; } // Base64 encoded image

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("monitor_index")]
        public int MonitorIndex { get; set; } = 0;

        [JsonProperty("quality")]
        public int Quality { get; set; } = 75;

        [JsonProperty("color_depth")]
        public string ColorDepth { get; set; } = "true"; // "64", "256", "true"

        [JsonProperty("frame_number")]
        public long FrameNumber { get; set; }
    }

    /// <summary>
    /// 마우스 이벤트 메시지
    /// </summary>
    public class MouseEventMessage : MessageBase
    {
        public MouseEventMessage()
        {
            Type = "mouse_event";
        }

        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        [JsonProperty("event")]
        public MouseEvent Event { get; set; }
    }

    public class MouseEvent
    {
        [JsonProperty("action")]
        public string Action { get; set; } // "move", "left_down", "left_up", "right_down", "right_up", "wheel"

        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("delta")]
        public int Delta { get; set; } // For wheel events

        [JsonProperty("screen_width")]
        public int ScreenWidth { get; set; }

        [JsonProperty("screen_height")]
        public int ScreenHeight { get; set; }
    }

    /// <summary>
    /// 키보드 이벤트 메시지
    /// </summary>
    public class KeyboardEventMessage : MessageBase
    {
        public KeyboardEventMessage()
        {
            Type = "keyboard_event";
        }

        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        [JsonProperty("event")]
        public KeyboardEvent Event { get; set; }
    }

    public class KeyboardEvent
    {
        [JsonProperty("action")]
        public string Action { get; set; } // "key_down", "key_up", "key_press"

        [JsonProperty("key_code")]
        public int KeyCode { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("modifiers")]
        public KeyModifiers Modifiers { get; set; }
    }

    public class KeyModifiers
    {
        [JsonProperty("ctrl")]
        public bool Ctrl { get; set; }

        [JsonProperty("alt")]
        public bool Alt { get; set; }

        [JsonProperty("shift")]
        public bool Shift { get; set; }

        [JsonProperty("win")]
        public bool Win { get; set; }
    }

    /// <summary>
    /// 채팅 메시지
    /// </summary>
    public class ChatMessage : MessageBase
    {
        public ChatMessage()
        {
            Type = "chat_message";
        }

        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("sender")]
        public string Sender { get; set; }
    }

    /// <summary>
    /// 파일 전송 메시지
    /// </summary>
    public class FileTransferMessage : MessageBase
    {
        public FileTransferMessage()
        {
            Type = "file_transfer";
        }

        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; } // "init", "chunk", "complete", "cancel"

        [JsonProperty("file_name")]
        public string FileName { get; set; }

        [JsonProperty("file_size")]
        public long FileSize { get; set; }

        [JsonProperty("file_type")]
        public string FileType { get; set; }

        [JsonProperty("chunk")]
        public string Chunk { get; set; } // Base64 encoded

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }

    /// <summary>
    /// 시스템 명령 메시지
    /// </summary>
    public class SystemCommandMessage : MessageBase
    {
        public SystemCommandMessage()
        {
            Type = "system_command";
        }

        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; } // "start_menu", "task_manager", "cmd", "ctrl_alt_del", "explorer", "show_desktop", "reboot"

        [JsonProperty("params")]
        public object Params { get; set; }
    }

    /// <summary>
    /// 클라이언트 연결 알림
    /// </summary>
    public class ClientConnectedMessage : MessageBase
    {
        [JsonProperty("client")]
        public ClientInfo Client { get; set; }
    }

    public class ClientInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("info")]
        public string Info { get; set; }

        [JsonProperty("connected_at")]
        public string ConnectedAt { get; set; }

        [JsonProperty("os")]
        public string OS { get; set; }

        [JsonProperty("resolution")]
        public string Resolution { get; set; }

        [JsonProperty("monitors")]
        public int Monitors { get; set; }
    }

    /// <summary>
    /// 설정 변경 메시지
    /// </summary>
    public class SettingsMessage : MessageBase
    {
        public SettingsMessage()
        {
            Type = "settings";
        }

        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        [JsonProperty("quality")]
        public int Quality { get; set; }

        [JsonProperty("color_depth")]
        public string ColorDepth { get; set; }

        [JsonProperty("network_priority")]
        public bool NetworkPriority { get; set; } // true: 네트워크 우선, false: 화면 갱신 우선

        [JsonProperty("capture_cursor")]
        public bool CaptureCursor { get; set; }
    }

    /// <summary>
    /// Heartbeat 메시지
    /// </summary>
    public class HeartbeatMessage : MessageBase
    {
        public HeartbeatMessage()
        {
            Type = "heartbeat";
        }
    }

    /// <summary>
    /// 에러 메시지
    /// </summary>
    public class ErrorMessage : MessageBase
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
