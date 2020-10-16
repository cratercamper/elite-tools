//
// TrajectoryMapTargets.sh
// - reads E:D trajectory (format from SRVTracker server)
// - 


using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;


public static class Const {
	public const string filename    = "/tmp/w/elite-trajectory/Assets/Data_Ninsun/ninsun-train00.json";
	public const string filenameOut = "/tmp/w/elite-trajectory/Assets/Data_Ninsun/ninsun-train00.TT.json";

//	public const string filename    = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Osashes.Tracking.json";
//	public const string filenameOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Osashes.TT.json";
//	public const string filename    = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.Tracking.json";
//	public const string filenameOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TT.json";

	public const string XDOTOOL="/usr/bin/xdotool";
	public const string BASH="/bin/bash";
	public const string CHMOD="/bin/chmod";

	public const bool isContinue=false; //continue with output file (do not rewrite, but append)

    public const bool isIgnoreUnfocused=true; //when true, operate only if user gives focus to Elite (script will not switch focus automatically)
	public const bool isGiveFocusBack=false;  //give focus back to where it was before script switched focus to Elite (effect only if !isIgnoreUnfocused)

	public const bool isAutopilotDisabled=false; //when false, only show bearing and distance to target, do not send commands to E:D window (see pointExclusion below)
//	public const bool isAutopilotDisabled=true;

	public const double exclusionRadius=2.5; //km  //avoid flying into Weierstrass lab at Hyldeptu

	public const float maxDistanceToPointForMeasurement=0.03f; //km

	public const string dummy="DUMMY_FOR_ERROR_CORRECTION";

}


public static class Debug { public static void Log(string s) { Console.WriteLine(s); } }



public static class Time {
	public static double time = ((float)DateTime.Now.Hour *60.0f*60.0f+ ((float)DateTime.Now.Minute*60.0f) + ((float)DateTime.Now.Second)+ (((float)DateTime.Now.Millisecond) * 0.001f));
	public static double timeStart = -1;
	public static double timeSinceStart = -1;

	public static void update() {
		time = 0.001 * (double) (-63737530119000+( DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)); //seconds from 2020-10-05T21:30 :)
		if (timeStart < 0) timeStart = time;
		timeSinceStart = time - timeStart;
		//Debug.Log("update(): Time.time is "+Time.time);
	}

}

public class TrajectoryMapAutopilot {
	int windowMy=-1; //handle to script window (store, get focus to Elite, restore our focus)
	int windowElite=-1; //handle to script window (store, get focus to Elite, restore our focus)
	int windowCurr=-1;
	Orientation autopilotTarget;



	double dist(Orientation o) {
		return Orientation.DistanceBetween(o, autopilotTarget);
	}

	double bear(Orientation o) {
		return Orientation.BearingToLocation(o, autopilotTarget);
	}

	public TrajectoryMapAutopilot () {
	}

	public void setTarget(Orientation targetNew) {
		this.autopilotTarget = targetNew;
	}

	public void display(Orientation newO) {
		if (newO == null) {
			Debug.Log("WARN: newO is NULL!");
			return;
		};

		Debug.Log(""
			+ string.Format("     HEAD: {0:0.00}",Orientation.BearingToLocation(newO, autopilotTarget))
			+ string.Format(" dLat:{0:0.0000}",(autopilotTarget.Latitude-newO.Latitude))
			+ string.Format(" dLon:{0:0.0000}",(autopilotTarget.Longitude-newO.Longitude))
			+ string.Format(" dist:{0:0.000}",Orientation.DistanceBetween(autopilotTarget,newO)));
	}


	bool isNextNudgeLeft=true;
	void nudge() {
		//send pips (left+right arrow) to force faster updates
//		getWindowHandles();
//		if (windowElite < 0) { return; }
//		if (windowMy < 0) { return; }
//		runTool("windowactivate "+windowElite);

		//nudge expects Elite has focus

		System.Threading.Thread.Sleep(100);

		if (isNextNudgeLeft) {
			runTool("key Left"); runTool("key Left");
		} else {
			runTool("key Right"); runTool("key Right");
		}
			
		runTool("key Up"); runTool("key Up");
		//System.Threading.Thread.Sleep(100);

		System.Threading.Thread.Sleep(100);
//		runTool("windowactivate "+windowMy);

		isNextNudgeLeft=!isNextNudgeLeft;
	}


	List<string> cmdsLast;

	public void stop() {
		List<string> cmds = new List<string>();
		cmds.Add("Stop");
		sendCommandsToElite(cmds);
	}

	public void increaseAltitude() {
		List<string> cmds = new List<string>();
		cmds.Add("Up");
		sendCommandsToElite(cmds);
	}

	public void decreaseAltitude() {
		List<string> cmds = new List<string>();
		cmds.Add("Down");
		cmds.Add("Freeze");
		sendCommandsToElite(cmds);
	}


	Random rnd = new Random();
	public void update(Orientation newO) {
		if (newO == null) return;
		Debug.Log("update(): newO:"+newO.ToString());

		List<string> cmds = new List<string>();


		double newoh = newO.Heading;
		double h = bear(newO);

		if (bear(newO) > newoh) {
			if(bear(newO) > 180.0 + newoh)
				newoh += 360.0;
			}

		if (h < newO.Heading) {
			if(h < -180.0 + newO.Heading)
				h += 360.0;
		}

/*
		if      (bear(newO) >  180+newO.Heading) cmds.Add("Left");
		else if (bear(newO) < -180+newO.Heading) cmds.Add("Right");
		else if (bear(newO) > newO.Heading) cmds.Add("Right");
		else if (bear(newO) < newO.Heading) cmds.Add("Left");
*/



		string right="Right";
		string left="Left";

		if ( dist(newO) < 2.0 ) { cmds.Add("Stop"); } 

		if (dist(newO) < 0.6) { cmds.Add("Freeze"); } //force slower reactions to avoid overshooting when near & data slow

		if ( dist(newO) < 2.0 ) { 
			if (dist(newO) > 0.5) { cmds.Add("Go"); cmds.Add("Go"); cmds.Add("Go"); cmds.Add("Go"); cmds.Add("Go"); }
			if (dist(newO) > 0.2) { cmds.Add("Go"); cmds.Add("Go"); cmds.Add("Go"); cmds.Add("Go"); cmds.Add("Go"); }
		}

		if ( (Math.Abs(h - newoh) > 5.0)  && (dist(newO)  < 1.5) ) { cmds.Add("Stop"); } 


		if   (Math.Abs(h - newoh) > 7.0)    { right = "RightFar"; left = "LeftFar";}
		if ( (Math.Abs(h - newoh) > 25.0) ) { right = "RightFarFar"; left = "LeftFarFar";}

		if ( (Math.Abs(h - newoh) <= 20.0) && (dist(newO) >= 5.0) )  { cmds.Add("GoFastTurbo"); } else
		if ( (Math.Abs(h - newoh) <= 10.0) && (dist(newO) >= 2.0) )   { cmds.Add("GoFast"); }
		if (                                   dist(newO) >= 2.0 )    { cmds.Add("Go");}
		if (                                   dist(newO) >= 0.05 )   { cmds.Add("Go");}
		if ( (Math.Abs(h - newoh) <= 5.0) ) cmds.Add("Go");
		


		if      (h > newoh) cmds.Add(right);
		else if (h < newoh) cmds.Add(left);


		if ( (dist(newO) < 0.15) &&  (Math.Abs(h-newoh) > 160) && (Math.Abs(h-newoh) < 200) ) {
			cmds = new List<string>(); 
			cmds.Add("Stop");
			cmds.Add("Back");
			cmds.Add("Back");
			cmds.Add("Back");
			cmds.Add("Back");
		}


		if (rnd.Next(15) < 3) cmds.Add("Go"); //might prevent endless rotations on the same spot


		cmdsLast = cmds;

		if (Const.isAutopilotDisabled) {
			Debug.Log("                                                                                 autopilot disabled, ignoring commands:'>>> "+string.Join(", >>>",cmds)+"'...");
			sleep(1000);
		} else {
			sendCommandsToElite(cmds);
		}
	}

	public void repeatLastCommands() {
		//we obtain two measurements when reaching target point (either we can select or inter-/extra-polate)
		if (Const.isAutopilotDisabled) return;
		if (cmdsLast == null) return;

		sendCommandsToElite(cmdsLast);
		cmdsLast = null;
	}

	bool isEliteFocused() {
		//getWindowHandles(); //will fill windowElite - but is called already in sendCommandsToElite() before this function runs

		return (windowCurr == windowElite);
	}

	void sleep(int ms) {
		System.Threading.Thread.Sleep(ms);
	}

	void runToolMany(int times, string cmd, string tool=null) {
		for (int i=0; i<times; i++) {

			if (tool!=null) 
				runTool(cmd,tool);
			else
				runTool(cmd);
		}
	}

	void sendCommandsToElite(List<string> cmds) {
		getWindowHandles();

		if (Const.isIgnoreUnfocused) {
			if (!isEliteFocused()) { Debug.Log(" -- ignored ----------------------------------------------------------------"); sleep(6000); return;}
		} else {
			if (windowElite < 0) { return; }
			runTool("windowactivate "+windowElite);
		}
		sleep(100);


		foreach (string c in cmds) {
			Debug.Log(">>> "+c);

			if (c=="Right")   { runToolMany(12, "key D"); }
			if (c=="Left")    { runToolMany(12, "key A"); }
			if (c=="RightFar")   { runToolMany(30, "key D"); }
			if (c=="LeftFar")    { runToolMany(30, "key A"); }
			if (c=="RightFarFar")   { runToolMany(100, "key D"); }
			if (c=="LeftFarFar")    { runToolMany(100, "key A"); }
			if (c=="Stop")    { runToolMany(12, "key X");}
			if (c=="GoFast")  { runToolMany(12, "key KP_7");}
			if (c=="GoFastTurbo")  { runToolMany(12, "key Tab");}
			if (c=="Go") 	  { runToolMany(10, "key W"); }
			if (c=="Back") 	  { runToolMany(12, "key S"); }
			if (c=="Freeze") 	  { sleep(700); }
			if (c=="Up") 	  { runToolMany(8, "key R"); }
			if (c=="Down") 	  { runToolMany(8, "key F"); }
		}

		


		sleep(300);


		nudge(); //only call when we know Elite has focus!

		if (windowMy < 0) { return; }
		if (Const.isGiveFocusBack) runTool("windowactivate "+windowMy);
	}

	void getWindowHandles() {
		string output="-failed-to-run-";

		if (windowElite < 0) {
			prepareShellScript("./getEliteWindow.sh");
			output = runTool("./getEliteWindow.sh", Const.BASH);
			Debug.Log("bash:"+output);

			if (Int32.TryParse(output, out int j)) {
				windowElite = j;
				Debug.Log("windowElite:"+windowElite);
			} else {
				Debug.Log("ERROR: Failed to parse windowElite handle ('./getEliteWindow.sh' output:"+output+")!");
			}
		}

		if (windowMy < 0) {
			output = runTool("getwindowfocus");
			if (Int32.TryParse(output, out int j)) {
				windowMy = j;
				Debug.Log("windowMy:"+windowMy);
			} else {
				Debug.Log("ERROR: Failed to parse window handle ('xdotool getwindowfocus' output:"+output+")!");
			}
		}

		if (true) {
			output = runTool("getwindowfocus");
			if (Int32.TryParse(output, out int j)) {
				windowCurr = j;
//				Debug.Log("windowCurr:"+windowCurr+" elite win:"+isEliteFocused());
			} else {
				Debug.Log("ERROR: Failed to parse window handle ('xdotool getwindowfocus' output:"+output+")!");
			}
		}
	}

	string runTool(string paramz, string tool=Const.XDOTOOL) {
		//System.Diagnostics.Process.Start($"/usr/bin/xdotool", "getwindowfocus");
		System.Diagnostics.Process p = new System.Diagnostics.Process();
		p.StartInfo = new System.Diagnostics.ProcessStartInfo(tool,paramz);
		p.StartInfo.RedirectStandardOutput = true;
		p.StartInfo.UseShellExecute = false;
		p.Start();
		string output = "";
		while ( ! p.HasExited ) {
			output += p.StandardOutput.ReadToEnd();
		}

		return output;
	}

	void prepareShellScript(string filename) {
		string[] script={
"#!/bin/bash",
"WIN_ELITE=$(wmctrl -lx | grep elitedangerous64.exe | grep CLIENT | sed -e 's/ .*//')",
"WIN_ELITE_DEC=$(printf '%i' $WIN_ELITE)",
"echo $WIN_ELITE_DEC "};

		System.IO.File.WriteAllLines(filename, script);
		runTool("+x "+filename, Const.CHMOD);

	}
}

public class TrajectoryMapTargets {


	ServerTrajectoryWithHeight orientation;

	double timeNextStatusFilePoll = 0.0;
	double timeNextStatusFilePollDelta = 0.7;


	void updateCurrentState(string newData) {
		//set Lat, Lon & Alt
//		Debug.Log("updateCurrentState():"+newData);

		ServerTrajectoryWithHeight o = JsonConvert.DeserializeObject<ServerTrajectoryWithHeight>(newData);

		Debug.Log("==> o.Head: "+o.Heading+"  o.Lat:"+o.Latitude+" o.Lon:"+o.Longitude+" o.Alt:"+o.Altitude+" o.t:"+o.timestamp);

		if (o.Latitude != Double.MaxValue && o.Longitude != Double.MaxValue && o.Altitude != int.MaxValue && o.Heading != int.MaxValue) {
			orientation = o;
			Debug.Log(" - setting new curr pos!:"+orientation.Latitude+" "+orientation.Longitude+" "+orientation.Altitude+" head:"+o.Heading+" radius:"+o.PlanetRadius);
		} else {
			if (orientation !=null)
			Debug.Log(" - keeping old values:"+orientation.Latitude+" "+orientation.Longitude+" "+orientation.Altitude+" head:"+o.Heading+" radius:"+o.PlanetRadius);
		}
	}

	void pollStatusFile() {
			Time.update();
			if (Time.time > timeNextStatusFilePoll) {
				timeNextStatusFilePoll = (float) Time.time + timeNextStatusFilePollDelta;

				string newData = statusFileReader.ProcessStatusFileUpdate();
				Debug.Log("newData:"+newData);
				if (!System.String.IsNullOrEmpty(newData)) {
					updateCurrentState(newData);
				}
			}
	}

	string statusFile="Status/Status.json";
	StatusFileReader statusFileReader;
	TrajectoryMapAutopilot autopilot;

	int totalCntFound;


	public void establishFlightAltitude() {

		autopilot.stop();
		pollStatusFile();
		System.Threading.Thread.Sleep(2000);

		//go up - 20 m above wanted level
		do {
			autopilot.increaseAltitude();
			pollStatusFile();
		} while (orientation.Altitude<(20+TrajectoryMapTargets.pointZero.Altitude));

		//go down to wanted level
		do {
			autopilot.decreaseAltitude();
			pollStatusFile();
		} while (orientation.Altitude>TrajectoryMapTargets.pointZero.Altitude);
	}


	public void loop() {
		bool isEstablishAltitudeAtFirstPoint=!Const.isContinue;


		Time.update();
		totalCntFound=0;
//		for (int i=0;i<1000;i++) {
		List<ServerTrajectoryWithHeight> wayToCoverNew = new List<ServerTrajectoryWithHeight>();
		double dist0=Double.MaxValue;
		bool reached = false;
		int round = -1;


		while (wayToCover.Count > 1) {
			round++;

			Debug.Log("======================================================================");
			Debug.Log(string.Format("== ROUND {0} == left: {1} ==========================================", round, wayToCover.Count));
			Debug.Log("======================================================================");

			wayToCoverNew.Add(pointZero); //first point is visited each round so we can calculate up/down error & subtract it from all results (interpolation) - may happen when pitch is not perfect 0.0

			int roundFound = 0;
			int roundFailed = 0;
			int roundSkipped = 0;

			int cntAdded=-1;
			int cntNextDummy=-1;
			int cntNextDummyPeriod=100;

			int index=-1;
			foreach (ServerTrajectoryWithHeight target in wayToCover) {
				index++;
				Debug.Log(string.Format("- target: Lat:{0:0.0000}",target.Latitude)+ " " + string.Format("Lon:{0:0.0000}",target.Longitude));
				reached = false;


				//skipping point not to get killed
				if (ServerTrajectoryWithHeight.DistanceBetween(target, pointExclusion) < Const.exclusionRadius) {
					target.TerrainHeight=Double.MinValue;
					std.Serialize(target);
					roundSkipped++;
					Debug.Log(string.Format(" no. {0} - skipped, exclusion.",index));
					continue;
				}

				//skipping point not to get killed
				if ( (target.Latitude == 0.0 ) && (target.Longitude == 0.0) && (target.Flags == 0) ){
					target.TerrainHeight=Double.MinValue;
					std.Serialize(target);
					roundSkipped++;
					Debug.Log(string.Format(" no. {0} - skipped, non-location.",index));
					continue;
				}

				autopilot.setTarget(target);

				double pointZeroOverride=0.0;
				if (target==TrajectoryMapTargets.pointZero) pointZeroOverride=1000.0;

				for (int i=0;i<20.0+pointZeroOverride+(round*50.0);i++) { //we absolutely must have zero point dummies reached, increase tries with every round
					pollStatusFile(); //fills orientation var
					System.Threading.Thread.Sleep(100);
					if (orientation == null) { Debug.Log(string.Format("WARN: Orientation is NULL! Check Elite is filling Status.json file... {0} data:{1}",i, target)); i--; continue; }
					autopilot.update(orientation);
					System.Threading.Thread.Sleep(100);
					autopilot.display(orientation);
					dist0 = ServerTrajectoryWithHeight.DistanceBetween(target, orientation);


					if (dist0 < Const.maxDistanceToPointForMeasurement) {reached = true ; break;}
				}

				if (!reached) {
					cntAdded++;

					if (cntAdded > cntNextDummy) {
						wayToCoverNew.Add(TrajectoryMapTargets.pointZero); //all trajectories must start and end with pointZero - error (difference) will be subtracted (with interpolation)
						cntNextDummy += cntNextDummyPeriod;                  //also we add dummies in the middle to catch possible non-linearities
						Debug.Log(" + dummy @ pos:"+cntAdded);
					}

					wayToCoverNew.Add(target);
					roundFailed++;
				} else {

					if (isEstablishAltitudeAtFirstPoint) {
						establishFlightAltitude();
						isEstablishAltitudeAtFirstPoint=false;
					}

					//terrain height measure close enough to target point
					double height0 = orientation.Altitude; 

					System.Threading.Thread.Sleep(100);
					autopilot.repeatLastCommands();
					System.Threading.Thread.Sleep(100);
					pollStatusFile(); //fills orientation var
					System.Threading.Thread.Sleep(100);


					target.TerrainHeight = height0;

					double dist1 = ServerTrajectoryWithHeight.DistanceBetween(target, orientation);
					double height1 = orientation.Altitude;

					if (dist1 < dist0) {
						Debug.Log(string.Format("- taking better ({0:0.0000} vs {1:0.0000} measurement height:{2:0.0000} vs {3:0.0000}",dist1, dist0, height1, height0));
						target.TerrainHeight = height1;
					}

					wayToCoverFinal.Add(target);
					roundFound++;
					std.Serialize(target);

					Debug.Log("!                                                ");
					Debug.Log("!   FOUND! FOUND! FOUND! FOUND! FOUND! FOUND! FOUND! ");
					Debug.Log("!       Altitude: "+target.TerrainHeight);
					Debug.Log("!       (found:"+roundFound+"+fail:"+roundFailed+"skip:"+roundSkipped+"/"+wayToCover.Count+")             ");
					totalCntFound++;
				}
			}


			wayToCover = wayToCoverNew;
			wayToCoverNew = new List<ServerTrajectoryWithHeight>();
		}

		Time.update();
		Debug.Log("----------------------------------------------");
		Debug.Log(string.Format("-- timeSinceStart: {0:0.0} s -------------------",Time.timeSinceStart));
		Debug.Log(string.Format("---time/point: {0:0.0} s -----------------------",Time.timeSinceStart/(double)totalCntFound));
		Debug.Log("Finished+++");
		Debug.Log("----------------------------------------------");
	}



//	List<Orientation> wayToCover;
//	List<ServerTrajectoryWithHeight> stl
	List<ServerTrajectoryWithHeight> wayToCover;
	List<ServerTrajectoryWithHeight> wayToCoverFinal;

	public static ServerTrajectoryWithHeight pointZero=null;
	public static ServerTrajectoryWithHeight pointExclusion=null;

	public TrajectoryMapTargets(List<ServerTrajectoryWithHeight> stlFromFile) {

		//Status.json - reading ship position
		statusFileReader = new StatusFileReader(statusFile);

		//robot piloting to next waypoint (next datapoint of which we want to measure terrain height)
		autopilot = new TrajectoryMapAutopilot();

		//lists with source and final data (final contain additional  TerrainHeight)
		this.wayToCover      = new List<ServerTrajectoryWithHeight>();
		this.wayToCoverFinal = new List<ServerTrajectoryWithHeight>();

		pointZero = new ServerTrajectoryWithHeight();
		pointZero.Latitude  =-59.957718; //NINSUN - Kube-McDowell Enterprise
		pointZero.Longitude =-62.621376;
		pointZero.PlanetRadius=2174714.25;
		pointZero.Altitude=3000; //used @ start

//		pointZero = new ServerTrajectoryWithHeight();
//		pointZero.Latitude  = -9.680371; //FLYNN
//		pointZero.Longitude = 98.182648; //FLYNN
//		pointZero.PlanetRadius=1800741.375;
//		pointZero.Altitude=3000; //used @ start

		pointExclusion = new ServerTrajectoryWithHeight();
		pointExclusion.Latitude  = -10.02166; //WEIER
		pointExclusion.Longitude = 97.635475; //WEIER
		pointExclusion.PlanetRadius=1800741.375;


		if (TrajectoryMapTargets.pointZero == null) {
			TrajectoryMapTargets.pointZero.Latitude  = stlFromFile[0].Latitude;
			TrajectoryMapTargets.pointZero.Longitude = stlFromFile[0].Longitude;
		}

		TrajectoryMapTargets.pointZero.Commander = Const.dummy;

		int cntAdded=-1;
		int cntNextDummy=-1;
		int cntNextDummyPeriod=100; //how much points to measure before return to point zero & create new dummy

		Debug.Log("Points from file:"+stlFromFile.Count);

		//we want 2 dummies at start, this is first, second is right below in foreach (we want 2 because of altitude establishing - we want to have the correct altitude stored in the 2nd dummy in record)
		this.wayToCover.Add(TrajectoryMapTargets.pointZero); 

		foreach(ServerTrajectoryWithHeight point in stlFromFile) {
			cntAdded++;
			if (cntAdded > cntNextDummy) {
				this.wayToCover.Add(TrajectoryMapTargets.pointZero); //all trajectories must start and end with pointZero - error (difference) will be subtracted (with interpolation)
				cntNextDummy += cntNextDummyPeriod;                  //also we add dummies in the middle to catch possible non-linearities
				Debug.Log(" + dummy @ pos:"+cntAdded);
			}
			this.wayToCover.Add(point);
		}

		this.wayToCover.Add(TrajectoryMapTargets.pointZero); //all trajectories must start and end with pointZero - error (difference) will be subtracted (with interpolation)
		Debug.Log(" + dummy @ pos:"+cntAdded);
	}



	static ServerTrajectoryDeserializer std;
    public static void Main(string[] args) {
		std = new ServerTrajectoryDeserializer(Const.filename, Const.filenameOut);

		TrajectoryMapTargets tmt = new TrajectoryMapTargets(std.stl);
//		System.Threading.Thread.Sleep(2000);
		tmt.loop();
		std.Serialize(null, true);
	}
}

