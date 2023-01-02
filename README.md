
## Building on Windows

### Prerequisites
- [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download)
- [Git](https://git-scm.com/downloads)



### Cloning

Go to the directory of your choice, and open the terminal by typing `cmd` in the address bar and pressing enter. Paste the following line in the terminal and press enter:

```
  git clone https://github.com/Foretack/vissb
```
In that directory, a new folder named `vissb` will appear. 

### Configuring
Navigate into the `vissb` folder and copy the `config_template.yml` file. Rename the new copy to `config.yml` and open it with a text editor (Notepad or Notepad++). 

Fill in your bot account's creditentials & configure the bot to your liking, make sure to leave a space after each colon. 

Some characters need to be escaped as well ({, }, [, ], ,, &, :, *, #, ?, |. -, <. >, =, !, %, @, \.), e.g: !ping should be '!ping'

[Example of a config](https://i.imgur.com/mdl8QUQ.png) in Notepad++

### Running the bot
Navigate into the `vissb` folder with your previously opened terminal by typing `cd vissb` in the terminal. Or open the terminal again inside the `vissb` folder if you closed it before, then type the following command:

```
  dotnet run -c Release
```

The project should build and run itself after that. If you encounter a message saying `'dotnet' is not recognized as an internal or external command`, make sure you installed the **.NET 7.0 SDK** correctly or restart your PC.

## Building on Linux
Exactly the same steps. You will have to do few more things to install the **.NET 7.0 SDK**, but it's all explained in the download link.

## Updating

`git pull` in the same directory & run the bot again.




If you wish to report an issue or ask a question, you can open an issue on this repository.
