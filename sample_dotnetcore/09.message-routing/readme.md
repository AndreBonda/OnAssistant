﻿This sample shows how to route messages to dialogs.
# Concepts introduced in this sample
In this sample, we create a bot that collects user information after the user greets the bot.  The bot can also provide help. 
# To try this sample
- Clone the samples repository
```bash
git clone https://github.com/Microsoft/botbuilder-samples.git
```
# Prerequisites
## Visual Studio
- Navigate to the samples folder (`botbuilder-samples/samples/csharp_dotnetcore/09.message-routing`) and open MessageRoutingBot.csproj in Visual Studio 
- Run the project (press `F5` key)
## Visual Studio Code
- Open `botbuilder-samples/samples/csharp_dotnetcore/09.message-routing` sample folder.
- Bring up a terminal, navigate to `botbuilder-samples/samples/csharp_dotnetcore/09.message-routing` folder
- type `dotnet run`
## Testing the bot using Bot Framework Emulator
[Microsoft Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) is a desktop application that allows bot developers to test and debug their bots on localhost or running remotely through a tunnel.
- Install the Bot Framework Emulator from [here](https://aka.ms/botframeworkemulator).
### Connect to bot using Bot Framework Emulator V4
- Launch Bot Framework Emulator
- File -> Open bot and navigate to `botbuilder-samples/samples/csharp_dotnetcore/09.message-routing` folder
- Select `BotConfiguration.bot` file
# Deploy this bot to Azure

You can use the [MSBot](https://github.com/microsoft/botbuilder-tools) Bot Builder CLI tool to clone and configure any services this sample depends on. In order to install this and other tools, you can read [Installing CLI Tools](../../../Installing_CLI_tools.md).

To clone this bot, run

```bash
msbot clone services -f DeploymentScripts/msbotClone -n <BOT-NAME> -l <Azure-location> --subscriptionId <Azure-subscription-id>
```
# Further reading
- [Azure Bot Service Introduction](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-overview-introduction?view=azure-bot-service-4.0)
