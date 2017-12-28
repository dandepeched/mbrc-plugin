using System.Collections.Generic;
using System.Linq;
using MusicBeeRemote.Core.ApiAdapters;
using MusicBeeRemote.Core.Events;
using MusicBeeRemote.Core.Model.Entities;
using MusicBeeRemote.Core.Network;
using MusicBeeRemote.Core.Utilities;
using Newtonsoft.Json.Linq;
using TinyMessenger;

namespace MusicBeeRemote.Core.Commands.Requests
{
    internal class RequestPlaylistPlay : LimitedCommand
    {
        private readonly ITinyMessengerHub _hub;
        private readonly ILibraryApiAdapter _libraryApiAdapter;

        public RequestPlaylistPlay(ITinyMessengerHub hub, ILibraryApiAdapter libraryApiAdapter)
        {
            _hub = hub;
            _libraryApiAdapter = libraryApiAdapter;
        }

        public override string Name() => "Playlist: Play";

        public override void Execute(IEvent @event)
        {
            var success = false;
            var token = @event.DataToken();
            if (token != null && token.Type == JTokenType.String)
            {
                var url = token.Value<string>();
                success = _libraryApiAdapter.PlayPlaylist(url);
            }
            var message = new SocketMessage(Constants.PlaylistPlay, success);
            _hub.Publish(new PluginResponseAvailableEvent(message));
        }

        protected override CommandPermissions GetPermissions() => CommandPermissions.AddTrack;
    }

    internal class RequestPlaylistList : ICommand
    {
        private readonly Authenticator _auth;
        private readonly ILibraryApiAdapter _libraryApiAdapter;
        private readonly ITinyMessengerHub _hub;

        public RequestPlaylistList(Authenticator auth, ILibraryApiAdapter libraryApiAdapter, ITinyMessengerHub hub)
        {
            _auth = auth;
            _libraryApiAdapter = libraryApiAdapter;
            _hub = hub;
        }

        public void Execute(IEvent @event)
        {
            var socketClient = _auth.GetConnection(@event.ConnectionId);
            var clientProtocol = socketClient?.ClientProtocolVersion ?? 2.1;

            var data = @event.Data as JObject;
            if (clientProtocol < 2.2 || data == null)
            {
                SendPage(@event.ConnectionId);
            }
            else
            {
                var offset = (int) data["offset"];
                var limit = (int) data["limit"];
                SendPage(@event.ConnectionId, offset, limit);
            }
        }

        private void SendPage(string connectionId, int offset = 0, int limit = 500)
        {
            var playlists = _libraryApiAdapter.GetPlaylists().ToList();
            var total = playlists.Count;
            var realLimit = offset + limit > total ? total - offset : limit;
            var message = new SocketMessage
            {
                Context = Constants.PlaylistList,
                Data = new Page<Playlist>
                {
                    Data = offset > total ? new List<Playlist>() : playlists.GetRange(offset, realLimit),
                    Offset = offset,
                    Limit = limit,
                    Total = total
                },
                NewLineTerminated = true
            };
            _hub.Publish(new PluginResponseAvailableEvent(message, connectionId));
        }
    }
}