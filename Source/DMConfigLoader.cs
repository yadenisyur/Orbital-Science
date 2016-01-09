﻿#region license
/* DMagic Orbital Science - DMConfigLoader
 * Monobehaviour to load science contract paramaters at startup
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
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace DMagic
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class DMConfigLoader: MonoBehaviour
	{
		private string[] WhiteList = new string[1] { "ScienceAlert" };
		private const string iconURL = "DMagicOrbitalScience/Icons/Waypoints/";

		private void Start()
		{
			initializeUtils();
			findAssemblies(WhiteList);
			configLoad();
			configLoad2();
			hackWaypointIcons();
			GameEvents.OnPQSCityLoaded.Add(scanBodyAnomalies);
			GameEvents.OnPQSCityUnloaded.Add(removePQS);
			DontDestroyOnLoad(this);
		}

		private void scanBodyAnomalies(CelestialBody b, string s)
		{
			DMUtils.DebugLog("PQS City [{0}] loaded for {1}", s, b.theName);
		}

		private void removePQS(CelestialBody b, string s)
		{
			DMUtils.DebugLog("PQS City [{0}] unloaded for {1}", s, b.theName);
		}

		private void configLoad2()
		{
			ConfigNode DMcontractDefs = GameDatabase.Instance.GetConfigNode("DMagicOrbitalScience/Resources/DMContracts/DMContracts");

			if (DMcontractDefs == null)
				return;

			foreach (ConfigNode node in DMcontractDefs.GetNodes("DM_CONTRACT_EXPERIMENT"))
			{
				if (node == null)
					continue;
				DMScienceContainer DMscience = null;
				ScienceExperiment exp = null;

				//Some apparently not impossible errors can cause duplicate experiments to be added to the R&D science experiment dictionary
				try
				{
					exp = ResearchAndDevelopment.GetExperiment(node.GetValue("experimentID"));
				}
				catch (Exception e)
				{
					Debug.LogError("[DM] Whoops. Something really wrong happened here; stopping this contract experiment from loading..." + e);
					continue;
				}
				if (exp != null)
				{
					string name = node.parse("name", "null");
					if (name == "null")
						continue;

					int sitMask = node.parse("sitMask", (int)1000);
					if (sitMask == 1000)
						continue;

					int bioMask = node.parse("bioMask", (int)1000);
					if (bioMask == 1000)
						continue;

					int type = node.parse("type", (int)1000);
					if (type == 1000)
						continue;

					float transmit = node.parse("xmitDataScalar", (float)1000);
					if (transmit >= 1000)
						continue;

					string part = node.parse("part", "None");

					string agent = node.parse("agent", "Any");

					if (DMUtils.whiteListed)
					{
						exp.situationMask = (uint)sitMask;
						exp.biomeMask = (uint)bioMask;
					}

					DMscience = new DMScienceContainer(exp, sitMask, bioMask, (DMScienceType)type, part, agent, transmit);

					foreach (var sciType in Enum.GetValues(typeof(DMScienceType)))
					{
						string typeString = ((DMScienceType)sciType).ToString();
						if (!string.IsNullOrEmpty(typeString))
						{
							if (DMUtils.availableScience.ContainsKey(typeString))
							{
								if ((DMScienceType)sciType == DMScienceType.All)
								{
									if (!DMUtils.availableScience[typeString].ContainsKey(name))
										DMUtils.availableScience[typeString].Add(name, DMscience);
								}
								else if (((DMScienceType)type & (DMScienceType)sciType) == (DMScienceType)sciType)
								{
									if (!DMUtils.availableScience[typeString].ContainsKey(name))
										DMUtils.availableScience[typeString].Add(name, DMscience);
								}
							}
						}
					}
				}
			}

			DMUtils.Logging("Successfully Added {0} New Experiments To Contract List", DMUtils.availableScience["All"].Count);

			ConfigNode DMAnomalyNode = DMcontractDefs.GetNode("DMAnomaly");
			ConfigNode DMAsteroidNode = DMcontractDefs.GetNode("DMAsteroid");
			ConfigNode DMMagNode = DMcontractDefs.GetNode("DMMag");
			ConfigNode DMReconNode = DMcontractDefs.GetNode("DMRecon");
			ConfigNode DMSurveyNode = DMcontractDefs.GetNode("DMSurvey");

			if (DMAnomalyNode != null)
			{
				DMContractDefs.DMAnomaly.maxOffers = DMAnomalyNode.parse("maxOffers", (int)2);
				DMContractDefs.DMAnomaly.maxActive = DMAnomalyNode.parse("maxActive", (int)3);

				DMContractDefs.DMAnomaly.TrivialReconLevelRequirement = DMAnomalyNode.parse("Trivial_Recon_Level_Requirement", (int)0);
				DMContractDefs.DMAnomaly.SignificantReconLevelRequirement = DMAnomalyNode.parse("Significant_Recon_Level_Requirement", (int)1);
				DMContractDefs.DMAnomaly.ExceptionalReconLevelRequirement = DMAnomalyNode.parse("Exceptional_Recon_Level_Requirement", (int)1);

				DMContractDefs.DMAnomaly.backStory = DMAnomalyNode.parse("Backstory", '|', new List<string>(1) { "Something, Something, Something..." });

				DMContractDefs.DMAnomaly.backStory = DMUtils.formatFixStringList(DMContractDefs.DMAnomaly.backStory);

				ConfigNode AnomalyExpireNode = DMAnomalyNode.GetNode("Expire");
				ConfigNode AnomalyFundsNode = DMAnomalyNode.GetNode("Funds");
				ConfigNode AnomalySciNode = DMAnomalyNode.GetNode("Science");
				ConfigNode AnomalyRepNode = DMAnomalyNode.GetNode("Reputation");

				if (AnomalyExpireNode != null)
				{
					DMContractDefs.DMAnomaly.Expire.MinimumExpireDays = AnomalyExpireNode.parse("MinimumExpireDays", (int)5);
					DMContractDefs.DMAnomaly.Expire.MaximumExpireDays = AnomalyExpireNode.parse("MaximumExpireDays", (int)15);
					DMContractDefs.DMAnomaly.Expire.DeadlineYears = AnomalyExpireNode.parse("DeadlineYears", (float)1.5);
				}

				if (AnomalyFundsNode != null)
				{
					DMContractDefs.DMAnomaly.Funds.BaseAdvance = AnomalyFundsNode.parse("BaseAdvance", (float)20000);
					DMContractDefs.DMAnomaly.Funds.BaseReward = AnomalyFundsNode.parse("BaseReward", (float)24000);
					DMContractDefs.DMAnomaly.Funds.BaseFailure = AnomalyFundsNode.parse("BaseFailure", (float)24000);
					DMContractDefs.DMAnomaly.Funds.ParamReward = AnomalyFundsNode.parse("ParamReward", (float)12000);
					DMContractDefs.DMAnomaly.Funds.ParamFailure = AnomalyFundsNode.parse("ParamFailure", (float)0);
				}

				if (AnomalySciNode != null)
				{
					DMContractDefs.DMAnomaly.Science.BaseReward = AnomalySciNode.parse("BaseReward", (float)0);
					DMContractDefs.DMAnomaly.Science.ParamReward = AnomalySciNode.parse("ParamReward", (float)6);
					DMContractDefs.DMAnomaly.Science.SecondaryReward = AnomalySciNode.parse("SecondaryReward", (float)0.25);
				}

				if (AnomalyRepNode != null)
				{
					DMContractDefs.DMAnomaly.Reputation.BaseReward = AnomalyRepNode.parse("BaseReward", (float)8);
					DMContractDefs.DMAnomaly.Reputation.BaseFailure = AnomalyRepNode.parse("BaseFailure", (float)9);
					DMContractDefs.DMAnomaly.Reputation.ParamReward = AnomalyRepNode.parse("ParamReward", (float)0);
					DMContractDefs.DMAnomaly.Reputation.ParamFailure = AnomalyRepNode.parse("ParamFailure", (float)0);
				}
			}

			if (DMAsteroidNode != null)
			{
				DMContractDefs.DMAsteroid.maxOffers = DMAsteroidNode.parse("maxOffers", (int)2);
				DMContractDefs.DMAsteroid.maxActive = DMAsteroidNode.parse("maxActive", (int)3);

				DMContractDefs.DMAsteroid.trivialScienceRequests = DMAsteroidNode.parse("Max_Trivial_Science_Requests", (int)3);
				DMContractDefs.DMAsteroid.significantScienceRequests = DMAsteroidNode.parse("Max_Significant_Science_Requests", (int)4);
				DMContractDefs.DMAsteroid.exceptionalScienceRequests = DMAsteroidNode.parse("Max_Exceptional_Science_Requests", (int)6);

				DMContractDefs.DMAsteroid.backStory = DMAsteroidNode.parse("Backstory", '|', new List<string>(1) { "Something, Something, Something..." });

				DMContractDefs.DMAsteroid.backStory = DMUtils.formatFixStringList(DMContractDefs.DMAsteroid.backStory);

				ConfigNode AsteroidExpireNode = DMAsteroidNode.GetNode("Expire");
				ConfigNode AsteroidFundsNode = DMAsteroidNode.GetNode("Funds");
				ConfigNode AsteroidSciNode = DMAsteroidNode.GetNode("Science");
				ConfigNode AsteroidRepNode = DMAsteroidNode.GetNode("Reputation");

				if (AsteroidExpireNode != null)
				{
					DMContractDefs.DMAsteroid.Expire.MinimumExpireDays = AsteroidExpireNode.parse("MinimumExpireDays", (int)5);
					DMContractDefs.DMAsteroid.Expire.MaximumExpireDays = AsteroidExpireNode.parse("MaximumExpireDays", (int)15);
					DMContractDefs.DMAsteroid.Expire.DeadlineYears = AsteroidExpireNode.parse("DeadlineYears", (float)3.8f);
				}

				if (AsteroidFundsNode != null)
				{
					DMContractDefs.DMAsteroid.Funds.BaseAdvance = AsteroidFundsNode.parse("BaseAdvance", (float)8000);
					DMContractDefs.DMAsteroid.Funds.BaseReward = AsteroidFundsNode.parse("BaseReward", (float)9500);
					DMContractDefs.DMAsteroid.Funds.BaseFailure = AsteroidFundsNode.parse("BaseFailure", (float)7000);
					DMContractDefs.DMAsteroid.Funds.ParamReward = AsteroidFundsNode.parse("ParamReward", (float)5000);
					DMContractDefs.DMAsteroid.Funds.ParamFailure = AsteroidFundsNode.parse("ParamFailure", (float)0);
				}

				if (AsteroidSciNode != null)
				{
					DMContractDefs.DMAsteroid.Science.BaseReward = AsteroidSciNode.parse("BaseReward", (float)0);
					DMContractDefs.DMAsteroid.Science.ParamReward = AsteroidSciNode.parse("ParamReward", (float)0.25);
				}

				if (AsteroidRepNode != null)
				{
					DMContractDefs.DMAsteroid.Reputation.BaseReward = AsteroidRepNode.parse("BaseReward", (float)1.5);
					DMContractDefs.DMAsteroid.Reputation.BaseFailure = AsteroidRepNode.parse("BaseFailure", (float)1.5);
					DMContractDefs.DMAsteroid.Reputation.ParamReward = AsteroidRepNode.parse("ParamReward", (float)0);
					DMContractDefs.DMAsteroid.Reputation.ParamFailure = AsteroidRepNode.parse("ParamFailure", (float)0);
				}
			}

			if (DMMagNode != null)
			{
				DMContractDefs.DMMagnetic.maxOffers = DMMagNode.parse("maxOffers", (int)2);
				DMContractDefs.DMMagnetic.maxActive = DMMagNode.parse("maxActive", (int)4);

				DMContractDefs.DMMagnetic.trivialEccentricityMultiplier = DMMagNode.parse("Trivial_Eccentricity_Modifier", (double)0.2);
				DMContractDefs.DMMagnetic.significantEccentricityMultiplier = DMMagNode.parse("Significant_Eccentricity_Modifier", (double)0.35);
				DMContractDefs.DMMagnetic.exceptionalEccentricityMultiplier = DMMagNode.parse("Exceptional_Eccentricity_Modifier", (double)0.5);

				DMContractDefs.DMMagnetic.trivialInclinationMultiplier = DMMagNode.parse("Trivial_Inclination_Modifier", (double)20);
				DMContractDefs.DMMagnetic.significantInclinationMultiplier = DMMagNode.parse("Significant_Inclination_Modifier", (double)40);
				DMContractDefs.DMMagnetic.exceptionalInclinationMultiplier = DMMagNode.parse("Exceptional_Inclination_Modifier", (double)60);

				DMContractDefs.DMMagnetic.useVesselWaypoints = DMMagNode.parse("Use_Vessel_Waypoints", (bool)true);

				DMContractDefs.DMMagnetic.magParts = DMMagNode.parse("Magnetometer_Parts", ',', new List<string>(2) { "dmmagBoom", "dmUSMagBoom" });
				DMContractDefs.DMMagnetic.rpwsParts = DMMagNode.parse("RPWS_Parts", ',', new List<string>(2) { "rpwsAnt", "USRPWS" });

				DMContractDefs.DMMagnetic.backStory = DMMagNode.parse("Backstory", '|', new List<string>(1) { "Something, Something, Something..." });

				DMContractDefs.DMMagnetic.backStory = DMUtils.formatFixStringList(DMContractDefs.DMMagnetic.backStory);

				ConfigNode MagExpireNode = DMMagNode.GetNode("Expire");
				ConfigNode MagFundsNode = DMMagNode.GetNode("Funds");
				ConfigNode MagSciNode = DMMagNode.GetNode("Science");
				ConfigNode MagRepNode = DMMagNode.GetNode("Reputation");

				if (MagExpireNode != null)
				{
					DMContractDefs.DMMagnetic.Expire.MinimumExpireDays = MagExpireNode.parse("MinimumExpireDays", (int)5);
					DMContractDefs.DMMagnetic.Expire.MaximumExpireDays = MagExpireNode.parse("MaximumExpireDays", (int)15);
					DMContractDefs.DMMagnetic.Expire.DeadlineModifier = MagExpireNode.parse("DeadlineModifier", (float)3.7);
				}

				if (MagFundsNode != null)
				{
					DMContractDefs.DMMagnetic.Funds.BaseAdvance = MagFundsNode.parse("BaseAdvance", (float)35000);
					DMContractDefs.DMMagnetic.Funds.BaseReward = MagFundsNode.parse("BaseReward", (float)40000);
					DMContractDefs.DMMagnetic.Funds.BaseFailure = MagFundsNode.parse("BaseFailure", (float)28000);
					DMContractDefs.DMMagnetic.Funds.ParamReward = MagFundsNode.parse("ParamReward", (float)5000);
					DMContractDefs.DMMagnetic.Funds.ParamFailure = MagFundsNode.parse("ParamFailure", (float)0);
				}

				if (MagSciNode != null)
				{
					DMContractDefs.DMMagnetic.Science.BaseReward = MagSciNode.parse("BaseReward", (float)15);
					DMContractDefs.DMMagnetic.Science.ParamReward = MagSciNode.parse("ParamReward", (float)2);
				}

				if (MagRepNode != null)
				{
					DMContractDefs.DMMagnetic.Reputation.BaseReward = MagRepNode.parse("BaseReward", (float)8);
					DMContractDefs.DMMagnetic.Reputation.BaseFailure = MagRepNode.parse("BaseFailure", (float)7);
					DMContractDefs.DMMagnetic.Reputation.ParamReward = MagRepNode.parse("ParamReward", (float)0);
					DMContractDefs.DMMagnetic.Reputation.ParamFailure = MagRepNode.parse("ParamFailure", (float)0);
				}
			}

			if (DMReconNode != null)
			{
				DMContractDefs.DMRecon.maxOffers = DMReconNode.parse("maxOffers", (int)2);
				DMContractDefs.DMRecon.maxActive = DMReconNode.parse("maxActive", (int)4);

				DMContractDefs.DMRecon.useVesselWaypoints = DMReconNode.parse("Use_Vessel_Waypoints", (bool)true);

				DMContractDefs.DMRecon.reconTrivialParts = DMReconNode.parse("Trivial_Parts", ',', new List<string>(1) { "dmReconSmall" });
				DMContractDefs.DMRecon.reconSignificantParts = DMReconNode.parse("Significant_Parts", ',', new List<string>(1) { "dmSIGINT" });
				DMContractDefs.DMRecon.reconExceptionalParts = DMReconNode.parse("Exceptional_Parts", ',', new List<string>(1) { "dmReconLarge" });

				DMContractDefs.DMRecon.backStory = DMReconNode.parse("Backstory", '|', new List<string>(1) { "Something, Something, Something..." });

				DMContractDefs.DMRecon.backStory = DMUtils.formatFixStringList(DMContractDefs.DMRecon.backStory);

				ConfigNode ReconExpireNode = DMReconNode.GetNode("Expire");
				ConfigNode ReconFundsNode = DMReconNode.GetNode("Funds");
				ConfigNode ReconSciNode = DMReconNode.GetNode("Science");
				ConfigNode ReconRepNode = DMReconNode.GetNode("Reputation");

				if (ReconExpireNode != null)
				{
					DMContractDefs.DMRecon.Expire.MinimumExpireDays = ReconExpireNode.parse("MinimumExpireDays", (int)5);
					DMContractDefs.DMRecon.Expire.MaximumExpireDays = ReconExpireNode.parse("MaximumExpireDays", (int)15);
					DMContractDefs.DMRecon.Expire.DeadlineModifier = ReconExpireNode.parse("DeadlineModifier", (float)3.7);
				}

				if (ReconFundsNode != null)
				{
					DMContractDefs.DMRecon.Funds.BaseAdvance = ReconFundsNode.parse("BaseAdvance", (float)35000);
					DMContractDefs.DMRecon.Funds.BaseReward = ReconFundsNode.parse("BaseReward", (float)40000);
					DMContractDefs.DMRecon.Funds.BaseFailure = ReconFundsNode.parse("BaseFailure", (float)28000);
					DMContractDefs.DMRecon.Funds.ParamReward = ReconFundsNode.parse("ParamReward", (float)5000);
					DMContractDefs.DMRecon.Funds.ParamFailure = ReconFundsNode.parse("ParamFailure", (float)0);
				}

				if (ReconSciNode != null)
				{
					DMContractDefs.DMRecon.Science.BaseReward = ReconSciNode.parse("BaseReward", (float)20);
					DMContractDefs.DMRecon.Science.ParamReward = ReconSciNode.parse("ParamReward", (float)2);
				}

				if (ReconRepNode != null)
				{
					DMContractDefs.DMRecon.Reputation.BaseReward = ReconRepNode.parse("BaseReward", (float)10);
					DMContractDefs.DMRecon.Reputation.BaseFailure = ReconRepNode.parse("BaseFailure", (float)12);
					DMContractDefs.DMRecon.Reputation.ParamReward = ReconRepNode.parse("ParamReward", (float)0);
					DMContractDefs.DMRecon.Reputation.ParamFailure = ReconRepNode.parse("ParamFailure", (float)0);
				}
			}

			if (DMSurveyNode != null)
			{
				DMContractDefs.DMSurvey.maxOffers = DMSurveyNode.parse("maxOffers", (int)2);
				DMContractDefs.DMSurvey.maxActive = DMSurveyNode.parse("maxActive", (int)4);

				DMContractDefs.DMSurvey.trivialScienceRequests = DMSurveyNode.parse("Max_Trivial_Science_Requests", (int)4);
				DMContractDefs.DMSurvey.significantScienceRequests = DMSurveyNode.parse("Max_Significant_Science_Requests", (int)6);
				DMContractDefs.DMSurvey.exceptionalScienceRequests = DMSurveyNode.parse("Max_Exceptional_Science_Requests", (int)8);

				DMContractDefs.DMSurvey.backStory = DMSurveyNode.parse("Backstory", '|', new List<string>(1) { "Something, Something, Something..." });

				DMContractDefs.DMSurvey.backStory = DMUtils.formatFixStringList(DMContractDefs.DMSurvey.backStory);

				ConfigNode SurveyExpireNode = DMSurveyNode.GetNode("Expire");
				ConfigNode SurveyFundsNode = DMSurveyNode.GetNode("Funds");
				ConfigNode SurveySciNode = DMSurveyNode.GetNode("Science");
				ConfigNode SurveyRepNode = DMSurveyNode.GetNode("Reputation");

				if (SurveyExpireNode != null)
				{
					DMContractDefs.DMSurvey.Expire.MinimumExpireDays = SurveyExpireNode.parse("MinimumExpireDays", (int)5);
					DMContractDefs.DMSurvey.Expire.MaximumExpireDays = SurveyExpireNode.parse("MaximumExpireDays", (int)15);
					DMContractDefs.DMSurvey.Expire.DeadlineYears = SurveyExpireNode.parse("DeadlineYears", (float)1.7);
				}

				if (SurveyFundsNode != null)
				{
					DMContractDefs.DMSurvey.Funds.BaseAdvance = SurveyFundsNode.parse("BaseAdvance", (float)8500);
					DMContractDefs.DMSurvey.Funds.BaseReward = SurveyFundsNode.parse("BaseReward", (float)10500);
					DMContractDefs.DMSurvey.Funds.BaseFailure = SurveyFundsNode.parse("BaseFailure", (float)7500);
					DMContractDefs.DMSurvey.Funds.ParamReward = SurveyFundsNode.parse("ParamReward", (float)3500);
					DMContractDefs.DMSurvey.Funds.ParamFailure = SurveyFundsNode.parse("ParamFailure", (float)0);
				}

				if (SurveySciNode != null)
				{
					DMContractDefs.DMSurvey.Science.BaseReward = SurveySciNode.parse("BaseReward", (float)0);
					DMContractDefs.DMSurvey.Science.ParamReward = SurveySciNode.parse("ParamReward", (float)0.2);
				}

				if (SurveyRepNode != null)
				{
					DMContractDefs.DMSurvey.Reputation.BaseReward = SurveyRepNode.parse("BaseReward", (float)1.9);
					DMContractDefs.DMSurvey.Reputation.BaseFailure = SurveyRepNode.parse("BaseFailure", (float)1.5);
					DMContractDefs.DMSurvey.Reputation.ParamReward = SurveyRepNode.parse("ParamReward", (float)0);
					DMContractDefs.DMSurvey.Reputation.ParamFailure = SurveyRepNode.parse("ParamFailure", (float)0);
				}
			}
		}

		private void configLoad()
		{
			//Load in global multipliers
			foreach (ConfigNode setNode in GameDatabase.Instance.GetConfigNodes("DM_CONTRACT_SETTINGS"))
			{
				if (setNode != null)
				{
					if (!float.TryParse(setNode.GetValue("Seismic_Near_Pod_Min_Distance"), out DMSeismicHandler.nearPodMinDistance))
						DMSeismicHandler.nearPodMinDistance = 10;
					if (!float.TryParse(setNode.GetValue("Seismic_Near_Pod_Max_Distance"), out DMSeismicHandler.nearPodMaxDistance))
						DMSeismicHandler.nearPodMaxDistance = 2500;
					if (!float.TryParse(setNode.GetValue("Seismic_Near_Pod_Threshold"), out DMSeismicHandler.nearPodThreshold))
						DMSeismicHandler.nearPodThreshold = 500;
					if (!float.TryParse(setNode.GetValue("Seismic_Far_Pod_Min_Distance"), out DMSeismicHandler.farPodMinDistance))
						DMSeismicHandler.farPodMinDistance = 2500;
					if (!float.TryParse(setNode.GetValue("Seismic_Far_Pod_Max_Distance"), out DMSeismicHandler.farPodMaxDistance))
						DMSeismicHandler.farPodMaxDistance = 15000;
					if (!float.TryParse(setNode.GetValue("Seismic_Far_Pod_Threshold"), out DMSeismicHandler.farPodThreshold))
						DMSeismicHandler.farPodThreshold = 4000;
					if (!float.TryParse(setNode.GetValue("Seismic_Pod_Min_Angle"), out DMSeismicHandler.podMinAngle))
						DMSeismicHandler.podMinAngle = 20;
					if (!float.TryParse(setNode.GetValue("Seismic_Pod_Angle_Threshold"), out DMSeismicHandler.podAngleThreshold))
						DMSeismicHandler.podAngleThreshold = 90;
					break;
				}
				else
					DMUtils.Logging("Broken Config.....");
			}
		}

		private void initializeUtils()
		{
			DMUtils.OnAnomalyScience = new EventData<CelestialBody, String, String>("OnAnomalyScience");
			DMUtils.OnAsteroidScience = new EventData<String, String>("OnAsteroidScience");

			var infoAtt = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
			if (infoAtt != null)
			{
				DMUtils.version = infoAtt.InformationalVersion;
				DMUtils.Logging("DMagic Orbital Science Version: [{0}] Loaded", DMUtils.version);
			}
			else
			{
				DMUtils.version = "";
				DMUtils.Logging("Something Went Wrong Here... Version Not Set, Contracts Might Reset");
			}

			DMUtils.rand = new System.Random();

			DMUtils.availableScience = new Dictionary<string, Dictionary<string, DMScienceContainer>>();

			foreach (var sciType in Enum.GetValues(typeof(DMScienceType)))
			{
				string type = ((DMScienceType)sciType).ToString();
				if (!string.IsNullOrEmpty(type))
				{
					if (!DMUtils.availableScience.ContainsKey(type))
						DMUtils.availableScience[type] = new Dictionary<string, DMScienceContainer>();
					else
						DMUtils.Logging("");
				}
				else
					DMUtils.Logging("");
			}
		}

		private void findAssemblies(string[] assemblies)
		{
			foreach (string name in assemblies)
			{
				AssemblyLoader.LoadedAssembly assembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == name);
				if (assembly != null)
				{
					DMUtils.Logging("Assembly: {0} Found; Reactivating Experiment Properties", assembly.assembly.GetName().Name);
					DMUtils.whiteListed = true;
					break;
				}
			}
		}

		private void hackWaypointIcons()
		{
			foreach (GameDatabase.TextureInfo ti in GameDatabase.Instance.databaseTexture.Where(t => t.name.StartsWith(iconURL)))
			{
				if (ti != null)
				{
					string s = ti.name.Remove(0, iconURL.Length);
					ti.name = "Squad/Contracts/Icons/" + s;
					DMUtils.Logging("DMagic Icon [{0}] Inserted Into FinePrint Database", s);
				}
			}
		}

	}
}
