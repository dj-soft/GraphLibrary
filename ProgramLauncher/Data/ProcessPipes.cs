using System;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjSoft.Tools.ProgramLauncher.Data
{

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

            pipeStream = __ServerPipeStream;
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
            pipeStream = clientPipeStream;
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

        protected PipeStream pipeStream;
        protected Action<BasicPipe> AsyncReaderStart;

        public BasicPipe()
        {
        }

        public void Close()
        {
            pipeStream.WaitForPipeDrain();
            pipeStream.Close();
            pipeStream.Dispose();
            pipeStream = null;
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
            pipeStream.Flush();
        }

        public Task WriteString(string str)
        {
            return WriteBytes(Encoding.UTF8.GetBytes(str));
        }

        public Task WriteBytes(byte[] bytes)
        {
            var blength = BitConverter.GetBytes(bytes.Length);
            var bfull = blength.Concat(bytes).ToArray();

            return pipeStream.WriteAsync(bfull, 0, bfull.Length);
        }

        protected void StartByteReaderAsync(Action<byte[]> packetReceived)
        {
            int intSize = sizeof(int);
            byte[] bDataLength = new byte[intSize];

            pipeStream.ReadAsync(bDataLength, 0, intSize).ContinueWith(t =>
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

                    pipeStream.ReadAsync(data, 0, dataLength).ContinueWith(t2 =>
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
