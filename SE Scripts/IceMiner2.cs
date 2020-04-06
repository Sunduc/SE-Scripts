using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
public sealed class Program : MyGridProgram
{
    // НАЧАЛО СКРИПТА
    List<IMyTerminalBlock> allBlocks;
    List<IMyPistonBase> rPistons;
    List<IMyPistonBase> vPistons;
    List<IMyShipDrill> drills;
    List<IMyInventory> cargo;
    IMyMotorAdvancedStator rotor;
    //IMyTextPanel lcd;

    float rVelocity;
    float rLimit;
    //float rStep;

    float vVelocity;
    float vLimit;
    //float vStep;

    float switchAngle;
    float drillVelocity;

    bool fCargo;
    bool jobDone;

    string wrotor;
    string wdrills;

    string state;

    public Program()
    {
        allBlocks = new List<IMyTerminalBlock>();
        rPistons = new List<IMyPistonBase>();
        vPistons = new List<IMyPistonBase>();
        drills = new List<IMyShipDrill>();
        cargo = new List<IMyInventory>();

        GridTerminalSystem.GetBlocks(allBlocks);
        foreach (IMyTerminalBlock block in allBlocks)
        {
            IMyPistonBase piston = block as IMyPistonBase;
            if (piston != null)
            {
                if (piston.CustomName.Contains("[R]")) rPistons.Add(piston);
                if (piston.CustomName.Contains("[V]")) vPistons.Add(piston);
            }

            IMyShipDrill drill = block as IMyShipDrill;
            if (drill != null)
            {
                drills.Add(drill);
            }

            IMyInventory cont = block.GetInventory();
            if (cont != null)
            {
                if (!block.CustomName.Contains("Stone Ejector")) cargo.Add(cont);
            }

            IMyMotorAdvancedStator rot = block as IMyMotorAdvancedStator;
            if (rot != null)
            {
                rotor = rot;
            }

            /*IMyTextPanel tp = block as IMyTextPanel;
            if (tp != null)
            {
                lcd = tp;
            }*/
        }
        InitMiner();

        Runtime.UpdateFrequency = UpdateFrequency.Update1;

    }

    public void Main(string argument)
    {
        if (argument == "Reset")
        {
            jobDone = false;
            state = "Reset";
        }

        if (argument == "Stop")
        {
            drillVelocity = 0;
            wrotor = "Off";
            wdrills = "Off";
            state = argument;
            UpdateMiner();
        }

        if (state == "Reset")
        {
            switchAngle = 0;
            rVelocity = -1f;
            rLimit = 0;
            vVelocity = -0.5f;
            vLimit = 0;
            wdrills = "Off";

            if (Math.Round(rotor.Angle * 1000) == 0)
            {
                state = "";
                drillVelocity = 0;
                wrotor = "Off";
            }
            else
            {
                rotor.UpperLimitDeg = 0;
                rotor.LowerLimitDeg = 0;
            }
            UpdateMiner();
        }

        if (argument == "Start")
        {
            IsCargoFull(cargo);
            if (jobDone || fCargo)
            {
                return;
            }
            
            if (vVelocity < 0)
                vVelocity = -vVelocity;

            drillVelocity = 2 / (7.7f + rLimit);
            wrotor = "On";
            wdrills = "On";
            state = "Work";
        }
        
        if (state == "Work")
        {
            if (rotor.Angle >= switchAngle && rotor.Angle < (switchAngle + Math.PI))
            {
                if (switchAngle == 0)
                    switchAngle = (float)Math.PI;
                else
                    switchAngle = 0;

                if ((rLimit + rVelocity) < 0 || (rLimit + rVelocity) > 10)
                {
                    if ((vLimit + vVelocity) > 10)
                    {
                        jobDone = true;
                        state = "Reset";
                        return;
                    }
                    rVelocity = -rVelocity;
                    vLimit += vVelocity;
                }
                else
                    rLimit += rVelocity;

                drillVelocity = 2 / (7.7f + rLimit);
            }
            UpdateMiner();
        }
        
        //UpdateMiner();
    }
    public void IsCargoFull(List<IMyInventory> conts)
    {
        float cVolume = 0;
        float mVolume = 0;
        foreach (IMyInventory cont in conts)
        {
            cVolume += (float)cont.CurrentVolume;
            mVolume += (float)cont.MaxVolume;
            if (cVolume / mVolume > 0.95f)
                fCargo = true;
            else
                fCargo = false;
        }
    }

    public void UpdateMiner()
    {
        rotor.TargetVelocityRad = drillVelocity;
        rotor.ApplyAction("OnOff_" + wrotor);
        foreach (IMyPistonBase piston in rPistons)
        {
            piston.Velocity = rVelocity;
            piston.MaxLimit = rLimit;
            piston.MinLimit = rLimit;
        }

        foreach (IMyPistonBase piston in vPistons)
        {
            piston.Velocity = vVelocity;
            piston.MaxLimit = vLimit;
            piston.MinLimit = vLimit;
        }
        foreach (IMyShipDrill drill in drills)
        {
            drill.ApplyAction("OnOff_" + wdrills);
        }
        //lcd.WritePublicText(state + "\n" + rotor.Angle.ToString());
    }

    public void InitMiner()
    {
        state = "";
        switchAngle = 0;
        jobDone = false;

        drillVelocity = 0;
        wrotor = "Off";

        rVelocity = -1;
        rLimit = 0;
        //rStep = 1;

        vVelocity = -0.5f;
        vLimit = 0;
        //vStep = 0.5f;

        wdrills = "Off";

        UpdateMiner();
    }    
    // КОНЕЦ СКРИПТА
}