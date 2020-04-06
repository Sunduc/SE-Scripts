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

namespace HelicopterThrust{
public sealed class Program : MyGridProgram{
    //=== от сюда ===>>>===>>>
    // код andrukha74#3658 (diskord)
    private const string CockpitName = "Кокпит";
    private const double GyroFactor = 3;
    private const double MaxAngularAcceleration = 0.3;
    private const double MaxSpeed = 99.99;
    private const double MaxDeviationAngle = 60.0 * (Math.PI / 180.0);
    private const double MaxAccelerationECO = 3.0;
    private const double MaxAccelerationDRIVE = 8.0;
    private const double MaxAccelerationSPORT = 20.0;
    private const float  PitchYawSensitivityFactor = 1.0f / 30f;

    const UpdateFrequency Frequency = UpdateFrequency.Update1;

    //==============================================================================

    List<IMyThrust> DownTrusters = new List<IMyThrust>();
    List<GyroDefinition> Gyros = new List<GyroDefinition>();
    IMyShipController Cockpit;

    //private const double ProportionalAccSpeedInv = 1.0 / ProportionalAccSpeed;
    
    private bool EngineIsON = false; 
    private int EngineMode = 0;
    private double CurrentRegimeAccel = MaxAccelerationSPORT;

    private static Vector2D MaxDeviationAngleMax = new Vector2D(MaxDeviationAngle, MaxDeviationAngle);
    private static Vector2D MaxDeviationAngleMin = Vector2D.Negate(MaxDeviationAngleMax);

    private static Vector3D MaxSpeedMax = new Vector3D(MaxSpeed, MaxSpeed, MaxSpeed);
    private static Vector3D MaxSpeedMin = Vector3D.Negate(MaxSpeedMax);

    private MatrixD PlanetMatrix, PlanetMatrixInv;
    private bool PlanetMatrixReady = false;

    private bool InertiaDampeners;

    private float GyroPitch = 0f;
    private float GyroRoll = 0f;
    private float GyroYaw = 0f;

    private Vector3D DesiredSpeed = Vector3D.Zero;
    private const double DeltaSpeed = 0.2;
    private bool CruiseControl = true;
    private bool AltitudeHoldMode = true;
    private double DesiredAltitude = 0;
    //private const double DesiredAltitudeDelta = 0;

    private const float TimeQuant = 1f / 60f;

    private PIDСontroller GyroPithPID = new PIDСontroller(1, 0.0, 0.03);
    private PIDСontroller GyroRollPID = new PIDСontroller(1, 0.0, 0.03);

    private bool initedFlag = false;
    private int initTicksCounter = 0;
    private int initTicksWait;

    private const string DebugLCDName = "Дисплей debug";
    private static IMyTextPanel DebugLCD;

    //==========================================================================================================

    public Program()
    {

        Random rnd = new Random();
        initTicksWait = 12 + rnd.Next(6);
        Runtime.UpdateFrequency = UpdateFrequency.Update10;

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


        Cockpit = GridTerminalSystem.GetBlockWithName(CockpitName) as IMyShipController;

        GridTerminalSystem.GetBlocksOfType<IMyThrust>(DownTrusters);

        List<IMyGyro> AllGyros = new List<IMyGyro>();
        GridTerminalSystem.GetBlocksOfType<IMyGyro>(AllGyros);
        SetupGyros(Cockpit, AllGyros, Gyros);

        DebugLCD = GridTerminalSystem.GetBlockWithName(DebugLCDName) as IMyTextPanel;

        InertiaDampeners = Cockpit.DampenersOverride;
        Cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out DesiredAltitude);

        SetEngineMode(0);

        Runtime.UpdateFrequency = UpdateFrequency.Update10;

    }
    private static void ToLog(string s, bool append = false)
    {
        if (DebugLCD == null) return;

        DebugLCD.WriteText(s, append);
    }


    public void Main(string argument, UpdateType updateSource)
    {

        if (!ModuleInited()) return;

        //проверим аргументы
        if (!string.IsNullOrEmpty(argument))
        {
            switch (argument)
            {
                case "power":
                    {
                        if (EngineIsON)
                        {
                            //выключаем движок
                            SetEngineMode(0);
                        }
                        else
                        {
                            //включаем движок
                            SetEngineMode(1);
                        }
                        break;
                    }

                case "power-":
                    {
                        SetEngineMode(EngineMode - 1);
                        break;
                    }
                case "power+":
                    {
                        SetEngineMode(EngineMode + 1);
                        break;
                    }
                case "stop":
                    DesiredSpeed.Z = 0;
                    break;
                case "max":
                    DesiredSpeed.Z = -MaxSpeed;
                    break;
                case "cruisecontrol":
                    CruiseControl = !CruiseControl;
                    break;
                case "altitudehold":
                    AltitudeHoldMode = !AltitudeHoldMode;
                    if (AltitudeHoldMode)
                    {
                        Cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out DesiredAltitude);
                    }
                    break;
                default: break;
            }
        }

        ToLog("Engine mode: " + EngineMode.ToString());

        InertiaDampeners = Cockpit.DampenersOverride;
        TestRotationInput();
        TestMotionInput();
    }


    private static string VectToStr(Vector3D v, string formatString = "#0.000", string delimiter = ", ")
    {
        //"{0:#0.000}, {1:#0.000}, {2:#0.000}" 
        string fstr = "{0:" + formatString + "}" + delimiter + "{1:" + formatString + "}" + delimiter + "{2:" + formatString + "}";
        string s = string.Format(fstr, v.X, v.Y, v.Z);
        return s;
    }
    private static string Vect2ToStr(Vector2D v, string formatString = "#0.000", string delimiter = ", ")
    {
        //"{0:#0.000}, {1:#0.000}, {2:#0.000}" 
        string fstr = "{0:" + formatString + "}" + delimiter + "{1:" + formatString + "}";
        string s = string.Format(fstr, v.X, v.Y);
        return s;

    }

    private static Vector2D NormAngles2D(Vector2D sourseAngles)
    {
        Vector2D angles = sourseAngles;
        if (angles.X >= Math.PI) angles.X -= MathHelperD.TwoPi;
        if (angles.X <= -Math.PI) angles.X += MathHelperD.TwoPi;
        if (angles.Y >= Math.PI) angles.Y -= MathHelperD.TwoPi;
        if (angles.Y <= -Math.PI) angles.Y += MathHelperD.TwoPi;

        return angles;
    }
    private static Vector2D GetDirectionAngles(Vector3D Direction)
    {
        Vector3D normDir = Vector3D.Normalize(Direction);

        double angleAz = Math.Asin(normDir.X);
        double angleEl = Math.Asin(normDir.Y);

        if (normDir.Z > 0)
        {
            //направление в заднюю полусферу
            if (angleAz < 0)
            {
                angleAz = -Math.PI - angleAz;
            }
            else
            {
                angleAz = Math.PI - angleAz;
            }

            if (angleEl < 0)
            {
                angleEl = -Math.PI - angleEl;
            }
            else
            {
                angleEl = Math.PI - angleEl;
            }

        }

        return new Vector2D(angleAz, angleEl);
    }

    private class PDСontroller
    {

        public Double factorP = 1.0;
        public Double factorD = 1.0;
        public Double generalFactor = 1.0;
        public const Double frequency = 60;
        public const Double timeFactor = 1.0 / frequency;

        private double prevErr = 0;

        public double channelP = 0;
        public double channelD = 0;

        public PDСontroller()
        {
        }
        public PDСontroller(Double factG)
        {
            generalFactor = factG;
        }
        public PDСontroller(Double factP, Double factD)
        {
            factorP = factP;
            factorD = factD;
        }
        public PDСontroller(Double factG, Double factP, Double factD)
        {
            generalFactor = factG;
            factorP = factP;
            factorD = factD;
        }

        public double Reaction(double err)
        {

            channelP = factorP * err;

            double dErr = err - prevErr;
            prevErr = err;
            //D = dErr/dT = dErr * freq
            channelD = factorD * dErr * frequency;

            double sumVal = channelP + channelD;
            return generalFactor * sumVal;
        }
        public void Reset()
        {

            channelP = 0;

            prevErr = 0;
            channelD = 0;
        }

    }
    private class PIDСontroller
    {

        private const int integralDeep = 10;
        private const double integralDeepFactor = 1.0 / integralDeep;
        private const double integralDecreaseFactor = 1.0 - integralDeepFactor;

        public Double factorP = 1.0;
        public Double factorI = 1.0;
        public Double factorD = 1.0;
        public Double generalFactor = 1.0;
        public const Double frequency = 60;
        public const Double timeFactor = 1.0 / frequency;

        private double iSum = 0;
        private double prevErr = 0;

        public double channelP = 0;
        public double channelI = 0;
        public double channelD = 0;

        public PIDСontroller()
        {
        }
        public PIDСontroller(Double factG)
        {
            generalFactor = factG;
        }
        public PIDСontroller(Double factP, Double factI, Double factD)
        {
            factorP = factP;
            factorI = factI;
            factorD = factD;
        }
        public PIDСontroller(Double factG, Double factP, Double factI, Double factD)
        {
            generalFactor = factG;
            factorP = factP;
            factorI = factI;
            factorD = factD;
        }

        public double Reaction(double err)
        {

            channelP = factorP * err;
            //затухание ошибки
            iSum *= integralDecreaseFactor;
            //S1 = S + Err*dT
            iSum += err * timeFactor;
            channelI = factorI * iSum;

            double dErr = err - prevErr;
            prevErr = err;
            //D = dErr/dT = dErr * freq
            channelD = factorD * dErr * frequency;

            double sumVal = channelP + channelI + channelD;
            return generalFactor * sumVal;
        }
        public void Reset()
        {

            channelP = 0;

            iSum = 0;
            channelI = 0;

            prevErr = 0;
            channelD = 0;
        }

    }

    private enum GyroRotations
    {
        Pitch,
        Yaw,
        Roll
    }
    private class GyroDefinition
    {
        public IMyGyro Gyro;
        public float PitchFactor = 1f;
        public float YawFactor = 1f;
        public float RollFactor = 1f;

        public GyroRotations Pitch = GyroRotations.Pitch;
        public GyroRotations Yaw = GyroRotations.Yaw;
        public GyroRotations Roll = GyroRotations.Roll;

        public GyroDefinition(IMyGyro mGyro)
        {
            Gyro = mGyro;
        }
    }
    private void SetupGyros(IMyShipController OrientationCockpit, List<IMyGyro> GyrosList, List<GyroDefinition> GyroDefsList)
    {

        Matrix cockpitMatrix, orientMatrix;
        OrientationCockpit.Orientation.GetMatrix(out cockpitMatrix);

        GyroDefinition GyroDef;
        foreach (IMyGyro gyro in GyrosList)
        {
            gyro.Orientation.GetMatrix(out orientMatrix);
            GyroDef = new GyroDefinition(gyro);

            if (orientMatrix.Forward == cockpitMatrix.Forward)
            {
                GyroDef.Roll = GyroRotations.Roll;
                GyroDef.RollFactor = 1f;

                if (orientMatrix.Up == cockpitMatrix.Up)
                {
                    GyroDef.Pitch = GyroRotations.Pitch;
                    GyroDef.PitchFactor = 1f;

                    GyroDef.Yaw = GyroRotations.Yaw;
                    GyroDef.YawFactor = 1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Right)
                {
                    GyroDef.Pitch = GyroRotations.Yaw;
                    GyroDef.PitchFactor = 1f;

                    GyroDef.Yaw = GyroRotations.Pitch;
                    GyroDef.YawFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Down)
                {
                    GyroDef.Pitch = GyroRotations.Pitch;
                    GyroDef.PitchFactor = -1f;

                    GyroDef.Yaw = GyroRotations.Yaw;
                    GyroDef.YawFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Left)
                {
                    GyroDef.Pitch = GyroRotations.Yaw;
                    GyroDef.PitchFactor = -1f;

                    GyroDef.Yaw = GyroRotations.Pitch;
                    GyroDef.YawFactor = 1f;
                }
            }
            else if (orientMatrix.Forward == cockpitMatrix.Down)
            {
                GyroDef.Yaw = GyroRotations.Roll;
                GyroDef.YawFactor = 1f;

                if (orientMatrix.Up == cockpitMatrix.Forward)
                {
                    GyroDef.Pitch = GyroRotations.Pitch;
                    GyroDef.PitchFactor = 1f;

                    GyroDef.Roll = GyroRotations.Yaw;
                    GyroDef.RollFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Right)
                {
                    GyroDef.Pitch = GyroRotations.Yaw;
                    GyroDef.PitchFactor = 1f;

                    GyroDef.Roll = GyroRotations.Pitch;
                    GyroDef.RollFactor = 1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Backward)
                {
                    GyroDef.Pitch = GyroRotations.Pitch;
                    GyroDef.PitchFactor = -1f;

                    GyroDef.Roll = GyroRotations.Yaw;
                    GyroDef.RollFactor = 1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Left)
                {
                    GyroDef.Pitch = GyroRotations.Yaw;
                    GyroDef.PitchFactor = -1f;

                    GyroDef.Roll = GyroRotations.Pitch;
                    GyroDef.RollFactor = -1f;
                }
            }
            else if (orientMatrix.Forward == cockpitMatrix.Backward)
            {
                GyroDef.Roll = GyroRotations.Roll;
                GyroDef.RollFactor = -1f;

                if (orientMatrix.Up == cockpitMatrix.Up)
                {
                    GyroDef.Pitch = GyroRotations.Pitch;
                    GyroDef.PitchFactor = -1f;

                    GyroDef.Yaw = GyroRotations.Yaw;
                    GyroDef.YawFactor = 1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Right)
                {
                    GyroDef.Pitch = GyroRotations.Yaw;
                    GyroDef.PitchFactor = 1f;

                    GyroDef.Yaw = GyroRotations.Pitch;
                    GyroDef.YawFactor = 1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Down)
                {
                    GyroDef.Pitch = GyroRotations.Pitch;
                    GyroDef.PitchFactor = 1f;

                    GyroDef.Yaw = GyroRotations.Yaw;
                    GyroDef.YawFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Left)
                {
                    GyroDef.Pitch = GyroRotations.Yaw;
                    GyroDef.PitchFactor = -1f;

                    GyroDef.Yaw = GyroRotations.Pitch;
                    GyroDef.YawFactor = -1f;
                }
            }
            else if (orientMatrix.Forward == cockpitMatrix.Up)
            {
                GyroDef.Yaw = GyroRotations.Roll;
                GyroDef.YawFactor = -1f;

                if (orientMatrix.Up == cockpitMatrix.Forward)
                {
                    GyroDef.Pitch = GyroRotations.Pitch;
                    GyroDef.PitchFactor = -1f;

                    GyroDef.Roll = GyroRotations.Yaw;
                    GyroDef.RollFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Right)
                {
                    GyroDef.Pitch = GyroRotations.Yaw;
                    GyroDef.PitchFactor = 1f;

                    GyroDef.Roll = GyroRotations.Pitch;
                    GyroDef.RollFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Backward)
                {
                    GyroDef.Pitch = GyroRotations.Pitch;
                    GyroDef.PitchFactor = 1f;

                    GyroDef.Roll = GyroRotations.Yaw;
                    GyroDef.RollFactor = 1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Left)
                {
                    GyroDef.Pitch = GyroRotations.Yaw;
                    GyroDef.PitchFactor = -1f;

                    GyroDef.Roll = GyroRotations.Pitch;
                    GyroDef.RollFactor = 1f;
                }
            }
            else if (orientMatrix.Forward == cockpitMatrix.Right)
            {
                GyroDef.Pitch = GyroRotations.Roll;
                GyroDef.PitchFactor = -1f;

                if (orientMatrix.Up == cockpitMatrix.Up)
                {
                    GyroDef.Yaw = GyroRotations.Yaw;
                    GyroDef.YawFactor = 1f;

                    GyroDef.Roll = GyroRotations.Pitch;
                    GyroDef.RollFactor = 1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Forward)
                {
                    GyroDef.Yaw = GyroRotations.Pitch;
                    GyroDef.YawFactor = 1f;

                    GyroDef.Roll = GyroRotations.Yaw;
                    GyroDef.RollFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Down)
                {
                    GyroDef.Yaw = GyroRotations.Yaw;
                    GyroDef.YawFactor = -1f;

                    GyroDef.Roll = GyroRotations.Pitch;
                    GyroDef.RollFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Backward)
                {
                    GyroDef.Yaw = GyroRotations.Pitch;
                    GyroDef.YawFactor = -1f;

                    GyroDef.Roll = GyroRotations.Yaw;
                    GyroDef.RollFactor = 1f;
                }
            }
            else if (orientMatrix.Forward == cockpitMatrix.Left)
            {
                GyroDef.Pitch = GyroRotations.Roll;
                GyroDef.PitchFactor = 1f;

                if (orientMatrix.Up == cockpitMatrix.Up)
                {
                    GyroDef.Yaw = GyroRotations.Yaw;
                    GyroDef.YawFactor = 1f;

                    GyroDef.Roll = GyroRotations.Pitch;
                    GyroDef.RollFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Forward)
                {
                    GyroDef.Yaw = GyroRotations.Pitch;
                    GyroDef.YawFactor = -1f;

                    GyroDef.Roll = GyroRotations.Yaw;
                    GyroDef.RollFactor = -1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Down)
                {
                    GyroDef.Yaw = GyroRotations.Yaw;
                    GyroDef.YawFactor = -1f;

                    GyroDef.Roll = GyroRotations.Pitch;
                    GyroDef.RollFactor = 1f;
                }
                else if (orientMatrix.Up == cockpitMatrix.Backward)
                {
                    GyroDef.Yaw = GyroRotations.Pitch;
                    GyroDef.YawFactor = 1f;

                    GyroDef.Roll = GyroRotations.Yaw;
                    GyroDef.RollFactor = 1f;
                }
            }

            GyroDefsList.Add(GyroDef);
        }
    }
    private void SetGyroParams(GyroDefinition gyroDef, bool GyroOverride = false, float pitchValue = 0f, float yawValue = 0f, float rollValue = 0f)
    {
        IMyGyro gyro = gyroDef.Gyro;
        gyro.GyroOverride = GyroOverride;
        switch (gyroDef.Pitch)
        {
            case GyroRotations.Pitch:
                gyro.Pitch = pitchValue * gyroDef.PitchFactor;
                break;
            case GyroRotations.Yaw:
                gyro.Yaw = pitchValue * gyroDef.PitchFactor;
                break;
            default:    //roll
                gyro.Roll = pitchValue * gyroDef.PitchFactor;
                break;
        }
        switch (gyroDef.Yaw)
        {
            case GyroRotations.Pitch:
                gyro.Pitch = yawValue * gyroDef.YawFactor;
                break;
            case GyroRotations.Yaw:
                gyro.Yaw = yawValue * gyroDef.YawFactor;
                break;
            default:    //roll
                gyro.Roll = yawValue * gyroDef.YawFactor;
                break;
        }
        switch (gyroDef.Roll)
        {
            case GyroRotations.Pitch:
                gyro.Pitch = rollValue * gyroDef.RollFactor;
                break;
            case GyroRotations.Yaw:
                gyro.Yaw = rollValue * gyroDef.RollFactor;
                break;
            default:    //roll
                gyro.Roll = rollValue * gyroDef.RollFactor;
                break;
        }
    }

    private void SetEngineMode(int mode)
    {

        EngineMode = MathHelper.Clamp(mode, 0, 3);

        if (EngineMode == 0)
        {
            //выключаем движок, гироскопы освобождаем

            EngineIsON = false;
            foreach (IMyThrust t in DownTrusters)
            {
                t.Enabled = false;
                t.ThrustOverride = 0f;
            }
            foreach (GyroDefinition g in Gyros)
            {
                SetGyroParams(g);
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        else
        {
            //включаем движок
            if (!EngineIsON)
            {
                GyroPithPID.Reset();
                GyroRollPID.Reset();

                EngineIsON = true;
                foreach (IMyThrust t in DownTrusters)
                {
                    t.Enabled = true;
                }
                foreach (GyroDefinition g in Gyros)
                {
                    SetGyroParams(g, true);
                }
            }
            switch (EngineMode)
            { 
                case 1:
                    CurrentRegimeAccel = MaxAccelerationECO;
                    break;
                case 2:
                    CurrentRegimeAccel = MaxAccelerationDRIVE;
                    break;
                case 3:
                    CurrentRegimeAccel = MaxAccelerationSPORT;
                    break;
            }
            Runtime.UpdateFrequency = Frequency;
        }

    }

    private void TestMotionInput()
    {

        if (!EngineIsON) return;
        
        Vector3D moveInput = Cockpit.MoveIndicator;
        //ToLog("\nmoveInput: " + VectToStr(moveInput), true);

        InertiaDampeners = Cockpit.DampenersOverride;
        //ToLog("\nDampeners: " + InertiaDampeners.ToString(), true);

        Vector3D WorldSpeed = Cockpit.GetShipVelocities().LinearVelocity;
        Vector3D WorldAngVel = Cockpit.GetShipVelocities().AngularVelocity;

        Vector3D Gravity = Cockpit.GetNaturalGravity();
        Vector3D normVertical = -Vector3D.Normalize(Gravity);

        //матрица вращения в СО, связанную с вектором гравитации
        if (!PlanetMatrixReady)
        {
            Vector3D normForward = normVertical.Cross(Cockpit.WorldMatrix.Right);
            PlanetMatrix = MatrixD.CreateWorld(Vector3D.Zero, normForward, normVertical);
            PlanetMatrixInv = MatrixD.Invert(PlanetMatrix);
            PlanetMatrixReady = true;
        }

        Vector3D PlanetSpeed = Vector3D.Rotate(WorldSpeed, PlanetMatrixInv);
        //ToLog("\nSpeed 3D: " + VectToStr(PlanetSpeed), true);
        Vector3D PlanetAngularVelocity = Vector3D.Rotate(WorldAngVel, PlanetMatrixInv);

        //test!!!
        //Vector3D vForw = Vector3D.Rotate(Cockpit.WorldMatrix.Forward, PlanetMatrixInv);
        //ToLog("\nForw. test: " + VectToStr(vForw), true);

        Vector3D moveInputSign = Vector3D.Sign(moveInput);
        double curAltitude, deltaAlt = 0;
        Cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out curAltitude);
        if (InertiaDampeners)
        {
            DesiredSpeed.X = MaxSpeed * moveInputSign.X;
            if (AltitudeHoldMode)
            {
                deltaAlt = Math.Abs(DesiredAltitude) * 0.01;
                if (deltaAlt < 0.1) deltaAlt = 0.1;
                if (moveInputSign.Y != 0.0)
                {
                    DesiredAltitude += deltaAlt * moveInputSign.Y;
                    if (DesiredAltitude < 0.0) DesiredAltitude = 0;
                }
                deltaAlt = DesiredAltitude - curAltitude;
                //ускорение потом вычислим, желаемая скорость для этого не нужна
            }
            DesiredSpeed.Y = MaxSpeed * moveInputSign.Y;

            if (CruiseControl) DesiredSpeed.Z += DeltaSpeed * moveInputSign.Z; else DesiredSpeed.Z = MaxSpeed * moveInputSign.Z;

            //DesiredSpeed = Vector3D.Sign(moveInput) * MaxSpeed;
        }
        else
        {
            //DesiredSpeed = PlanetSpeed;
            if (moveInput.X == 0d) DesiredSpeed.X = PlanetSpeed.X; else DesiredSpeed.X = MaxSpeed * moveInputSign.X;
            if (moveInput.Y == 0d) DesiredSpeed.Y = PlanetSpeed.Y; else DesiredSpeed.Y = MaxSpeed * moveInputSign.Y;
            if (moveInput.Z == 0d) DesiredSpeed.Z = PlanetSpeed.Z; else
            {
                if (CruiseControl) DesiredSpeed.Z += DeltaSpeed * moveInputSign.Z; else DesiredSpeed.Z = MaxSpeed * moveInputSign.Z;
            }
        }
        DesiredSpeed = Vector3D.Clamp(DesiredSpeed, MaxSpeedMin, MaxSpeedMax);
        if (CruiseControl) ToLog("\nСкорость: " + (-PlanetSpeed.Z).ToString("#0") + " / " + (-DesiredSpeed.Z).ToString("#0"), true);
        else ToLog("\nСкорость: " + (-PlanetSpeed.Z).ToString("#0"), true);

        if (InertiaDampeners && AltitudeHoldMode) ToLog("\nВысота: " + curAltitude.ToString("#0") + " / " + DesiredAltitude.ToString("#0"), true);
        else ToLog("\nВысота: " + curAltitude.ToString("#0"), true);

        Vector3D SpeedDeviation = DesiredSpeed - PlanetSpeed;
        //ToLog("\nDes.spd: " + VectToStr(DesiredSpeed), true);
        //ToLog("\nSpd.dev: " + VectToStr(SpeedDeviation), true);

        //Z - вперед, X - вправо, Y - вверх
        Vector3D DesiredAccelerations;
        const double SigmoidFactorAlpha = 0.5;
        DesiredAccelerations.X = Sigmoid(SpeedDeviation.X, SigmoidFactorAlpha) * CurrentRegimeAccel;
        DesiredAccelerations.Z = Sigmoid(SpeedDeviation.Z, SigmoidFactorAlpha) * CurrentRegimeAccel;
        if (AltitudeHoldMode && InertiaDampeners)
        {
            ToLog("\ndeltaAlt: " + deltaAlt.ToString("#0.000000"), true);
            //ToLog("\nPlanetSpeed.Y: " + PlanetSpeed.Y.ToString("#0.000000"), true);

            ////нужно такое ускорение, чтобы корабль остановился при нулевом отклонении по высоте
            //double absDeltaAlt = Math.Abs(deltaAlt);
            //if (absDeltaAlt >= 0.5)
            //{
            //    int signDeltaAlt = Math.Sign(deltaAlt);
            //    //минимальное расстояние, на котором мы еще успеем затормозить
            //    if (Math.Sign(PlanetSpeed.Y) != signDeltaAlt)
            //    {
            //        //удаляемся
            //        DesiredAccelerations.Y = CurrentRegimeAccel * signDeltaAlt;
            //        //ToLog("\nудаляемся", true);
            //    }
            //    else
            //    {
            //        //приближаемся, оценим тормозной путь
            //        double temp = 0.5 * PlanetSpeed.Y * PlanetSpeed.Y;
            //        double brakingDist = temp / CurrentRegimeAccel;
            //        //ToLog("\nbrakingDist: " + brakingDist.ToString("#0.00000"), true);

            //        if (absDeltaAlt > brakingDist)
            //        {
            //            //еще успеваем затормозить, поэтому даем максимальное ускорение
            //            DesiredAccelerations.Y = CurrentRegimeAccel * signDeltaAlt;
            //        }
            //        else
            //        {
            //            //уменьшаем ускорение
            //            DesiredAccelerations.Y = MathHelperD.Clamp(-temp / deltaAlt, -CurrentRegimeAccel, CurrentRegimeAccel);
            //        }
            //    }
            //}
            //else
            //{
            //    DesiredAccelerations.Y = Sigmoid(SpeedDeviation.Y, SigmoidFactorAlpha) * CurrentRegimeAccel;
            //    //ToLog("\n!!!", true);
            //}

            DesiredAccelerations.Y = CalcAccelToStopAtPos(deltaAlt, PlanetSpeed.Y, CurrentRegimeAccel, 0.5, 0.5);

        }
        else DesiredAccelerations.Y = Sigmoid(SpeedDeviation.Y, SigmoidFactorAlpha) * CurrentRegimeAccel;

        ToLog("\nDes.acc: " + VectToStr(DesiredAccelerations), true);

        //double PlanetElevation;
        //Cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out PlanetElevation);

        double MaxDownThrust = 0;
        foreach (IMyThrust t in DownTrusters)
        {
            MaxDownThrust += t.MaxEffectiveThrust;
        }
        double ShipMass = Cockpit.CalculateShipMass().PhysicalMass;
        //ToLog("\nMax thr: " + MaxDownThrust.ToString("#0.0"), true);
        //ToLog("\nMass: " + ShipMass.ToString("#0.0"), true);

        double DesiredVertivalAcc = DesiredAccelerations.Y + Gravity.Length();
        if (DesiredVertivalAcc < 1.0) DesiredVertivalAcc = 1.0;
        ToLog("\nDes.vert.acc: " + DesiredVertivalAcc.ToString("#0.000"), true);

        double cosGravityToUp = normVertical.Dot(Cockpit.WorldMatrix.Up);
        double Thrust = ShipMass * DesiredVertivalAcc / cosGravityToUp;
        //ToLog("\nThrust: " + Thrust.ToString("#0.000"), true);

        //раздадим на движки
        float ThrustFactor = (float)(Thrust / MaxDownThrust);
        ToLog("\nThr.factor: " + ThrustFactor.ToString("#0.000"), true);

        if (ThrustFactor > 1f) ThrustFactor = 1f;
        foreach (IMyThrust t in DownTrusters)
        {
            t.ThrustOverridePercentage = ThrustFactor;
        }

        //углы откладываются от вертикали, X - тангаж, Y - крен
        Vector2D DesiredAngles;
        DesiredAngles.X = Math.Atan2(DesiredAccelerations.Z, DesiredVertivalAcc);
        DesiredAngles.Y = Math.Atan2(DesiredAccelerations.X, DesiredVertivalAcc);
        DesiredAngles = Vector2D.Clamp(DesiredAngles, MaxDeviationAngleMin, MaxDeviationAngleMax);
        ToLog("\nDes.angl: " + Vect2ToStr(DesiredAngles), true);

        double cosGravityToForward = normVertical.Dot(Cockpit.WorldMatrix.Forward);
        double cosGravityToRight = normVertical.Dot(Cockpit.WorldMatrix.Left);
        //ToLog("\nCos(G) to F/R: " + cosGravityToForward.ToString("#0.000") + " / " + cosGravityToRight.ToString("#0.000"), true);
        Vector2D CurrentAngles;
        CurrentAngles.X = Math.Atan2(cosGravityToForward, cosGravityToUp);
        CurrentAngles.Y = Math.Atan2(cosGravityToRight, cosGravityToUp);
        ToLog("\nCur.angl: " + Vect2ToStr(CurrentAngles), true);

        //ошибка угла
        Vector2D AnglesDeviation = Vector2D.Clamp(NormAngles2D(DesiredAngles - CurrentAngles), MaxDeviationAngleMin, MaxDeviationAngleMax);
        ToLog("\nAngl.dev: " + Vect2ToStr(AnglesDeviation, "#0.00000"), true);

        Vector2D GyroSignals;

        ////демфируем сигнал при приближении отклонения к максимальному значению
        //GyroSignals.X = SignalDamper(AnglesDeviation.X, MaxDeviationAngle, CurrentAngles.X, MaxDeviationAngle, 0.5);
        //GyroSignals.Y = SignalDamper(AnglesDeviation.Y, MaxDeviationAngle, CurrentAngles.Y, MaxDeviationAngle, 0.5);

        ToLog("\nPAV: " + VectToStr(PlanetAngularVelocity), true);
        GyroSignals.X = CalcAccelToStopAtPos(AnglesDeviation.X, PlanetAngularVelocity.X, MaxAngularAcceleration, 0.5 * Math.PI / 180.0, 1);
        GyroSignals.Y = CalcAccelToStopAtPos(AnglesDeviation.Y, -PlanetAngularVelocity.Z, MaxAngularAcceleration, 0.5 * Math.PI / 180.0, 1);
        ToLog("\nGyroSignals: " + Vect2ToStr(GyroSignals, "#0.00000"), true);

        //раздадим на гороскопы
        GyroPitch = -(float)(GyroPithPID.Reaction(GyroSignals.X) * GyroFactor);
        GyroRoll = (float)(GyroRollPID.Reaction(GyroSignals.Y) * GyroFactor);
        //GyroPitch = -(float)(AnglesDeviation.X * GyroFactor);
        //GyroRoll = (float)(AnglesDeviation.Y * GyroFactor);

        ToLog("\nGyro P/Y/R: " + GyroPitch.ToString("#0.000") + " / " + GyroYaw.ToString("#0.000") + " / " + GyroRoll.ToString("#0.000"), true);

        foreach (GyroDefinition g in Gyros)
        {
            SetGyroParams(g, true, GyroPitch, GyroYaw, GyroRoll);
        }



    }

    private void TestRotationInput()
    {

        if (!EngineIsON) return;

        /*
            * гироскопы стоят панелью вперед (круг спереди).
            * 
            * Значения в AngularVelocity, приведенной в СО корабля:
            *      тангаж (pitch, AngularVelocity.X), положительный вверх
            *      рысканье (yaw, AngularVelocity.Y), положительный влево
            *      крен (roll, AngularVelocity.Z), положительный против часовой
            * 
            * сигналы кокпита:
            *      Roll:   Cockpit.RollIndicator):         (-1;+1) положительный - поворот по часовой
            *      Pitch:  Cockpit.RotationIndicator.X     положительный - поворот вниз
            *      Yaw:    Cockpit.RotationIndicator.Y     положительный - поворот вправо
            * 
            */


        //GyroRoll = MaxGyroTorque * Math.Sign(Cockpit.RollIndicator);
        //GyroPitch = MaxGyroTorque * PitchYawSensitivityFactor * Cockpit.RotationIndicator.X;
        GyroYaw = PitchYawSensitivityFactor * Cockpit.RotationIndicator.Y * (float)GyroFactor;
        if (Cockpit.RotationIndicator.Y != 0f) PlanetMatrixReady = false;

        ToLog("\nGyroYaw: " + GyroYaw.ToString("#0.000"), true);

    }

    private double CalcAccelToStopAtPos(double deltaPos, double curSpd, double maxAccel, double posAccuracy, double AccelPerDeltaPosFactor)
    {

        double accel;
        double absDeltaPos = Math.Abs(deltaPos);
        //if (absDeltaPos >= posAccuracy)
        //{
            int signDeltaPos = Math.Sign(deltaPos);
            if (Math.Sign(curSpd) != signDeltaPos)
            {
                //удаляемся или не движемся
                //accel = maxAccel * signDeltaPos;
                accel = MathHelperD.Clamp(AccelPerDeltaPosFactor * deltaPos * maxAccel, -maxAccel, maxAccel);
                ToLog("\nудаляемся", true);
            }
            else
            {
                //приближаемся, оценим тормозной путь
                double temp = 0.5 * curSpd * curSpd;
                double brakingDist = temp / maxAccel;
                ToLog("\nbrakingDist: " + brakingDist.ToString("#0.000"), true);
                if (absDeltaPos >= brakingDist)
                {
                    //еще успеем затормозить, поэтому даем максимальное ускорение
                    //accel = maxAccel * signDeltaPos;
                    accel = MathHelperD.Clamp(AccelPerDeltaPosFactor * deltaPos * maxAccel, -maxAccel, maxAccel);
                    ToLog("\nеще успеем: " + absDeltaPos.ToString("#0.00000") + " / " + brakingDist.ToString("#0.00000"), true);
                }
                else
                {
                    //уменьшаем ускорение
                    accel = -temp / deltaPos;
                    ToLog("\nуменьш: " + accel.ToString("#0.000"), true);
                }
            }
        //}
        //else
        //{
        //    accel = AccelPerDeltaPosFactor * deltaPos * maxAccel;
        //    ToLog("\n!!!",true);
        //}
            return accel;
    }



    private static double SignalDamper(double signal, double maxSignal, double value, double maxValue, double beginDamperWorkFactor)
    {

        double resault = signal;

        double beginDamperWork = beginDamperWorkFactor * maxValue;
        double absValue = Math.Abs(value);
        if (absValue > beginDamperWork && Math.Sign(signal) == Math.Sign(value))
        {
            double damperWidth = maxValue - beginDamperWork;
            double damperFactor = MathHelperD.Clamp((maxValue - absValue) / damperWidth, -1.0, 1.0);
            if (damperFactor >= 0.0) resault = signal * damperFactor; else resault = maxSignal * damperFactor;
        }

        return resault;
    }
    private static double Sigmoid(double x, double alpha)
    {
        double y = x * alpha;
        double s = y / (1.0 + Math.Abs(y));
        return s;
    }

    //=== до сюда ===>>>===>>>
}}
