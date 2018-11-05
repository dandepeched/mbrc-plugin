﻿using MusicBeeRemote.Core.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MusicBeeRemote.Core.Network
{
    public class ServiceDiscovery
    {
        private readonly PersistanceManager _settings;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private const int Port = 45345;
        private static readonly IPAddress MulticastAddress = IPAddress.Parse("239.1.5.10");
        private readonly ConcurrentDictionary<IPAddress, UdpClient> _udpClients = new ConcurrentDictionary<IPAddress, UdpClient>();


        public ServiceDiscovery(PersistanceManager settings)
        {
            _settings = settings;
        }

        public void Start()
        {
            var ips = Tools.GetAddressList();

            ips.ForEach(address =>
            {
                _logger.Debug($"Starting discovery listener at {MulticastAddress}:{Port} for interface {address}");
                var udpClient = new UdpClient(AddressFamily.InterNetwork) {EnableBroadcast = true};
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(address, Port));
                udpClient.JoinMulticastGroup(MulticastAddress, address);
                udpClient.BeginReceive(OnDataReceived, udpClient);
                _udpClients.TryAdd(address, udpClient);
            });
        }

        private void OnDataReceived(IAsyncResult ar)
        {
            var udpClient = (UdpClient) ar.AsyncState;            
            try
            {
                var mEndPoint = new IPEndPoint(IPAddress.Any, Port);
                var request = udpClient.EndReceive(ar, ref mEndPoint);
                HandleDiscovery(request, mEndPoint, udpClient);
            }
            catch (Exception e)
            {
               _logger.Error(e, "Failed to proceed with messages");
            }

            try
            {
                udpClient.BeginReceive(OnDataReceived, udpClient);
            }
            catch (ObjectDisposedException e)
            {
                _logger.Debug(e, "Client was already disposed");
            }
            
        }

        private void HandleDiscovery(byte[] request, IPEndPoint mEndPoint, UdpClient udpClient)
        {
            var mRequest = Encoding.UTF8.GetString(request);
            var incoming = JObject.Parse(mRequest);

            _logger.Debug($"Discovery incoming message {mRequest}");

            var discovery = ((string)incoming["context"])?.Contains("discovery") ?? false;
            if (discovery)
            {
                var addresses = Tools.GetPrivateAddressList();
                var ipString = (string)incoming["address"];
                if (string.IsNullOrEmpty(ipString))
                {
                    var notify = ErrorMessage("missing address");
                    SendResponse(notify, mEndPoint, udpClient);
                }
                else
                {
                    var interfaceAddress = InterfaceAddress(ipString, addresses);
                    var notify = DiscoveryResponse(interfaceAddress);
                    _logger.Debug($"Replying to {ipString} discovery message with {notify}");
                    SendResponse(notify, mEndPoint, udpClient);
                }
            }
            else
            {
                var notify = ErrorMessage("unsupported action");
                SendResponse(notify, mEndPoint, udpClient);
            }
        }

        private void SendResponse(Dictionary<string, object> notify, IPEndPoint mEndPoint, UdpClient udpClient)
        {
            var response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notify));
            udpClient.Send(response, response.Length, mEndPoint);
        }

        private static Dictionary<string, object> ErrorMessage(string errorMessage)
        {
            var notify = new Dictionary<string, object>
            {
                {"context", "error"},
                {"description", errorMessage},
            };
            return notify;
        }

        private Dictionary<string, object> DiscoveryResponse(string interfaceAddress)
        {
            var notify = new Dictionary<string, object>
            {
                {"context", "notify"},
                {"address", interfaceAddress},
                {"name", Environment.GetEnvironmentVariable("COMPUTERNAME")},
                {"port", _settings.UserSettingsModel.ListeningPort}
            };
            return notify;
        }

        private static string InterfaceAddress(string ipString, IEnumerable<string> addresses)
        {
            var clientAddress = IPAddress.Parse(ipString);
            var interfaceAddress = string.Empty;
            foreach (var address in addresses)
            {
                var ifAddress = IPAddress.Parse(address);
                var subnetMask = Tools.GetSubnetMask(address);

                var firstNetwork = Tools.GetNetworkAddress(ifAddress, subnetMask);
                var secondNetwork = Tools.GetNetworkAddress(clientAddress, subnetMask);
                if (!firstNetwork.Equals(secondNetwork)) continue;
                interfaceAddress = ifAddress.ToString();
                break;
            }
            return interfaceAddress;
        }

        public void Stop()
        {
            foreach (var address in _udpClients.Keys)
            {
                UdpClient client;
                if (_udpClients.TryRemove(address, out client))
                {
                    client.DropMulticastGroup(MulticastAddress);
                    client.Close();                    
                }
            }            
        }
    }
}