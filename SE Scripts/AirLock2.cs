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
    public Program()
    { }

    public void Main(string args)
    {

    }

    public void Save()
    { }
    public static class CConfig
    {
        public static string GROUP_TAG = "airlock";
        public static string INNER_TAG = "inner:";
        public static string OUTER_TAG = "outer:";
        public static string CONTROL_TAG = "control:";
        public static bool OPEN_ANYWAY = false;

        public static Color LOCK_COLOR;
        public static Color WARN_COLOR;
        public static Color OPEN_COLOR;
    }

    public class CAirlock
    {
        public List<IMyAirVent> airVents = new List<IMyAirVent>();

        public List<IMyDoor> inDoors = new List<IMyDoor>();
        public List<IMyLightingBlock> inLights = new List<IMyLightingBlock>();
        public List<IMySoundBlock> inSounds = new List<IMySoundBlock>();

        public List<IMyDoor> outDoors = new List<IMyDoor>();
        public List<IMyLightingBlock> outLights = new List<IMyLightingBlock>();
        public List<IMySoundBlock> outSounds = new List<IMySoundBlock>();

        public string command = "";
        public bool inOpen = false;
        public bool outOpen = false;
        private bool Opening = false;
        private bool Stuck = false;
        public int wantedState = 0;
        public int currentState = 1;
        public float outPressure = 0.01f;
        public float inPressure = 0.99f;

        public void Reset()
        {
            airVents.Clear();
            inDoors.Clear();
            inLights.Clear();
            outDoors.Clear();
            outLights.Clear();
            inSounds.Clear();
            outSounds.Clear();
            inOpen = false;
            outOpen = false;            
        }

        private void SetLight(List<IMyLightingBlock> lights, Color c, float BlinkInt = 0f, float BlinkLen = 100f, float BlinkOff = 0f)
        {
            foreach (IMyLightingBlock light in lights)
            {
                if (light.GetProperty("Color").AsColor().GetValue(lights[i]) != c
                    || BlinkInt != light.BlinkIntervalSeconds
                    || !light.IsWorking)
                {
                    if (!light.IsWorking)
                    {
                        light.SetValue("Blink Interval", BlinkInt);
                        light.SetValue("Blink Lenght", BlinkLen);
                        light.SetValue("Blink Offset", BlinkOff);
                        light.SetValue("Color", c);
                        light.ApplyAction("OnOff_On");
                    }
                    else
                        light.ApplyAction("OnOff_Off");
                }
            }
        }

        private void SetClose(List<IMyDoor> doors, bool closed)
        {
            foreach (IMyDoor door in doors)
            {
                if (closed == (door.Status == DoorStatus.Closed) && closed == !door.IsWorking)
                    continue;

                if (!door.IsWorking)
                {
                    door.ApplyAction("OnOff_On");
                    continue;
                }

                if (closed && (door.Status == DoorStatus.Open))
                {
                    door.ApplyAction("Open_Off");
                    continue;
                }

                if (!closed && (door.Status == DoorStatus.Closed))
                {
                    door.ApplyAction("Open_On");
                    continue;
                }
            }
        }

        private void PlaySound(List<IMySoundBlock> sounds)
        {
            foreach (IMySoundBlock sound in sounds)
            {
                sound.ApplyAction("PlaySound");
            }
        }

        private void SetLock(List<IMyDoor> doors, bool locked)
        {
            string action = (locked ? "OnOff_Off" : "OnOff_On");
            foreach (IMyDoor door in doors)
                door.ApplyAction(action);
        }

        private bool IsPressureOk(float wantedPressure)
        {
            float press = airVents[0].GetOxygenLevel();
            return (wantedPressure + 0.01f >= press && wantedPressure - 0.01f <= press);
        }

        private void SetLights(int state)
        {
            if ((Opening && !Stuck) || state == 0)
            {
                if (wantedState == 1)
                    SetLight(inLights, CConfig.LOCK_COLOR, 2f, 50f);
                else
                    SetLight(inLights, CConfig.LOCK_COLOR);
                if (wantedState == 2)
                    SetLight(outLights, CConfig.LOCK_COLOR, 2f, 50f);
                else
                    SetLight(outLights, CConfig.LOCK_COLOR);
                return;
            }

            switch (state)
            {
                case 1:
                    if (!IsPressureOk(inPressure) && !inOpen)
                        SetLight(inLights, CConfig.WARN_COLOR);
                    else
                        SetLight(inLights, CConfig.OPEN_COLOR);

                    SetLight(outLights, CConfig.LOCK_COLOR);
                    break;
                case 2:
                    SetLight(inLights, CConfig.LOCK_COLOR);

                    if (!IsPressureOk(outPressure) && !outOpen)
                        SetLight(outLights, CConfig.WARN_COLOR);
                    else
                        SetLight(outLights, CConfig.OPEN_COLOR);
                    break;
            }
        }

        private bool IsDepressurizing()
        {
            foreach (IMyAirVent airVent in airVents)
                if (airVent.Depressurize)
                    return true;

            return false;
        }

        private void Depressurize(bool on)
        {
            foreach (IMyAirVent airVent in airVents)
            {
                if (on)
                    airVent.ApplyAction("Depressurize_On");
                else
                    airVent.ApplyAction("Depressurize_Off");
            }
        }

        public void Process()
        {
            if (!inOpen && !outOpen && !IsPressureOk(inPressure) && !IsPressureOk(outPressure))
                currentState = 0;
            else
            if (inOpen && !outOpen && !IsDepressurizing() && inPressure >= 0.98f)
                currentState = 1;
            else
            if (!inOpen && outOpen && IsDepressurizing())
                currentState = 2;
            else
            if (!(inPressure + 0.01f >= outPressure && inPressure - 0.01f <= outPressure))
                currentState = 3;
            else
            {
                SetClose(inDoors, true);
                SetClose(outDoors, true);
                currentState = 0;
            }

            if (command != "")
            {
                switch (command)
                {
                    case "in":
                        wantedState = 1;
                        break;
                    case "out":
                        wantedState = 2;
                        break;
                    case "toggle":
                        wantedState = (wantedState == 2 ? 1 : 2);
                        break;
                }
            }
            if (currentState == wantedState || currentState == 3) wantedState = 0;
            switch (wantedState)
            {
                case 0:
                    break;
                case 1:
                    if (currentState == 2)
                    {
                        SetClose(outDoors, true);
                        currentState = 0;
                    }
                    if (currentState == 0)
                    {
                        Depressurize(false);
                        if (IsPressureOk(inPressure))
                        {
                            SetClose(inDoors, false);
                            currentState = 1;
                            wantedState = 0;
                        }
                    }
                    break;
                case 2:
                    if (currentState == 1)
                    {
                        SetClose(inDoors, true);
                        currentState = 0;
                    }
                    if (currentState == 0)
                    {
                        Depressurize(true);
                        if (IsPressureOk(outPressure))
                        {
                            SetClose(outDoors, false);
                            currentState = 2;
                            wantedState = 0;
                        }
                    }
                    break;

            }
        }
    }
    // КОНЕЦ СКРИПТА
}