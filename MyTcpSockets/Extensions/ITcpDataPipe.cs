using System.Threading.Tasks;

namespace MyTcpSockets.Extensions
{
    public interface ITcpDataPipe
    {
        void PushData(TcpDataPiece tcpDataPiece);

        Task StopAsync();
    }

}