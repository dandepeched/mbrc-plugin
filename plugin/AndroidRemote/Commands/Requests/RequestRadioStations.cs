﻿using MusicBeePlugin.AndroidRemote.Interfaces;
using ServiceStack.Text;

namespace MusicBeePlugin.AndroidRemote.Commands.Requests
{
    internal class RequestRadioStations : ICommand
    {
        public void Execute(IEvent eEvent)
        {
            var data = eEvent.Data as JsonObject;
            if (data != null)
            {
                var offset = data.Get<int>("offset");
                var limit = data.Get<int>("limit");
                Plugin.Instance.RequestRadioStations(eEvent.ConnectionId, offset, limit);
            }
            else
            {
                Plugin.Instance.RequestRadioStations(eEvent.ConnectionId);
            }
        }
    }
}