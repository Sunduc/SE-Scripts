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
namespace shuttle
{
    public sealed class Program : MyGridProgram
    {
            // НАЧАЛО СКРИПТА
            //содержит куски кода andrukha74#3658 (diskord)

            const UpdateFrequency UPD = UpdateFrequency.Update10;

            const string HOVER_TAG = "Hover";
            const float KV = 0.01f;
            const float KI = 0.1f;
            const float MAX_ANGLE = 10f / 180f * (float)Math.PI;
            const float HOVER_ALT = 20f;
            const float WING_MLT = 32f;

            private struct CoordPair
            {
                public string Name;
                public Vector3D Landing, Destination;
            }
            List<CoordPair> CoordPairList = new List<CoordPair>()
            {
                { new CoordPair() { Name = "Земля", Landing = new Vector3D (195205.3d, 267365.54d, -313667.34d), Destination = new Vector3D (196149.73d, 268647.59d, -313282.29d)} },
                { new CoordPair() { Name = "Планета сокровищ", Landing = new Vector3D (105945d, 100647.67d, -45532.01d), Destination = new Vector3D (105517.22d, 101030.74d, -43620.33d)} }
            };
            private class CPlanet
            {
                public string Name;
                public Vector3D Position;
                public float Radius;
                public float HillParams;
                public float FallOff;
                public float Gravity;
                public float LimitAltitude;
                public float PSL;
                public float GravityRadius()
                {         
                        if (Name != "Космос")
                            return ((float)Math.Pow(Gravity / 0.05f, 1f / FallOff) * Radius * (1f + HillParams));
                        else
                            return 0f;
                }
                public float Atmosphere()
                {
                        if (Name != "Космос")
                            return (Radius * HillParams * LimitAltitude);
                        else
                            return 0f;
                }
                public float AtmosphereDensity(double ASL)
                {
                    if (Name != "Космос")
                        return PSL * (1f - ((float)ASL / Atmosphere()));
                    else
                        return 0;
                }
                public float ShutUpThrusters(float speed)
                {
                if (Name != "Космос")
                    return (float)Math.Pow(((FallOff - 1f) / (2f * Gravity * 9.81f)) * ((speed * speed) / (float)Math.Pow(Radius * (1f + HillParams), 7)) + (float)Math.Pow(GravityRadius(), 1 - FallOff), 1 / (1 - FallOff)) - Radius;
                else
                    return 0;
                }
            }
            private static float[] ELEVATIONS = new float[] { 1500, 2500, 3500, 4500, 6000, 8000, 10000 };
            public static float[] SPEED_LIMIT = new float[] { 90, 110, 130, 150, 170, 190, 210 };
            private int i = 0;

            private Dictionary<string, CPlanet> Planets = new Dictionary<string, CPlanet>
            {
                { "Earth", new CPlanet() { Name = "Земля", Position = new Vector3D(217402.83d, 238110.81d, -264843.85d), Radius = 60000f, HillParams = 0.12f, FallOff = 3f, Gravity = 1f, LimitAltitude = 4f, PSL = 1.1f } },
                { "Treasure", new CPlanet() { Name = "Планета Сокровищ", Position = new Vector3D(52795.1429370835d, 71165.037240268d, -50475.5759076541d), Radius = 60000f, HillParams = 0.12f, FallOff = 7f, Gravity = 1.5f, LimitAltitude = 4f, PSL = 1.1f } },
                { "Alien", new CPlanet() { Name = "Чужая планета", Position = new Vector3D(29793.01d, 569999.22d, 58776.87d), Radius = 20007f, HillParams = 0.12f, FallOff = 3.8f, Gravity = 2.5f, LimitAltitude = 3.2f, PSL = 1.1f } },
                { "Ravcor", new CPlanet() { Name = "Равкор", Position = new Vector3D(294495.88d, -316728.18d, -461254.08d), Radius = 20007f, HillParams = 0.12f, FallOff = 4.8f, Gravity = 5f, LimitAltitude = 2f, PSL = 1.1f } },
                { "Space", new CPlanet() { Name = "Космос" } }
            };

            private string init_message;
            private bool init_error = false;

            IMyShipController Cockpit;
            IMyTextSurface LCD, LCD2, ProgBlockLCD;
            List<IMyThrust> HoverList;
            List<IMyThrust> JetList;
            List<IMyThrust> HThrustList;
            List<IMyBatteryBlock> BatteryList;
            List<IMyGasTank> TankList;
            List<IMyPowerProducer> EngineList;
            List<IMyAdvancedDoor> AirBrakeList;
            //List<IMyParachute> ParList;
            //List<IMyGyro> GyroList;
            List<IMyCargoContainer> CargoList;
            List<GyroDefinition> GyroDefList = new List<GyroDefinition>();

            Vector3D GravityVector;
            Vector3D GravityVectorNorm;
            Vector3D HorizonForwardNorm;
            Vector3D HorizonRightNorm;
            Vector3D VelocityVector;
            Vector3D HorizonForwardVel;
            Vector3D HorizonRightVel;
            Vector3D ForwardInput;
            Vector3D RightInput;
            Vector3D AlignVector;
            Vector3D ShipAlign;
            Vector3D TargetVector;
            Vector3D TargetPoint;


        //Vector3D Test;

        StringBuilder LCDText = new StringBuilder();    
            private enum AutopilotStageEnum
            {
                Off,
                ChoiseDestination,
                Start,
                AthmosClimb,
                AthmosFlight,
                NonAthmosClimb,            
                SpaceFlight,
                NonAthmosDecline,
                AthmosDecline,
                Landing
            }
            AutopilotStageEnum AutopilotStage = AutopilotStageEnum.Off;
            //private bool Autopilot = false;*/

            /*private enum LocationEnum
            {
                Earth,
                Treasure,
                Alien,
                Ravcor,
                Space
            }*/
            private CPlanet Location;
            private CoordPair DestinationPoint;
            private bool DestP = false;
            private bool align = false;
            private bool takeoff = false;
            private bool once = false;

            private enum PilotModeEnum
            {
                Hover,
                Fly,
                Autopilot
            }
            private PilotModeEnum PilotMode = PilotModeEnum.Fly;

            private enum HoverModeEnum
            {
                FlyControl,
                DirectControl,
                CruiseControl
            }
            private HoverModeEnum HoverMode = HoverModeEnum.DirectControl;

            private float PitchInput;
            private float RollInput;
            private float YawInput;
            private float cruisespeed;
            private float altitude;
            private double ASF;
            private double ASL;
            private float angle = 10f;
            private float OldSpeed = 0;

            int DisplayUpdateCounter = 0;
            const int DisplayUpdateInterval = 1;
            const double DisplayUpdateFreq = (1d / 10d) / (double)DisplayUpdateInterval * 1000d;
            double runTime = 0d;

            private bool initedFlag = false;
            private int initTicksCounter = 0;
            private int initTicksWait;

            public Program()
            {
                Random rnd = new Random();
                initTicksWait = 1;
                Runtime.UpdateFrequency = UPD;
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

                //DebugLCD = FindDisplay(DEBUG_LCD_NAME, GridTerminalSystem, Me);
                //ToLog("", false, false);

                AirBrakeList = FindAllBlocksOfType<IMyAdvancedDoor>(GridTerminalSystem, Me);
                JetList = FindAllBlocksOfType<IMyThrust>(GridTerminalSystem, Me, "Jet");
                HThrustList = FindAllBlocksOfType<IMyThrust>(GridTerminalSystem, Me, "Hydrogen");
                BatteryList = FindAllBlocksOfType<IMyBatteryBlock>(GridTerminalSystem, Me);
                EngineList = FindAllBlocksOfType<IMyPowerProducer>(GridTerminalSystem, Me, "Engine");
                TankList = FindAllBlocksOfType<IMyGasTank>(GridTerminalSystem, Me);
                HoverList = FindAllBlocksOfType<IMyThrust>(GridTerminalSystem, Me, HOVER_TAG);
                //ParList = FindAllBlocksOfType<IMyParachute>(GridTerminalSystem, Me);
                Cockpit = FindDefaultCockpit(GridTerminalSystem, Me);
                CargoList = FindAllBlocksOfType<IMyCargoContainer>(GridTerminalSystem, Me, "Cargo");
                List<IMyGyro> GyroList = FindAllBlocksOfType<IMyGyro>(GridTerminalSystem, Me);
                SetupGyros(Cockpit, GyroList, GyroDefList);
            
                LCD = FindDisplay(Cockpit.CustomName + "(0)", GridTerminalSystem, Me);
                LCD2 = FindDisplay(Cockpit.CustomName + "(2)", GridTerminalSystem, Me);

                ProgBlockLCD = Me.GetSurface(0);

                if (LCD == null) sb.AppendLine("Инфо дисплей не обнаружен");
                if (LCD2 == null) sb.AppendLine("Дисплей меню не обнаружен");

                if (JetList.Count == 0) { init_error = true; sb.AppendLine("Джеты не обнаружены!"); } else sb.AppendFormat("Джетов: {0}\n", JetList.Count);
                if (HThrustList.Count == 0) { init_error = true; sb.AppendLine("Водородники не обнаружены!"); } else sb.AppendFormat("Водородников: {0}\n", HThrustList.Count);
                if (HoverList.Count == 0) { init_error = true; sb.AppendLine("Ховеры не обнаружены!"); } else sb.AppendFormat("Ховеров: {0}\n", HoverList.Count);
                if (GyroDefList.Count == 0) { init_error = true; sb.AppendLine("Гироскопы не обнаружены!"); } else sb.AppendFormat("Гироскопов: {0}\n", GyroDefList.Count);
                if (CargoList.Count == 0) { init_error = true; sb.AppendLine("Контейнеры не обнаружены!"); } else sb.AppendFormat("Контейнеров: {0}\n", CargoList.Count);
                if (AirBrakeList.Count == 0) { init_error = true; sb.AppendLine("Аэротормоза не обнаружены!"); } else sb.AppendFormat("Аэротормозов: {0}\n", AirBrakeList.Count);
                if (BatteryList.Count == 0) { init_error = true; sb.AppendLine("Батареи не обнаружены!"); } else sb.AppendFormat("Батарей: {0}\n", BatteryList.Count);
                if (EngineList.Count == 0) { init_error = true; sb.AppendLine("Генераторы не обнаружены!"); } else sb.AppendFormat("Генераторов: {0}\n", EngineList.Count);
                if (TankList.Count == 0) { init_error = true; sb.AppendLine("Баки не обнаружены!"); } else sb.AppendFormat("Баков: {0}\n", TankList.Count);
                //if (ParList.Count == 0) { init_error = true; sb.AppendLine("Парашут не обнаружены!"); } else sb.AppendFormat("Парашут: {0}\n", ParList.Count);

                init_message = sb.ToString();

                Echo(init_message);

                Runtime.UpdateFrequency = UPD;

                if (init_error)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    LCD.WriteText(init_message);
                }

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

                Cockpit.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out ASL);
                Cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out ASF);

                int arg;

                if (!string.IsNullOrEmpty(argument)) arg = int.Parse(argument);
                else arg = 0;

                foreach (KeyValuePair<string, CPlanet> keyValue in Planets)
                {
                    if (keyValue.Key != "Space" && (Cockpit.GetPosition() - keyValue.Value.Position).Length() < keyValue.Value.GravityRadius())
                    {
                        Location = keyValue.Value;

                        GravityVector = Cockpit.GetNaturalGravity();
                        GravityVectorNorm = Vector3D.Normalize(GravityVector);

                        if (!Vector3D.IsZero(Cockpit.WorldMatrix.Forward.Cross(GravityVectorNorm)))
                            HorizonForwardNorm = Vector3D.Normalize(Vector3D.Reject(Cockpit.WorldMatrix.Forward, GravityVectorNorm));
                        else
                            HorizonForwardNorm = Vector3D.Normalize(Vector3D.Reject(Cockpit.WorldMatrix.Up, GravityVectorNorm));
                        HorizonRightNorm = Vector3D.Normalize(GravityVectorNorm.Cross(HorizonForwardNorm));

                        VelocityVector = Cockpit.GetShipVelocities().LinearVelocity;
                        HorizonForwardVel = Vector3D.ProjectOnVector(ref VelocityVector, ref HorizonForwardNorm);
                        HorizonRightVel = Vector3D.ProjectOnVector(ref VelocityVector, ref HorizonRightNorm);

                        if (PilotMode != PilotModeEnum.Autopilot)
                        if (ASF < HOVER_ALT)
                        {
                            PilotMode = PilotModeEnum.Hover;
                            HoverControl();
                        }
                        else
                        {
                            PilotMode = PilotModeEnum.Fly;
                            GyroDefList.ForEach(g => SetGyroParams(g));
                            if (ASF < 200d) HoverList.ForEach(h => h.Enabled = true);
                            else HoverList.ForEach(h => h.Enabled = false);
                        }
                        break;
                    }
                    else
                        if (keyValue.Key == "Space")
                        {
                            Location = keyValue.Value;
                        }
                
                }
                if (PilotMode == PilotModeEnum.Autopilot)
                    Autopilot();
                LCDMenu(arg);
                arg = 0;

            }

            public void Save()
            { }

            private void Autopilot()
            {
                //TargetVector = Vector3D.Zero;
                AlignVector = Vector3D.Zero;
                switch (AutopilotStage)
                {
                    case AutopilotStageEnum.Off:
                        AutopilotStage = AutopilotStageEnum.ChoiseDestination;
                        break;
                    case AutopilotStageEnum.ChoiseDestination:
                        if (DestP)
                        {
                            TargetVector = DestinationPoint.Landing - Cockpit.GetPosition();
                            if (Vector3D.IsZero(TargetVector.Cross(GravityVectorNorm))) TargetVector = Cockpit.WorldMatrix.Forward;
                            else if (TargetVector.Dot(Cockpit.WorldMatrix.Up) != 0) TargetVector = Vector3D.Normalize(Vector3D.Reject(DestinationPoint.Landing - Cockpit.GetPosition(), Cockpit.WorldMatrix.Up));
                            else TargetVector = Vector3D.Normalize(TargetVector);
                        
                            TargetVector = new Vector3D(TargetVector.Dot(Cockpit.WorldMatrix.Right), TargetVector.Dot(Cockpit.WorldMatrix.Up), TargetVector.Dot(Cockpit.WorldMatrix.Forward));
                            AlignVector += HorizonRightVel * KV;
                            AlignVector += HorizonForwardVel * KV;
                            AlignVector += GravityVectorNorm;
                            AlignVector = Vector3D.Normalize(AlignVector);
                            if (AlignVector.Dot(GravityVectorNorm) < Math.Cos(MAX_ANGLE))
                            {
                                AlignVector = Vector3D.Normalize(Vector3D.Reject(AlignVector, GravityVectorNorm)) * Math.Sin(MAX_ANGLE) + GravityVectorNorm * Math.Cos(MAX_ANGLE);
                            }

                            ShipAlign = new Vector3D(AlignVector.Dot(Cockpit.WorldMatrix.Right), AlignVector.Dot(Cockpit.WorldMatrix.Up), AlignVector.Dot(Cockpit.WorldMatrix.Forward));

                            PitchInput = (float)Math.Atan2(-ShipAlign.Z, -ShipAlign.Y);
                            RollInput = (float)Math.Atan2(-ShipAlign.X, -ShipAlign.Y);
                            if (!align)
                                if (Math.Truncate(Math.Atan2(TargetVector.X, TargetVector.Z) * 100) != 0) YawInput = (float)Math.Atan2(TargetVector.X, TargetVector.Z);
                                else align = true;
                            else YawInput = Cockpit.RotationIndicator.Y / 4f;
                            GyroDefList.ForEach(g => SetGyroParams(g, true, PitchInput, YawInput, RollInput));
                        
                        }
                        break;
                    case AutopilotStageEnum.Start:
                        DestP = false;
                        align = false;
                        TankList.ForEach(t => t.Stockpile = false);
                        EngineList.ForEach(e => e.Enabled = true);
                        BatteryList.ForEach(b => b.ChargeMode = ChargeMode.Auto);
                        AirBrakeList.ForEach(a => a.CloseDoor());
                        JetList.ForEach(j => j.ThrustOverridePercentage = 1);
                        JetList.ForEach(j => j.Enabled = true);
                        //float angle = ((5f / 180f) * (float)Math.PI);
                        if (VelocityVector.Length() > 70)
                        {
                            VectorFlight(Vector3D.Normalize((HorizonForwardNorm * Math.Cos(MathHelper.ToRadians(5f))) + (-GravityVectorNorm * Math.Sin(MathHelper.ToRadians(5f)))));
                            takeoff = true;
                            if (VelocityVector.Length() > 90) AutopilotStage = AutopilotStageEnum.AthmosClimb;
                        }
                        else
                        {
                            VectorFlight(HorizonForwardNorm);
                        }
                        break;
                    case AutopilotStageEnum.AthmosClimb:
                        takeoff = false;
                        if (!once)
                        {
                            foreach (KeyValuePair<string, CPlanet> keyValue in Planets)
                            {
                                TargetPoint = DestinationPoint.Landing - keyValue.Value.Position;
                                if (TargetPoint.Length() < keyValue.Value.GravityRadius())
                                {
                                    TargetPoint = Vector3D.Normalize(TargetPoint) * (keyValue.Value.GravityRadius());
                                    TargetPoint = Vector3D.Normalize(TargetPoint - Location.Position + keyValue.Value.Position) * (Location.Radius + Location.Atmosphere());
                                    TargetPoint += Location.Position;
                                    once = true;
                                    break;
                                }
                            }
                        }

                        TargetVector = TargetPoint - Cockpit.GetPosition();
                        if (TargetVector.Length() < Location.Atmosphere() && Vector3D.Reject(TargetVector,GravityVectorNorm).Length() < 100 )
                        {
                            AutopilotStage = AutopilotStageEnum.NonAthmosClimb;
                            return;
                        }

                        if (Vector3D.IsZero(TargetVector.Cross(GravityVectorNorm))) TargetVector = HorizonForwardNorm;
                        else if (TargetVector.Dot(GravityVectorNorm) != 0) TargetVector = Vector3D.Normalize(Vector3D.Reject(TargetPoint - Cockpit.GetPosition(), GravityVectorNorm));
                        else TargetVector = Vector3D.Normalize(TargetVector);
                        TargetVector = TargetVector * Math.Cos(MathHelper.ToRadians(angle)) + -GravityVectorNorm * Math.Sin(MathHelper.ToRadians(angle));

                        VectorFlight(TargetVector);

                        float DSpeedLim;
                        if (i == 0) DSpeedLim = 90;
                        else DSpeedLim = SPEED_LIMIT[i] - SPEED_LIMIT[i - 1];

                        if (ASL > ELEVATIONS[i] && i + 1 < ELEVATIONS.Length) i++;

                        if (VelocityVector.Length() < SPEED_LIMIT[i] - DSpeedLim / 2 && angle > 1) angle -= 0.1f;
                        else if (VelocityVector.Length() < SPEED_LIMIT[i] && VelocityVector.Length() - OldSpeed < 0) angle -= 0.1f;
                        else if (VelocityVector.Length() > (SPEED_LIMIT[i] - DSpeedLim / 2) && VelocityVector.Length() - OldSpeed > 0) angle += 0.1f;
                        else if (angle < 45) angle += 0.1f;

                        if (VelocityVector.Dot(-GravityVectorNorm) < 40 && ASL < 10000) HThrustList.ForEach(h => { h.Enabled = true; h.ThrustOverridePercentage = 1f - ((float)VelocityVector.Dot(-GravityVectorNorm) / 40f); });
                        else HThrustList.ForEach(h => h.Enabled = false);

                        OldSpeed = (float)VelocityVector.Length();
                        break;
                    case AutopilotStageEnum.AthmosFlight:
                        
                        break;
                    case AutopilotStageEnum.NonAthmosClimb:
                        once = false;

                        HThrustList.ForEach(h => h.Enabled = true);
                        TargetVector = new Vector3D(-GravityVectorNorm.Dot(Cockpit.WorldMatrix.Right), -GravityVectorNorm.Dot(Cockpit.WorldMatrix.Up), -GravityVectorNorm.Dot(Cockpit.WorldMatrix.Forward));
                        YawInput = (float)Math.Atan2(TargetVector.X, TargetVector.Z);
                        PitchInput = (float)Math.Atan2(-TargetVector.Y, TargetVector.Z);
                        RollInput = 0;
                        GyroDefList.ForEach(g => SetGyroParams(g, true, PitchInput, YawInput, RollInput));
                        
                        float NeedThrust = (float)GravityVector.Length() * Cockpit.CalculateShipMass().PhysicalMass;
                        float SumThrust = 0;

                        if (ASL < Location.Atmosphere() * 0.7f)
                            JetList.ForEach(j => SumThrust += j.CurrentThrust);
                        else
                            JetList.ForEach(j => j.Enabled = false);
                        HThrustList.ForEach(h => SumThrust += h.CurrentThrust);

                        //if (Location.ShutUpThrusters(VelocityVector.Dot(-GravityVectorNorm)) >)
                        if (VelocityVector.Length() >= 210 && SumThrust > NeedThrust)
                        {
                            HThrustList.ForEach(h => h.ThrustOverride = h.CurrentThrust - ((SumThrust - NeedThrust) / HThrustList.Count));
                        }
                        else HThrustList.ForEach(h => h.ThrustOverridePercentage = 1);

                        break;
                    default:
                        break;

                }
            }
            private void HoverControl ()
            {
                AlignVector = Vector3D.Zero;

                if (HoverMode != HoverModeEnum.FlyControl)
                {
                    if (Cockpit.MoveIndicator.Z != 0)
                    {
                        if (ForwardInput.Length() < 2) ForwardInput += HorizonForwardNorm * 0.1 * -Cockpit.MoveIndicator.Z;
                        else if (ForwardInput.Length() < 20) ForwardInput += HorizonForwardNorm * -Cockpit.MoveIndicator.Z;
                             else if (ForwardInput.Length() < 100) ForwardInput += HorizonForwardNorm * 10 * -Cockpit.MoveIndicator.Z;
                    }
                    else ForwardInput = Vector3D.Zero;

                    if (Cockpit.MoveIndicator.X != 0)
                    {
                        if (RightInput.Length() < 2) RightInput += HorizonRightNorm * 0.1 * Cockpit.MoveIndicator.X;
                        else if (RightInput.Length() < 20) RightInput += HorizonRightNorm * Cockpit.MoveIndicator.X;
                             else if (RightInput.Length() < 100) RightInput += HorizonRightNorm * 10 * Cockpit.MoveIndicator.X;
                    }
                    else RightInput = Vector3D.Zero;

                    float UpInput = Cockpit.RollIndicator / 10;

                    if (!Vector3D.IsZero(RightInput)) AlignVector += -RightInput;
                    else if (Cockpit.DampenersOverride) AlignVector += HorizonRightVel * KV;

                    if (!Vector3D.IsZero(ForwardInput))
                    {
                        if (HoverMode == HoverModeEnum.CruiseControl)
                        {
                            cruisespeed += (float)ForwardInput.Dot(HorizonForwardNorm);
                            AlignVector += (HorizonForwardVel - HorizonForwardNorm * cruisespeed) * KV;
                        }
                        else AlignVector += -ForwardInput;
                    }
                    else if (HoverMode == HoverModeEnum.CruiseControl) AlignVector += (HorizonForwardVel - HorizonForwardNorm * cruisespeed) * KV;
                         else if (Cockpit.DampenersOverride) AlignVector += HorizonForwardVel * KV;

                    AlignVector += GravityVectorNorm;

                    AlignVector = Vector3D.Normalize(AlignVector);
                    if (AlignVector.Dot(GravityVectorNorm) < Math.Cos(MAX_ANGLE))
                    {
                        AlignVector = Vector3D.Normalize(Vector3D.Reject(AlignVector, GravityVectorNorm)) * Math.Sin(MAX_ANGLE) + GravityVectorNorm * Math.Cos(MAX_ANGLE);
                    }

                    ShipAlign = new Vector3D(AlignVector.Dot(Cockpit.WorldMatrix.Right), AlignVector.Dot(Cockpit.WorldMatrix.Up), AlignVector.Dot(Cockpit.WorldMatrix.Forward));

                    PitchInput = (float)Math.Atan2(-ShipAlign.Z, -ShipAlign.Y);
                    RollInput = (float)Math.Atan2(-ShipAlign.X, -ShipAlign.Y);
                    YawInput = Cockpit.RotationIndicator.Y / 4;

                    foreach (IMyThrust hover in HoverList)
                    {
                        altitude = hover.GetValueFloat("altitudemin_slider_L");
                        altitude += UpInput;
                        hover.SetValueFloat("altitudemin_slider_L", altitude);
                    }

                    GyroDefList.ForEach(g => SetGyroParams(g, true, PitchInput, YawInput, RollInput));
                }
                else GyroDefList.ForEach(g => SetGyroParams(g));
            }
        
            private void LCDMenu(int argument = 0)
            {
                StringBuilder LCDMenuText = new StringBuilder();
                LCDText.Clear();

                LCDText.Append(Location.Name + ": ");
                if (Location.Name != "Космос" && ASF < HOVER_ALT) LCDText.Append("На поверхности\n");
                else if ((Cockpit.GetPosition() - Location.Position).Length() < Location.Atmosphere())
                    LCDText.Append("В атмосфере\n");
                else
                    LCDText.Append("В гравитации\n");

                switch (PilotMode)
                {
                    case PilotModeEnum.Hover:

                        switch (argument)
                        {
                            case 1:
                                HoverMode = HoverModeEnum.FlyControl;
                                break;
                            case 2:
                                HoverMode = HoverModeEnum.DirectControl;
                                break;
                            case 3:
                                HoverMode = HoverModeEnum.CruiseControl;
                                cruisespeed = (float)HorizonForwardVel.Dot(HorizonForwardNorm);
                                break;
                            case 4:
                                PilotMode = PilotModeEnum.Autopilot;
                                break;
                            default:
                                break;
                        }

                        LCDText.Append("Ховер режим: ");

                        switch (HoverMode)
                        {
                            case HoverModeEnum.FlyControl:
                                LCDText.Append("Летный контроль\n");
                                break;
                            case HoverModeEnum.DirectControl:
                                LCDText.Append("Ручной контроль\n");
                                break;
                            case HoverModeEnum.CruiseControl:
                                LCDText.AppendFormat("Круиз: {0:#0.0}\n", cruisespeed);
                                break;
                            default:
                                break;
                        }
                        LCDText.AppendFormat("Скорость: {0:#0.0} / Вперед: {1:#0.0} / Вправо: {2:#0.0}\n", VelocityVector.Length(), HorizonForwardVel.Dot(HorizonForwardNorm), HorizonRightVel.Dot(HorizonRightNorm));
                        LCDText.AppendFormat("Высота: пар. {0:#0.0} / ур. м. {1:#0.0} / пов. {2:#0.0}\n", altitude, ASL, ASF);
                        LCDText.Append("\n1. Летный контроль\n2. Ручной контроль\n3. Круиз контроль\n4. Автопилот");

                        break;
                    case PilotModeEnum.Fly:


                        break;
                    case PilotModeEnum.Autopilot:

                        switch (AutopilotStage)
                        {
                            case AutopilotStageEnum.ChoiseDestination:
                                if (!DestP)
                                {
                                    LCDText.Append("Выберите точку назначения:\n1. Ручное управление");
                                    CoordPairList.ForEach(l => LCDText.Append("\n" + (CoordPairList.IndexOf(l) + 2) + ". " + l.Name));
                                    if (argument == 1)
                                    {
                                        if (ASF < HOVER_ALT) PilotMode = PilotModeEnum.Hover;
                                        else PilotMode = PilotModeEnum.Fly;
                                        AutopilotStage = AutopilotStageEnum.Off;
                                        DestP = false;
                                        return;
                                    }
                                    if (argument > 1 && CoordPairList.Count >= argument-1)
                                    {
                                        DestinationPoint = CoordPairList[argument-2];
                                        DestP = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    if (!align) LCDText.AppendFormat("Дождитесь наведения на цель, скорректируйте направление взлета если необходимо и подтвердите старт.\n{0:#0.000}\n1. Ручной режим", Math.Atan2(TargetVector.X, TargetVector.Z));
                                    else LCDText.Append("Дождитесь наведения на цель, скорректируйте направление взлета если необходимо и подтвердите старт.\nНаведение завершено\n1. Ручной режим\n2. Старт");
                                    if (argument == 1)
                                    {
                                        if (ASF < HOVER_ALT) PilotMode = PilotModeEnum.Hover;
                                        else PilotMode = PilotModeEnum.Fly;
                                        AutopilotStage = AutopilotStageEnum.Off;
                                        DestP = false;
                                        align = false;
                                        return;
                                    }
                                    else if (argument == 2 && align)
                                    {
                                        AutopilotStage = AutopilotStageEnum.Start;
                                    }
                                }
                                break;
                            case AutopilotStageEnum.Start:
                                LCDText.AppendFormat("Скорость: {0:#0.0}\nНабор высоты: {1:#0.0}\nОтрыв: {2:}\n", VelocityVector.Length(), VelocityVector.Dot(-GravityVectorNorm), takeoff);
                                break;
                            case AutopilotStageEnum.AthmosClimb:
                                LCDText.AppendFormat("Скорость: {0:#0.0}\nНабор высоты: {1:#0.0}\n", VelocityVector.Length(), VelocityVector.Dot(-GravityVectorNorm));
                                LCDText.AppendFormat("До точки взлета: {0:#0.0}\n", Vector3D.Reject(TargetPoint - Cockpit.GetPosition(), GravityVectorNorm).Length());
                                break;
                            default:
                                break;
                        }
                    
                        break;
                    default:
                        break;
                }

                DisplayUpdateCounter += 1;
                if (DisplayUpdateCounter >= DisplayUpdateInterval)
                {
                    Echo("Cycle time, µs: " + (runTime * DisplayUpdateFreq).ToString("#0.0") + "\n" + init_message);
                    DisplayUpdateCounter = 0;
                    runTime = 0d;

                    ProgBlockLCD.WriteText(LCDText);

                    if (LCD != null)
                    {
                        string t = ProgBlockLCD.GetText();
                        LCD.WriteText(t);
                    }
                }
                runTime += Runtime.LastRunTimeMs;
            }

            private Vector3D ResultForce (float AtackAngle)
            {
                if (Location.Name != "Космос" && (Cockpit.GetPosition() - Location.Position).Length() < Location.Radius + Location.Atmosphere())
                {
                    float WingScalar = WING_MLT * (float)MathHelper.Clamp(VelocityVector.LengthSquared() * 2f, 0f, 20000f) * (float)MathHelper.Clamp((Location.AtmosphereDensity(ASL) - 0.4f) / 0.3f, 0f, 1f);
                    Vector3D Forward = Vector3D.Normalize(Cockpit.WorldMatrix.Up * Math.Sin(AtackAngle) + Cockpit.WorldMatrix.Forward * Math.Cos(AtackAngle));
                    Vector3D Up = Vector3D.Normalize(Vector3D.Cross(Forward, Cockpit.WorldMatrix.Left));
                    Vector3D WingForce = (-Up * Up.Dot(VelocityVector) * WingScalar) - Vector3D.Normalize(VelocityVector) * (WingScalar * 0.25f);
                    Vector3D JetForce = Vector3D.Zero;
                    JetList.ForEach(j => JetForce += Forward * j.CurrentThrust);
                    Vector3D GravityForce = GravityVector * Cockpit.CalculateShipMass().PhysicalMass;
                    return GravityForce + JetForce + WingForce;
                }
                else
                    return Vector3D.Zero;
            }

            private void VectorFlight (Vector3D TargetFlight)
            {
                TargetFlight = new Vector3D(TargetFlight.Dot(Cockpit.WorldMatrix.Right), TargetFlight.Dot(Cockpit.WorldMatrix.Up), TargetFlight.Dot(Cockpit.WorldMatrix.Forward));
                AlignVector = Vector3D.Normalize(VelocityVector.Dot(Cockpit.WorldMatrix.Right) * KV * Cockpit.WorldMatrix.Right + Cockpit.WorldMatrix.Up);
                YawInput = (float)Math.Atan2(TargetFlight.X, TargetFlight.Z);
                PitchInput = (float)Math.Atan2(-TargetFlight.Y, TargetFlight.Z);
                if (AlignVector.Dot(Cockpit.WorldMatrix.Up) < Math.Cos(MathHelper.ToRadians(1f)))
                {
                    AlignVector = Vector3D.Normalize(Vector3D.Reject(AlignVector, Cockpit.WorldMatrix.Up)) * Math.Sin(MathHelper.ToRadians(1f)) + Cockpit.WorldMatrix.Up * Math.Cos(MathHelper.ToRadians(1f));
                }
                ShipAlign = new Vector3D(AlignVector.Dot(Cockpit.WorldMatrix.Right), AlignVector.Dot(Cockpit.WorldMatrix.Up), AlignVector.Dot(Cockpit.WorldMatrix.Forward));
                RollInput = (float)Math.Atan2(-ShipAlign.X, ShipAlign.Y);
                GyroDefList.ForEach(g => SetGyroParams(g, true, PitchInput, YawInput, RollInput));
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
                catch { }

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

            #endregion

            // КОНЕЦ СКРИПТА
    }
}