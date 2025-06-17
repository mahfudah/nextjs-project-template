using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RawPrinterClient
{
    public class WebSocketClient
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        public bool IsConnected { get; private set; }

        public event EventHandler<string> MessageReceived;
        public event EventHandler<bool> ConnectionStatusChanged;

        public WebSocketClient()
        {
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task ConnectAsync(string url)
        {
            if (IsConnected)
                return;

            try
            {
                await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);
                IsConnected = true;
                ConnectionStatusChanged?.Invoke(this, true);
                _ = ReceiveMessagesAsync();
            }
            catch (Exception)
            {
                IsConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected)
                return;

            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cancellationTokenSource.Token);
            }
            catch { }
            finally
            {
                IsConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
                _webSocket = new ClientWebSocket();
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];

            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                    }
                    else
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        MessageReceived?.Invoke(this, message);
                    }
                }
            }
            catch
            {
                await DisconnectAsync();
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!IsConnected)
                throw new InvalidOperationException("WebSocket is not connected");

            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
        }
    }
}
