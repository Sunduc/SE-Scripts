/* v:0.973 (01.185 game compatibility) 
In-Game script by MMaster & Morphologis  
 
 * Last update: Fixed timing to improve airlock response times 
 *  Updated for 01.185 version of game 
 *  Timer block is not required anymore! 
 *  
 * Previous updates: Sound bug fixed! 
 *  Added option to open the doors even if the pressure is not right (bottom of the Configuration() below) 
 *  CT: option works again (some modded doors report incorrect OpenRatio so you can customize the closing time) 
 * Airlocks can be controlled by using Programmable Block argument 
 * Automatically figure out when doors are fully closed 
 * Improve detection of depressurize/pressurize of airvent 
 * Make colors configurable in script config & made it smarter when pressure changes  
 * Closing time per door & Airvents in inner and outer groups 
* (look at extra features section below)  
  
Intelligent airlock system. Automatically controls any number of airlocks on ships and stations.   
  
Any number of airlocks using single programmable block & timer  
Any number of doors per airlock (can be used with Hangar Doors)  
Any number of airvents per airlock (so you can use it for large airlocks)  
Automatic pressurization / depressurization  
Doors automatically closed & locked / unlocked & opened  
Lights changing colors based on state of airlock  
Play sound blocks when airlock is pressurized, depressurized or in both cases  
LCD display support to display state of any number of airlocks on any LCD  
Unlocks doors without opening when airlock can't be depressurized / pressurized  
3 button actions support: transfer to inner doors, outer doors or switch between them  
Airlock can be also controlled by switching 'Depressurize' on airvents  
   
NO PROGRAMMING NEEDED.  
  
VIDEO GUIDE BY MORPHOLOGIS:  
 * https://www.youtube.com/watch?v=viWsqLvqkAM  
   
QUICK GUIDE:  
 * Load script to programmable block, Check code, Remember & Exit 
 * Timer Block is not required anymore! 
 
Setup 3 groups per airlock like this:  
 Group named "Airlock Control: Airlock name" should contain:  
 - airvents inside airlock  
 - lcds that should show this airlock state [optional]  
   
 Group named "Airlock Inner: Airlock name" should contain:  
 - inner doors (opened when airlock is pressurized)  
 - lights for showing inner door status [optional]  
 - sound block played when airlock is being pressurized [optional]  
   
 Group named "Airlock Outer: Airlock name" should contain:  
 - outer doors (opened when airlock is depressurized)  
 - lights for showing outer door status [optional]  
 - sound block played when airlock is being depressurized [optional]  
   
HINT:  
You can build single sound block and add it to both Inner and Outer group  
to play sound each time airlock changes state (e.g. hissing sound).  
  
  
Setup buttons to switch Depressurize on airvents you added to Control group.  
 NOTE: this is much less effective than using the control light, but it works  
  
 OR  
 
Setup buttons to Run Programmable block with argument (without quotes!): 
 "Airlock name in" - transfer to inner doors (pressurize) 
 "Airlock name out" - transfer to outer doors (depressurize)  
 "Airlock name toggle" - switch between inner doors / outer doors 
  
 * Done.  
  
EXTRA FEATURES  
 Change color of lights in script LIGHT COLOR SETTINGS right below this description.   
   
 Place airvent behind inner doors to Airlock Inner group and/or airvent outside of outer doors to Airlock Outer group   
 to let airlock automatically figure out what should be the pressure when opening inner and/or outer doors.  
  
 Add closing time to doors name to make sure doors are closed before locking them by adding "CT:seconds":  
 * if your doors are named "Hangar Door 3" rename them to "Hangar Door 3 CT:10" to set closing time to 10 seconds  
 * if your doors are named "Door 3" rename them to "Door 3 CT:5" to set closing time to 5 seconds  
 
 Tell the script to open the doors after some time even if pressure is not right in the airlock: 
 * change line "MMConfig.OPEN_ANYWAY = false;" in Configuration() section at the beginning of the script  
 * to "MMConfig.OPEN_ANYWAY = true;" (without quotes) 
  
RECOMMENDED sound pack made by Tartaross  
Tartaross Inc. A.I. Soundpack #2: Community Wishlist  
http://steamcommunity.com/sharedfiles/filedetails/?id=431231192  
  
VERY SPECIAL THANKS  
Morphologis for this idea, his help with debugging this script and for his support of community & awesome videos!  
Make sure to check out his YouTube channel:  
https://www.youtube.com/user/Morphologis  
  
Also thanks to Direwolf20 for fix to depressurization on load issue. 
 
Also look at Deep Space Nation reddit page:  
https://www.reddit.com/r/DeepSpaceNation  
  
  
Watch MMaster's Steam group: http://steamcommunity.com/groups/mmnews  
Youtube Channel: https://www.youtube.com/user/MattsPlayCorner1080p  
Twitter: https://twitter.com/MattsPlayCorner  
and Facebook: https://www.facebook.com/MattsPlayCorner1080p  
for more crazy stuff in the future :)  
 */

void Configuration()
{
    /* LIGHT COLOR SETTINGS  
     *   
     * 1. number is red color from 0.0 (black) to 1.0 (red)  
     * 2. number is green color from 0.0 (black) to 1.0 (green)  
     * 3. number is blue color from 0.0 (black) to 1.0 (blue)  
     *   
     * Examples:  
     *   Color(0.5f, 0.0f, 0.0f) is dark red  
     *   Color(0.0f, 0.5f, 0.0f) is dark green  
     *   Color(1.0f, 1.0f, 1.0f) is white color  
     *   Color(1.0f, 1.0f, 0.0f) is yellow  
     *   Color(1.0f, 0.5f, 0.0f) is orange  
     *   Color(0.0f, 1.0f, 1.0f) is cyan  
     *   
     * change only the numbers (leave f at the end of each number) */

    // color when doors are locked  
    MMConfig.LOCK_COLOR = new Color(1.0f, 0.0f, 0.0f);
    // color when pressure is wrong but doors are unlocked  
    MMConfig.WARN_COLOR = new Color(1.0f, 0.5f, 0.0f);
    // color when doors are open  
    MMConfig.OPEN_COLOR = new Color(0.0f, 1.0f, 0.0f);


    // Group name must start with this to be considered by this script (case insensitive) 
    MMConfig.GROUP_TAG = "airlock";

    // Colon needs to stay at the end  
    MMConfig.INNER_TAG = "inner:";
    MMConfig.OUTER_TAG = "outer:";
    MMConfig.CONTROL_TAG = "control:";

    // Should we completely open the doors even if pressure is not ok? 
    MMConfig.OPEN_ANYWAY = false;
}

// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  
// DO NOT MODIFY ANYTHING BELOW THIS  
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  

// (for developer) Enable debug to antenna or LCD marked with [DEBUG]  
public static bool EnableDebug = false;


Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

void Main(string argument)
{
    Configuration();

    // Init MMAPI and debug panels marked with [DEBUG]  
    MM.Init(GridTerminalSystem, EnableDebug);

    AirlockControlProgram prog = new AirlockControlProgram();
    prog.Run(argument);
}  
}  
  
  
public static class MMConfig
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

public class MMAirlock
{
    public List<IMyAirVent> airVents = new List<IMyAirVent>();
    public List<IMyDoor> innerDoors = new List<IMyDoor>();
    public List<IMySoundBlock> innerSound = new List<IMySoundBlock>();

    public List<IMyLightingBlock> innerLights = new List<IMyLightingBlock>();
    public List<IMyDoor> outerDoors = new List<IMyDoor>();
    public List<IMySoundBlock> outerSound = new List<IMySoundBlock>();

    public List<IMyLightingBlock> outerLights = new List<IMyLightingBlock>();
    public List<MMPanel> lcds = new List<MMPanel>();

    public IMyLightingBlock control = null;
    public string command = "";

    public const int ClosingTimeDefault = 3;
    public const int FullTimeThreshold = 3;
    public const int EmptyTimeThreshold = 3;
    public const int DepressurizeMaxTime = 20;
    public const int PressurizeMaxTime = 20;

    public float OuterPressure = 0f;
    public float InnerPressure = 0.99f;

    public int WantedState = 0;
    public int CurrentState = 1;
    public bool WasDepressurizing = false;

    public int ClosingTimer = -1;
    public int FullTime = 0;
    public int EmptyTime = 0;
    public int ChangingTime = 0;
    public int TimeSinceChange = 0;

    public bool OuterOpen = false;
    public bool InnerOpen = false;

    public double lowestPressure = 100.0f;

    public string name = "Airlock";

    public void Reset()
    {
        airVents.Clear();
        innerDoors.Clear();
        innerLights.Clear();
        outerDoors.Clear();
        outerLights.Clear();
        innerSound.Clear();
        outerSound.Clear();
        lcds.Clear();
        control = null;
        InnerOpen = false;
        OuterOpen = false;
        lowestPressure = 100.0f;
        OuterPressure = 0f;
        InnerPressure = 0.99f;
        command = "";
    }

    private void SetLight(List<IMyLightingBlock> lights, Color c, float BlinkInt = 0f, float BlinkLen = 100f, float BlinkOff = 0f)
    {
        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i].GetProperty("Color").AsColor().GetValue(lights[i]) != c
                || BlinkInt != lights[i].BlinkIntervalSeconds
                || !lights[i].IsWorking)
            {
                if (!lights[i].IsWorking)
                {
                    lights[i].SetValue("Blink Interval", BlinkInt);
                    lights[i].SetValue("Blink Lenght", BlinkLen);
                    lights[i].SetValue("Blink Offset", BlinkOff);
                    lights[i].SetValue("Color", c);
                    lights[i].ApplyAction("OnOff_On");
                }
                else
                    lights[i].ApplyAction("OnOff_Off");
            }
        }
    }

    private void SetClose(List<IMyDoor> doors, bool closed)
    {
        for (int i = 0; i < doors.Count; i++)
        {
            if (closed == (doors[i].Status == DoorStatus.Closed) && closed == !doors[i].IsWorking)
                continue;

            if (!doors[i].IsWorking)
            {
                doors[i].ApplyAction("OnOff_On");
                continue;
            }

            if (closed && (doors[i].Status == DoorStatus.Open))
            {
                SetupClosingTimer(doors[i]);

                doors[i].ApplyAction("Open_Off");
                continue;
            }

            if (!closed && (doors[i].Status == DoorStatus.Closed))
            {
                SetupClosingTimer(doors[i]);

                doors[i].ApplyAction("Open_On");
                continue;
            }

            if (closed)
            {
                if ((!doors[i].CustomName.Contains(" CT:") && doors[i].Status == DoorStatus.Closed) ||
                    (doors[i].CustomName.Contains(" CT:") && ClosingTimer <= 0))
                {
                    doors[i].ApplyAction("OnOff_Off");
                }
                else
                {
                    if (!doors[i].CustomName.Contains(" CT:"))
                        ClosingTimer = (int)Math.Ceiling(ClosingTimeDefault * doors[i].OpenRatio) + 1;
                }
            }
        }
    }

    private void SetupClosingTimer(IMyTerminalBlock doors)
    {
        int default_time = (int)Math.Ceiling(ClosingTimeDefault * (doors as IMyDoor).OpenRatio) + 1;
        string ct = doors.CustomName;
        int idx = ct.IndexOf(" CT:");
        if (idx >= 0)
        {
            int val = -1;
            ct = ct.Substring(idx + 4).TrimStart(' ');
            idx = ct.IndexOf(' ');
            if (idx >= 0)
                ct = ct.Substring(0, idx);

            if (int.TryParse(ct, out val))
            {
                if (val > ClosingTimer)
                    ClosingTimer = val;
            }
            else
                if (default_time > ClosingTimer)
                ClosingTimer = default_time;
        }
        else
            if (default_time > ClosingTimer)
            ClosingTimer = default_time;
    }

    private void PlaySound(List<IMySoundBlock> sounds)
    {
        for (int i = 0; i < sounds.Count; i++)
        {
            sounds[i].ApplyAction("PlaySound");
        }
    }

    private void SetLock(List<IMyDoor> doors, bool locked)
    {
        string action = (locked ? "OnOff_Off" : "OnOff_On");
        for (int i = 0; i < doors.Count; i++)
            doors[i].ApplyAction(action);
    }

    private bool IsPressureOk(float wantedPressure)
    {
        if (wantedPressure <= 0.01f)
            return (EmptyTime >= EmptyTimeThreshold);
        else
            return (FullTime >= FullTimeThreshold);
    }

    private void SetLights(int state)
    {
        if ((Opening && !Stuck) || state == 0)
        {
            if (WantedState == 1)
                SetLight(innerLights, MMConfig.LOCK_COLOR, 2f, 50f);
            else
                SetLight(innerLights, MMConfig.LOCK_COLOR);
            if (WantedState == 2)
                SetLight(outerLights, MMConfig.LOCK_COLOR, 2f, 50f);
            else
                SetLight(outerLights, MMConfig.LOCK_COLOR);
            return;
        }

        switch (state)
        {
            case 1:
                if (!IsPressureOk(InnerPressure) && !InnerOpen)
                    SetLight(innerLights, MMConfig.WARN_COLOR);
                else
                    SetLight(innerLights, MMConfig.OPEN_COLOR);

                SetLight(outerLights, MMConfig.LOCK_COLOR);
                break;
            case 2:
                SetLight(innerLights, MMConfig.LOCK_COLOR);

                if (!IsPressureOk(OuterPressure) && !OuterOpen)
                    SetLight(outerLights, MMConfig.WARN_COLOR);
                else
                    SetLight(outerLights, MMConfig.OPEN_COLOR);
                break;
        }
    }

    private bool IsAVDepressurizing(IMyAirVent airvent)
    {
        return airvent.Depressurize;
    }

    private bool IsDepressurizing()
    {
        for (int i = 0; i < airVents.Count; i++)
            if (IsAVDepressurizing(airVents[i]))
                return true;

        return false;
    }

    private void Depressurize(bool on)
    {
        for (int i = 0; i < airVents.Count; i++)
        {
            if (on)
                airVents[i].ApplyAction("Depressurize_On");
            else
                airVents[i].ApplyAction("Depressurize_Off");
        }
    }

    private bool Opening = false;
    private bool Stuck = false;

    public void Process()
    {
        MM.Debug("Executing " + name);
        bool IsDep = IsDepressurizing();

        MM.Debug("Airlock: " + name + " IsDep: " + IsDep.ToString() + " WasDep: " + WasDepressurizing.ToString());
        MM.Debug("InnerOpen: " + InnerOpen.ToString() + " OuterOpen: " + OuterOpen.ToString());
        if (IsDep != WasDepressurizing
            || (TimeSinceChange > 40 && !Stuck &&
                ((InnerOpen && !IsPressureOk(InnerPressure)) ||
                 (OuterOpen && !IsPressureOk(OuterPressure)))))
        {
            if (!IsDep)
            {
                if (InnerPressure > 0.01f)
                    WantedState = 1;
                else
                    WantedState = 2;
            }
            else
            {
                if (OuterPressure <= 0.01f)
                    WantedState = 2;
                else
                    WantedState = 1;
            }
            if (CurrentState == WantedState && IsDep != WasDepressurizing)
                WantedState = (CurrentState == 1 ? 2 : 1);
            CurrentState = 0;
        }

        if (control != null)
        {
            // buttons  
            if (control.BlinkLength > 50f)
                WantedState = 1;
            else
                if (control.BlinkLength < 50f)
                WantedState = 2;

            control.SetValueFloat("Blink Lenght", 50f);

            // switch  
            if (control.BlinkOffset > 50f)
                WantedState = (WantedState == 2 ? 1 : 2);

            control.SetValueFloat("Blink Offset", 50f);
        }

        if (command != "")
        {
            switch (command)
            {
                case "in":
                    WantedState = 1;
                    break;
                case "out":
                    WantedState = 2;
                    break;
                case "toggle":
                    WantedState = (WantedState == 2 ? 1 : 2);
                    break;
            }
        }

        if (WantedState == 0)
            WantedState = (IsDep ? 2 : 1);

        if (WantedState != CurrentState)
        {
            CurrentState = 0;
            TimeSinceChange = 0;
        }

        if (ClosingTimer >= 0)
            ClosingTimer--;

        MM.Debug("WantSt: " + WantedState.ToString() + " CurSt: " + CurrentState.ToString());
        MM.Debug("Closing Timer: " + ClosingTimer.ToString());
        MM.Debug("FullTime: " + FullTime + " EmptyTime: " + EmptyTime);
        MM.Debug("ChangingTime: " + ChangingTime);
        MM.Debug("Inner: " + InnerPressure.ToString("F2") + " Outer: " + OuterPressure.ToString("F2"));
        MM.Debug("Opening: " + Opening.ToString() + " Stuck: " + Stuck.ToString());
        MM.Debug("LastChange: " + TimeSinceChange.ToString());

        if (CurrentState == 0 && ChangingTime > 0)
        {
            if (WantedState == 1 && IsPressureOk(InnerPressure))
            {
                CurrentState = 1;
                Opening = true;
                Stuck = false;
            }
            else
                if (WantedState == 2 && IsPressureOk(OuterPressure))
            {
                CurrentState = 2;
                Opening = true;
                Stuck = false;
            }
            else
                    if (WantedState == 2 && ChangingTime >= DepressurizeMaxTime)
            {
                CurrentState = 2;
                Opening = true;
                Stuck = true;
            }
            else
                        if (WantedState == 1 && ChangingTime >= PressurizeMaxTime)
            {
                CurrentState = 1;
                Opening = true;
                Stuck = true;
            }
        }

        switch (CurrentState)
        {
            case 0:
                ChangingTime++;
                TimeSinceChange = 0;
                SetClose(innerDoors, true);
                SetClose(outerDoors, true);
                SetLights(0);
                break;
            case 1:
                ChangingTime = 0;
                SetClose(outerDoors, true);
                if (!InnerOpen && Opening)
                {
                    if (IsPressureOk(InnerPressure) || MMConfig.OPEN_ANYWAY)
                        SetClose(innerDoors, false);
                    else
                        SetLock(innerDoors, false);
                }
                else
                {
                    SetLock(innerDoors, false);
                    if (Stuck && IsPressureOk(InnerPressure))
                        Stuck = false;
                    Opening = false;
                }
                SetLights(1);
                break;
            case 2:
                ChangingTime = 0;
                SetClose(innerDoors, true);
                if (!OuterOpen && Opening)
                {
                    if (IsPressureOk(OuterPressure) || MMConfig.OPEN_ANYWAY)
                        SetClose(outerDoors, false);
                    else
                        SetLock(outerDoors, false);
                }
                else
                {
                    SetLock(outerDoors, false);
                    if (Stuck && IsPressureOk(OuterPressure))
                        Stuck = false;
                    Opening = false;
                }
                SetLights(2);
                break;
        }

        for (int lid = 0; lid < lcds.Count; lid++)
            MMLCDTextManager.Add(lcds[lid], name);

        if (lowestPressure >= 100f)
            lowestPressure = -1f;

        string pressure = (lowestPressure < 0 ? "[N/A]" : "[" + (lowestPressure * 100f).ToString("F0") + "%]");
        switch (WantedState)
        {
            case 0: // no change  
                for (int lid = 0; lid < lcds.Count; lid++)
                    MMLCDTextManager.AddRightAlign(lcds[lid], "!! " + pressure, MMPanel.LCD_LINE_WIDTH);
                break;
            case 1: // in (pressurize)  
                if (ClosingTimer <= 0)
                {
                    if (InnerPressure > 0.01f)
                    {
                        MM.Debug("Pressurize");
                        Depressurize(false);
                        IsDep = false;
                    }
                    else
                    {
                        MM.Debug("Depressurize");
                        Depressurize(true);
                        IsDep = true;
                    }
                }

                if (ClosingTimer > 0 || CurrentState == 0 || Opening)
                    if (Stuck)
                        for (int lid = 0; lid < lcds.Count; lid++)
                            MMLCDTextManager.AddRightAlign(lcds[lid], "!! IN " + pressure, MMPanel.LCD_LINE_WIDTH);
                    else
                        for (int lid = 0; lid < lcds.Count; lid++)
                            MMLCDTextManager.AddRightAlign(lcds[lid], ">> IN " + pressure, MMPanel.LCD_LINE_WIDTH);
                else
                    for (int lid = 0; lid < lcds.Count; lid++)
                        MMLCDTextManager.AddRightAlign(lcds[lid], "IN " + pressure, MMPanel.LCD_LINE_WIDTH);
                break;
            case 2: // out (decompress)  
                if (ClosingTimer <= 0)
                {
                    if (OuterPressure > 0.01f)
                    {
                        Depressurize(false);
                        MM.Debug("Pressurize");
                        IsDep = false;
                    }
                    else
                    {
                        Depressurize(true);
                        MM.Debug("Depressurize");
                        IsDep = true;
                    }
                }

                if (ClosingTimer > 0 || CurrentState == 0 || Opening)
                    if (Stuck)
                        for (int lid = 0; lid < lcds.Count; lid++)
                            MMLCDTextManager.AddRightAlign(lcds[lid], "!! OUT " + pressure, MMPanel.LCD_LINE_WIDTH);
                    else
                        for (int lid = 0; lid < lcds.Count; lid++)
                            MMLCDTextManager.AddRightAlign(lcds[lid], ">> OUT " + pressure, MMPanel.LCD_LINE_WIDTH);
                else
                    for (int lid = 0; lid < lcds.Count; lid++)
                        MMLCDTextManager.AddRightAlign(lcds[lid], "OUT " + pressure, MMPanel.LCD_LINE_WIDTH);
                break;
        }

        for (int lid = 0; lid < lcds.Count; lid++)
        {
            MMLCDTextManager.AddLine(lcds[lid], "");
            MMLCDTextManager.AddProgressBar(lcds[lid], Math.Max(0f, lowestPressure * 100f), lcds[lid].FULL_PROGRESS_CHARS);
            MMLCDTextManager.AddLine(lcds[lid], "");
        }


        if (ClosingTimer == 0)
        {
            ChangingTime = 0;

            if (CurrentState == 0)
                switch (WantedState)
                {
                    case 1:
                        PlaySound(innerSound);
                        break;
                    case 2:
                        PlaySound(outerSound);
                        break;
                }
        }

        if (!Stuck)
            TimeSinceChange++;
        WasDepressurizing = IsDep;
    }
}

public class AirlockControlProgram
{
    public static MMAirlockCollection airlocks = new MMAirlockCollection();
    public static Dictionary<string, MMPanel> panels = new Dictionary<string, MMPanel>();
    public List<MMPanel> panelList = new List<MMPanel>();

    public MMAirlock GetAirlock(string name)
    {
        MMAirlock airlock = null;
        if (airlocks.ContainsKey(name))
            airlock = airlocks.GetItem(name);
        else
        {
            airlock = new MMAirlock();
            airlock.name = name;
            airlocks.AddItem(name, airlock);
        }

        return airlock;
    }

    public MMPanel GetPanel(IMyTextPanel lcd)
    {
        MMPanel panel = null;
        string key = lcd.CustomName + lcd.GetPosition().ToString("F0") + lcd.NumberInGrid.ToString();
        if (panels.ContainsKey(key))
            panel = panels[key];
        else
        {
            panel = new MMPanel();
            panels.Add(key, panel);
        }

        if (!panel.panels.ContainsKey(key))
            panel.panels.AddItem(key, lcd);

        if (!panelList.Contains(panel))
            panelList.Add(panel);

        return panel;
    }

    public void AddGroup(IMyBlockGroup group)
    {
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

        MM.Debug("Group: '" + group.Name + "'");
        int seppos = group.Name.IndexOf(':');
        if (seppos < 0)
            return;

        string tag = group.Name.Substring(0, seppos + 1).ToLower();
        string name = (group.Name.Length > seppos + 1 ? group.Name.Substring(seppos + 1).Trim() : "");
        group.GetBlocks(blocks);

        if (tag == MMConfig.GROUP_TAG + ' ' + MMConfig.CONTROL_TAG)
        {
            MMAirlock airlock = GetAirlock(name);

            for (int i = 0; i < blocks.Count; i++)
            {
                IMyTerminalBlock block = blocks[i];
                IMyAirVent airvent = block as IMyAirVent;
                if (airvent != null)
                {
                    airlock.airVents.Add(airvent);
                    if (airvent.CanPressurize)
                    {
                        double oxyLevel = airvent.GetOxygenLevel();
                        if (oxyLevel < airlock.lowestPressure)
                            airlock.lowestPressure = oxyLevel;
                    }
                    else
                        airlock.lowestPressure = -1f;
                    continue;
                }

                IMyLightingBlock light = block as IMyLightingBlock;
                if (light != null)
                    airlock.control = light;

                IMyTextPanel lcd = block as IMyTextPanel;
                if (lcd != null)
                {
                    MMPanel panel = GetPanel(lcd);
                    airlock.lcds.Add(panel);
                }
            }

            MM.Debug("LowestPressure: " + airlock.lowestPressure.ToString("F0"));

            if (airlock.lowestPressure > 0.01f && airlock.lowestPressure >= Math.Max(airlock.InnerPressure, airlock.OuterPressure))
            {
                if (airlock.FullTime < MMAirlock.FullTimeThreshold)
                    airlock.FullTime++;
                airlock.EmptyTime = 0;
            }
            else
            {
                airlock.FullTime = 0;

                if (airlock.lowestPressure <= 0.01f)
                {
                    if (airlock.EmptyTime < MMAirlock.EmptyTimeThreshold)
                        airlock.EmptyTime++;
                }
                else
                    airlock.EmptyTime = 0;
            }

            return;
        }

        if (tag == MMConfig.GROUP_TAG + ' ' + MMConfig.INNER_TAG)
        {
            MMAirlock airlock = GetAirlock(name);

            for (int i = 0; i < blocks.Count; i++)
            {
                IMyDoor door = blocks[i] as IMyDoor;
                if (door != null)
                {
                    if (door.Status == DoorStatus.Open)
                        airlock.InnerOpen = true;
                    airlock.innerDoors.Add(door);
                    continue;
                }

                IMyLightingBlock light = blocks[i] as IMyLightingBlock;
                if (light != null)
                {
                    airlock.innerLights.Add(light);
                    continue;
                }

                IMySoundBlock sound = blocks[i] as IMySoundBlock;
                if (sound != null)
                {
                    airlock.innerSound.Add(sound);
                    continue;
                }

                IMyAirVent airvent = blocks[i] as IMyAirVent;
                if (airvent != null)
                {
                    float pres = (airvent.CanPressurize ? airvent.GetOxygenLevel() : -1f);
                    if (pres < airlock.InnerPressure)
                        airlock.InnerPressure = pres;
                    continue;
                }
            }
            return;
        }

        if (tag == MMConfig.GROUP_TAG + ' ' + MMConfig.OUTER_TAG)
        {
            MMAirlock airlock = GetAirlock(name);

            for (int i = 0; i < blocks.Count; i++)
            {
                IMyDoor door = blocks[i] as IMyDoor;
                if (door != null)
                {
                    if (door.Status == DoorStatus.Open)
                        airlock.OuterOpen = true;
                    airlock.outerDoors.Add(door);
                    continue;
                }

                IMyLightingBlock light = blocks[i] as IMyLightingBlock;
                if (light != null)
                {
                    airlock.outerLights.Add(light);
                    continue;
                }

                IMySoundBlock sound = blocks[i] as IMySoundBlock;
                if (sound != null)
                {
                    airlock.outerSound.Add(sound);
                }

                IMyAirVent airvent = blocks[i] as IMyAirVent;
                if (airvent != null)
                {
                    float pres = (airvent.CanPressurize ? airvent.GetOxygenLevel() : -1f);
                    if (pres > airlock.OuterPressure)
                        airlock.OuterPressure = pres;
                    continue;
                }
            }
            return;
        }
    }

    public void Run(string argument)
    {
        int tmpIdx = argument.Trim().LastIndexOf(" ");
        string cmd_airlock;
        string cmd_command;
        if (tmpIdx >= 0)
        {
            cmd_airlock = argument.Substring(0, tmpIdx);
            cmd_command = (tmpIdx + 1 < argument.Length ? argument.Substring(tmpIdx + 1) : "toggle");
        }
        else
        {
            cmd_airlock = argument;
            cmd_command = "toggle";
        }

        for (int i = 0; i < airlocks.CountAll(); i++)
            airlocks.GetItemAt(i).Reset();

        List<IMyBlockGroup> controlGroups = new List<IMyBlockGroup>();
        MM.Debug("Processing inner and outer groups");
        List<IMyBlockGroup> BlockGroups = new List<IMyBlockGroup>();
        MM._GridTerminalSystem.GetBlockGroups(BlockGroups);
        for (int gid = 0; gid < BlockGroups.Count; gid++)
        {
            IMyBlockGroup group = BlockGroups[gid];
            string name = group.Name.ToLower();
            if (!name.StartsWith(MMConfig.GROUP_TAG))
                continue;

            if (name.StartsWith(MMConfig.GROUP_TAG + ' ' + MMConfig.CONTROL_TAG))
            {
                controlGroups.Add(group);
                continue;
            }

            AddGroup(group);
        }
        MM.Debug("Processing control group");
        for (int gid = 0; gid < controlGroups.Count; gid++)
        {
            AddGroup(controlGroups[gid]);
        }
        MM.Debug("Processing LCD panels");
        for (int i = 0; i < panelList.Count; i++)
        {
            panelList[i].SortPanels();
            MMLCDTextManager.SetupLCDText(panelList[i]);
            MMLCDTextManager.ClearText(panelList[i]);
        }

        for (int i = 0; i < airlocks.CountAll(); i++)
        {
            MMAirlock airlock = airlocks.GetItemAt(i);
            if (cmd_airlock == airlock.name)
                airlock.command = cmd_command;
            airlock.Process();
        }
        MM.Debug("Updating panels");
        for (int i = 0; i < panelList.Count; i++)
            panelList[i].Update();
    }
}

// MMAPI below (do not modify)    

// IMyTerminalBlock collection with useful methods    
public class MMBlockCollection
{
    public List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();

    // add Blocks with name containing nameLike    
    public void AddBlocksOfNameLike(string nameLike)
    {
        if (nameLike == "" || nameLike == "*")
        {
            List<IMyTerminalBlock> lBlocks = new List<IMyTerminalBlock>();
            MM._GridTerminalSystem.GetBlocks(lBlocks);
            Blocks.AddList(lBlocks);
            return;
        }

        string group = (nameLike.StartsWith("G:") ? nameLike.Substring(2).Trim().ToLower() : "");
        if (group != "")
        {
            List<IMyBlockGroup> BlockGroups = new List<IMyBlockGroup>();
            MM._GridTerminalSystem.GetBlockGroups(BlockGroups);

            for (int i = 0; i < BlockGroups.Count; i++)
            {
                IMyBlockGroup g = BlockGroups[i];
                if (g.Name.ToLower() == group)
                    g.GetBlocks(Blocks);
            }
            return;
        }

        MM._GridTerminalSystem.SearchBlocksOfName(nameLike, Blocks);
    }

    // add Blocks of type (optional: with name containing nameLike)    
    public void AddBlocksOfType(string type, string nameLike = "")
    {
        if (nameLike == "" || nameLike == "*")
        {
            List<IMyTerminalBlock> blocksOfType = new List<IMyTerminalBlock>();
            MM.GetBlocksOfType(ref blocksOfType, type);
            Blocks.AddList(blocksOfType);
        }
        else
        {
            string group = (nameLike.StartsWith("G:") ? nameLike.Substring(2).Trim().ToLower() : "");
            if (group != "")
            {
                List<IMyBlockGroup> BlockGroups = new List<IMyBlockGroup>();
                MM._GridTerminalSystem.GetBlockGroups(BlockGroups);

                for (int i = 0; i < BlockGroups.Count; i++)
                {
                    IMyBlockGroup g = BlockGroups[i];
                    if (g.Name.ToLower() == group)
                    {
                        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                        g.GetBlocks(blocks);
                        for (int j = 0; j < blocks.Count; j++)
                            if (MM.IsBlockOfType(blocks[j], type))
                                Blocks.Add(blocks[j]);
                        return;
                    }
                }
                return;
            }
            List<IMyTerminalBlock> blocksOfType = new List<IMyTerminalBlock>();
            MM.GetBlocksOfType(ref blocksOfType, type);

            for (int i = 0; i < blocksOfType.Count; i++)
                if (blocksOfType[i].CustomName.Contains(nameLike))
                    Blocks.Add(blocksOfType[i]);
        }
    }

    // add all Blocks from collection col to this collection    
    public void AddFromCollection(MMBlockCollection col)
    {
        Blocks.AddList(col.Blocks);
    }

    // clear all blocks from this collection    
    public void Clear()
    {
        Blocks.Clear();
    }

    // number of blocks in collection    
    public int Count()
    {
        return Blocks.Count;
    }
}

public class MMPanel
{
    // full line of progress bar  
    public const int DFULL_PROGRESS_CHARS = 116;
    public int FULL_PROGRESS_CHARS = 0;
    // approximate width of LCD panel line  
    public const float LCD_LINE_WIDTH = 730;

    public MMTextPanelCollection panels = new MMTextPanelCollection();
    public MMLCDTextManager.MMLCDText text = null;
    public IMyTextPanel first = null;

    public void SetFontSize(float size)
    {
        for (int i = 0; i < panels.CountAll(); i++)
            panels.GetItemAt(i).SetValueFloat("FontSize", size);
    }

    public void SortPanels()
    {
        panels.SortAll();
        first = panels.GetItemAt(0);
    }

    public bool IsWide()
    {
        return (first.DefinitionDisplayNameText.Contains("Wide")
            || first.DefinitionDisplayNameText == "Computer Monitor");
    }

    public void Update()
    {
        if (text == null)
            return;

        int cnt = panels.CountAll();

        if (cnt > 1)
            SetFontSize(first.GetValueFloat("FontSize"));

        for (int i = 0; i < panels.CountAll(); i++)
        {
            IMyTextPanel panel = panels.GetItemAt(i);
            panel.WritePublicText(text.GetDisplayString(i));
            if (MMLCDTextManager.forceRedraw)
            {
                panel.ShowTextureOnScreen();
                panel.ShowPublicTextOnScreen();
            }
        }
    }

}

public static class MMLCDTextManager
{
    private static Dictionary<IMyTextPanel, MMLCDText> panelTexts = new Dictionary<IMyTextPanel, MMLCDText>();
    public static bool forceRedraw = true;

    public static void SetupLCDText(MMPanel p)
    {
        MMLCDText lcdText = GetLCDText(p);
        lcdText.SetTextFontSize(p.first.GetValueFloat("FontSize"));
        lcdText.SetNumberOfScreens(p.panels.CountAll());
        p.FULL_PROGRESS_CHARS = (int)(MMPanel.DFULL_PROGRESS_CHARS * lcdText.widthMod);
        lcdText.widthMod = (p.IsWide() ? 2.0f : 1.0f) * (0.8f / lcdText.fontSize);
    }

    public static MMLCDText GetLCDText(MMPanel p)
    {
        MMLCDText lcdText = null;
        IMyTextPanel panel = p.first;

        if (!panelTexts.TryGetValue(panel, out lcdText))
        {
            lcdText = new MMLCDText();
            p.text = lcdText;
            panelTexts.Add(panel, lcdText);
        }

        p.text = lcdText;
        return lcdText;
    }

    public static void AddLine(MMPanel panel, string line)
    {
        MMLCDText lcd = GetLCDText(panel);
        lcd.AddLine(line);
    }

    public static void Add(MMPanel panel, string text)
    {
        MMLCDText lcd = GetLCDText(panel);
        lcd.AddFast(text);
        lcd.current_width += MMStringFunc.GetStringSize(text);
    }

    public static void AddRightAlign(MMPanel panel, string text, float end_screen_x)
    {
        MMLCDText lcd = GetLCDText(panel);

        float text_width = MMStringFunc.GetStringSize(text);
        end_screen_x *= lcd.widthMod;
        end_screen_x -= lcd.current_width;

        if (end_screen_x < text_width)
        {
            lcd.AddFast(text);
            lcd.current_width += text_width;
            return;
        }

        end_screen_x -= text_width;
        int fillchars = (int)Math.Round(end_screen_x / MMStringFunc.WHITESPACE_WIDTH, MidpointRounding.AwayFromZero);
        float fill_width = fillchars * MMStringFunc.WHITESPACE_WIDTH;

        string filler = new String(' ', fillchars);
        lcd.AddFast(filler + text);
        lcd.current_width += fill_width + text_width;
    }

    public static void AddCenter(MMPanel panel, string text, float screen_x)
    {
        MMLCDText lcd = GetLCDText(panel);
        float text_width = MMStringFunc.GetStringSize(text);
        screen_x *= lcd.widthMod;
        screen_x -= lcd.current_width;

        if (screen_x < text_width / 2)
        {
            lcd.AddFast(text);
            lcd.current_width += text_width;
            return;
        }

        screen_x -= text_width / 2;
        int fillchars = (int)Math.Round(screen_x / MMStringFunc.WHITESPACE_WIDTH, MidpointRounding.AwayFromZero);
        float fill_width = fillchars * MMStringFunc.WHITESPACE_WIDTH;

        string filler = new String(' ', fillchars);
        lcd.AddFast(filler + text);
        lcd.current_width += fill_width + text_width;
    }

    public static void AddProgressBar(MMPanel panel, double percent, int width = 22)
    {
        MMLCDText lcd = GetLCDText(panel);
        int totalBars = width - 2;
        int fill = (int)(percent * totalBars) / 100;
        if (fill > totalBars)
            fill = totalBars;
        string progress = "[" + new String('|', fill) + new String('\'', totalBars - fill) + "]";

        lcd.AddFast(progress);
        lcd.current_width += MMStringFunc.PROGRESSCHAR_WIDTH * width;
    }

    public static void ClearText(MMPanel panel)
    {
        GetLCDText(panel).ClearText();
    }

    public static void UpdatePanel(MMPanel panel)
    {
        panel.Update();
        GetLCDText(panel).ScrollNextLine();
    }

    public class MMLCDText
    {
        public int SCROLL_LINES = 5;
        public float fontSize = 0.8f;
        public float widthMod = 1.0f;
        public int scrollPosition = 0;
        public int scrollDirection = 1;
        public int DisplayLines = 22; // 22 for font size 0.8  
        public int screens = 1;

        public List<string> lines = new List<string>();
        public int current_line = 0;
        public float current_width = 0;

        public MMLCDText(float _fontSize = 0.8f)
        {
            SetTextFontSize(_fontSize);
            lines.Add("");
        }

        public void SetTextFontSize(float _fontSize)
        {
            fontSize = _fontSize;
            DisplayLines = (int)Math.Round(22 * (0.8 / fontSize) * screens);
        }

        public void SetNumberOfScreens(int _screens)
        {
            screens = _screens;
            DisplayLines = (int)Math.Round(22 * (0.8 / fontSize) * screens);
        }

        public void AddFast(string text)
        {
            lines[current_line] += text;
        }

        public void AddLine(string line)
        {
            lines[current_line] += line;
            lines.Add("");
            current_line++;
            current_width = 0;
        }

        public void ClearText()
        {
            lines.Clear();
            lines.Add("");
            current_width = 0;
            current_line = 0;
        }

        public string GetFullString()
        {
            return String.Join("\n", lines);
        }

        // Display only X lines from scrollPos  
        public string GetDisplayString(int screenidx = 0)
        {
            if (lines.Count < DisplayLines / screens)
            {
                if (screenidx == 0)
                {
                    scrollPosition = 0;
                    scrollDirection = 1;
                    return GetFullString();
                }
                return "";
            }


            int scrollPos = scrollPosition + screenidx * (DisplayLines / screens);
            if (scrollPos > lines.Count)
                scrollPos = lines.Count;

            List<string> display =
                lines.GetRange(scrollPos,
                    Math.Min(lines.Count - scrollPos, DisplayLines / screens));

            return String.Join("\n", display);
        }

        public void ScrollNextLine()
        {
            int lines_cnt = lines.Count - 1;
            if (lines_cnt <= DisplayLines)
            {
                scrollPosition = 0;
                scrollDirection = 1;
                return;
            }

            if (scrollDirection > 0)
            {
                if (scrollPosition + SCROLL_LINES + DisplayLines > lines_cnt)
                {
                    scrollDirection = -1;
                    scrollPosition = Math.Max(lines_cnt - DisplayLines, 0);
                    return;
                }

                scrollPosition += SCROLL_LINES;
            }
            else
            {
                if (scrollPosition - SCROLL_LINES < 0)
                {
                    scrollPosition = 0;
                    scrollDirection = 1;
                    return;
                }

                scrollPosition -= SCROLL_LINES;
            }
        }
    }
}

public static class MMStringFunc
{
    private static Dictionary<char, float> charSize = new Dictionary<char, float>();

    public const float WHITESPACE_WIDTH = 8f;
    public const float PROGRESSCHAR_WIDTH = 6f;

    public static void InitCharSizes()
    {
        if (charSize.Count > 0)
            return;

        AddCharsSize("3FKTabdeghknopqsuy", 17f);
        AddCharsSize("#0245689CXZ", 19f);
        AddCharsSize("$&GHPUVY", 20f);
        AddCharsSize("ABDNOQRS", 21f);
        AddCharsSize("(),.1:;[]ft{}", 9f);
        AddCharsSize("+<=>E^~", 18f);
        AddCharsSize(" !I`ijl", 8f);
        AddCharsSize("7?Jcz", 16f);
        AddCharsSize("L_vx", 15f);
        AddCharsSize("\"-r", 10f);
        AddCharsSize("mw", 27f);
        AddCharsSize("M", 26f);
        AddCharsSize("W", 31f);
        AddCharsSize("'|", 6f);
        AddCharsSize("*", 11f);
        AddCharsSize("\\", 12f);
        AddCharsSize("/", 14f);
        AddCharsSize("%", 24f);
        AddCharsSize("@", 25f);
        AddCharsSize("\n", 0f);
    }

    private static void AddCharsSize(string chars, float size)
    {
        for (int i = 0; i < chars.Length; i++)
            charSize.Add(chars[i], size);
    }

    public static float GetCharSize(char c)
    {
        float width = 17f;
        charSize.TryGetValue(c, out width);

        return width;
    }

    public static float GetStringSize(string str)
    {
        float sum = 0;
        for (int i = 0; i < str.Length; i++)
            sum += GetCharSize(str[i]);

        return sum;
    }

    public static string GetStringTrimmed(string text, float pixel_width)
    {
        int trimlen = Math.Min((int)pixel_width / 14, text.Length - 2);
        float stringSize = GetStringSize(text);
        if (stringSize <= pixel_width)
            return text;

        while (stringSize > pixel_width - 20)
        {
            text = text.Substring(0, trimlen);
            stringSize = GetStringSize(text);
            trimlen -= 2;
        }
        return text + "..";
    }
}


// MMAPI Helper functions    
public static class MM
{
    public static bool EnableDebug = false;
    public static IMyGridTerminalSystem _GridTerminalSystem = null;
    public static MMBlockCollection _DebugTextPanels = null;
    public static Dictionary<string, Action<List<IMyTerminalBlock>>> BlocksOfStrType = null;

    public static void Init(IMyGridTerminalSystem gridSystem, bool _EnableDebug)
    {
        _GridTerminalSystem = gridSystem;
        EnableDebug = _EnableDebug;
        _DebugTextPanels = new MMBlockCollection();

        // prepare debug panels  
        // select all text panels with [DEBUG] in name   
        if (_EnableDebug)
        {
            _DebugTextPanels.AddBlocksOfType("textpanel", "[DEBUG]");
            Debug("DEBUG Panel started.", false, "DEBUG PANEL");
        }

        MMStringFunc.InitCharSizes();
    }

    public static float GetAirVentPressure(IMyTerminalBlock airvent)
    {
        string data = GetLastDetailedValue(airvent);
        float perc = 0f;
        string valstr = (data.Length > 0 ? data.Substring(0, data.Length - 1) : "");

        if (!float.TryParse(valstr, out perc))
            return -1f;
        return perc;
    }

    public static double GetPercent(double current, double max)
    {
        return (max > 0 ? (current / max) * 100 : 100);
    }

    public static List<double> GetDetailedInfoValues(IMyTerminalBlock block)
    {
        List<double> result = new List<double>();

        string di = block.DetailedInfo;
        string[] attr_lines = block.DetailedInfo.Split('\n');
        string valstr = "";

        for (int i = 0; i < attr_lines.Length; i++)
        {
            string[] parts = attr_lines[i].Split(':');
            // broken line? (try German)  
            if (parts.Length < 2)
                parts = attr_lines[i].Split('r');
            valstr = (parts.Length < 2 ? parts[0] : parts[1]);
            string[] val_parts = valstr.Trim().Split(' ');
            string str_val = val_parts[0];
            char str_unit = (val_parts.Length > 1 ? val_parts[1][0] : '.');

            double val = 0;
            double final_val = 0;
            if (Double.TryParse(str_val, out val))
            {
                final_val = val * Math.Pow(1000.0, ".kMGTPEZY".IndexOf(str_unit));
                result.Add(final_val);
            }
        }

        return result;
    }

    public static string GetLastDetailedValue(IMyTerminalBlock block)
    {
        string[] info_lines = block.DetailedInfo.Split('\n');
        string[] state_parts = info_lines[info_lines.Length - 1].Split(':');
        string state = (state_parts.Length > 1 ? state_parts[1] : state_parts[0]);
        return state;
    }


    public static string GetBlockTypeDisplayName(IMyTerminalBlock block)
    {
        return block.DefinitionDisplayNameText;
    }

    public static void GetBlocksOfExactType(ref List<IMyTerminalBlock> blocks, string exact)
    {
        if (exact == "CargoContainer") _GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(blocks);
        else
        if (exact == "TextPanel") _GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blocks);
        else
        if (exact == "Assembler") _GridTerminalSystem.GetBlocksOfType<IMyAssembler>(blocks);
        else
        if (exact == "Refinery") _GridTerminalSystem.GetBlocksOfType<IMyRefinery>(blocks);
        else
        if (exact == "Reactor") _GridTerminalSystem.GetBlocksOfType<IMyReactor>(blocks);
        else
        if (exact == "SolarPanel") _GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(blocks);
        else
        if (exact == "BatteryBlock") _GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(blocks);
        else
        if (exact == "Beacon") _GridTerminalSystem.GetBlocksOfType<IMyBeacon>(blocks);
        else
        if (exact == "RadioAntenna") _GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(blocks);
        else
        if (exact == "AirVent") _GridTerminalSystem.GetBlocksOfType<IMyAirVent>(blocks);
        else
        if (exact == "OxygenTank") _GridTerminalSystem.GetBlocksOfType<IMyGasTank>(blocks);
        else
        if (exact == "OxygenGenerator") _GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(blocks);
        else
        if (exact == "LaserAntenna") _GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(blocks);
        else
        if (exact == "Thrust") _GridTerminalSystem.GetBlocksOfType<IMyThrust>(blocks);
        else
        if (exact == "Gyro") _GridTerminalSystem.GetBlocksOfType<IMyGyro>(blocks);
        else
        if (exact == "SensorBlock") _GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(blocks);
        else
        if (exact == "ShipConnector") _GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks);
        else
        if (exact == "ReflectorLight") _GridTerminalSystem.GetBlocksOfType<IMyReflectorLight>(blocks);
        else
        if (exact == "InteriorLight") _GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(blocks);
        else
        if (exact == "LandingGear") _GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(blocks);
        else
        if (exact == "ProgrammableBlock") _GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks);
        else
        if (exact == "TimerBlock") _GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(blocks);
        else
        if (exact == "MotorStator") _GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(blocks);
        else
        if (exact == "PistonBase") _GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(blocks);
        else
        if (exact == "Projector") _GridTerminalSystem.GetBlocksOfType<IMyProjector>(blocks);
        else
        if (exact == "ShipMergeBlock") _GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(blocks);
        else
        if (exact == "SoundBlock") _GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(blocks);
        else
        if (exact == "Collector") _GridTerminalSystem.GetBlocksOfType<IMyCollector>(blocks);
        else
        if (exact == "Door") _GridTerminalSystem.GetBlocksOfType<IMyDoor>(blocks);
        else
        if (exact == "GravityGeneratorSphere") _GridTerminalSystem.GetBlocksOfType<IMyGravityGeneratorSphere>(blocks);
        else
        if (exact == "GravityGenerator") _GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(blocks);
        else
        if (exact == "ShipDrill") _GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(blocks);
        else
        if (exact == "ShipGrinder") _GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(blocks);
        else
        if (exact == "ShipWelder") _GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(blocks);
        else
        if (exact == "LargeGatlingTurret") _GridTerminalSystem.GetBlocksOfType<IMyLargeGatlingTurret>(blocks);
        else
        if (exact == "LargeInteriorTurret") _GridTerminalSystem.GetBlocksOfType<IMyLargeInteriorTurret>(blocks);
        else
        if (exact == "LargeMissileTurret") _GridTerminalSystem.GetBlocksOfType<IMyLargeMissileTurret>(blocks);
        else
        if (exact == "SmallGatlingGun") _GridTerminalSystem.GetBlocksOfType<IMySmallGatlingGun>(blocks);
        else
        if (exact == "SmallMissileLauncherReload") _GridTerminalSystem.GetBlocksOfType<IMySmallMissileLauncherReload>(blocks);
        else
        if (exact == "SmallMissileLauncher") _GridTerminalSystem.GetBlocksOfType<IMySmallMissileLauncher>(blocks);
        else
        if (exact == "VirtualMass") _GridTerminalSystem.GetBlocksOfType<IMyVirtualMass>(blocks);
        else
        if (exact == "Warhead") _GridTerminalSystem.GetBlocksOfType<IMyWarhead>(blocks);
        else
        if (exact == "FunctionalBlock") _GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks);
        else
        if (exact == "LightingBlock") _GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(blocks);
        else
        if (exact == "ControlPanel") _GridTerminalSystem.GetBlocksOfType<IMyControlPanel>(blocks);
        else
        if (exact == "Cockpit") _GridTerminalSystem.GetBlocksOfType<IMyCockpit>(blocks);
        else
        if (exact == "MedicalRoom") _GridTerminalSystem.GetBlocksOfType<IMyMedicalRoom>(blocks);
        else
        if (exact == "RemoteControl") _GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(blocks);
        else
        if (exact == "ButtonPanel") _GridTerminalSystem.GetBlocksOfType<IMyButtonPanel>(blocks);
        else
        if (exact == "CameraBlock") _GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(blocks);
        else
        if (exact == "OreDetector") _GridTerminalSystem.GetBlocksOfType<IMyOreDetector>(blocks);
    }

    public static void GetBlocksOfType(ref List<IMyTerminalBlock> blocks, string typestr)
    {
        typestr = typestr.Trim().ToLower();

        GetBlocksOfExactType(ref blocks, TranslateToExactBlockType(typestr));
    }

    public static bool IsBlockOfExactType(IMyTerminalBlock block, string exact)
    {
        if (exact == "FunctionalBlock")
            return block.IsFunctional;
        else
            if (exact == "LightingBlock")
            return ((block as IMyLightingBlock) != null);
        return block.BlockDefinition.ToString().Contains(exact);
    }

    public static bool IsBlockOfType(IMyTerminalBlock block, string typestr)
    {
        string exact = TranslateToExactBlockType(typestr);
        if (exact == "FunctionalBlock")
            return block.IsFunctional;
        else
            if (exact == "LightingBlock")
            return ((block as IMyLightingBlock) != null);
        return block.BlockDefinition.ToString().Contains(exact);
    }

    public static string TranslateToExactBlockType(string typeInStr)
    {
        typeInStr = typeInStr.ToLower();

        if (typeInStr.StartsWith("carg") || typeInStr.StartsWith("conta"))
            return "CargoContainer";
        if (typeInStr.StartsWith("text") || typeInStr.StartsWith("lcd"))
            return "TextPanel";
        if (typeInStr.StartsWith("ass"))
            return "Assembler";
        if (typeInStr.StartsWith("refi"))
            return "Refinery";
        if (typeInStr.StartsWith("reac"))
            return "Reactor";
        if (typeInStr.StartsWith("solar"))
            return "SolarPanel";
        if (typeInStr.StartsWith("bat"))
            return "BatteryBlock";
        if (typeInStr.StartsWith("bea"))
            return "Beacon";
        if (typeInStr.Contains("vent"))
            return "AirVent";
        if (typeInStr.Contains("tank") && typeInStr.Contains("oxy"))
            return "OxygenTank";
        if (typeInStr.Contains("gene") && typeInStr.Contains("oxy"))
            return "OxygenGenerator";
        if (typeInStr == "laserantenna")
            return "LaserAntenna";
        if (typeInStr.Contains("antenna"))
            return "RadioAntenna";
        if (typeInStr.StartsWith("thrust"))
            return "Thrust";
        if (typeInStr.StartsWith("gyro"))
            return "Gyro";
        if (typeInStr.StartsWith("sensor"))
            return "SensorBlock";
        if (typeInStr.Contains("connector"))
            return "ShipConnector";
        if (typeInStr.StartsWith("reflector"))
            return "ReflectorLight";
        if ((typeInStr.StartsWith("inter") && typeInStr.EndsWith("light")))
            return "InteriorLight";
        if (typeInStr.StartsWith("land"))
            return "LandingGear";
        if (typeInStr.StartsWith("program"))
            return "ProgrammableBlock";
        if (typeInStr.StartsWith("timer"))
            return "TimerBlock";
        if (typeInStr.StartsWith("motor"))
            return "MotorStator";
        if (typeInStr.StartsWith("piston"))
            return "PistonBase";
        if (typeInStr.StartsWith("proj"))
            return "Projector";
        if (typeInStr.Contains("merge"))
            return "ShipMergeBlock";
        if (typeInStr.StartsWith("sound"))
            return "SoundBlock";
        if (typeInStr.StartsWith("col"))
            return "Collector";
        if (typeInStr == "door")
            return "Door";
        if ((typeInStr.Contains("grav") && typeInStr.Contains("sphe")))
            return "GravityGeneratorSphere";
        if (typeInStr.Contains("grav"))
            return "GravityGenerator";
        if (typeInStr.EndsWith("drill"))
            return "ShipDrill";
        if (typeInStr.Contains("grind"))
            return "ShipGrinder";
        if (typeInStr.EndsWith("welder"))
            return "ShipWelder";
        if ((typeInStr.Contains("turret") && typeInStr.Contains("gatl")))
            return "LargeGatlingTurret";
        if ((typeInStr.Contains("turret") && typeInStr.Contains("inter")))
            return "LargeInteriorTurret";
        if ((typeInStr.Contains("turret") && typeInStr.Contains("miss")))
            return "LargeMissileTurret";
        if (typeInStr.Contains("gatl"))
            return "SmallGatlingGun";
        if ((typeInStr.Contains("launcher") && typeInStr.Contains("reload")))
            return "SmallMissileLauncherReload";
        if ((typeInStr.Contains("launcher")))
            return "SmallMissileLauncher";
        if (typeInStr.Contains("mass"))
            return "VirtualMass";
        if (typeInStr == "warhead")
            return "Warhead";
        if (typeInStr.StartsWith("func"))
            return "FunctionalBlock";
        if (typeInStr.StartsWith("light"))
            return "LightingBlock";
        if (typeInStr.StartsWith("contr"))
            return "ControlPanel";
        if (typeInStr.StartsWith("coc"))
            return "Cockpit";
        if (typeInStr.StartsWith("medi"))
            return "MedicalRoom";
        if (typeInStr.StartsWith("remote"))
            return "RemoteControl";
        if (typeInStr.StartsWith("but"))
            return "ButtonPanel";
        if (typeInStr.StartsWith("cam"))
            return "CameraBlock";
        if (typeInStr.Contains("detect"))
            return "OreDetector";
        return "Unknown";
    }

    public static string FormatLargeNumber(double number, bool compress = true)
    {
        if (!compress)
            return number.ToString(
                "#,###,###,###,###,###,###,###,###,###");

        string ordinals = " kMGTPEZY";
        double compressed = number;

        var ordinal = 0;

        while (compressed >= 1000)
        {
            compressed /= 1000;
            ordinal++;
        }

        string res = Math.Round(compressed, 1, MidpointRounding.AwayFromZero).ToString();

        if (ordinal > 0)
            res += " " + ordinals[ordinal];

        return res;
    }

    public static void WriteLine(IMyTextPanel textpanel, string message, bool append = true, string title = "")
    {
        textpanel.WritePublicText(message + "\n", append);
        if (title != "")
            textpanel.WritePublicTitle(title);
        textpanel.ShowTextureOnScreen();
        textpanel.ShowPublicTextOnScreen();
    }

    public static void Debug(string message, bool append = true, string title = "")
    {
        if (!EnableDebug)
            return;
        if (_DebugTextPanels == null || _DebugTextPanels.Count() == 0)
            DebugAntenna(message, append, title);
        else
            DebugTextPanel(message, append, title);
    }

    public static void DebugAntenna(string message, bool append = true, string title = "")
    {
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

        _GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(blocks);
        IMyRadioAntenna ant = blocks[0] as IMyRadioAntenna;
        if (append)
            ant.CustomName = ant.CustomName + message + "\n";
        else
            ant.CustomName = "PROG: " + message + "\n";
    }

    public static void DebugTextPanel(string message, bool append = true, string title = "")
    {
        for (int i = 0; i < _DebugTextPanels.Count(); i++)
        {
            IMyTextPanel debugpanel = _DebugTextPanels.Blocks[i] as IMyTextPanel;
            debugpanel.CustomName = "[DEBUG] Prog: " + message;
            WriteLine(debugpanel, message, append, title);
        }
    }
}
public class MMTextPanelCollection
{
    public Dictionary<string, IMyTextPanel> dict = new Dictionary<string, IMyTextPanel>();
    public List<string> keys = new List<string>();

    public void AddItem(string key, IMyTextPanel item) { if (!dict.ContainsKey(key)) { keys.Add(key); dict.Add(key, item); } }
    public int CountAll() { return dict.Count; }
    public bool ContainsKey(string k) { return dict.ContainsKey(k); }
    public bool ContainsItem(IMyTextPanel item) { return dict.ContainsValue(item); }
    public IMyTextPanel GetItem(string key) { if (dict.ContainsKey(key)) return dict[key]; return null; }
    public IMyTextPanel GetItemAt(int index) { return dict[keys[index]]; }
    public void ClearAll() { keys.Clear(); dict.Clear(); }
    public void SortAll() { keys.Sort(); }
}
public class MMAirlockCollection
{
    public Dictionary<string, MMAirlock> dict = new Dictionary<string, MMAirlock>();
    public List<string> keys = new List<string>();

    public void AddItem(string key, MMAirlock item) { if (!dict.ContainsKey(key)) { keys.Add(key); dict.Add(key, item); } }
    public int CountAll() { return dict.Count; }
    public bool ContainsKey(string k) { return dict.ContainsKey(k); }
    public bool ContainsItem(MMAirlock item) { return dict.ContainsValue(item); }
    public MMAirlock GetItem(string key) { if (dict.ContainsKey(key)) return dict[key]; return null; }
    public MMAirlock GetItemAt(int index) { return dict[keys[index]]; }
    public void ClearAll() { keys.Clear(); dict.Clear(); }
    public void SortAll() { keys.Sort(); }
