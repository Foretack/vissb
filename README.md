
## Building on Windows

### Prerequisites
- Download the [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download)
- Download [Git](https://git-scm.com/downloads)



### Cloning

Go to the directory of your choice, and open the terminal by typing `cmd` in the address bar and pressing enter. Paste the following line in the terminal and press enter:

```
  git clone https://github.com/Foretack/vissb
```
In that directory, a new folder named `vissb` will appear. 

### Configuring
Navigate into the `vissb` folder and open `Config.cs` with a text editor (Notepad or Notepad++ work fine). 

Fill in your bot account's creditentials:

- Username: The bot's username (in lowercase)
- Token: The bot account's access Token
- ClientID: The bot account's client id 
- OpenAIToken: The OpenAI secret key [can be found or generated here](https://beta.openai.com/account/api-keys)
- Channel: The channel the bot will run in (in lowercase) 
- HosterName: Your Twitch username, in lowercase. (This will be used to permit you to update the bot with a chat command)
- AskCommandCooldown: The cooldown for each user in seconds (default is 1 minute)

[Example of a modified config](https://i.imgur.com/n3pp2Zv.png) in Notepad++

### Building
Navigate into the `vissb` folder with your previously opened terminal by typing `cd vissb` in the terminal. Or open the terminal again inside the `vissb` folder if you closed it before, then type the following command:

```
  dotnet watch run --no-hot-reload
```

The project should build and run itself after that. If you encounter a message saying `'dotnet' is not recognized as an internal or external command`, make sure you installed the **.NET 6.0 SDK** correctly or restart your PC.

## Building on Linux
Exactly the same steps. You will have to do few more things to install the **.NET 6.0 SDK**, but it's all explained in the download link.

## Updating
If you did all the building steps correctly without any errors, as well as configuring `Config.cs` correctly, then updating is a piece of cake. 

All you have to do is type the following in the chat the bot is running in: 

```
  !botname update
```

`botname` being replaced by the name of your bot. i.e if your bot's name is "xDDdBot", you type "!xdddbot update".


## Contact & Reporting issues
If you wish to report an issue or ask a question, you can open an issue on this repository.

Unfortunately I do not accept random friend requests on Discord due to bot spam and trolling, and  with my Twitch account being indefinitely suspended, your only option is opening an issue here ¯\\\_(ツ)\_/¯
