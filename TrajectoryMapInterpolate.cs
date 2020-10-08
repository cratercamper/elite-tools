//
// TrajectoryMapInterpolate.cs - process file create by TrajectoryMapTargets.cs 
//   - interpolate - fix non-0-pitch of the ship while taking terrain height measurements - search for dummies, interpolate to zero out the effect of ship going up/down
//   - sanitize - replace entries with undefined Terrain Height (i.e. Double.MinValue) - by putting there last known terrain height (TODO: also interpolate - to following sane height that is in future)
//   - merge - put more files together, sort by time
//   - change - change TerrainHeight to (3000 - TerrainHeight) - from direct Altitude measurement from ship to real terrain height (0.0 height is starting point)
//

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;


public static class Const {
//	public const string filenameInterIn  = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TT.json";

//	public const string filenameInterIn  = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TT_1of2.json";
//	public const string filenameInterOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TTI_1of2.json";

	public const string filenameInterIn  = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Osashes.TT.json";
	public const string filenameInterOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Osashes.TTI.json";

//	public const string filenameMergeIn0 = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TTI_2of2.json";
//	public const string filenameMergeIn1 = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TTI_1of2.json";
//	public const string filenameMergeOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TTI_final.json";

	public const string filenameMergeIn0 = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Osashes.TTI_final.json";
	public const string filenameMergeOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Osashes.TTI_final.json";

	public const string filenameChangeIn  = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TTI_final.json";
	public const string filenameChangeOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TTI_final.json";

//	public const string filenameChangeIn  = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Osashes.TTI.json";
//	public const string filenameChangeOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Osashes.TTI_final.json";

//	public const string filenameInterIn  = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TT_2of2.json";
//	public const string filenameInterOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TTI_2of2.json";

	public const bool isContinue=false;
	public const string dummy="DUMMY_FOR_ERROR_CORRECTION";
}

public static class Debug { public static void Log(string s) { Console.WriteLine(s); } }


public class TrajectoryMapInterpolate {
	List<ServerTrajectoryWithHeight> points;

	public TrajectoryMapInterpolate(List<ServerTrajectoryWithHeight> stlFromFile) {
		points = stlFromFile;

		int i=-1; foreach(var point in points) { i++; Debug.Log(i+" "+point); }

	}


	List<int> dummyIndexes() {
		List<int> ret = new List<int>();

		int i=-1;
		foreach(var point in points) {
			i++;
			if (point.Commander==Const.dummy) ret.Add(i);
		}

		return ret;
	}

	void adjustPoint(int i, double diff) {
		string printThis=""+points[i];

		points[i].TerrainHeight += diff;

//		Debug.Log("  "+i+" diff:"+diff+" adjustPoint(): "+printThis+" ---> "+string.Format("{0:0.0000}",points[i].TerrainHeight));
	}

	void InterpolateBetween(int start, int end, List<int> dummies, bool isTrailingAfterEnd=false) {
		int iStart = dummies[start];
		int iEnd   = dummies[end];


		double diffStartShift = - (points[iStart].TerrainHeight - points[dummies[0]].TerrainHeight) ;

		double heightStart=points[iStart].TerrainHeight;
		double heightEnd=points[iEnd].TerrainHeight;
		int len = iEnd - iStart;
		double step = - (heightEnd - heightStart) / (double) len;

		double diff=diffStartShift; //start dummy is already shifted versus the very first dummy

		if (!isTrailingAfterEnd) {
			Debug.Log(string.Format("InterpolateBetween({0},{1}): {2:0.000}->{3:0.000} in {4} indices => step: {5:0.000}",iStart,iEnd, heightStart, heightEnd, len, step));

			for (int i=iStart+1; i<iEnd;i++) {
				diff += step;
				adjustPoint(i,diff);
			}
		} else {
			Debug.Log(string.Format("InterpolateBetween({0},{1} - trailing after {1}): {2:0.000}->{3:0.000} in {4} indices => step: {5:0.000}",iStart,iEnd, heightStart, heightEnd, len, step));
			diff -=(heightEnd - heightStart);

			for (int i=iEnd+1; i<points.Count;i++) {
				diff += step;
				adjustPoint(i,diff);
			}
		}
	}

	void interpolate() {
		List<int> dummies = dummyIndexes();
		Debug.Log("Dummies: ["+string.Join(", ",dummies)+"]");

		int i=-1;
		foreach(var dummyIndex in dummies) {
			i++;
			if (i+1 < dummies.Count) {
				InterpolateBetween(i,i+1,dummies);
			} else { 
				InterpolateBetween(i-1,i,dummies, true);
			}
		}
	}

	void sanitize() {
		//maintain the TerrainHeight in following points where it was unknown
		double lastSaneValue=Double.MinValue;
		for(int i=0;i<points.Count-1;i++) {
			if (points[i].Commander!=Const.dummy)
				if (points[i].TerrainHeight!=Double.MinValue)
					lastSaneValue=points[i].TerrainHeight;

			if (points[i+1].Commander!=Const.dummy) {
				if (points[i+1].TerrainHeight==Double.MinValue) 
					points[i+1].TerrainHeight=1.3+lastSaneValue;
			}
		}

		//go backwards to sanitize initial points
		for(int i=points.Count-1;i>0;i--) {
			if (points[i].Commander!=Const.dummy)
				if (points[i].TerrainHeight!=Double.MinValue)
					lastSaneValue=points[i].TerrainHeight;

			if (points[i-1].Commander!=Const.dummy) {
				if (points[i-1].TerrainHeight==Double.MinValue) 
					points[i-1].TerrainHeight=lastSaneValue;
			}
		}
	}

	void removeDummies() {
		int printThis = points.Count;
		points.RemoveAll(x => x.Commander == Const.dummy);
		Debug.Log("removeDummies(): points cnt: "+printThis+" ---> "+points.Count);
	}

	void change() {
		Debug.Log("change()");
		points.ToList().ForEach(x => x.TerrainHeight = 3000.0 - x.TerrainHeight );

//		foreach(var point in points) { Debug.Log("xxxx "+point); }
	}

    public static void Main(string[] args) {
		ServerTrajectoryDeserializer std;
		TrajectoryMapInterpolate tmi;
/*
		ServerTrajectoryDeserializer std = new ServerTrajectoryDeserializer(Const.filenameChangeIn, Const.filenameChangeOut);
		TrajectoryMapInterpolate tmi = new TrajectoryMapInterpolate(std.stl);
		tmi.change();
		std.Serialize(tmi.points, Const.filenameChangeOut);

		return;
*/

//	sanitize, interpolate
		std = new ServerTrajectoryDeserializer(Const.filenameInterIn, Const.filenameInterOut);
		tmi = new TrajectoryMapInterpolate(std.stl);
		List<string> filelist = new List<string> ();

		tmi.sanitize();
		tmi.interpolate();
		tmi.removeDummies();
		std.Serialize(tmi.points, Const.filenameInterOut);

// merge, remove dummies, sort by time
//		List<string> filelist = new List<string> () {Const.filenameMergeIn0, Const.filenameMergeIn1};
//		ServerTrajectoryDeserializer std = new ServerTrajectoryDeserializer(filelist, Const.filenameInterOut);

		filelist.Add(Const.filenameMergeIn0);
		std = new ServerTrajectoryDeserializer(filelist, Const.filenameInterOut);
		std.Serialize(std.stl, Const.filenameMergeOut);


//		ServerTrajectoryDeserializer std = new ServerTrajectoryDeserializer(Const.filenameChangeIn, Const.filenameChangeOut);
		std = new ServerTrajectoryDeserializer(Const.filenameChangeIn, Const.filenameChangeOut);
		tmi = new TrajectoryMapInterpolate(std.stl);
		tmi.change();
		std.Serialize(tmi.points, Const.filenameChangeOut);

	}
}

