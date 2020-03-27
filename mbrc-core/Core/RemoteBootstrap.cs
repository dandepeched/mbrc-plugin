﻿using System.Net;
using System.Net.NetworkInformation;
using LiteDB;
using MusicBeeRemote.Core.ApiAdapters;
using MusicBeeRemote.Core.Caching;
using MusicBeeRemote.Core.Caching.Monitor;
using MusicBeeRemote.Core.Commands;
using MusicBeeRemote.Core.Commands.Logs;
using MusicBeeRemote.Core.Logging;
using MusicBeeRemote.Core.Model;
using MusicBeeRemote.Core.Monitoring;
using MusicBeeRemote.Core.Network;
using MusicBeeRemote.Core.Podcasts;
using MusicBeeRemote.Core.Settings;
using MusicBeeRemote.Core.Settings.Dialog.BasePanel;
using MusicBeeRemote.Core.Settings.Dialog.Commands;
using MusicBeeRemote.Core.Settings.Dialog.PartyModePanel;
using MusicBeeRemote.Core.Settings.Dialog.Whitelist;
using MusicBeeRemote.Core.Utilities;
using MusicBeeRemote.Core.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using StructureMap;
using TinyMessenger;

namespace MusicBeeRemote.Core
{
    public class RemoteBootstrap
    {
        private readonly Container _container;

        public RemoteBootstrap()
        {
            _container = new Container();
        }

        public IMusicBeeRemotePlugin BootStrap(MusicBeeDependencies dependencies)
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Local,
                    NullValueHandling = NullValueHandling.Ignore
                };

                settings.Converters.Add(new StringEnumConverter {NamingStrategy = new SnakeCaseNamingStrategy()});
                return settings;
            };

            //Bson serialization for client IP addresses
            BsonMapper.Global.RegisterType(
                ipAddress => ipAddress.ToString(),
                bson => IPAddress.Parse(bson.AsString)
            );

            //Bson serialization for client Physical Addresses
            BsonMapper.Global.RegisterType(
                mac => mac.ToString(),
                bson =>
                {
                    var newAddress = PhysicalAddress.Parse(bson);
                    return PhysicalAddress.None.Equals(newAddress) ? null : newAddress;
                }
            );
            
            _container.Configure(c =>
            {
                c.For<ILibraryApiAdapter>().Use(() => dependencies.LibraryAdapter).Singleton();
                c.For<INowPlayingApiAdapter>().Use(() => dependencies.NowPlayingAdapter).Singleton();
                c.For<IOutputApiAdapter>().Use(() => dependencies.OutputAdapter).Singleton();
                c.For<IPlayerApiAdapter>().Use(() => dependencies.PlayerAdapter).Singleton();
                c.For<IQueueAdapter>().Use(() => dependencies.QueueAdapter).Singleton();
                c.For<ITrackApiAdapter>().Use(() => dependencies.TrackAdapter).Singleton();
                c.For<IInvokeHandler>().Use(() => dependencies.InvokeHandler).Singleton();              

                c.For<IWindowManager>().Use<WindowManager>().Singleton();

                c.For<IPluginLogManager>().Use<PluginLogManager>().Singleton();
                c.For<IPlayerStateMonitor>().Use<PlayerStateMonitor>().Singleton();
                c.For<ITrackStateMonitor>().Use<TrackStateMonitor>().Singleton();

                c.For<CommandExecutor>().Use<CommandExecutor>().Singleton();

                c.For<SocketServer>().Use<SocketServer>().Singleton();
                c.For<HttpSupport>().Use<HttpSupport>().Singleton();
                c.For<LyricCoverModel>().Use<LyricCoverModel>().Singleton();
                c.For<ServiceDiscovery>().Use<ServiceDiscovery>().Singleton();

                c.For<PersistanceManager>().Use<PersistanceManager>().Singleton();
                c.For<IJsonSettingsFileManager>().Use<JsonSettingsFileManager>().Singleton();
                c.For<ILegacySettingsMigration>().Use<LegacySettingsMigration>().Singleton();

                c.For<IStorageLocationProvider>()
                    .Use<StorageLocationProvider>()
                    .Ctor<string>()
                    .Is(dependencies.BaseStoragePath)
                    .Singleton();

                c.For<IVersionProvider>()
                    .Use<VersionProvider>()
                    .Ctor<string>()
                    .Is(dependencies.CurrentVersion)
                    .Singleton();

                c.For<Authenticator>().Use<Authenticator>().Singleton();               
                c.For<ITrackRepository>().Use<TrackRepository>().Singleton();
                c.For<ILibraryScanner>().Use<LibraryScanner>().Singleton();
                c.For<ITinyMessengerHub>().Use<TinyMessengerHub>().Singleton();
                c.For<IMusicBeeRemotePlugin>().Use<MusicBeeRemotePlugin>().Singleton();

                c.For<OpenHelpCommand>().Use<OpenHelpCommand>();
                c.For<OpenLogDirectoryCommand>().Use<OpenLogDirectoryCommand>();
                c.For<SaveConfigurationCommand>().Use<SaveConfigurationCommand>();
                c.For<ConfigurationPanel>().Use<ConfigurationPanel>();
                c.For<ConfigurationPanelViewModel>().Use<ConfigurationPanelViewModel>();
                c.For<IConfigurationPanelPresenter>().Use<ConfigurationPanelPresenter>();

                c.For<PartyModePanel>().Use<PartyModePanel>();

                c.For<IWhitelistManagementPresenter>().Use<WhitelistManagementPresenter>();
                c.For<WhitelistManagementControl>().Use<WhitelistManagementControl>();

                c.For<PodcastHttpApi>().Use<PodcastHttpApi>().Singleton();
                c.For<ClientRepository>().Singleton();
                c.For<LogRepository>().Singleton();
                c.For<ClientManager>().Singleton();
            });

            var controller = _container.GetInstance<CommandExecutor>();
            Configuration.Register(controller, _container);


            return _container.GetInstance<IMusicBeeRemotePlugin>();
        }
    }
}