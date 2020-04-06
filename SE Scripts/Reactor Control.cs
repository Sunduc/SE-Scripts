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
    string state = "On";

    IMyTextPanel tp;
    List<IMyTextPanel> tpanels;

    List<IMyReactor> reactors;
    List<IMyReactor> reactorsIG;

    List<IMyBatteryBlock> batterys;
    List<IMyBatteryBlock> batterysIG;

    public Program()
    {
        //tp = GridTerminalSystem.GetBlockWithName("TP") as IMyTextPanel;        

        tpanels = new List<IMyTextPanel>();

        reactors = new List<IMyReactor>();
        reactorsIG = new List<IMyReactor>();

        batterys = new List<IMyBatteryBlock>();
        batterysIG = new List<IMyBatteryBlock>();

        GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(tpanels);
        foreach (IMyTextPanel tpanel in tpanels)
            if (tpanel.CustomName.Contains("LCD Reactor")) tp = tpanel;

        GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors);        
        foreach (IMyReactor reactor in reactors)
            if (reactor.CubeGrid == Me.CubeGrid) reactorsIG.Add(reactor);

        GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batterys);
        foreach (IMyBatteryBlock battery in batterys)
            if (battery.CubeGrid == Me.CubeGrid) batterysIG.Add(battery);

        Runtime.UpdateFrequency = UpdateFrequency.Update10;
    }

    public void Main()
    {
        float mspower = 0;
        float cspower = 0;
        float mpower = 0;
        float cpower = 0;
        string state1 = "";
        
        foreach (IMyBatteryBlock battery in batterysIG)
        {
            mspower += battery.MaxStoredPower;
            cspower += battery.CurrentStoredPower;
        }
        if (cspower / mspower >= 0.67f)
            state = "Off";

        if (cspower / mspower <= 0.33f)
            state = "On";

        foreach (IMyReactor reactor in reactorsIG)
        {
            reactor.ApplyAction("OnOff_" + state);
            mpower += reactor.MaxOutput;
            cpower += reactor.CurrentOutput;
        }

        if (state == "On") state1 = "Вкл.";
        else state1 = "Выкл.";

        tp.WritePublicText("Заряд батарей: " + Math.Round(cspower, 2).ToString() + "MWh/" + mspower.ToString() + "MWh   " + Math.Round(100 * cspower / mspower) +
            "%\n\nРеакторы: " + state1 + "   " + Math.Round(cpower, 2).ToString() + "MW/" + mpower.ToString() + "MW   " + Math.Round(100 * cpower / mpower) + "%");
    }

    public void Save()
    { }
    // КОНЕЦ СКРИПТА
}