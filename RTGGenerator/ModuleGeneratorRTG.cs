/* 
 * Written by Gribbleshnibit8
 * 
 * Released under the 
 * GNU GENERAL PUBLIC LICENSE Version 3
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;

// fuel types are:   Strontium-90      Plutonium-238   Americium-241
//      Half-life:   28.8 years        87.7 years      432 years
// Energy Density:   0.46kw/kg         0.54kw/kg       0.135kw/kg
//       Watts/kg:   460W/kg           500W/kg         135W/kg
//    Cost/Weight:   very cheap        expensive       less expensive

// half-lives measured in Kerbin years, where a year converts as follows:
//          Hours       Days        Months      Years
// Kerbin   2556.50	    426.08	    66.23	    1.00
// Earth    2,556.48    106.52      3.50        0.29

namespace RTGGenerator
{
    [KSPModule("RTG Power Supply")]
    public class ModuleGeneratorRTG : PartModule
    {
        [KSPField]
        public float FuelMass = 5;

		[KSPField]
		public float EnergyDensity = 0.46F;
		private bool toggle = true;

    // information output
        [KSPField(guiName = "Power Output", isPersistant = false, guiActiveEditor = true, guiActive = true, guiUnits = " Ec/s")]
		private float PowerOutput = 0;

        [KSPField(guiName = "Half-life", isPersistant = false, guiActiveEditor = true, guiActive = true, guiUnits = " years")]
		public float Halflife = 8.352F;

        [KSPField(guiName = "Efficiency", isPersistant = false, guiActiveEditor = true)]
		public float ThermocoupleEfficiency = 0.05F;

		//[KSPField(guiName = "Output at 3/4", isPersistant = false, guiActiveEditor = true, guiActive = true, guiUnits = " years")]
		public double LifePoint1 = 0;

		//[KSPField(guiName = "Time > 1/2 Power", isPersistant = false, guiActiveEditor = true, guiActive = true, guiUnits = " years")]
		public double LifePoint2 = 0;

    // flight scene outputs
        [KSPField(guiName = "Fuel Remaining", isPersistant = false, guiActiveEditor = false, guiActive = true, guiUnits = "%")]
		public float FuelRemaining = 100;

    /* ==================================
     *  UNITY FUNCTIONS
     * ================================== */

		public override void OnFixedUpdate()
		{
			UpdateModule();
		}

		public override void OnUpdate()
		{
			part.RequestResource("ElectricCharge", -GetPowerOutput() * TimeWarp.deltaTime);
		}

		public override void OnAwake()
		{
			UpdateModule();
			if (CalculateDecay() < 6.25)
				ScreenMessages.PostScreenMessage("The RTG is operating below one sixteenth efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
			else if (CalculateDecay() < 12.5)
				ScreenMessages.PostScreenMessage("The RTG is operating at one eighth efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
			else if (CalculateDecay() < 25)
				ScreenMessages.PostScreenMessage("The RTG is operating at one quarter efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
			else if (CalculateDecay() < 50)
				ScreenMessages.PostScreenMessage("The RTG is operating at one half efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
		}

        // module info as displayed in VAB/SPH
		public override string GetInfo()
		{
			string s = "Output power decays over time.\n\n";
			s += "Half-life: " + Halflife + " years\n";
			return s;
		}

    /* ==================================
     *  MODUEL FUNCTIONS
     * ================================== */

        // recalculate the display values based on selected fuel type
		private void UpdateModule()
		{
			PowerOutput = (float)GetPowerOutput();
			FuelRemaining = (float)Math.Round(CalculateDecay(), 0);
			LifePoint1 = CalculateLifepoint(0.75);
			LifePoint2 = CalculateLifepoint(0.5);
		}

		private double CalculateDecay()
		{
			double missionTime;
			if (HighLogic.LoadedSceneIsEditor)
				missionTime = 0;
			else
				missionTime = vessel.missionTime;

			// mission time / seconds in minute / minutes in hour / hours in day / days in year
			double elapsedYears = missionTime / 60 / 60 / 6 / 426.08;

			return 100 * Math.Pow(2, (-elapsedYears / Halflife));
		}

		private double GetPowerOutput()
		{
			// energy output = Decay Rate * power from fuel * efficiency of thermocouple / some value that makes it right?
			return Math.Round( ( ( CalculateDecay() * ( EnergyDensity * FuelMass ) * ThermocoupleEfficiency ) / 15 ), 2 );
		}

		private double CalculateLifepoint(double lifePoint)
		{
			return Math.Round(Halflife - (Halflife * lifePoint), 3);
		}
    }
}
