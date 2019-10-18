// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class AddSignalRTests
    {
        [Fact]
        public void ServicesAddedBeforeAddSignalRAreUsed()
        {
            var serviceCollection = new ServiceCollection();

            var markerService = new SignalRCoreMarkerService();
            serviceCollection.AddSingleton(markerService);
            serviceCollection.AddSingleton<IUserIdProvider, CustomIdProvider>();
            serviceCollection.AddSingleton(typeof(HubLifetimeManager<>), typeof(CustomHubLifetimeManager<>));
            serviceCollection.AddSingleton<IHubProtocolResolver, CustomHubProtocolResolver>();
            serviceCollection.AddScoped(typeof(IHubActivator<>), typeof(CustomHubActivator<>));
            serviceCollection.AddSingleton(typeof(IHubContext<>), typeof(CustomHubContext<>));
            serviceCollection.AddSingleton(typeof(IHubContext<,>), typeof(CustomHubContext<,>));
            var hubOptions = new HubOptionsSetup(new List<IHubProtocol>());
            serviceCollection.AddSingleton<IConfigureOptions<HubOptions>>(hubOptions);
            serviceCollection.AddSingleton(typeof(IHubMessageSerializer<>), typeof(CustomHubMessageSerializer<>));
            serviceCollection.AddSignalR();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsType<CustomIdProvider>(serviceProvider.GetRequiredService<IUserIdProvider>());
            Assert.IsType<CustomHubLifetimeManager<CustomHub>>(serviceProvider.GetRequiredService<HubLifetimeManager<CustomHub>>());
            Assert.IsType<CustomHubProtocolResolver>(serviceProvider.GetRequiredService<IHubProtocolResolver>());
            Assert.IsType<CustomHubActivator<CustomHub>>(serviceProvider.GetRequiredService<IHubActivator<CustomHub>>());
            Assert.IsType<CustomHubContext<CustomHub>>(serviceProvider.GetRequiredService<IHubContext<CustomHub>>());
            Assert.IsType<CustomHubContext<CustomTHub, string>>(serviceProvider.GetRequiredService<IHubContext<CustomTHub, string>>());
            Assert.IsType<CustomHubContext<CustomDynamicHub>>(serviceProvider.GetRequiredService<IHubContext<CustomDynamicHub>>());
            Assert.IsType<CustomHubMessageSerializer<CustomDynamicHub>>(serviceProvider.GetRequiredService<IHubMessageSerializer<CustomDynamicHub>>());
            Assert.Equal(hubOptions, serviceProvider.GetRequiredService<IConfigureOptions<HubOptions>>());
            Assert.Equal(markerService, serviceProvider.GetRequiredService<SignalRCoreMarkerService>());
        }

        [Fact]
        public void ServicesAddedAfterAddSignalRAreUsed()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSignalR();
            serviceCollection.AddSingleton<IUserIdProvider, CustomIdProvider>();
            serviceCollection.AddSingleton(typeof(HubLifetimeManager<>), typeof(CustomHubLifetimeManager<>));
            serviceCollection.AddSingleton<IHubProtocolResolver, CustomHubProtocolResolver>();
            serviceCollection.AddScoped(typeof(IHubActivator<>), typeof(CustomHubActivator<>));
            serviceCollection.AddSingleton(typeof(IHubContext<>), typeof(CustomHubContext<>));
            serviceCollection.AddSingleton(typeof(IHubContext<,>), typeof(CustomHubContext<,>));
            serviceCollection.AddSingleton(typeof(IHubMessageSerializer<>), typeof(CustomHubMessageSerializer<>));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsType<CustomIdProvider>(serviceProvider.GetRequiredService<IUserIdProvider>());
            Assert.IsType<CustomHubLifetimeManager<CustomHub>>(serviceProvider.GetRequiredService<HubLifetimeManager<CustomHub>>());
            Assert.IsType<CustomHubProtocolResolver>(serviceProvider.GetRequiredService<IHubProtocolResolver>());
            Assert.IsType<CustomHubActivator<CustomHub>>(serviceProvider.GetRequiredService<IHubActivator<CustomHub>>());
            Assert.IsType<CustomHubContext<CustomHub>>(serviceProvider.GetRequiredService<IHubContext<CustomHub>>());
            Assert.IsType<CustomHubContext<CustomTHub, string>>(serviceProvider.GetRequiredService<IHubContext<CustomTHub, string>>());
            Assert.IsType<CustomHubContext<CustomDynamicHub>>(serviceProvider.GetRequiredService<IHubContext<CustomDynamicHub>>());
            Assert.IsType<CustomHubMessageSerializer<CustomDynamicHub>>(serviceProvider.GetRequiredService<IHubMessageSerializer<CustomDynamicHub>>());
        }

        [Fact]
        public void HubSpecificOptionsDoNotAffectGlobalHubOptions()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSignalR().AddHubOptions<CustomHub>(options =>
            {
                options.SupportedProtocols.Clear();
                options.AdditionalHubProtocols.Add(new JsonHubProtocol());
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.Equal(1, serviceProvider.GetRequiredService<IOptions<HubOptions>>().Value.SupportedProtocols.Count);
            Assert.Equal(0, serviceProvider.GetRequiredService<IOptions<HubOptions<CustomHub>>>().Value.SupportedProtocols.Count);

            Assert.Equal(0, serviceProvider.GetRequiredService<IOptions<HubOptions>>().Value.AdditionalHubProtocols.Count);
            Assert.Equal(1, serviceProvider.GetRequiredService<IOptions<HubOptions<CustomHub>>>().Value.AdditionalHubProtocols.Count);
        }

        [Fact]
        public void HubSpecificOptionsHaveSameValuesAsGlobalHubOptions()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSignalR().AddHubOptions<CustomHub>(options =>
            {
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var hubOptions = serviceProvider.GetRequiredService<IOptions<HubOptions<CustomHub>>>().Value;
            var globalHubOptions = serviceProvider.GetRequiredService<IOptions<HubOptions>>().Value;

            Assert.Equal(globalHubOptions.MaximumReceiveMessageSize, hubOptions.MaximumReceiveMessageSize);
            Assert.Equal(globalHubOptions.StreamBufferCapacity, hubOptions.StreamBufferCapacity);
            Assert.Equal(globalHubOptions.EnableDetailedErrors, hubOptions.EnableDetailedErrors);
            Assert.Equal(globalHubOptions.KeepAliveInterval, hubOptions.KeepAliveInterval);
            Assert.Equal(globalHubOptions.HandshakeTimeout, hubOptions.HandshakeTimeout);
            Assert.Equal(globalHubOptions.SupportedProtocols, hubOptions.SupportedProtocols);
            Assert.Equal(globalHubOptions.ClientTimeoutInterval, hubOptions.ClientTimeoutInterval);
            Assert.True(hubOptions.UserHasSetValues);
        }

        [Fact]
        public void StreamBufferCapacityGetSet()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSignalR().AddHubOptions<CustomHub>(options =>
            {
                options.StreamBufferCapacity = 42;
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.Equal(42, serviceProvider.GetRequiredService<IOptions<HubOptions<CustomHub>>>().Value.StreamBufferCapacity);
        }

        [Fact]
        public void UserSpecifiedOptionsRunAfterDefaultOptions()
        {
            var serviceCollection = new ServiceCollection();

            // null is special when the default options setup runs, so we set to null to verify that our options run after the default
            // setup runs
            serviceCollection.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = null;
                options.StreamBufferCapacity = null;
                options.EnableDetailedErrors = null;
                options.KeepAliveInterval = null;
                options.HandshakeTimeout = null;
                options.SupportedProtocols = null;
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(1);
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var globalOptions = serviceProvider.GetRequiredService<IOptions<HubOptions>>().Value;
            Assert.Null(globalOptions.MaximumReceiveMessageSize);
            Assert.Null(globalOptions.StreamBufferCapacity);
            Assert.Null(globalOptions.EnableDetailedErrors);
            Assert.Null(globalOptions.KeepAliveInterval);
            Assert.Null(globalOptions.HandshakeTimeout);
            Assert.Null(globalOptions.SupportedProtocols);
            Assert.Equal(TimeSpan.FromSeconds(1), globalOptions.ClientTimeoutInterval);
        }
    }

    public class CustomHub : Hub
    {
    }

    public class CustomTHub : Hub<string>
    {
    }

    public class CustomDynamicHub : DynamicHub
    {
    }

    public class CustomIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            throw new System.NotImplementedException();
        }
    }

    public class CustomHubProtocolResolver : IHubProtocolResolver
    {
        public IReadOnlyList<IHubProtocol> AllProtocols => throw new System.NotImplementedException();

        public IHubProtocol GetProtocol(string protocolName, IReadOnlyList<string> supportedProtocols)
        {
            throw new System.NotImplementedException();
        }
    }

    public class CustomHubActivator<THub> : IHubActivator<THub> where THub : Hub
    {
        public THub Create()
        {
            throw new System.NotImplementedException();
        }

        public void Release(THub hub)
        {
            throw new System.NotImplementedException();
        }
    }

    public class CustomHubContext<THub> : IHubContext<THub> where THub : Hub
    {
        public IHubClients Clients => throw new System.NotImplementedException();

        public IGroupManager Groups => throw new System.NotImplementedException();
    }

    public class CustomHubContext<THub, T> : IHubContext<THub, T>
        where THub : Hub<T>
        where T : class
    {
        public IHubClients<T> Clients => throw new System.NotImplementedException();

        public IGroupManager Groups => throw new System.NotImplementedException();
    }

    public class CustomHubLifetimeManager<THub> : HubLifetimeManager<THub> where THub : Hub
    {
        public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            throw new System.NotImplementedException();
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            throw new System.NotImplementedException();
        }

        public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }

    public class CustomHubMessageSerializer<THub> : IHubMessageSerializer<THub> where THub : Hub
    {
        public SerializedHubMessage SerializeMessage(HubMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}
