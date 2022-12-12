using System;
using System.Threading.Tasks;
using Jellyfin.Sdk;
using SystemException = Jellyfin.Sdk.SystemException;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace TVSimulator.ChannelGenerator;

/// <summary>
/// Sample Jellyfin service.
/// </summary>
public class SampleService
{
    private readonly SdkClientSettings _sdkClientSettings;
    private readonly IConfiguration _configuration;
    private readonly ISystemClient _systemClient;
    private readonly IUserClient _userClient;
    private readonly IUserViewsClient _userViewsClient;
    private readonly ITvShowsClient _tvShowsClient;
    private readonly IItemsClient _itemsClient;
    private readonly ILibraryClient _libraryClient;

    public SampleService(
        SdkClientSettings sdkClientSettings,
        IConfiguration configuration,
        ISystemClient systemClient,
        IUserClient userClient,
        IUserViewsClient userViewsClient,
        ITvShowsClient tvShowsClient,
        IItemsClient itemsClient,
        ILibraryClient libraryClient)
    {
        _sdkClientSettings = sdkClientSettings;
        _configuration = configuration;
        _systemClient = systemClient;
        _userClient = userClient;
        _userViewsClient = userViewsClient;
        _tvShowsClient = tvShowsClient;
        _itemsClient = itemsClient;
        _libraryClient = libraryClient;
        
    }

    /// <summary>
    /// Run the sample.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunAsync()
    {
        var validServer = false;
        var hasConfig = _configuration != null;
        do
        {
            // Prompt for server url.
            // Url must be proto://host/path
            // ex: https://demo.jellyfin.org/stable
            var host = _configuration?.GetValue<string>("ServerUrl");
            if (host == null)
            {
                Console.Write("Server Url: ");
                host = Console.ReadLine();
            }
            
            _sdkClientSettings.BaseUrl = host;
            try
            {
                // Get public system info to verify that the url points to a Jellyfin server.
                var systemInfo = await _systemClient.GetPublicSystemInfoAsync()
                    .ConfigureAwait(false);
                validServer = true;
                Console.WriteLine($"Connected to {host}");
                Console.WriteLine($"Server Name: {systemInfo.ServerName}");
                Console.WriteLine($"Server Version: {systemInfo.Version}");
            }
            catch (InvalidOperationException ex)
            {
                await Console.Error.WriteLineAsync("Invalid url").ConfigureAwait(false);
                await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            }
            catch (SystemException ex)
            {
                await Console.Error.WriteLineAsync($"Error connecting to {host}").ConfigureAwait(false);
                await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            }
        }
        while (!validServer);

        var validUser = false;

        UserDto userDto = null!;
        do
        {
            try
            {
                var username = _configuration?.GetValue<string>("Username");
                if (username == null)
                {
                    Console.Write("Username: ");
                    username = Console.ReadLine();
                }
                var password = _configuration?.GetValue<string>("Password");
                if (password == null)
                {
                    Console.Write("Password: ");
                    password = Console.ReadLine();
                }
                Console.WriteLine($"Logging into {_sdkClientSettings.BaseUrl}");

                // Authenticate user.
                var authenticationResult = await _userClient.AuthenticateUserByNameAsync(new AuthenticateUserByName
                    {
                        Username = username,
                        Pw = password
                    })
                    .ConfigureAwait(false);

                _sdkClientSettings.AccessToken = authenticationResult.AccessToken;
                userDto = authenticationResult.User;
                Console.WriteLine("Authentication success.");
                Console.WriteLine($"Welcome to Jellyfin - {userDto.Name}");
                validUser = true;
            }
            catch (UserException ex)
            {
                await Console.Error.WriteLineAsync("Error authenticating.").ConfigureAwait(false);
                await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            }
        }
        while (!validUser);

        
        //await PrintViews(userDto.Id)
        //    .ConfigureAwait(false);



        var showName = "";
        do
        {
            Console.Write("Show Name (or 'quit'): ");
            showName = Console.ReadLine();
            await PrintShowInfo(userDto.Id, showName)
            .ConfigureAwait(false);
        } while (showName != "quit");


    }

    private async Task PrintViews(Guid userId)
    {
        try
        {
            var views = await _userViewsClient.GetUserViewsAsync(userId)
                .ConfigureAwait(false);
            Console.WriteLine("Printing Views:");
            foreach (var view in views.Items)
            {
                Console.WriteLine($"{view.Id} - {view.Name}");
            }
        }
        catch (UserViewsException ex)
        {
            await Console.Error.WriteLineAsync("Error getting user views").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
        }
    }
    
    private async Task PrintShowInfo(Guid userId, string showName)
    {
        try
        {

            var includeItemTypes = new BaseItemKind[]{BaseItemKind.Series};

            var shows = await _itemsClient.GetItemsAsync(userId, includeItemTypes:includeItemTypes, recursive:true)
                .ConfigureAwait(false);

            
            Console.WriteLine("Printing Items Stuff:");
            BaseItemDto usedshow = default;

            foreach (var show in shows.Items)
            {
                Console.WriteLine($"{show.Id} - {show.Name}");
                if(show.Name == showName)
                {
                    usedshow = show;
                    Console.WriteLine("found it!!!");
                }
            }

            
            var episodes = await _tvShowsClient.GetEpisodesAsync(usedshow.Id, enableImages:false);
            Console.WriteLine("got eps!!!");
            var paths = new List<string>();

            foreach(var episode in episodes.Items)
            {
                Console.WriteLine($"{episode.Id} - {episode.Name} - {episode.RunTimeTicks}");
                var path = $"{_libraryClient.GetDownloadUrl(episode.Id)}?api_key={_sdkClientSettings.AccessToken}";
                paths.Add(path);
                Console.WriteLine(path);

            }

            await MakeTvFile(paths, usedshow.Name);

            //var usedep = episodes.Items[0];
            
            //Console.WriteLine(JsonSerializer.Serialize(usedep));
            
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync("Error getting show info").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
        }
    }

    private async Task MakeTvFile(List<string> paths, string name)
    {
        var channel = new Channel();
        channel.Name = name;
        channel.Number = 666;
        channel.playlist.Clear();
        foreach(var path in paths)
        {
            var entry = new PlaylistEntry();
            entry.Path = path;
            entry.PathType = PathType.ABSOLUTE;
            entry.Length = -1;
            channel.playlist.Add(entry);
        }

        Console.WriteLine($"Making channel: {channel.Name}");

        //Console.WriteLine(channel.ToString());

        var json = JsonConvert.SerializeObject(channel, Formatting.Indented);

        //Console.WriteLine("JSON");
        //Console.WriteLine(json);

        using StreamWriter file = new($"{channel.Name}.tv");
        {
            await file.WriteAsync(json);
        }
    }
}
