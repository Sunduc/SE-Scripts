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

    float ThrustTresh = 250000; // Порог тяги тестового двигателя, начиная с которого подключается гравдрайв.

    bool GravOn = false;
    
    IMyThrust ThrBackward;
    IMyThrust ThrForward;
    List<IMyThrust> ThrList;
    
    List<IMyArtificialMassBlock> MassList;

    //IMyGravityGenerator gravigen;
    List<IMyGravityGenerator> gravigenlist;
    
    IMyGyro gyroscope;
    
    Program()
    {
        // Создаем списки
        ThrList = new List<IMyThrust>();
        MassList = new List<IMyArtificialMassBlock>();
        gravigenlist = new List<IMyGravityGenerator>();

        // Находим генераторы гравитации
        GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(gravigenlist);

        //Находим гироскоп
        gyroscope = GridTerminalSystem.GetBlockWithName("MB-1 Gyro GD") as IMyGyro;
        
        // Находим движки
        GridTerminalSystem.GetBlocksOfType<IMyThrust>(ThrList);
        foreach (IMyThrust thr in ThrList)
        {            
            if (thr.CustomName.Contains("[Backward]"))
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

        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }

    public void Main(string argument)
    {
        if (argument == "Start")
        {
            GravOn = true;
        }
        if (argument == "Stop")
        {
            foreach (IMyGravityGenerator grav in gravigenlist)
            {
                grav.ApplyAction("OnOff_Off");
            }

            foreach (IMyArtificialMassBlock mass in MassList)
            {
                mass.ApplyAction("OnOff_Off");
            }

            gyroscope.GyroOverride = false;
            gyroscope.ApplyAction("OnOff_Off");

            GravOn = false;
        }

        if (GravOn)
        {
            UpdateGravDrive();
        }
    }

    public void UpdateGravDrive()
    {
        foreach (IMyGravityGenerator grav in gravigenlist)
        {
            grav.ApplyAction("OnOff_Off");
        }

        foreach (IMyArtificialMassBlock mass in MassList)
        {
            mass.ApplyAction("OnOff_Off");
        }

        gyroscope.GyroOverride = false;
        gyroscope.ApplyAction("OnOff_Off");

        if (ThrBackward.CurrentThrust > ThrustTresh)
        {
            foreach (IMyGravityGenerator grav in gravigenlist)
            {
                grav.ApplyAction("OnOff_On");
                grav.GravityAcceleration = -9.8f;
            }

            foreach (IMyArtificialMassBlock mass in MassList)
            {
                mass.ApplyAction("OnOff_On");
            }

            gyroscope.GyroOverride = true;
            gyroscope.ApplyAction("OnOff_On");

        }
        else if (ThrForward.CurrentThrust > ThrustTresh)
        {
            foreach (IMyGravityGenerator grav in gravigenlist)
            {
                grav.ApplyAction("OnOff_On");
                grav.GravityAcceleration = 9.8f;
            }

            foreach (IMyArtificialMassBlock mass in MassList)
            {
                mass.ApplyAction("OnOff_On");
            }

            gyroscope.GyroOverride = true;
            gyroscope.ApplyAction("OnOff_On");
            
        }

    }

    // КОНЕЦ СКРИПТА
}