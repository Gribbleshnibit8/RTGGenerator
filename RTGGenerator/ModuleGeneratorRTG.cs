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
        
        static private string[] FuelTypes = { "Blutonium", "Karborundum", "Kerbaricium" };
        static private decimal[] EnergyDensity = { 0.46M, 0.54M, 0.135M };
        static private double[] Halflives = { 8.352, 25.433, 125.28 };
        static private string[] ThermocoupleTypes = { "Kerbanium Telluride", "Thermionic", "Thermophotovoltaic" };
        static private double[] ThermocoupleEfficiencies = { 0.05, 0.18, 0.3 };
        
        // index of the fuel type selected, 
        [KSPField(isPersistant = true)]
        private int selectedFuel = 0;

        // index of the thermocouple type selected, 
        [KSPField(isPersistant = true)]
        private int selectedThermocouple = 0;

    // information output
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Power Output", guiUnits = " Ec/s")]
        private double PowerOutput = 0;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Fuel")]
        public string CurrentFuel = String.Empty;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Half-life", guiUnits = " years")]
        public double Halflife = 0;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Thermocouple")]
        public string CurrentThermocouple = String.Empty;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Efficiency")]
        public double ThermocoupleEfficiency = 0;


    // flight scene outputs
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Fuel Remaining", guiUnits = "%")]
        public double FuelRemaining = 100;


    // input options
        // value measured in kilograms, max is 50 kilograms
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Fuel Mass", guiUnits = "kg"), UI_FloatRange(minValue = 1f, maxValue = 50f, stepIncrement = 1f)]
        public float FuelMass = 5;

        // select the next fuel type
        [KSPEvent(guiName = "Next Fuel", guiActiveEditor = true, guiActive = false)]
        public void nextFuel()
        {
            selectedFuel++;
            if (selectedFuel >= FuelTypes.Length)
                selectedFuel = 0;
            updateValues();
        }

        // select the next thermocouple type
        [KSPEvent(guiName = "Next Thermocouple", guiActiveEditor = true, guiActive = false)]
        public void nextThermocouple()
        {
            selectedThermocouple++;
            if (selectedThermocouple >= ThermocoupleTypes.Length)
                selectedThermocouple = 0;
            updateValues();
        }


        // Game predetermined updates
        // updates part values once every Unity FixedUpdate cycle (once per physics frame) once the part has been activated. 
        public override void OnFixedUpdate()
        {
            updateValues();
        }

        public override void OnUpdate()
        {
            part.RequestResource( "ElectricCharge", -getPowerOutput() );
        }

        public override void OnAwake()
        {
            updateValues();
            if (calculateDecay() < 6.25)
                ScreenMessages.PostScreenMessage("The RTG is operating below one sixteenth efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
            else if (calculateDecay() < 12.5)
                ScreenMessages.PostScreenMessage("The RTG is operating at one eighth efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
            else if (calculateDecay() < 25)
                ScreenMessages.PostScreenMessage("The RTG is operating at one quarter efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
            else if (calculateDecay() < 50)
                ScreenMessages.PostScreenMessage("The RTG is operating at one half efficiency.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        // module info as displayed in VAB/SPH
        public override string GetInfo()
        {
            string s = "Nuclear based RTG with various fuel and thermocouple types and selectable fuel mass.\nFine tune your energy production needs on a per-mission basis.";
            return s;
        }

        // recalculate the display values based on selected fuel type
        private void updateValues()
        {
            CurrentFuel = FuelTypes[selectedFuel];
            Halflife = Halflives[selectedFuel];
            PowerOutput = getPowerOutput();

            CurrentThermocouple = ThermocoupleTypes[selectedThermocouple];
            ThermocoupleEfficiency = ThermocoupleEfficiencies[selectedThermocouple];

            //ScreenMessages.PostScreenMessage("Current Thermocouple: " + CurrentThermocouple + ".\nEfficiency " + ThermocoupleEfficiency + ".", 5f, ScreenMessageStyle.UPPER_CENTER);

            FuelRemaining = Math.Round(calculateDecay(), 0);
        }

        private double calculateDecay()
        {
            double missionTime;
            if (HighLogic.LoadedSceneIsEditor)
                missionTime = 0;
            else
                missionTime = vessel.missionTime;

            // mission time / seconds in minute / minutes in hour / hours in day / days in year
            double elapsedYears = missionTime / 60 / 60 / 6 / 426.08;

            double result = 100 * Math.Pow(2, (-elapsedYears / Halflife));

            //ScreenMessages.PostScreenMessage("RTG Decay calculated for mission time " + missionTime + ".\nAmount Remaining is " + result + ".", 5f, ScreenMessageStyle.UPPER_CENTER);

            return result;
        }

        private double getPowerOutput()
        {
            double reactionPower = (double)Decimal.Multiply(EnergyDensity[selectedFuel], (decimal)FuelMass);

            //ScreenMessages.PostScreenMessage("Reaction power is " + reactionPower + ".", 5f, ScreenMessageStyle.UPPER_CENTER);

            // energy output = Decay Rate * power from fuel * efficiency of thermocouple / some value that makes it right?
            double energyFromGenerator = (calculateDecay() * reactionPower * ThermocoupleEfficiency) / 15;

            //ScreenMessages.PostScreenMessage("Output power is " + energyFromGenerator + ".", 5f, ScreenMessageStyle.UPPER_CENTER);

            return Math.Round(energyFromGenerator, 2);
        }

    }
}
