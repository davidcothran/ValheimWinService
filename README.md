# ValheimWinService
Windows Service to back up valheim game files on an Azure VM. Requires valheim be installed in C:\valheim. Copy valheimservice.config to this folder as well. (sorry its manual and not automatic)

Incomplete, working (passively) on adding auto updating. It is just a convenience so don't expect the best code in the world or all the options..

To Install from the exe, copy release files (exe and exe.config) into a folder such as C:\valheimservice.
Open a CMD window and navigate to
"%windir%\Microsoft.NET\Framework64\v4.0.30319\"
run the following command (alter the path as necessary)
InstallUtil.exe C:\valheimservice\ValheimWindowsService.exe
To uninstall run InstallUtil.exe /u C:\valheimservice\ValheimWindowsService.exe

Requires no restart.
Just open services (Windows+R and type services.msc) and start the ValheimWindowsService.

Compiled in VS 2019

Requires putting valheimservice.config in your C:\valheim folder. Edit the file for your own configuration settings.

Currently will back up your game according to the contained config file. Easy to edit. 

I'm not responsible if anything happens to you. Use at your own risk.

Reference Material if you have any issues:
https://docs.microsoft.com/en-us/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer
