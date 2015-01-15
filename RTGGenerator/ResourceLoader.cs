using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

// ReSharper disable LoopCanBeConvertedToQuery

namespace RTGGenerator
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class ResourceLoader : MonoBehaviour
	{

		public RtgFuelList RtgFuelDefinitions = new RtgFuelList();

		void Awake()
		{
			Debug.Log("RRTG Main Menu [" + this.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.0000") + "]: Awake: " + this.name);

			foreach (var node in GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION"))
			{
				if (node.HasValue("halfLife"))
				{
					var newFuel = new RtgFuelDefinition(node);
					if (RtgFuelDefinitions.Contains(newFuel))
						Debug.LogWarning("[RRTG] Ignored duplicate fuel type " + node.GetValue("name"));
					else
						RtgFuelDefinitions.Add(newFuel);
				}
			}

			RtgFuelDefinitions.Dump();
		}
	}


	public class RtgFuelList : List<RtgFuelDefinition>
	{

		public bool Contains(string toFind)
		{
			foreach (var fuel in this)
			{
				Debug.LogWarning("[RRTG] : " + fuel.Name + " compared to " + toFind);
				if (fuel.Name.Equals(toFind))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns a RtgFuelDefinition from the name of a fuel resource
		/// </summary>
		/// <param name="toFind">The name of the fuel resource to find</param>
		/// <returns></returns>
		public RtgFuelDefinition RetreiveByName(string toFind)
		{
			foreach (RtgFuelDefinition fuel in this)
			{
				Debug.LogWarning("[RRTG] Retreive by name " + fuel.Name);
				if (fuel.Name == toFind) return fuel;
			}
				
			return null;
		}

		public void Dump()
		{
			int counter = 0;
			foreach (var fuel in this)
			{
				counter++;
				Debug.LogWarning("[RRTG] : Fuel list dump : " + counter + fuel);
			}
		}

	}


	public class RtgFuelDefinitionList : KeyedCollection<string, RtgFuelDefinition>, IConfigNode
	{
		protected override string GetKeyForItem(RtgFuelDefinition item)
		{
			return item.Name;
		}

		public void Load(ConfigNode node)
		{
			foreach (var fuelNode in node.GetNodes("RESOURCE_DEFINITION"))
			{
				Add(new RtgFuelDefinition(fuelNode));
			}
		}

		public void Save(ConfigNode node)
		{
			foreach (var fuel in this)
			{
				ConfigNode tankNode = new ConfigNode("TANK");
				fuel.Save(tankNode);
				node.AddNode(tankNode);
			}
		}

		/// <summary>
		/// Returns a RtgFuelDefinition from the name of a fuel resource
		/// </summary>
		/// <param name="toFind">The name of the fuel resource to find</param>
		/// <returns></returns>
		public RtgFuelDefinition FindByName(string toFind)
		{
			foreach (RtgFuelDefinition fuel in this)
				if (fuel.Name == toFind) return fuel;
			return null;
		}

		public RtgFuelDefinition GetNextDefinition(RtgFuelDefinition node)
		{
			Debug.LogWarning("[RRTG] Finding next resource definition");
			for (int index = 0; index < Count; index++)
			{
				var fuel = this[index];
				if (fuel == node && this[Count-1] != node)
				{
					Debug.LogWarning("[RRTG] Found next resource: " + this[index+1]);
					return this[index + 1];
				}
			}
			return this[0];
		}

		public RtgFuelDefinition GetPreviousDefinition(RtgFuelDefinition node)
		{
			for (int index = Count; index > 0; index--)
			{
				var fuel = this[index];
				if (fuel == node && this[0] != node)
					return this[index - 1];
			}
			return this[Count-1];
		}

		public void Dump()
		{
			int counter = 0;
			foreach (var fuel in this)
			{
				counter++;
				Debug.LogWarning("[RRTG] : Fuel list dump : " + counter + fuel);
			}
		}


	}


	// ReSharper disable InconsistentNaming
	public class RtgFuelDefinition : IConfigNode
	{

		#region Fields

		[Persistent]
		private string name;

		[Persistent]
		private string halfLife;

		[Persistent]
		private string energyDensity;

		[Persistent]
		private string density;

		[Persistent]
		private string cost;

		#endregion

		#region Constructors

		public RtgFuelDefinition()
		{
			name = "Blutonium-238";
			halfLife = 25.43.ToString();
			energyDensity = 0.54.ToString();
			density = 0.0.ToString();
			cost = 0.0.ToString();
		}

		public RtgFuelDefinition(ConfigNode node)
		{
			Load(node);
		}

		#endregion

		#region Accessors

		public string Name { get { return name; } }

		public double HalfLife { get { return Convert.ToDouble(halfLife); } }

		public double EnergyDensity { get { return Convert.ToDouble(energyDensity); } }

		public double Density { get { return Convert.ToDouble(density); } }

		public double Cost { get { return Convert.ToDouble(cost); } }

		#endregion

		public void Load(ConfigNode node)
		{
			ConfigNode.LoadObjectFromConfig(this, node);
		}

		public void Save(ConfigNode node)
		{
			ConfigNode.CreateConfigFromObject(this, node);
		}

		public override string ToString()
		{
			return "Name: " + name + Environment.NewLine +
				   "Half-life: " + halfLife + Environment.NewLine +
				   "Energy Density: " + energyDensity;
		}
	}

}
