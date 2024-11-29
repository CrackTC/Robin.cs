# Robin.cs

Robin.cs is a bot framework written in C# that supports extending its functionalities through extensions. It also provides various ~~useless~~ extensions that demonstrate different bot capabilities.

## Features


- **Automatic Extension/Implementation Discovery**: Robin automatically discovers and loads extensions/implementations from `Extensions/` and `Implementations/` using reflection.
- **Multiple Protocol Support**: Theoretically, Robin can support multiple protocols by implementing the `IBackendFactory` interface. Currently, only OneBot v11 is implemented.
- **Fluent Middleware**: A middleware system that allows you to easily create bot functions using a fluent API.


## Extensions


- **Approve**: Auto approve group invitations & friend requests.
- **AtPoke**: Poke someone by @ them.
- **B23**: Resolve original link for b23 card.
- **Dice**: Roll a dice using `/dice`
- **Gemini**: Gemini chat bot in private chat.
- **Gray**: [It's Ikuyover](https://github.com/CrackTC/Gray)
- **Help**: List all extensions & descriptions.
- **Oa**: JLU OA announcement.
- **Oled**: Display messages on my SSD1306 OLED screen using OrangePi 5 Plus, **you may need to modify the code to adapt to your own hardware**.
- **PokeBack**: Poke back the one who poked you.
- **RandReply**: Random reply to the one who @ you.
- **ReplyAction**: Reply to a message with `/{verb} {adverb}` results in `{your name} {verb} {sender name} {adverb}`.
- **Saucenao**: Search image source using saucenao.
- **Status**: Get the status of the bot.
- **Test**: Test extension.
- **UserRank**: Get the rank of user in the group, based on the number of messages sent.
- **Welcome**: Welcome new members.
- **WhoAtMe**: Who @ me.
- **Wife**: Randomly choose a daily wife ~~(really random)~~.
- **WordCloud**: Generate word cloud image from group messages, see [WordCloud](https://github.com/CrackTC/WordCloud), [WordCloud.Server](https://github.com/CrackTC/WordCloud.Server) and [hanlp.server](https://github.com/CrackTC/hanlp.server) for more details.


## Prerequisites

- Docker

Currently it is recommended to build and deploy robin using docker, see `Dockerfile`.


## Setup


1. Clone the repository:
   ```bash
   git clone https://github.com/CrackTC/Robin.cs.git
   cd Robin.cs
   ```

2. *Optional*: opt out of some extensions by removing them from the `Extensions/` directory.

3. Run the bot using Docker:
   ```bash
   docker build -t robin .
   docker run \
     -v /path/to/data:/app/data \
     -it robin
   ```

## Configuration


Configuration file is located at `${cwd}/config.json`.

Here is an example configuration file:

```json
{
  "Bots": [
    {
      "Uin": 123456789,
      "EventInvokerName": "OneBotForwardWebSocket",
      "OperationProviderName": "OneBotHttpClient",
      "EventInvokerConfig": {
        "Url": "ws://<host>:<port>",
        "ReconnectInterval": 5
      },
      "OperationProviderConfig": {
        "Url": "http://<host>:<port>"
      },
      "Filters": {
        "oa": {
          "Group": {
            "Whitelist": true,
            "Ids": [
              123456789,
              987654321
            ]
          }
        },
        "gemini": {
          "Private": {
            "Whitelist": false,
            "Ids": [
              1145141919810
            ]
          }
        }
      },
      "Configurations": [
        "fluent": {
          "Crons": {
            "user_rank": {
              "rank cron": "0 0 0 * * ?"
            },
            "word_cloud": {
              "word cloud cron": "0 0 0 * * ?"
            },
            "oa": {
              "main cron": "0 0 * * * ?"
            }
          }
        },
        "gemini": {
          "Model": "gemini-1.5-flash-latest",
          "ApiKey": "<your-api-key>"
        },
        "gray": {
          "ApiAddress": "http://<host>:<port>"
        },
        "rand_reply": {
          "Texts": [
            "我喜欢你❤️",
            "我讨厌你😡"
          ],
          "ImagePaths": []
        },
        "user_rank": {
          "TopN": 10
        },
        "word_cloud": {
          "ApiAddress": "http://<host>:<port>/wordcloud",
          "CloudOption": {
            "Colors": [
              "ffbf616a",
              "ffd08770",
              "ffebcb8b",
              "ffa3be8c",
              "ff88c0d0"
            ],
            "FontUrl": "<font-url>",
            "Padding": 5,
            "BackgroundImageUrl": "<image-url>",
            "StrokeRatio": 0.01,
            "StrokeColors": [
              "ff2e3440"
            ]
          }
        },
        "sauce_nao": {
          "ApiKey": "<your-api-key>"
        },
        "welcome": {
          "WelcomeTexts": {
            "123456789": "Welcome {at}",
            "987654321": "{at} 我讨厌你😡"
          }
        },
        "oa": {
          "TempGroup": 123456789
          "UseVpn": false
        }
      ]
    }
  ]
}
```


## Writing an Extension

### Step 1: Create a New Extension Project

1. Navigate to the `Extensions` directory:
   ```bash
   cd Extensions
   ```

2. Create a new directory for your extension:
   ```bash
   mkdir Robin.Extensions.MyExtension
   cd Robin.Extensions.MyExtension
   ```

3. Create a new class library project:
   ```bash
   dotnet new classlib -n Robin.Extensions.MyExtension
   ```

### Step 2: Implement the Extension

1. Add references to required projects:
   ```xml
   <!-- Robin.Extensions.MyExtension.csproj -->
   <Project Sdk="Microsoft.NET.Sdk">

     <ItemGroup>
       <ProjectReference Include="..\..\Middlewares\Robin.Middlewares.Fluent\Robin.Middlewares.Fluent.csproj" />
     </ItemGroup>

     <PropertyGroup>
       <TargetFramework>net9.0</TargetFramework>
       <ImplicitUsings>enable</ImplicitUsings>
       <Nullable>enable</Nullable>
       <!-- Enable dynamic loading for proper dependency resolution -->
       <EnableDynamicLoading>true</EnableDynamicLoading>
     </PropertyGroup>

   </Project>
   ```

2. Create your extension class:
   ```csharp
   // MyExtensionFunction.cs
   using Robin.Abstractions;
   using Robin.Abstractions.Context;
   using Robin.Abstractions.Event.Message;
   using Robin.Middlewares.Fluent;
   using Robin.Middlewares.Fluent.Event;

   namespace Robin.Extensions.MyExtension;

   [BotFunctionInfo("my_extension", "Description of my extension")]
   public class MyExtensionFunction(FunctionContext<MyExtensionOption> context)
       : BotFunction<MyExtensionOption>(context), // required
         IFluentFunction                          // optional, for Fluent API
   {
       public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
       {
           builder.On<MessageEvent>()
               .OnCommand("hello")
               .Where(e => e.Event.GroupId == 1145141919810L)
               .Do(async tuple =>
               {
                   var (e, t) = tuple;
                   if (_context.Configuration.Enabled)
                       await e.NewMessageRequest([new TextData(_context.Configuration.Text)])
                           .SendAsync(_context, t);
               });

           return Task.CompletedTask;
       }
   }
   ```

3. Create your extension option class:
   ```csharp
   // MyExtensionOption.cs
   namespace Robin.Extensions.MyExtension;

   public class MyExtensionOption
   {
       public bool Enabled { get; set; }
       public string Text { get; set; }
   }
   ```

### Step 3: Build the Extension

Simply run `docker build -t robin .` in the root directory to build the bot with your extension.

# License

GPL-2.0-only
