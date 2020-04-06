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
    IMyTimerBlock timer;
    //IMyTextPanel tp;

    float rVelocity;
    float rLimit;

    float vVelocity;
    float vLimit;
    float rpm;
    bool fCargo;
    bool jobDone;
    string wrotor;
    string wdrills;
    bool stimer;
    float timerDelay;

    public Program()
    {
        //tp = GridTerminalSystem.GetBlockWithName("DEBUG") as IMyTextPanel;
        //tp.WritePublicText("");
        allBlocks = new List<IMyTerminalBlock>();
        rPistons = new List<IMyPistonBase>();
        vPistons = new List<IMyPistonBase>();
        drills = new List<IMyShipDrill>();
        cargo = new List<IMyInventory>();

        GridTerminalSystem.GetBlocks(allBlocks);
        foreach (IMyTerminalBlock block in allBlocks)
        {
            //tp.WritePublicText(block.CustomName + "\n", true);
            IMyPistonBase piston = block as IMyPistonBase;
            if (piston != null)
            {
                //tp.WritePublicText(piston.CustomName + "\n", true);
                if (piston.CustomName.Contains("[R]")) rPistons.Add(piston);
                if (piston.CustomName.Contains("[V]")) vPistons.Add(piston);
            }

            IMyShipDrill drill = block as IMyShipDrill;
            if (drill != null)
            {
                //tp.WritePublicText(drill.CustomName + "\n", true);
                drills.Add(drill);
            }

            IMyInventory cont = block.GetInventory();
            if (cont != null)
            {
                //tp.WritePublicText(block.CustomName + "\n", true);
                if (!block.CustomName.Contains("Stone Ejector")) cargo.Add(cont);
            }

            IMyMotorAdvancedStator rot = block as IMyMotorAdvancedStator;
            if (rot != null)
            {
                rotor = rot;
                //tp.WritePublicText(rot.CustomName + "\n", true);
            }
            IMyTimerBlock tim = block as IMyTimerBlock;
            if (tim != null)
            {
                timer = tim;
            }

            //ResetMiner();

            //Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        ResetMiner();

        //Runtime.UpdateFrequency = UpdateFrequency.Update10;

    }

    public void Main(string argument)
    {
        if (argument == "Reset")
        {
            ResetMiner();
            return;
        }

        if (argument == "Stop")
        {
            rpm = 0;
            wrotor = "Off";
            wdrills = "Off";
            stimer = false;
            //UpdateMiner();
        }
        if (argument == "Start")
        {
            IsCargoFull(cargo);
            if (jobDone || fCargo)
            {
                //argument = "Stop";                
                return;
            }
            if (rLimit != 0 || vLimit !=0)
            {
                if (rLimit <= 2)
                {
                    rpm = 3;                    
                }
                if (rLimit > 2 && rLimit < 8)
                {
                    rpm = 1.5f;                    
                }
                if (rLimit >= 8)
                {
                    rpm = 1f;                    
                }
            }
            else
            {
                rpm = 3;
                vVelocity = -vVelocity;
            }
            wrotor = "On";
            wdrills = "On";
            stimer = true;            
            //UpdateMiner();
        }
        //int res;
        //int angle = (int) Math.Round(rotor.Angle * 100);
        //Math.DivRem((int)Math.Round(rotor.Angle * 1000), (int)Math.Round(Math.PI * 10000), out res);
        if (argument == "Timer")
        //if ((rotor.Angle % Math.PI) == 0)
        {
            if ((rLimit + rVelocity) < 0 || (rLimit + rVelocity) > 10)
            {
                if ((vLimit + vVelocity) > 10)
                {
                    jobDone = true;
                    //argument = "Stop";
                    rpm = 0;
                    wrotor = "Off";
                    wdrills = "Off";
                    stimer = false;
                    UpdateMiner();
                    return;
                }
                rVelocity = -rVelocity;
                vLimit += vVelocity;
            }
            else
                rLimit += rVelocity;

            if (rLimit <= 2)
            {
                rpm = 3;
                timerDelay = 10;
            }
            if (rLimit > 2 && rLimit < 8)
            {
                rpm = 1.5f;
                timerDelay = 20;
            }
            if (rLimit >= 8)
            {
                rpm = 1f;
                timerDelay = 30;
            }
            
            stimer = true;
        }
        UpdateMiner();
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

        rotor.TargetVelocityRPM = rpm;
        //rotor.TargetVelocityRad = (float) Math.PI * 2 * rpm / 60;
        rotor.ApplyAction("OnOff_" + wrotor);
        //rotor.SafetyLock = true;
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
        timer.TriggerDelay = timerDelay;
        if (stimer)
            timer.StartCountdown();
        else
            timer.StopCountdown();
    }

    public void ResetMiner()
    {
        jobDone = false;
        rpm = 0;
        wrotor = "Off";
        rVelocity = -1f;
        rLimit = 0;
        vVelocity = -0.5f;
        vLimit = 0;
        wdrills = "Off";
        stimer = false;
        timerDelay = 10;

        UpdateMiner();
    }
    public void Save()
    { }
    // КОНЕЦ СКРИПТА
}