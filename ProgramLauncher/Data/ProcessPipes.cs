using System;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DjSoft.Tools.ProgramLauncher.Data
{
    #region PipeSet - funkční sada komunikačních kanálů
    public class PipeSet : IDisposable
    {
        public PipeSet(string pipeName)
        {
            __ServerPipe = new ServerPipe(pipeName, p => p.StartStringReaderAsync());
            __ServerPipe.Connected += __ServerPipe_Connected;
            __ServerPipe.DataReceived += __ServerPipe_DataReceived;

            __ClientPipe = new ClientPipe(".", pipeName, p => p.StartStringReaderAsync());
        }

        private void __ServerPipe_Connected(object sender, EventArgs e)
        {
            
        }

        private void __ServerPipe_DataReceived(object sender, PipeEventArgs e)
        {
            
        }

        private ServerPipe __ServerPipe;
        private ClientPipe __ClientPipe;

        public void Dispose()
        {
            __ClientPipe?.Close();
            __ClientPipe = null;
            __ServerPipe?.Close();
            __ServerPipe = null;
        }
    }


    /*  ze zdroje

    public Form1()
        {
            InitializeComponent();
            serverPipes = new List<ServerPipe>();
            clientPipes = new List<ClientPipe>();
            tbServerSenders = new List<TextBox>() { tbServerSend };
            tbServerReceivers = new List<TextBox>() { tbServerReceived };
            tbClientSenders = new List<TextBox>() { tbClientSend };
            tbClientReceivers = new List<TextBox>() { tbClientReceived };
        }

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            CreateServer();
            btnCreateClient.Enabled = true;
            btnStartServer.Enabled = false;
        }

        private ServerPipe CreateServer()
        {
            int serverIdx = serverPipes.Count;
			ServerPipe serverPipe = new ServerPipe("Test", p => p.StartStringReaderAsync());
            serverPipes.Add(serverPipe);

			serverPipe.DataReceived += (sndr, args) =>
				this.BeginInvoke(() =>
					tbServerReceivers[serverIdx].Text = args.String);

            serverPipe.Connected += (sndr, args) =>
                this.BeginInvoke(() =>
                    {
                        CreateServerUI();
                        nextServer = CreateServer();
                    });

            return serverPipe;
        }

        private void btnCreateClient_Click(object sender, EventArgs e)
        {
            btnSendToClient.Enabled = true;
            btnSendToServer.Enabled = true;
            int clientIdx = clientPipes.Count;
            ClientPipe clientPipe = new ClientPipe(".", "Test", p=>p.StartStringReaderAsync());
            clientPipes.Add(clientPipe);

            CreateClientUI();

			clientPipe.DataReceived += (sndr, args) =>
				this.BeginInvoke(() =>
					tbClientReceivers[clientIdx].Text = args.String);

            clientPipe.Connect();
        }

        private void btnSendToServer_Click(object sender, EventArgs e)
        {
            int clientIdx = Convert.ToInt32(((Control)sender).Tag);
            clientPipes[clientIdx].WriteString(tbClientSenders[clientIdx].Text);
        }

        private void btnSendToClient_Click(object sender, EventArgs e)
        {
            int serverIdx = Convert.ToInt32(((Control)sender).Tag);
            serverPipes[serverIdx].WriteString(tbServerSenders[serverIdx].Text);
        }

        protected void CreateServerUI()
        {
            if (serverPipes.Count > 1)
            {
                CreateServerControls(serverPipes.Count - 1);
            }
        }

        protected void CreateClientUI()
        {
            if (clientPipes.Count > 1)
            {
                CreateClientControls(clientPipes.Count - 1);
            }
        }

        protected void CreateServerControls(int n)
        {
            ServerConnection cc = new ServerConnection();
            Button btnSend = (Button)cc.Controls.Find("btnSendToClient", true)[0];
            btnSend.Click += btnSendToClient_Click;
            btnSend.Tag = n;
            tbServerSenders.Add((TextBox)cc.Controls.Find("tbServerSend", true)[0]);
            tbServerReceivers.Add((TextBox)cc.Controls.Find("tbServerReceived", true)[0]);
            cc.Location = new Point(gbServer.Location.X - 3, gbServer.Location.Y + (gbServer.Size.Height + 10) * n);
            Controls.Add(cc);
        }

        protected void CreateClientControls(int n)
        {
            ClientConnection cc = new ClientConnection();
            Button btnSend = (Button)cc.Controls.Find("btnSendToServer", true)[0];
            btnSend.Click += btnSendToServer_Click;
            btnSend.Tag = n;
            tbClientSenders.Add((TextBox)cc.Controls.Find("tbClientSend", true)[0]);
            tbClientReceivers.Add((TextBox)cc.Controls.Find("tbClientReceived", true)[0]);
            cc.Location = new Point(gbClient.Location.X - 3, gbClient.Location.Y + (gbClient.Size.Height + 10) * n);
            Controls.Add(cc);
        }




    */
    #endregion
    #region ServerPipe 
    public class ServerPipe : BasicPipe
    {
        public event EventHandler<EventArgs> Connected;

        protected NamedPipeServerStream __ServerPipeStream;
        public string PipeName { get; protected set; }

        public ServerPipe(string pipeName, Action<BasicPipe> asyncReaderStart)
        {
            this.AsyncReaderStart = asyncReaderStart;
            PipeName = pipeName;

            __ServerPipeStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            PipeStream = __ServerPipeStream;
            __ServerPipeStream.BeginWaitForConnection(new AsyncCallback(PipeConnected), null);
        }

        protected void PipeConnected(IAsyncResult ar)
        {
            __ServerPipeStream.EndWaitForConnection(ar);
            Connected?.Invoke(this, new EventArgs());
            AsyncReaderStart(this);
        }
    }
    #endregion
    #region ClientPipe
    public class ClientPipe : BasicPipe
    {
        protected NamedPipeClientStream clientPipeStream;

        public ClientPipe(string serverName, string pipeName, Action<BasicPipe> asyncReaderStart)
        {
            this.AsyncReaderStart = asyncReaderStart;
            clientPipeStream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            PipeStream = clientPipeStream;
        }

        public void Connect()
        {
            clientPipeStream.Connect();
            AsyncReaderStart(this);
        }
    }
    #endregion
    #region BasicPipe 
    public abstract class BasicPipe
    {
        public event EventHandler<PipeEventArgs> DataReceived;
        public event EventHandler<EventArgs> PipeClosed;

        protected PipeStream PipeStream;
        protected Action<BasicPipe> AsyncReaderStart;

        public BasicPipe()
        {
        }

        public void Close()
        {
            PipeStream.WaitForPipeDrain();
            PipeStream.Close();
            PipeStream.Dispose();
            PipeStream = null;
        }

        /// <summary>
        /// Reads an array of bytes, where the first [n] bytes (based on the server's intsize) indicates the number of bytes to read 
        /// to complete the packet.
        /// </summary>
        public void StartByteReaderAsync()
        {
            StartByteReaderAsync((b) => DataReceived?.Invoke(this, new PipeEventArgs(b, b.Length)));
        }

        /// <summary>
        /// Reads an array of bytes, where the first [n] bytes (based on the server's intsize) indicates the number of bytes to read 
        /// to complete the packet, and invokes the DataReceived event with a string converted from UTF8 of the byte array.
        /// </summary>
        public void StartStringReaderAsync()
        {
            StartByteReaderAsync((b) =>
            {
                string str = Encoding.UTF8.GetString(b).TrimEnd('\0');
                DataReceived?.Invoke(this, new PipeEventArgs(str));
            });
        }

        public void Flush()
        {
            PipeStream.Flush();
        }

        public Task WriteString(string str)
        {
            return WriteBytes(Encoding.UTF8.GetBytes(str));
        }

        public Task WriteBytes(byte[] bytes)
        {
            var blength = BitConverter.GetBytes(bytes.Length);
            var bfull = blength.Concat(bytes).ToArray();

            return PipeStream.WriteAsync(bfull, 0, bfull.Length);
        }

        protected void StartByteReaderAsync(Action<byte[]> packetReceived)
        {
            int intSize = sizeof(int);
            byte[] bDataLength = new byte[intSize];

            PipeStream.ReadAsync(bDataLength, 0, intSize).ContinueWith(t =>
            {
                int len = t.Result;

                if (len == 0)
                {
                    PipeClosed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    int dataLength = BitConverter.ToInt32(bDataLength, 0);
                    byte[] data = new byte[dataLength];

                    PipeStream.ReadAsync(data, 0, dataLength).ContinueWith(t2 =>
                    {
                        len = t2.Result;

                        if (len == 0)
                        {
                            PipeClosed?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            packetReceived(data);
                            StartByteReaderAsync(packetReceived);
                        }
                    });
                }
            });
        }
    }
    #endregion
    #region PipeEventArgs 
    public class PipeEventArgs
    {
        public byte[] Data { get; protected set; }
        public int Len { get; protected set; }
        public string String { get; protected set; }

        public PipeEventArgs(string str)
        {
            String = str;
        }

        public PipeEventArgs(byte[] data, int len)
        {
            Data = data;
            Len = len;
        }
    }

    #endregion
}
