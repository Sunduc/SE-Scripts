using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRage;
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

namespace DrillStation
{public sealed class Program : MyGridProgram{

//=== от сюда ===>>>===>>>
// код andrukha74#3658 (diskord)

const string LCDName = "Control Seat(0)";                      //если пусто, используется LCD прог.блока
//const string CockpitName = "";                  //если пусто, не используется
const string AZIMUTH_ROTOR_TAG = "[A]";         //часть имени ротора
const string HORIZONTAL_PISTONS_TAG = "[H]";    //часть имени горизонтальных поршней
const string VERTICAL_PISTONS_TAG = "[V]";      //часть имени вертикальных поршней

const float MIN_RADIUS = 20.7f;
const float ZERO_ROTOR_ANGLE = 0f / 180f * MathHelper.Pi;
const float MAX_ANGLE = 60 / 180f * MathHelper.Pi;
const float STOP_PERCENTAGE = 95f;
const float CONTINUE_PERCENTAGE = 80f;
const float H_STEP = 7.5f;
const float V_STEP = 2.5f;

const float FREE_VELOCITY = 3.5f;
const float DRILL_VELOCITY = 1.8f;
const float FADE_SPEED_RANGE = 2f;

const float PISTON_FORCE = 50000f;
const float PISTON_MIN_FORCE = 100f;
const float ROTOR_FORCE = 33.6E6f;
const float ROTOR_BRAKE_FORCE = 1.0E6f;

const float V_DIRECTION = 1f;
const float POSITION_ACCURACY = 0.1f;
const float ANGLE_ACCURACY = 2.0f / 180f * MathHelper.Pi;
const float PRECISSION_POSITION_ACCURACY = 0.001f;
const float PRECISSION_ANGLE_ACCURACY = 0.01f / 180f * MathHelper.Pi;

//IMyShipController Cockpit;
IMyTextSurface LCD, ProgBlockLCD;
IMyMotorStator AzimuthRotor;
List<IMyPistonBase> VerticalPistons, HorizontalPistons;
List<IMyCargoContainer> Containers;
List<IMyShipDrill> Drills;

private string init_message;
private bool init_error = false;

private bool radius_direction = true, turn_direction = true;

private float angle_min = -MAX_ANGLE, angle_max = MAX_ANGLE, radius_min = 0f, radius_max, depth_min = 0f, depth_max;
private float rotor_spd, rad_spd, vert_spd;

private float DesiredRadius = 0f, DesiredDepth = 0f, DesiredAngle = 0f;
private float MAX_PISTONS_RADIUS, MAX_PISTONS_DEPTH;

const int CARGO_CHECK_INTERVAL = 6;
private int cargo_check_counter = 0;
private float cargo_volume, cargo_fullness, cargo_fullness_percentage;

private enum DrillProgrammEnum
{ 
    None,
    GotoHome,
    GotoBegin,
    Drill,
    Paused,
    Move,
    WaitFull
}
DrillProgrammEnum CurrentProgramm;
private enum ProgrammStateEnum
{
    None,
    MoveVertical,
    MoveHorizontal,
    Turning,
    TurningMoveHorisontal,
    MoveDown
}
ProgrammStateEnum ProgrammState;

//==========================================================================================================

//private const string DEBUG_LCD_NAME = "Control Seat(0)";
private const string DEBUG_LCD_NAME = "Дисплей debug";
private static IMyTextSurface DebugLCD;
private static void ToLog(string s, bool newLine = true, bool append = true)
{
    if (DebugLCD == null) return;

    if (newLine) s = "\n" + s;
    DebugLCD.WriteText(s, append);
}

//==========================================================================================================
int DisplayUpdateCounter = 0;
const int DisplayUpdateInterval = 1;
const double DisplayUpdateFreq = (1d/10d) / (double)DisplayUpdateInterval * 1000d;
double runTime = 0;

private bool initedFlag = false;
private int initTicksCounter = 0;
private int initTicksWait;

public Program()
{

    Random rnd = new Random();
    initTicksWait = 1;
    Runtime.UpdateFrequency = UpdateFrequency.Update1;

}
private bool ModuleInited()
{
    if (initedFlag) return true;
    if (initTicksCounter < initTicksWait) { initTicksCounter++; return false; }
    InitModule();
    initedFlag = true;

    return true;
}
private void InitModule()
{

    StringBuilder sb = new StringBuilder();

    DebugLCD = FindDisplay(DEBUG_LCD_NAME, GridTerminalSystem, Me);
    ToLog("", false, false);

    if (!string.IsNullOrEmpty(LCDName))
    {
        LCD = FindDisplay(LCDName, GridTerminalSystem, Me);
    }
    ProgBlockLCD = Me.GetSurface(0);
    
    if (LCD == null) sb.AppendLine("Дисплей не обнаружен");

    HorizontalPistons = FindAllBlocksOfType<IMyPistonBase>(GridTerminalSystem, Me, HORIZONTAL_PISTONS_TAG);
    VerticalPistons = FindAllBlocksOfType<IMyPistonBase>(GridTerminalSystem, Me, VERTICAL_PISTONS_TAG);
    AzimuthRotor = FindBlockOfType<IMyMotorStator>(null, GridTerminalSystem, Me, AZIMUTH_ROTOR_TAG);
    Drills = FindAllBlocksOfType<IMyShipDrill>(GridTerminalSystem, Me);
    Containers = FindAllBlocksOfType<IMyCargoContainer>(GridTerminalSystem, Me);

    MAX_PISTONS_RADIUS = 0f;
    HorizontalPistons.ForEach(b => MAX_PISTONS_RADIUS += b.HighestPosition);
    MAX_PISTONS_DEPTH = 0f;
    VerticalPistons.ForEach(b => MAX_PISTONS_DEPTH += b.HighestPosition);

    if (HorizontalPistons.Count == 0) {init_error = true; sb.AppendLine("Гор. поршни не обнаружены!");} else sb.AppendFormat("Гор. поршней: {0}\n", HorizontalPistons.Count);
    if (VerticalPistons.Count == 0) {init_error = true; sb.AppendLine("Верт. поршни не обнаружены!");} else sb.AppendFormat("Верт. поршней: {0}\n", VerticalPistons.Count);
    if (AzimuthRotor == null) {init_error = true; sb.AppendLine("Ротор не обнаружен!");} else sb.AppendLine("Ротор: ОК");
    if (Drills.Count == 0) {init_error = true; sb.AppendLine("Буры не обнаружены!");} else sb.AppendFormat("Буров: {0}\n", Drills.Count);
    if (Containers.Count == 0) {init_error = true; sb.AppendLine("Контейнеры не обнаружены!");} else sb.AppendFormat("Контейнеров: {0}\n", Containers.Count);

    init_message = sb.ToString();

    ClearPrgSettings();
    LoadState();

    Echo(init_message);

    Runtime.UpdateFrequency = UpdateFrequency.Update10;

    if (init_error)
    {
        Runtime.UpdateFrequency = UpdateFrequency.None;
        LCD.WriteText(init_message);
    }

}
public void Save()
{
    SaveState();
    StopMotion();
}

public void Main(string argument, UpdateType updateSource)
{

    if (!ModuleInited()) return;
    if (init_error)
    {
        Echo(init_message);
        ProgBlockLCD.WriteText(init_message);
        if (LCD != null)
        {
            LCD.WriteText(init_message);
        }
        return;
    }

    ToLog("", false, false);

    if (!string.IsNullOrEmpty(argument))
    {
        string cmd, cmd_param;
        string argument_norm = argument.Trim().ToLower();
        int spc_idx = argument_norm.IndexOf(" ");
        if (spc_idx == -1)
        {
            cmd = argument_norm;
            cmd_param = string.Empty;
        }
        else
        {
            cmd = argument_norm.Substring(0, spc_idx);
            cmd_param = argument_norm.Substring(spc_idx + 1).TrimStart();
        }

        switch (cmd)
        {
            case "on":
                {
                    HardwareOn();
                    break;
                }
            case "off":
                {
                    HardwareOff();
                    break;
                }
            case "begin":
                {
                    GotoStartPos();
                    break;
                }
            case "drill":
                {
                    StartDrill();
                    break;
                }
            case "home":
                {
                    GotoHome();
                    break;
                }
            case "pause":
                {
                    PauseWork();
                    break;
                }
            case "continue":
                {
                    ContinueWork();
                    break;
                }

            case "save":
                {
                    switch (cmd_param)
                    {
                        case "l":
                        case "left":
                            {
                                FullStop();
                                angle_min = CurrentAngle();
                                break;
                            }
                        case "r":
                        case "right":
                            {
                                FullStop();
                                angle_max = CurrentAngle();
                                break;
                            }
                        case "b":
                        case "back":
                            {
                                FullStop();
                                radius_min = CurrentRadius();
                                break;
                            }
                        case "f":
                        case "forward":
                            {
                                FullStop();
                                radius_max = CurrentRadius();
                                break;
                            }
                        case "u":
                        case "up":
                            {
                                FullStop();
                                depth_min = CurrentDepth();
                                break;
                            }
                        case "d":
                        case "down":
                            {
                                FullStop();
                                depth_max = CurrentDepth();
                                break;
                            }
                        default:
                            break;
                    }
                    break;
                }

            case "move":
                {
                    switch (cmd_param)
                    { 
                        case "l":
                        case "left":
                            {
                                FullStop();
                                MoveLeft();
                                break;
                            }
                        case "r":
                        case "right":
                            {
                                FullStop();
                                MoveRight();
                                break;
                            }
                        case "f":
                        case "forward":
                            {
                                FullStop();
                                MoveForward();
                                break;
                            }
                        case "b":
                        case "back":
                            {
                                FullStop();
                                MoveBack();
                                break;
                            }
                        case "u":
                        case "up":
                            {
                                FullStop();
                                MoveUp();
                                break;
                            }
                        case "d":
                        case "down":
                            {
                                FullStop();
                                MoveDown();
                                break;
                            }
                        default:
                            {
                                FullStop();
                                break;
                            }
                    }
                    break;
                }
            case "clear":
                FullStop();
                ClearPrgSettings();
                break;
            case "stop":
                FullStop();
                break;
            case "chdir":
                {
                    switch (cmd_param)
                    {
                        case "r":
                        case "radius":
                            {
                                radius_direction = !radius_direction;
                                break;
                            }
                        case "a":
                        case "angle":
                            {
                                turn_direction = !turn_direction;
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                    break;
                }

            default:
                break;
        }
        return;
    }

    if ((updateSource & UpdateType.Update10) == 0) return;

    //if (control_locked && Cockpit != null)
    //{
    //    if (Cockpit.MoveIndicator.X > 0f)
    //    {
    //        DesiredAngle = MAX_ANGLE;
    //        SetAngleSpeed(FREE_VELOCITY);
    //    }
    //    else if (Cockpit.MoveIndicator.X < 0f)
    //    {
    //        DesiredAngle = -MAX_ANGLE;
    //        SetAngleSpeed(FREE_VELOCITY);
    //    }
    //    else
    //    {
    //        SetAngleSpeed(0f);
    //    }
    //}

    cargo_check_counter += 1;
    if (cargo_check_counter >= CARGO_CHECK_INTERVAL)
    {
        cargo_check_counter = 0;
        CheckCargo();

        if (CurrentProgramm == DrillProgrammEnum.Drill)
        {
            if (cargo_fullness_percentage >= STOP_PERCENTAGE)
            {
                WaitCargoFull();
            }
        }
        else if (CurrentProgramm == DrillProgrammEnum.WaitFull)
        {
            if (cargo_fullness_percentage <= CONTINUE_PERCENTAGE)
            {
                ContinueWork();
            }
        
        }
    }

    if (CheckProgramm())
    {
        switch (CurrentProgramm)
        {
            case DrillProgrammEnum.Drill:
                {
                    GotoHome();
                    break;
                }
            case DrillProgrammEnum.GotoHome:
                {
                    HardwareOff();
                    break;
                }
            case DrillProgrammEnum.GotoBegin:
            case DrillProgrammEnum.Move:
                {
                    FullStop();
                    break;
                }
        }
    
    }

    ToLog(string.Format("Тек.верт {0:#0.00} / {1:#0.00}", CurrentDepth(), DesiredDepth));
    ToLog(string.Format("Отклонение {0:#0.00}", DepthDeviation()));
    ToLog(string.Format("Верт.скорость {0:#0.00}", vert_spd));
    ToLog(string.Format("Скорость поршня {0:#0.00}", VerticalPistons[0].Velocity));
    ToLog(string.Format("Поз. поршня {0:#0.00}", VerticalPistons[0].CurrentPosition));

    VerticalPistons.ForEach(b => ToLog(string.Format("Длина/скор {0:#0.00000} / {1:#0.00000}", b.CurrentPosition, b.Velocity)));
    ToLog(string.Format("Поз. ротора {0:#0.00000}", CurrentAngle()));

    DisplayUpdateCounter += 1;
    if (DisplayUpdateCounter >= DisplayUpdateInterval)
    {

        ProgBlockLCD.WriteText("Cycle time, µs: " + (runTime * DisplayUpdateFreq).ToString("#0.0")+"\n");
        Echo("Cycle time, µs: " + (runTime * DisplayUpdateFreq).ToString("#0.0") + "\n" + init_message);
        DisplayUpdateCounter = 0;
        runTime = 0d;

        ProgBlockLCD.WriteText("Программа: " + ProgramToString() + "\n", true);
        ProgBlockLCD.WriteText("Состояние: " + StateToString() + "\n", true);

        ProgBlockLCD.WriteText(string.Format("Инвентарь: {0:#0.0}%\n", cargo_fullness_percentage), true);

        ProgBlockLCD.WriteText("=== Задано ===\n", true);
        ProgBlockLCD.WriteText(string.Format("Радиус: {0:#0.0} / {1:#0.0}\n", radius_min, radius_max), true);
        ProgBlockLCD.WriteText(string.Format("Глубина: {0:#0.0} / {1:#0.0}\n", depth_min, depth_max), true);
        ProgBlockLCD.WriteText(string.Format("Угол: {0:#0} / {1:#0}\n", MathHelper.ToDegrees(angle_min), MathHelper.ToDegrees(angle_max)), true);
        ProgBlockLCD.WriteText(string.Format("Направление: {0} / {1}\n", (turn_direction ? "вправо" : "влево"), (radius_direction ? "вперёд" : "назад")), true);

        ProgBlockLCD.WriteText("=== Положение ===\n", true);
        ProgBlockLCD.WriteText(string.Format("Радиус: {0:#0.0} / {1:#0.0}\n", CurrentRadius(), DesiredRadius), true);
        ProgBlockLCD.WriteText(string.Format("Глубина: {0:#0.0} / {1:#0.0}\n", CurrentDepth(), DesiredDepth), true);
        ProgBlockLCD.WriteText(string.Format("Угол: {0:#0} / {1:#0}\n", MathHelper.ToDegrees(CurrentAngle()), MathHelper.ToDegrees(DesiredAngle)), true);

        if (LCD != null)
        {
            string t = ProgBlockLCD.GetText();
            LCD.WriteText(t);
        }
    }
    runTime += Runtime.LastRunTimeMs;
}

private void HardwareOff()
{
    VerticalPistons.ForEach(b =>
        {
            b.Velocity = 0f;
            b.SetValue<float>("MaxImpulseAxis", PISTON_MIN_FORCE);
            b.SetValue<float>("MaxImpulseNonAxis", PISTON_MIN_FORCE);
            b.Enabled = false;
        });
    
    HorizontalPistons.ForEach(b =>
    {
        b.Velocity = 0f;
        b.SetValue<float>("MaxImpulseAxis", PISTON_MIN_FORCE);
        b.SetValue<float>("MaxImpulseNonAxis", PISTON_MIN_FORCE);
        b.Enabled = false;
    });
    
    AzimuthRotor.TargetVelocityRad = 0f;
    AzimuthRotor.RotorLock = true;
    AzimuthRotor.Torque = 0f;
    AzimuthRotor.BrakingTorque = 0f;
    AzimuthRotor.Enabled = false;

    CurrentProgramm = DrillProgrammEnum.None;
    ProgrammState = ProgrammStateEnum.None;
}
private void HardwareOn()
{
    VerticalPistons.ForEach(b =>
    {
        b.Velocity = 0f;
        b.SetValue<float>("MaxImpulseAxis", PISTON_FORCE);
        b.SetValue<float>("MaxImpulseNonAxis", PISTON_FORCE);
        b.Enabled = true;
    });

    HorizontalPistons.ForEach(b =>
    {
        b.Velocity = 0f;
        b.SetValue<float>("MaxImpulseAxis", PISTON_FORCE);
        b.SetValue<float>("MaxImpulseNonAxis", PISTON_FORCE);
        b.Enabled = true;
    });

    AzimuthRotor.TargetVelocityRad = 0f;
    AzimuthRotor.RotorLock = false;
    AzimuthRotor.Torque = ROTOR_FORCE;
    AzimuthRotor.BrakingTorque = ROTOR_BRAKE_FORCE;
    AzimuthRotor.Enabled = true;
}

private void StopMotion()
{
    VerticalPistons.ForEach(b => b.Velocity = 0f);
    HorizontalPistons.ForEach(b => b.Velocity = 0f);
    AzimuthRotor.TargetVelocityRad = 0f;
}
private void FullStop()
{
    StopMotion();
    Drills.ForEach(b => b.Enabled = false);

    CurrentProgramm = DrillProgrammEnum.None;
    ProgrammState = ProgrammStateEnum.None;
}
private void ClearPrgSettings()
{
    radius_min = 0f;
    radius_max = MAX_PISTONS_RADIUS;
    depth_min = 0f;
    depth_max = MAX_PISTONS_DEPTH;
    angle_min = -MAX_ANGLE;
    angle_max = MAX_ANGLE;
}
private void GotoHome()
{
    DesiredRadius = 0f;
    DesiredDepth = 0f;
    DesiredAngle = 0f;

    Drills.ForEach(b => b.Enabled = false);

    SetDepthSpeed(FREE_VELOCITY);
    HorizontalPistons.ForEach(b => b.Velocity = 0f);
    AzimuthRotor.TargetVelocityRad = 0f;

    CurrentProgramm = DrillProgrammEnum.GotoHome;
    ProgrammState = ProgrammStateEnum.MoveVertical;
}
private void GotoStartPos()
{
    DesiredRadius = radius_min;
    DesiredDepth = 0f;  // depth_min;  через верхнюю точку, в конце опустим
    DesiredAngle = angle_min;

    Drills.ForEach(b => b.Enabled = false);

    SetDepthSpeed(FREE_VELOCITY);
    HorizontalPistons.ForEach(b => b.Velocity = 0f);
    AzimuthRotor.TargetVelocityRad = 0f;

    CurrentProgramm = DrillProgrammEnum.GotoBegin;
    ProgrammState = ProgrammStateEnum.MoveVertical;
}
private void StartDrill()
{

    DesiredDepth = CurrentDepth();
    DesiredRadius = CurrentRadius();

    if (turn_direction) DesiredAngle = angle_max; else DesiredAngle = angle_min;

    Drills.ForEach(b => b.Enabled = true);

    VerticalPistons.ForEach(b => b.Velocity = 0f);
    HorizontalPistons.ForEach(b => b.Velocity = 0f);

    SetAngleSpeed(DRILL_VELOCITY);

    CurrentProgramm = DrillProgrammEnum.Drill;
    ProgrammState = ProgrammStateEnum.Turning;
}
private void PauseWork()
{

    Drills.ForEach(b => b.Enabled = false);
    StopMotion();

    CurrentProgramm = DrillProgrammEnum.Paused;
}
private void WaitCargoFull()
{
    PauseWork();

    CurrentProgramm = DrillProgrammEnum.WaitFull;
}
private void ContinueWork()
{

    Drills.ForEach(b => b.Enabled = true);

    switch (ProgrammState)
    {
        case ProgrammStateEnum.Turning:
            {
                SetAngleSpeed(DRILL_VELOCITY);
                break;
            }
        case ProgrammStateEnum.MoveVertical:
            {
                SetDepthSpeed(DRILL_VELOCITY);
                break;
            }
        case ProgrammStateEnum.MoveHorizontal:
            {
                SetRadiusSpeed(DRILL_VELOCITY);
                break;
            }
    }

    CurrentProgramm = DrillProgrammEnum.Drill;
}
private void MoveLeft()
{
    DesiredAngle = -MAX_ANGLE;
    SetAngleSpeed(FREE_VELOCITY);

    CurrentProgramm = DrillProgrammEnum.Move;
    ProgrammState = ProgrammStateEnum.Turning;
}
private void MoveRight()
{
    DesiredAngle = MAX_ANGLE;
    SetAngleSpeed(FREE_VELOCITY);

    CurrentProgramm = DrillProgrammEnum.Move;
    ProgrammState = ProgrammStateEnum.Turning;
}
private void MoveForward()
{
    DesiredRadius = MAX_PISTONS_RADIUS;
    SetRadiusSpeed(FREE_VELOCITY);

    CurrentProgramm = DrillProgrammEnum.Move;
    ProgrammState = ProgrammStateEnum.MoveHorizontal;
}
private void MoveBack()
{
    DesiredRadius = 0;
    SetRadiusSpeed(FREE_VELOCITY);

    CurrentProgramm = DrillProgrammEnum.Move;
    ProgrammState = ProgrammStateEnum.MoveHorizontal;
}
private void MoveUp()
{
    DesiredDepth = 0f;
    SetDepthSpeed(FREE_VELOCITY);

    CurrentProgramm = DrillProgrammEnum.Move;
    ProgrammState = ProgrammStateEnum.MoveVertical;
}
private void MoveDown()
{
    DesiredDepth = MAX_PISTONS_DEPTH;
    SetDepthSpeed(FREE_VELOCITY);

    CurrentProgramm = DrillProgrammEnum.Move;
    ProgrammState = ProgrammStateEnum.MoveVertical;
}

private float CurrentRadius()
{
    //float R = MIN_RADIUS;
    float R = 0f;
    HorizontalPistons.ForEach(b => R += b.CurrentPosition);
    return R;
}
private float CurrentDepth()
{
    float H = 0f;
    VerticalPistons.ForEach(b => H += b.CurrentPosition);
    return H;
}
private float CurrentAngle()
{
    float curAngle = NormalizeAngle(NormalizeAngle(AzimuthRotor.Angle) - ZERO_ROTOR_ANGLE);
    return curAngle;
}
private float AngleDeviation()
{

    float curAngle = CurrentAngle();
    float delta = NormalizeAngle(DesiredAngle - curAngle);

    if (DesiredAngle == 0f)
    {
        if (Math.Abs(delta) < PRECISSION_ANGLE_ACCURACY) return 0f;
        return delta;
    }

    if (Math.Abs(delta) <= ANGLE_ACCURACY) return 0f;
    return delta;
}
private float RadiusDeviation()
{

    float des = DesiredRadius;
    if (des >= MAX_PISTONS_RADIUS) des = MAX_PISTONS_RADIUS;
    float curR = CurrentRadius();
    float delta = des - curR;

    if (DesiredRadius == 0f)
    {
        if (Math.Abs(delta) < PRECISSION_POSITION_ACCURACY) return 0f;
        return delta;
    }

    if (Math.Abs(delta) < POSITION_ACCURACY) return 0f;
    return delta;
}
private float DepthDeviation()
{

    float des = DesiredDepth;
    if (des >= MAX_PISTONS_DEPTH) des = MAX_PISTONS_DEPTH;
    float curD = CurrentDepth();
    float delta = des - curD;

    if (DesiredDepth == 0f)
    {
        if (Math.Abs(delta) < PRECISSION_POSITION_ACCURACY) return 0f;
        return delta;
    }

    if (Math.Abs(delta) < POSITION_ACCURACY) return 0f;
    return delta;
}

private void SetAngleSpeed(float linearSpeed)
{

    rotor_spd = linearSpeed;

    float R = CurrentRadius() + MIN_RADIUS;
    float vel = FadeSpeed(rotor_spd, R * AngleDeviation());
    AzimuthRotor.TargetVelocityRad = vel / R;

}
private void SetRadiusSpeed(float linearSpeed)
{
    rad_spd = linearSpeed;

    float vel = FadeSpeed(rad_spd, RadiusDeviation()) / HorizontalPistons.Count;
    HorizontalPistons.ForEach(b => b.Velocity = vel);
}
private void SetDepthSpeed(float linearSpeed)
{
    vert_spd = linearSpeed;

    float vel = FadeSpeed(vert_spd, DepthDeviation()) / VerticalPistons.Count;
    VerticalPistons.ForEach(b => b.Velocity = vel);
}

private bool CheckRadius(bool correctSpeed)
{
    float d = RadiusDeviation();
    if (d == 0f) return true;

    if (!correctSpeed) return false;

    float vel = FadeSpeed(rad_spd, d) / HorizontalPistons.Count;
    HorizontalPistons.ForEach(b => b.Velocity = vel);

    return false;
}
private bool CheckDepth(bool correctSpeed)
{
    float d = DepthDeviation();
    if (d == 0f) return true;

    if (!correctSpeed) return false;

    float vel = FadeSpeed(vert_spd, d) / VerticalPistons.Count;
    VerticalPistons.ForEach(b => b.Velocity = vel);

    return false;
}
private bool CheckAngle(bool correctSpeed)
{
    float d = AngleDeviation();
    if (d == 0f) return true;

    if (!correctSpeed) return false;

    float R = CurrentRadius() + MIN_RADIUS;
    float vel = FadeSpeed(rotor_spd, R * d);
    AzimuthRotor.TargetVelocityRad = vel / R;

    return false;
}

private float FadeSpeed(float linearSpeed, float deviation)
{

    float abs_dev = Math.Abs(deviation);
    //if (abs_dev < POSITION_ACCURACY) return 0f;

    float dev_sign = (float)Math.Sign(deviation);
    if (linearSpeed < 0.2) return linearSpeed * dev_sign;

    if (abs_dev >= FADE_SPEED_RANGE) return linearSpeed * dev_sign;

    float vel = (0.1f + (linearSpeed - 0.1f) * abs_dev / FADE_SPEED_RANGE) * dev_sign;
    return vel;
}

private void CheckCargo()
{
    cargo_volume = 0;
    cargo_fullness = 0;
    IMyInventory inv;
    Containers.ForEach(b =>
    {
        inv = b.GetInventory();
        cargo_volume += (float)inv.MaxVolume;
        cargo_fullness += (float)inv.CurrentVolume;
    });
    cargo_fullness_percentage = cargo_fullness / cargo_volume * 100f;
}

private bool CheckProgramm()
{

    switch (CurrentProgramm)
    {
        case DrillProgrammEnum.None:
            {
                return false;
            }
        case DrillProgrammEnum.GotoHome:
        case DrillProgrammEnum.GotoBegin:
            {
                if (CheckProgrammState())
                {
                    switch (ProgrammState)
                    {
                        case ProgrammStateEnum.MoveVertical:
                            {
                                ProgrammState = ProgrammStateEnum.TurningMoveHorisontal;
                                SetRadiusSpeed(FREE_VELOCITY);
                                SetAngleSpeed(FREE_VELOCITY);
                                return false;
                            }
                        case ProgrammStateEnum.TurningMoveHorisontal:
                            {
                                if (CurrentProgramm == DrillProgrammEnum.GotoHome)
                                {
                                    ProgrammState = ProgrammStateEnum.None;
                                    return true;
                                }
                                else     //CurrentProgramm == DrillProgrammEnum.GotoBegin
                                {
                                    ProgrammState = ProgrammStateEnum.MoveDown;
                                    DesiredDepth = depth_min;
                                    SetDepthSpeed(FREE_VELOCITY);
                                    return false;
                                }
                            }
                        case ProgrammStateEnum.MoveDown:
                            {
                                ProgrammState = ProgrammStateEnum.None;
                                return true;
                            }
                    }
                }
                else
                {
                    if (ProgrammState == ProgrammStateEnum.TurningMoveHorisontal)
                    {
                        SetAngleSpeed(FREE_VELOCITY);
                    }
                }
                return false;
            }
        case DrillProgrammEnum.Drill:
            {
                if (CheckProgrammState())
                {
                    switch (ProgrammState)
                    {
                        case ProgrammStateEnum.Turning:
                            {
                                //закончили поворот, сменим направление и пока остановим
                                //AzimuthRotor.TargetVelocityRad = 0f;
                                turn_direction = !turn_direction;
                                if (turn_direction) DesiredAngle = angle_max; else DesiredAngle = angle_min;

                                //сделаем шаг по радиусу
                                if (radius_direction)
                                {
                                    DesiredRadius += H_STEP;
                                    if (DesiredRadius > radius_max) DesiredRadius = radius_max;
                                }
                                else
                                {
                                    DesiredRadius -= H_STEP;
                                    if (DesiredRadius < radius_min) DesiredRadius = radius_min;
                                }

                                if (CheckRadius(false))
                                {
                                    //дошли до конца, сменим направление
                                    radius_direction = !radius_direction;
                                    //if (radius_direction) DesiredRadius = radius_max; else DesiredRadius = radius_min;

                                    //и заглубимся
                                    DesiredDepth += V_DIRECTION * V_STEP;
                                    if (DesiredDepth > depth_max) DesiredDepth = depth_max;

                                    if (CheckDepth(false))
                                    {
                                        //мы на максимальной глубине, конец программы
                                        //GotoHome();
                                        return true;
                                    }
                                    else
                                    {
                                        ProgrammState = ProgrammStateEnum.MoveVertical;
                                        SetDepthSpeed(DRILL_VELOCITY);
                                        return false;
                                    }
                                }
                                else
                                {
                                    ProgrammState = ProgrammStateEnum.MoveHorizontal;
                                    SetRadiusSpeed(DRILL_VELOCITY);
                                    return false;
                                }
                            }
                        case ProgrammStateEnum.MoveVertical:
                            {
                                ProgrammState = ProgrammStateEnum.Turning;
                                SetAngleSpeed(DRILL_VELOCITY);
                                return false;
                            }
                        case ProgrammStateEnum.MoveHorizontal:
                            {
                                ProgrammState = ProgrammStateEnum.Turning;
                                SetAngleSpeed(DRILL_VELOCITY);
                                return false;
                            }
                    }
                }
                return false;
            }
        case DrillProgrammEnum.Move:
            {
                if (CheckProgrammState())
                {
                    switch (ProgrammState)
                    {
                        case ProgrammStateEnum.Turning:
                        case ProgrammStateEnum.MoveVertical:
                        case ProgrammStateEnum.MoveHorizontal:
                        case ProgrammStateEnum.MoveDown:
                            return true;
                        default:
                            break;
                    }
                }
                return false;
            }
        default:
            break;
    }
    return false;

}
private bool CheckProgrammState()
{

    switch (ProgrammState)
    {
        case ProgrammStateEnum.None:
            {
                return false;
            }
        case ProgrammStateEnum.MoveVertical:
        case ProgrammStateEnum.MoveDown:
            {
                if (CheckDepth(true))
                {
                    VerticalPistons.ForEach(b => b.Velocity = 0f);
                    return true;
                }
                break;
            }
        case ProgrammStateEnum.MoveHorizontal:
            {
                if (CheckRadius(true))
                {
                    HorizontalPistons.ForEach(b => b.Velocity = 0f);
                    return true;
                }
                break;
            }
        case ProgrammStateEnum.TurningMoveHorisontal:
            {
                bool flag = true;
                if (CheckRadius(true))
                {
                    HorizontalPistons.ForEach(b => b.Velocity = 0f);
                }
                else
                {
                    flag = false;
                }
                if (CheckAngle(true))
                {
                    AzimuthRotor.TargetVelocityRad = 0f;
                }
                else
                {
                    flag = false;
                }

                return flag;
            }
        case ProgrammStateEnum.Turning:
            {
                if (CheckAngle(true))
                {
                    AzimuthRotor.TargetVelocityRad = 0f;
                    return true;
                }
                break;
            }
        default:
            break;
    }
    return false;
}

private string ProgramToString()
{
    switch (CurrentProgramm)
    { 
        case DrillProgrammEnum.None:
            return "нет";
        case DrillProgrammEnum.GotoHome:
            return "парковка";
        case DrillProgrammEnum.GotoBegin:
            return "в начало";
        case DrillProgrammEnum.Drill:
            return "работа";
        case DrillProgrammEnum.Paused:
            return "пауза";
        case DrillProgrammEnum.Move:
            return "движение";
        case DrillProgrammEnum.WaitFull:
            return "контейнер полный";
        default:
            return CurrentProgramm.ToString();
    }
}
private string StateToString()
{
    switch (ProgrammState)
    {
        case ProgrammStateEnum.None:
            return "нет";
        case ProgrammStateEnum.MoveVertical:
            return "по вертикали";
        case ProgrammStateEnum.MoveHorizontal:
            return "по горизонтали";
        case ProgrammStateEnum.Turning:
            return "поворот";
        case ProgrammStateEnum.TurningMoveHorisontal:
            return "поворот в движении";
        default:
            return ProgrammState.ToString();
    }
}

private void SaveState()
{
    StringBuilder sb = new StringBuilder();
    //0 - версия
    sb.AppendLine("0");

    //1-2 - программа
    sb.AppendLine(((int)CurrentProgramm).ToString());
    sb.AppendLine(((int)ProgrammState).ToString());

    //3-8 - параметры программы
    sb.AppendLine(angle_min.ToString());
    sb.AppendLine(angle_max.ToString());
    sb.AppendLine(radius_min.ToString());
    sb.AppendLine(radius_max.ToString());
    sb.AppendLine(depth_min.ToString());
    sb.AppendLine(depth_max.ToString());

    //9-11 - текущие целевые значения
    sb.AppendLine(DesiredAngle.ToString());
    sb.AppendLine(DesiredRadius.ToString());
    sb.AppendLine(DesiredDepth.ToString());

    //12-14 - последние заданные скорости
    sb.AppendLine(rotor_spd.ToString());
    sb.AppendLine(rad_spd.ToString());
    sb.AppendLine(vert_spd.ToString());

    //15-16 - направление
    sb.AppendLine(radius_direction.ToString());
    sb.AppendLine(turn_direction.ToString());

    Storage = sb.ToString();
}
private bool LoadState()
{

    string[] strs = Storage.Split('\n');
    if (strs.Length < 14) return false;

    //0 - версия
    int saveFormatVersion = Int32.Parse(strs[0]);

    //1-2 - программа
    CurrentProgramm = (DrillProgrammEnum)Int32.Parse(strs[1]);
    ProgrammState = (ProgrammStateEnum)Int32.Parse(strs[2]);

    //3-8 - параметры программы
    angle_min = float.Parse(strs[3]);
    angle_max = float.Parse(strs[4]);
    radius_min = float.Parse(strs[5]);
    radius_max = float.Parse(strs[6]);
    depth_min = float.Parse(strs[7]);
    depth_max = float.Parse(strs[8]);

    //9-11 - текущие целевые значения
    DesiredAngle = float.Parse(strs[9]);
    DesiredRadius = float.Parse(strs[10]);
    DesiredDepth = float.Parse(strs[11]);

    //12-14 - последние заданные скорости
    rotor_spd = float.Parse(strs[12]);
    rad_spd = float.Parse(strs[13]);
    vert_spd = float.Parse(strs[14]);

    //15-16 - направление
    radius_direction = bool.Parse(strs[15]);
    turn_direction = bool.Parse(strs[16]);

    return true;
}

#region Helpers

private static T FindBlockOfType<T>(string CustomName, IMyGridTerminalSystem gts, IMyTerminalBlock anyBlock, string Tag = null) where T : class
{
    List<T> Blocks = new List<T>();
    IMyTerminalBlock block;
    bool flag = false;
    try
    {
        gts.GetBlocksOfType<T>(Blocks, b =>
        {
            if (flag) throw new Exception();
            
            block = (IMyTerminalBlock)b;
            if (!block.IsSameConstructAs(anyBlock)) return false;
            if (CustomName == null)
            {
                if (block.CustomName.Contains(Tag))
                {
                    flag = true;
                    return true;
                }
            }
            else
            {
                if (block.CustomName == CustomName) 
                {
                    flag = true;
                    return true;
                }
            }
            return false;
        });
    }    
    catch {}

    if (Blocks.Count == 0) return null;
    return Blocks[0];
}
private static IMyTextSurface FindDisplay(string DisplayName, IMyGridTerminalSystem gts, IMyTerminalBlock anyBlock)
{
    IMyTextSurface TextSurface = null;

    int x = DisplayName.IndexOf('(');
    if (x == -1)
    {
        TextSurface = FindBlockOfType<IMyTextSurface>(DisplayName, gts, anyBlock);
    }
    else
    {
        string mName = DisplayName.Substring(0, x);
        IMyTextSurfaceProvider sp = FindBlockOfType<IMyTextSurfaceProvider>(mName, gts, anyBlock);
        if (sp != null)
        {
            int y = DisplayName.IndexOf(')');
            string indexStr = DisplayName.Substring(x + 1, y - x - 1).Trim();
            int surfIndex = int.Parse(indexStr);
            TextSurface = sp.GetSurface(surfIndex);
        }
    }

    return TextSurface;
}
private static IMyShipController FindDefaultCockpit(IMyGridTerminalSystem gts, IMyTerminalBlock anyBlock)
{
    List<IMyShipController> allBlocks = new List<IMyShipController>();
    gts.GetBlocksOfType<IMyShipController>(allBlocks, block => block.IsSameConstructAs(anyBlock) && block.CanControlShip);

    if (allBlocks.Count == 0) return null;

    IMyShipController findedCtrl = allBlocks[0];
    foreach (IMyShipController ctrl in allBlocks)
    {
        if (ctrl.IsUnderControl) return ctrl;
        if (ctrl.IsMainCockpit) findedCtrl = ctrl;
    }
    return findedCtrl;
}
private static List<T> FindAllBlocksOfType<T>(IMyGridTerminalSystem gts, IMyTerminalBlock anyBlock, string NamePart = null) where T : class
{
    List<T> allBlocksOfT = new List<T>();
    IMyTerminalBlock b;
    gts.GetBlocksOfType<T>(allBlocksOfT, block => 
    {
        if (!(block is IMyTerminalBlock)) return false;
        b = (IMyTerminalBlock)block;
        if (!b.IsSameConstructAs(anyBlock)) return false;
        if (NamePart == null) return true;
        return b.CustomName.Contains(NamePart);
    });
    return allBlocksOfT;
}

private static float NormalizeAngle(float SourseAngle)
{
    float angle = SourseAngle;

    if (angle >= MathHelper.Pi) angle -= MathHelper.TwoPi;
    if (angle <= -MathHelper.Pi) angle += MathHelper.TwoPi;

    return angle;
}

#endregion


//=== до сюда ===>>>===>>>
}}
