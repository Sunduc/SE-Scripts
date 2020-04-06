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
namespace Graboid
{
    public sealed class Program : MyGridProgram
    {
        // НАЧАЛО СКРИПТА
        //содержит куски кода andrukha74#3658 (diskord)
        const string LCDName = "LHV-1 Cockpit(0)";                      //если пусто, используется LCD прог.блока
        //const string CockpitName = "";                  //если пусто, не используется

        const string HOVER_TAG = "Hover";
        const float KV = 0.01f;
        const float KI = 0.25f;

        IMyShipController Cockpit;
        IMyTextSurface LCD, ProgBlockLCD;
        List<IMyThrust> HoverList;
        List<IMyGyro> GyroList;

        private string init_message;
        private bool init_error = false;

        private enum PilotModeEnum
        {
            FreeMove,
            Dampeners,
            Cruise
        }
        PilotModeEnum PilotMode = PilotModeEnum.Dampeners;
        private float cruisespeed;
        private float altitude;
        private double SealevelAlt;
        private double SurfaceAlt;

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
        const double DisplayUpdateFreq = (1d / 10d) / (double)DisplayUpdateInterval * 1000d;
        double runTime = 0;

        private bool initedFlag = false;
        private int initTicksCounter = 0;
        private int initTicksWait;

        public Program()
        {
            //allBlocks = new List<IMyTerminalBlock>();
            //ThrusterList = new List<IMyThrust>();
            //HoverList = new List<IMyThrust>();
            //GyroList = new List<IMyGyro>();

            //GridTerminalSystem.GetBlocks(allBlocks);
            //foreach (IMyTerminalBlock block in allBlocks)
            //{
            //    IMyThrust thruster = block as IMyThrust;
            //    if (thruster != null)
            //    {
            //        if (thruster.CustomName.Contains("Hover")) HoverList.Add(thruster);
            //    }
            //    //IMyGyro gyro = block as IMyGyro;
            //    //if (gyro != null) GyroList.Add(gyro);
            //}
            //GridTerminalSystem.GetBlocksOfType<IMyGyro>(GyroList);
            //Cockpit = GridTerminalSystem.GetBlockWithName("Cockpit") as IMyShipController;
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

            HoverList = FindAllBlocksOfType<IMyThrust>(GridTerminalSystem, Me, HOVER_TAG);
            GyroList = FindAllBlocksOfType<IMyGyro>(GridTerminalSystem, Me);
            Cockpit = FindDefaultCockpit(GridTerminalSystem, Me);

            if (HoverList.Count == 0) { init_error = true; sb.AppendLine("Ховеры не обнаружены!"); } else sb.AppendFormat("Ховеров: {0}\n", HoverList.Count);
            if (GyroList.Count == 0) { init_error = true; sb.AppendLine("Гироскопы не обнаружены!"); } else sb.AppendFormat("Гироскопов: {0}\n", GyroList.Count);

            init_message = sb.ToString();

            Echo(init_message);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;

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

            ToLog("", false, false);

            Vector3D GravityVector = Cockpit.GetNaturalGravity();
            Vector3D GravityNorm = Vector3D.Normalize(GravityVector);

            Vector3D VelocityVector = Cockpit.GetShipVelocities().LinearVelocity;
            Vector3D VelocityVectorNorm = Vector3D.Normalize(VelocityVector);

            Vector3D ForwardVectorNorm = Vector3D.Normalize(Vector3D.Reject(Cockpit.WorldMatrix.Forward, GravityNorm));
            Vector3D LeftVectorNorm = Vector3D.Normalize(Vector3D.Reject(Cockpit.WorldMatrix.Left, GravityNorm));

            Vector3D ForwardInput = ForwardVectorNorm * Cockpit.MoveIndicator.Z;
            Vector3D LeftInput = LeftVectorNorm * Cockpit.MoveIndicator.X;

            Vector3D ForwardVelocity = Vector3D.ProjectOnVector(ref VelocityVector, ref ForwardVectorNorm);
            Vector3D LeftVelocity = Vector3D.ProjectOnVector(ref VelocityVector, ref LeftVectorNorm);

            float UpInput = Cockpit.MoveIndicator.Y / 10;

            Vector3D AlignVector = Vector3D.Zero;

            /*if (!string.IsNullOrEmpty(argument))
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
                }*/

                switch (argument)
                {
                    case "Dampeners":
                        {
                            PilotMode = PilotModeEnum.Dampeners;
                            break;
                        }
                    case "Cruise":
                        {
                            PilotMode = PilotModeEnum.Cruise;
                            if (ForwardVectorNorm == Vector3D.Normalize(ForwardVelocity)) cruisespeed = -(float)ForwardVelocity.Length();
                            else
                                cruisespeed = (float)ForwardVelocity.Length();
                            break;
                        }
                    case "FreeMove":
                        {
                            PilotMode = PilotModeEnum.FreeMove;
                            break;
                        }
                    case "Drill":
                        {
                            PilotMode = PilotModeEnum.Cruise;
                            cruisespeed = 3f;
                            altitude = 4f;
                            foreach (IMyThrust hover in HoverList)
                            {
                                hover.SetValue<float>("altitudemin_slider_L", altitude);
                            }
                        break;
                        }
                    case "Down":
                        {
                            PilotMode = PilotModeEnum.Cruise;
                            cruisespeed = 5f;
                            altitude = 5f;
                            foreach (IMyThrust hover in HoverList)
                            {
                                hover.SetValue<float>("altitudemin_slider_L", altitude);
                            }
                            break;
                        }
                default:
                        break;
                }
            //}

            if (!Vector3D.IsZero(LeftInput)) AlignVector += LeftInput * KI;
            else
                if (PilotMode != PilotModeEnum.FreeMove) AlignVector += LeftVelocity * KV;

            if (!Vector3D.IsZero(ForwardInput))
            {
                if (PilotMode == PilotModeEnum.Cruise)
                {
                    cruisespeed += -Cockpit.MoveIndicator.Z / 10;
                    AlignVector += (ForwardVelocity - ForwardVectorNorm * cruisespeed) * KV;
                }
                else
                    AlignVector += ForwardInput * KI;
            }
            else
                if (PilotMode == PilotModeEnum.Cruise) AlignVector += (ForwardVelocity - ForwardVectorNorm * cruisespeed) * KV;
            else
                    if (PilotMode != PilotModeEnum.FreeMove) AlignVector += ForwardVelocity * KV;
            AlignVector += GravityNorm;



            float PitchInput = -(float)AlignVector.Dot(Cockpit.WorldMatrix.Forward);
            float RollInput = (float)AlignVector.Dot(Cockpit.WorldMatrix.Left);
            float YawInput = Cockpit.RollIndicator;

            foreach (IMyThrust hover in HoverList)
            {
                altitude = hover.GetValue<float>("altitudemin_slider_L");
                altitude += UpInput;
                hover.SetValue<float>("altitudemin_slider_L", altitude);
            }

            foreach (IMyGyro gyro in GyroList)
            {
                gyro.GyroOverride = true;
                gyro.Yaw = YawInput;
                gyro.Roll = RollInput;
                gyro.Pitch = PitchInput;
            }
            DisplayUpdateCounter += 1;
            if (DisplayUpdateCounter >= DisplayUpdateInterval)
            {

                ProgBlockLCD.WriteText("Cycle time, µs: " + (runTime * DisplayUpdateFreq).ToString("#0.0") + "\n");
                Echo("Cycle time, µs: " + (runTime * DisplayUpdateFreq).ToString("#0.0") + "\n" + init_message);
                DisplayUpdateCounter = 0;
                runTime = 0d;

                Cockpit.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out SealevelAlt);
                Cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out SurfaceAlt);
                ProgBlockLCD.WriteText("Режим: " + PilotModeToString() + "\n", true);
                ProgBlockLCD.WriteText(string.Format("cruisespeed: {0:#0.0}\n", cruisespeed), true);
                ProgBlockLCD.WriteText(string.Format("FV: {0:#0.0} / LV: {1:#0.0} / V: {2:#0.0}\n", ForwardVelocity.Length(), LeftVelocity.Length(), VelocityVector.Length()), true);
                ProgBlockLCD.WriteText(string.Format("Alt: {0:#0.0} / SLA: {1:#0.0} / SFA: {2:#0.0}\n", altitude, SealevelAlt, SurfaceAlt), true);

                if (LCD != null)
                {
                    string t = ProgBlockLCD.GetText();
                    LCD.WriteText(t);
                }
            }
            runTime += Runtime.LastRunTimeMs;
        }

        public void Save()
        { }
        private string PilotModeToString()
        {
            switch (PilotMode)
            {
                case PilotModeEnum.Cruise:
                    return "Круиз";
                case PilotModeEnum.Dampeners:
                    return "Гасители Вкл";
                case PilotModeEnum.FreeMove:
                    return "Гасители Выкл";
                default:
                    return PilotMode.ToString();
            }
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

        #endregion

        // КОНЕЦ СКРИПТА
    }
}