using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;



//[{"Latitude":-9.8379650000000005,"Longitude":98.004852,"Heading":246,"Flags":69206280,"TimeStamp":"2020-09-27T17:38:35.7264038Z","BodyName":"Hyldeptu 1 e","PlanetRadius":1800741.375,"Altitude":207,"Commander":"Sgurr","EventName":"Status","Health":-1,"PlayerControlled":true,"TargetedShipName":""},
//{"Latitude":-9.8394820000000003,"Longitude":98.002730999999997,"Heading":246,"Flags":69206280,"TimeStamp":"2020-09-27T17:38:36.7603569Z","BodyName":"Hyldeptu 1 e","PlanetRadius":1800741.375,"Altitude":217,"Commander":"Sgurr","EventName":"Status","Health":-1,"PlayerControlled":true,"TargetedShipName":""},
//...
//]


public class ServerTrajectoryDeserializer {
	public List<ServerTrajectoryWithHeight> stl {get; private set;}

//	string filename    = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.Tracking.json";
//	string filenameOut = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/Sgurr.TT.json";

	string filename;
	string filenameOut;


	List<ServerTrajectoryWithHeight> deserialize(string filename) {
		List<ServerTrajectoryWithHeight> ret = new List<ServerTrajectoryWithHeight> ();
		string jsonString = File.ReadAllText(filename);
		ret = JsonConvert.DeserializeObject<List<ServerTrajectoryWithHeight>>(jsonString);

		Debug.Log("deserialize() -  entries:"+ret.Count+" file:"+filename);

		foreach(ServerTrajectoryWithHeight t in ret) {
			if (t.Flags == 0) continue; //will ERROR when empty entries (comma misplaced)
//			Debug.Log(string.Format("tLat:{0:0.0000}",t.Latitude) + string.Format(" tLon:{0:0.0000}",t.Latitude) + string.Format(" tTeHe:{0:0.0000}",t.TerrainHeight));
		}

		return ret;
	}


	public ServerTrajectoryDeserializer(List<string> filenames, string filenameOut) { 
		//TODO: more than 2
		List<ServerTrajectoryWithHeight> addThis;

		stl = new List<ServerTrajectoryWithHeight> ();
		foreach (var file in filenames) {
			addThis  = deserialize(file);

		if ((addThis.Where(x => x.Commander==Const.dummy)).Count() > 0) {
			Debug.Log("ERROR: Dummies (e.g. DUMMY_FOR_ERROR_CORRECTION) found, skipping! File:"+file);
			continue;
		}

			stl.AddRange(addThis);
		}
		Debug.Log("--- total entries:"+stl.Count+" file:"+string.Join(", ",filenames));



		stl.Sort((x,y) => x.timestamp.CompareTo(y.timestamp));
	}

	public ServerTrajectoryDeserializer(string filename, string filenameOut) { 
		this.filename = filename;
		this.filenameOut = filenameOut;
		//string filename = "/tmp/w/elite-trajectory/Assets/Data_race5_hyldeptu/TTTT.json";

		stl = deserialize(filename);
	}

	static async void appendLineToFileAsync([System.Diagnostics.CodeAnalysis.NotNull] string path, string line, bool recreate=false) {
		if (string.IsNullOrWhiteSpace(path)) 
			throw new ArgumentOutOfRangeException(nameof(path), path, "Was null or whitepsace.");

		if (recreate)
			if (File.Exists(path))
				 File.Delete(path);

		using (var file = File.Open(path, FileMode.Append, FileAccess.Write))
		using (var writer = new StreamWriter(file)) {
			await writer.WriteLineAsync(line);
//			await writer.FlushAsync();
		}
	}

    bool isFirstSerializeLine=!Const.isContinue;
	public void Serialize(ServerTrajectoryWithHeight pointToOutput, bool isClose=false) {

		try {
			string comma=", ";
			if (isClose) {
				appendLineToFileAsync(filenameOut,"]");
				return;
			}

			if (isFirstSerializeLine) {
				appendLineToFileAsync(filenameOut, "[", true);
				isFirstSerializeLine=false;
				comma="";
			}

			appendLineToFileAsync(filenameOut, comma + JsonConvert.SerializeObject(pointToOutput));
		} catch (Exception) {
			Debug.Log("ERROR: Exception @ Serialize().");
			Debug.Log("FAILED_OUTPUT::"+JsonConvert.SerializeObject(pointToOutput));
		}
	}


	public void Serialize(List<ServerTrajectoryWithHeight> listToWrite, string file="") {
		if (file=="") file = filenameOut;
		System.IO.File.WriteAllText(file, 
			JsonConvert.SerializeObject(listToWrite)
				.Replace("},","},"+ System.Environment.NewLine)
				.Replace("[","["+ System.Environment.NewLine)
				.Replace("]",System.Environment.NewLine+"]")
			);
	}

}
