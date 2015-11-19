﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	class DMSeismicHammer : DMBasicScienceModule, IDMSeismometer
	{
		[KSPField]
		public string animationName = "";

		private Dictionary<DMSeismicSensor, Vector2> nearbySensors = new Dictionary<DMSeismicSensor, Vector2>();
		private float experimentScore = 0.5f;

		private Animation Anim;

		public override void OnStart(PartModule.StartState state)
		{
			if (!string.IsNullOrEmpty(animationName))
				Anim = part.FindModelAnimators(animationName)[0];
			if (!string.IsNullOrEmpty(experimentID))
				exp = ResearchAndDevelopment.GetExperiment(experimentID);
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);
		}

		private void Update()
		{
			base.EventsCheck();
		}

		#region Animator

		//Controls the main, door-opening animation
		private void animator(float speed, float time, Animation a, string name)
		{
			if (a != null)
			{
				a[name].speed = speed;
				if (!a.IsPlaying(name))
				{
					a[name].normalizedTime = time;
					a.Blend(name, 1f);
				}
			}
		}


		#endregion

		#region IDMSeismometer

		public void addSeismometer(IDMSeismometer s, Vector2 v)
		{
			if (!nearbySensors.ContainsKey((DMSeismicSensor)s))
				nearbySensors.Add((DMSeismicSensor)s, v);
			else
				nearbySensors[(DMSeismicSensor)s] = v;

		}

		public void removeSeismometer(IDMSeismometer s)
		{
			if (nearbySensors.ContainsKey((DMSeismicSensor)s))
				nearbySensors.Remove((DMSeismicSensor)s);
		}

		public void updateScore()
		{
			experimentScore = 0.5f;
		}

		#endregion

		public int sensorsInRange
		{
			get { return nearbySensors.Count; }
		}

	}
}
