using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;




public class ServerTrajectory : Orientation {
//	public double Latitude         { get; set; }
//	public double Longitude        { get; set; }
//	public int Heading             { get; set; }
	public long Flags              { get; set; }
//	public string TimeStamp        { get; set; }
//	public string BodyName         { get; set; }
//	public double PlanetRadius     { get; set; }
//	public int Altitude            { get; set; }
	public string Commander        { get; set; }
	public string EventName        { get; set; }
	public double Health           { get; set; }
	public bool PlayerControlled   { get; set; }
	public string TargetedShipName { get; set; }
}


public class ServerTrajectoryWithHeight : ServerTrajectory {
	public double TerrainHeight = 0.0;

	public override string ToString() {
		return string.Format("|| Lat:{0:0.0000} | Lon:{1:0.0000} | Cmdr:{2} | TerrainHeight:{3:0.0}",Latitude,Longitude,Commander,TerrainHeight);
	}
}

//[{"Latitude":-9.8379650000000005,"Longitude":98.004852,"Heading":246,"Flags":69206280,"TimeStamp":"2020-09-27T17:38:35.7264038Z","BodyName":"Hyldeptu 1 e","PlanetRadius":1800741.375,"Altitude":207,"Commander":"Sgurr","EventName":"Status","Health":-1,"PlayerControlled":true,"TargetedShipName":""},
//{"Latitude":-9.8394820000000003,"Longitude":98.002730999999997,"Heading":246,"Flags":69206280,"TimeStamp":"2020-09-27T17:38:36.7603569Z","BodyName":"Hyldeptu 1 e","PlanetRadius":1800741.375,"Altitude":217,"Commander":"Sgurr","EventName":"Status","Health":-1,"PlayerControlled":true,"TargetedShipName":""},
//...
//]



/*
{ 
"timestamp":"2020-10-05T19:36:13Z",
"event":"Status",
"Flags":287375368,
"Pips":[4,8,0],
"FireGroup":0,
"GuiFocus":0,
"Fuel":{ "FuelMain":15.500000, "FuelReservoir":0.454908 },
"Cargo":0.000000,
"LegalState":"Clean",
"Latitude":-9.687840,
"Longitude":98.148560,
"Heading":53,
"Altitude":1876,
"BodyName":"Hyldeptu 1 e",
"PlanetRadius":1800741.375000 }
*/

public class Orientation {
	public string timestamp;
	public double Latitude=Double.MaxValue;
	public double Longitude=Double.MaxValue;
	public int Altitude=int.MaxValue;
	public int Heading=int.MaxValue;
	public string BodyName;
	public double PlanetRadius;



	public override string ToString() {
		return string.Format("|| Lat:{0:0.0000} | Lon:{1:0.0000}",Latitude,Longitude);
	}

	public static double DistanceBetween(Orientation location1, Orientation location2) {
		double R = location1.PlanetRadius;
		if (R <= 0) {
			R = location2.PlanetRadius;
			if (R <= 0)
				Debug.Log("ERROR: points without PlanetRadius!");
				return 0;
		}
		var lat = ConvertToRadians(location2.Latitude - location1.Latitude);
		var lng = ConvertToRadians(location2.Longitude - location1.Longitude);
		var h1 = Math.Sin(lat / 2) * Math.Sin(lat / 2) +
					  Math.Cos(ConvertToRadians(location1.Latitude)) * Math.Cos(ConvertToRadians(location2.Latitude)) *
					  Math.Sin(lng / 2) * Math.Sin(lng / 2);
		var h2 = 2 * Math.Asin(Math.Min(1, Math.Sqrt(h1)));
		return 0.001 * Math.Abs(R * h2);
	}

	public static double BearingToLocation(Orientation sourceLocation, Orientation targetLocation) {
		var dLon = ConvertToRadians(targetLocation.Longitude - sourceLocation.Longitude);
		var dPhi = Math.Log(
			Math.Tan(ConvertToRadians(targetLocation.Latitude) / 2 + Math.PI / 4) / Math.Tan(ConvertToRadians(sourceLocation.Latitude) / 2 + Math.PI / 4));
		if (Math.Abs(dLon) > Math.PI)
			dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
		return ConvertToBearing(Math.Atan2(dLon, dPhi));
	}

	private static double ConvertToRadians(double angle) {
		return (Math.PI / 180) * angle;
	}

	public static double ConvertToDegrees(double radians) {
		return radians * 180 / Math.PI;
	}

	public static double ConvertToBearing(double radians) {
		// convert radians to degrees (as bearing: 0...360)
		return (ConvertToDegrees(radians) + 360) % 360;
	}

}

