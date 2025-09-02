using Serilog;
using System.Net.Sockets;
using System.Text;

namespace PinPadTester
{
    public enum ControlCodes : byte
    {
        STX = 0x02,
        ETX = 0x03,
        ACK = 0x06,
        NAK = 0x15,
        ENQ = 0x05,
        FS = 0x1C,
        GS = 0x1D,
        EOT = 0x04,

        // PAX Specific ??
        US = 0x1F,
        RS = 0x1E,
        COMMA = 0x2C,
        COLON = 0x3A,
        PTGS = 0x7C,
        LF = 0x0A
    }


    public class GlobalTest
    {
        private readonly string ipAddress;
        private readonly int port;
        private readonly int timeout;
        public GlobalTest(string ip, int port)
        {
            ipAddress = ip;
            this.port = port;
            timeout = 30000;
        }

        public static byte[] BuildRawUpaRequest(string jsonRequest)
        {

            jsonRequest = jsonRequest.Replace("ecrId", "EcrId");

            jsonRequest = jsonRequest.Replace("<LF>", "\r\n");
            jsonRequest = jsonRequest.Replace("{", "{\r\n");
            jsonRequest = jsonRequest.Replace("}", "}\r\n");
            jsonRequest = jsonRequest.Replace(",", ",\r\n");
            var buffer = new List<byte>();

            // Begin Message
            buffer.Add((byte)ControlCodes.STX);
            buffer.Add(0x0A);

            // Add the Message
            if (!string.IsNullOrEmpty(jsonRequest))
            {
                foreach (char c in jsonRequest)
                    buffer.Add((byte)c);
            }

            // End the Message
            buffer.Add((byte)ControlCodes.LF);
            buffer.Add((byte)ControlCodes.ETX);
            buffer.Add((byte)ControlCodes.LF);
            return buffer.ToArray();
        }

        public async Task Go()
        {
            #region Connect
            using var client = new TcpClient();
            var cancel = new CancellationTokenSource(timeout);
            await client.ConnectAsync(ipAddress, port, cancel.Token);

            var stream = client.GetStream();

            #endregion
            var id = Random.Shared.Next(100000, 999999);
            var ping = "{\"message\":\"MSG\",\"data\":{\"command\":\"Ping\",\"EcrId\":\"13\",\"requestId\":\"379166\"}}";
            ping = ping.Replace("379166", id.ToString());
            var hackReq = "  ";
            var buffer = BuildRawUpaRequest(ping);
            var task = stream.WriteAsync(buffer, 0, buffer.Length);

            if (!task.Wait(timeout))
            {
                throw new Exception($"Terminal did not respond in the given timeout. RequestID = {id}");
            }


            List<byte[]> responses = GetTerminalResponses(stream);
            if (responses != null)
            {
                foreach (var messageBytes in responses)
                {
                    var jsonObject = Encoding.UTF8.GetString(TrimResponse(messageBytes));
                    if (jsonObject != "{    \"message\": \"ACK\",    \"data\": \"\"}")
                    {
                        Log.Information("Received message {jsonObject}", jsonObject);
                    }

                }
            }
        }

        private byte[] TrimResponse(byte[] value)
        {
            return System.Text.Encoding.UTF8.GetBytes(
                System.Text.Encoding.UTF8.GetString(value)
                    .TrimStart((char)ControlCodes.STX, (char)ControlCodes.LF)
                    .TrimEnd((char)ControlCodes.LF, (char)ControlCodes.ETX));
        }

        private int ReadWithTimeout(NetworkStream stream, byte[] buffer)
        {
            var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
            if (Task.WhenAny(readTask, Task.Delay(timeout)).Result == readTask)
            {
                return readTask.Result;
            }
            else
            {
                throw new Exception("Terminal stream read did not respond in the given timeout.");
            }
        }

        private List<byte[]> GetTerminalResponses(NetworkStream stream)
        {
            byte[] buffer = new byte[32768];

            int bytesReceived = ReadWithTimeout(stream, buffer);

            List<byte[]> responses = new List<byte[]>();
            List<byte> tempBuffer = new List<byte>();
            bool inMessage = false;
            if (bytesReceived > 0)
            {
                byte[] readBuffer = new byte[bytesReceived];
                Array.Copy(buffer, readBuffer, bytesReceived);

                ControlCodes code = (ControlCodes)readBuffer[0];
                if (code == ControlCodes.STX)
                {
                    for (int i = 0; i < bytesReceived; i++)
                    {
                        byte b = buffer[i];
                        if (b == (byte)ControlCodes.STX)
                        {
                            tempBuffer.Clear();
                            inMessage = true;
                        }
                        else if (b == (byte)ControlCodes.ETX)
                        {
                            if (inMessage)
                            {
                                responses.Add(tempBuffer.ToArray());
                                tempBuffer.Clear();
                                inMessage = false;
                            }
                        }
                        else if (b != 0x0A && inMessage)
                        {
                            tempBuffer.Add(b);
                        }
                    }
                    return responses;
                }
                else throw new Exception(string.Format("Unknown message received: {0}", code));
            }
            return null;
        }
    }

}
