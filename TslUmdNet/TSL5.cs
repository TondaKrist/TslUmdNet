using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

namespace TslUmdNet
{
    public class TSL5
    {
        private UdpClient udpClient;
        private TcpListener tcpListener;
        
        
        private const byte DLE = 0xFE;
        private const byte STX = 0x02;

        private const int PBC_OFFSET = 0;
        private const int VER_OFFSET = 2;
        private const int FLAGS_OFFSET = 3;
        private const int SCREEN_OFFSET = 4;
        private const int INDEX_OFFSET = 6;
        private const int CONTROL_OFFSET = 8;
        private const int LENGTH_OFFSET = 10;
        
            


        public delegate void TallyDataEventHandler(TallyData tally);
        public event TallyDataEventHandler TallyDataRecieved;

        public void ListenUDP(int port)
        {
            udpClient = new UdpClient(port);

            udpClient.BeginReceive(HandleUDPClient, null);

            Console.WriteLine($"Listening for UDP messages on port {port}...");
        }

        public void ListenTCP(int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();

            Console.WriteLine($"Listening for TCP connections on port {port}...");

            while (true)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Task.Run(() => HandleTCPClient(client));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"TCP listener error: {ex}");
                }
            }
        }


        public void SendTallyUDP(string ip, int port, TallyData tally, bool? sequence = null)
        {
            try
            {
                if (string.IsNullOrEmpty(ip) || port == 0 || tally == null)
                {
                    throw new ArgumentException("Missing Parameter from call SendTallyUDP()");
                }

                if (sequence == null)
                {
                    Console.WriteLine("No DLE/STX sequence by default for UDP.");
                    sequence = false;
                }

                byte[] msg = ConstructPacket(tally, sequence);

                using (UdpClient client = new UdpClient())
                {
                    client.Send(msg, msg.Length, ip, port);
                    Console.WriteLine("TSL 5 UDP Data sent.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending TSL 5 UDP tally: {ex}");
                throw;
            }
        }

        public void SendTallyTCP(string ip, int port, TallyData tally, bool? sequence = null)
        {
            try
            {
                if (string.IsNullOrEmpty(ip) || port == 0 || tally == null)
                {
                    throw new ArgumentException("Missing Parameter from call SendTallyTCP()");
                }

                if (sequence == null)
                {
                    Console.WriteLine("Adding DLE/STX sequence by default for TCP.");
                    sequence = true;
                }

                byte[] msg = ConstructPacket(tally, sequence);

                using (TcpClient client = new TcpClient())
                {
                    client.Connect(ip, port);
                    NetworkStream stream = client.GetStream();
                    stream.Write(msg, 0, msg.Length);
                    stream.Close();
                    client.Close();
                    Console.WriteLine("TSL 5 TCP Data sent.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending TSL 5 TCP tally: {ex}");
                throw;
            }
        }

        

        //private methods



        private void HandleTCPClient(TcpClient client)
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    
                    ProcessTally(buffer, client.Client.RemoteEndPoint.ToString());
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"TCP Message received from {client.Client.RemoteEndPoint}: {message}");
                }
            }

            client.Close();
        }


        private void HandleUDPClient(IAsyncResult ar)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = udpClient.EndReceive(ar, ref endPoint);
                

                ProcessTally(receivedBytes, endPoint.Address.ToString());

                string messageString = Encoding.ASCII.GetString(receivedBytes);

                Console.WriteLine($"UDP Message received from {endPoint.Address}:{endPoint.Port}: {messageString}");

                udpClient.BeginReceive(HandleUDPClient, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP listener error: {ex}");
            }
        }

        private void ProcessTally(byte[] data, string source)
        {
            byte[] buf = data;
            var tally = new TallyData();

            // Strip DLE/STX if present and un-stuff any DLE stuffing
            if (buf[0] == DLE && buf[1] == STX)
            {
                buf = new ArraySegment<byte>(buf, 2, buf.Length - 2).ToArray();
                for (int index = 4; index < buf.Length; index++)
                {
                    if (buf[index] == DLE && buf[index + 1] == DLE)
                    {
                        var temp = new byte[index];
                        Array.Copy(buf, temp, index);
                        Array.Copy(buf, index + 2, temp, index, buf.Length - index - 2);
                        buf = temp;
                    }
                }
            }

            tally.Sender = source;
            tally.Pbc = BitConverter.ToInt16(buf, PBC_OFFSET);
            tally.Ver = buf[VER_OFFSET];
            tally.Flags = buf[FLAGS_OFFSET];
            tally.Screen = BitConverter.ToInt16(buf, SCREEN_OFFSET);
            tally.Index = BitConverter.ToInt16(buf, INDEX_OFFSET);
            tally.Control = BitConverter.ToInt16(buf, CONTROL_OFFSET);
            tally.Length = BitConverter.ToInt16(buf, LENGTH_OFFSET);
            tally.Display.Text = Encoding.ASCII.GetString(buf, LENGTH_OFFSET + 2, tally.Length);

            tally.Display.RhTally = (byte)(tally.Control >> 0 & 0b11);
            tally.Display.TextTally = (byte)(tally.Control >> 2 & 0b11);
            tally.Display.LhTally = (byte)(tally.Control >> 4 & 0b11);
            tally.Display.Brightness = (byte)(tally.Control >> 6 & 0b11);
            tally.Display.Reserved = (byte)(tally.Control >> 8 & 0b1111111);
            tally.Display.ControlData = (byte)(tally.Control >> 15 & 0b1);

            OnMessage(tally);


        }

     
        public byte[] ConstructPacket(TallyData tally, bool? sequence = null)
        {
            var screen = tally.Screen;
            var index = tally.Index;

            if (index == 0)
            {
                index = 1; // Default to index 1
            }

            ushort lenText = 0;
            byte[] textBytes = Array.Empty<byte>();

            if (tally.Display != null)
            {
                TallyDisplayData display = tally.Display;

                if (!string.IsNullOrEmpty(display.Text))
                {
                    textBytes = Encoding.ASCII.GetBytes(display.Text);
                    lenText = (ushort)textBytes.Length;
                }

                byte control = 0;
                control |= (byte)(display.RhTally << 0);
                control |= (byte)(display.TextTally << 2);
                control |= (byte)(display.LhTally << 4);
                control |= (byte)(display.Brightness << 6);

                byte[] bufUMD = new byte[12 + lenText];
                BitConverter.GetBytes(screen).CopyTo(bufUMD, SCREEN_OFFSET);
                BitConverter.GetBytes(index).CopyTo(bufUMD, INDEX_OFFSET);
                BitConverter.GetBytes(lenText).CopyTo(bufUMD, LENGTH_OFFSET);
                BitConverter.GetBytes(control).CopyTo(bufUMD, CONTROL_OFFSET);

                if (lenText > 0)
                {
                    textBytes.CopyTo(bufUMD, CONTROL_OFFSET + 2);
                }

                ushort msgLength = (ushort)(bufUMD.Length - 2);
                BitConverter.GetBytes(msgLength).CopyTo(bufUMD, PBC_OFFSET);

                if (sequence == true)
                {
                    MemoryStream packetStream = new MemoryStream();
                    packetStream.WriteByte(DLE);
                    packetStream.WriteByte(STX);

                    foreach (byte b in bufUMD)
                    {
                        if (b == DLE)
                        {
                            packetStream.WriteByte(DLE);
                            packetStream.WriteByte(DLE);
                        }
                        else
                        {
                            packetStream.WriteByte(b);
                        }
                    }

                    return packetStream.ToArray();
                }
                else
                {
                    return bufUMD;
                }
            }
            else
            {
                return null;
            }
        }




        private void OnMessage(TallyData tally)
        {
            TallyDataRecieved?.Invoke(tally);

        #if DEBUG 
            

            Console.WriteLine("Received Tally Message: ");
            Console.WriteLine($"Sender: {tally.Sender}");
            Console.WriteLine($"Pbc: {tally.Pbc}");
            Console.WriteLine($"Ver: {tally.Ver}");
            Console.WriteLine($"Flags: {tally.Flags}");
            Console.WriteLine($"Screen: {tally.Screen}");
            Console.WriteLine($"Index: {tally.Index}");
            Console.WriteLine($"Control: {tally.Control}");
            Console.WriteLine($"Length: {tally.Length}");
            Console.WriteLine($"Text: {tally.Display.Text}");
            Console.WriteLine($"RhTally: {tally.Display.RhTally}");
            Console.WriteLine($"TextTally: {tally.Display.TextTally}");
            Console.WriteLine($"LhTally: {tally.Display.LhTally}");
            Console.WriteLine($"Brightness: {tally.Display.Brightness}");
            Console.WriteLine($"Reserved: {tally.Display.Reserved}");
            Console.WriteLine($"ControlData: {tally.Display.ControlData}");
        
        #endif

    }



    }
}