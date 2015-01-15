/* 
 * Written by Gribbleshnibit8
 * 
 * Released under the 
 * GNU GENERAL PUBLIC LICENSE Version 3
*/
using System;
using System.Text;
using UnityEngine;

// ReSharper disable InconsistentNaming
namespace RTGGenerator
{
    [KSPModule("RTG Power Supply")]
    public class ModuleGeneratorRTG : PartModule
    {
	    readonly ResourceLoader rL = new ResourceLoader();

		#region Persistent Part Values

		[KSPField(isPersistant = true)]
		public RtgFuelDefinition resource = null;

		#endregion

		#region Module Input Values

		[KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true)]
		public string fuel = "Blutonium-238";

		[KSPField(isPersistant = true)]
		public float fuelMass = 7.8F;

		[KSPField(guiName = "Efficiency", isPersistant = true, guiActiveEditor = true)]
		public float efficiency = 0.05F;

		#endregion

		#region Editor Info Output

        [KSPField(guiName = "Power Output", isPersistant = false, guiActiveEditor = true, guiActive = true, guiUnits = " Ec/s")]
		private double PowerOutput;

		[KSPField(guiName = "Half-life", isPersistant = false, guiActiveEditor = true, guiActive = true, guiUnits = " years")]
		public double Halflife = 25.43;

		[KSPField(guiName = "Time to 3/4 output", isPersistant = false, guiActiveEditor = true, guiActive = true, guiUnits = " years")]
		public double LifePoint1 = 0;

		[KSPField(guiName = "Time to 1/4 output", isPersistant = false, guiActiveEditor = true, guiActive = true, guiUnits = " years")]
		public double LifePoint2 = 0;

		#endregion

		#region Editor Events

		[KSPEvent(guiName = "Next Fuel", guiActiveEditor = true)]
		public void SelectNextFuel()
		{
			Debug.LogWarning("[RRTG] Changing resource: " + resource);

			rL.RtgFuelDefinitions.Dump();

			//resource = rL.RtgFuelDefinitions.GetNextDefinition(resource);
			//UpdateResource();
			//UpdateModule();
		}

		#endregion

		#region Flight Scene Outputs

		[KSPField(guiName = "Fuel Remaining", isPersistant = false, guiActiveEditor = false, guiActive = true, guiUnits = "%")]
		public float FuelRemaining = 100;

	#endregion

		#region Unity Functions

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
			Debug.LogWarning("[RRTG] Fixed Update");
			UpdateModule();
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			part.RequestResource("ElectricCharge", -GetPowerOutput() * TimeWarp.deltaTime);
		}

		public override string GetInfo()
		{
			UpdateModule();
			base.GetInfo();
			var sb = new StringBuilder();
			sb.Append("Output power decays over time.");
			sb.Append("\n\n");

			// Output fuel info
			sb.Append("Fuel: ");
			sb.Append(fuel);
			sb.Append("\n");
			sb.Append("Half-life: ");
			sb.Append(GetDayOrYears(Halflife));

			// Output life-point info
			sb.Append("Time to 3/4 output: ");
			sb.Append(GetDayOrYears(LifePoint1));
			sb.Append("Time to 1/4 output: ");
			sb.Append(GetDayOrYears(LifePoint2));

			sb.Append("\n\n");
			sb.Append("Output: ");
			sb.Append(PowerOutput);
			sb.Append(" Ec/s");

			return sb.ToString();
		}

		/// <summary>
		/// Called when the game is loading the part information. It comes from: the part's cfg file,
        /// the .craft file, the persistence file, or the quicksave file.
		/// </summary>
		/// <param name="node"></param>
		public override void OnLoad(ConfigNode node)
		{
			if (node.HasValue("fuel"))
			{
				Debug.LogWarning("[RRTG] : On Load : Loaded the config node");
				fuel = node.GetValue("fuel");
				fuelMass = (float)Convert.ToDouble(node.GetValue("fuelMass"));
				efficiency = (float) Convert.ToDouble(node.GetValue("efficiency"));
				Debug.LogWarning("[RRTG] : Fuel:" + fuel + " : fuelMass:" + fuelMass + " : efficiency:" + efficiency);
			}


			resource = LoadResource();
			Debug.LogWarning("[RRTG] : OnLoad : Loaded resource : " + resource);
			UpdateModule();
		}

	#endregion

	#region Module Functions

		/// <summary>
		/// Loads the resource data from the resource list. Returns a default resource if none found.
		/// </summary>
		/// <returns></returns>
		private RtgFuelDefinition LoadResource()
		{
			RtgFuelDefinition newResource;
			Debug.LogWarning("[RRTG] Looking to load : " + fuel);
			if (rL.RtgFuelDefinitions.Contains(fuel))
			{
				newResource = rL.RtgFuelDefinitions.RetreiveByName(fuel);
				Debug.LogWarning("[RRTG] Found resource " + newResource);
			}
			else 
				newResource = new RtgFuelDefinition();

			return newResource;
		}

	    /// <summary>
	    /// Updates the displayed values
	    /// </summary>
		private void UpdateModule()
	    {
		    Halflife = resource.HalfLife;
			PowerOutput = GetPowerOutput();
			FuelRemaining = (float)Math.Round(CalculateDecay(), 0);
			LifePoint1 = CalculateLifepoint(0.75);
			LifePoint2 = CalculateLifepoint(0.25);
		}

	    private void UpdateResource()
	    {
		    fuel = resource.Name;
		    Halflife = (float)resource.HalfLife;
	    }

		/// <summary>
		/// Calculates the fuel decay based on the mission time.
		/// In the editor it will return a decay of 0
		/// </summary>
		/// <returns></returns>
		private double CalculateDecay()
		{
			double missionTime = !HighLogic.LoadedSceneIsEditor ? 0 : vessel.missionTime;

			// mission time / seconds in minute / minutes in hour / hours in day / days in year
			var elapsedYears = missionTime / 60 / 60 / 6 / 426.08;

			return 100 * Math.Pow(2, (-elapsedYears / resource.HalfLife));
		}

		/// <summary>
		/// Calculates the output in Ec/s based on the part's settings
		/// </summary>
		/// <returns></returns>
		private double GetPowerOutput()
		{
			// energy output = Decay Rate * power from fuel * efficiency of thermocouple / some value that makes it right?
			return Math.Round(((CalculateDecay() * (resource.EnergyDensity * fuelMass) * efficiency) / 28), 2);
		}

		/// <summary>
		/// Calculates the time until the output will be the lifePoint percentage
		/// </summary>
		/// <param name="lifePoint">A value from 0.0 to 1.0 as a percentage of power output</param>
		/// <returns></returns>
		private double CalculateLifepoint(double lifePoint)
		{
			return Math.Round(Halflife - (Halflife * lifePoint), 3);
		}

		/// <summary>
		/// Takes a double and returns a string with the suffix in number of days or years in Kerbin time
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
	    private string GetDayOrYears(double time)
		{
			time = Math.Round(time, 3);
			var sb = new StringBuilder();
			if (time < 1)
			{
				// Get the half-life in terms of days instead
				var temp = Math.Round(time * 426);
				sb.Append(temp);
				sb.Append(" days");
			}
			else
			{
				sb.Append(time);
				sb.Append(" years");
			}
			sb.Append("\n");
		    return sb.ToString();
		}

	    private void EfficiencyStatusMessage()
	    {
			if (CalculateDecay() < 6.25)
				ScreenMessages.PostScreenMessage("The RTG is operating below one sixteenth efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
			else if (CalculateDecay() < 12.5)
				ScreenMessages.PostScreenMessage("The RTG is operating at one eighth efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
			else if (CalculateDecay() < 25)
				ScreenMessages.PostScreenMessage("The RTG is operating at one quarter efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
			else if (CalculateDecay() < 50)
				ScreenMessages.PostScreenMessage("The RTG is operating at one half efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
			else if (CalculateDecay() < 75)
				ScreenMessages.PostScreenMessage("The RTG is operating at three quarter efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
	    }

		#endregion

		// NOTES

		// To calculate the volume of fuel any of the RTGs can store use
		// Volume = Density / Mass


    }
}
