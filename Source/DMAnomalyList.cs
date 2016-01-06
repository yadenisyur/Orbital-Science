﻿#region license
/* DMagic Orbital Science - DMAnomalyList
 * An class to update and store info on anomalies
 *
 * Copyright (c) 2014, David Grandy <david.grandy@gmail.com>
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, 
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice, 
 * this list of conditions and the following disclaimer in the documentation and/or other materials 
 * provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used 
 * to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic
{
	public class DMAnomalyList : MonoBehaviour
	{
		//private Dictionary<string, Dictionary<string, DMAnomalyObject>> anomalies = new Dictionary<string, Dictionary<string, DMAnomalyObject>>();
		//private List<DMAnomalyObject> currentBodyAnomalies = new List<DMAnomalyObject>();
		private static bool scannerUpdating;
		//private bool loaded = false;
		private static Dictionary<string, DMAnomalyStorage> anomalies = new Dictionary<string, DMAnomalyStorage>();

		//private void Start()
		//{
		//	GameEvents.onVesselSOIChanged.Add(SOIChange);
		//}

		//private void OnDestroy()
		//{
		//	GameEvents.onVesselSOIChanged.Remove(SOIChange);
		//}

		//private void Update()
		//{
		//	if (HighLogic.LoadedSceneHasPlanetarium && !loaded)
		//	{
		//		pqsBuild(FlightGlobals.currentMainBody);
		//		loaded = true;
		//	}
		//}

		//public List<DMAnomalyObject> anomObjects()
		//{
		//	return currentBodyAnomalies;
		//}

		public static DMAnomalyObject getAnomalyObject(string body, string city)
		{
			if (!anomalies.ContainsKey(body))
			{
				DMUtils.Logging("No anomaly of name [{0}] found for body [{1}]", city, body);
				return null;
			}

			return anomalies[body].getAnomaly(city);

			//if (anomalies.ContainsKey(body))
			//{

			//	if (anomalies[body].ContainsKey(city))
			//		return anomalies[body][city];
			//	else
			//		DMUtils.Logging("No anomaly of name [{0}] found for body [{1}]", city, body);
			//}
			//else
			//	DMUtils.Logging("No anomalies found for body [{0}]", body);

			//return null;
		}

		public static DMAnomalyStorage getAnomalyStorage(int index)
		{
			if (anomalies.Count > index)
				return anomalies.ElementAt(index).Value;

			return null;
		}

		public static DMAnomalyStorage getAnomalyStorage(string body)
		{
			if (anomalies.ContainsKey(body))
				return anomalies[body];

			return null;
		}

		public static void clearAnomalies()
		{
			anomalies.Clear();
		}

		public static bool ScannerUpdating
		{
			get { return scannerUpdating; }
			internal set { scannerUpdating = value; }
		}

		public static int AnomalyCount
		{
			get { return anomalies.Count; }
		}

		//private void SOIChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> VB)
		//{
		//	StartCoroutine(updateCoordinates(VB.to));
		//}

		public static void updateCoordinates(CelestialBody b)
		{
			//yield return new WaitForSeconds(3);

			//currentBodyAnomalies.Clear();

			if (anomalies.ContainsKey(b.name))
			{
				for (int i = 0; i < anomalies[b.name].AnomalyCount; i++)
				{
					DMAnomalyObject anom = anomalies[b.name].getAnomaly(i);

					if (anom == null)
						continue;

					anom.WorldLocation = b.GetWorldSurfacePosition(anom.Lat, anom.Lon, anom.Alt);
					//anom.WorldLocation = anom.City.transform.position;
					anom.Lat = b.GetLatitude(anom.WorldLocation);
					anom.Lon = b.GetLongitude(anom.WorldLocation);
				}

				//currentBodyAnomalies = anomalies[b.name].Values.ToList();
			}
			//else
			//	currentBodyAnomalies = new List<DMAnomalyObject>();
		}

		//private void pqsBuild(CelestialBody b)
		//{
		//	PQSCity[] Cities = FindObjectsOfType(typeof(PQSCity)) as PQSCity[];
		//	foreach (PQSCity anomalyObject in Cities)
		//	{
		//		if (!anomalies.ContainsKey(anomalyObject.transform.parent.name))
		//		{
		//			Dictionary<string, DMAnomalyObject> anomDict = new Dictionary<string, DMAnomalyObject>();
		//			DMAnomalyObject obj = new DMAnomalyObject(anomalyObject);
		//			anomDict.Add(anomalyObject.name, obj);
		//			anomalies.Add(anomalyObject.transform.parent.name, anomDict);

		//		}
		//		else if (!anomalies[anomalyObject.transform.parent.name].ContainsKey(anomalyObject.name))
		//		{
		//			DMAnomalyObject obj = new DMAnomalyObject(anomalyObject);
		//			anomalies[anomalyObject.transform.parent.name].Add(anomalyObject.name, obj);
		//		}
		//	}

		//	currentBodyAnomalies.Clear();

		//	if (anomalies.ContainsKey(b.name))
		//		currentBodyAnomalies = anomalies[b.name].Values.ToList();
		//	else
		//		currentBodyAnomalies = new List<DMAnomalyObject>();
		//}

		public static void updateAnomaly(Vessel v, DMAnomalyObject a)
		{
			Vector3d vPos = v.transform.position;
			//a.WorldLocation = a.City.transform.position;
			a.WorldLocation = v.mainBody.GetWorldSurfacePosition(a.Lat, a.Lon, a.Alt);

			a.Lat = v.mainBody.GetLatitude(a.WorldLocation);
			a.Lon = v.mainBody.GetLongitude(a.WorldLocation);

			//Calculate vectors from CBody position to object positions
			Vector3d anomBody = v.mainBody.position - a.WorldLocation;
			Vector3d vesselBody = v.mainBody.position - v.transform.position;

			//Project vessel vector onto anomaly vector
			Vector3d projectedV = Vector3d.Project(vesselBody, anomBody);

			//Calculate height above or below anomaly by drawing a line between the projected vector and the anomaly vector
			//Take the magnitude of that line, which equals the height
			a.VHeight = (anomBody - projectedV).magnitude;
			a.VDistance = (a.WorldLocation - vPos).magnitude;
			a.VHorizontal = Math.Sqrt((a.VDistance * a.VDistance) - (a.VHeight * a.VHeight));
		}

		public static void bearing(Vessel v, DMAnomalyObject a)
		{
			a.Bearing = DMUtils.bearing(v.latitude, v.longitude, a.Lat, a.Lon);
		}
	}
}
