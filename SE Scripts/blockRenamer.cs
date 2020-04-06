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
    List<IMyTerminalBlock> allblocks;
    string gridTag = "PS-1";

    public Program()
    {
        allblocks = new List<IMyTerminalBlock>();
        GridTerminalSystem.GetBlocks(allblocks);
    }

    public void Main(string args)
    {
        foreach (IMyTerminalBlock block in allblocks)
        {
            if (block.CustomName.StartsWith(gridTag) || block.CubeGrid != Me.CubeGrid) continue;
            else block.CustomName = gridTag + " " + block.CustomName;
        }
    }

    public void Save()
    { }
    // КОНЕЦ СКРИПТА
}