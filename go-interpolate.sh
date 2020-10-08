#!/bin/bash

# called from ./go.sh

cp ./Newtonsoft.Json.12.0.3/lib/net45/Newtonsoft.Json.dll .

if [ -d "Status" ]; then
	echo "Status dir exists"
else
	ln -s "/games/games-steam/steamapps/compatdata/359320/pfx/drive_c/users/steamuser/Saved Games/Frontier Developments/Elite Dangerous" Status
fi


#cat "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TTI_final.json" | grep -o time...........................
#exit 0

rm -f TrajectoryMapInterpolate.exe  

csc  -define:DEBUG -optimize -r:Newtonsoft.Json.dll TrajectoryMapTrajectory.cs TrajectoryMapDeserializer.cs TrajectoryMapInterpolate.cs
mono TrajectoryMapInterpolate.exe  #| grep DUMMY_FOR_ERROR_CORRECTION

