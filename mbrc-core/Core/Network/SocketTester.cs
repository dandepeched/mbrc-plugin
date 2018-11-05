﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MusicBeeRemote.Core.Model.Entities;
using MusicBeeRemote.Core.Settings;
using Newtonsoft.Json.Linq;
using NLog;

namespace MusicBeeRemote.Core.Network
{
    public class SocketTester
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly PersistanceManager _settings;

        public SocketTester(PersistanceManager settings)
        {
            _settings = settings;
        }

        // State object for receiving data from remote device.
        public class StateObject
        {
            // Client socket.
            public Socket WorkSocket;

            // Size of receive buffer.
            public const int BufferSize = 256;

            // Receive buffer.
            public byte[] Buffer = new byte[BufferSize];

            // Received data string.
            public StringBuilder Sb = new StringBuilder();
        }

        public delegate void ConnectionStatusHandler(bool connectionStatus);

        public event ConnectionStatusHandler ConnectionChangeListener;

        public void VerifyConnection()
        {
            try
            {
                var port = _settings.UserSettingsModel.ListeningPort;
                var ipEndpoint = new IPEndPoint(IPAddress.Loopback, (int) port);
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.BeginConnect(ipEndpoint, ConnectCallback, client);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Debug, e, "Tester Connection error");
                OnConnectionChange(false);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var state = (StateObject) ar.AsyncState;
                var client = state.WorkSocket;
                var received = client.EndReceive(ar);
                var chars = new char[received + 1];
                var decoder = Encoding.UTF8.GetDecoder();
                decoder.GetChars(state.Buffer, 0, received, chars, 0);
                var message = new string(chars);
                var json = JObject.Parse(message);
                var verified = (string) json["context"] == Constants.VerifyConnection;

                _logger.Log(LogLevel.Info, $"Connection verified: {verified}");
                OnConnectionChange(verified);

                client.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Debug, e, "Tester Connection error");
                OnConnectionChange(false);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                var client = (Socket) ar.AsyncState;
                client.EndConnect(ar);
                var state = new StateObject {WorkSocket = client};
                client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, ReceiveCallback, state);
                client.ReceiveTimeout = 3000;
                client.Send(Payload());
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Debug, e, "Tester Connection error");
                OnConnectionChange(false);
            }
        }

        private static byte[] Payload()
        {
            var socketMessage = new SocketMessage(Constants.VerifyConnection, string.Empty);
            var payload = Encoding.UTF8.GetBytes(socketMessage.ToJsonString() + "\r\n");
            return payload;
        }

        protected virtual void OnConnectionChange(bool connectionstatus)
        {
            ConnectionChangeListener?.Invoke(connectionstatus);
        }
    }
}