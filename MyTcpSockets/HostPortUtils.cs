using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MyTcpSockets
{
    public static class HostPortUtils
    {


        public static IPAddress TryParseIp(string hostOrIp)
        {
            var lines = hostOrIp.Split('.');

            if (lines.Length != 4)
                return null;

            var result = new byte[4];

            if (byte.TryParse(lines[0], out byte b0))
                result[0] = b0;
            else
                return null;

            if (byte.TryParse(lines[1], out byte b1))
                result[1] = b1;
            else
                return null;

            if (byte.TryParse(lines[2], out byte b2))
                result[2] = b2;
            else
                return null;

            if (byte.TryParse(lines[3], out byte b3))
            {
                result[3] = b3;
                return new IPAddress(result);
            }

            return null;
        }

        private static int TryParsePort(this string port)
        {
            
            if (int.TryParse(port, out var result))
                return result;


            return -1;

        }

        private static async Task<IReadOnlyList<IPEndPoint>> ResolveHost(this string host, int port)
        {
            var result = await Dns.GetHostAddressesAsync(host);

            if (result.Length == 0)
                throw new Exception("Can not resolve host: " + host);

            return result.Select(ip => new IPEndPoint(ip, port)).ToList();
        }




        public static ValueTask<IReadOnlyList<IPEndPoint>> ParseAndResolveHostPort(this string hostPort)
        {

            var hostAndPort = hostPort.Split(':');
            
            if (hostAndPort.Length != 2)
                throw new Exception("Invalid host and port: "+hostPort);


            var port = hostAndPort[1].TryParsePort();

            if (port < 0)
                throw new Exception("Invalid port at host and port line: "+hostPort);

            var ipAddress = TryParseIp(hostAndPort[0]);

            if (ipAddress == null)
                return new ValueTask<IReadOnlyList<IPEndPoint>>(hostAndPort[0].ResolveHost(port));

            return new ValueTask<IReadOnlyList<IPEndPoint>>(new[] {new IPEndPoint(ipAddress, port)}); ;

        }
        
    }
}