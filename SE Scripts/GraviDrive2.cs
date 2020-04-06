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
    // Нужно 6 тестовых трастеров, с которых снимается тяга для проверки, куда движемся.
    // Называться движки должны со слов "Right", "Left", "Up", "Down", "Forward", "Backward"  
    // Сам гравдрайв - кубик, внутри которого грависферы, накрученные на максимум отталкивания
    // По сторонам блоки искусств. массы с названиями, начиная с "Right", "Left", "Up", "Down", "Forward", "Backward".
    // Аргументы для кнопок в кабине: "Start", "Stop"  

    float ThrustTresh = 3500000; // Порог тяги тестового двигателя, начиная с которого подключается гравдрайв.

    bool GravOn = false;

    IMyThrust ThrRight;
    IMyThrust ThrLeft;
    IMyThrust ThrUp;
    IMyThrust ThrDown;
    IMyThrust ThrBackward;
    IMyThrust ThrForward;
    List<IMyThrust> ThrList;

    List<IMyArtificialMassBlock> MassList;
    List<IMyArtificialMassBlock> RightMassList;
    List<IMyArtificialMassBlock> LeftMassList;
    List<IMyArtificialMassBlock> UpMassList;
    List<IMyArtificialMassBlock> DownMassList;
    List<IMyArtificialMassBlock> BackwardMassList;
    List<IMyArtificialMassBlock> ForwardMassList;

    IMyGravityGenerator gravigen;
    //List<IMyGravityGenerator> gravigenlist;
    List<IMyGravityGeneratorSphere> gravidrivelist;

    IMyGyro gyroscope;

    Program()
    {
        // Создаем списки
        ThrList = new List<IMyThrust>();
        MassList = new List<IMyArtificialMassBlock>();
        RightMassList = new List<IMyArtificialMassBlock>();
        LeftMassList = new List<IMyArtificialMassBlock>();
        UpMassList = new List<IMyArtificialMassBlock>();
        DownMassList = new List<IMyArtificialMassBlock>();
        BackwardMassList = new List<IMyArtificialMassBlock>();
        ForwardMassList = new List<IMyArtificialMassBlock>();
        //gravigenlist = new List<IMyGravityGenerator>();
        gravidrivelist = new List<IMyGravityGeneratorSphere>();

        // Находим генераторы гравитации
        //GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(gravigenlist);
        gravigen = GridTerminalSystem.GetBlockWithName("Gravity Generator") as IMyGravityGenerator;

        GridTerminalSystem.GetBlocksOfType<IMyGravityGeneratorSphere>(gravidrivelist);

        //Находим гироскоп
        gyroscope = GridTerminalSystem.GetBlockWithName("Gyroscope 1") as IMyGyro;
        
        // Находим движки
        GridTerminalSystem.GetBlocksOfType<IMyThrust>(ThrList);
        foreach (IMyThrust thr in ThrList)
        {            
            if (thr.CustomName.Contains("[Right]"))
            {
                ThrRight = thr;
            }
            else if (thr.CustomName.Contains("[Left]"))
            {
                ThrLeft = thr;
            }
            else if (thr.CustomName.Contains("[Up]"))
            {
                ThrUp = thr;
            }
            else if (thr.CustomName.Contains("[Down]"))
            {
                ThrDown = thr;
            }
            else if (thr.CustomName.Contains("[Backward]"))
            {
                ThrBackward = thr;
            }
            else if (thr.CustomName.Contains("[Forward]"))
            {
                ThrForward = thr;
            }
        }
        // Находим блоки искусст. массы

        GridTerminalSystem.GetBlocksOfType<IMyArtificialMassBlock>(MassList);
        foreach (IMyArtificialMassBlock mass in MassList)
        {
            if (mass.CustomName.Contains("[Right]"))
            {
                RightMassList.Add(mass);
            }
            else if (mass.CustomName.Contains("[Left]"))
            {
                LeftMassList.Add(mass);
            }
            else if (mass.CustomName.Contains("[Up]"))
            {
                UpMassList.Add(mass);
            }
            else if (mass.CustomName.Contains("[Down]"))
            {
                DownMassList.Add(mass);
            }
            else if (mass.CustomName.Contains("[Backward]"))
            {
                BackwardMassList.Add(mass);
            }
            else if (mass.CustomName.Contains("[Forward]"))
            {
                ForwardMassList.Add(mass);
            }
        }
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }

    public void Main(string argument)
    {
        if (argument == "Start")
        {
            //gravigen.ApplyAction("OnOff_Off");
            //gyroscope.GyroOverride = true;
            //foreach (IMyGravityGeneratorSphere grav in gravidrivelist)
            //{
            //    grav.ApplyAction("OnOff_On");
            //}
            GravOn = true;
        }
        if (argument == "Stop")
        {
            SetMassGroup(MassList, "Off");
            //gravigen.ApplyAction("OnOff_On");
            //gyroscope.GyroOverride = false;
            //foreach (IMyGravityGeneratorSphere grav in gravidrivelist)
            //{
            //    grav.ApplyAction("OnOff_Off");
            //}
            GravOn = false;
        }

        if (GravOn)
        {
            UpdateGravDrive();

        }
    }

    public void UpdateGravDrive()
    {
        SetMassGroup(MassList, "Off");

        if (ThrRight.CurrentThrust > ThrustTresh)
        {
            SetMassGroup(RightMassList, "On");
        }
        else if (ThrLeft.CurrentThrust > ThrustTresh)
        {
            SetMassGroup(LeftMassList, "On");
        }

        if (ThrUp.CurrentThrust > ThrustTresh)
        {
            SetMassGroup(UpMassList, "On");
        }
        else if (ThrDown.CurrentThrust > ThrustTresh)
        {
            SetMassGroup(DownMassList, "On");
        }

        if (ThrBackward.CurrentThrust > ThrustTresh)
        {
            SetMassGroup(BackwardMassList, "On");
        }
        else if (ThrForward.CurrentThrust > ThrustTresh)
        {
            SetMassGroup(ForwardMassList, "On");
        }

    }

    public void SetMassGroup(List<IMyArtificialMassBlock> MassGroup, string OnOff)
    {
        switch (OnOff)
        {
            case "On":
                gyroscope.GyroOverride = true;
                gravigen.ApplyAction("OnOff_Off");
                break;
            case "Off":
                gyroscope.GyroOverride = false;
                gravigen.ApplyAction("OnOff_On");
                break;
        }

        foreach (IMyGravityGeneratorSphere grav in gravidrivelist)
        {
            grav.ApplyAction("OnOff_" + OnOff);
        }
        foreach (IMyArtificialMassBlock mass in MassGroup)
        {
            mass.ApplyAction("OnOff_" + OnOff);
        }
    }
    // КОНЕЦ СКРИПТА
}