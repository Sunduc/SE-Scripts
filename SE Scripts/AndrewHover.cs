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
namespace AndrewHover
{
    public sealed class Program : MyGridProgram
    {
        const UpdateFrequency UpdFreq = UpdateFrequency.Update1;

        const string CockpitName = "Control Seat";
        const string DisplayName = "Control Seat(0)";

        const float MAX_SIDE_SPEED = 8.0f;
        const float MAX_ANGLE = 10.0f / 180f * MathHelper.Pi;
        const float KEY_PUSH_DELTA_SPEED = 1.0f;
        const float KEY_PUSH_DELTA_ALTITUDE = 0.5f;

        const float SPEED_ERROR_FACTOR = 0.15f;
        const float GYRO_FACTOR = 3.0f;

        bool CruiseControl = false, UnderControl = false, MustSetSpeed = false;
        float DesiredSpeed = 0;
        float DesiredAltitude = 0;

        IMyShipController Cockpit;
        IMyTextSurface LCD;

        List<IMyThrust> Hovers;

        float RotationFactor = 0.015f;   //0.025f;
        List<GyroDefinition> GyroDefList = new List<GyroDefinition>();

        private List<IMyInventory> InventoryList = new List<IMyInventory>();
        private const int INV_UPD_INTERVAL = 6;
        private int inventoryUpdateCounter = 0;
        private float inventoryTotalVolume = 0f, inventoryUsedVolume = 0f, inventoryPercent = 0f, inventoryMass = 0f;

        PDСontrollerF RollPID = new PDСontrollerF(1f, 0.1f);
        PDСontrollerF PitchPID = new PDСontrollerF(1f, 0.1f);

        //==========================================================================================================

        private string init_message;
        private bool init_error = false;

        public Program()
        {

            StringBuilder sb = new StringBuilder();
            LCD = FindDisplay(DisplayName, GridTerminalSystem, Me);
            if (LCD == null)
            {
                LCD = Me.GetSurface(0);
                sb.AppendLine("Дисплей не обнаружен!");
            }

            Cockpit = FindBlockOfType<IMyShipController>(CockpitName, GridTerminalSystem, Me);
            if (Cockpit == null)
            {
                sb.AppendLine("Кокпит не обнаружен!");
                init_error = true;
            }
            else
            {

                List<IMyGyro> tempList = FindAllBlocksOfType<IMyGyro>(GridTerminalSystem, Me);
                SetupGyros(Cockpit, tempList, GyroDefList);
                if (GyroDefList.Count == 0)
                {
                    sb.AppendLine("Гироскопы не найдены!");
                    init_error = true;
                }

                Hovers = FindAllBlocksOfType<IMyThrust>(GridTerminalSystem, Me);
                if (Hovers.Count == 0)
                {
                    sb.AppendLine("Ховеры не найдены!");
                    init_error = true;
                }
            }

            IMyInventory invent;
            List<IMyCargoContainer> tmpCargo = FindAllBlocksOfType<IMyCargoContainer>(GridTerminalSystem, Me);
            tmpCargo.ForEach(b =>
            {
                invent = b.GetInventory();
                InventoryList.Add(invent);
                inventoryTotalVolume += (float)invent.MaxVolume;
            });
            List<IMyShipDrill> tmpDrills = FindAllBlocksOfType<IMyShipDrill>(GridTerminalSystem, Me);
            tmpDrills.ForEach(b =>
            {
                invent = b.GetInventory();
                InventoryList.Add(invent);
                //inventoryTotalVolume += (float)invent.MaxVolume;
            });
            List<IMyShipConnector> tmpConn = FindAllBlocksOfType<IMyShipConnector>(GridTerminalSystem, Me);
            tmpConn.ForEach(b =>
            {
                invent = b.GetInventory();
                InventoryList.Add(invent);
                //inventoryTotalVolume += (float)invent.MaxVolume;
            });
            List<IMyRefinery> tmpRef = FindAllBlocksOfType<IMyRefinery>(GridTerminalSystem, Me);
            tmpRef.ForEach(b =>
            {
                invent = b.GetInventory();
                InventoryList.Add(invent);
                //inventoryTotalVolume += (float)invent.MaxVolume;
            });
            if (InventoryList.Count == 0)
            {
                sb.AppendLine("Инвентари не найдены!");
            }

            if (!init_error)
            {
                sb.AppendLine("Всё ОК");
            }

            init_message = sb.ToString();
            Echo(init_message);
            if (init_error)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                if (LCD != null) LCD.WriteText(init_message);
                return;
            }

            DesiredAltitude = Hovers[0].GetValue<float>("altitudemin_slider_L");

            Runtime.UpdateFrequency = UpdFreq;
        }
        public void Main(string argument, UpdateType updateSource)
        {

            //Echo(init_message);
            if (init_error)
            {
                if (LCD != null) LCD.WriteText(init_message);
                Echo(init_message);
                return;
            }

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
                    case "setspeed":
                        float spd;
                        if (float.TryParse(cmd_param, out spd))
                        {
                            DesiredSpeed = spd;
                        }
                        break;

                    case "stop":
                        DesiredSpeed = 0f;
                        break;

                    case "cruise":
                        CruiseControl = !CruiseControl;
                        MustSetSpeed = CruiseControl;
                        break;

                    case "control":
                        UnderControl = !UnderControl;
                        if (UnderControl)
                        {
                            PitchPID.Reset();
                            RollPID.Reset();
                        }
                        else
                        {
                            GyroDefList.ForEach(g => SetGyroParams(g, false));
                        }
                        break;

                    default:
                        break;
                }

                //return;
            }

            if ((updateSource & UpdateType.Update1) == 0 && (updateSource & UpdateType.Update10) == 0) return;

            float YawControll = Cockpit.RotationIndicator.Y * RotationFactor;

            Vector3D PlanetUp = -Vector3D.Normalize(Cockpit.GetNaturalGravity());
            Vector3D PlanetForward = -Vector3D.Normalize(Vector3D.Cross(Cockpit.WorldMatrix.Right, PlanetUp));
            MatrixD PlanetMatrix = MatrixD.CreateWorld(Cockpit.WorldMatrix.Translation, PlanetForward, PlanetUp);
            MatrixD TransposedPlanetMatrix = MatrixD.Transpose(PlanetMatrix);

            Vector3D WorldSpeed = Cockpit.GetShipVelocities().LinearVelocity;
            Vector3D PlanetSpeed = Vector3D.Rotate(WorldSpeed, TransposedPlanetMatrix);

            float desSpeed = 0;
            if (UnderControl)
            {
                if (CruiseControl)
                {
                    if (MustSetSpeed)
                    {
                        MustSetSpeed = false;
                        DesiredSpeed = (float)PlanetSpeed.Z;
                    }

                    DesiredSpeed += Cockpit.MoveIndicator.Z * KEY_PUSH_DELTA_SPEED;
                    if (Math.Abs(DesiredSpeed) < 0.5f * KEY_PUSH_DELTA_SPEED) DesiredSpeed = 0f;

                    desSpeed = DesiredSpeed;
                }
                else
                {
                    desSpeed = Cockpit.MoveIndicator.Z * MAX_SIDE_SPEED;
                }

                float DesiredSideSpeed = (float)Math.Sign(Cockpit.MoveIndicator.X) * MAX_SIDE_SPEED;

                Vector3D CockpitFwd = Vector3D.Rotate(Cockpit.WorldMatrix.Forward, TransposedPlanetMatrix);
                Vector3D CockpitRight = Vector3D.Rotate(Cockpit.WorldMatrix.Right, TransposedPlanetMatrix);

                Vector3D ForwardInYZPlane = Vector3D.Normalize(new Vector3D(0f, CockpitFwd.Y, CockpitFwd.Z));
                float PitchAngle = (float)(GetDirectionAngles(ForwardInYZPlane).Y);

                Vector3D RightInYXPlane = Vector3D.Normalize(new Vector3D(0f, CockpitRight.Y, CockpitRight.X));
                float RollAngle = NormalizeAngle(MathHelper.Pi - (float)(GetDirectionAngles(RightInYXPlane).Y));

                float ForwSpeedErr = MathHelper.Clamp((desSpeed - (float)PlanetSpeed.Z) * SPEED_ERROR_FACTOR, -1f, 1f);
                float SideSpeedErr = MathHelper.Clamp((DesiredSideSpeed - (float)PlanetSpeed.X) * SPEED_ERROR_FACTOR, -1f, 1f);
                float YawErr = MathHelper.Clamp(YawControll, -1f, 1f);

                ForwSpeedErr = (float)Math.Sqrt(Math.Abs(ForwSpeedErr)) * (float)Math.Sign(ForwSpeedErr);
                SideSpeedErr = (float)Math.Sqrt(Math.Abs(SideSpeedErr)) * (float)Math.Sign(SideSpeedErr);

                float desAnglePith = ForwSpeedErr * MAX_ANGLE;
                float desAngleRoll = -SideSpeedErr * MAX_ANGLE;

                float angleErrPith = MathHelper.Clamp(desAnglePith - PitchAngle, -1f, 1f);
                float angleErrRoll = MathHelper.Clamp(desAngleRoll - RollAngle, -1f, 1f);

                float SignalPitch = -GYRO_FACTOR * MathHelper.Clamp(PitchPID.Reaction(angleErrPith), -1f, 1f);
                float SignalRoll = -GYRO_FACTOR * MathHelper.Clamp(RollPID.Reaction(angleErrRoll), -1f, 1f);
                float SignalYaw = GYRO_FACTOR * YawErr;

                GyroDefList.ForEach(g => SetGyroParams(g, true, SignalPitch, SignalYaw, SignalRoll));
            }

            if (Cockpit.MoveIndicator.Y != 0f)
            {
                DesiredAltitude += Cockpit.MoveIndicator.Y * KEY_PUSH_DELTA_ALTITUDE;
                Hovers.ForEach(h => h.SetValue<float>("altitudemin_slider_L", DesiredAltitude));
                DesiredAltitude = Hovers[0].GetValue<float>("altitudemin_slider_L");
            }

            inventoryUpdateCounter++;
            if (inventoryUpdateCounter >= INV_UPD_INTERVAL)
            {
                inventoryUpdateCounter = 0;

                inventoryUsedVolume = 0f;
                inventoryMass = 0f;
                InventoryList.ForEach(inv =>
                {
                    inventoryUsedVolume += (float)inv.CurrentVolume;
                    inventoryMass += (float)inv.CurrentMass;
                });

                inventoryPercent = inventoryUsedVolume / inventoryTotalVolume * 100f;
                inventoryMass /= 1000f;
            }

            LCD.WriteText(string.Format("Упр: {0}  Круиз: {1}\n", UnderControl ? "ВКЛ" : "---", CruiseControl ? "ВКЛ" : "---"));
            if (CruiseControl)
                LCD.WriteText(string.Format("Скор: {0:#0.0} / {1:#0.0},  Боковая: {2:#0.0}\n", -PlanetSpeed.Z, -desSpeed, PlanetSpeed.X), true);
            else
                LCD.WriteText(string.Format("Скор: {0:#0.0},  Боковая: {1:#0.0}\n", -PlanetSpeed.Z, PlanetSpeed.X), true);

            LCD.WriteText(string.Format("Высота: {0:#0.0}\n", DesiredAltitude), true);
            LCD.WriteText(string.Format("Груз: {0:#0} тн, {1:#0} %{2}\n", inventoryMass, inventoryPercent, inventoryPercent > 90f ? " !!!" : ""), true);

        }

        #region Helpers

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
        private static void SetupGyros(IMyShipController OrientationCockpit, List<IMyGyro> GyrosList, List<GyroDefinition> GyroDefsList)
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
        private static void SetGyroParams(GyroDefinition gyroDef, bool GyroOverride = false, float pitchValue = 0f, float yawValue = 0f, float rollValue = 0f)
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

        private class PDСontrollerF
        {

            public float factorP = 1.0f;
            public float factorD = 1.0f;
            private float mFrequency = 6f;
            private float timeFactor = 1.0f / 6.0f;

            private float prevErr = 0;

            public float channelP = 0;
            public float channelD = 0;
            public float OutValue = 0;

            public PDСontrollerF()
            {
            }
            public PDСontrollerF(float factP, float factD)
            {
                factorP = factP;
                factorD = factD;
            }

            public float Frequency
            {
                get { return mFrequency; }
                set
                {
                    mFrequency = value;
                    timeFactor = 1.0f / mFrequency;
                }
            }

            public float Reaction(float err)
            {

                channelP = factorP * err;
                float dErr = err - prevErr;
                prevErr = err;
                // D = dErr/dT = dErr * freq
                channelD = factorD * dErr * mFrequency;

                float absP = Math.Abs(channelP);
                if (Math.Abs(channelD) > absP) channelD = absP * Math.Sign(channelD);

                OutValue = channelP + channelD;
                return OutValue;
            }
            public void Reset()
            {

                channelP = 0;

                prevErr = 0;
                channelD = 0;
            }

        }

        private static Vector2D         (Vector3D Direction)
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
        private static float NormalizeAngle(float SourseAngle)
        {
            float angle = SourseAngle;

            if (angle >= MathHelper.Pi) angle -= MathHelper.TwoPi;
            if (angle <= -MathHelper.Pi) angle += MathHelper.TwoPi;

            return angle;
        }

        #endregion

    }
}