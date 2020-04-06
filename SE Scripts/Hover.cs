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
namespace Hover
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
        List<IMyCargoContainer> CargoList;

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

            Random rnd = new Random();
            initTicksWait = 1;

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

            StringBuilder sb = new StringBuilder();

            DebugLCD = FindDisplay(DEBUG_LCD_NAME, GridTerminalSystem, Me);
            ToLog("", false, false);

            HoverList = FindAllBlocksOfType<IMyThrust>(GridTerminalSystem, Me, HOVER_TAG);
            GyroList = FindAllBlocksOfType<IMyGyro>(GridTerminalSystem, Me);
            Cockpit = FindDefaultCockpit(GridTerminalSystem, Me);
            CargoList = FindAllBlocksOfType<IMyCargoContainer>(GridTerminalSystem, Me, "Cargo");

            if (!string.IsNullOrEmpty(LCDName))
            {
                LCD = FindDisplay(Cockpit.CustomName + "(0)", GridTerminalSystem, Me);
            }
            ProgBlockLCD = Me.GetSurface(0);

            if (LCD == null) sb.AppendLine("Дисплей не обнаружен");
            
            if (HoverList.Count == 0) { init_error = true; sb.AppendLine("Ховеры не обнаружены!"); } else sb.AppendFormat("Ховеров: {0}\n", HoverList.Count);
            if (GyroList.Count == 0) { init_error = true; sb.AppendLine("Гироскопы не обнаружены!"); } else sb.AppendFormat("Гироскопов: {0}\n", GyroList.Count);
            if (CargoList.Count == 0) { init_error = true; sb.AppendLine("Контейнеры не обнаружены!"); } else sb.AppendFormat("Контейнеров: {0}\n", CargoList.Count);

            init_message = sb.ToString();

            Echo(init_message);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;

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
            Vector3D LeftVectorNorm = Vector3D.Normalize(ForwardVectorNorm.Cross(GravityNorm));

            Vector3D ForwardInput = ForwardVectorNorm * Cockpit.MoveIndicator.Z;
            Vector3D LeftInput = LeftVectorNorm * Cockpit.MoveIndicator.X;

            Vector3D ForwardVelocity = Vector3D.ProjectOnVector(ref VelocityVector, ref ForwardVectorNorm);
            Vector3D LeftVelocity = Vector3D.ProjectOnVector(ref VelocityVector, ref LeftVectorNorm);

            float UpInput = Cockpit.RollIndicator / 10;            
            
            Vector3D AlignVector = Vector3D.Zero;
                     
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
                        if ((ForwardVectorNorm + ForwardVelocity).Length() > ForwardVelocity.Length()) cruisespeed = (float)ForwardVelocity.Length();
                        else
                            cruisespeed = -(float)ForwardVelocity.Length();                        
                        break;
                    }
                case "FreeMove":
                    {
                        PilotMode = PilotModeEnum.FreeMove;
                        break;
                    }
                default:
                    break;
            }

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
            float YawInput = Cockpit.RotationIndicator.Y;
                        
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

                ProgBlockLCD.WriteText("\n" + "\n" + "\n" + "Cycle time, µs: " + (runTime * DisplayUpdateFreq).ToString("#0.0") + "\n");
                Echo("Cycle time, µs: " + (runTime * DisplayUpdateFreq).ToString("#0.0") + "\n" + init_message);
                DisplayUpdateCounter = 0;
                runTime = 0d;

                ProgBlockLCD.WriteText(PilotModeToString(), true);

                string speed = string.Format("Скорость: {0:#0.0} / ", VelocityVector.Length());
                if ((ForwardVectorNorm + ForwardVelocity).Length() > ForwardVelocity.Length())
                    speed += string.Format("Вперед: {0:#0.0} / ", ForwardVelocity.Length());
                else
                    speed += string.Format("Назад:  {0:#0.0} / ", ForwardVelocity.Length());
                if ((LeftVectorNorm + LeftVelocity).Length() > LeftVelocity.Length())
                    speed += string.Format("Влево:  {0:#0.0}\n", LeftVelocity.Length());
                else
                    speed += string.Format("Вправо: {0:#0.0}\n", LeftVelocity.Length());
                ProgBlockLCD.WriteText(speed, true);

                double sla;
                double sfa;
                Cockpit.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out sla);
                Cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out sfa);
                ProgBlockLCD.WriteText(string.Format("Высота: пар. {0:#0.0} / ур. м. {1:#0.0} / пов. {2:#0.0}\n", altitude, sla, sfa), true);

                float thrust = 0;
                foreach (IMyThrust hover in HoverList)
                {
                    thrust += hover.MaxThrust;
                }
                float curmass = Cockpit.CalculateShipMass().PhysicalMass;
                float maxmass = thrust / (float)Cockpit.GetNaturalGravity().Length() * 3f;
                ProgBlockLCD.WriteText(string.Format("Груз: {0:# ### ###} кг из {1:# ### ###} кг / {2: ##.# %}\n", curmass, maxmass, curmass / maxmass), true);

                float curvol = 0;
                float maxvol = 0;
                IMyInventory inv;
                foreach (IMyCargoContainer cargo in CargoList)
                {
                    inv = cargo.GetInventory();
                    curvol += (float)inv.CurrentVolume;
                    maxvol += (float)inv.MaxVolume;
                }
                ProgBlockLCD.WriteText(string.Format("Трюм: {0:# ### ###} л из {1:# ### ###} л / {2: ##.# %}\n", curvol * 1000, maxvol * 1000, curvol / maxvol), true);
               
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
                    return string.Format("Круиз: {0:#0.0} м/с\n", cruisespeed);
                case PilotModeEnum.Dampeners:
                    return "Гасители инерции: Вкл\n";
                case PilotModeEnum.FreeMove:
                    return "Гасители инерции: Выкл\n";
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