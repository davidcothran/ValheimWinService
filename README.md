# ValheimWinService
Windows Service to back up valheim game files on an Azure VM.

Incomplete, working (passively) on adding auto updating. It is just a convenience so don't expect the best code in the world.

Compiled in VS 2019
To Install, compile and copy the Release folder to your VM.
Open a CMD window and navigate to
"%windir%\Microsoft.NET\Framework64\v4.0.30319\"
run the following command
InstallUtil.exe [C:\Path\to\ValheimWindowsService.exe]
For reference, I put mine at C:\valheimservice for convenience.

Requires no restart, just open services (Windows+R and type services.msc) and start the ValheimWindowsService.

If you have any issues or would like to uninstall, stop the service and run the same command with "/u" (no quotes) after InstallUtil.exe.

Requires putting valheimservice.config in your C:\valheim folder. Edit the file for your own configuration settings.

Currently will back up your game according to the contained config file. Easy to edit. I'm not responsible if anything happens to you. Use at your own risk.

Reference Material if you have any issues:
https://docs.microsoft.com/en-us/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer