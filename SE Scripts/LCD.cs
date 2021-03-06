﻿using System;
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
    /* v:2.0120 Automatic font detection per LCD, Timers not required, HScroll, PropBool command, Proper Corner LCDs scrolling support, CustomData command to read Custom Data of block
    * In-game script by MMaster
    *
    * Last Update: Improved display of Inventory command (better alignment, better compatibility with larger fonts)
    *  Canvas component support added
    *  Improvements in text formatting in Working, Damage and other commands
    * Previous Update: (support for game update 1.185.1)
    *  Timers are not required anymore - just compile the script & remember and it will work
    *  Automatic detection of Debug / Monospace font per LCD
    *  Much nicer progress bars for monospace font
    *  HScroll command for slow horizontal scrolling text (can't make it faster, sorry)
    *  PropBool and PropsBool commands to display boolean properties of blocks (Handbrake, CollectAll, Open, OnOff, etc) (read more in guide)
    *  LCD linking finally fixed (hopefully)
    *  Vanilla Corner LCDs are now recognized with correct number of lines
    *  Customizable progressbar characters (look below at MMStyle)
    *  CustomData command to read Custom Data of specified block
    *  Working command now displays override of thrusters when they are on
    *  Parachute support
    *  Mass and ShipMass commands now show g, kg, t, kt, Mt, Gt, etc instead of kg, Mg, Gg, etc.
    *  Fixed Scrap now being Ore instead of Ingot (it was changed in game)
    *  Removed everything 'static' from the code
    *  LCD_TAG can be set in Custom Data of Programmable Block (read more in guide section 'What is LCD_TAG?')
    *
    * Previous updates: Look at Change notes tab on Steam workshop page.
    *
    * Customize these: */

    // Use this tag to identify LCDs managed by this script
    // Name filtering rules can be used here so you can use even G:Group or T:[My LCD]
    public string LCD_TAG = "T:[LCD]";

    // How many lines to scroll per step
    public const int SCROLL_LINES_PER_STEP = 1;

    // Script automatically figures if LCD is using monospace font
    // if you use custom font scroll down to the bottom, then scroll a bit up until you find AddCharsSize lines
    // monospace font name and size definition is above those

    // Enable initial boot sequence (after compile / world load)
    public const bool ENABLE_BOOT = true;

    /* READ THIS FULL GUIDE
    http://steamcommunity.com/sharedfiles/filedetails/?id=407158161

    Basic video guide
    Please watch the video guide even if you don't understand my English. You can see how things are done there.

    https://youtu.be/vqpPQ_20Xso


    Read Change Notes (above screenshots) for latest updates and new features.
    I notify about updates on twitter so follow if interested.

    Please carefully read the FULL GUIDE before asking questions I had to remove guide from here to add more features :(
    Please DO NOT publish this script or its derivations without my permission! Feel free to use it in blueprints!

    Special Thanks
    Keen Software House for awesome Space Engineers game
    Malware for contributing to programmable blocks game code
    Textor and CyberVic for their great script related contributions on Keen forums.

    Watch Twitter: https://twitter.com/MattsPlayCorner
    and Facebook: https://www.facebook.com/MattsPlayCorner1080p
    for more crazy stuff from me in the future :)
    */


    /* Customize characters used by script */
    class MMStyle
    {
        // Monospace font characters (\uXXXX is special character code)
        public const char BAR_MONO_START = '[';
        public const char BAR_MONO_END = ']';
        public const char BAR_MONO_EMPTY = '\u2591'; // 25% rect
        public const char BAR_MONO_FILL = '\u2588'; // full rect

        // Classic (Debug) font characters
        // Start and end characters of progress bar need to be the same width!
        public const char BAR_START = '[';
        public const char BAR_END = ']';
        // Empty and fill characters of progress bar need to be the same width!
        public const char BAR_EMPTY = '\'';
        public const char BAR_FILL = '|';

    }


    // (for developer) Debug level to show
    public const int DebugLevel = 0;

    // (for modded lcds) Affects all LCDs managed by this programmable block
    /* LCD height modifier
    0.5f makes the LCD have only 1/2 the lines of normal LCD
    2.0f makes it fit 2x more lines on LCD */
    public const float HEIGHT_MOD = 1.0f;

    /* line width modifier
    0.5f moves the right edge to 50% of normal LCD width
    2.0f makes it fit 200% more text on line */
    public const float WIDTH_MOD = 1.0f;

    List<string> BOOT_FRAMES = new List<string>() {
/* BOOT FRAMES
* Each @"<text>" marks single frame, add as many as you want each will be displayed for one second
* @"" is multiline string so you can write multiple lines */
@"
Initializing systems"
,
@"
Verifying connections"
,
@"
Loading commands"
};

    void ItemsConf()
    {
        // ITEMS AND QUOTAS LIST
        // (subType, mainType, quota, display name, short name)
        // VANILLA ITEMS
        Add("Stone", "Ore");
        Add("Iron", "Ore");
        Add("Nickel", "Ore");
        Add("Cobalt", "Ore");
        Add("Magnesium", "Ore");
        Add("Silicon", "Ore");
        Add("Silver", "Ore");
        Add("Gold", "Ore");
        Add("Platinum", "Ore");
        Add("Uranium", "Ore");
        Add("Ice", "Ore");
        Add("Scrap", "Ore");
        Add("Stone", "Ingot", 40000, "Gravel", "gravel");
        Add("Iron", "Ingot", 300000);
        Add("Nickel", "Ingot", 900000);
        Add("Cobalt", "Ingot", 120000);
        Add("Magnesium", "Ingot", 80000);
        Add("Silicon", "Ingot", 80000);
        Add("Silver", "Ingot", 800000);
        Add("Gold", "Ingot", 80000);
        Add("Platinum", "Ingot", 45000);
        Add("Uranium", "Ingot", 12000);
        Add("AutomaticRifleItem", "Tool", 0, "Automatic Rifle");
        Add("PreciseAutomaticRifleItem", "Tool", 0, "* Precise Rifle");
        Add("RapidFireAutomaticRifleItem", "Tool", 0, "** Rapid-Fire Rifle");
        Add("UltimateAutomaticRifleItem", "Tool", 0, "*** Elite Rifle");
        Add("WelderItem", "Tool", 0, "Welder");
        Add("Welder2Item", "Tool", 0, "* Enh. Welder");
        Add("Welder3Item", "Tool", 0, "** Prof. Welder");
        Add("Welder4Item", "Tool", 0, "*** Elite Welder");
        Add("AngleGrinderItem", "Tool", 0, "Angle Grinder");
        Add("AngleGrinder2Item", "Tool", 0, "* Enh. Grinder");
        Add("AngleGrinder3Item", "Tool", 0, "** Prof. Grinder");
        Add("AngleGrinder4Item", "Tool", 0, "*** Elite Grinder");
        Add("HandDrillItem", "Tool", 0, "Hand Drill");
        Add("HandDrill2Item", "Tool", 0, "* Enh. Drill");
        Add("HandDrill3Item", "Tool", 0, "** Prof. Drill");
        Add("HandDrill4Item", "Tool", 0, "*** Elite Drill");
        Add("Construction", "Component", 50000);
        Add("MetalGrid", "Component", 15500, "Metal Grid");
        Add("InteriorPlate", "Component", 55000, "Interior Plate");
        Add("SteelPlate", "Component", 300000, "Steel Plate");
        Add("Girder", "Component", 3500);
        Add("SmallTube", "Component", 26000, "Small Tube");
        Add("LargeTube", "Component", 6000, "Large Tube");
        Add("Motor", "Component", 16000);
        Add("Display", "Component", 500);
        Add("BulletproofGlass", "Component", 12000, "Bulletp. Glass", "bpglass");
        Add("Computer", "Component", 6500);
        Add("Reactor", "Component", 10000);
        Add("Thrust", "Component", 16000, "Thruster", "thruster");
        Add("GravityGenerator", "Component", 250, "GravGen", "gravgen");
        Add("Medical", "Component", 120);
        Add("RadioCommunication", "Component", 250, "Radio-comm", "radio");
        Add("Detector", "Component", 400);
        Add("Explosives", "Component", 500);
        Add("SolarCell", "Component", 2800, "Solar Cell");
        Add("PowerCell", "Component", 2800, "Power Cell");
        Add("Superconductor", "Component", 3000);
        Add("Canvas", "Component", 300);
        Add("NATO_5p56x45mm", "Ammo", 8000, "5.56x45mm", "5.56x45mm");
        Add("NATO_25x184mm", "Ammo", 2500, "25x184mm", "25x184mm");
        Add("Missile200mm", "Ammo", 1600, "200mm Missile", "200mmmissile");
        Add("OxygenBottle", "OxygenContainerObject", 5, "Oxygen Bottle");
        Add("HydrogenBottle", "GasContainerObject", 5, "Hydrogen Bottle");

        // MODDED ITEMS
        // (subType, mainType, quota, display name, short name, used)
        // * if used is true, item will be shown in inventory even for 0 items
        // * if used is false, item will be used only for display name and short name
        // AzimuthSupercharger
        Add("AzimuthSupercharger", "Component", 1600, "Supercharger", "supercharger", false);
        // OKI Ammo
        Add("OKI23mmAmmo", "Ammo", 500, "23x180mm", "23x180mm", false);
        Add("OKI50mmAmmo", "Ammo", 500, "50x450mm", "50x450mm", false);
        Add("OKI122mmAmmo", "Ammo", 200, "122x640mm", "122x640mm", false);
        Add("OKI230mmAmmo", "Ammo", 100, "230x920mm", "230x920mm", false);

        // REALLY REALLY REALLY
        // DO NOT MODIFY ANYTHING BELOW THIS (TRANSLATION STRINGS ARE AT THE BOTTOM)
    }
    void Add(string sT, string mT, int q = 0, string dN = "", string sN = "", bool u = true) { MMItems.Add(sT, mT, q, dN, sN, u); }
    uM MMItems; uV k8; uE k9; uX ka = null; void kb(string a) { }
    void kc(string b, string d)
    {
        var e = b.ToLower();
        switch (e) { case "lcd_tag": LCD_TAG = d; break; }
    }
    void kd()
    {
        string[] f = Me.CustomData.Split('\n'); for (int g = 0; g < f.Length; g++)
        {
            var i = f[g];
            int j = i.IndexOf('='); if (j < 0) { kb(i); continue; }
            var k = i.Substring(0, j).Trim(); var l = i.Substring(j + 1).Trim(); kc(k, l);
        }
    }
    void ke(uV n)
    {
        MMItems = new uM(); ItemsConf(); kd(); ka = new uX(this, DebugLevel, n); ka.MMItems = MMItems; ka.t1 = LCD_TAG; ka.t2 = SCROLL_LINES_PER_STEP; ka.t3 =
                           ENABLE_BOOT; ka.t4 = BOOT_FRAMES; ka.t5 = false; ka.t8 = HEIGHT_MOD; ka.t7 = WIDTH_MOD; ka.u1();
    }
    void kf() { k8.sj = this; ka.ts = this; }
    Program()
    {
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }
    void Main(string o, UpdateType q)
    {
        try
        {
            if (k8 == null)
            {
                k8 = new uV(this, DebugLevel); ke(
k8); k9 = new uE(ka); k8.sl(k9, 0);
            }
            else { kf(); ka.tu.qv(); }
            if (o.Length == 0 && (q & (UpdateType.Update1 | UpdateType.Update10 | UpdateType.
Update100)) == 0) { k8.sp(); return; }
            if (o != "") { if (k9.oF(o)) { k8.sp(); return; } }
            k9.oq = 0; k8.sq();
        }
        catch (Exception ex)
        {
            Echo(ex.ToString());
            Me.Enabled = false;
        }
    }
    class u3 : uz
    {
        public u3() { s3 = 7; r_ = "CmdInvList"; }
        float kg = -1; float kh = -1; public override void Init()
        {
            kv = new uK(s7
, nB.tu); kw = new uO(s7, nB);
        }
        void ki(string r, double u, int v)
        {
            if (v > 0)
            {
                nB.tN(Math.Min(100, 100 * u / v), 0.3f); var w = nB.u0(r, nB.ta * 0.5f - kk -
kh); nB.Add(' ' + w + ' '); nB.tJ(nB.tV(u), 1.0f, kk + kh); nB.tF(" / " + nB.tV(v));
            }
            else { nB.Add(r + ':'); nB.tI(nB.tV(u), 1.0f, kg); }
        }
        void kj(
string x, double y, double z, int A)
        {
            if (A > 0)
            {
                nB.Add(x + ' '); nB.tJ(nB.tV(y), 0.51f); nB.Add(" / " + nB.tV(A)); nB.tI(" +" + nB.tV(z) + " " + nD.T(
"I1"), 1.0f); nB.tM(Math.Min(100, 100 * y / A));
            }
            else { nB.Add(x + ':'); nB.tJ(nB.tV(y), 0.51f); nB.tI(" +" + nB.tV(z) + " " + nD.T("I1"), 1.0f); }
        }
        float kk = 0; bool kl(uP B) { int C = (kA ? B.r8 : B.r9); if (C < 0) return true; float D = nB.t_(nB.tV(C), nB.tB); if (D > kk) kk = D; return true; }
        bool km =
true; List<uP> kn; int ko = 0; int kp = 0; bool kq(bool E, bool F, string G, string H)
        {
            if (!E) { kp = 0; ko = 0; }
            if (kp == 0)
            {
                if (km)
                {
                    if ((kn = kw.r5(G, E)) ==
null) return false; km = false;
                }
                else { if ((kn = kw.r5(G, E, kl)) == null) return false; }
                kp++; E = false;
            }
            if (kn.Count > 0)
            {
                if (!F && !E)
                {
                    if (!nB.tC) nB.tF
(""); nB.tK("<< " + H + " " + nD.T("I2") + " >>");
                }
                for (; ko < kn.Count; ko++)
                {
                    if (!s7.sv(30)) return false; double I = kn[ko].rd; if (kA && I >= kn[ko].r8
) continue; int J = kn[ko].r9; if (kA) J = kn[ko].r8; var K = nB.tT(kn[ko].ra, kn[ko].rb); ki(K, I, J);
                }
            }
            return true;
        }
        List<uP> kr; int ks = 0; int kt = 0
; bool ku(bool L)
        {
            if (!L) { ks = 0; kt = 0; }
            if (kt == 0) { if ((kr = kw.r5("Ingot", L)) == null) return false; kt++; L = false; }
            if (kr.Count > 0)
            {
                if (!kB && !L)
                {
                    if (!nB.tC) nB.tF(""); nB.tK("<< " + nD.T("I4") + " " + nD.T("I2") + " >>");
                }
                for (; ks < kr.Count; ks++)
                {
                    if (!s7.sv(40)) return false; double N = kr[ks
].rd; if (kA && N >= kr[ks].r8) continue; int O = kr[ks].r9; if (kA) O = kr[ks].r8; var P = nB.tT(kr[ks].ra, kr[ks].rb); if (kr[ks].ra != "Scrap")
                    {
                        double
Q = kw.r2(kr[ks].ra + " Ore", kr[ks].ra, "Ore").rd; kj(P, N, Q, O);
                    }
                    else ki(P, N, O);
                }
            }
            return true;
        }
        uK kv = null; uO kw; List<uI> kx; bool ky, kz, kA,
kB; int kC, kD; string kE = ""; void kF()
        {
            if (nB.tB != kE) { kh = nB.t_(" / ", nB.tB); kg = nB.tZ(' ', nB.tB); kE = nB.tB; }
            kv.qo(); ky = nA.pk.EndsWith(
"x") || nA.pk.EndsWith("xs"); kz = nA.pk.EndsWith("s") || nA.pk.EndsWith("sx"); kA = nA.pk.StartsWith("missing"); kB = nA.pk.StartsWith(
"invlist"); kw.qY(); kx = nA.pr; if (kx.Count == 0) kx.Add(new uI("all"));
        }
        bool kG(bool R)
        {
            if (!R) kC = 0; for (; kC < kx.Count; kC++)
            {
                uI S = kx[kC]; S.
pB(); var U = S.py.ToLower(); if (!R) kD = 0; else R = false; for (; kD < S.pA.Count; kD++)
                {
                    if (!s7.sv(30)) return false; string[] V = S.pA[kD].ToLower()
.Split(':'); double W; if (V[0] == "all") V[0] = ""; int X = 1; int Y = -1; if (V.Length > 1)
                    {
                        if (Double.TryParse(V[1], out W))
                        {
                            if (kA) X = (int)Math.
Ceiling(W);
                            else Y = (int)Math.Ceiling(W);
                        }
                    }
                    var Z = V[0]; if (U != "") Z += ' ' + U; kw.qZ(Z, (S.px == "-"), X, Y);
                }
            }
            return true;
        }
        int kH = 0; int kI = 0;
        int kJ = 0; bool kK(bool _)
        {
            uK a0 = kv; if (!_) kH = 0; for (; kH < a0.qp(); kH++)
            {
                if (!_) kI = 0; for (; kI < a0.q4[kH].InventoryCount; kI++)
                {
                    if (!_) kJ = 0;
                    else _ = false; IMyInventory a1 = a0.q4[kH].GetInventory(kI); List<IMyInventoryItem> a2 = a1.GetItems(); for (; kJ < a2.Count; kJ++)
                    {
                        if (!s7.sv(40
)) return false; IMyInventoryItem a3 = a2[kJ]; var a4 = nB.tR(a3); var a5 = a4.ToLower(); string a6, a7; nB.tS(a5, out a6, out a7); if (a7 == "ore")
                        {
                            if (kw.r0(a6.ToLower() + " ingot", a6, "Ingot") && kw.r0(a4, a6, a7)) continue;
                        }
                        else { if (kw.r0(a4, a6, a7)) continue; }
                        nB.tS(a4, out a6, out a7); uP
a8 = kw.r2(a5, a6, a7); a8.rd += (double)a3.Amount;
                    }
                }
            }
            return true;
        }
        int kL = 0; public override bool RunCmd(bool a9)
        {
            if (!a9) { kF(); kL = 0; }
            for (;
kL <= 9; kL++)
            {
                switch (kL)
                {
                    case 0: if (!kv.qe(nA.pl, a9)) return false; break;
                    case 1:
                        if (!kG(a9)) return false; if (!ky)
                        {
                            if (!kw.r7(a9)) return
false;
                        }
                        break;
                    case 2: if (!kK(a9)) return false; break;
                    case 3: if (!kq(a9, kB, "Ore", nD.T("I3"))) return false; break;
                    case 4:
                        if (kz)
                        {
                            if (!kq(a9
, kB, "Ingot", nD.T("I4"))) return false;
                        }
                        else { if (!ku(a9)) return false; }
                        break;
                    case 5:
                        if (!kq(a9, kB, "Component", nD.T("I5"))) return false
; break;
                    case 6: if (!kq(a9, kB, "OxygenContainerObject", nD.T("I6"))) return false; break;
                    case 7:
                        if (!kq(a9, true, "GasContainerObject", ""))
                            return false; break;
                    case 8: if (!kq(a9, kB, "AmmoMagazine", nD.T("I7"))) return false; break;
                    case 9:
                        if (!kq(a9, kB, "PhysicalGunObject", nD.T(
"I8"))) return false; break;
                }
                a9 = false;
            }
            return true;
        }
    }
    class u4 : uz
    {
        uK kM; public u4() { s3 = 2; r_ = "CmdCargo"; }
        public override void Init()
        {
            kM = new uK(s7, nB.tu);
        }
        bool kN = true; bool kO = false; bool kP = false; double kQ = 0; double kR = 0; int kS = 0; public override bool RunCmd(bool aa
     )
        {
            if (!aa) { kM.qo(); kN = nA.pk.Contains("all"); kO = (nA.pk[nA.pk.Length - 1] == 'x'); kP = (nA.pk[nA.pk.Length - 1] == 'p'); kQ = 0; kR = 0; kS = 0; }
            if (kS ==
0) { if (kN) { if (!kM.qe(nA.pl, aa)) return false; } else { if (!kM.qm("cargocontainer", nA.pl, aa)) return false; } kS++; aa = false; }
            double ab = kM.q6
(ref kQ, ref kR, aa); if (Double.IsNaN(ab)) return false; nB.Add(nD.T("C2") + " "); if (!kO && !kP)
            {
                nB.tI(nB.tV(kQ) + "L / " + nB.tV(kR) + "L"); nB.
tN(ab, 1.0f, nB.th); nB.tF(' ' + nB.tX(ab) + "%");
            }
            else if (kP) { nB.tI(nB.tX(ab) + "%"); nB.tM(ab); } else nB.tI(nB.tX(ab) + "%"); return true;
        }
    }
    class u5 : uz
    {
        uK kT; public u5() { s3 = 2; r_ = "CmdMass"; }
        public override void Init() { kT = new uK(s7, nB.tu); }
        bool kU = false; bool kV = false; int
kW = 0; public override bool RunCmd(bool ac)
        {
            if (!ac) { kT.qo(); kU = (nA.pk[nA.pk.Length - 1] == 'x'); kV = (nA.pk[nA.pk.Length - 1] == 'p'); kW = 0; }
            if
(kW == 0) { if (!kT.qe(nA.pl, ac)) return false; kW++; ac = false; }
            double ad = kT.q9(ac); if (Double.IsNaN(ad)) return false; double ae = 0; int af = nA
.pr.Count; if (af > 0)
            {
                double.TryParse(nA.pr[0].pz.Trim(), out ae); if (af > 1)
                {
                    var ag = nA.pr[1].pz.Trim().ToLower(); if (ag != "") ae *= Math.Pow(
1000.0, "kmgtpezy".IndexOf(ag[0]));
                }
                ae *= 1000.0;
            }
            nB.Add(nD.T("M1") + " "); if (ae <= 0) { nB.tI(nB.tW(ad, false)); return true; }
            double ah = ad /
ae * 100; if (!kU && !kV) { nB.tI(nB.tW(ad) + " / " + nB.tW(ae)); nB.tN(ah, 1.0f, nB.th); nB.tF(' ' + nB.tX(ah) + "%"); }
            else if (kV)
            {
                nB.tI(nB.tX(ah) +
"%"); nB.tM(ah);
            }
            else nB.tI(nB.tX(ah) + "%"); return true;
        }
    }
    class u6 : uz
    {
        uR kX; uK kY; public u6() { s3 = 3; r_ = "CmdOxygen"; }
        public override
void Init()
        { kX = nB.tt; kY = new uK(s7, nB.tu); }
        int kZ = 0; int k_ = 0; bool l0 = false; int l1 = 0; double l2 = 0; double l3 = 0; double l4; public
override bool RunCmd(bool aj)
        {
            if (!aj) { kY.qo(); kZ = 0; k_ = 0; l4 = 0; }
            if (kZ == 0)
            {
                if (!kY.qm("airvent", nA.pl, aj)) return false; l0 = (kY.qp() > 0);
                kZ++; aj = false;
            }
            if (kZ == 1)
            {
                for (; k_ < kY.qp(); k_++)
                {
                    if (!s7.sv(8)) return false; IMyAirVent ak = kY.q4[k_] as IMyAirVent; l4 = Math.Max(ak.
GetOxygenLevel() * 100, 0f); nB.Add(ak.CustomName); if (ak.CanPressurize) nB.tI(nB.tX(l4) + "%"); else nB.tI(nD.T("O1")); nB.tM(l4);
                }
                kZ++; aj =
false;
            }
            if (kZ == 2) { if (!aj) kY.qo(); if (!kY.qm("oxyfarm", nA.pl, aj)) return false; l1 = kY.qp(); kZ++; aj = false; }
            if (kZ == 3)
            {
                if (l1 > 0)
                {
                    if (!aj) k_ =
0; double al = 0; for (; k_ < l1; k_++) { if (!s7.sv(4)) return false; IMyOxygenFarm am = kY.q4[k_] as IMyOxygenFarm; al += am.GetOutput() * 100; }
                    l4 = al /
l1; if (l0) nB.tF(""); l0 |= (l1 > 0); nB.Add(nD.T("O2")); nB.tI(nB.tX(l4) + "%"); nB.tM(l4);
                }
                kZ++; aj = false;
            }
            if (kZ == 4)
            {
                if (!aj) kY.qo(); if (!kY.qm
("oxytank", nA.pl, aj)) return false; l1 = kY.qp(); if (l1 == 0) { if (!l0) nB.tF(nD.T("O3")); return true; }
                kZ++; aj = false;
            }
            if (kZ == 5)
            {
                if (!aj)
                {
                    l2 = 0
; l3 = 0; k_ = 0;
                }
                if (!kX.rv(kY.q4, "oxygen", ref l3, ref l2, aj)) return false; if (l2 == 0) { if (!l0) nB.tF(nD.T("O3")); return true; }
                l4 = l3 / l2 * 100;
                if (l0) nB.tF(""); nB.Add(nD.T("O4")); nB.tI(nB.tX(l4) + "%"); nB.tM(l4); kZ++;
            }
            return true;
        }
    }
    class u7 : uz
    {
        uR l5; uK l6; public u7()
        {
            s3 = 2; r_
= "CmdTanks";
        }
        public override void Init() { l5 = nB.tt; l6 = new uK(s7, nB.tu); }
        int l7 = 0; string l8; string l9; double la = 0; double lb = 0; double
lc; public override bool RunCmd(bool an)
        {
            List<uI> ao = nA.pr; if (ao.Count == 0) { nB.tF(nD.T("T4")); return true; }
            if (!an)
            {
                l8 = (nA.pk.EndsWith
("x") ? "s" : (nA.pk.EndsWith("p") ? "p" : (nA.pk.EndsWith("v") ? "v" : "n"))); l7 = 0; l9 = ao[0].pz.Trim().ToLower(); l6.qo(); la = 0; lb = 0;
            }
            if (l7 == 0)
            {
                if (!l6.qm("oxytank", nA.pl, an)) return false; an = false; l7++;
            }
            if (l7 == 1)
            {
                if (!l5.rv(l6.q4, l9, ref la, ref lb, an)) return false; an = false; l7
++;
            }
            if (lb == 0) { nB.tF(String.Format(nD.T("T5"), l9)); return true; }
            lc = la / lb * 100; l9 = char.ToUpper(l9[0]) + l9.Substring(1); nB.Add(l9 + " " +
nD.T("T6")); switch (l8) { case "s": nB.tI(' ' + nB.tX(lc) + "%"); break; default: nB.tI(' ' + nB.tX(lc) + "%"); nB.tM(lc); break; }
            return true;
        }
    }
    class u8 : uz
    {
        public u8() { s3 = 7; r_ = "CmdPowerTime"; }
        class u9
        {
            public TimeSpan lD = new TimeSpan(-1); public double lE = -1; public double
lF = 0;
        }
        u9 ld = new u9(); uK le; uK lg; public override void Init() { le = new uK(s7, nB.tu); lg = new uK(s7, nB.tu); }
        int lh = 0; double li = 0; double
lj = 0, lk = 0; double ll = 0, lm = 0, ln = 0; double lo = 0, lp = 0; int lq = 0; bool lr(string ap, out TimeSpan aq, out double ar, bool at)
        {
            MyResourceSourceComponent au; MyResourceSinkComponent av; double aw = s2; u9 ax = ld; aq = ax.lD; ar = ax.lE; if (!at)
            {
                le.qo(); lg.qo(); ax.lE = 0; lh
= 0; li = 0; lj = lk = 0; ll = 0; lm = ln = 0; lo = lp = 0; lq = 0;
            }
            if (lh == 0) { if (!le.qm("reactor", ap, at)) return false; at = false; lh++; }
            if (lh == 1)
            {
                for (; lq < le.
q4.Count; lq++)
                {
                    if (!s7.sv(6)) return false; IMyReactor ay = le.q4[lq] as IMyReactor; if (ay == null || !ay.IsWorking) continue; if (ay.Components
                   .TryGet<MyResourceSourceComponent>(out au)) { lj += au.CurrentOutputByType(nB.tt.rn); lk += au.MaxOutputByType(nB.tt.rn); }
                    li += (double)ay.
GetInventory(0).CurrentMass;
                }
                at = false; lh++;
            }
            if (lh == 2) { if (!lg.qm("battery", ap, at)) return false; at = false; lh++; }
            if (lh == 3)
            {
                if (!at) lq = 0
; for (; lq < lg.q4.Count; lq++)
                {
                    if (!s7.sv(15)) return false; IMyBatteryBlock ay = lg.q4[lq] as IMyBatteryBlock; if (ay == null || !ay.IsWorking)
                        continue; if (ay.Components.TryGet<MyResourceSourceComponent>(out au))
                    {
                        lm = au.CurrentOutputByType(nB.tt.rn); ln = au.MaxOutputByType(nB.
tt.rn);
                    }
                    if (ay.Components.TryGet<MyResourceSinkComponent>(out av)) { lm -= av.CurrentInputByType(nB.tt.rn); }
                    double az = (lm < 0 ? (ay.
MaxStoredPower - ay.CurrentStoredPower) / (-lm / 3600) : 0); if (az > ax.lE) ax.lE = az; if (ay.OnlyRecharge) continue; lo += lm; lp += ln; ll += ay.
CurrentStoredPower;
                }
                at = false; lh++;
            }
            double aA = lj + lo; if (aA <= 0) ax.lD = TimeSpan.FromSeconds(-1);
            else
            {
                double aB = ax.lD.TotalSeconds;
                double aC; double aD = (ax.lF - li) / aw; if (lj <= 0) aD = Math.Min(aA, lk) / 3600000; double aE = 0; if (lp > 0) aE = Math.Min(aA, lp) / 3600; if (aD <= 0 && aE <= 0)
                    aC = -1;
                else if (aD <= 0) aC = ll / aE; else if (aE <= 0) aC = li / aD; else { double aF = aE; double aG = (lj <= 0 ? aA / 3600 : aD * aA / lj); aC = ll / aF + li / aG; }
                if (aB <= 0
|| aC < 0) aB = aC;
                else aB = (aB + aC) / 2; try { ax.lD = TimeSpan.FromSeconds(aB); } catch { ax.lD = TimeSpan.FromSeconds(-1); }
            }
            ax.lF = li; ar = ax.lE; aq = ax.
lD; return true;
        }
        string ls(TimeSpan aI)
        {
            var aJ = ""; if (aI.Ticks <= 0) return "-"; if ((int)aI.TotalDays > 0) aJ += (long)aI.TotalDays + " " + nD.T(
"C5") + " "; if (aI.Hours > 0 || aJ != "") aJ += aI.Hours + "h "; if (aI.Minutes > 0 || aJ != "") aJ += aI.Minutes + "m "; return aJ + aI.Seconds + "s";
        }
        int lt = 0;
        bool lu = false; bool lv = false; double lw = 0; TimeSpan lx; int ly = 0, lz = 0, lA = 0; int lB = 0; int lC = 0; public override bool RunCmd(bool aK)
        {
            if (!
aK) { lu = (nA.pk[nA.pk.Length - 1] == 'x'); lv = (nA.pk[nA.pk.Length - 1] == 'p'); lt = 0; ly = lz = lA = lB = 0; lC = 0; lw = 0; }
            if (lt == 0)
            {
                if (nA.pr.Count > 0)
                {
                    for (
; lC < nA.pr.Count; lC++)
                    {
                        if (!s7.sv(100)) return false; nA.pr[lC].pB(); if (nA.pr[lC].pA.Count <= 0) continue; var aL = nA.pr[lC].pA[0]; int.
              TryParse(aL, out lB); if (lC == 0) ly = lB; else if (lC == 1) lz = lB; else if (lC == 2) lA = lB;
                    }
                }
                lt++; aK = false;
            }
            if (lt == 1)
            {
                if (!lr(nA.pl, out lx, out lw,
aK)) return false; lt++; aK = false;
            }
            if (!s7.sv(30)) return false; double aM = 0; TimeSpan aN; try { aN = new TimeSpan(ly, lz, lA); }
            catch
            {
                aN =
TimeSpan.FromSeconds(-1);
            }
            string aO; if (lx.TotalSeconds > 0 || lw <= 0) { nB.Add(nD.T("PT1") + " "); aO = ls(lx); aM = lx.TotalSeconds; }
            else
            {
                nB.Add
(nD.T("PT2") + " "); aO = ls(TimeSpan.FromSeconds(lw)); if (aN.TotalSeconds >= lw) aM = aN.TotalSeconds - lw; else aM = 0;
            }
            if (aN.Ticks <= 0)
            {
                nB.tI(aO
); return true;
            }
            double aP = aM / aN.TotalSeconds * 100; if (aP > 100) aP = 100; if (!lu && !lv)
            {
                nB.tI(aO); nB.tN(aP, 1.0f, nB.th); nB.tF(' ' + aP.ToString
("0.0") + "%");
            }
            else if (lv) { nB.tI(aP.ToString("0.0") + "%"); nB.tM(aP); } else nB.tI(aP.ToString("0.0") + "%"); return true;
        }
    }
    class ua : uz
    {
        public ua() { s3 = 7; r_ = "CmdPowerUsed"; }
        uR lG; uK lH; public override void Init() { lH = new uK(s7, nB.tu); lG = nB.tt; }
        string lI; string lJ;
        string lK; void lL(double aQ, double aR)
        {
            double aS = (aR > 0 ? aQ / aR * 100 : 0); switch (lI)
            {
                case "s": nB.tI(aS.ToString("0.0") + "%", 1.0f); break;
                case "v": nB.tI(nB.tV(aQ) + "W / " + nB.tV(aR) + "W", 1.0f); break;
                case "c": nB.tI(nB.tV(aQ) + "W", 1.0f); break;
                case "p":
                    nB.tI(aS.ToString(
"0.0") + "%", 1.0f); nB.tM(aS); break;
                default:
                    nB.tI(nB.tV(aQ) + "W / " + nB.tV(aR) + "W"); nB.tN(aS, 1.0f, nB.th); nB.tI(' ' + aS.ToString("0.0") +
"%"); break;
            }
        }
        double lM = 0, lN = 0; int lO = 0; int lP = 0; ub lQ = new ub(); public override bool RunCmd(bool aT)
        {
            if (!aT)
            {
                lI = (nA.pk.EndsWith("x"
) ? "s" : (nA.pk.EndsWith("usedp") || nA.pk.EndsWith("topp") ? "p" : (nA.pk.EndsWith("v") ? "v" : (nA.pk.EndsWith("c") ? "c" : "n")))); lJ = (nA.pk.
Contains("top") ? "top" : ""); lK = (nA.pr.Count > 0 ? nA.pr[0].pz : nD.T("PU1")); lM = lN = 0; lP = 0; lO = 0; lH.qo(); lQ.lV();
            }
            if (lP == 0)
            {
                if (!lH.qe(nA.pl,
aT)) return false; aT = false; lP++;
            }
            MyResourceSinkComponent aU; MyResourceSourceComponent aV; switch (lJ)
            {
                case "top":
                    if (lP == 1)
                    {
                        for (; lO < lH
.q4.Count; lO++)
                        {
                            if (!s7.sv(20)) return false; IMyTerminalBlock aW = lH.q4[lO]; if (aW.Components.TryGet<MyResourceSinkComponent>(out aU))
                            { ListReader<MyDefinitionId> aX = aU.AcceptedResources; if (aX.IndexOf(lG.rn) < 0) continue; lM = aU.CurrentInputByType(lG.rn) * 0xF4240; }
                            else
                                continue; lQ.lS(lM, aW);
                        }
                        aT = false; lP++;
                    }
                    if (lQ.lT() <= 0) { nB.tF("PowerUsedTop: " + nD.T("D2")); return true; }
                    int aY = 10; if (nA.pr.Count > 0) if
(!int.TryParse(lK, out aY)) { aY = 10; }
                    if (aY > lQ.lT()) aY = lQ.lT(); if (lP == 2)
                    {
                        if (!aT) { lO = lQ.lT() - 1; lQ.lW(); }
                        for (; lO >= lQ.lT() - aY; lO--)
                        {
                            if (!
s7.sv(30)) return false; IMyTerminalBlock aW = lQ.lU(lO); var aZ = nB.u0(aW.CustomName, nB.ta * 0.4f); if (aW.Components.TryGet<
MyResourceSinkComponent>(out aU)) { lM = aU.CurrentInputByType(lG.rn) * 0xF4240; lN = aU.MaxRequiredInputByType(lG.rn) * 0xF4240; }
                            nB.Add(aZ +
" "); lL(lM, lN);
                        }
                    }
                    break;
                default:
                    for (; lO < lH.q4.Count; lO++)
                    {
                        if (!s7.sv(10)) return false; double a_; IMyTerminalBlock aW = lH.q4[lO]; if (aW.
Components.TryGet<MyResourceSinkComponent>(out aU))
                        {
                            ListReader<MyDefinitionId> aX = aU.AcceptedResources; if (aX.IndexOf(lG.rn) < 0)
                                continue; a_ = aU.CurrentInputByType(lG.rn); lN += aU.MaxRequiredInputByType(lG.rn);
                        }
                        else continue; if (aW.Components.TryGet<
MyResourceSourceComponent>(out aV) && (aW as IMyBatteryBlock != null)) { a_ -= aV.CurrentOutputByType(lG.rn); if (a_ <= 0) continue; }
                        lM += a_;
                    }
                    nB
.Add(lK); lL(lM * 0xF4240, lN * 0xF4240); break;
            }
            return true;
        }
        public class ub
        {
            List<KeyValuePair<double, IMyTerminalBlock>> lR = new List<
KeyValuePair<double, IMyTerminalBlock>>(); public void lS(double b3, IMyTerminalBlock b4)
            {
                lR.Add(new KeyValuePair<double,
IMyTerminalBlock>(b3, b4));
            }
            public int lT() { return lR.Count; }
            public IMyTerminalBlock lU(int b5) { return lR[b5].Value; }
            public void lV
()
            { lR.Clear(); }
            public void lW() { lR.Sort((b6, b7) => (b6.Key.CompareTo(b7.Key))); }
        }
    }
    class uc : uz
    {
        public uc() { s3 = 3; r_ = "CmdPower"; }
        uR lX
; uK lY; uK lZ; uK l_; uK m0; public override void Init()
        {
            lY = new uK(s7, nB.tu); lZ = new uK(s7, nB.tu); l_ = new uK(s7, nB.tu); m0 = new uK(s7, nB.
tu); lX = nB.tt;
        }
        string m1; bool m2; string m3; string m4; int m5; int m6 = 0; public override bool RunCmd(bool b8)
        {
            if (!b8)
            {
                m1 = (nA.pk.
EndsWith("x") ? "s" : (nA.pk.EndsWith("p") ? "p" : (nA.pk.EndsWith("v") ? "v" : "n"))); m2 = (nA.pk.StartsWith("powersummary")); m3 = "a"; m4 = ""; if (
nA.pk.Contains("stored")) m3 = "s";
                else if (nA.pk.Contains("in")) m3 = "i"; else if (nA.pk.Contains("out")) m3 = "o"; m6 = 0; lY.qo(); lZ.qo(); l_.
qo();
            }
            if (m3 == "a")
            {
                if (m6 == 0) { if (!lY.qm("reactor", nA.pl, b8)) return false; b8 = false; m6++; }
                if (m6 == 1)
                {
                    if (!lZ.qm("solarpanel", nA.pl, b8))
                        return false; b8 = false; m6++;
                }
            }
            else if (m6 == 0) m6 = 2; if (m6 == 2) { if (!l_.qm("battery", nA.pl, b8)) return false; b8 = false; m6++; }
            int b9 = lY.qp()
; int ba = lZ.qp(); int bb = l_.qp(); if (m6 == 3)
            {
                m5 = 0; if (b9 > 0) m5++; if (ba > 0) m5++; if (bb > 0) m5++; if (m5 < 1) { nB.tF(nD.T("P6")); return true; }
                if (nA
.pr.Count > 0) { if (nA.pr[0].pz.Length > 0) m4 = nA.pr[0].pz; }
                m6++; b8 = false;
            }
            if (m3 != "a")
            {
                if (!mk(l_, (m4 == "" ? nD.T("P7") : m4), m3, m1, b8)) return
false; return true;
            }
            var bc = nD.T("P8"); if (!m2)
            {
                if (m6 == 4)
                {
                    if (b9 > 0) if (!mc(lY, (m4 == "" ? nD.T("P9") : m4), m1, b8)) return false; m6++; b8 = false;
                }
                if (m6 == 5) { if (ba > 0) if (!mc(lZ, (m4 == "" ? nD.T("P10") : m4), m1, b8)) return false; m6++; b8 = false; }
                if (m6 == 6)
                {
                    if (bb > 0) if (!mk(l_, (m4 == "" ? nD.T(
"P7") : m4), m3, m1, b8)) return false; m6++; b8 = false;
                }
            }
            else { bc = nD.T("P11"); m5 = 10; if (m6 == 4) m6 = 7; }
            if (m5 == 1) return true; if (!b8)
            {
                m0.qo(); m0.
qn(lY); m0.qn(lZ); m0.qn(l_);
            }
            if (!mc(m0, bc, m1, b8)) return false; return true;
        }
        void m7(double bd, double be)
        {
            double bf = (be > 0 ? bd / be * 100 : 0
); switch (m1)
            {
                case "s": nB.tI(' ' + bf.ToString("0.0") + "%"); break;
                case "v": nB.tI(nB.tV(bd) + "W / " + nB.tV(be) + "W"); break;
                case "c":
                    nB.tI(
nB.tV(bd) + "W"); break;
                case "p": nB.tI(' ' + bf.ToString("0.0") + "%"); nB.tM(bf); break;
                default:
                    nB.tI(nB.tV(bd) + "W / " + nB.tV(be) + "W"); nB.
tN(bf, 1.0f, nB.th); nB.tI(' ' + bf.ToString("0.0") + "%"); break;
            }
        }
        double m8 = 0; double m9 = 0, ma = 0; int mb = 0; bool mc(uK bg, string bh, string
bi, bool bj)
        {
            if (!bj) { m9 = 0; ma = 0; mb = 0; }
            if (mb == 0) { if (!lX.rt(bg.q4, lX.rn, ref m8, ref m8, ref m9, ref ma, bj)) return false; mb++; bj = false; }
            if
(!s7.sv(50)) return false; double bk = (ma > 0 ? m9 / ma * 100 : 0); nB.Add(bh + ": "); m7(m9 * 0xF4240, ma * 0xF4240); return true;
        }
        double md = 0, me = 0, mf = 0
, mg = 0; double mh = 0, mi = 0; int mj = 0; bool mk(uK bl, string bm, string bn, string bo, bool bp)
        {
            if (!bp) { md = me = 0; mf = mg = 0; mh = mi = 0; mj = 0; }
            if (mj ==
0)
            {
                if (!lX.rr(bl.q4, ref mf, ref mg, ref md, ref me, ref mh, ref mi, bp)) return false; mf *= 0xF4240; mg *= 0xF4240; md *= 0xF4240; me *= 0xF4240; mh *=
                                   0xF4240; mi *= 0xF4240; mj++; bp = false;
            }
            double bq = (mi > 0 ? mh / mi * 100 : 0); double br = (me > 0 ? md / me * 100 : 0); double bs = (mg > 0 ? mf / mg * 100 : 0); var bt =
                bn == "a"; if (mj == 1)
            {
                if (!s7.sv(50)) return false; if (bt)
                {
                    if (bo != "p")
                    {
                        nB.Add(bm + ": "); nB.tI("(IN " + nB.tV(mf) + "W / OUT " + nB.tV(md) + "W)");
                    }
                    else nB.tF(bm + ": "); nB.Add("  " + nD.T("P3") + ": ");
                }
                else nB.Add(bm + ": "); if (bt || bn == "s") switch (bo)
                    {
                        case "s":
                            nB.tI(' ' + bq.ToString(
"0.0") + "%"); break;
                        case "v": nB.tI(nB.tV(mh) + "Wh / " + nB.tV(mi) + "Wh"); break;
                        case "p":
                            nB.tI(' ' + bq.ToString("0.0") + "%"); nB.tM(bq);
                            break;
                        default: nB.tI(nB.tV(mh) + "Wh / " + nB.tV(mi) + "Wh"); nB.tN(bq, 1.0f, nB.th); nB.tI(' ' + bq.ToString("0.0") + "%"); break;
                    }
                if (bn == "s")
                    return true; mj++; bp = false;
            }
            if (mj == 2)
            {
                if (!s7.sv(50)) return false; if (bt) nB.Add("  " + nD.T("P4") + ": "); if (bt || bn == "o") switch (bo)
                    {
                        case
"s":
                            nB.tI(' ' + br.ToString("0.0") + "%"); break;
                        case "v": nB.tI(nB.tV(md) + "W / " + nB.tV(me) + "W"); break;
                        case "p":
                            nB.tI(' ' + br.ToString(
"0.0") + "%"); nB.tM(br); break;
                        default:
                            nB.tI(nB.tV(md) + "W / " + nB.tV(me) + "W"); nB.tN(br, 1.0f, nB.th); nB.tI(' ' + br.ToString("0.0") + "%");
                            break;
                    }
                if (bn == "o") return true; mj++; bp = false;
            }
            if (!s7.sv(50)) return false; if (bt) nB.Add("  " + nD.T("P5") + ": "); if (bt || bn == "i") switch (
bo)
                {
                    case "s": nB.tI(' ' + bs.ToString("0.0") + "%"); break;
                    case "v": nB.tI(nB.tV(mf) + "W / " + nB.tV(mg) + "W"); break;
                    case "p":
                        nB.tI(' ' + bs.
ToString("0.0") + "%"); nB.tM(bs); break;
                    default:
                        nB.tI(nB.tV(mf) + "W / " + nB.tV(mg) + "W"); nB.tN(bs, 1.0f, nB.th); nB.tI(' ' + bs.ToString(
"0.0") + "%"); break;
                }
            return true;
        }
    }
    class ud : uz
    {
        public ud() { s3 = 0.5; r_ = "CmdSpeed"; }
        public override bool RunCmd(bool bu)
        {
            double bv = 0;
            double bw = 1; var bx = "m/s"; if (nA.pk.Contains("kmh")) { bw = 3.6; bx = "km/h"; } else if (nA.pk.Contains("mph")) { bw = 2.23694; bx = "mph"; }
            if (nA.pl
!= "") double.TryParse(nA.pl.Trim(), out bv); nB.Add(nD.T("S1") + " "); nB.tI((nB.tv.pG * bw).ToString("F1") + " " + bx + " "); if (bv > 0) nB.tM(nB.
tv.pG / bv * 100); return true;
        }
    }
    class ue : uz
    {
        public ue() { s3 = 0.5; r_ = "CmdAccel"; }
        public override bool RunCmd(bool by)
        {
            double bz = 0; if (nA.
pl != "") double.TryParse(nA.pl.Trim(), out bz); nB.Add(nD.T("AC1") + " "); nB.tI(nB.tv.pI.ToString("F1") + " m/s²"); if (bz > 0)
            {
                double bA = nB.
tv.pI / bz * 100; nB.tM(bA);
            }
            return true;
        }
    }
    class uf : uz
    {
        public uf() { s3 = 30; r_ = "CmdEcho"; }
        public override bool RunCmd(bool bB)
        {
            var bC = (nA
.pk == "center" ? "c" : (nA.pk == "right" ? "r" : "n")); switch (bC)
            {
                case "c": nB.tK(nA.pn); break;
                case "r": nB.tI(nA.pn); break;
                default:
                    nB.tF(nA.pn
); break;
            }
            return true;
        }
    }
    class ug : uz
    {
        public ug() { s3 = 3; r_ = "CmdCharge"; }
        uK ml; public override void Init() { ml = new uK(s7, nB.tu); }
        int mm
= 0; int mn = 0; public override bool RunCmd(bool bD)
        {
            var bE = nA.pk.Contains("x"); if (!bD) { ml.qo(); mn = 0; mm = 0; }
            if (mm == 0)
            {
                if (!ml.qm(
"jumpdrive", nA.pl, bD)) return false; if (ml.qp() <= 0) { nB.tF("Charge: " + nD.T("D2")); return true; }
                mm++; bD = false;
            }
            for (; mn < ml.qp(); mn++)
            {
                if (!s7.sv(25)) return false; IMyJumpDrive bF = ml.q4[mn] as IMyJumpDrive; double bG, bH, bI; bI = nB.tu.qD(bF, out bG, out bH); nB.Add(bF.
                               CustomName + " "); if (!bE) { nB.tI(nB.tV(bG) + "Wh / " + nB.tV(bH) + "Wh"); nB.tN(bI, 1.0f, nB.th); }
                nB.tI(' ' + bI.ToString("0.0") + "%");
            }
            return
true;
        }
    }
    class uh : uz
    {
        public uh() { s3 = 1; r_ = "CmdDateTime"; }
        public override bool RunCmd(bool bJ)
        {
            var bK = (nA.pk.StartsWith("datetime"));
            var bL = (nA.pk.StartsWith("date")); var bM = nA.pk.Contains("c"); int bN = nA.pk.IndexOf('+'); if (bN < 0) bN = nA.pk.IndexOf('-'); float bO = 0; if
                               (bN >= 0) float.TryParse(nA.pk.Substring(bN), out bO); DateTime bP = DateTime.Now.AddHours(bO); var bQ = ""; int bR = nA.pm.IndexOf(' '); if (bR
                                             >= 0) bQ = nA.pm.Substring(bR + 1); if (!bK) { if (!bL) bQ += bP.ToShortTimeString(); else bQ += bP.ToShortDateString(); }
            else
            {
                if (bQ == "") bQ = String.
Format("{0:d} {0:t}", bP);
                else
                {
                    bQ = bQ.Replace("/", "\\/"); bQ = bQ.Replace(":", "\\:"); bQ = bQ.Replace("\"", "\\\""); bQ = bQ.Replace("'", "\\'"
  ); bQ = bP.ToString(bQ + ' '); bQ = bQ.Substring(0, bQ.Length - 1);
                }
            }
            if (bM) nB.tK(bQ); else nB.tF(bQ); return true;
        }
    }
    class ui : uz
    {
        public ui()
        {
            s3
= 1; r_ = "CmdCountdown";
        }
        public override bool RunCmd(bool bS)
        {
            var bT = nA.pk.EndsWith("c"); var bU = nA.pk.EndsWith("r"); var bV = ""; int bW =
nA.pm.IndexOf(' '); if (bW >= 0) bV = nA.pm.Substring(bW + 1).Trim(); DateTime bX = DateTime.Now; DateTime bY; if (!DateTime.TryParseExact(bV,
"H:mm d.M.yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out bY))
            {
                nB.tF(nD.T(
"C3")); nB.tF("  Countdown 19:02 28.2.2015"); return true;
            }
            TimeSpan bZ = bY - bX; var b_ = ""; if (bZ.Ticks <= 0) b_ = nD.T("C4");
            else
            {
                if ((int)bZ.
TotalDays > 0) b_ += (int)bZ.TotalDays + " " + nD.T("C5") + " "; if (bZ.Hours > 0 || b_ != "") b_ += bZ.Hours + "h "; if (bZ.Minutes > 0 || b_ != "") b_ += bZ.
Minutes + "m "; b_ += bZ.Seconds + "s";
            }
            if (bT) nB.tK(b_); else if (bU) nB.tI(b_); else nB.tF(b_); return true;
        }
    }
    class uj : uz
    {
        public uj()
        {
            s3 = 1;
            r_ = "CmdTextLCD";
        }
        public override bool RunCmd(bool c0)
        {
            IMyTextPanel c1 = nC.nV.rz; if (c1 == null) return true; var c2 = ""; if (nA.pl != "" && nA.
pl != "*")
            {
                IMyTextPanel c3 = nB.tx.GetBlockWithName(nA.pl) as IMyTextPanel; if (c3 == null)
                {
                    nB.tF("TextLCD: " + nD.T("T1") + nA.pl); return true
;
                }
                c2 = c3.GetPublicText();
            }
            else { nB.tF("TextLCD:" + nD.T("T2")); return true; }
            if (c2.Length == 0) return true; nB.tG(c2); return true;
        }
    }
    class
uk : uz
    {
        public uk() { s3 = 15; r_ = "CmdBlockCount"; }
        uK mo; public override void Init() { mo = new uK(s7, nB.tu); }
        bool mp; bool mq; int mr = 0; int
ms = 0; public override bool RunCmd(bool c4)
        {
            if (!c4) { mp = (nA.pk == "enabledcount"); mq = (nA.pk == "prodcount"); mr = 0; ms = 0; }
            if (nA.pr.Count == 0)
            { if (ms == 0) { if (!c4) mo.qo(); if (!mo.qe(nA.pl, c4)) return false; ms++; c4 = false; } if (!mC(mo, "blocks", mp, mq, c4)) return false; return true; }
            for (; mr < nA.pr.Count; mr++) { uI c5 = nA.pr[mr]; if (!c4) c5.pB(); if (!mv(c5, c4)) return false; c4 = false; }
            return true;
        }
        int mt = 0; int mu = 0; bool
mv(uI c6, bool c7)
        {
            if (!c7) { mt = 0; mu = 0; }
            for (; mt < c6.pA.Count; mt++)
            {
                if (mu == 0)
                {
                    if (!c7) mo.qo(); if (!mo.qm(c6.pA[mt], nA.pl, c7)) return false
; mu++; c7 = false;
                }
                if (!mC(mo, c6.pA[mt], mp, mq, c7)) return false; mu = 0; c7 = false;
            }
            return true;
        }
        Dictionary<string, int> mw = new Dictionary<
string, int>(); Dictionary<string, int> mx = new Dictionary<string, int>(); List<string> my = new List<string>(); int mz = 0; int mA = 0; int mB = 0;
        bool mC(uK c8, string c9, bool ca, bool cb, bool cc)
        {
            string cd; if (c8.qp() == 0)
            {
                cd = c9.ToLower(); cd = char.ToUpper(cd[0]) + cd.Substring(1).
ToLower(); nB.Add(cd + " " + nD.T("C1") + " "); var ce = (ca || cb ? "0 / 0" : "0"); nB.tI(ce); return true;
            }
            if (!cc)
            {
                mw.Clear(); mx.Clear(); my.Clear(
); mz = 0; mA = 0; mB = 0;
            }
            if (mB == 0)
            {
                for (; mz < c8.qp(); mz++)
                {
                    if (!s7.sv(15)) return false; IMyProductionBlock cf = c8.q4[mz] as IMyProductionBlock;
                    cd = c8.q4[mz].DefinitionDisplayNameText; if (my.Contains(cd))
                    {
                        mw[cd]++; if ((ca && c8.q4[mz].IsWorking) || (cb && cf != null && cf.IsProducing))
                            mx[cd]++;
                    }
                    else
                    {
                        mw.Add(cd, 1); my.Add(cd); if (ca || cb) if ((ca && c8.q4[mz].IsWorking) || (cb && cf != null && cf.IsProducing)) mx.Add(cd, 1);
                            else mx
.Add(cd, 0);
                    }
                }
                mB++; cc = false;
            }
            for (; mA < mw.Count; mA++)
            {
                if (!s7.sv(8)) return false; nB.Add(my[mA] + " " + nD.T("C1") + " "); var ce = (ca || cb ? mx[
my[mA]] + " / " : "") + mw[my[mA]]; nB.tI(ce);
            }
            return true;
        }
    }
    class ul : uz
    {
        public ul() { s3 = 5; r_ = "CmdShipCtrl"; }
        uK mD; public override void
Init()
        { mD = new uK(s7, nB.tu); }
        public override bool RunCmd(bool ch)
        {
            if (!ch) mD.qo(); if (!mD.qm("shipctrl", nA.pl, ch)) return false; if (mD.
qp() <= 0) { if (nA.pl != "" && nA.pl != "*") nB.tF(nA.pk + ": " + nD.T("SC1") + " (" + nA.pl + ")"); else nB.tF(nA.pk + ": " + nD.T("SC1")); return true; }
            if (
nA.pk.StartsWith("damp")) { var ci = (mD.q4[0] as IMyShipController).DampenersOverride; nB.Add(nD.T("SCD")); nB.tI(ci ? "ON" : "OFF"); }
            else
            {
                var ci = (mD.q4[0] as IMyShipController).IsUnderControl; nB.Add(nD.T("SCO")); nB.tI(ci ? "YES" : "NO");
            }
            return true;
        }
    }
    class um : uz
    {
        public
um()
        { s3 = 5; r_ = "CmdWorking"; }
        uK mE; public override void Init() { mE = new uK(s7, nB.tu); }
        int mF = 0; int mG = 0; bool mH; public override bool
RunCmd(bool ck)
        {
            if (!ck) { mF = 0; mH = (nA.pk == "workingx"); mG = 0; }
            if (nA.pr.Count == 0)
            {
                if (mF == 0)
                {
                    if (!ck) mE.qo(); if (!mE.qe(nA.pl, ck)) return
false; mF++; ck = false;
                }
                if (!mQ(mE, mH, "", ck)) return false; return true;
            }
            for (; mG < nA.pr.Count; mG++)
            {
                uI cl = nA.pr[mG]; if (!ck) cl.pB(); if (!mN
(cl, ck)) return false; ck = false;
            }
            return true;
        }
        int mI = 0; int mJ = 0; string[] mK; string mL; string mM; bool mN(uI cm, bool cn)
        {
            if (!cn)
            {
                mI = 0;
                mJ = 0;
            }
            for (; mJ < cm.pA.Count; mJ++)
            {
                if (mI == 0)
                {
                    if (!cn)
                    {
                        if (cm.pA[mJ] == "") continue; mE.qo(); mK = cm.pA[mJ].ToLower().Split(':'); mL = mK[0]; mM =
(mK.Length > 1 ? mK[1] : "");
                    }
                    if (mL != "") { if (!mE.qm(mL, nA.pl, cn)) return false; } else { if (!mE.qe(nA.pl, cn)) return false; }
                    mI++; cn = false;
                }
                if (!
mQ(mE, mH, mM, cn)) return false; mI = 0; cn = false;
            }
            return true;
        }
        string mO(IMyTerminalBlock co)
        {
            uL cp = nB.tu; if (!co.IsWorking) return nD.T(
"W1"); IMyProductionBlock cq = co as IMyProductionBlock; if (cq != null) if (cq.IsProducing) return nD.T("W2"); else return nD.T("W3");
            IMyAirVent cr = co as IMyAirVent; if (cr != null)
            {
                if (cr.CanPressurize) return (cr.GetOxygenLevel() * 100).ToString("F1") + "%";
                else return nD.
T("W4");
            }
            IMyGasTank cs = co as IMyGasTank; if (cs != null) return (cs.FilledRatio * 100).ToString("F1") + "%"; IMyBatteryBlock cu = co as
                IMyBatteryBlock; if (cu != null) return cp.qB(cu); IMyJumpDrive cv = co as IMyJumpDrive; if (cv != null) return cp.qE(cv).ToString("0.0") + "%";
            IMyLandingGear cw = co as IMyLandingGear; if (cw != null)
            {
                switch ((int)cw.LockMode)
                {
                    case 0: return nD.T("W8");
                    case 1: return nD.T("W10");
                    case 2: return nD.T("W7");
                }
            }
            IMyDoor cx = co as IMyDoor; if (cx != null)
            {
                if (cx.Status == DoorStatus.Open) return nD.T("W5"); return nD.T("W6")
;
            }
            IMyShipConnector cy = co as IMyShipConnector; if (cy != null)
            {
                if (cy.Status == MyShipConnectorStatus.Unconnected) return nD.T("W8"); if (cy.
Status == MyShipConnectorStatus.Connected) return nD.T("W7");
                else return nD.T("W10");
            }
            IMyLaserAntenna cz = co as IMyLaserAntenna; if (cz
!= null) return cp.qC(cz); IMyRadioAntenna cA = co as IMyRadioAntenna; if (cA != null) return nB.tV(cA.Radius) + "m"; IMyBeacon cB = co as
IMyBeacon; if (cB != null) return nB.tV(cB.Radius) + "m"; IMyThrust cC = co as IMyThrust; if (cC != null && cC.ThrustOverride > 0) return nB.tV(cC.
ThrustOverride) + "N"; return nD.T("W9");
        }
        int mP = 0; bool mQ(uK cD, bool cE, string cF, bool cG)
        {
            if (!cG) mP = 0; for (; mP < cD.qp(); mP++)
            {
                if (!s7.
sv(20)) return false; IMyTerminalBlock cH = cD.q4[mP]; var cI = (cE ? (cH.IsWorking ? nD.T("W9") : nD.T("W1")) : mO(cH)); if (cF != "" && cI.ToLower()
!= cF) continue; if (cE) cI = mO(cH); var cJ = cH.CustomName; cJ = nB.u0(cJ, nB.ta * 0.7f); nB.Add(cJ); nB.tI(cI);
            }
            return true;
        }
    }
    class un : uz
    {
        public
un()
        { s3 = 5; r_ = "CmdDamage"; }
        uK mR; public override void Init() { mR = new uK(s7, nB.tu); }
        bool mS = false; int mT = 0; public override bool
RunCmd(bool cK)
        {
            var cL = nA.pk.StartsWith("damagex"); var cM = nA.pk.EndsWith("noc"); var cN = (!cM && nA.pk.EndsWith("c")); float cO = 100; if (
          !cK) { mR.qo(); mS = false; mT = 0; }
            if (!mR.qe(nA.pl, cK)) return false; if (nA.pr.Count > 0) { if (!float.TryParse(nA.pr[0].pz, out cO)) cO = 100; }
            for (
; mT < mR.qp(); mT++)
            {
                if (!s7.sv(30)) return false; IMyTerminalBlock cP = mR.q4[mT]; IMySlimBlock cQ = cP.CubeGrid.GetCubeBlock(cP.Position);
                if (cQ == null) continue; float cR = (cM ? cQ.MaxIntegrity : cQ.BuildIntegrity); if (!cN) cR -= cQ.CurrentDamage; float cS = 100 * (cR / cQ.MaxIntegrity)
                                 ; if (cS >= cO) continue; mS = true; var cT = nB.u0(cQ.FatBlock.DisplayNameText, nB.ta * 0.69f - nB.th); nB.Add(cT + ' '); if (!cL)
                {
                    nB.tJ(nB.tV(cR) +
" / ", 0.69f); nB.Add(nB.tV(cQ.MaxIntegrity));
                }
                nB.tI(' ' + cS.ToString("0.0") + '%'); nB.tM(cS);
            }
            if (!mS) nB.tF(nD.T("D3")); return true;
        }
    }
    class uo : uz
    {
        public uo() { s3 = 2; r_ = "CmdAmount"; }
        uK mU; public override void Init() { mU = new uK(s7, nB.tu); }
        bool mV; int mW = 0; int mX = 0; int
mY = 0; public override bool RunCmd(bool cU)
        {
            if (!cU)
            {
                mV = !nA.pk.EndsWith("x"); if (nA.pr.Count == 0) nA.pr.Add(new uI(
"reactor,gatlingturret,missileturret,interiorturret,gatlinggun,launcherreload,launcher,oxygenerator")); mX = 0;
            }
            for (; mX < nA.pr.Count;
mX++)
            {
                uI cV = nA.pr[mX]; if (!cU) { cV.pB(); mW = 0; mY = 0; }
                for (; mY < cV.pA.Count; mY++)
                {
                    if (mW == 0)
                    {
                        if (!cU) { if (cV.pA[mY] == "") continue; mU.qo(); }
                        var cW = cV.pA[mY]; if (!mU.qm(cW, nA.pl, cU)) return false; mW++; cU = false;
                    }
                    if (!n8(cU)) return false; cU = false; mW = 0;
                }
            }
            return true;
        }
        int mZ = 0;
        int m_ = 0; double n0 = 0; double n1 = 0; double n2 = 0; int n3 = 0; IMyTerminalBlock n4; IMyInventory n5; List<IMyInventoryItem> n6; string n7 = "";
        bool n8(bool cX)
        {
            if (!cX) { mZ = 0; m_ = 0; }
            for (; mZ < mU.qp(); mZ++)
            {
                if (m_ == 0)
                {
                    if (!s7.sv(50)) return false; n4 = mU.q4[mZ]; n5 = n4.GetInventory(0);
                    if (n5 == null) continue; m_++; cX = false;
                }
                if (!cX) { n6 = n5.GetItems(); n7 = (n6.Count > 0 ? n6[0].Content.ToString() : ""); n3 = 0; n0 = 0; n1 = 0; n2 = 0; }
                for (
; n3 < n6.Count; n3++)
                {
                    if (!s7.sv(30)) return false; IMyInventoryItem cY = n6[n3]; if (cY.Content.ToString() != n7) n2 += (double)cY.Amount;
                    else
                        n0 += (double)cY.Amount;
                }
                var cZ = nD.T("A1"); var c_ = n4.CustomName; if (n0 > 0 && (double)n5.CurrentVolume > 0)
                {
                    double d0 = n2 * (double)n5.
CurrentVolume / (n0 + n2); n1 = Math.Floor(n0 * ((double)n5.MaxVolume - d0) / (double)n5.CurrentVolume - d0); cZ = nB.tV(n0) + " / " + (n2 > 0 ? "~" : "") + nB.
tV(n1);
                }
                c_ = nB.u0(c_, nB.ta * 0.8f); nB.Add(c_); nB.tI(cZ); if (mV && n1 > 0) { double d1 = 100 * n0 / n1; nB.tM(d1); }
                m_ = 0; cX = false;
            }
            return true;
        }
    }
    class up : uz
    {
        public up() { s3 = 1; r_ = "CmdPosition"; }
        public override bool RunCmd(bool d2)
        {
            var d3 = (nA.pk == "posxyz"); var d4 = (nA.pk ==
"posgps"); IMyTerminalBlock d5 = nC.nV.rz; if (nA.pl != "" && nA.pl != "*")
            {
                d5 = nB.tx.GetBlockWithName(nA.pl); if (d5 == null)
                {
                    nB.tF("Pos: " + nD.T(
"P1") + ": " + nA.pl); return true;
                }
            }
            if (d4)
            {
                VRageMath.Vector3D d6 = d5.GetPosition(); nB.tF("GPS:" + nD.T("P2") + ":" + d6.GetDim(0).ToString(
"F2") + ":" + d6.GetDim(1).ToString("F2") + ":" + d6.GetDim(2).ToString("F2") + ":"); return true;
            }
            nB.Add(nD.T("P2") + ": "); if (!d3)
            {
                nB.tI(d5.
GetPosition().ToString("F0")); return true;
            }
            nB.tF(""); nB.Add(" X: "); nB.tI(d5.GetPosition().GetDim(0).ToString("F0")); nB.Add(" Y: "
); nB.tI(d5.GetPosition().GetDim(1).ToString("F0")); nB.Add(" Z: "); nB.tI(d5.GetPosition().GetDim(2).ToString("F0")); return true;
        }
    }
    class uq : uz
    {
        public uq() { s3 = 5; r_ = "CmdDetails"; }
        string n9 = ""; uK na; public override void Init()
        {
            na = new uK(s7, nB.tu); if (nA.pr.Count > 0
) n9 = nA.pr[0].pz.Trim();
        }
        int nb = 0; int nc = 1; IMyTerminalBlock nd; public override bool RunCmd(bool d7)
        {
            if (nA.pl == "" || nA.pl == "*")
            {
                nB.tF
("Details: " + nD.T("D1")); return true;
            }
            if (!d7) { na.qo(); nb = 0; nc = 1; }
            if (nb == 0)
            {
                if (!na.qe(nA.pl, d7)) return true; if (na.qp() <= 0)
                {
                    nB.tF(
"Details: " + nD.T("D2")); return true;
                }
                nb++; d7 = false;
            }
            int d8 = (nA.pk.EndsWith("x") ? 1 : 0); if (nb == 1)
            {
                if (!d7)
                {
                    nd = na.q4[0]; nB.tF(nd.
CustomName);
                }
                if (!nh(nd, d8, d7)) return false; nb++; d7 = false;
            }
            for (; nc < na.qp(); nc++)
            {
                if (!d7)
                {
                    nd = na.q4[nc]; nB.tF(""); nB.tF(nd.CustomName
);
                }
                if (!nh(nd, d8, d7)) return false; d7 = false;
            }
            return true;
        }
        string[] ne; int nf = 0; bool ng = false; bool nh(IMyTerminalBlock d9, int da, bool
db)
        {
            if (!db) { ne = (d9.DetailedInfo + "\n" + d9.CustomInfo).Split('\n'); nf = da; ng = (n9 == ""); }
            for (; nf < ne.Length; nf++)
            {
                if (!s7.sv(5)) return
false; if (ne[nf] == "") continue; if (!ng) { if (!ne[nf].Contains(n9)) continue; ng = true; }
                nB.tF("  " + ne[nf]);
            }
            return true;
        }
    }
    class ur : uz
    {
        public ur() { s3 = 1; r_ = "CmdShipMass"; }
        public override bool RunCmd(bool dc)
        {
            var dd = nA.pk.EndsWith("base"); double de = 0; if (nA.pl != "")
                double.TryParse(nA.pl.Trim(), out de); int df = nA.pr.Count; if (df > 0)
            {
                var dg = nA.pr[0].pz.Trim().ToLower(); if (dg != "") de *= Math.Pow(1000.0
, "kmgtpezy".IndexOf(dg[0]));
            }
            double dh = (dd ? nB.tv.pQ : nB.tv.pP); if (!dd) nB.Add(nD.T("SM1") + " "); else nB.Add(nD.T("SM2") + " "); nB.tI(nB
.tW(dh, true, 'k') + " "); if (de > 0) nB.tM(dh / de * 100); return true;
        }
    }
    class us : uz
    {
        public us() { s3 = 1; r_ = "CmdDistance"; }
        string ni = ""; string[]
nj; Vector3D nk; string nl = ""; bool nm = false; public override void Init()
        {
            nm = false; if (nA.pr.Count <= 0) return; ni = nA.pr[0].pz.Trim(); nj =
ni.Split(':'); if (nj.Length < 5 || nj[0] != "GPS") return; double di, dj, dk; if (!double.TryParse(nj[2], out di)) return; if (!double.TryParse(nj[
3], out dj)) return; if (!double.TryParse(nj[4], out dk)) return; nk = new Vector3D(di, dj, dk); nl = nj[1]; nm = true;
        }
        public override bool RunCmd
(bool dl)
        {
            if (!nm) { nB.tF("Distance: " + nD.T("DTU") + " '" + ni + "'."); return true; }
            IMyTerminalBlock dm = nC.nV.rz; if (nA.pl != "" && nA.pl != "*")
            { dm = nB.tx.GetBlockWithName(nA.pl); if (dm == null) { nB.tF("Distance: " + nD.T("P1") + ": " + nA.pl); return true; } }
            double dn = Vector3D.Distance
(dm.GetPosition(), nk); nB.Add(nl + ": "); nB.tI(nB.tV(dn) + "m "); return true;
        }
    }
    class ut : uz
    {
        public ut() { s3 = 1; r_ = "CmdAltitude"; }
        public
override bool RunCmd(bool dp)
        {
            var dq = (nA.pk.EndsWith("sea") ? "sea" : "ground"); switch (dq)
            {
                case "sea":
                    nB.Add(nD.T("ALT1")); nB.tI(nB.tv
.pS.ToString("F0") + " m"); break;
                default: nB.Add(nD.T("ALT2")); nB.tI(nB.tv.pU.ToString("F0") + " m"); break;
            }
            return true;
        }
    }
    class uu : uz
    {
        public uu() { s3 = 1; r_ = "CmdStopTask"; }
        public override bool RunCmd(bool dr)
        {
            double ds = 0; if (nA.pk.Contains("best")) ds = nB.tv.pG / nB.tv.pK
;
            else ds = nB.tv.pG / nB.tv.pN; double dt = nB.tv.pG / 2 * ds; if (nA.pk.Contains("time"))
            {
                nB.Add(nD.T("ST")); if (double.IsNaN(ds))
                {
                    nB.tI("N/A")
; return true;
                }
                var du = ""; try
                {
                    TimeSpan dv = TimeSpan.FromSeconds(ds); if ((int)dv.TotalDays > 0) du = " > 24h";
                    else
                    {
                        if (dv.Hours > 0) du = dv.Hours
+ "h "; if (dv.Minutes > 0 || du != "") du += dv.Minutes + "m "; du += dv.Seconds + "s";
                    }
                }
                catch { du = "N/A"; }
                nB.tI(du); return true;
            }
            nB.Add(nD.T("SD"));
            if (!double.IsNaN(dt) && !double.IsInfinity(dt)) nB.tI(nB.tV(dt) + "m "); else nB.tI("N/A"); return true;
        }
    }
    class uv : uz
    {
        public uv()
        {
            s3 = 1;
            r_ = "CmdGravity";
        }
        public override bool RunCmd(bool dw)
        {
            var dx = (nA.pk.Contains("nat") ? "n" : (nA.pk.Contains("art") ? "a" : (nA.pk.Contains
("tot") ? "t" : "s"))); Vector3D dy; switch (dx)
            {
                case "n":
                    nB.Add(nD.T("G2") + " "); dy = nB.tv.pX.GetNaturalGravity(); nB.tI(dy.Length().
ToString("F1") + " m/s²"); break;
                case "a":
                    nB.Add(nD.T("G3") + " "); dy = nB.tv.pX.GetArtificialGravity(); nB.tI(dy.Length().ToString("F1") +
" m/s²"); break;
                case "t": nB.Add(nD.T("G1") + " "); dy = nB.tv.pX.GetTotalGravity(); nB.tI(dy.Length().ToString("F1") + " m/s²"); break;
                default:
                    nB.Add(nD.T("GN")); nB.tJ(" | ", 0.33f); nB.tJ(nD.T("GA") + " | ", 0.66f); nB.tI(nD.T("GT"), 1.0f); nB.Add(""); dy = nB.tv.pX.
                      GetNaturalGravity(); nB.tJ(dy.Length().ToString("F1") + " | ", 0.33f); dy = nB.tv.pX.GetArtificialGravity(); nB.tJ(dy.Length().ToString(
                              "F1") + " | ", 0.66f); dy = nB.tv.pX.GetTotalGravity(); nB.tI(dy.Length().ToString("F1") + " "); break;
            }
            return true;
        }
    }
    class uw : uz
    {
        public uw
()
        { s3 = 1; r_ = "CmdCustomData"; }
        public override bool RunCmd(bool dz)
        {
            IMyTextPanel dA = nC.nV.rz; if (dA == null) return true; var dB = ""; if (nA.
pl != "" && nA.pl != "*")
            {
                IMyTerminalBlock dC = nB.tx.GetBlockWithName(nA.pl) as IMyTerminalBlock; if (dC == null)
                {
                    nB.tF("CustomData: " + nD.T(
"CD1") + nA.pl); return true;
                }
                dB = dC.CustomData;
            }
            else { nB.tF("CustomData:" + nD.T("CD2")); return true; }
            if (dB.Length == 0) return true; nB.tG(
dB); return true;
        }
    }
    class ux : uz
    {
        uK nn; public ux() { s3 = 1; r_ = "CmdProp"; }
        public override void Init() { nn = new uK(s7, nB.tu); }
        int no = 0; int
np = 0; bool nq = false; string nr = null; string ns = null; string nt = null; string nu = null; public override bool RunCmd(bool dD)
        {
            if (!dD)
            {
                nq = nA.
pk.StartsWith("props"); nr = ns = nt = nu = null; np = 0; no = 0;
            }
            if (nA.pr.Count < 1) { nB.tF(nA.pk + ": " + "Missing property name."); return true; }
            if (no
== 0) { if (!dD) nn.qo(); if (!nn.qe(nA.pl, dD)) return false; nv(); no++; dD = false; }
            if (no == 1)
            {
                int dE = nn.qp(); if (dE == 0)
                {
                    nB.tF(nA.pk + ": " +
"No blocks found."); return true;
                }
                for (; np < dE; np++)
                {
                    if (!s7.sv(50)) return false; IMyTerminalBlock dF = nn.q4[np]; if (dF.GetProperty(nr) !=
null) { if (ns == null) { var dG = nB.u0(dF.CustomName, nB.ta * 0.7f); nB.Add(dG); } else nB.Add(ns); nB.tI(nw(dF, nr, nt, nu)); if (!nq) return true; }
                }
            }
            return true;
        }
        void nv()
        {
            nr = nA.pr[0].pz; if (nA.pr.Count > 1)
            {
                if (!nq) ns = nA.pr[1].pz; else nt = nA.pr[1].pz; if (nA.pr.Count > 2)
                {
                    if (!nq) nt = nA.
pr[2].pz;
                    else nu = nA.pr[2].pz; if (nA.pr.Count > 3 && !nq) nu = nA.pr[3].pz;
                }
            }
        }
        string nw(IMyTerminalBlock dH, string dI, string dJ = null, string
dK = null)
        { return (dH.GetValue<bool>(dI) ? (dJ != null ? dJ : nD.T("W9")) : (dK != null ? dK : nD.T("W1"))); }
    }
    class uy : uz
    {
        public uy()
        {
            s3 = 0.5; r_ =
"CmdHScroll";
        }
        StringBuilder nx = new StringBuilder(); int ny = 1; public override bool RunCmd(bool dL)
        {
            if (nx.Length == 0)
            {
                var dM = nA.pn +
"  "; if (dM.Length == 0) return true; float dN = nB.ta; float dO = nB.t_(dM, nB.tB); float dP = dN / dO; if (dP > 1) nx.Append(string.Join("",
Enumerable.Repeat(dM, (int)Math.Ceiling(dP))));
                else nx.Append(dM); if (dM.Length > 40) ny = 3; else if (dM.Length > 5) ny = 2; else ny = 1; nB.tF(nx.
ToString()); return true;
            }
            var dQ = nA.pk.EndsWith("r"); if (dQ) { nx.Insert(0, nx.ToString(nx.Length - ny, ny)); nx.Remove(nx.Length - ny, ny); }
            else { nx.Append(nx.ToString(0, ny)); nx.Remove(0, ny); }
            nB.tF(nx.ToString()); return true;
        }
    }
    class uz : uU
    {
        public uW nz = null; protected uH
nA; protected uX nB; protected uC nC; protected TranslationTable nD; public uz() { s3 = 3600; r_ = "CommandTask"; }
        public void nE(uC dR, uH dS)
        { nC = dR; nB = nC.nU; nA = dS; nD = nB.tw; }
        public virtual bool RunCmd(bool dT) { nB.tF(nD.T("UC") + ": '" + nA.pm + "'"); return true; }
        public override
bool Run(bool dU)
        { nz = nB.tD(nz, nC.nV); if (!dU) nB.tO(); return RunCmd(dU); }
    }
    class uA : uU
    {
        uE nF; uX nG; string nH = ""; public uA(uX dV, uE
dW, string dX)
        { s3 = -1; r_ = "ArgScroll"; nH = dX; nF = dW; nG = dV; }
        int nI; uK nJ; public override void Init() { nJ = new uK(s7, nG.tu); }
        int nK = 0; int
nL = 0; uH nM; public override bool Run(bool dY)
        {
            if (!dY) { nL = 0; nJ.qo(); nM = new uH(s7); nK = 0; }
            if (nL == 0)
            {
                if (!nM.pw(nH, dY)) return false; if (
nM.pr.Count > 0) { if (!int.TryParse(nM.pr[0].pz, out nI)) nI = 1; else if (nI < 1) nI = 1; }
                if (nM.pk.EndsWith("up")) nI = -nI;
                else if (!nM.pk.EndsWith
("down")) nI = 0; nL++; dY = false;
            }
            if (nL == 1) { if (!nJ.qm("textpanel", nM.pl, dY)) return false; nL++; dY = false; }
            uS dZ; for (; nK < nJ.qp(); nK++)
            {
                if (
!s7.sv(20)) return false; IMyTextPanel d_ = nJ.q4[nK] as IMyTextPanel; if (!nF.ow.TryGetValue(d_, out dZ)) continue; if (dZ == null || dZ.rz != d_)
                    continue; if (dZ.rD) dZ.ry.sW = 10; if (nI > 0) dZ.ry.sV(nI); else if (nI < 0) dZ.ry.sU(-nI); else dZ.ry.sX(); dZ.rO();
            }
            return true;
        }
    }
    class uB : uU
    {
        uX nN; uE nO; public int nP = 0; public uB(uX e0, uE e1) { r_ = "BootPanelsTask"; s3 = 1; nN = e0; nO = e1; if (!nN.t3) { nP = int.MaxValue; nO.ox = true; } }
        TranslationTable nQ; public override void Init() { nQ = nN.tw; }
        public override bool Run(bool e2)
        {
            if (nP > nN.t4.Count) { sb(); return true; }
            if (nP == 0) { nO.ox = false; }
            nS(); nP++; return true;
        }
        public override void End() { nO.ox = true; }
        public void nR()
        {
            uF e3 = nO.os; for (int e4 = 0; e4 <
e3.oJ(); e4++) { uS e5 = e3.oL(e4); nN.tD(e5.ry, e5); nN.tO(); nN.tP(e5); }
            nP = (nN.t3 ? 0 : int.MaxValue);
        }
        public void nS()
        {
            uF e6 = nO.os; for (int
e7 = 0; e7 < e6.oJ(); e7++)
            {
                uS e8 = e6.oL(e7); nN.tD(e8.ry, e8); nN.tO(); if (e8.rz.FontSize > 3f) continue; nN.tK(nQ.T("B1")); double e9 = (double)nP
      / nN.t4.Count * 100; nN.tM(e9); if (nP == nN.t4.Count) { nN.tF(""); nN.tK("Automatic LCDs 2"); nN.tK("by MMaster"); } else nN.tG(nN.t4[nP]); var
                     ea = e8.rD; e8.rD = false; nN.tP(e8); e8.rD = ea;
            }
        }
        public bool nT() { return nP <= nN.t4.Count; }
    }
    class uC : uU
    {
        public uX nU; public uS nV; public
uD nW = null; string nX = "N/A"; public Dictionary<string, uz> nY = new Dictionary<string, uz>(); public List<string> nZ = null; public uE n_;
        public bool o0 { get { return n_.ox; } }
        public uC(uE eb, uS ec) { s3 = 5; nV = ec; n_ = eb; nU = eb.or; r_ = "PanelProcess"; }
        TranslationTable o1; public
override void Init()
        { o1 = nU.tw; }
        uH o2 = null; uz o3(string ed, bool ee)
        {
            if (!ee) o2 = new uH(s7); if (!o2.pw(ed, ee)) return null; uz ef = o2.pq()
; ef.nE(this, o2); s7.sl(ef, 0); return ef;
        }
        string o4 = ""; void o5()
        {
            try { o4 = nV.rz.CustomData; } catch { o4 = ""; nV.rz.CustomData = ""; }
            o4 = o4.
Replace("\\\n", "");
        }
        int o6 = 0; int o7 = 0; List<string> o8 = null; HashSet<string> o9 = new HashSet<string>(); int oa = 0; bool ob(bool eg)
        {
            if (!eg
)
            {
                char[] eh = { ';', '\n' }; var ei = o4.Replace("\\;", "\f"); o8 = new List<string>(ei.Split(eh, StringSplitOptions.RemoveEmptyEntries)); o9.
                            Clear(); o6 = 0; o7 = 0; oa = 0;
            } while (o6 < o8.Count)
            {
                if (!s7.sv(500)) return false; if (o8[o6].StartsWith("//")) { o8.RemoveAt(o6); continue; }
                o8[o6
] = o8[o6].Replace('\f', ';'); if (!nY.ContainsKey(o8[o6]))
                {
                    if (oa != 1) eg = false; oa = 1; uz ej = o3(o8[o6], eg); if (ej == null) return false; eg =
false; nY.Add(o8[o6], ej); oa = 0;
                }
                if (!o9.Contains(o8[o6])) o9.Add(o8[o6]); o6++;
            }
            if (nZ != null)
            {
                uz ek; while (o7 < nZ.Count)
                {
                    if (!s7.sv(7))
                        return false; if (!o9.Contains(nZ[o7])) if (nY.TryGetValue(nZ[o7], out ek)) { ek.sb(); nY.Remove(nZ[o7]); }
                    o7++;
                }
            }
            nZ = o8; return true;
        }
        public
override void End()
        {
            if (nZ != null) { uz el; for (int em = 0; em < nZ.Count; em++) { if (nY.TryGetValue(nZ[em], out el)) el.sb(); } nZ = null; }
            if (nW !=
null) { nW.sb(); nW = null; }
        }
        string oc = ""; bool od = false; public override bool Run(bool en)
        {
            if (nV.rx.rV() <= 0) { sb(); return true; }
            if (!en)
            {
                nV.ry = nU.tD(nV.ry, nV); o5(); if (nV.rz.CustomName != oc) { od = true; } else { od = false; }
                oc = nV.rz.CustomName;
            }
            if (o4 != nX)
            {
                if (!ob(en)) return
false; if (o4 == "") { if (n_.ox) { nU.tO(); nU.tF(o1.T("H1")); nU.tP(nV); return true; } return this.s9(2); }
                od = true;
            }
            nX = o4; if (nW != null && od)
            {
                s7.
sm(nW); nW.oi(); s7.sl(nW, 0);
            }
            else if (nW == null) { nW = new uD(this); s7.sl(nW, 0); }
            return true;
        }
    }
    class uD : uU
    {
        public uX oe; public uS of; uC
og; public uD(uC eo) { og = eo; oe = og.nU; of = og.nV; s3 = 0.5; r_ = "PanelDisplay"; }
        double oh = 0; public void oi() { oh = 0; }
        int oj = 0; int ok = 0; bool ol
= true; double om = double.MaxValue; int on = 0; public override bool Run(bool ep)
        {
            uz eq; if (!ep && (!og.o0 || og.nZ == null || og.nZ.Count <= 0))
                return true; if (og.n_.oq > 5) return s9(0); if (!ep) { ok = 0; ol = false; om = double.MaxValue; on = 0; }
            if (on == 0)
            {
                while (ok < og.nZ.Count)
                {
                    if (!s7.sv(5)
) return false; if (og.nY.TryGetValue(og.nZ[ok], out eq))
                    {
                        if (!eq.s5) return s9(eq.s0 - s7.sf + 0.001); if (eq.s1 > oh) ol = true; if (eq.s0 < om) om = eq
.s0;
                    }
                    ok++;
                }
                on++; ep = false;
            }
            double er = om - s7.sf + 0.001; if (!ol && !of.rE()) return s9(er); oe.tE(of); if (ol)
            {
                if (!ep)
                {
                    oh = s7.sf; oe.tO(); var es
= of.rz.CustomName; es = (es.Contains("#") ? es.Substring(es.LastIndexOf('#') + 1) : ""); if (es != "") oe.tF(es); oj = 0;
                } while (oj < og.nZ.Count)
                {
                    if (
!s7.sv(7)) return false; if (!og.nY.TryGetValue(og.nZ[oj], out eq)) { oe.tF("ERR: No cmd task (" + og.nZ[oj] + ")"); oj++; continue; }
                    oe.tH(eq.
nz.sO()); oj++;
                }
            }
            oe.tP(of); og.n_.oq++; if (s3 < er && !of.rE()) return s9(er); return true;
        }
    }
    class uE : uU
    {
        public int oq = 0; public uX or;
        public uF os = new uF(); uK ot; uK ou; Dictionary<uS, uC> ov = new Dictionary<uS, uC>(); public Dictionary<IMyTextPanel, uS> ow = new Dictionary<
                   IMyTextPanel, uS>(); public bool ox = false; uB oy = null; public uE(uX et) { s3 = 5; or = et; r_ = "ProcessPanels"; }
        public override void Init()
        {
            ot =
new uK(s7, or.tu); ou = new uK(s7, or.tu); oy = new uB(or, this);
        }
        int oz = 0; bool oA(bool eu)
        {
            if (!eu) oz = 0; if (oz == 0)
            {
                if (!ot.qm("textpanel", or.
t1, eu)) return false; oz++; eu = false;
            }
            if (oz == 1)
            {
                if (or.t1 == "T:[LCD]" && "T:!LCD!" != "") if (!ot.qm("textpanel", "T:!LCD!", eu)) return false;
                oz++; eu = false;
            }
            return true;
        }
        string oB(IMyTextPanel ev)
        {
            return ev.CustomName + " " + ev.NumberInGrid + " " + ev.GetPosition().ToString("F0"
);
        }
        void oC(IMyTextPanel ew)
        {
            uS ey = null; if (!ow.TryGetValue(ew, out ey)) { return; }
            ey.rx.rU(ew); ow.Remove(ew); if (ey.rx.rV() <= 0)
            {
                uC ez;
                if (ov.TryGetValue(ey, out ez)) { os.oM(ey.rC); ov.Remove(ey); ez.sb(); }
            }
        }
        int oD = 0; int oE = 0; public override bool Run(bool eA)
        {
            if (!eA)
            {
                ot
.qo(); oD = 0; oE = 0;
            }
            if (!oA(eA)) return false; while (oD < ot.qp())
            {
                if (!s7.sv(20)) return false; IMyTextPanel eB = (ot.q4[oD] as IMyTextPanel);
                if (eB == null || !eB.IsWorking) { ot.q4.RemoveAt(oD); continue; }
                uS eC = null; var eD = false; var eE = oB(eB); int eF = eE.IndexOf("!LINK:"); if (eF >=
0 && eE.Length > eF + 6) { eE = eE.Substring(eF + 6); eD = true; }
                string[] eG = eE.Split(' '); var eH = eG[0]; if (ow.ContainsKey(eB))
                {
                    eC = ow[eB]; if (eC.rC
== eE || (eD && eC.rC == eH)) { oD++; continue; }
                    this.oC(eB);
                }
                if (!eD)
                {
                    eC = new uS(or, eE); eC.rx.rS(eE, eB); uC eI = new uC(this, eC); s7.sl(eI, 0); ov.
Add(eC, eI); os.oI(eE, eC); ow.Add(eB, eC); oD++; continue;
                }
                eC = os.oK(eH); if (eC == null)
                {
                    eC = new uS(or, eH); os.oI(eH, eC); uC eI = new uC(this, eC)
; s7.sl(eI, 0); ov.Add(eC, eI);
                }
                eC.rx.rS(eE, eB); ow.Add(eB, eC); oD++;
            } while (oE < ou.qp())
            {
                if (!s7.sv(300)) return false; IMyTextPanel eB = ou.
q4[oE] as IMyTextPanel; if (eB == null) continue; if (!ot.q4.Contains(eB)) { this.oC(eB); }
                oE++;
            }
            ou.qo(); ou.qn(ot); if (!oy.s4 && oy.nT()) s7.sl(
oy, 0); return true;
        }
        public bool oF(string eL)
        {
            var eM = eL.ToLower(); if (eM == "clear") { oy.nR(); if (!oy.s4) s7.sl(oy, 0); return true; }
            if (eM
== "boot") { oy.nP = 0; if (!oy.s4) s7.sl(oy, 0); return true; }
            if (eM.StartsWith("scroll"))
            {
                uA eN = new uA(or, this, eL); s7.sl(eN, 0); return true;
            }
            if (eM == "props")
            {
                uL eO = or.tu; var eP = new List<IMyTerminalBlock>(); var eQ = new List<ITerminalAction>(); var eR = new List<
        ITerminalProperty>(); IMyTextPanel eS = s7.sj.GridTerminalSystem.GetBlockWithName("DEBUG") as IMyTextPanel; if (eS == null) { return true; }
                eS.WritePublicText("Properties: "); foreach (var item in eO.qu)
                {
                    eS.WritePublicText(item.Key + " ==============" + "\n", true); item.Value(
eP, null); if (eP.Count <= 0) { eS.WritePublicText("No blocks\n", true); continue; }
                    eP[0].GetProperties(eR, eT => {
                        return eT.Id != "Name" && eT.Id
!= "OnOff" && !eT.Id.StartsWith("Show");
                    }); foreach (var prop in eR) { eS.WritePublicText("P " + prop.Id + " " + prop.TypeName + "\n", true); }
                    eR.
Clear(); eP.Clear();
                }
            }
            return false;
        }
    }
    public class uF
    {
        Dictionary<string, uS> oG = new Dictionary<string, uS>(); List<string> oH = new List<
string>(); public void oI(string eU, uS eV) { if (!oG.ContainsKey(eU)) { oH.Add(eU); oG.Add(eU, eV); } }
        public int oJ() { return oG.Count; }
        public uS oK(string eW) { if (oG.ContainsKey(eW)) return oG[eW]; return null; }
        public uS oL(int eX) { return oG[oH[eX]]; }
        public void oM(
string eY)
        { oG.Remove(eY); oH.Remove(eY); }
        public void oN() { oH.Clear(); oG.Clear(); }
        public void oO() { oH.Sort(); }
    }
    public enum uG
    {
        oP = 0,
        oQ = 1, oR = 2, oS = 3, oT = 4, oU = 5, oV = 6, oW = 7, oX = 8, oY = 9, oZ = 10, o_ = 11, p0 = 12, p1 = 13, p2 = 14, p3 = 15, p4 = 16, p5 = 17, p6 = 18, p7 = 19, p8 = 20, p9 = 21, pa = 22, pb = 23,
        pc = 24, pd = 25, pe = 26, pf = 27, pg = 28, ph = 29, pi = 30,
    }
    class uH
    {
        uV pj; public string pk = ""; public string pl = ""; public string pm = ""; public
string pn = ""; public uG po = uG.oP; public uH(uV eZ) { pj = eZ; }
        uG pp()
        {
            if (pk == "echo" || pk == "center" || pk == "right") return uG.oQ; if (pk.
StartsWith("hscroll")) return uG.pi; if (pk.StartsWith("inventory") || pk == "missing" || pk.StartsWith("invlist")) return uG.oR; if (pk.
StartsWith("working")) return uG.p6; if (pk.StartsWith("cargo")) return uG.oS; if (pk.StartsWith("mass")) return uG.oT; if (pk.StartsWith(
"shipmass")) return uG.pb; if (pk == "oxygen") return uG.oU; if (pk.StartsWith("tanks")) return uG.oV; if (pk.StartsWith("powertime")) return
uG.oW; if (pk.StartsWith("powerused")) return uG.oX; if (pk.StartsWith("power")) return uG.oY; if (pk.StartsWith("speed")) return uG.oZ; if (
  pk.StartsWith("accel")) return uG.o_; if (pk.StartsWith("alti")) return uG.pd; if (pk.StartsWith("charge")) return uG.p0; if (pk.StartsWith
          ("time") || pk.StartsWith("date")) return uG.p1; if (pk.StartsWith("countdown")) return uG.p2; if (pk.StartsWith("textlcd")) return uG.p3;
            if (pk.EndsWith("count")) return uG.p4; if (pk.StartsWith("dampeners") || pk.StartsWith("occupied")) return uG.p5; if (pk.StartsWith(
                    "damage")) return uG.p7; if (pk.StartsWith("amount")) return uG.p8; if (pk.StartsWith("pos")) return uG.p9; if (pk.StartsWith("distance"))
                return uG.pc; if (pk.StartsWith("details")) return uG.pa; if (pk.StartsWith("stop")) return uG.pe; if (pk.StartsWith("gravity")) return uG.
                         pf; if (pk.StartsWith("customdata")) return uG.pg; if (pk.StartsWith("prop")) return uG.ph; return uG.oP;
        }
        public uz pq()
        {
            switch (po)
            {
                case
uG.oQ:
                    return new uf();
                case uG.oR: return new u3();
                case uG.oS: return new u4();
                case uG.oT: return new u5();
                case uG.oU: return new u6();
                case uG.oV: return new u7();
                case uG.oW: return new u8();
                case uG.oX: return new ua();
                case uG.oY: return new uc();
                case uG.oZ:
                    return new
ud();
                case uG.o_: return new ue();
                case uG.p0: return new ug();
                case uG.p1: return new uh();
                case uG.p2: return new ui();
                case uG.p3:
                    return
new uj();
                case uG.p4: return new uk();
                case uG.p5: return new ul();
                case uG.p6: return new um();
                case uG.p7: return new un();
                case uG.p8:
                    return new uo();
                case uG.p9: return new up();
                case uG.pa: return new uq();
                case uG.pb: return new ur();
                case uG.pc: return new us();
                case
uG.pd:
                    return new ut();
                case uG.pe: return new uu();
                case uG.pf: return new uv();
                case uG.pg: return new uw();
                case uG.ph: return new ux();
                case uG.pi: return new uy();
                default: return new uz();
            }
        }
        public List<uI> pr = new List<uI>(); string[] ps = null; string pt = ""; bool pu = false;
        int pv = 1; public bool pw(string e_, bool f0)
        {
            if (!f0)
            {
                po = uG.oP; pl = ""; pk = ""; pm = e_.TrimStart(' '); pr.Clear(); if (pm == "") return true; int
f1 = pm.IndexOf(' '); if (f1 < 0 || f1 >= pm.Length - 1) pn = ""; else pn = pm.Substring(f1 + 1); ps = pm.Split(' '); pt = ""; pu = false; pk = ps[0].ToLower(); pv
                = 1;
            }
            for (; pv < ps.Length; pv++)
            {
                if (!pj.sv(40)) return false; var f2 = ps[pv]; if (f2 == "") continue; if (f2[0] == '{' && f2[f2.Length - 1] == '}')
                {
                    f2 = f2
.Substring(1, f2.Length - 2); if (f2 == "") continue; if (pl == "") pl = f2; else pr.Add(new uI(f2)); continue;
                }
                if (f2[0] == '{')
                {
                    pu = true; pt = f2.
Substring(1); continue;
                }
                if (f2[f2.Length - 1] == '}')
                {
                    pu = false; pt += ' ' + f2.Substring(0, f2.Length - 1); if (pl == "") pl = pt;
                    else pr.Add(new uI(pt
)); continue;
                }
                if (pu) { if (pt.Length != 0) pt += ' '; pt += f2; continue; }
                if (pl == "") pl = f2; else pr.Add(new uI(f2));
            }
            po = pp(); return true;
        }
    }
    public
class uI
    {
        public string px = ""; public string py = ""; public string pz = ""; public List<string> pA = new List<string>(); public uI(string f3
           )
        { pz = f3; }
        public void pB()
        {
            if (pz == "" || px != "" || py != "" || pA.Count > 0) return; var f4 = pz.Trim(); if (f4[0] == '+' || f4[0] == '-')
            {
                px += f4[0]; f4 = pz
.Substring(1);
            }
            string[] f5 = f4.Split('/'); var f6 = f5[0]; if (f5.Length > 1) { py = f5[0]; f6 = f5[1]; } else py = ""; if (f6.Length > 0)
            {
                string[] f7 = f6.
Split(','); for (int f8 = 0; f8 < f7.Length; f8++) if (f7[f8] != "") pA.Add(f7[f8]);
            }
        }
    }
    public class uJ : uU
    {
        MyShipVelocities pC; public VRageMath
.Vector3D pD
        { get { return pC.LinearVelocity; } }
        public VRageMath.Vector3D pE { get { return pC.AngularVelocity; } }
        double pF = 0; public double
pG
        { get { if (pV != null) return pV.GetShipSpeed(); else return pF; } }
        double pH = 0; public double pI { get { return pH; } }
        double pJ = 0; public
double pK
        { get { return pJ; } }
        double pL = 0; double pM = 0; public double pN { get { return pL; } }
        MyShipMass pO; public double pP
        {
            get
            {
                return pO.
TotalMass;
            }
        }
        public double pQ { get { return pO.BaseMass; } }
        double pR = double.NaN; public double pS { get { return pR; } }
        double pT = double.NaN;
        public double pU { get { return pT; } }
        IMyShipController pV = null; IMySlimBlock pW = null; public IMyShipController pX { get { return pV; } }
        VRageMath.Vector3D pY; public uJ(uV f9) { r_ = "ShipMgr"; s7 = f9; pY = s7.sj.Me.GetPosition(); s3 = 0.5; }
        List<IMyTerminalBlock> pZ = new List<
IMyTerminalBlock>(); int p_ = 0; public override bool Run(bool fa)
        {
            if (!fa)
            {
                pZ.Clear(); s7.sj.GridTerminalSystem.GetBlocksOfType<
IMyShipController>(pZ); p_ = 0; if (pV != null && pV.CubeGrid.GetCubeBlock(pV.Position) != pW) pV = null;
            }
            if (pZ.Count > 0)
            {
                for (; p_ < pZ.Count; p_++)
                {
                    if (!s7.sv(20)) return false; IMyShipController fb = pZ[p_] as IMyShipController; if (fb.IsMainCockpit || fb.IsUnderControl)
                    {
                        pV = fb; pW = fb.
CubeGrid.GetCubeBlock(fb.Position); if (fb.IsMainCockpit) { p_ = pZ.Count; break; }
                    }
                }
                if (pV == null)
                {
                    pV = pZ[0] as IMyShipController; pW = pV.
CubeGrid.GetCubeBlock(pV.Position);
                }
                pO = pV.CalculateShipMass(); if (!pV.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out pR)) pR =
double.NaN; if (!pV.TryGetPlanetElevation(MyPlanetElevation.Surface, out pT)) pT = double.NaN; pC = pV.GetShipVelocities();
            }
            double fc = pF; pF
= pD.Length(); pH = (pF - fc) / s2; if (-pH > pJ) pJ = -pH; if (-pH > pL) { pL = -pH; pM = s7.sf; }
            if (s7.sf - pM > 5 && -pH > 0.1) pL -= (pL + pH) * 0.3f; return true;
        }
    }
    public class uK
    {
        uV q0 = null; uL q1; IMyCubeGrid q2 { get { return q0.sj.Me.CubeGrid; } }
        IMyGridTerminalSystem q3
        {
            get
            {
                return q0.sj.
GridTerminalSystem;
            }
        }
        public List<IMyTerminalBlock> q4 = new List<IMyTerminalBlock>(); public uK(uV fd, uL fe) { q0 = fd; q1 = fe; }
        int q5 = 0;
        public double q6(ref double ff, ref double fg, bool fh)
        {
            if (!fh) q5 = 0; for (; q5 < q4.Count; q5++)
            {
                if (!q0.sv(4)) return Double.NaN;
                IMyInventory fi = q4[q5].GetInventory(0); if (fi == null) continue; ff += (double)fi.CurrentVolume; fg += (double)fi.MaxVolume;
            }
            ff *= 1000; fg *=
1000; return (fg > 0 ? ff / fg * 100 : 100);
        }
        int q7 = 0; double q8 = 0; public double q9(bool fj)
        {
            if (!fj) { q7 = 0; q8 = 0; }
            for (; q7 < q4.Count; q7++)
            {
                if (!q0.
sv(6)) return Double.NaN; for (int fk = 0; fk < 2; fk++)
                {
                    IMyInventory fl = q4[q7].GetInventory(fk); if (fl == null) continue; q8 += (double)fl.
CurrentMass;
                }
            }
            return q8 * 1000;
        }
        int qa = 0; bool qb(bool fm = false)
        {
            if (!fm) qa = 0; while (qa < q4.Count)
            {
                if (!q0.sv(4)) return false; if (q4[qa].
CubeGrid != q2) { q4.RemoveAt(qa); continue; }
                qa++;
            }
            return true;
        }
        List<IMyBlockGroup> qc = new List<IMyBlockGroup>(); int qd = 0; public bool qe
(string fn, bool fo)
        {
            int fp = fn.IndexOf(':'); var fq = (fp >= 1 && fp <= 2 ? fn.Substring(0, fp) : ""); var fr = fq.Contains("T"); if (fq != "") fn = fn.
                Substring(fp + 1); if (fn == "" || fn == "*")
            {
                if (!fo) { var fs = new List<IMyTerminalBlock>(); q3.GetBlocks(fs); q4.AddList(fs); }
                if (fr) if (!qb(fo))
                        return false; return true;
            }
            var ft = (fq.Contains("G") ? fn.Trim().ToLower() : ""); if (ft != "")
            {
                if (!fo)
                {
                    qc.Clear(); q3.GetBlockGroups(qc); qd =
0;
                }
                for (; qd < qc.Count; qd++)
                {
                    IMyBlockGroup fu = qc[qd]; if (fu.Name.ToLower() == ft)
                    {
                        if (!fo) fu.GetBlocks(q4); if (fr) if (!qb(fo)) return false;
                        return true;
                    }
                }
                return true;
            }
            if (!fo) q3.SearchBlocksOfName(fn, q4); if (fr) if (!qb(fo)) return false; return true;
        }
        List<IMyBlockGroup> qf =
new List<IMyBlockGroup>(); List<IMyTerminalBlock> qg = new List<IMyTerminalBlock>(); int qh = 0; int qi = 0; public bool qj(string fv, string
fw, bool fx, bool fy)
        {
            if (!fy) { qf.Clear(); q3.GetBlockGroups(qf); qh = 0; }
            for (; qh < qf.Count; qh++)
            {
                IMyBlockGroup fz = qf[qh]; if (fz.Name.
ToLower() == fw)
                {
                    if (!fy) { qi = 0; qg.Clear(); fz.GetBlocks(qg); } else fy = false; for (; qi < qg.Count; qi++)
                    {
                        if (!q0.sv(6)) return false; if (fx && qg[
qi].CubeGrid != q2) continue; if (q1.qy(qg[qi], fv)) q4.Add(qg[qi]);
                    }
                    return true;
                }
            }
            return true;
        }
        List<IMyTerminalBlock> qk = new List<
IMyTerminalBlock>(); int ql = 0; public bool qm(string fA, string fB, bool fC)
        {
            int fD = fB.IndexOf(':'); var fE = (fD >= 1 && fD <= 2 ? fB.Substring(
0, fD) : ""); var fF = fE.Contains("T"); if (fE != "") fB = fB.Substring(fD + 1); if (!fC) { qk.Clear(); ql = 0; }
            var fG = (fE.Contains("G") ? fB.Trim().
ToLower() : ""); if (fG != "") { if (!qj(fA, fG, fF, fC)) return false; return true; }
            if (!fC) q1.qx(ref qk, fA); if (fB == "" || fB == "*")
            {
                if (!fC) q4.
AddList(qk); if (fF) if (!qb(fC)) return false; return true;
            }
            for (; ql < qk.Count; ql++)
            {
                if (!q0.sv(4)) return false; if (fF && qk[ql].CubeGrid != q2
) continue; if (qk[ql].CustomName.Contains(fB)) q4.Add(qk[ql]);
            }
            return true;
        }
        public void qn(uK fH) { q4.AddList(fH.q4); }
        public void qo()
        { q4.Clear(); }
        public int qp() { return q4.Count; }
    }
    public class uL
    {
        uV qq; uX qr; public MyGridProgram qs { get { return qq.sj; } }
        public
IMyGridTerminalSystem qt
        { get { return qq.sj.GridTerminalSystem; } }
        public Dictionary<string, Action<List<IMyTerminalBlock>, Func<
IMyTerminalBlock, bool>>> qu = null; public uL(uV fI, uX fJ) { qq = fI; qr = fJ; }
        public void qv()
        {
            if (qu != null && qt.GetBlocksOfType<
IMyCargoContainer> == qu["CargoContainer"]) return; qu = new Dictionary<string, Action<List<IMyTerminalBlock>, Func<IMyTerminalBlock, bool>
>>(){{"CargoContainer",qt.GetBlocksOfType<IMyCargoContainer>},{"TextPanel",qt.GetBlocksOfType<IMyTextPanel>},{"Assembler",qt.
GetBlocksOfType<IMyAssembler>},{"Refinery",qt.GetBlocksOfType<IMyRefinery>},{"Reactor",qt.GetBlocksOfType<IMyReactor>},{
"SolarPanel",qt.GetBlocksOfType<IMySolarPanel>},{"BatteryBlock",qt.GetBlocksOfType<IMyBatteryBlock>},{"Beacon",qt.GetBlocksOfType<
IMyBeacon>},{"RadioAntenna",qt.GetBlocksOfType<IMyRadioAntenna>},{"AirVent",qt.GetBlocksOfType<IMyAirVent>},{"ConveyorSorter",qt.
GetBlocksOfType<IMyConveyorSorter>},{"OxygenTank",qt.GetBlocksOfType<IMyGasTank>},{"OxygenGenerator",qt.GetBlocksOfType<
IMyGasGenerator>},{"OxygenFarm",qt.GetBlocksOfType<IMyOxygenFarm>},{"LaserAntenna",qt.GetBlocksOfType<IMyLaserAntenna>},{"Thrust",
qt.GetBlocksOfType<IMyThrust>},{"Gyro",qt.GetBlocksOfType<IMyGyro>},{"SensorBlock",qt.GetBlocksOfType<IMySensorBlock>},{
"ShipConnector",qt.GetBlocksOfType<IMyShipConnector>},{"ReflectorLight",qt.GetBlocksOfType<IMyReflectorLight>},{"InteriorLight",qt
.GetBlocksOfType<IMyInteriorLight>},{"LandingGear",qt.GetBlocksOfType<IMyLandingGear>},{"ProgrammableBlock",qt.GetBlocksOfType<
IMyProgrammableBlock>},{"TimerBlock",qt.GetBlocksOfType<IMyTimerBlock>},{"MotorStator",qt.GetBlocksOfType<IMyMotorStator>},{
"PistonBase",qt.GetBlocksOfType<IMyPistonBase>},{"Projector",qt.GetBlocksOfType<IMyProjector>},{"ShipMergeBlock",qt.
GetBlocksOfType<IMyShipMergeBlock>},{"SoundBlock",qt.GetBlocksOfType<IMySoundBlock>},{"Collector",qt.GetBlocksOfType<IMyCollector>
},{"JumpDrive",qt.GetBlocksOfType<IMyJumpDrive>},{"Door",qt.GetBlocksOfType<IMyDoor>},{"GravityGeneratorSphere",qt.GetBlocksOfType
<IMyGravityGeneratorSphere>},{"GravityGenerator",qt.GetBlocksOfType<IMyGravityGenerator>},{"ShipDrill",qt.GetBlocksOfType<
IMyShipDrill>},{"ShipGrinder",qt.GetBlocksOfType<IMyShipGrinder>},{"ShipWelder",qt.GetBlocksOfType<IMyShipWelder>},{"Parachute",qt
.GetBlocksOfType<IMyParachute>},{"LargeGatlingTurret",qt.GetBlocksOfType<IMyLargeGatlingTurret>},{"LargeInteriorTurret",qt.
GetBlocksOfType<IMyLargeInteriorTurret>},{"LargeMissileTurret",qt.GetBlocksOfType<IMyLargeMissileTurret>},{"SmallGatlingGun",qt.
GetBlocksOfType<IMySmallGatlingGun>},{"SmallMissileLauncherReload",qt.GetBlocksOfType<IMySmallMissileLauncherReload>},{
"SmallMissileLauncher",qt.GetBlocksOfType<IMySmallMissileLauncher>},{"VirtualMass",qt.GetBlocksOfType<IMyVirtualMass>},{"Warhead",
qt.GetBlocksOfType<IMyWarhead>},{"FunctionalBlock",qt.GetBlocksOfType<IMyFunctionalBlock>},{"LightingBlock",qt.GetBlocksOfType<
IMyLightingBlock>},{"ControlPanel",qt.GetBlocksOfType<IMyControlPanel>},{"Cockpit",qt.GetBlocksOfType<IMyCockpit>},{"MedicalRoom",
qt.GetBlocksOfType<IMyMedicalRoom>},{"RemoteControl",qt.GetBlocksOfType<IMyRemoteControl>},{"ButtonPanel",qt.GetBlocksOfType<
IMyButtonPanel>},{"CameraBlock",qt.GetBlocksOfType<IMyCameraBlock>},{"OreDetector",qt.GetBlocksOfType<IMyOreDetector>},{
"ShipController",qt.GetBlocksOfType<IMyShipController>}};
        }
        public void qw(ref List<IMyTerminalBlock> fK, string fL)
        {
            Action<List<
IMyTerminalBlock>, Func<IMyTerminalBlock, bool>> fM = null; if (qu.TryGetValue(fL, out fM)) fM(fK, null);
            else
            {
                if (fL == "CryoChamber")
                {
                    qt.
GetBlocksOfType<IMyCockpit>(fK, fN => fN.BlockDefinition.ToString().Contains("Cryo")); return;
                }
            }
        }
        public void qx(ref List<
IMyTerminalBlock> fO, string fP)
        { qw(ref fO, qz(fP.Trim())); }
        public bool qy(IMyTerminalBlock fQ, string fR)
        {
            var fS = qz(fR); switch (fS)
            {
                case "FunctionalBlock": return true;
                case "ShipController": return (fQ as IMyShipController != null);
                default:
                    return fQ.BlockDefinition.
ToString().Contains(qz(fR));
            }
        }
        public string qz(string fT)
        {
            fT = fT.ToLower(); if (fT.StartsWith("carg") || fT.StartsWith("conta")) return
"CargoContainer"; if (fT.StartsWith("text") || fT.StartsWith("lcd")) return "TextPanel"; if (fT.StartsWith("ass")) return "Assembler"; if (
fT.StartsWith("refi")) return "Refinery"; if (fT.StartsWith("reac")) return "Reactor"; if (fT.StartsWith("solar")) return "SolarPanel"; if
(fT.StartsWith("bat")) return "BatteryBlock"; if (fT.StartsWith("bea")) return "Beacon"; if (fT.Contains("vent")) return "AirVent"; if (fT.
Contains("sorter")) return "ConveyorSorter"; if (fT.Contains("tank")) return "OxygenTank"; if (fT.Contains("farm") && fT.Contains("oxy"))
                return "OxygenFarm"; if (fT.Contains("gene") && fT.Contains("oxy")) return "OxygenGenerator"; if (fT.Contains("cryo")) return
                        "CryoChamber"; if (fT == "laserantenna") return "LaserAntenna"; if (fT.Contains("antenna")) return "RadioAntenna"; if (fT.StartsWith(
                                 "thrust")) return "Thrust"; if (fT.StartsWith("gyro")) return "Gyro"; if (fT.StartsWith("sensor")) return "SensorBlock"; if (fT.Contains(
                                         "connector")) return "ShipConnector"; if (fT.StartsWith("reflector")) return "ReflectorLight"; if ((fT.StartsWith("inter") && fT.EndsWith(
                                              "light"))) return "InteriorLight"; if (fT.StartsWith("land")) return "LandingGear"; if (fT.StartsWith("program")) return
                                                     "ProgrammableBlock"; if (fT.StartsWith("timer")) return "TimerBlock"; if (fT.StartsWith("motor")) return "MotorStator"; if (fT.StartsWith(
                                                            "piston")) return "PistonBase"; if (fT.StartsWith("proj")) return "Projector"; if (fT.Contains("merge")) return "ShipMergeBlock"; if (fT.
                                                                    StartsWith("sound")) return "SoundBlock"; if (fT.StartsWith("col")) return "Collector"; if (fT.Contains("jump")) return "JumpDrive"; if (fT
                                                                            == "door") return "Door"; if ((fT.Contains("grav") && fT.Contains("sphe"))) return "GravityGeneratorSphere"; if (fT.Contains("grav")) return
                                                                                      "GravityGenerator"; if (fT.EndsWith("drill")) return "ShipDrill"; if (fT.Contains("grind")) return "ShipGrinder"; if (fT.EndsWith("welder"
                                                                                             )) return "ShipWelder"; if (fT.StartsWith("parach")) return "Parachute"; if ((fT.Contains("turret") && fT.Contains("gatl"))) return
                                                                                                      "LargeGatlingTurret"; if ((fT.Contains("turret") && fT.Contains("inter"))) return "LargeInteriorTurret"; if ((fT.Contains("turret") && fT.
                                                                                                            Contains("miss"))) return "LargeMissileTurret"; if (fT.Contains("gatl")) return "SmallGatlingGun"; if ((fT.Contains("launcher") && fT.
                                                                                                                 Contains("reload"))) return "SmallMissileLauncherReload"; if ((fT.Contains("launcher"))) return "SmallMissileLauncher"; if (fT.Contains(
                                                                                                                      "mass")) return "VirtualMass"; if (fT == "warhead") return "Warhead"; if (fT.StartsWith("func")) return "FunctionalBlock"; if (fT == "shipctrl"
                                                                                                                                ) return "ShipController"; if (fT.StartsWith("light")) return "LightingBlock"; if (fT.StartsWith("contr")) return "ControlPanel"; if (fT.
                                                                                                                                        StartsWith("coc")) return "Cockpit"; if (fT.StartsWith("medi")) return "MedicalRoom"; if (fT.StartsWith("remote")) return "RemoteControl"
                                                                                                                                               ; if (fT.StartsWith("but")) return "ButtonPanel"; if (fT.StartsWith("cam")) return "CameraBlock"; if (fT.Contains("detect")) return
                                                                                                                                                        "OreDetector"; return "Unknown";
        }
        public List<double> qA(IMyTerminalBlock fU, int fV = -1)
        {
            var fW = new List<double>(); string[] fX = fU.
DetailedInfo.Split('\n'); int fY = Math.Min(fX.Length, (fV > 0 ? fV : fX.Length)); for (int fZ = 0; fZ < fY; fZ++)
            {
                string[] f_ = fX[fZ].Split(':'); if (
f_.Length < 2) { f_ = fX[fZ].Split('r'); if (f_.Length < 2) f_ = fX[fZ].Split('x'); }
                var g0 = (f_.Length < 2 ? f_[0] : f_[1]); string[] g1 = g0.Trim().Split
(' '); var g2 = g1[0].Trim(); var g3 = (g1.Length > 1 && g1[1].Length > 1 ? g1[1][0] : '.'); double g4; if (Double.TryParse(g2, out g4))
                {
                    double g5 = g4 *
Math.Pow(1000.0, ".kMGTPEZY".IndexOf(g3)); fW.Add(g5);
                }
            }
            return fW;
        }
        public string qB(IMyBatteryBlock g6)
        {
            var g7 = ""; if (g6.OnlyRecharge
) g7 = "(+) ";
            else if (g6.OnlyDischarge) g7 = "(-) "; else g7 = "(±) "; return g7 + qr.tX((g6.CurrentStoredPower / g6.MaxStoredPower) * 100.0f) + "%"
         ;
        }
        public string qC(IMyLaserAntenna g8) { string[] g9 = g8.DetailedInfo.Split('\n'); return g9[g9.Length - 1].Split(' ')[0].ToUpper(); }
        public double qD(IMyJumpDrive ga, out double gb, out double gc)
        {
            List<double> gd = qA(ga, 5); if (gd.Count < 4) { gc = 0; gb = 0; return 0; }
            gc = gd[1];
            gb = gd[3]; return (gc > 0 ? gb / gc * 100 : 0);
        }
        public double qE(IMyJumpDrive ge)
        {
            List<double> gf = qA(ge, 5); double gg = 0, gh = 0; if (gf.Count < 4) return
0; gg = gf[1]; gh = gf[3]; return (gg > 0 ? gh / gg * 100 : 0);
        }
    }
    public class uM
    {
        public Dictionary<string, uN> qF = new Dictionary<string, uN>();
        Dictionary<string, uN> qG = new Dictionary<string, uN>(); public List<string> qH = new List<string>(); public Dictionary<string, uN> qI = new
                  Dictionary<string, uN>(); public void Add(string gi, string gj, int gk, string gl, string gm, bool gn)
        {
            if (gj == "Ammo") gj = "AmmoMagazine";
            else if (gj == "Tool") gj = "PhysicalGunObject"; var go = gi + ' ' + gj; uN gp = new uN(gi, gj, gk, gl, gm, gn); qF.Add(go, gp); if (!qG.ContainsKey(gi)) qG
                                      .Add(gi, gp); if (gm != "") qI.Add(gm.ToLower(), gp); qH.Add(go);
        }
        public uN qJ(string gq = "", string gr = "")
        {
            if (qF.ContainsKey(gq + " " + gr))
                return qF[gq + " " + gr]; if (gr == "") { uN gs = null; qG.TryGetValue(gq, out gs); return gs; }
            if (gq == "") for (int gt = 0; gt < qF.Count; gt++)
                {
                    uN gs = qF[
qH[gt]]; if (gr == gs.qL) return gs;
                }
            return null;
        }
    }
    public class uN
    {
        public string qK; public string qL; public int qM; public string qN;
        public string qO; public bool qP; public uN(string gv, string gw, int gx = 0, string gy = "", string gz = "", bool gA = true)
        {
            qK = gv; qL = gw; qM = gx;
            qN = gy; qO = gz; qP = gA;
        }
    }
    public class uO
    {
        readonly Dictionary<string, string> qQ = new Dictionary<string, string>(){{"ingot","ingot" },{
"ore","ore" },{"component","component" },{"tool","physicalgunobject" },{"ammo","ammomagazine" },{"oxygen","oxygencontainerobject"
},{"gas","gascontainerobject" }}; uV qR; uX qS; uQ qT; uQ qU; uQ qV; uM MMItems; bool qW; public uQ qX; public uO(uV gB, uX gC, int gD = 20)
        {
            qT
= new uQ(); qU = new uQ(); qV = new uQ(); qW = false; qX = new uQ(); qR = gB; qS = gC; MMItems = qS.MMItems;
        }
        public void qY()
        {
            qV.rk(); qU.rk(); qT.rk(); qW
= false; qX.rk();
        }
        public void qZ(string gE, bool gF = false, int gG = 1, int gH = -1)
        {
            if (gE == "") { qW = true; return; }
            string[] gI = gE.Split(' '); var
gJ = ""; uP gK = new uP(gF, gG, gH); if (gI.Length == 2) { if (!qQ.TryGetValue(gI[1], out gJ)) gJ = gI[1]; }
            var gL = gI[0]; if (qQ.TryGetValue(gL, out gK.
rb)) { qU.rg(gK.rb, gK); return; }
            qS.tU(ref gL, ref gJ); if (gJ == "") { gK.ra = gL.ToLower(); qT.rg(gK.ra, gK); return; }
            gK.ra = gL; gK.rb = gJ; qV.rg(gL
.ToLower() + ' ' + gJ.ToLower(), gK);
        }
        public uP q_(string gM, string gN, string gO)
        {
            uP gP; gM = gM.ToLower(); gP = qV.ri(gM); if (gP != null) return
gP; gN = gN.ToLower(); gP = qT.ri(gN); if (gP != null) return gP; gO = gO.ToLower(); gP = qU.ri(gO); if (gP != null) return gP; return null;
        }
        public bool
r0(string gQ, string gR, string gS)
        {
            uP gT; var gU = false; gT = qU.ri(gS.ToLower()); if (gT != null) { if (gT.rc) return true; gU = true; }
            gT = qT.ri(gR
.ToLower()); if (gT != null) { if (gT.rc) return true; gU = true; }
            gT = qV.ri(gQ.ToLower()); if (gT != null) { if (gT.rc) return true; gU = true; }
            return !(
qW || gU);
        }
        public uP r1(string gV, string gW, string gX)
        {
            uP gY = new uP(); gV = gV.ToLower(); uP gZ = q_(gV, gW.ToLower(), gX.ToLower()); if (gZ !=
null) { gY.r8 = gZ.r8; gY.r9 = gZ.r9; }
            gY.ra = gW; gY.rb = gX; qX.rg(gV, gY); return gY;
        }
        public uP r2(string g_, string h0, string h1)
        {
            uP h2 = qX.ri(
g_.ToLower()); if (h2 == null) h2 = r1(g_, h0, h1); return h2;
        }
        int r3 = 0; List<uP> r4; public List<uP> r5(string h3, bool h4, Func<uP, bool> h5 = null)
        {
            if (!h4) { r4 = new List<uP>(); r3 = 0; }
            for (; r3 < qX.rh(); r3++)
            {
                if (!qR.sv(5)) return null; uP h6 = qX.rj(r3); if (r0((h6.ra + ' ' + h6.rb).ToLower(),
h6.ra, h6.rb)) continue; if (h6.rb == h3 && (h5 == null || h5(h6))) r4.Add(h6);
            }
            return r4;
        }
        int r6 = 0; public bool r7(bool h7)
        {
            if (!h7) { r6 = 0; }
            for (;
r6 < MMItems.qH.Count; r6++)
            {
                if (!qR.sv(10)) return false; uN h8 = MMItems.qF[MMItems.qH[r6]]; if (!h8.qP) continue; var h9 = h8.qK + ' ' + h8.qL; if
      (r0(h9, h8.qK, h8.qL)) continue; uP ha = r2(h9, h8.qK, h8.qL); if (ha.r9 == -1) ha.r9 = h8.qM;
            }
            return true;
        }
    }
    public class uP
    {
        public int r8;
        public int r9; public string ra = ""; public string rb = ""; public bool rc; public double rd; public uP(bool hb = false, int hc = 1, int hd = -1)
        {
            r8 = hc; rc = hb; r9 = hd;
        }
    }
    public class uQ
    {
        Dictionary<string, uP> re = new Dictionary<string, uP>(); List<string> rf = new List<string>(); public
void rg(string he, uP hf)
        { if (!re.ContainsKey(he)) { rf.Add(he); re.Add(he, hf); } }
        public int rh() { return re.Count; }
        public uP ri(string
hg)
        { if (re.ContainsKey(hg)) return re[hg]; return null; }
        public uP rj(int hh) { return re[rf[hh]]; }
        public void rk()
        {
            rf.Clear(); re.Clear(
);
        }
        public void rl() { rf.Sort(); }
    }
    public class uR
    {
        uV rm; public MyDefinitionId rn = new MyDefinitionId(typeof(VRage.Game.
ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity"); public MyDefinitionId ro = new MyDefinitionId(typeof(VRage.
Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Oxygen"); public MyDefinitionId rp = new MyDefinitionId(typeof(VRage.
Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Hydrogen"); public uR(uV hi) { rm = hi; }
        int rq = 0; public bool rr(List<
IMyTerminalBlock> hj, ref double hk, ref double hl, ref double hm, ref double hn, ref double ho, ref double hp, bool hq)
        {
            if (!hq) rq = 0;
            MyResourceSinkComponent hr; MyResourceSourceComponent hs; for (; rq < hj.Count; rq++)
            {
                if (!rm.sv(8)) return false; if (hj[rq].Components.
TryGet<MyResourceSinkComponent>(out hr)) { hk += hr.CurrentInputByType(rn); hl += hr.MaxRequiredInputByType(rn); }
                if (hj[rq].Components.
TryGet<MyResourceSourceComponent>(out hs)) { hm += hs.CurrentOutputByType(rn); hn += hs.MaxOutputByType(rn); }
                ho += (hj[rq] as
IMyBatteryBlock).CurrentStoredPower; hp += (hj[rq] as IMyBatteryBlock).MaxStoredPower;
            }
            return true;
        }
        int rs = 0; public bool rt(List<
IMyTerminalBlock> ht, MyDefinitionId hu, ref double hv, ref double hw, ref double hx, ref double hy, bool hz)
        {
            if (!hz) rs = 0;
            MyResourceSinkComponent hA; MyResourceSourceComponent hB; for (; rs < ht.Count; rs++)
            {
                if (!rm.sv(6)) return false; if (ht[rs].Components.
TryGet<MyResourceSinkComponent>(out hA)) { hv += hA.CurrentInputByType(hu); hw += hA.MaxRequiredInputByType(hu); }
                if (ht[rs].Components.
TryGet<MyResourceSourceComponent>(out hB)) { hx += hB.CurrentOutputByType(hu); hy += hB.MaxOutputByType(hu); }
            }
            return true;
        }
        int ru = 0;
        public bool rv(List<IMyTerminalBlock> hC, string hD, ref double hE, ref double hF, bool hG)
        {
            hD = hD.ToLower(); if (!hG) { ru = 0; hF = 0; hE = 0; }
            MyResourceSinkComponent hH; for (; ru < hC.Count; ru++)
            {
                if (!rm.sv(30)) return false; IMyGasTank hI = hC[ru] as IMyGasTank; if (hI == null)
                    continue; double hJ = 0; if (hI.Components.TryGet<MyResourceSinkComponent>(out hH))
                {
                    ListReader<MyDefinitionId> hK = hH.AcceptedResources;
                    int hL = 0; for (; hL < hK.Count; hL++) { if (hK[hL].SubtypeId.ToString().ToLower() == hD) { hJ = 100; hF += hJ; hE += hJ * hI.FilledRatio; break; } }
                }
            }
            return
true;
        }
    }
    public class uS
    {
        uX rw = null; public uT rx = new uT(); public uW ry = null; public IMyTextPanel rz = null; public int rA = 0; public
string rB = ""; public string rC = ""; public bool rD = true; public uS(uX hM, string hN) { rw = hM; rC = hN; }
        public bool rE()
        {
            return ry.sG.Count >
ry.sC || ry.sD != 0;
        }
        public void rF(float hO) { for (int hP = 0; hP < rx.rV(); hP++) rx.rX(hP).SetValueFloat("FontSize", hO); }
        public void rG()
        {
            rx
.rZ(); rz = rx.rX(0); int hQ = rz.CustomName.IndexOf("!MARGIN:"); if (hQ < 0 || hQ + 8 >= rz.CustomName.Length) { rA = 1; rB = " "; }
            else
            {
                var hR = rz.
CustomName.Substring(hQ + 8); int hS = hR.IndexOf(" "); if (hS >= 0) hR = hR.Substring(0, hS); if (!int.TryParse(hR, out rA)) rA = 1; rB = new String(
' ', rA);
            }
            if (rz.CustomName.Contains("!NOSCROLL")) rD = false; else rD = true;
        }
        public bool rH()
        {
            return (rz.BlockDefinition.SubtypeId.
Contains("Wide") || rz.DefinitionDisplayNameText == "Computer Monitor");
        }
        float rI = 1.0f; bool rJ = false; public float rK()
        {
            if (rJ) return rI
; rJ = true; rI = (rH() ? 2.0f : 1.0f); return rI;
        }
        float rL = 1.0f; bool rM = false; public float rN()
        {
            if (rM) return rL; rM = true; if (rz.
BlockDefinition.SubtypeId.Contains("Corner_LCD_Flat")) rL = 0.1765f;
            else if (rz.BlockDefinition.SubtypeId.Contains("Corner_LCD")) rL =
0.15f; if (rz.BlockDefinition.SubtypeId.Contains("Small")) rL *= 1.8f; return rL;
        }
        public void rO()
        {
            if (ry == null || rz == null) return; float hT
= rz.FontSize; var hU = rz.Font; for (int hV = 0; hV < rx.rV(); hV++)
            {
                IMyTextPanel hW = rx.rX(hV); if (hV > 0) { hW.FontSize = hT; hW.Font = hU; }
                hW.
WritePublicText(ry.sT(hV)); if (rw.t5) hW.ShowTextureOnScreen(); hW.ShowPublicTextOnScreen();
            }
        }
    }
    public class uT
    {
        Dictionary<string,
IMyTextPanel> rP = new Dictionary<string, IMyTextPanel>(); Dictionary<IMyTextPanel, string> rQ = new Dictionary<IMyTextPanel, string>(); List
<string> rR = new List<string>(); public void rS(string hX, IMyTextPanel hY)
        {
            if (!rR.Contains(hX))
            {
                rR.Add(hX); rP.Add(hX, hY); rQ.Add(hY, hX
);
            }
        }
        public void rT(string hZ) { if (rR.Contains(hZ)) { rR.Remove(hZ); rQ.Remove(rP[hZ]); rP.Remove(hZ); } }
        public void rU(IMyTextPanel h_)
        {
            if (rQ.ContainsKey(h_)) { rR.Remove(rQ[h_]); rP.Remove(rQ[h_]); rQ.Remove(h_); }
        }
        public int rV() { return rP.Count; }
        public IMyTextPanel rW
(string i0)
        { if (rR.Contains(i0)) return rP[i0]; return null; }
        public IMyTextPanel rX(int i1) { return rP[rR[i1]]; }
        public void rY()
        {
            rR.
Clear(); rP.Clear(); rQ.Clear();
        }
        public void rZ() { rR.Sort(); }
    }
    public class uU
    {
        public string r_ = "MMTask"; public double s0 = 0; public
double s1 = 0; public double s2 = 0; public double s3 = -1; public bool s4 = false; public bool s5 = false; double s6 = 0; protected uV s7; public
void s8(uV i2)
        { s7 = i2; }
        protected bool s9(double i3) { s6 = Math.Max(i3, 0.0001); return true; }
        public bool sa()
        {
            if (s1 > 0)
            {
                s2 = s7.sf - s1; s7.sn
((s5 ? "Running" : "Resuming") + " task: " + r_); s5 = Run(!s5);
            }
            else
            {
                s2 = 0; s7.sn("Init task: " + r_); Init(); s7.sn("Running.."); s5 = Run(false); if
(!s5) s1 = 0.001;
            }
            if (s5) { s1 = s7.sf; if ((s3 >= 0 || s6 > 0) && s4) s7.sl(this, (s6 > 0 ? s6 : s3)); else { s4 = false; s1 = 0; } } else { if (s4) s7.sl(this, 0, true); }
            s7.sn("Task " + (s5 ? "" : "NOT ") + "finished. " + (s4 ? (s6 > 0 ? "Postponed by " + s6.ToString("F1") + "s" : "Scheduled after " + s3.ToString("F1") + "s"
            ) : "Stopped.")); s6 = 0; return s5;
        }
        public void sb() { s7.sm(this); End(); s4 = false; s5 = false; s1 = 0; }
        public virtual void Init() { }
        public
virtual bool Run(bool i4)
        { return true; }
        public virtual void End() { }
    }
    public class uV
    {
        public double sf { get { return sh; } }
        int sg = 1000;
        double sh = 0; List<uU> si = new List<uU>(100); public MyGridProgram sj; int sk = 0; public uV(MyGridProgram i5, int i6 = 1) { sj = i5; sk = i6; }
        public
void sl(uU i7, double i8, bool i9 = false)
        {
            sn("Scheduling task: " + i7.r_ + " after " + i8.ToString("F2")); i7.s4 = true; i7.s8(this); if (i9)
            {
                i7.
s0 = sf; si.Insert(0, i7); return;
            }
            if (i8 <= 0) i8 = 0.001; i7.s0 = sf + i8; for (int ia = 0; ia < si.Count; ia++)
            {
                if (si[ia].s0 > i7.s0)
                {
                    si.Insert(ia, i7);
                    return;
                }
                if (i7.s0 - si[ia].s0 < 0.05) i7.s0 = si[ia].s0 + 0.05;
            }
            si.Add(i7);
        }
        public void sm(uU ib)
        {
            if (si.Contains(ib))
            {
                si.Remove(ib); ib.s4 =
false;
            }
        }
        public void sn(string ic, int id = 1) { if (sk == id) sj.Echo(ic); }
        double so = 0; public void sp()
        {
            so += sj.Runtime.TimeSinceLastRun.
TotalSeconds;
        }
        public void sq()
        {
            double ie = sj.Runtime.TimeSinceLastRun.TotalSeconds + so; so = 0; sh += ie; sn("Total time: " + sh.ToString(
"F1") + " Time Step: " + ie.ToString("F2")); sg = (int)Math.Min((ie * 60) * 1000, 20000 - 1000); sn("Total tasks: " + si.Count + " InstrPerRun: " + sg)
       ; while (si.Count >= 1)
            {
                uU ig = si[0]; if (sg - sj.Runtime.CurrentInstructionCount <= 0) break; if (ig.s0 > sh)
                {
                    int ih = (int)(60 * (ig.s0 - sh)); if (ih >=
100) { sj.Runtime.UpdateFrequency = UpdateFrequency.Update100; }
                    else
                    {
                        if (ih >= 10) sj.Runtime.UpdateFrequency = UpdateFrequency.Update10;
                        else
                            sj.Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    }
                    break;
                }
                si.Remove(ig); if (!ig.sa()) break; sn("Done. NextExecTime: " + ig.s0.
ToString("F1")); sn("Remaining Instr: " + su().ToString());
            }
        }
        int sr = 0; StringBuilder ss = new StringBuilder(); public void st()
        {
            double ii
= sj.Runtime.LastRunTimeMs * 1000; if (sr == 5000)
            {
                IMyTextPanel ij = sj.GridTerminalSystem.GetBlockWithName("AUTOLCD Profiler") as
IMyTextPanel; if (ij == null) return; ij.WritePublicText(ss.ToString()); sr++; return;
            }
            ss.Append(sr).Append(";").AppendLine(ii.ToString(
"F2")); sr++;
        }
        public int su() { return (20000 - sj.Runtime.CurrentInstructionCount); }
        public bool sv(int ik)
        {
            return ((20000 - sj.Runtime.
CurrentInstructionCount) >= ik);
        }
        public void sw() { sn("Remaining Instr: " + su().ToString()); }
    }
    public class uW
    {
        uX sx = null; public float
sy = 1.0f; public string sz = "Debug"; public float sA = 1.0f; public float sB = 1.0f; public int sC = 17; public int sD = 0; int sE = 1; int sF = 1;
        public List<string> sG = new List<string>(); public int sH = 0; public float sI = 0; public uW(uX il, float im = 1.0f)
        {
            sx = il; sJ(im); sG.Add("");
        }
        public void sJ(float io) { sy = io; }
        public void sK(int ip) { sF = ip; }
        public void sL() { sC = (int)Math.Floor(uX.sZ * sB * sF / uX.t0); }
        public void
sM(string iq)
        { sG[sH] += iq; }
        public void sN(List<string> ir)
        {
            if (sG[sH] == "") sG.RemoveAt(sH); else sH++; sG.AddList(ir); sH += ir.Count; sG.
Add(""); sI = 0;
        }
        public List<string> sO() { if (sG[sH] == "") return sG.GetRange(0, sH); else return sG; }
        public void sP(string it, string iu = ""
)
        { string[] iv = it.Split('\n'); for (int iw = 0; iw < iv.Length; iw++) sQ(iu + iv[iw]); }
        public void sQ(string ix)
        {
            sG[sH] += ix; sG.Add(""); sH++; sI =
0;
        }
        public void sR() { sG.Clear(); sG.Add(""); sI = 0; sH = 0; }
        public string sS() { return String.Join("\n", sG); }
        public string sT(int iy = 0)
        {
            if
(sG.Count <= iy * sC / sF) return ""; if (sG.Count <= sC / sF) { sD = 0; sE = 1; return sS(); }
            int iz = sD + iy * (sC / sF); if (iz > sG.Count) iz = sG.Count; List<
string> iA = sG.GetRange(iz, Math.Min(sG.Count - iz, sC / sF)); return String.Join("\n", iA);
        }
        public bool sU(int iB = -1)
        {
            if (iB <= 0) iB = sx.t2; if (
sD - iB <= 0) { sD = 0; return true; }
            sD -= iB; return false;
        }
        public bool sV(int iC = -1)
        {
            if (iC <= 0) iC = sx.t2; int iD = sG.Count - 1; if (sD + iC + sC >= iD)
            {
                sD
= Math.Max(iD - sC, 0); return true;
            }
            sD += iC; return false;
        }
        public int sW = 0; public void sX()
        {
            if (sW > 0) { sW--; return; }
            if (sG.Count - 1 <= sC)
            {
                sD =
0; sE = 1; return;
            }
            if (sE > 0) { if (sV()) { sE = -1; sW = 2; } } else { if (sU()) { sE = 1; sW = 2; } }
        }
    }
    public class uX
    {
        public const float sZ = 512 / 0.7783784f;
        public const float s_ = 512 / 0.7783784f; public const float t0 = 37; public string t1 = "T:[LCD]"; public int t2 = 1; public bool t3 = true;
        public List<string> t4 = null; public bool t5 = true; public int t6 = 0; public float t7 = 1.0f; public float t8 = 1.0f; public float t9
        {
            get
            {
                return s_ * tA.sA;
            }
        }
        public float ta { get { return (float)t9 - 2 * ti[tB] * tl; } }
        string tb; string tc; float td = -1; Dictionary<string, float> te = new
Dictionary<string, float>(2); Dictionary<string, float> tf = new Dictionary<string, float>(2); Dictionary<string, float> tg = new Dictionary<
string, float>(2); public float th { get { return tg[tB]; } }
        Dictionary<string, float> ti = new Dictionary<string, float>(2); Dictionary<string,
float> tj = new Dictionary<string, float>(2); Dictionary<string, float> tk = new Dictionary<string, float>(2); int tl = 0; string tm = "";
        Dictionary<string, char> tn = new Dictionary<string, char>(2); Dictionary<string, char> to = new Dictionary<string, char>(2); Dictionary<
                    string, char> tp = new Dictionary<string, char>(2); Dictionary<string, char> tq = new Dictionary<string, char>(2); uV tr; public MyGridProgram
                                 ts; public uR tt; public uL tu; public uJ tv; public uM MMItems; public TranslationTable tw; public IMyGridTerminalSystem tx
        {
            get
            {
                return
ts.GridTerminalSystem;
            }
        }
        public IMyProgrammableBlock ty { get { return ts.Me; } }
        public Action<string> tz { get { return ts.Echo; } }
        public uX(
MyGridProgram iE, int iF, uV iG)
        {
            tr = iG; t6 = iF; ts = iE; tw = new TranslationTable(); tt = new uR(iG); tu = new uL(iG, this); tu.qv(); tv = new uJ(tr);
            tr.sl(tv, 0);
        }
        uW tA = null; public string tB { get { return tA.sz; } }
        public bool tC { get { return !(tA.sH > 0 && tA.sG[0] != ""); } }
        public uW tD(uW iH
, uS iI)
        {
            iI.rG(); IMyTextPanel iJ = iI.rz; if (iH == null) iH = new uW(this, iJ.FontSize); else iH.sJ(iJ.FontSize); iH.sz = iI.rz.Font; if (!ti.
                    ContainsKey(iH.sz)) iH.sz = tb; iH.sK(iI.rx.rV()); iH.sA = iI.rK() * t7 / iH.sy; iH.sB = iI.rN() * t8 / iH.sy; iH.sL(); tm = iI.rB; tl = iI.rA; tA = iH; return
                                                 iH;
        }
        public void tE(uS iK) { tA = iK.ry; }
        public void tF(string iL) { if (tA.sI <= 0) iL = tm + iL; tA.sQ(iL); }
        public void tG(string iM)
        {
            tA.sP(iM,
tm);
        }
        public void tH(List<string> iN) { tA.sN(iN); }
        public void Add(string iO) { if (tA.sI <= 0) iO = tm + iO; tA.sM(iO); tA.sI += t_(iO, tA.sz); }
        public void tI(string iP, float iQ = 1.0f, float iR = 0f) { tJ(iP, iQ, iR); tF(""); }
        public void tJ(string iS, float iT = 1.0f, float iU = 0f)
        {
            float
iV = t_(iS, tA.sz); float iW = iT * s_ * tA.sA - tA.sI - iU; if (tl > 0) iW -= ti[tA.sz] * tl; if (iW < iV) { tA.sM(iS); tA.sI += iV; return; }
            iW -= iV; int iX = (int)
Math.Floor(iW / ti[tA.sz]); float iY = iX * ti[tA.sz]; tA.sM(new String(' ', iX) + iS); tA.sI += iY + iV;
        }
        public void tK(string iZ)
        {
            tL(iZ); tF("");
        }
        public void tL(string i_)
        {
            float j0 = t_(i_, tA.sz); float j1 = s_ / 2 * tA.sA - tA.sI; if (j1 < j0 / 2) { tA.sM(i_); tA.sI += j0; return; }
            j1 -= j0 / 2; int j2
= (int)Math.Round(j1 / ti[tA.sz], MidpointRounding.AwayFromZero); float j3 = j2 * ti[tA.sz]; tA.sM(new String(' ', j2) + i_); tA.sI += j3 + j0;
        }
        public void tM(double j4, float j5 = 1.0f, float j6 = 0f)
        {
            if (tl > 0) j6 += tl * ti[tA.sz] * ((tA.sI <= 0) ? 2 : 1); float j7 = s_ * j5 * tA.sA - tA.sI - j6; if (
Double.IsNaN(j4)) j4 = 0; int j8 = (int)(j7 / tj[tA.sz]) - 2; if (j8 <= 0) j8 = 2; int j9 = Math.Min((int)(j4 * j8) / 100, j8); if (j9 < 0) j9 = 0; tA.sQ((tA.sI <= 0
                     ? tm : "") + tn[tA.sz] + new String(tq[tA.sz], j9) + new String(tp[tA.sz], j8 - j9) + to[tA.sz]);
        }
        public void tN(double ja, float jb = 1.0f, float jc
= 0f)
        {
            if (tl > 0) jc += tl * ti[tA.sz] * ((tA.sI <= 0) ? 2 : 1); float jd = s_ * jb * tA.sA - tA.sI - jc; if (Double.IsNaN(ja)) ja = 0; int je = (int)(jd / tj[tA.sz]) - 2
                                        ; if (je <= 0) je = 2; int jf = Math.Min((int)(ja * je) / 100, je); if (jf < 0) jf = 0; tA.sM((tA.sI <= 0 ? tm : "") + tn[tA.sz] + new String(tq[tA.sz], jf) + new
                                                               String(tp[tA.sz], je - jf) + to[tA.sz]); tA.sI += (tA.sI <= 0 ? tl * ti[tA.sz] : 0) + tj[tA.sz] * je + 2 * tk[tA.sz];
        }
        public void tO() { tA.sR(); }
        public
void tP(uS jg)
        { jg.rO(); if (jg.rD) tA.sX(); }
        public void tQ(string jh, string ji)
        {
            IMyTextPanel jj = ts.GridTerminalSystem.
GetBlockWithName(jh) as IMyTextPanel; if (jj == null) return; jj.WritePublicText(ji + "\n", true);
        }
        public string tR(IMyInventoryItem jk)
        {
            var
jl = jk.Content.TypeId.ToString(); jl = jl.Substring(jl.LastIndexOf('_') + 1); return jk.Content.SubtypeId + " " + jl;
        }
        public void tS(string
jm, out string jn, out string jo)
        {
            int jp = jm.LastIndexOf(' '); if (jp >= 0) { jn = jm.Substring(0, jp); jo = jm.Substring(jp + 1); return; }
            jn = jm; jo =
"";
        }
        public string tT(string jq) { string jr, js; tS(jq, out jr, out js); return tT(jr, js); }
        public string tT(string jt, string ju)
        {
            uN jv =
MMItems.qJ(jt, ju); if (jv != null) { if (jv.qN != "") return jv.qN; return jv.qK; }
            return System.Text.RegularExpressions.Regex.Replace(jt,
"([a-z])([A-Z])", "$1 $2");
        }
        public void tU(ref string jw, ref string jx)
        {
            var jy = jw.ToLower(); uN jz; if (MMItems.qI.TryGetValue(jy, out
jz)) { jw = jz.qK; jx = jz.qL; return; }
            jz = MMItems.qJ(jw, jx); if (jz != null) { jw = jz.qK; if (jx == "Ore" || jx == "Ingot") return; jx = jz.qL; }
        }
        public
string tV(double jA, bool jB = true, char jC = ' ')
        {
            if (!jB) return jA.ToString("#,###,###,###,###,###,###,###,###,###"); var jD =
" kMGTPEZY"; double jE = jA; int jF = jD.IndexOf(jC); int jG = (jF < 0 ? 0 : jF); while (jE >= 1000 && jG + 1 < jD.Length) { jE /= 1000; jG++; }
            var jH = Math.Round
(jE, 1, MidpointRounding.AwayFromZero).ToString(); if (jG > 0) jH += " " + jD[jG]; return jH;
        }
        public string tW(double jI, bool jJ = true, char jK =
' ')
        {
            if (!jJ) return jI.ToString("#,###,###,###,###,###,###,###,###,###"); var jL = " ktkMGTPEZY"; double jM = jI; int jN = jL.IndexOf(jK);
            int jO = (jN < 0 ? 0 : jN); while (jM >= 1000 && jO + 1 < jL.Length) { jM /= 1000; jO++; }
            var jP = Math.Round(jM, 1, MidpointRounding.AwayFromZero).ToString()
; if (jO == 1) jP += " kg"; else if (jO == 2) jP += " t"; else if (jO > 2) jP += " " + jL[jO] + "t"; return jP;
        }
        public string tX(double jQ)
        {
            return (Math.
Floor(jQ * 10) / 10).ToString("F1");
        }
        Dictionary<char, float> tY = new Dictionary<char, float>(); void AddCharsSize(string jR, float jS)
        {
            jS += 1
; for (int jT = 0; jT < jR.Length; jT++) { if (jS > te[tb]) te[tb] = jS; tY.Add(jR[jT], jS); }
        }
        public float tZ(char jU, string jV)
        {
            float jW; if (jV == tc
|| !tY.TryGetValue(jU, out jW)) return te[jV]; return jW;
        }
        public float t_(string jX, string jY)
        {
            if (jY == tc) return jX.Length * te[jY]; float
jZ = 0; for (int j_ = 0; j_ < jX.Length; j_++) jZ += tZ(jX[j_], jY); return jZ;
        }
        public string u0(string k0, float k1)
        {
            if (k1 / te[tA.sz] >= k0.Length)
                return k0; float k2 = t_(k0, tA.sz); if (k2 <= k1) return k0; float k3 = k2 / k0.Length; k1 -= tf[tA.sz]; int k4 = (int)Math.Max(k1 / k3, 1); if (k4 < k0.
                                        Length / 2) { k0 = k0.Remove(k4); k2 = t_(k0, tA.sz); }
            else { k4 = k0.Length; } while (k2 > k1 && k4 > 1) { k4--; k2 -= tZ(k0[k4], tA.sz); }
            if (k0.Length > k4) k0 = k0
.Remove(k4); return k0 + "..";
        }
        void SetupClassicFont(string k5)
        {
            tb = k5; tn[tb] = MMStyle.BAR_START; to[tb] = MMStyle.BAR_END; tp[tb] = MMStyle.
BAR_EMPTY; tq[tb] = MMStyle.BAR_FILL; te[tb] = 0f;
        }
        void SetupMonospaceFont(string k6, float k7)
        {
            tc = k6; td = k7; te[tc] = td + 1; tf[tc] = 2 * (td + 1);
            tn[tc] = MMStyle.BAR_MONO_START; to[tc] = MMStyle.BAR_MONO_END; tp[tc] = MMStyle.BAR_MONO_EMPTY; tq[tc] = MMStyle.BAR_MONO_FILL; ti[tc] = tZ(' '
                        , tc); tj[tc] = tZ(tp[tc], tc); tk[tc] = tZ(tn[tc], tc); tg[tc] = t_(" 100.0%", tc);
        }
        public void u1()
        {
            if (tY.Count > 0) return;


            // Monospace font name, width of single character
            // Change this if you want to use different (modded) monospace font
            SetupMonospaceFont("Monospace", 24f);

            // Classic/Debug font name (uses widths of characters below)
            // Change this if you want to use different font name (non-monospace)
            SetupClassicFont("Debug");
            // Font characters width (font "aw" values here)
            AddCharsSize("3FKTabdeghknopqsuy£µÝàáâãäåèéêëðñòóôõöøùúûüýþÿāăąďđēĕėęěĝğġģĥħĶķńņňŉōŏőśŝşšŢŤŦũūŭůűųŶŷŸșȚЎЗКЛбдекруцяёђћўџ", 17f);
            AddCharsSize("ABDNOQRSÀÁÂÃÄÅÐÑÒÓÔÕÖØĂĄĎĐŃŅŇŌŎŐŔŖŘŚŜŞŠȘЅЊЖф□", 21f);
            AddCharsSize("#0245689CXZ¤¥ÇßĆĈĊČŹŻŽƒЁЌАБВДИЙПРСТУХЬ€", 19f);
            AddCharsSize("￥$&GHPUVY§ÙÚÛÜÞĀĜĞĠĢĤĦŨŪŬŮŰŲОФЦЪЯжы†‡", 20f);
            AddCharsSize("！ !I`ijl ¡¨¯´¸ÌÍÎÏìíîïĨĩĪīĮįİıĵĺļľłˆˇ˘˙˚˛˜˝ІЇії‹›∙", 8f);
            AddCharsSize("？7?Jcz¢¿çćĉċčĴźżžЃЈЧавийнопсъьѓѕќ", 16f);
            AddCharsSize("（）：《》，。、；【】(),.1:;[]ft{}·ţťŧț", 9f);
            AddCharsSize("+<=>E^~¬±¶ÈÉÊË×÷ĒĔĖĘĚЄЏЕНЭ−", 18f);
            AddCharsSize("L_vx«»ĹĻĽĿŁГгзлхчҐ–•", 15f);
            AddCharsSize("\"-rª­ºŀŕŗř", 10f);
            AddCharsSize("WÆŒŴ—…‰", 31f);
            AddCharsSize("'|¦ˉ‘’‚", 6f);
            AddCharsSize("@©®мшњ", 25f);
            AddCharsSize("mw¼ŵЮщ", 27f);
            AddCharsSize("/ĳтэє", 14f);
            AddCharsSize("\\°“”„", 12f);
            AddCharsSize("*²³¹", 11f);
            AddCharsSize("¾æœЉ", 28f);
            AddCharsSize("%ĲЫ", 24f);
            AddCharsSize("MМШ", 26f);
            AddCharsSize("½Щ", 29f);
            AddCharsSize("ю", 23f);
            AddCharsSize("ј", 7f);
            AddCharsSize("љ", 22f);
            AddCharsSize("ґ", 13f);
            AddCharsSize("™", 30f);
            // End of font characters width
            ti[tb] = tZ(' ', tb); tj[tb] = tZ(tp[tb], tb); tk[tb] = tZ(tn[tb], tb); tg[tb] = t_(" 100.0%", tb); tf[tb] = tZ('.', tb
                           ) * 2;
        }
    }

    public class TranslationTable
    {
        public string T(string msgid) { return TT[msgid]; }

        readonly Dictionary<string, string> TT = new Dictionary<string, string>
{
// TRANSLATION STRINGS
// msg id, text
{ "AC1", "Acceleration:" },
// amount
{ "A1", "EMPTY" },
{ "ALT1", "Altitude:"},
{ "ALT2", "Ground:"},
{ "B1", "Booting up..." },
{ "C1", "count:" },
{ "C2", "Cargo Used:" },
{ "C3", "Invalid countdown format, use:" },
{ "C4", "EXPIRED" },
{ "C5", "days" },
// customdata
{ "CD1", "Block not found: " }, // NEW
{ "CD2", "Missing block name" }, // NEW
{ "D1", "You need to enter name." },
{ "D2", "No blocks found." },
{ "D3", "No damaged blocks found." },
{ "DTU", "Invalid GPS format" },
{ "GA", "Artif."}, // (not more than 5 characters)
{ "GN", "Natur."}, // (not more than 5 characters)
{ "GT", "Total"}, // (not more than 5 characters)
{ "G1", "Total Gravity:"},
{ "G2", "Natur. Gravity:"},
{ "G3", "Artif. Gravity:"},
{ "H1", "Write commands to Custom Data of this panel." },
// inventory
{ "I1", "ore" },
{ "I2", "summary" },
{ "I3", "Ores" },
{ "I4", "Ingots" },
{ "I5", "Components" },
{ "I6", "Gas" },
{ "I7", "Ammo" },
{ "I8", "Tools" },
{ "M1", "Cargo Mass:" },
// oxygen
{ "O1", "Leaking" },
{ "O2", "Oxygen Farms" },
{ "O3", "No oxygen blocks found." },
{ "O4", "Oxygen Tanks" },
// position
{ "P1", "Block not found" },
{ "P2", "Location" },
// power
{ "P3", "Stored" },
{ "P4", "Output" },
{ "P5", "Input" },
{ "P6", "No power source found!" },
{ "P7", "Batteries" },
{ "P8", "Total Output" },
{ "P9", "Reactors" },
{ "P10", "Solars" },
{ "P11", "Power" },
{ "PT1", "Power Time:" },
{ "PT2", "Charge Time:" },
{ "PU1", "Power Used:" },
{ "S1", "Speed:" },
{ "SM1", "Ship Mass:" },
{ "SM2", "Ship Base Mass:" },
{ "SD", "Stop Distance:" },
{ "ST", "Stop Time:" },
// text
{ "T1", "Source LCD not found: " },
{ "T2", "Missing source LCD name" },
{ "T3", "LCD Private Text is empty" },
// tanks
{ "T4", "Missing tank type. eg: 'Tanks * Hydrogen'" },
{ "T5", "No {0} tanks found." }, // {0} is tank type
{ "T6", "Tanks" },
{ "UC", "Unknown command" },
// occupied & dampeners
{ "SC1", "Cannot find control block." },
{ "SCD", "Dampeners: " },
{ "SCO", "Occupied: " },
// working
{ "W1", "OFF" },
{ "W2", "WORK" },
{ "W3", "IDLE" },
{ "W4", "LEAK" },
{ "W5", "OPEN" },
{ "W6", "CLOSED" },
{ "W7", "LOCK" },
{ "W8", "UNLOCK" },
{ "W9", "ON" },
{ "W10", "READY" }
};
    }
    // КОНЕЦ СКРИПТА
}