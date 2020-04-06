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
    
    const string RADIAL_PISTON_TAG = "[R]";
    const string VERTICAL_PISTON_TAG = "[V]";
    
    const float STOP_PERCENTAGE = 95f;
    const float CONTINUE_PERCENTAGE = 80f;
    const float R_STEP = 2.5f;
    const float V_STEP = 1.25f;
    const float FREE_SPEED = 4f;
    const float DRILL_SPEED = 2f;
    const float MIN_RADIUS = 15.7f;
    
    List<IMyTerminalBlock> allBlocks;
    List<IMyPistonBase> rPistons;
    List<IMyPistonBase> vPistons;
    IMyShipDrill drills;
    IMyInventory cargo;
    IMyMotorAdvancedStator rotor;
    
    string state;
    
    float rVelocity;
    float rLimit;

    float vVelocity;
    float vLimit;
    
    float rotSpeed;
    
    bool wRotor;
    bool wDrills;
    bool wPistons;
    
    public Program()
    {
        allBlocks = new List<IMyTerminalBlock>();
        //cargo = new List<IMyInventory>();
        rPistons = new List<IMyPistonBase>();
        vPistons = new List<IMyPistonBase>();       

        GridTerminalSystem.GetBlocks(allBlocks);
        foreach (IMyTerminalBlock block in allBlocks)
        {
            IMyPistonBase piston = block as IMyPistonBase;
            if (piston != null)
            {
                if (piston.CustomName.Contains(RADIAL_PISTON_TAG)) rPistons.Add(piston);
                if (piston.CustomName.Contains(VERTICAL_PISTON_TAG)) vPistons.Add(piston);
            }

            IMyShipDrill drill = block as IMyShipDrill;
            if (drill != null)
            {
                drills = drill;
            }

            IMyInventory cont = block.GetInventory();
            if (cont != null)
            {
                if (block.CustomName.Contains("Cargo")) cargo = cont;
            }

            IMyMotorAdvancedStator rot = block as IMyMotorAdvancedStator;
            if (rot != null)
            {
                rotor = rot;
            }
        }
        Runtime.UpdateFrequency = UpdateFrequency.Update100;

    }

    public void Main(string argument)
    {
        switch (argument)
        {
            case "Begin":
            {
                if (state != "Begin") return;
                
                vLimit = 0;
                vVelocity = DRILL_SPEED;
                rLimit = 0f;
                rVelocity = -DRILL_SPEED;
                rotSpeed = DRILL_SPEED/MIN_RADIUS;
                wDrills = true;
                wPistons = true;
                wRotor = true;
                state = "Work";
                
                UpdateMiner();
                break;
            }
            case "Stop":
            {
                wDrills = false;
                wPistons = false;
                wRotor = false;
                state = "Stop";
                
                UpdateMiner();
                break;
            }
            case "Start":
            {
                wDrills = true;
                wPistons = true;
                wRotor = true;
                state = "Work";
                if (rotSpeed > 0)
                    rotSpeed = DRILL_SPEED/(MIN_RADIUS+rLimit);
                else
                    rotSpeed = -DRILL_SPEED/(MIN_RADIUS+rLimit);
                UpdateMiner();
                break;
            }
            case "Reset":
            {
                vLimit = 0f;
                vVelocity = -FREE_SPEED;
                rLimit = 0f;
                rVelocity = -FREE_SPEED;
                rotSpeed = -FREE_SPEED/MIN_RADIUS;
                wDrills = false;
                wPistons = true;
                wRotor = true;
                state = "Parking";
                
                UpdateMiner();
                break;
            }
            case "V+":
            {
                wPistons = true;
                vLimit += V_STEP;
                if (rotSpeed > 0)
                    rotSpeed = DRILL_SPEED / (MIN_RADIUS + rLimit);
                else
                    rotSpeed = -DRILL_SPEED / (MIN_RADIUS + rLimit);
                UpdateMiner();
                break;
            }
            case "V-":
            {
                wPistons = true;
                vLimit -= V_STEP;
                if (rotSpeed > 0)
                    rotSpeed = DRILL_SPEED / (MIN_RADIUS + rLimit);
                else
                    rotSpeed = -DRILL_SPEED / (MIN_RADIUS + rLimit);
                UpdateMiner();
                break;
            }
            case "R+":
            {
                wPistons = true;
                rLimit += R_STEP;
                if (rotSpeed > 0)
                    rotSpeed = DRILL_SPEED / (MIN_RADIUS + rLimit);
                else
                    rotSpeed = -DRILL_SPEED / (MIN_RADIUS + rLimit);
                UpdateMiner();
                break;
            }
            case "R-":
            {
                wPistons = true;
                rLimit -= R_STEP;
                if (rotSpeed > 0)
                    rotSpeed = DRILL_SPEED / (MIN_RADIUS + rLimit);
                else
                    rotSpeed = -DRILL_SPEED / (MIN_RADIUS + rLimit);
                UpdateMiner();
                break;
            }
            default:
                break;
        }
        switch (state)
        {
            case "Work":
            {
                if (((float)cargo.CurrentVolume / (float)cargo.MaxVolume) > STOP_PERCENTAGE)
                {
                wDrills = false;
                wPistons = false;
                wRotor = false;
                state = "Stop";
                
                UpdateMiner();
                return;
                }
                if (Math.Round(rotor.Angle * 1000) == 0)
                {
                    SwitchMiner();
                    rotSpeed = DRILL_SPEED/(MIN_RADIUS+rLimit);
                }
                if (Math.Round(rotor.Angle * 1000) == Math.Round(Math.PI * 1000))
                {
                    SwitchMiner();
                    rotSpeed = -DRILL_SPEED/(MIN_RADIUS+rLimit);
                }
                UpdateMiner();
                break;
            }
            case "Parking":
            {
                wDrills = false;
                if (Math.Round(rotor.Angle * 1000) == 0 && rPistons[0].CurrentPosition == 0 && vPistons[0].CurrentPosition == 0)
                {
                    rVelocity = 0f;
                    vVelocity = 0f;
                    rotSpeed = 0f;
                    wRotor = false;
                    wPistons = false;
                    state = "Begin";
                }
                UpdateMiner();
                break;
            }
            default:
                break;
        }
    }
    public void SwitchMiner()
    {
        if ((rLimit + R_STEP * rVelocity / DRILL_SPEED) < 0 || (rLimit + R_STEP * rVelocity / DRILL_SPEED) > 35)
        {
            if ((vLimit + V_STEP * vVelocity / DRILL_SPEED) > 70)
            {
                state = "Parking";
                return;
            }
            rVelocity = -rVelocity;
            vLimit += V_STEP;
        }
        else
            rLimit += R_STEP * rVelocity / DRILL_SPEED;
    }
        public void UpdateMiner()
    {
        rotor.TargetVelocityRad = rotSpeed;
        rotor.Enabled = wRotor;
        foreach (IMyPistonBase piston in rPistons)
        {
            piston.Velocity = rVelocity;
            piston.MaxLimit = rLimit;
            piston.MinLimit = rLimit;
            piston.Enabled = wPistons;
        }

        foreach (IMyPistonBase piston in vPistons)
        {
            piston.Velocity = vVelocity;
            piston.MaxLimit = vLimit;
            piston.MinLimit = vLimit;
            piston.Enabled = wPistons;
        }        
        
        drills.Enabled = wDrills;
    }

    // КОНЕЦ СКРИПТА
}