﻿using System;
using System.Collections.Generic;
using System.Linq;
using MusicBeePlugin.AndroidRemote.Model.Entities;
using static MusicBeePlugin.Plugin.MetaDataType;

namespace MusicBeePlugin.AndroidRemote.Core
{
    internal interface IApiAdapter
    {
        int GetTrackNumber(string currentTrack);

        int GetDiskNumber(string currentTrack);

        string GetGenreForTrack(string currentTrack);

        string GetAlbumArtistForTrack(string currentTrack);

        string GetAlbumForTrack(string currentTrack);

        string GetTitleForTrack(string currentTrack);

        string GetArtistForTrack(string currentTrack);

        bool QueryFiles();

        string GetNextFile();

        IEnumerable<Genre> GetGenres();

        IEnumerable<Album> GetAlbums();

        IEnumerable<Artist> GetArtists();

        bool LookupGenres();

        bool LookupArtists();

        bool LookupAlbums();

        void CleanLookup();
    }

    class ApiAdapter : IApiAdapter
    {
        private readonly Plugin.MusicBeeApiInterface _api;

        public ApiAdapter(Plugin.MusicBeeApiInterface api)
        {
            _api = api;
        }

        public int GetTrackNumber(string currentTrack)
        {
            int trackNumber;
            int.TryParse(_api.Library_GetFileTag(currentTrack, TrackNo), out trackNumber);
            return trackNumber;
        }

        public int GetDiskNumber(string currentTrack)
        {
            int discNumber;
            int.TryParse(_api.Library_GetFileTag(currentTrack, DiscNo), out discNumber);
            return discNumber;
        }

        public string GetGenreForTrack(string currentTrack)
        {
            return _api.Library_GetFileTag(currentTrack, Plugin.MetaDataType.Genre).Cleanup();
        }

        public string GetAlbumArtistForTrack(string currentTrack)
        {
            return _api.Library_GetFileTag(currentTrack, AlbumArtist).Cleanup();
        }

        public string GetAlbumForTrack(string currentTrack)
        {
            return _api.Library_GetFileTag(currentTrack, Plugin.MetaDataType.Album).Cleanup();
        }

        public string GetTitleForTrack(string currentTrack)
        {
            return _api.Library_GetFileTag(currentTrack, TrackTitle).Cleanup();
        }

        public string GetArtistForTrack(string currentTrack)
        {
            return _api.Library_GetFileTag(currentTrack, Plugin.MetaDataType.Artist).Cleanup();
        }

        public bool QueryFiles()
        {
            return _api.Library_QueryFiles(null);
        }

        public string GetNextFile()
        {
            return _api.Library_QueryGetNextFile();
        }

        public bool LookupGenres()
        {
            return _api.Library_QueryLookupTable("genre", "count", null);
        }

        public bool LookupArtists()
        {
            return _api.Library_QueryLookupTable("artist", "count", null);
        }

        public bool LookupAlbums()
        {
            return _api.Library_QueryLookupTable("album", "albumartist" + '\0' + "album", null);
        }

        public IEnumerable<Genre> GetGenres()
        {
            return _api.Library_QueryGetLookupTableValue(null)
                .Split(new[] {"\0\0"}, StringSplitOptions.None)
                .Select(entry => entry.Split(new[] {'\0'}, StringSplitOptions.None))
                .Select(genreInfo => new Genre(genreInfo[0].Cleanup(), int.Parse(genreInfo[1])));
        }

        public IEnumerable<Artist> GetArtists()
        {
            return _api.Library_QueryGetLookupTableValue(null)
                .Split(new[] {"\0\0"}, StringSplitOptions.None)
                .Select(entry => entry.Split('\0'))
                .Select(artistInfo => new Artist(artistInfo[0].Cleanup(), int.Parse(artistInfo[1])));
        }

        public IEnumerable<Album> GetAlbums()
        {
            return _api.Library_QueryGetLookupTableValue(null)
                .Split(new[] {"\0\0"}, StringSplitOptions.None)
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s.Trim())
                .Select(CreateAlbum)
                .Distinct()
                .ToList();
        }

        public void CleanLookup()
        {
            _api.Library_QueryLookupTable(null, null, null);
        }

        private static Album CreateAlbum(string queryResult)
        {
            var albumInfo = queryResult.Split('\0');

            albumInfo = albumInfo.Select(s => s.Cleanup()).ToArray();

            if (albumInfo.Length == 1)
            {
                return new Album(albumInfo[0], string.Empty);
            }
            if (albumInfo.Length == 2 && queryResult.StartsWith("\0"))
            {
                return new Album(albumInfo[1], string.Empty);
            }

            var current = albumInfo.Length == 3
                ? new Album(albumInfo[1], albumInfo[2])
                : new Album(albumInfo[0], albumInfo[1]);

            return current;
        }
    }
}