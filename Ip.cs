//https://discord.com/channels/1160907911501991946/1233009182857494588
//nuko8964
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;

namespace Sessions;

public class Ip
{
    private delegate nint CNetworkSystemUpdatePublicIp(nint a1);
    private CNetworkSystemUpdatePublicIp? _networkSystemUpdatePublicIp;
    private readonly nint _networkSystem = NativeAPI.GetValveInterface(0, "NetworkSystemVersion001");

    public string GetPublicIp()
    {
        unsafe
        {
            if (_networkSystemUpdatePublicIp == null)
            {
                var funcPtr = *(nint*)(*(nint*)_networkSystem + 256);
                _networkSystemUpdatePublicIp = Marshal.GetDelegateForFunctionPointer<CNetworkSystemUpdatePublicIp>(funcPtr);
            }

            var ipBytes = (byte*)(_networkSystemUpdatePublicIp(_networkSystem) + 4);
            return $"{ipBytes[0]}.{ipBytes[1]}.{ipBytes[2]}.{ipBytes[3]}";
        }
    }
}