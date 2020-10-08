#!/bin/bash

set -eu
set -o pipefail

# colors
B_BLACK="$(echo -e '\e[48;5;0m')" # background (0m - 15m)
B_RED="$(echo -e '\e[48;5;1m')"   # background (0m - 15m)
B_GREEN="$(echo -e '\e[48;5;2m')"   # background (0m - 15m)
B_YELLOW="$(echo -e '\e[48;5;3m')"   # background (0m - 15m)
B_ORANGE="$(echo -e '\e[48;5;166m')"   # background (0m - 15m)

BLACK="$(echo -e '\e[38;5;0m')" # foreground (0m - 255m)
GREEN="$(echo -e '\e[37;5;2m')" # foreground (0m - 255m)
ORANGE="$(echo -e '\e[38;5;208m')" # foreground (0m - 255m)
NOCOLOR="$(echo -e '\e[0m')"    # foreground

trap '
RET=$?
if [ "$RET" == "0" ]; then echo "$B_GREEN" ; else echo "$B_RED" ; fi ; echo -n "RET:$RET "
echo -e "  Finished+++                                ${B_ORANGE}${BLACK} Press ENTER...$NOCOLOR"
read
' EXIT

DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1








./go-interpolate.sh
exit 0








#wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
#sudo dpkg -i packages-microsoft-prod.deb


#apt search dotnet
#sudo apt install libgtk-dotnet3.0-cil
#sudo apt install libgtk-dotnet3.0-cil-dev
#dpkg -L libgtk-dotnet3.0-cil
#dpkg -L libgtk-dotnet3.0-cil-dev
#sudo apt-get update; sudo apt-get install -y apt-transport-https && sudo apt-get update &&  sudo apt-get install -y aspnetcore-runtime-3.1
#dotnet add package Newtonsoft.Json

#ls -laXF ~/.dotnet
#ls -laXF /usr/share/dotnet

#apt search dotnet SDK
#sudo apt install dotnet-sdk-3.1
#exit 0
#dotnet --info
#dotnet --list-sdks
#dotnet --list-runtimes

#dotnet  add package Newtonsoft.Json
#

#sudo apt install nuget
#dpkg -L nuget
#nuget install Newtonsoft.Json -Version 12.0.3

#mcs --help
#exit 0

cp ./Newtonsoft.Json.12.0.3/lib/net45/Newtonsoft.Json.dll .

if [ -d "Status" ]; then
	echo "Status dir exists"
else
	ln -s "/games/games-steam/steamapps/compatdata/359320/pfx/drive_c/users/steamuser/Saved Games/Frontier Developments/Elite Dangerous" Status
fi


csc  -define:DEBUG -optimize -r:Newtonsoft.Json.dll ../elite-trajectory/Assets/Scripts/StatusFileReader.cs TrajectoryMapDeserializer.cs TrajectoryMapTrajectory.cs TrajectoryMapTargets.cs
#csc TrajectoryMapTargets.cs
mono TrajectoryMapTargets.exe

