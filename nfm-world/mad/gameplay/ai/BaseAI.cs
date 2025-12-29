using FixedMathSharp.Utility;
using Maxine.Extensions;
using NFMWorld.Util;
using SoftFloat;

namespace NFMWorld.Mad.ai;

/// <summary>
/// Base AI class for gamemode-specific AI implementations.
/// </summary>
public abstract class BaseAi
{
    public abstract void RunAi(InGameCar car, int currentCarIndex);
}

/// <summary>
/// Re-Volt inspired AI implementation for racing vehicles.
/// Handles AI decision making, path finding, and control inputs based on difficulty and race conditions.
/// </summary>
public class ReLitAi(BaseGamemode gamemode, BaseRacePhase racePhase) : BaseAi
{
    /// <summary>
    /// Pythagorean distance squared calculation (integer version).
    /// Used for fast distance comparisons without square root.
    /// </summary>
    private static int pyo(int x1, int x2, int z1, int z2) {
        return (((x1 - x2) * (x1 - x2)) + ((z1 - z2) * (z1 - z2)));
    }
    
    /// <summary>
    /// Pythagorean distance squared calculation (fixed-point version).
    /// Used for fast distance comparisons without square root.
    /// </summary>
    private static fix64 pyo(fix64 x1, fix64 x2, fix64 z1, fix64 z2) {
        return (((x1 - x2) * (x1 - x2)) + ((z1 - z2) * (z1 - z2)));
    }
    
    /// <summary>
    /// Bit flags representing different race stages/tracks.
    /// Each stage may require different AI behavior.
    /// </summary>
    public enum StageFlags : uint
    
    {
        Introductory = 1 << 0,
        DreamIsReal = 1 << 1,
        WrongSideOfTheLaw = 1 << 2,
        CounterRetaliation = 1 << 3,
        NeverGoingBack = 1 << 4,
        BlueDragonInTheSky = 1 << 5,
        HeIsComingForYouNext = 1 << 6,
        FlightOfTheBarkingLunatics = 1 << 7,
        TheKingIsCalmWhenThreatened = 1 << 8,
        SkylineRail = 1 << 9,
        RollingDark = 1 << 10,
        ItsATrap = 1 << 11,
        JokersPlanet = 1 << 12,
        BigJump = 1 << 13,
        RadicalAndTheMonster = 1 << 14,
        LostIn4DAgain = 1 << 15,
        MadParty = 1 << 16,
    }

    /// <summary>
    /// Bit flags representing different vehicle types.
    /// Each car may have unique AI handling characteristics.
    /// </summary>
    public enum CarFlags : uint
    {
        TShark,
        Formula7,
        WowCaninaro,
        LaVitaCrab,
        Nimi,
        MaxRevenge,
        LeadOxide,
        Tragdor,
        KoolKat,
        IceCream,
        DrifterX,
        ShadowRider,
        SwordOfJustice,
        HighRider,
        ElKing,
        MightyEight,
        Masheen,
        RadicalOne,
        DrMonstaa
    }
    
    // ===== AI Configuration Parameters =====
    private int difficulty; // 0-7, easy to hard - controls AI accuracy and aggression
    private int rubberband; // 0-7, high to low - controls catch-up mechanics
    private bool booststunting; // true to greatly encourage stunting behavior
    
    // ===== Internal AI State Variables =====
    private int stcnt, statusque, acuracy, clrnce; // State counters and accuracy tracking
    private Nibble<uint> stageFlags => (uint)theStageFlags; // Current stage characteristics
    private Nibble<uint> carFlags => (uint)theCarFlags; // Current car characteristics
    public StageFlags theStageFlags;
    public CarFlags theCarFlags;
    
    // ===== Timing and Control Variables =====
    private int upwait; // Wait counter for forward acceleration
    private fix64 skiplev; // Level/threshold for skipping actions
    private int acr; // Accuracy counter
    private int rampp; // Ramp counter
    private int cntrn; // Center turn counter
    private bool agressed; // Whether AI is in aggressive mode
    private int turntyp; // Type of turn being executed
    private int attack; // Attack mode counter
    private fix64 saftey; // Safety margin for navigation
    private fix64 mustland; // Flag indicating must land soon
    private int stuntf; // Stunt flag/counter
    private int avoidnlev; // Avoidance level
    private fix64 trickprf; // Trick preference value
    private bool usebounce; // Whether to use bounce mechanics
    private bool perfection; // Perfect driving mode flag
    private bool afta; // After-action flag
    private bool bulistc; // Ballistic mode flag
    private fix64 aim; // Aiming value for targeting
    private bool exitattack; // Flag to exit attack mode
    private int trfix; // Trick fix counter
    private int oupnt; // Out point counter
    private int trickfase; // Trick phase/state
    private int upcnt; // Up counter
    private int revstart; // Reverse start counter
    private int _tick = 0; // Main tick counter for AI updates
    private int actwait; // Action wait counter
    private int flycnt; // Flying/airborne counter
    private bool forget; // Flag to forget current objective
    private int focus; // Focus/attention value
    private bool nofocus; // No focus flag
    private int wall = -1; // Wall detection index
    private bool gowait; // Go/wait flag
    private int turncnt; // Turn counter
    private int randtcnt; // Random turn counter
    private fix64 pan; // Pan/steering adjustment value
    private int wtx; // Wait X coordinate
    private int wtz; // Wait Z coordinate
    private int runbul; // Run bullet/projectile counter
    private int frx; // From X coordinate
    private int frz; // From Z coordinate
    private int frad; // From radius
    private int hold; // Hold counter
    private bool lastl; // Last left flag
    private int lwall = -1; // Left wall detection index
    private bool wlastl; // Wall last left flag
    private fix64 oxy; // Old X/Y coordinate
    private fix64 ozy; // Old Z/Y coordinate
    private int uddirect; // Up/down direction
    private int lrdirect; // Left/right direction
    private bool udswt; // Up/down switch flag
    private bool lrswt; // Left/right switch flag
    private int udstart; // Up/down start value
    private int lrstart; // Left/right start value
    private int apunch; // Attack punch counter
    private bool onceu; // Once up flag
    private bool onced; // Once down flag
    private bool oncel; // Once left flag
    private bool oncer; // Once right flag
    private bool lrcomp; // Left/right complete flag
    private bool udcomp; // Up/down complete flag
    private bool udbare; // Up/down bare flag
    private bool lrbare; // Left/right bare flag
    private int swat; // Swat/attack counter

    /// <summary>
    /// Main AI update function. Called every frame to compute control inputs for the AI vehicle.
    /// </summary>
    /// <param name="car">The AI-controlled car</param>
    /// <param name="u">Control output structure</param>
    /// <param name="position">Current race position</param>
    /// <param name="currentCarIndex">Index of the current car</param>
    public override void RunAi(InGameCar car, int currentCarIndex)
    {
        // Get current race state information
        var u = car.Control;
        var position = car.placement;
        var currentStage = racePhase.CurrentStage;
        var pcleared = car.currentCheckpointNode; // Previous cleared checkpoint
        var clear = car.totalCheckpoint; // Total checkpoints cleared

        // AI updates every other frame for performance
        bool newTick = false;
        if (++_tick == 2)
        {
            newTick = true;
            _tick = 0;
        }

        // Initialize random number generator with deterministic seed based on car position
        var conto = new ContO(car.CarRef);
        DeterministicRandom random = new((ulong)(conto.X.Value.m_rawValue ^ conto.Y.Value.m_rawValue ^ conto.Z.Value.m_rawValue));
        
        int missedcp = 0; // todo: calculate in racePhase

        // Get car state and stats
        var mad = car.Mad;
        var power = mad.Power;
        var hitmag = mad.Hitmag;
        var maxmag = mad.Stat.Maxmag;
        var point = mad.Point;
        
        // Initialize all control inputs to false
        u.Left = false;
        u.Right = false;
        u.Up = false;
        u.Down = false;
        u.Handb = false;
        
        // Only process AI if car is not wasted/destroyed
        if (!car.Wasted) {
            // Only update AI logic when car is touching the ground
            if (mad.Mtouch) {
                // Update AI parameters periodically
                if (stcnt > statusque) {
                    // Calculate accuracy based on difficulty and position
                    var tstage = difficulty;
                    acuracy = ((7 - position) * (6 - (tstage * 2)));
                    if (acuracy < 0) {
                        acuracy = 0;
                    }
                    
                    // Set clearance value based on stage characteristics
                    clrnce = 5; // Default clearance
                    if (stageFlags[6] || stageFlags[11]) {
                        clrnce = 2; // Tighter clearance for certain stages
                    }
                    if (stageFlags[12]) {
                        if (pcleared == 27 || pcleared == 17) {
                            clrnce = 3;
                        }
                    }
                    if (stageFlags[16]) {
                        clrnce = 3;
                    }
                    
                    // Calculate rubberband multiplier based on stage
                    fix64 mw = 0;
                    if (stageFlags[1]) {
                        mw = 2;
                    }
                    if (stageFlags[2]) {
                        mw = (fix64)1.5f;
                    }
                    if ((stageFlags[3]) && (!carFlags[12])) {
                        mw = (fix64)0.5f;
                    }
                    if (stageFlags[4]) {
                        mw = (fix64)0.5f;
                    }

                    // Calculate wait time based on rubberband and position
                    // This creates catch-up mechanics - cars behind wait less before accelerating
                    upwait = fix64.FloorToInt((rubberband - position) * (rubberband - position) * (rubberband - position) * mw);
                    if (upwait > 80) {
                        upwait = 80; // Cap maximum wait time
                    }
                    if (stageFlags[1]) {
                        if (upwait < 20) {
                            upwait = 20; // Minimum wait time for certain stages
                        }
                    }
                    
                    // Calculate skip level multiplier
                    mw = 0;
                    if (stageFlags[1] || stageFlags[2]) {
                        mw = 1;
                    }
                    if (stageFlags[3]) {
                        mw = (fix64)0.5f;
                    }
                    if (stageFlags[4]) {
                        mw = (fix64)0.5f;
                    }
                    if (stageFlags[5]) {
                        mw = (fix64)0.2f;
                    }
                    
                    // Dynamically adjust skip level based on position relative to rubberband target
                    if ((position - rubberband) >= -1) {
                        skiplev -= (fix64)0.1f; // Decrease skip level when ahead
                        if (skiplev < 0) {
                            skiplev = 0;
                        }
                    } else {
                        skiplev += (fix64)0.2f; // Increase skip level when behind (more aggressive)
                        if (skiplev > mw) {
                            skiplev = mw;
                        }
                    }
                    
                    // Stage-specific skip level overrides
                    if (stageFlags[9] || stageFlags[12]) {
                        skiplev = 1;
                    }
                    if (stageFlags[11]) {
                        skiplev = 0;
                        if (pcleared == 89 || pcleared == 18) {
                            skiplev = 1;
                        }
                        if (pcleared == 45 || pcleared == 52) {
                            skiplev = (fix64)0.5f;
                        }
                    }
                    if ((stageFlags[13]) && (pcleared == 20)) {
                        skiplev = 1;
                    }
                    if (stageFlags[14]) {
                        skiplev = 1;
                        if (carFlags[17]) {
                            if (random.NextSFloat() > (fix64)0.8f) {
                                acr = 1;
                            } else {
                                acr = 0;
                            }
                        }
                    }
                    if (stageFlags[15]) {
                        skiplev = 0;
                    }
                    if (stageFlags[16]) {
                        if (pcleared >= 45) {
                            skiplev = 0;
                        } else {
                            skiplev = 1;
                        }
                    }
                    
                    // Calculate ramp parameter based on power and randomness
                    rampp = fix64.FloorToInt((random.NextSFloat() * 4) - 2);
                    if (rampp == 2) {
                        rampp = 1;
                    }
                    if (power == 98) {
                        rampp = -1; // Full power gets special handling
                    }
                    if ((power < 75) && (rampp == -1)) {
                        rampp = 0;
                    }
                    if (power < 60) {
                        rampp = 1; // Low power adjusts ramp
                    }
                    
                    // Stage-specific ramp overrides
                    if (stageFlags[11]) {
                        if (pcleared == 18) {
                            rampp = 2;
                        }
                    }
                    if (stageFlags[12]) {
                        if (pcleared == 17) {
                            rampp = 2;
                        }
                    }
                    if (stageFlags[15] || stageFlags[16]) {
                        rampp = 0;
                    }
                    
                    // Determine turn type and aggression periodically
                    if (cntrn == 0) {
                        agressed = false;
                        // Randomly select turn type (0-3)
                        turntyp = fix64.FloorToInt((random.NextSFloat() * 4));
                        if (turntyp == 4) {
                            turntyp = 3;
                        }
                        
                        // Special handling for certain car/stage combos
                        if ((stageFlags[3]) && (carFlags[12])) {
                            turntyp = 1;
                            if (attack == 0) {
                                agressed = true;
                            }
                        }
                        
                        // Reduce turn variety when behind or on certain stages
                        if (((rubberband - position) < 0) || (stageFlags[10])) {
                            turntyp = fix64.FloorToInt((random.NextSFloat() * 2));
                            if (turntyp == 2) {
                                turntyp = 1;
                            }
                        }
                        
                        // Stage-specific turn type overrides
                        if (stageFlags[11]) {
                            if (pcleared == 89) {
                                if (point >= 5) {
                                    turntyp = 2;
                                } else {
                                    turntyp = 0;
                                }
                            }
                            if (pcleared == 9) {
                                turntyp = 2;
                            }
                        }
                        if (stageFlags[13]) {
                            // if (pcleared == 20 || pcleared == 36 || pcleared == 52) {
                            //     turntyp = 1;
                            // } else {
                                if (turntyp == 3) {
                                    turntyp = 1;
                                }
                            // }
                        }
                        if (stageFlags[14]) {
                            turntyp = 0;
                        }
                        
                        // Attack mode changes turn behavior
                        if (attack != 0) {
                            turntyp = 2;
                            if (stageFlags[9] || stageFlags[11] || stageFlags[13] || stageFlags[16] || stageFlags[17]) {
                                turntyp = fix64.FloorToInt((random.NextSFloat() * 3));
                                if (turntyp == 3) {
                                    turntyp = 2;
                                }
                            }
                            if (stageFlags[16]) {
                                if (carFlags[14]) {
                                    turntyp = 0;
                                }
                            }
                        }
                        
                        // Certain stages force aggressive behavior
                        if (stageFlags[6] || stageFlags[7] || stageFlags[8] || stageFlags[10] || stageFlags[11] || stageFlags[12] || stageFlags[14] || stageFlags[16] || stageFlags[17]) {
                            agressed = true;
                        }
                        cntrn = 5; // Reset turn counter
                    } else {
                        cntrn--; // Decrement turn counter
                    }
                    
                    // Calculate safety margin based on power level with randomness
                    saftey = fix64.FloorToInt(((98 - power) * (fix64)0.5f) * ((random.NextSFloat() * (fix64)0.5f) + (fix64)0.5f));
                    if (saftey > 20) {
                        saftey = 20; // Cap maximum safety margin
                    }
                    
                    // Calculate mustland threshold (desire to land soon when airborne)
                    mw = 0;
                    if (stageFlags[1]) {
                        mw = (fix64)0.9f;
                    }
                    if (stageFlags[2]) {
                        mw = (fix64)0.7f;
                    }
                    if (stageFlags[3]) {
                        mw = (fix64)0.4f;
                    }
                    mustland = (mw + ((random.NextSFloat() / 2) - (fix64)0.25f));
                    
                    // Adjust safety multiplier based on stage
                    mw = 1;
                    if (stageFlags[1]) {
                        mw = 5;
                    }
                    if (stageFlags[2]) {
                        mw = 2;
                    }
                    if (stageFlags[3]) {
                        mw = (fix64)1.5f;
                    }
                    
                    // Adjust safety and landing behavior based on power and position
                    if (power > 50) {
                        if ((rubberband - position) > 0) {
                            saftey *= mw; // Increase safety when ahead with good power
                        } else {
                            mustland = 0; // Don't worry about landing when behind
                        }
                    } else {
                        mustland -= (fix64)0.5f; // Lower power = less concern about landing
                    }
                    
                    // Stage-specific landing overrides
                    if (stageFlags[6]) {
                        if (carFlags[13]) {
                            mustland = 0;
                        }
                    }
                    if (stageFlags[8] || stageFlags[10] || stageFlags[12] || stageFlags[14] || stageFlags[16]) {
                        mustland = 0; // These stages don't care about landing
                    }
                    
                    // Configure stunt behavior based on stage
                    stuntf = 0; // 0 = no special stunting
                    if (stageFlags[10]) {
                        if (((position - rubberband) > 1) || (random.NextSFloat() > random.NextSFloat())) {
                            stuntf = 1;
                            saftey = 10;
                        } else {
                            stuntf = 2;
                        }
                    }
                    // if ((stageFlags[11]) && (pcleared == 18)) {
                    //     stuntf = 2;
                    // }
                    if ((stageFlags[11]) && (carFlags[16])) {
                        stuntf = 3;
                    }
                    if (stageFlags[14]) {
                        if (carFlags[17]) {
                            stuntf = 4;
                            saftey = 20;
                        } else {
                            saftey = 10;
                            // if (pcleared == 47) {
                            //     stuntf = 2;
                            // }
                        }
                    }
                    if (stageFlags[16]) {
                        mustland = 0;
                        saftey = 20;
                        // Specific checkpoint stunting configurations
                        // if (pcleared == 152 || pcleared == 20) {
                        //     stuntf = 2;
                        // }
                        // if (pcleared == 35) {
                        //     stuntf = 1;
                        // }
                        // if (pcleared == 112) {
                        //     stuntf = 1;
                        // }
                        // if (pcleared == 120) {
                        //     stuntf = 2;
                        // }
                        avoidnlev = fix64.FloorToInt(2700 * random.NextSFloat());
                    }
                    
                    // Calculate trick preference based on power level
                    trickprf = (((power - 38) / 50) - (random.NextSFloat() * (fix64)0.5f));
                    if (power < 60) {
                        trickprf = -1; // Low power cars avoid tricks
                    }
                    
                    // Stage/car-specific trick preference adjustments
                    if ((stageFlags[3]) && (carFlags[12])) {
                        if (trickprf > (fix64)0.7f) {
                            trickprf = (fix64)0.7f;
                        }
                    }
                    if ((stageFlags[6]) && (!carFlags[13])) {
                        if (trickprf > (fix64)0.3f) {
                            trickprf = (fix64)0.3f;
                        }
                    }
                    if (stageFlags[8]) {
                        if (trickprf > (fix64)0.2f) {
                            trickprf = (fix64)0.2f;
                        }
                    }
                    if (stageFlags[11]) {
                        if (trickprf != -1) {
                            trickprf *= (fix64)0.75f;
                        }
                    }
                    if (stageFlags[12]) {
                        if (pcleared == 55 || pcleared == 7) {
                            trickprf = -1;
                        }
                    }
                    if (stageFlags[14]) {
                        if (trickprf > (fix64)0.5f) {
                            trickprf = (fix64)0.5f;
                        }
                    }
                    if (stageFlags[17]) {
                        trickprf = -1;
                    }
                    
                    // Determine whether to use bounce mechanics
                    if (random.NextSFloat() > (power / 100)) {
                        usebounce = true;
                    } else {
                        usebounce = false;
                    }
                    
                    // Stage/car-specific bounce overrides
                    if (stageFlags[4]) {
                        usebounce = true;
                    }
                    if (stageFlags[6]) {
                        if (carFlags[13]) {
                            usebounce = false;
                        } else {
                            usebounce = true;
                        }
                    }
                    if (stageFlags[9] || stageFlags[10] || stageFlags[14]) {
                        usebounce = false;
                    }
                    
                    // Determine perfection mode (more precise driving) based on health
                    if (random.NextSFloat() > (hitmag / maxmag)) {
                        perfection = false;
                    } else {
                        perfection = true;
                    }
                    if ((100 * hitmag / maxmag) > 60) {
                        perfection = true; // High health = perfect driving
                    }
                    
                    // Stage/car-specific perfection overrides
                    if ((stageFlags[3]) && (carFlags[12])) {
                        perfection = true;
                    }
                    if (stageFlags[6] || stageFlags[8] || stageFlags[10] || stageFlags[11] || stageFlags[12] || stageFlags[14] || stageFlags[16] || stageFlags[17]) {
                        perfection = true; // These stages require perfect driving
                    }
                    if ((stageFlags[15]) && (carFlags[18])) {
                        perfection = true;
                    }
                    
                    // Attack mode initialization
                    if (attack == 0) {
                        // Determine if AI should start swaying
                        var startsway = true;
                        if (stageFlags[3] || stageFlags[1] || stageFlags[4] || stageFlags[9] || stageFlags[13] || stageFlags[16]) {
                            startsway = afta;
                        }
                        if (stageFlags[6] || stageFlags[8] || stageFlags[10] || stageFlags[14]) {
                            startsway = false;
                        }
                        
                        // Determine if only player should be attacked
                        var onlyou = false;
                        if ((stageFlags[3]) && (carFlags[12])) {
                            onlyou = true;
                        }
                        if ((stageFlags[8]) && (carFlags[14])) {
                            onlyou = true;
                        }
                        if ((stageFlags[9]) /*&& (cp.clear[0] >= 8)*/ && (rubberband == 0)) {
                            onlyou = true;
                        }
                        if (stageFlags[11] || stageFlags[12] || stageFlags[13] || stageFlags[15] || stageFlags[16]) {
                            onlyou = true;
                        }
                        
                        // Calculate minimum power required to attack based on stage/car
                        var powtoattack = 60; // Default threshold
                        // Stage-specific power thresholds for attacking
                        if (stageFlags[3] || stageFlags[17]) {
                            powtoattack = 30; // Lower threshold for these stages
                        }
                        if (stageFlags[4]) {
                            powtoattack = 20;
                        }
                        if ((stageFlags[5]) /*&& (c != 6)*/) {
                            powtoattack = 40;
                        }
                        if ((stageFlags[2] || stageFlags[13]) && (carFlags[16])) {
                            powtoattack = 50;
                        }
                        if (stageFlags[7]) {
                            powtoattack = 40;
                        }
                        if ((stageFlags[8]) && (carFlags[14])) {
                            powtoattack = 40;
                        }
                        if ((stageFlags[9]) && (onlyou)) {
                            powtoattack = 30;
                        }
                        if ((stageFlags[11]) && (bulistc)) {
                            powtoattack = 30;
                        }
                        if (stageFlags[12]) {
                            powtoattack = 50;
                        }
                        if ((stageFlags[15]) && (bulistc)) {
                            powtoattack = 40;
                        }
                        if (stageFlags[16]) {
                            if (carFlags[14]) {
                                powtoattack = 40;
                            }
                            if (carFlags[16]) {
                                powtoattack = 50;
                            }
                            if (carFlags[18] || carFlags[12]) {
                                powtoattack = 50;
                            }
                            if (rubberband > position) {
                                powtoattack = 80; // Much higher threshold when ahead of rubberband target
                            }
                        }
                        
                        // Check all other cars to decide if we should enter attack mode
                        for (var i = 0; i < racePhase.CarsInRace.Count; i++)
                        {
                            var otherCarClear = 0; // todo: get from cp.clear[i]
                            var otherCarPosition = 0; // todo: get from position[i]
                            // Skip self and invalid checkpoints
                            if ((i != currentCarIndex) && (clear != -1)) {
                                var otherCar = racePhase.CarsInRace[i];
                                
                                // Calculate our heading angle
                                var myxz = car.Rotation.Xz.Degrees;
                                if (u.Zyinv) {
                                    myxz += 180; // Adjust if car is inverted
                                }
                                // Normalize to -180 to 180
                                while (myxz < 0) {
                                    myxz += 360;
                                }
                                while (myxz > 180) {
                                    myxz -= 360;
                                }
                                
                                // Calculate angle to other car
                                var ad = 0;
                                if ((otherCar.Position.X - conto.X) >= 0) {
                                    ad = 180;
                                }

                                var a = otherCar.Position.Z - conto.Z;
                                var b = (otherCar.Position.X - conto.X);
                                var pnxz = (90 + ad + ((fix64.Atan(b != 0 ? (a / b) : 0) / (fix64)0.017453292519943295f)));
                                while (pnxz < 0) {
                                    pnxz += 360;
                                }
                                while (pnxz > 180) {
                                    pnxz -= 360;
                                }
                                
                                // Calculate visibility angle (how far off-center the target is)
                                var vis = fix64.Abs(myxz - pnxz);
                                if (vis > 180) {
                                    vis = fix64.Abs(vis - 360);
                                }
                                
                                // Calculate attack radius based on checkpoint difference
                                var attackrad = (2000 * (fix64.Abs(otherCarClear - clear) + 1));
                                
                                // Stage/car-specific attack radius overrides
                                if ((stageFlags[3]) && (carFlags[12])) {
                                    if (attackrad < 12000) {
                                        attackrad = 12000; // Minimum attack radius for this combo
                                    }
                                }
                                if (stageFlags[4]) {
                                    if (attackrad < 4000) {
                                        attackrad = 4000;
                                    }
                                }
                                if ((stageFlags[8]) && (carFlags[14])) {
                                    if (attackrad < 12000) {
                                        attackrad = 12000;
                                    }
                                    vis = 10; // Wide visibility angle
                                }
                                if (stageFlags[9]) {
                                    if ((onlyou) && (attackrad < 12000)) {
                                        attackrad = 12000;
                                    }
                                }
                                if (stageFlags[11]) {
                                    if (bulistc) {
                                        attackrad = 8000;
                                        vis = 10;
                                        afta = true;
                                    } else {
                                        if (attackrad < 6000) {
                                            attackrad = 6000;
                                        }
                                    }
                                }
                                if ((stageFlags[12]) && (bulistc)) {
                                    attackrad = 6000;
                                    vis = 10;
                                }
                                // if (stageFlags[13]) {
                                //     if (clear - cp.clear[0] > 3) {
                                //         attackrad = 21000;
                                //     }
                                // }
                                if (stageFlags[15]) {
                                    attackrad *= (fix64.Abs(otherCarClear - clear) + 1);
                                    if (bulistc) {
                                        attackrad = (4000 * (fix64.Abs(otherCarClear - clear) + 1));
                                        vis = 10;
                                    }
                                }
                                if (stageFlags[16]) {
                                    if (carFlags[14]) {
                                        attackrad = 16000;
                                        vis = 10;
                                    }
                                    if (carFlags[16]) {
                                        attackrad = 6000;
                                        vis = 10;
                                    }
                                    if (carFlags[18] || carFlags[12]) {
                                        attackrad *= (fix64.Abs(otherCarClear - clear) + 1);
                                    }
                                }
                                
                                // Calculate visibility angle threshold
                                var visan = (85 + (15 * (fix64.Abs(otherCarClear - clear) + 1)));
                                // if ((stageFlags[9]) && (!onlyou)) {
                                //     if ((car[0].typ != 14) || ((clear - cp.clear[0]) < 8)) {
                                //         visan = 45;
                                //     }
                                // }
                                // Stage-specific visibility angle overrides
                                if (stageFlags[13]) {
                                    visan = 45; // Narrower vision cone
                                }
                                if (stageFlags[16]) {
                                    if (carFlags[18] || carFlags[12] || carFlags[17]) {
                                        visan = (50 + (70 * fix64.Abs(otherCarClear - clear)));
                                    }
                                }
                                
                                // Check if target is within attack range and visibility
                                if ((vis < visan) && (pyo((car.Position.X / 10), (otherCar.Position.X / 10), (car.Position.Z / 10), (otherCar.Position.Z / 10)) < attackrad) && (afta) && (power > powtoattack)) {
                                    // Calculate base importance modifier
                                    var bim = (35 - (fix64.Abs(otherCarClear - clear) * 10));
                                    if (bim < 1) {
                                        bim = 1;
                                    }
                                    
                                    // Calculate attack probability based on position and checkpoint difference
                                    var atp = (((position + 1) * (7 - otherCarPosition)) / bim);
                                    if (!stageFlags[17]) {
                                        if (atp > (fix64)0.7f) {
                                            atp = (fix64)0.7f; // Cap probability
                                        }
                                    }
                                    // if ((i != 0) && (rubberband < position)) {
                                    //     atp = 0;
                                    // }
                                    // if ((i != 0) && (onlyou)) {
                                    //     atp = 0;
                                    // }
                                    
                                    // Stage/car-specific attack probability modifiers
                                    if (stageFlags[3]) {
                                        if ((carFlags[12]) || ((carFlags[16]) && (bulistc))) {
                                            atp *= 2;
                                        } else {
                                            atp *= (fix64)0.5f;
                                        }
                                    }
                                    // if ((stageFlags[5]) && (c == 6)) {
                                    //     atp *= (fix64)0.7f;
                                    // }
                                    if (stageFlags[6]) {
                                        atp = 0;
                                    }
                                    // if ((stageFlags[7]) && (c == 6) && (i == 0)) {
                                    //     atp *= (fix64)1.5f;
                                    // }
                                    if (stageFlags[8]) {
                                        if ((bulistc) && (i == 0)) {
                                            atp = 1;
                                        } else {
                                            atp = 0;
                                        }
                                    }
                                    if (stageFlags[10]) {
                                        atp = 0;
                                    }
                                    if (stageFlags[11]) {
                                        if ((bulistc) && (i == 0)) {
                                            atp = 1;
                                        }
                                        if ((position == 0) || ((position == 1) && (rubberband == 0))) {
                                            atp = 0;
                                        }
                                    }
                                    if (stageFlags[12]) {
                                        if ((!carFlags[14]) && (!carFlags[16])) {
                                            atp = 0;
                                        }
                                        if ((carFlags[16] || carFlags[14]) && (i == 0)) {
                                            atp = 1;
                                        }
                                    }
                                    if (stageFlags[14]) {
                                        atp = 0;
                                    }
                                    if (stageFlags[15]) {
                                        if (position == 0) {
                                            atp *= (fix64)0.5f;
                                        }
                                        if (rubberband < position) {
                                            atp *= 2;
                                        }
                                        if ((bulistc) && (i == 0)) {
                                            atp = 1;
                                        }
                                    }
                                    if (stageFlags[16]) {
                                        if (!carFlags[17]) {
                                            // if (rubberband < position) {
                                            //     if ((cp.clear[0] - clear) != 1) {
                                            //         atp *= 2;
                                            //     }
                                            // }
                                        } else {
                                            atp *= (fix64)0.5f;
                                        }
                                        if ((carFlags[14] || carFlags[16]) && (i == 0) && (bulistc)) {
                                            atp = 1;
                                        }
                                        if ((position == 0) || ((position == 1) && (rubberband == 0))) {
                                            atp = 0;
                                        }
                                        // if (((clear - cp.clear[0]) >= 5) && (i == 0)) {
                                        //     atp = 1;
                                        // }
                                        if (carFlags[11] || carFlags[13] || carFlags[15]) {
                                            atp = 0;
                                        }
                                    }
                                    // Decide to enter attack mode based on probability
                                    if (random.NextSFloat() < atp) {
                                        // Set attack duration based on checkpoint difference
                                        attack = (40 * (Math.Abs(otherCarClear - clear) + 1));
                                        if (attack > 500) {
                                            attack = 500; // Cap attack duration
                                        }
                                        
                                        // Configure aim parameter for different attack behaviors
                                        aim = 0; // 0 = direct, higher = more leading/prediction
                                        // Stage-specific aim adjustments
                                        if ((stageFlags[3]) && (carFlags[12])) {
                                            if (random.NextSFloat() > random.NextSFloat()) {
                                                aim = 1;
                                            }
                                        }
                                        if (stageFlags[4]) {
                                            if ((i == 0) && (rubberband < position)) {
                                                aim = (fix64)1.5f;
                                            } else {
                                                aim = random.NextSFloat();
                                            }
                                        }
                                        if (stageFlags[5]) {
                                            aim = (random.NextSFloat() * (fix64)1.5f);
                                        }
                                        if (stageFlags[7]) {
                                            // if (c != 6) {
                                            if ((random.NextSFloat() > random.NextSFloat()) || (rubberband < position)) {
                                                aim = 1;
                                            }
                                            // }
                                        }
                                        if ((stageFlags[8]) && (carFlags[14])) {
                                            if (random.NextSFloat() > random.NextSFloat()) {
                                                aim = ((fix64)0.76f + (random.NextSFloat() * (fix64)0.76f));
                                            }
                                        }
                                        if (stageFlags[9]) {
                                            aim = 1;
                                            // if ((car[0].typ != 14) || ((cp.clear - cp.clear[0]) < 8)) {
                                            if ((attack > 150) && (!onlyou)) {
                                                attack = 150;
                                            }
                                            // }
                                        }
                                        if (stageFlags[11]) {
                                            if (bulistc) {
                                                aim = (fix64)0.7f;
                                                if (attack > 150) {
                                                    attack = 150;
                                                }
                                            } else {
                                                aim = random.NextSFloat();
                                            }
                                        }
                                        if (stageFlags[12]) {
                                            if (random.NextSFloat() > random.NextSFloat()) {
                                                aim = (fix64)0.7f;
                                                if (carFlags[14]) {
                                                    aim += (fix64)0.7f;
                                                }
                                            }
                                            if (bulistc) {
                                                if (attack > 150) {
                                                    attack = 150;
                                                }
                                            }
                                        }
                                        if (stageFlags[13]) {
                                            if (attack > 60) {
                                                attack = 60;
                                            }
                                        }
                                        if (stageFlags[15]) {
                                            aim = (random.NextSFloat() * (fix64)1.5f);
                                            attack = (attack / 2);
                                            if (random.NextSFloat() > random.NextSFloat()) {
                                                exitattack = true;
                                            } else {
                                                exitattack = false;
                                            }
                                        }
                                        if (stageFlags[16]) {
                                            if (carFlags[14]) {
                                                aim = ((fix64)0.2f + (random.NextSFloat() * (fix64)0.8f));
                                                attack = 70;
                                            }
                                            if (carFlags[16]) {
                                                if (random.NextSFloat() > random.NextSFloat()) {
                                                    aim = (fix64)0.7f;
                                                }
                                                if (attack > 150) {
                                                    attack = 150;
                                                }
                                            }
                                            if (carFlags[18] || carFlags[12] || carFlags[17]) {
                                                aim = (random.NextSFloat() * (fix64)1.5f);
                                                if ((fix64.Abs(otherCarClear - clear) <= 2) || carFlags[17]) {
                                                    attack = (attack / 3);
                                                }
                                            }
                                        }
                                        acr = i; // Store target car index
                                        // Randomize turn type for attack
                                        turntyp = fix64.FloorToInt(1 + random.NextSFloat() * 2);
                                        if (turntyp == 3) {
                                            turntyp = 2;
                                        }
                                    }
                                }
                                // Check if AI should reduce clearance/accuracy due to nearby car (sway behavior)
                                if ((startsway) && (vis > 100) && (pyo((car.Position.X / 10), (otherCar.Position.X / 10), (car.Position.Z / 10), (otherCar.Position.Z / 10)) < 300) && (random.NextSFloat() > ((fix64)0.6f - (position / 10)))) {
                                    clrnce = 0; // Reduce clearance when car behind
                                    acuracy = 0; // Reduce accuracy (more erratic)
                                }
                            }
                        }
                    }
                    // Configure "trick fix" mode based on health
                    var norunfix = false;
                    // Stages where run fix is disabled
                    if (stageFlags[6] || stageFlags[10] || stageFlags[11] || stageFlags[14]) {
                        norunfix = true;
                    }
                    if ((stageFlags[8]) && (!carFlags[14])) {
                        norunfix = true;
                    }
                    if ((stageFlags[15]) && (!bulistc)) {
                        norunfix = true;
                    }
                    if (stageFlags[17]) {
                        norunfix = true;
                    }
                    
                    // Set trick fix level based on health percentage
                    if (trfix != 3) {
                        trfix = 0; // 0 = normal, 1 = cautious, 2 = very cautious, 3 = recovery mode
                        var trunfix = 50; // Threshold for trick fix level 1
                        if (stageFlags[16]) {
                            trunfix = 100; // Never use trick fix on this stage
                        }
                        if ((100 * hitmag / maxmag) > trunfix) {
                            trfix = 1; // Cautious mode when health above threshold
                        }
                        if (!norunfix) {
                            var runfix = 80; // Threshold for trick fix level 2
                            if (stageFlags[9]) {
                                runfix = 70;
                            }
                            if ((100 * hitmag / maxmag) > runfix) {
                                trfix = 2; // Very cautious mode when health very high
                            }
                        }
                    } else {
                        // Override parameters in recovery mode (trfix == 3)
                        upwait = 0;
                        acuracy = 0;
                        skiplev = 1;
                        clrnce = 2;
                    }
                    
                    // Determine if AI should enter "ballistic" (super aggressive) mode
                    if (!bulistc) {
                        // Stage/car-specific ballistic mode triggers
                        if ((stageFlags[8]) && (carFlags[14])) {
                            bulistc = true;
                        }
                        if ((stageFlags[11]) && (carFlags[16])) {
                            bulistc = true;
                        }
                        if ((stageFlags[12]) && (carFlags[16])) {
                            bulistc = true;
                        }
                        if ((stageFlags[15]) /*&& ((cp.clear[0] - cp.clear) >= 2)*/ && (trfix == 0)) {
                            bulistc = true;
                            oupnt = -1; // Reset output point
                        }
                        if (stageFlags[16]) {
                            if ((carFlags[14]) && (clear >= 1)) {
                                bulistc = true;
                            }
                            if (carFlags[16]) {
                                bulistc = true;
                            }
                        }
                        if (stageFlags[5] || stageFlags[8]) {
                            if ((carFlags[16]) /*&& (fix64.Abs(cp.clear[0] - clear) >= 2)*/) {
                                bulistc = true;
                            }
                        }
                    } else {}
                    
                    // Reset status counter with random delay
                    stcnt = 0;
                    statusque = fix64.FloorToInt(20 * random.NextSFloat());
                } else {
                    // Increment status counter if not time to update
                    if (newTick) {
                        stcnt++;
                    }
                }
            }
            
            // Determine which touch detection to use for "riding" state
            bool ride;
            if (usebounce) {
                ride = mad.Wtouch; // Use wheel touch when bounce enabled
            } else {
                ride = mad.Mtouch; // Use main/body touch otherwise
            }
            
            // ===== Main Driving Logic =====
            if (ride) {
                // Reset trick phase when landing
                if (trickfase != 0) {
                    trickfase = 0;
                }
                
                // Cancel attack in certain trick fix modes
                if (trfix == 2 || trfix == 3) {
                    attack = 0;
                }

                // ===== Non-Attack Mode Driving =====
                if (attack == 0) {
                    // Throttle control with periodic pauses for rubberband
                    if (upcnt < 30) {
                        if (revstart <= 0) {
                            u.Up = true; // Normal acceleration
                        } else {
                            u.Down = true; // Reverse start
                            if (newTick) {
                                revstart--;
                            }
                        }
                    }
                    // Cycle throttle with wait periods
                    if (upcnt < (25 + actwait)) {
                        if (newTick) {
                            upcnt++;
                        }
                    } else {
                        upcnt = 0;
                        actwait = upwait; // Use calculated wait time
                    }
                    
                    var pnt = point; // Current AI node to target
                    
                    // Calculate power threshold for ballistic mode
                    var powbul = 50;
                    if (stageFlags[8]) {
                        powbul = 20;
                    }
                    if (stageFlags[15]) {
                        powbul = 40;
                    }
                    if (stageFlags[16]) {
                        if (carFlags[14] || carFlags[16]) {
                            powbul = 0; // Always ballistic for these cars
                        }
                    }
                    
                    // ===== Normal Navigation (non-ballistic) =====
                    if ((!bulistc) || (trfix == 2) || (trfix == 3) || (trfix == 4) || (power < powbul)) {
                        var tpnt = 0;
                        
                        // Handle ramp nodes based on rampp setting
                        if ((rampp == 1) && (currentStage.nodes[pnt].Kind is not AiNodeKind.CheckPoint)) {
                            tpnt = (pnt + 1);
                            if (tpnt >= currentStage.nodes.Count) {
                                tpnt = 0;
                            }
                            if (currentStage.nodes[pnt].Kind is AiNodeKind.Ramp) {
                                pnt = tpnt; // Skip ramp node
                            }
                        }
                        if (rampp == -1) {
                            if (currentStage.nodes[pnt].Kind is AiNodeKind.Ramp) {
                                pnt++; // Skip ramp node
                                if (pnt >= currentStage.nodes.Count) {
                                    pnt = 0;
                                }
                            }
                        }
                        
                        // Node skipping logic based on skiplev
                        if ((!stageFlags[9]) || (pnt != 0)) {
                            if (random.NextSFloat() > skiplev) {
                                tpnt = pnt;
                                // Check if current node is an important checkpoint
                                var notimpcp = false;
                                if (currentStage.nodes[tpnt].Kind is AiNodeKind.CheckPoint) {
                                    var ic = 0;
                                    for (var i = 0; i < currentStage.nodes.Count; i++) {
                                        if ((currentStage.nodes[tpnt].Kind is AiNodeKind.CheckPoint) && (i < tpnt)) {
                                            ic++;
                                        }
                                    }
                                    notimpcp = (clear != (ic + (car.currentLap * currentStage.checkpoints.Count)));
                                }
                                // Skip Road and Halfpipe nodes, and non-important checkpoints
                                while (currentStage.nodes[tpnt].Kind is AiNodeKind.Road or AiNodeKind.Halfpipe || notimpcp) {
                                    pnt = tpnt;
                                    tpnt++;
                                    if (tpnt >= currentStage.nodes.Count) {
                                        tpnt = 0;
                                    }
                                    notimpcp = false;
                                    if (currentStage.nodes[tpnt].Kind is AiNodeKind.CheckPoint) {
                                        var ic = 0;
                                        for (var i = 0; i < currentStage.nodes.Count; i++) {
                                            if (currentStage.nodes[i].Kind is AiNodeKind.CheckPoint && (i < tpnt)) {
                                                ic++;
                                            }
                                        }
                                        notimpcp = (clear != (ic + (car.currentLap * currentStage.checkpoints.Count)));
                                    }
                                }
                            } else {
                                if (random.NextSFloat() > skiplev) {
                                    // Skip all Road nodes
                                    while (currentStage.nodes[pnt].Kind is AiNodeKind.Road) { // was 'Turn' but this makes more sense
                                        pnt++;
                                        if (pnt >= currentStage.nodes.Count) {
                                            pnt = 0;
                                        }
                                    }
                                }
                            }
                        }
                        // if ((stageFlags[1]) && (unlocked == 1)) {
                        //     if (pnt == 20) {
                        //         pnt = 0;
                        //     }
                        // }
                        // if (stageFlags[11]) {
                        //     if ((pcleared == 18) && (pnt >= 26)) {
                        //         if (pnt <= 32) {
                        //             pnt = 32;
                        //         } else {
                        //             pnt = 38;
                        //         }
                        //     }
                        //     if (pcleared == 67) {
                        //         pnt = 74;
                        //     }
                        // }
                        
                        // ===== Stage-Specific Waypoint Overrides =====
                        if (stageFlags[12]) {
                            // Skip road nodes at specific checkpoints
                            if (pcleared == 27 || pcleared == 37) {
                                while (currentStage.nodes[pnt].Kind is AiNodeKind.Road) { // was 'Turn' but this makes more sense
                                    pnt++;
                                    if (pnt >= currentStage.nodes.Count) {
                                        pnt = 0;
                                    }
                                }
                            }
                        }
                        if (stageFlags[13]) {
                            // if ((pcleared == 20) && (focus == 36) && (car.y < 100)) {
                            //     pnt = 30;
                            //     if (pyo((car.Position.X / 10), (cp.x[30] / 10), (car.Position.Z / 10), (cp.z[30] / 10)) < 1000) {
                            //         focus = -1;
                            //     }
                            // }
                            // if ((pcleared == 20) && (trfix < 2)) {
                            //     if ((pnt >= 26) && (pnt < 30)) {
                            //         pnt = 30;
                            //     }
                            // }
                            // if ((pcleared == 52) && (trfix < 2)) {
                            //     pnt = 60;
                            // }
                            // if (trfix == 3 || trfix == 4) {
                            //     nofocus = true;
                            //     if (pnt < 66) {
                            //         pnt = 66;
                            //     }
                            // }
                            // if (trfix == 1) {
                            //     fpnt[0] = 61;
                            // }
                            // if (trfix == 2) {
                            //     fpnt[0] = 65;
                            // }
                            
                            // Recovery mode timeout logic
                            if ((100 * hitmag / maxmag) < 10) {
                                trfix = 0; // Exit recovery mode when health too low
                            }
                            if (trfix == 3) {
                                oupnt++; // Count frames in recovery mode
                                if (oupnt == 300) {
                                    trfix = 0; // Exit recovery after 300 frames
                                }
                            } else {
                                if (oupnt != 0) {
                                    oupnt = 0; // Reset counter when not in recovery
                                }
                            }
                        }
                        if (stageFlags[14]) {
                            // Set flag when passing specific checkpoint
                            if ((pcleared == 47) && (pnt > 58) && (oupnt == 0)) {
                                oupnt = 1;
                            }
                            // Skip road nodes for most cars
                            if ((!carFlags[17]) || (acr == 0)) {
                                while (currentStage.nodes[pnt].Kind is AiNodeKind.Road) { // was 'Turn' but this makes more sense
                                    pnt++;
                                    if (pnt >= currentStage.nodes.Count) {
                                        pnt = 0;
                                    }
                                }
                            }
                        }
                        if (stageFlags[15]) {
                            // Force specific waypoint at checkpoint 10
                            if ((pcleared == 10) && (trfix < 2)) {
                                if (pnt != 18) {
                                    pnt = 18;
                                }
                            }
                        }
                        if (stageFlags[16]) {
                            // Complex position-based waypoint selection
                            if ((trfix != 2) && (trfix != 3)) {
                                // Position-based waypoint overrides for optimal routing
                                if ((pcleared == 152) && (car.Position.Z < 2700)) {
                                    pnt = 5;
                                }
                                if ((pcleared == 10) && (car.Position.X < 700)) {
                                    pnt = 13;
                                }
                                if (pcleared == 20) {
                                    if ((car.Position.Z < 6200) && (car.Position.Z > 3400)) {
                                        pnt = 126;
                                    }
                                    if (car.Position.Z < 3400) {
                                        pnt = 35;
                                    }
                                }
                                if ((pcleared == 35) && (car.Position.X < 6300) && (car.Position.X > 4000)) {
                                    pnt = 116;
                                }
                                if ((pcleared == 55) && (car.Position.X < 4500)) {
                                    pnt = 59;
                                }
                                // Sequential waypoint forcing for specific sections
                                if (pcleared == 64) {
                                    pnt = 74;
                                }
                                if (pcleared == 74) {
                                    pnt = 82;
                                }
                                if (pcleared == 82) {
                                    pnt = 92;
                                }
                                if (pcleared == 103) {
                                    pnt = 112;
                                }
                                if ((pcleared == 112) && (car.Position.X > 3200)) {
                                    pnt = 120;
                                }
                                if ((pcleared == 120) && (car.Position.X > 2800)) {
                                    pnt = 129;
                                }
                                if ((pcleared == 129)) {
                                    pnt = 152;
                                }
                            }
                            // Avoidance behavior (TODO generalize to all cars)
                            // if (((cp.clear - cp.clear[0]) >= 2) && (pyo((car.x / 10), (car[0].x / 10), (car.z / 10), (car[0].z / 10)) < (1000 + avoidnlev))) {
                            //     var myxz = conto.Xz;
                            //     if (u.Zyinv) {
                            //         myxz += 180;
                            //     }
                            //     while (myxz < 0) {
                            //         myxz += 360;
                            //     }
                            //     while (myxz > 180) {
                            //         myxz -= 360;
                            //     }
                            //     var ad = 0;
                            //     if ((car[0].x - car.x) >= 0) {
                            //         ad = 180;
                            //     }
                            //     var pnxz = fix64.FloorToInt(90 + ad + ((fix64.Atan(((car[0].z - car.z) / (car[0].x - car.x))) / 0.017453292519943295)));
                            //     while (pnxz < 0) {
                            //         pnxz += 360;
                            //     }
                            //     while (pnxz > 180) {
                            //         pnxz -= 360;
                            //     }
                            //     var vis = fix64.Abs(myxz - pnxz);
                            //     if (vis > 180) {
                            //         vis = fix64.Abs(vis - 360);
                            //     }
                            //     if (vis < 90) {
                            //         wall = 0;
                            //     }
                            // }
                        }
                        
                        // Special ramp handling mode
                        if (rampp == 2) {
                            tpnt = (pnt + 1);
                            if (tpnt >= currentStage.nodes.Count) {
                                tpnt = 0;
                            }
                            // Move back one node if next is a ramp and we're not at current point
                            if ((currentStage.nodes[tpnt].Kind is AiNodeKind.Ramp) && (pnt != point)) {
                                pnt--;
                                if (pnt < 0) {
                                    pnt += currentStage.nodes.Count;
                                }
                            }
                        }
                        
                        // Override focus behavior in ballistic mode
                        if (bulistc) {
                            nofocus = true;
                            if (gowait) {
                                gowait = false;
                            }
                        }
                    } else {
                        // ===== Ballistic Mode Navigation =====
                        if (!stageFlags[15] || runbul == 0) {
                            // Move back 2 nodes and skip halfpipe nodes
                            pnt -= 2;
                            if (pnt < 0) {
                                pnt += currentStage.nodes.Count;
                            }
                            while (currentStage.nodes[pnt].Kind is AiNodeKind.Halfpipe) {
                                pnt--;
                                if (pnt < 0) {
                                    pnt += currentStage.nodes.Count;
                                }
                            }
                        }
                        
                        // Stage-specific ballistic waypoint adjustments
                        if (stageFlags[11]) {
                            if ((pnt < 38) && (pnt > 25)) {
                                pnt = 25;
                            }
                        }
                        // Camping behavior. Does not make sense in multiplayer
                        // if (stageFlags[12]) {
                        //     if (!gowait) {
                        //         if (cp.clear[0] == 0) {
                        //             wtx = 350;
                        //             wtz = 1900;
                        //             frx = 350;
                        //             frz = 3900;
                        //             frad = 12000;
                        //             oupnt = 37;
                        //             gowait = true;
                        //             afta = false;
                        //         }
                        //         if (cp.clear[0] == 7) {
                        //             wtx = 4480;
                        //             wtz = 4032;
                        //             frx = 4480;
                        //             frz = 3472;
                        //             frad = 30000;
                        //             oupnt = 27;
                        //             gowait = true;
                        //             afta = false;
                        //         }
                        //         if (cp.clear[0] == 10) {
                        //             wtx = 0;
                        //             wtz = 4873;
                        //             frx = 0;
                        //             frz = 3858;
                        //             frad = 90000;
                        //             oupnt = 55;
                        //             gowait = true;
                        //             afta = false;
                        //         }
                        //         if (cp.clear[0] == 14) {
                        //             wtx = 350;
                        //             wtz = 1900;
                        //             frx = 1470;
                        //             frz = 3900;
                        //             frad = 45000;
                        //             oupnt = 37;
                        //             gowait = true;
                        //             afta = false;
                        //         }
                        //         if (cp.clear[0] == 18) {
                        //             wtx = 4830;
                        //             wtz = -455;
                        //             frx = 4830;
                        //             frz = 560;
                        //             frad = 90000;
                        //             oupnt = 17;
                        //             gowait = true;
                        //             afta = false;
                        //         }
                        //     }
                        //     if (gowait) {
                        //         if (pyo((car.Position.X / 10), (wtx / 10), (car.Position.Z / 10), (wtz / 10)) < 10000) {
                        //             if (mad.Speed > 5) {
                        //                 u.Up = false;
                        //             }
                        //         }
                        //         if (pyo((car.Position.X / 10), (wtx / 10), (car.Position.Z / 10), (wtz / 10)) < 200) {
                        //             u.Up = false;
                        //             u.Handb = true;
                        //         }
                        //         if ((pcleared[0] == oupnt) && (pyo((car[0].x / 10), (frx / 10), (car[0].z / 10), (frz / 10)) < frad)) {
                        //             afta = true;
                        //             gowait = false;
                        //         }
                        //         if (pyo((car.x / 10), (car[0].x / 10), (car.z / 10), (car[0].z / 10)) < 25) {
                        //             afta = true;
                        //             gowait = false;
                        //             attack = 200;
                        //             acr = 0;
                        //         }
                        //     }
                        // }
                        if (stageFlags[15]) {
                            // Find closest ramp node when oupnt is -1
                            if (oupnt == -1) {
                                fix64 pyclos = -10; // -10 means not found yet
                                for (var i = 0; i < currentStage.nodes.Count; i++) {
                                    if ((currentStage.nodes[pnt].Kind is AiNodeKind.Ramp) && (i < 71)) {
                                        if ((pyo((car.Position.X / 10), (currentStage.nodes[i].Position.X / 10), (car.Position.Z / 10), (currentStage.nodes[i].Position.Z / 10)) < pyclos) || (pyclos == -10)) {
                                            pyclos = pyo((car.Position.X / 10), (currentStage.nodes[i].Position.X / 10), (car.Position.Z / 10), (currentStage.nodes[i].Position.Z / 10));
                                            oupnt = i; // Store closest ramp
                                        }
                                    }
                                }
                                oupnt--; // Target node before the ramp
                                if (oupnt < 0) {
                                    oupnt += currentStage.nodes.Count;
                                }
                            }
                            // Navigate to the stored target
                            if ((oupnt >= 0) && (oupnt < currentStage.nodes.Count)) {
                                pnt = oupnt;
                                // When close to target, reset with random delay
                                if (pyo((car.Position.X / 10), (currentStage.nodes[pnt].Position.X / 10), (car.Position.Z / 10), (currentStage.nodes[pnt].Position.Z / 10)) < 800) {
                                    oupnt = -fix64.FloorToInt(75 + (random.NextSFloat() * 200)); // Negative = delay counter
                                    runbul = fix64.FloorToInt(50 + (random.NextSFloat() * 100)); // Duration counter
                                }
                            }
                            // Count down delay
                            if (oupnt < -1) {
                                oupnt++;
                            }
                            // Count down duration
                            if (runbul != 0) {
                                runbul--;
                            }
                        }
                        if (stageFlags[16]) {
                            // MASHEEN camping behavior. Maybe could be reimplmented as general behavior
                            // if (carFlags[14]) {
                            //     if (power > 60) {
                            //         var i = pcleared[0];
                            //         var found = -1;
                            //         while (found == -1) {
                            //             i++;
                            //             if (i >= currentStage.nodes.Count) {
                            //                 i -= currentStage.nodes.Count;
                            //                 found = 152;
                            //             }
                            //             if (cp.typ[i] == 1 || cp.typ[i] == 2) {
                            //                 found = i;
                            //             }
                            //         }
                            //         if (found == 129) {
                            //             found = 152;
                            //         }
                            //         if (pyo((car.x / 10), (cp.x[found] / 10), (car.z / 10), (cp.z[found] / 10)) < 1000) {
                            //             acr = 0;
                            //             aim = 1;
                            //             attack = 100;
                            //         }
                            //         pnt = found;
                            //     } else {
                            //         var pyclos = -10;
                            //         for (var i = 0; i < currentStage.nodes.Count; i++) {
                            //             if (obo[cp.obi[i]].typ == 24 || obo[cp.obi[i]].typ == 25 || obo[cp.obi[i]].typ == 43 || obo[cp.obi[i]].typ == 45) {
                            //                 if ((pyo((car.x / 10), (cp.x[i] / 10), (car.z / 10), (cp.z[i] / 10)) < pyclos) || (pyclos == -10)) {
                            //                     pyclos = pyo((car.x / 10), (cp.x[i] / 10), (car.z / 10), (cp.z[i] / 10));
                            //                     pnt = i;
                            //                 }
                            //             }
                            //         }
                            //     }
                            // }
                            // DR Monstaa camping behavior
                            // if (carFlags[16]) {
                            //     if (!gowait) {
                            //         if (pcleared[0] == 152 || pcleared[0] == 10 || pcleared[0] == 20) {
                            //             wtx = 820;
                            //             wtz = 120;
                            //             frx = 1364;
                            //             frz = 218;
                            //             frad = 50000;
                            //             oupnt = 35;
                            //             gowait = true;
                            //             afta = false;
                            //             runbul = 1;
                            //         }
                            //         if (pcleared[0] == 45 || pcleared[0] == 55) {
                            //             if (power > 60) {
                            //                 wtx = 3000;
                            //                 wtz = 1600;
                            //                 frx = 4206;
                            //                 frz = 1365;
                            //                 frad = 30000;
                            //                 oupnt = 64;
                            //                 gowait = true;
                            //                 afta = false;
                            //                 runbul = 1;
                            //             } else {
                            //                 pnt = 73;
                            //             }
                            //         }
                            //         if (pcleared[0] == 92 || pcleared[0] == 103) {
                            //             if (power > 60) {
                            //                 wtx = 6408.2;
                            //                 wtz = 1571.4;
                            //                 frx = 6184;
                            //                 frz = 780;
                            //                 frad = 90000;
                            //                 oupnt = 112;
                            //                 gowait = true;
                            //                 afta = false;
                            //                 runbul = 0;
                            //             } else {
                            //                 pnt = 73;
                            //             }
                            //         }
                            //         if ((pcleared[0] == 112) && (oupnt == 112)) {
                            //             stcnt = 0;
                            //             statusque = 10;
                            //             if (pyo((car.x / 10), (frx / 10), (car.z / 10), (frz / 10)) < 100) {
                            //                 attack = 200;
                            //                 acr = 0;
                            //                 aim = 0.7;
                            //                 oupnt = -1;
                            //             }
                            //         }
                            //     }
                            //     if (gowait) {
                            //         if (pyo((car.x / 10), (wtx / 10), (car.z / 10), (wtz / 10)) < 10000) {
                            //             if (speed > 5) {
                            //                 u.up = false;
                            //             }
                            //         }
                            //         if (pyo((car.x / 10), (wtx / 10), (car.z / 10), (wtz / 10)) < 200) {
                            //             u.up = false;
                            //             u.handb = true;
                            //         }
                            //         if ((pcleared[0] == oupnt) && (pyo((car[0].x / 10), (frx / 10), (car[0].z / 10), (frz / 10)) < frad)) {
                            //             afta = true;
                            //             gowait = false;
                            //             if (runbul) {
                            //                 attack = 200;
                            //                 acr = 0;
                            //                 aim = 0.7;
                            //             }
                            //         }
                            //         if (pyo((car.x / 10), (car[0].x / 10), (car.z / 10), (car[0].z / 10)) < 25) {
                            //             afta = true;
                            //             gowait = false;
                            //             attack = 200;
                            //             acr = 0;
                            //         }
                            //     }
                            // }
                        }
                        nofocus = true; // Disable focus targeting in ballistic mode
                    }
                    
                    // ===== Fix Hoops / Recovery Navigation =====
                    // if (!stageFlags[17]) {
                    // Fixing except on Mad Party
                    if (missedcp == 0 || forget || trfix == 4) {
                        if (trfix != 0) {
                            var istr = 0; // Start index for fix hoop search
                            // if ((stageFlags[9]) && (car[0].typ == 14) && ((cp.clear - cp.clear[0]) >= 4)) {
                            //     istr = 1;
                            // }
                            // if (stageFlags[16]) {
                            //     istr = 2;
                            // }
                            
                            // trfix == 2: Find and navigate to closest fix hoop
                            if (trfix == 2) {
                                // if (stageFlags[15]) {
                                //     istr = 2;
                                // }
                                fix64 pyclos = -10; // Closest distance (-10 = not found)
                                var closi = 0; // Closest hoop index
                                for (var i = istr; i < currentStage.fixHoops.Count; i++) {
                                    if ((pyo((car.Position.X / 10), (currentStage.fixHoops[i].Position.X / 10), (car.Position.Z / 10), (currentStage.fixHoops[i].Position.Z / 10)) < pyclos) || (pyclos == -10)) {
                                        pyclos = pyo((car.Position.X / 10), (currentStage.fixHoops[i].Position.X / 10), (car.Position.Z / 10), (currentStage.fixHoops[i].Position.Z / 10));
                                        closi = i;
                                    }
                                }
                                // if (stageFlags[12]) {
                                //     closi = 1;
                                // }
                                pnt = currentStage.nodes.IndexOf(currentStage.fixHoops[closi]);
                                if (currentStage.fixHoops[closi].IsSpecial) {
                                    forget = true; // Special hoops can be forgotten
                                } else {
                                    forget = false;
                                }
                            }
                            
                            // Check if close to any fix hoop to enter trfix == 3 (immediate recovery)
                            for (var i = istr; i < currentStage.fixHoops.Count; i++) {
                                if (pyo((car.Position.X / 10), (currentStage.fixHoops[i].Position.X / 10), (car.Position.Z / 10), (currentStage.fixHoops[i].Position.Z / 10)) < 2000) {
                                    forget = false;
                                    actwait = 0;
                                    upwait = 0;
                                    turntyp = 2;
                                    randtcnt = -1;
                                    acuracy = 0;
                                    rampp = 0;
                                    trfix = 3; // Enter immediate recovery mode
                                }
                            }
                            if (trfix == 3) {
                                nofocus = true; // Disable focus in recovery mode
                            }
                        }
                    }
                    // }
                    
                    // ===== Calculate steering target angle (pan) =====
                    if (turncnt > randtcnt) {
                        if (!gowait) {
                            // Calculate angle to target waypoint
                            var ad = 0;
                            if ((currentStage.nodes[pnt].Position.X - car.Position.X) >= 0) {
                                ad = 180;
                            }

                            var a = (currentStage.nodes[pnt].Position.Z - car.Position.Z);
                            var b = (currentStage.nodes[pnt].Position.X - car.Position.X);
                            pan = fix64.FloorToInt(90 + ad + ((fix64.Atan(b != 0 ? (a / b) : 0) / (fix64)0.017453292519943295f)));
                        } else {
                            // Calculate angle to wait position
                            var ad = 0;
                            if ((wtx - currentStage.nodes[pnt].Position.X) >= 0) {
                                ad = 180;
                            }

                            var a = (wtz - currentStage.nodes[pnt].Position.Z);
                            var b = (wtx - car.Position.X);
                            pan = fix64.FloorToInt(90 + ad + ((fix64.Atan(b != 0 ? (a / b) : 0) / (fix64)0.017453292519943295f)));
                        }
                        turncnt = 0;
                        randtcnt = fix64.FloorToInt(acuracy * random.NextSFloat()); // Random angle update delay
                    } else {
                        if (newTick) {
                            turncnt++;
                        }
                    }
                } else {
                    // ===== Attack Mode Steering =====
                    u.Up = true; // Always accelerate in attack mode
                    
                    // Calculate predicted target position with aim lead
                    var ad = 0;
                    var adsto = ((UMath.Py(car.Position.X, racePhase.CarsInRace[acr].Position.X, car.Position.Z, racePhase.CarsInRace[acr].Position.Z) / 2) * aim);
                    var acx = (racePhase.CarsInRace[acr].Position.X - (adsto * UMath.Sin(racePhase.CarsInRace[acr].Mad.Mxz)));
                    var acz = (racePhase.CarsInRace[acr].Position.Z + (adsto * UMath.Cos(racePhase.CarsInRace[acr].Mad.Mxz)));
                    if ((acx - car.Position.X) >= 0) {
                        ad = 180;
                    }

                    var a = (acz - car.Position.Z);
                    var b = (acx - car.Position.X);
                    pan = (90 + ad + ((fix64.Atan(b != 0 ? (a / b) : 0) / (fix64)0.017453292519943295f)));
                    
                    // Decrement attack timer
                    if (newTick) {
                        attack--;
                    }
                    if (attack <= 0) {
                        attack = 0;
                    }
                    
                    // Stage-specific attack exit conditions
                    if ((stageFlags[15]) && (exitattack) && (!bulistc) && (missedcp != 0)) {
                        attack = 0; // Exit if missed checkpoint in stage 15
                    }
                    if ((stageFlags[16]) && (missedcp != 0)) {
                        if ((position == 0) || ((position == 1) && (rubberband == 0))) {
                            attack = 0; // Leaders don't attack when off course
                        }
                    }
                    if ((stageFlags[16]) && (rubberband > position) && (power < 80)) {
                        attack = 0; // Don't attack when behind and low power
                    }
                }
                
                // ===== Steering Logic =====
                var crxz = car.Rotation.Xz.Degrees; // Current car heading
                if (u.Zyinv) {
                    crxz += 180; // Adjust if car is inverted
                }
                // Normalize current angle to -180 to 180 range
                while (crxz < 0) {
                    crxz += 360;
                }
                while (crxz > 180) {
                    crxz -= 360;
                }
                // Normalize target angle to -180 to 180 range
                while (pan < 0) {
                    pan += 360;
                }
                while (pan > 180) {
                    pan -= 360;
                }
                
                // Wall collision reduces clearance
                if ((wall != -1) && (hold == 0)) {
                    clrnce = 0;
                }
                
                // Execute steering unless in hold state
                if (hold == 0) {
                    // Calculate angle difference and steer accordingly
                    if (fix64.Abs(crxz - pan) < 180) {
                        if (fix64.FloorToInt(fix64.Abs(crxz - pan)) > clrnce) {
                            if (crxz > pan) {
                                u.Left = true;
                                lastl = true; // Remember last turn direction
                            } else {
                                u.Right = true;
                                lastl = false;
                            }
                            // Use advanced turning techniques for sharp turns
                            if ((fix64.Abs(crxz - pan) > 50) && (mad.Speed > mad.Stat.Swits[0]) && (turntyp != 0)) {
                                if (turntyp == 1) {
                                    u.Down = true; // Reverse turning
                                }
                                if (turntyp == 2) {
                                    u.Handb = true; // Handbrake turning
                                }
                                if (!agressed) {
                                    u.Up = false; // Release throttle unless aggressive
                                }
                            }
                        }
                    } else {
                        // Handle wrapping around 180/-180 boundary
                        if (fix64.FloorToInt(fix64.Abs(crxz - pan)) < (360 - clrnce)) {
                            if (crxz > pan) {
                                u.Right = true;
                                lastl = false;
                            } else {
                                u.Left = true;
                                lastl = true;
                            }
                            if ((fix64.Abs(crxz - pan) < 310) && (mad.Speed > mad.Stat.Swits[0]) && (turntyp != 0)) {
                                if (turntyp == 1) {
                                    u.Down = true;
                                }
                                if (turntyp == 2) {
                                    u.Handb = true;
                                }
                                if (!agressed) {
                                    u.Up = false;
                                }
                            }
                        }
                    }
                }
                
                // Wall collision handling
                if (wall != -1) {
                    // If hit a different wall, use last turn direction
                    if (lwall != wall) {
                        if (lastl) {
                            u.Left = true;
                        } else {
                            u.Right = true;
                        }
                        wlastl = lastl; // Store wall last left
                        lwall = wall; // Store last wall
                    } else {
                        // Same wall, use stored direction
                        if (wlastl) {
                            u.Left = true;
                        } else {
                            u.Right = true;
                        }
                    }
                    // This goes unused
                    // if (build[obo[wall].typ].dam != 0) {
                    //     var hmlt = 1;
                    //     if (build[obo[wall].typ].skid == 1) {
                    //         hmlt = 3;
                    //     }
                    //     hold += hmlt;
                    //     if (hold > (10 * hmlt)) {
                    //         hold = (10 * hmlt);
                    //     }
                    // } else {
                        hold = 1; // Lock steering briefly after wall hit
                    // }
                    wall = -1; // Clear wall flag
                } else {
                    if (hold != 0) {
                        hold--; // Decrement hold counter
                    }
                }
            } else {
                // ===== Airborne Logic (not riding on ground) =====
                
                // Trick initiation phase
                if (trickfase == 0) {
                    // Calculate "upward velocity" factor from suspension compression
                    var upv = (((mad.Scy[0] + mad.Scy[1] + mad.Scy[2] + mad.Scy[3]) * (car.Position.Y + 5)) / 40);
                    var divo = 3; // Trick preference divisor
                    if (stageFlags[15] || stageFlags[16]) {
                        divo = 10; // Less likely to trick on these stages
                    }
                    // if ((stageFlags[13]) && (pcleared == 20) && (point > 32) && (cp.clear - cp.clear[0] <= 3)) {
                    //     upv = 0;
                    // }
                    // if ((stageFlags[16]) && (pcleared == 120) && (car.z < 2200)) {
                    //     upv = 0;
                    // }
                    
                    // Decide whether to perform tricks based on upv and trickprf
                    if ((upv > 7) && ((random.NextSFloat() > (trickprf / divo)) || (stuntf == 4))) {
                        // Store initial pitch/yaw for trick reference
                        oxy = mad.Pxy;
                        ozy = mad.Pzy;
                        flycnt = 0;
                        uddirect = 0; // Up/down trick direction
                        lrdirect = 0; // Left/right trick direction
                        udswt = false; // Up/down switch flag
                        lrswt = false; // Left/right switch flag
                        trickfase = 1; // Enter trick execution phase
                        
                        if (upv < 16) {
                            // Low airtime: simple backflip
                            uddirect = -1;
                            udstart = 0;
                            udswt = false;
                        } else {
                            // High airtime: complex tricks
                            if (((random.NextSFloat() > random.NextSFloat()) && (stuntf != 3)) || (stuntf == 1) || (stuntf == 2) || (stuntf == 4)) {
                                // Prioritize pitch tricks (flips)
                                if ((stuntf == 4) && (pcleared == 47) && (oupnt == 0)) {
                                    stuntf = 5;
                                }
                                if ((random.NextSFloat() > random.NextSFloat() || stuntf == 2 || stuntf == 5) && (stuntf != 1) && (stuntf != 4)) {
                                    uddirect = -1; // Backflip
                                } else {
                                    uddirect = 1; // Frontflip
                                }
                                udstart = fix64.FloorToInt(10 * random.NextSFloat() * trickprf); // Delay before starting
                                if (stuntf == 4 || stuntf == 5 || stageFlags[16]) {
                                    udstart = 0; // No delay for special stunts
                                }
                                if ((random.NextSFloat() > (fix64)0.85f) && (stuntf != 1) && (stuntf != 4) && (stuntf != 5)) {
                                    udswt = true; // Switch direction mid-trick
                                }
                                // Add roll trick component
                                if ((random.NextSFloat() > (trickprf + (fix64)0.3f)) && (stuntf != 1) && (stuntf != 2) && (stuntf != 4) && (stuntf != 5)) {
                                    if (random.NextSFloat() > random.NextSFloat()) {
                                        lrdirect = -1; // Roll left
                                    } else {
                                        lrdirect = 1; // Roll right
                                    }
                                    lrstart = fix64.FloorToInt(30 * random.NextSFloat());
                                    if (random.NextSFloat() > (fix64)0.75f) {
                                        lrswt = true;
                                    }
                                }
                            } else {
                                // Prioritize roll tricks
                                if (random.NextSFloat() > random.NextSFloat()) {
                                    lrdirect = -1;
                                } else {
                                    lrdirect = 1;
                                }
                                lrstart = fix64.FloorToInt(10 * random.NextSFloat() * trickprf);
                                if (random.NextSFloat() > (fix64)0.75f) {
                                    lrswt = true;
                                }
                                // Add flip trick component
                                if (random.NextSFloat() > (trickprf + (fix64)0.3f)) {
                                    if ((random.NextSFloat() > random.NextSFloat()) || (stuntf == 3)) {
                                        uddirect = -1;
                                    } else {
                                        uddirect = 1;
                                    }
                                    udstart = fix64.FloorToInt(30 * random.NextSFloat());
                                    if (random.NextSFloat() > (fix64)0.85f) {
                                        udswt = true;
                                    }
                                }
                            }
                        }
                        
                        // Boost stunting mode: aggressive double flip
                        if ((stageFlags[16]) && (carFlags[16]) || booststunting) {
                            lrdirect = -1;
                            lrstart = 0;
                            uddirect = -1;
                            udstart = 0;
                        }
                        
                        // Override tricks in recovery mode
                        if (trfix == 3 || trfix == 4) {
                            if (!stageFlags[8]) {
                                if (lrdirect == -1) {
                                    uddirect = -1; // Force backflip if rolling left
                                }
                                lrdirect = 0; // Cancel roll
                                if (power < 60) {
                                    uddirect = -1; // Force backflip when low power
                                }
                            } else {
                                if (uddirect != 0) {
                                    uddirect = -1; // Force backflip
                                }
                                lrdirect = 0; // Cancel roll
                            }
                        }
                    } else {
                        trickfase = -1; // Skip tricks
                    }
                    if (!afta) {
                        afta = true; // Enable attack mode after first jump
                    }
                    if (trfix == 3) {
                        trfix = 4; // Advance recovery mode
                        statusque += 30; // Delay next AI update
                    }
                }
                
                // Trick execution phase
                if (trickfase == 1) {
                    flycnt++; // Count airborne frames
                    
                    // Execute roll trick
                    if (lrdirect != 0) {
                        if (flycnt > lrstart) {
                            // Switch roll direction mid-trick
                            if (lrswt) {
                                if (fix64.Abs(mad.Pxy - oxy) > 180) {
                                    if (lrdirect == -1) {
                                        lrdirect = 1;
                                    } else {
                                        lrdirect = -1;
                                    }
                                    lrswt = false; // Only switch once
                                }
                            }
                            // Apply roll input
                            if (lrdirect == -1) {
                                u.Handb = true;
                                u.Left = true;
                            } else {
                                u.Handb = true;
                                u.Right = true;
                            }
                        }
                    }
                    
                    // Execute flip trick
                    if (uddirect != 0) {
                        if (flycnt > udstart) {
                            // Switch flip direction mid-trick
                            if (udswt) {
                                if (fix64.Abs(mad.Pzy - ozy) > 180) {
                                    if (uddirect == -1) {
                                        uddirect = 1;
                                    } else {
                                        uddirect = -1;
                                    }
                                    udswt = false; // Only switch once
                                }
                            }
                            // Apply flip input
                            if (uddirect == -1) {
                                u.Handb = true;
                                u.Down = true; // Backflip
                            } else {
                                u.Handb = true;
                                u.Up = true; // Frontflip
                                if (apunch > 0) {
                                    u.Down = true; // Punch boost
                                    apunch--;
                                }
                            }
                        }
                    }
                    
                    // Check if approaching ground (negative suspension velocity)
                    if (((mad.Scy[0] + mad.Scy[1] + mad.Scy[2] + mad.Scy[3]) * 100 / (car.Position.Y + 5)) < -saftey) {
                        // Reset landing flags
                        onceu = false;
                        onced = false;
                        oncel = false;
                        oncer = false;
                        lrcomp = false;
                        udcomp = false;
                        udbare = false;
                        lrbare = false;
                        trickfase = 2; // Enter landing phase
                        swat = 0;
                    }
                }
                
                // Landing phase - correct car orientation for safe landing
                if (trickfase == 2) {
                    if (swat == 0) {
                        // Determine which axes need correction
                        if (mad.Dcomp != 0 || mad.Ucomp != 0) {
                            udbare = true; // Pitch needs correction
                        }
                        if (mad.Lcomp != 0 || mad.Rcomp != 0) {
                            lrbare = true; // Roll needs correction
                        }
                        swat = 1;
                    }
                    
                    // Detect wheel touch
                    if (mad.Wtouch) {
                        if (swat == 1) {
                            swat = 2; // Wheels touched
                        }
                    } else {
                        if (swat == 2) {
                            // Bad landing: swap correction priorities
                            if ((mad.BadLanding) && (random.NextSFloat() > mustland)) {
                                if (udbare) {
                                    lrbare = true;
                                    udbare = false;
                                } else {
                                    if (lrbare) {
                                        udbare = true;
                                        lrbare = false;
                                    }
                                }
                            }
                            swat = 3;
                        }
                    }
                    
                    // Correct pitch if needed
                    if (udbare) {
                        // Calculate pitch relative to horizontal
                        var pzyr = (mad.Pzy + 90);
                        while (pzyr < 0) {
                            pzyr += 360;
                        }
                        while (pzyr > 180) {
                            pzyr -= 360;
                        }
                        pzyr = fix64.Abs(pzyr);
                        
                        // Check if roll is level
                        if (fix64.Abs(mad.Lcomp - mad.Rcomp) < 5) {
                            if (onced || onceu) {
                                udcomp = true; // Pitch correction complete
                            }
                        }
                        
                        // Correct based on which way car is pitched
                        if (mad.Dcomp > mad.Ucomp) {
                            // Nose down
                            if (mad.BadLanding) {
                                if (udcomp) {
                                    if (pzyr < 90) {
                                        u.Up = true;
                                    } else {
                                        u.Down = true;
                                    }
                                } else {
                                    if (!onced) {
                                        u.Down = true;
                                    }
                                }
                            } else {
                                if (udcomp) {
                                    if ((perfection) && (fix64.Abs(pzyr - 90) > 30)) {
                                        if (pzyr < 90) {
                                            u.Up = true;
                                        } else {
                                            u.Down = true;
                                        }
                                    }
                                } else {
                                    if (random.NextSFloat() > mustland) {
                                        u.Up = true;
                                    }
                                }
                                onced = true;
                            }
                        } else {
                            // Nose up
                            if (mad.BadLanding) {
                                if (udcomp) {
                                    if (pzyr < 90) {
                                        u.Up = true;
                                    } else {
                                        u.Down = true;
                                    }
                                } else {
                                    if (!onceu) {
                                        u.Up = true;
                                    }
                                }
                            } else {
                                if (udcomp) {
                                    if ((perfection) && (fix64.Abs(pzyr - 90) > 30)) {
                                        if (pzyr < 90) {
                                            u.Up = true;
                                        } else {
                                            u.Down = true;
                                        }
                                    }
                                } else {
                                    if (random.NextSFloat() > mustland) {
                                        u.Down = true;
                                    }
                                }
                                onceu = true;
                            }
                        }
                    }
                    
                    // Correct roll if needed
                    if (lrbare) {
                        // Calculate roll relative to horizontal
                        var pxyr = (mad.Pxy + 90);
                        if (u.Zyinv) {
                            pxyr += 180;
                        }
                        while (pxyr < 0) {
                            pxyr += 360;
                        }
                        while (pxyr > 180) {
                            pxyr -= 360;
                        }
                        pxyr = fix64.Abs(pxyr);
                        
                        // Check if roll is close to level
                        if (fix64.Abs(mad.Lcomp - mad.Rcomp) < 10) {
                            if (oncel || oncer) {
                                lrcomp = true; // Roll correction complete
                            }
                        }
                        
                        // Correct based on which way car is rolled
                        if (mad.Lcomp > mad.Rcomp) {
                            // Rolled left
                            if (mad.BadLanding) {
                                if (lrcomp) {
                                    if (pxyr > 90) {
                                        u.Left = true;
                                    } else {
                                        u.Right = true;
                                    }
                                } else {
                                    if (!oncel) {
                                        u.Left = true;
                                    }
                                }
                            } else {
                                if (lrcomp) {
                                    if ((perfection) && (fix64.Abs(pxyr - 90) > 30)) {
                                        if (pxyr > 90) {
                                            u.Left = true;
                                        } else {
                                            u.Right = true;
                                        }
                                    }
                                } else {
                                    if (random.NextSFloat() > mustland) {
                                        u.Right = true;
                                    }
                                }
                                oncel = true;
                            }
                        } else {
                            // Rolled right
                            if (mad.BadLanding) {
                                if (lrcomp) {
                                    if (pxyr > 90) {
                                        u.Left = true;
                                    } else {
                                        u.Right = true;
                                    }
                                } else {
                                    if (!oncer) {
                                        u.Right = true;
                                    }
                                }
                            } else {
                                if (lrcomp) {
                                    if ((perfection) && (fix64.Abs(pxyr - 90) > 30)) {
                                        if (pxyr > 90) {
                                            u.Left = true;
                                        } else {
                                            u.Right = true;
                                        }
                                    }
                                } else {
                                    if (random.NextSFloat() > mustland) {
                                        u.Left = true;
                                    }
                                }
                                oncer = true; // Mark that correction was attempted
                            }
                        }
                    }
                }
            }
        }
    } // End of RunAi method
} // End of ReLitAi class