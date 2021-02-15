﻿using MusicBeePlugin.AndroidRemote.Interfaces;
using MusicBeePlugin.AndroidRemote.Utilities;
using ServiceStack.Text;

namespace MusicBeePlugin.AndroidRemote.Commands.Requests
{
    class RequestNowPlayingList : ICommand
    {
        public void Dispose()
        {
        }

        public void Execute(IEvent eEvent)
        {
            var socketClient = Authenticator.Client(eEvent.ClientId);
            var clientProtocol = socketClient?.ClientProtocolVersion ?? 2.1;

            var data = eEvent.Data as JsonObject;
            if (clientProtocol < 2.2 || data == null)
            {
                Plugin.Instance.RequestNowPlayingList(eEvent.ClientId);
            }
            else
            {
                var offset = data.Get<int>("offset");
                var limit = data.Get<int>("limit");
                var unordered = data.Get<bool>("unordered");
                if (unordered)
                {
                    Plugin.Instance.RequestNowPlayingListPage(eEvent.ClientId, offset, limit);
                }
                else
                {
                    Plugin.Instance.RequestNowPlayingListOrdered(eEvent.ClientId, offset, limit);
                }
            }
        }
    }
}