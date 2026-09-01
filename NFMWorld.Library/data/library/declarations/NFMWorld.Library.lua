---@class BaseMouseDragEvent : System.IEquatable_BaseMouseDragEvent
---@field dragStart LuaVector2
---@field position LuaVector2
---@field button integer
---@field buttons MouseButtons
---@field ctrlKey boolean
---@field metaKey boolean
---@field shiftKey boolean

BaseMouseDragEvent = {}


---@class BaseMouseEvent : System.IEquatable_BaseMouseEvent
---@field position LuaVector2
---@field button MouseButton
---@field buttons MouseButtons
---@field ctrlKey boolean
---@field altKey boolean
---@field shiftKey boolean

BaseMouseEvent = {}


---@class BaseMouseMoveEvent : System.IEquatable_BaseMouseMoveEvent
---@field position LuaVector2
---@field buttons MouseButtons
---@field ctrlKey boolean
---@field altKey boolean
---@field shiftKey boolean

BaseMouseMoveEvent = {}


---@class BaseMouseWheelEvent : System.IEquatable_BaseMouseWheelEvent
---@field delta LuaVector3
---@field position LuaVector2
---@field buttons MouseButtons
---@field ctrlKey boolean
---@field metaKey boolean
---@field shiftKey boolean

BaseMouseWheelEvent = {}


---@class KeyboardEvent : System.IEquatable_KeyboardEvent
---@field keyChar Key
---@field keyCode Key
---@field keys Keys

KeyboardEvent = {}


---@class KeyboardTypingEvent : System.IEquatable_KeyboardTypingEvent
---@field keyChar integer

KeyboardTypingEvent = {}


---@class MouseDragEvent : System.IEquatable_MouseDragEvent
---@field dragStart LuaVector2
---@field relativeDragStart LuaVector2
---@field position LuaVector2
---@field button integer
---@field buttons MouseButtons
---@field ctrlKey boolean
---@field metaKey boolean
---@field shiftKey boolean
---@field relativePosition LuaVector2

MouseDragEvent = {}


---@class MouseEvent : System.IEquatable_MouseEvent
---@field position LuaVector2
---@field button MouseButton
---@field buttons MouseButtons
---@field ctrlKey boolean
---@field metaKey boolean
---@field shiftKey boolean
---@field relativePosition LuaVector2

MouseEvent = {}


---@class MouseMoveEvent : System.IEquatable_MouseMoveEvent
---@field position LuaVector2
---@field buttons MouseButtons
---@field ctrlKey boolean
---@field metaKey boolean
---@field shiftKey boolean
---@field relativePosition LuaVector2

MouseMoveEvent = {}


---@class MouseWheelEvent : System.IEquatable_MouseWheelEvent
---@field delta LuaVector3
---@field position LuaVector2
---@field buttons MouseButtons
---@field ctrlKey boolean
---@field metaKey boolean
---@field shiftKey boolean
---@field relativePosition LuaVector2

MouseWheelEvent = {}


---@class HudStateData
---@field speed number
---@field power number
---@field damage number
---@field lap integer
---@field totalLaps integer
---@field lapTime integer
---@field position integer
---@field totalRacers integer
---@field stateText string
---@field lapDiffMs integer|nil
---@field lastLapDiffMs integer|nil
---@field chkDiffMs integer|nil
---@field lastChkDiffMs integer|nil
---@field countdownTimer integer
---@field stateTextEndsAt number|nil

HudStateData = {}


---@class Key : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

Key = {}

---@type Key
Key.returnKey = nil
---@type Key
Key.endKey = nil

---@class Keys : System.IEquatable_Keys, System.IComparable_Keys
---@field none boolean
---@field lButton boolean
---@field rButton boolean
---@field cancel boolean
---@field mButton boolean
---@field xButton1 boolean
---@field xButton2 boolean
---@field back boolean
---@field tab boolean
---@field lineFeed boolean
---@field clear boolean
---@field returnKey boolean
---@field enter boolean
---@field shiftKey boolean
---@field controlKey boolean
---@field menu boolean
---@field pause boolean
---@field capital boolean
---@field capsLock boolean
---@field kanaMode boolean
---@field hanguelMode boolean
---@field hangulMode boolean
---@field junjaMode boolean
---@field finalMode boolean
---@field hanjaMode boolean
---@field kanjiMode boolean
---@field escape boolean
---@field iMEConvert boolean
---@field iMENonconvert boolean
---@field iMEAccept boolean
---@field iMEAceept boolean
---@field iMEModeChange boolean
---@field space boolean
---@field prior boolean
---@field pageUp boolean
---@field next boolean
---@field pageDown boolean
---@field endKey boolean
---@field home boolean
---@field left boolean
---@field up boolean
---@field right boolean
---@field down boolean
---@field select boolean
---@field print boolean
---@field execute boolean
---@field snapshot boolean
---@field printScreen boolean
---@field insert boolean
---@field delete boolean
---@field help boolean
---@field d0 boolean
---@field d1 boolean
---@field d2 boolean
---@field d3 boolean
---@field d4 boolean
---@field d5 boolean
---@field d6 boolean
---@field d7 boolean
---@field d8 boolean
---@field d9 boolean
---@field a boolean
---@field b boolean
---@field c boolean
---@field d boolean
---@field e boolean
---@field f boolean
---@field g boolean
---@field h boolean
---@field i boolean
---@field j boolean
---@field k boolean
---@field l boolean
---@field m boolean
---@field n boolean
---@field o boolean
---@field p boolean
---@field q boolean
---@field r boolean
---@field s boolean
---@field t boolean
---@field u boolean
---@field v boolean
---@field w boolean
---@field x boolean
---@field y boolean
---@field z boolean
---@field lWin boolean
---@field rWin boolean
---@field apps boolean
---@field sleep boolean
---@field numPad0 boolean
---@field numPad1 boolean
---@field numPad2 boolean
---@field numPad3 boolean
---@field numPad4 boolean
---@field numPad5 boolean
---@field numPad6 boolean
---@field numPad7 boolean
---@field numPad8 boolean
---@field numPad9 boolean
---@field multiply boolean
---@field add boolean
---@field separator boolean
---@field subtract boolean
---@field decimal boolean
---@field divide boolean
---@field f1 boolean
---@field f2 boolean
---@field f3 boolean
---@field f4 boolean
---@field f5 boolean
---@field f6 boolean
---@field f7 boolean
---@field f8 boolean
---@field f9 boolean
---@field f10 boolean
---@field f11 boolean
---@field f12 boolean
---@field f13 boolean
---@field f14 boolean
---@field f15 boolean
---@field f16 boolean
---@field f17 boolean
---@field f18 boolean
---@field f19 boolean
---@field f20 boolean
---@field f21 boolean
---@field f22 boolean
---@field f23 boolean
---@field f24 boolean
---@field numLock boolean
---@field scroll boolean
---@field lShiftKey boolean
---@field rShiftKey boolean
---@field lControlKey boolean
---@field rControlKey boolean
---@field lMenu boolean
---@field rMenu boolean
---@field browserBack boolean
---@field browserForward boolean
---@field browserRefresh boolean
---@field browserStop boolean
---@field browserSearch boolean
---@field browserFavorites boolean
---@field browserHome boolean
---@field volumeMute boolean
---@field volumeDown boolean
---@field volumeUp boolean
---@field mediaNextTrack boolean
---@field mediaPreviousTrack boolean
---@field mediaStop boolean
---@field mediaPlayPause boolean
---@field launchMail boolean
---@field selectMedia boolean
---@field launchApplication1 boolean
---@field launchApplication2 boolean
---@field oemSemicolon boolean
---@field oem1 boolean
---@field oemplus boolean
---@field oemcomma boolean
---@field oemMinus boolean
---@field oemPeriod boolean
---@field oemQuestion boolean
---@field oem2 boolean
---@field oem3 boolean
---@field oemtilde boolean
---@field oemOpenBrackets boolean
---@field oem4 boolean
---@field oemPipe boolean
---@field oem5 boolean
---@field oemCloseBrackets boolean
---@field oem6 boolean
---@field oem7 boolean
---@field oemQuotes boolean
---@field oem8 boolean
---@field oem102 boolean
---@field oemBackslash boolean
---@field processKey boolean
---@field packet boolean
---@field attn boolean
---@field crsel boolean
---@field exsel boolean
---@field eraseEof boolean
---@field play boolean
---@field zoom boolean
---@field noName boolean
---@field pa1 boolean
---@field oemClear boolean

Keys = {}


---@class MouseButton : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

MouseButton = {}


---@class MouseButtons : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

MouseButtons = {}


---@class Component : Node, NFMWorld.Reactor.IAnimationCallback
---@field visualChildren { [integer|number]: Node }
---@field canHaveChildren boolean
---@field name string
---@field isFocusable boolean
---@field layoutMarginPosition LuaVector2
---@field layoutMarginSize LuaVector2
---@field layoutBorderPosition LuaVector2
---@field layoutBorderSize LuaVector2
---@field layoutPaddingPosition LuaVector2
---@field layoutPaddingSize LuaVector2
---@field layoutContentPosition LuaVector2
---@field layoutContentSize LuaVector2
---@field layoutMargin LuaVector2
---@field layoutPadding LuaVector2
---@field layoutBorder LuaVector2
---@field layoutWidth number
---@field layoutHeight number
---@field layoutX number
---@field layoutY number
---@field layoutDirection Direction
---@field hadOverflow boolean
---@field layoutMarginTop number
---@field layoutMarginBottom number
---@field layoutMarginLeft number
---@field layoutMarginRight number
---@field layoutPaddingTop number
---@field layoutPaddingBottom number
---@field layoutPaddingLeft number
---@field layoutPaddingRight number
---@field layoutBorderTop number
---@field layoutBorderBottom number
---@field layoutBorderLeft number
---@field layoutBorderRight number
---@field hasNewLayout boolean
---@field isDirty boolean
---@field isReferenceBaseline boolean
---@field scrollLeft number
---@field scrollTop number
---@field scrollableWidth number
---@field scrollableHeight number
---@field isClipping boolean
---@field isDisplayed boolean
---@field visualParent Node|nil
---@field addChild fun(self: Component, child: Node)
---@field insertAt fun(self: Component, index: integer, child: Node)
---@field removeAt fun(self: Component, index: integer)
---@field scrollIntoView fun(self: Component)
---@field focus fun(self: Component)
---@field blur fun(self: Component)

Component = {}


---@class Direction : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

Direction = {}


---@class Node
---@field visualParent Node|nil
---@field visualChildren { [integer|number]: Node }

Node = {}


---@class TextInput : Component, NFMWorld.Reactor.IAnimationCallback
---@field placeholder string
---@field text string
---@field visualChildren { [integer|number]: Node }
---@field canHaveChildren boolean
---@field name string
---@field isFocusable boolean
---@field layoutMarginPosition LuaVector2
---@field layoutMarginSize LuaVector2
---@field layoutBorderPosition LuaVector2
---@field layoutBorderSize LuaVector2
---@field layoutPaddingPosition LuaVector2
---@field layoutPaddingSize LuaVector2
---@field layoutContentPosition LuaVector2
---@field layoutContentSize LuaVector2
---@field layoutMargin LuaVector2
---@field layoutPadding LuaVector2
---@field layoutBorder LuaVector2
---@field layoutWidth number
---@field layoutHeight number
---@field layoutX number
---@field layoutY number
---@field layoutDirection Direction
---@field hadOverflow boolean
---@field layoutMarginTop number
---@field layoutMarginBottom number
---@field layoutMarginLeft number
---@field layoutMarginRight number
---@field layoutPaddingTop number
---@field layoutPaddingBottom number
---@field layoutPaddingLeft number
---@field layoutPaddingRight number
---@field layoutBorderTop number
---@field layoutBorderBottom number
---@field layoutBorderLeft number
---@field layoutBorderRight number
---@field hasNewLayout boolean
---@field isDirty boolean
---@field isReferenceBaseline boolean
---@field scrollLeft number
---@field scrollTop number
---@field scrollableWidth number
---@field scrollableHeight number
---@field isClipping boolean
---@field isDisplayed boolean
---@field visualParent Node|nil
---@field addChild fun(self: TextInput, child: Node)
---@field insertAt fun(self: TextInput, index: integer, child: Node)
---@field removeAt fun(self: TextInput, index: integer)
---@field scrollIntoView fun(self: TextInput)
---@field focus fun(self: TextInput)
---@field blur fun(self: TextInput)

TextInput = {}


---@class TextNode : Node, NFMWorld.Reactor.IReceivesTextInvalidation, NFMWorld.Reactor.IRichTextLeaf, NFMWorld.Reactor.IRichTextElement
---@field visualChildren { [integer|number]: Node }
---@field text string
---@field visualParent Node|nil

TextNode = {}


---@class View : Component, NFMWorld.Reactor.IAnimationCallback
---@field visualChildren { [integer|number]: Node }
---@field canHaveChildren boolean
---@field name string
---@field isFocusable boolean
---@field layoutMarginPosition LuaVector2
---@field layoutMarginSize LuaVector2
---@field layoutBorderPosition LuaVector2
---@field layoutBorderSize LuaVector2
---@field layoutPaddingPosition LuaVector2
---@field layoutPaddingSize LuaVector2
---@field layoutContentPosition LuaVector2
---@field layoutContentSize LuaVector2
---@field layoutMargin LuaVector2
---@field layoutPadding LuaVector2
---@field layoutBorder LuaVector2
---@field layoutWidth number
---@field layoutHeight number
---@field layoutX number
---@field layoutY number
---@field layoutDirection Direction
---@field hadOverflow boolean
---@field layoutMarginTop number
---@field layoutMarginBottom number
---@field layoutMarginLeft number
---@field layoutMarginRight number
---@field layoutPaddingTop number
---@field layoutPaddingBottom number
---@field layoutPaddingLeft number
---@field layoutPaddingRight number
---@field layoutBorderTop number
---@field layoutBorderBottom number
---@field layoutBorderLeft number
---@field layoutBorderRight number
---@field hasNewLayout boolean
---@field isDirty boolean
---@field isReferenceBaseline boolean
---@field scrollLeft number
---@field scrollTop number
---@field scrollableWidth number
---@field scrollableHeight number
---@field isClipping boolean
---@field isDisplayed boolean
---@field visualParent Node|nil
---@field addChild fun(self: View, child: Node)
---@field insertAt fun(self: View, index: integer, child: Node)
---@field removeAt fun(self: View, index: integer)
---@field scrollIntoView fun(self: View)
---@field focus fun(self: View)
---@field blur fun(self: View)

View = {}


---@class AiNodeKind : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

AiNodeKind = {}


---@class BaseAi
---@field runAi fun(self: BaseAi)

BaseAi = {}


---@class AiContext
---@field players { [integer|number]: ClientSidePlayer }
---@field player ClientSidePlayer
---@field stage BackendStage
---@field config table|nil

AiContext = {}


---@class BackendCar : BackendGameObject, NFMWorldLibrary.ITransform
---@field groundAt integer
---@field maxRadius integer
---@field wheelAngle f64euler
---@field turningWheelAngle f64euler
---@field wheels { [integer|number]: Rad3dWheelDef }
---@field carPhysics CarPhysics
---@field control Control
---@field currentCheckpoint integer
---@field currentLap integer
---@field totalCheckpoint integer
---@field lastCheckpointNode integer
---@field placement integer
---@field rad Rad3d
---@field stats CarStats
---@field wasted boolean
---@field player ClientSidePlayerInfo
---@field children { [integer|number]: BackendGameObject }
---@field parent BackendGameObject|nil
---@field position fixed64vector3
---@field rotation f64euler
---@field drive fun(self: BackendCar, stage: BackendStage)

BackendCar = {}


---@class BackendGameObject : NFMWorldLibrary.ITransform
---@field children { [integer|number]: BackendGameObject }
---@field parent BackendGameObject|nil
---@field position fixed64vector3
---@field rotation f64euler

BackendGameObject = {}


---@class BackendStage
---@field pieces { [integer|number]: BackendGameObject }
---@field nodes { [integer|number]: StageObject }
---@field checkpoints { [integer|number]: StageObject }
---@field fixHoops { [integer|number]: StageObject }
---@field nlaps integer
---@field name string
---@field path string
---@field stageLoader StageLoader

BackendStage = {}


---@class PhysicsController
---@field gameTick fun(self: PhysicsController)

PhysicsController = {}


---@class IGamemodeContext

IGamemodeContext = {}


---@class StageObject : BackendGameObject, NFMWorldLibrary.IAiNode, NFMWorldLibrary.ICollidable, NFMWorldLibrary.ITransform
---@field originalPlacement PiecePlacement
---@field rad Rad3d
---@field nodeKind AiNodeKind
---@field isSpecial boolean
---@field boxes { [integer|number]: Rad3dBoxDef }
---@field maxRadius integer
---@field fileName string
---@field children { [integer|number]: BackendGameObject }
---@field parent BackendGameObject|nil
---@field position fixed64vector3
---@field rotation f64euler

StageObject = {}


---@class WallCollision : BackendGameObject, NFMWorldLibrary.ICollidable, NFMWorldLibrary.ITransform
---@field boxes { [integer|number]: Rad3dBoxDef }
---@field maxRadius integer
---@field children { [integer|number]: BackendGameObject }
---@field parent BackendGameObject|nil
---@field position fixed64vector3
---@field rotation f64euler

WallCollision = {}


---@class CarPhysics
---@field halted boolean
---@field btab boolean
---@field capcnt integer
---@field capsized boolean
---@field caught { [integer|number]: boolean }
---@field stat CarStats
---@field cn integer
---@field cntdest integer
---@field cntouch integer
---@field collidingWithClientPlayer boolean
---@field crank { [integer|number]: integer }
---@field lcrank { [integer|number]: integer }
---@field cxz fixed64
---@field staticCameraXz fixed64
---@field dcnt integer
---@field dcomp fixed64
---@field lcomp fixed64
---@field wasted boolean
---@field dominate { [integer|number]: boolean }
---@field drag fixed64
---@field fixes integer
---@field forca fixed64
---@field ftab boolean
---@field turnXz fixed64
---@field gtouch boolean
---@field hitmag integer
---@field im integer
---@field lastcolido integer
---@field loop integer
---@field lxz fixed64
---@field mtouch boolean
---@field mxz fixed64
---@field numRoofDamage integer
---@field newcar boolean
---@field newedcar integer
---@field nmlt integer
---@field nofocus boolean
---@field outshakedam integer
---@field pd boolean
---@field pl boolean
---@field pmlt integer
---@field point integer
---@field power fixed64
---@field powerup fixed64
---@field pr boolean
---@field pu boolean
---@field pushed boolean
---@field pxy fixed64
---@field pzy fixed64
---@field rcomp fixed64
---@field rtab boolean
---@field scx { [integer|number]: fixed64 }
---@field scy { [integer|number]: fixed64 }
---@field scz { [integer|number]: fixed64 }
---@field shakedam integer
---@field skid integer
---@field speed fixed64
---@field roofDamage integer
---@field surfCount integer
---@field surfing boolean
---@field tilt fixed64
---@field totalStuntXy fixed64
---@field totalStuntXz fixed64
---@field totalStuntZy fixed64
---@field tcnt integer
---@field txz fixed64
---@field ucomp fixed64
---@field wtouch boolean
---@field xtpower integer

CarPhysics = {}


---@class CarStats : System.IEquatable_CarStats
---@field swits Int3
---@field acelf fixed64vector3
---@field handb integer
---@field airs fixed64
---@field airc integer
---@field grip fixed64
---@field bounce fixed64
---@field simag fixed64
---@field moment fixed64
---@field comprad fixed64
---@field push fixed64
---@field revpush fixed64
---@field lift integer
---@field revlift integer
---@field powerloss integer
---@field flipy integer
---@field msquash integer
---@field clrad integer
---@field dammult fixed64
---@field maxmag integer
---@field dishandle fixed64
---@field outdam fixed64
---@field name string
---@field enginsignature integer
---@field turnradius integer
---@field roadgrip fixed64|nil
---@field offroadgrip fixed64|nil
---@field offtrackgrip fixed64|nil
---@field turn fixed64

CarStats = {}


---@class CloudsInstruction : EnvironmentInstruction, System.IEquatable_EnvironmentInstruction, System.IEquatable_CloudsInstruction
---@field clouds { [integer|number]: integer }

CloudsInstruction = {}


---@class Collection : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

Collection = {}


---@class Control
---@field arrace boolean
---@field chatup integer
---@field down boolean
---@field enter boolean
---@field exit boolean
---@field handb boolean
---@field multion integer
---@field mutem boolean
---@field mutes boolean
---@field radar boolean
---@field right boolean
---@field up boolean
---@field left boolean
---@field lookback integer
---@field wall integer
---@field zyinv boolean

Control = {}


---@class EnvironmentInstruction : System.IEquatable_EnvironmentInstruction

EnvironmentInstruction = {}


---@class FogInstruction : EnvironmentInstruction, System.IEquatable_EnvironmentInstruction, System.IEquatable_FogInstruction
---@field color Color3

FogInstruction = {}


---@class FrameTrace

FrameTrace = {}


---@param message string
function FrameTrace.addMessage(message) end

---@class ClientSidePlayer
---@field info ClientSidePlayerInfo
---@field index integer
---@field car BackendCar|nil
---@field bot BaseAi|nil
---@field isFake boolean

ClientSidePlayer = {}


---@class ClientSidePlayerInfo
---@field playerName string
---@field carName string
---@field color Color3
---@field isBot boolean
---@field isClientPlayer boolean

ClientSidePlayerInfo = {}


---@class LuaClientCarContext
---@field castsShadow boolean
---@field getsShadowed boolean|nil
---@field alphaOverride number|nil
---@field glow boolean|nil
---@field finish boolean|nil

LuaClientCarContext = {}


---@class LuaClientContext
---@field resetCheckpointGlow fun(self: LuaClientContext)
---@field updateCheckpointGlow fun(self: LuaClientContext, currentCheckpoint: integer, isFinish: boolean)
---@field getClientCarCallbacks fun(self: LuaClientContext, car: BackendCar): LuaClientCarContext

LuaClientContext = {}


---@class GamemodeContext
---@field stage BackendStage
---@field players { [integer|number]: ClientSidePlayer }
---@field clientPlayer ClientSidePlayer
---@field hudState HudStateData
---@field physics PhysicsController
---@field config table|nil
---@field client LuaClientContext
---@field countdownInterval integer
---@field createCar fun(self: GamemodeContext, playerIndex: integer, x: fixed64, z: fixed64): BackendCar
---@field calculatePositions fun(self: GamemodeContext)
---@field handleCheckPoint fun(self: GamemodeContext, car: BackendCar): boolean
---@field handleFixHoops fun(self: GamemodeContext, car: BackendCar): boolean
---@field clientReset fun(self: GamemodeContext)
---@field sendEvent fun(self: GamemodeContext, type: string, payload: table)
---@field updateHudAndSounds fun(self: GamemodeContext, car: BackendCar)
---@field removeFakePlayers fun(self: GamemodeContext)
---@field clonePlayer fun(self: GamemodeContext, basedOnPlayer: ClientSidePlayer): ClientSidePlayer

GamemodeContext = {}


---@class ServerGamemodeContext
---@field currentStage BackendStage
---@field players { [integer|number]: ServerSidePlayerInfo }
---@field config table|nil
---@field countdownInterval integer
---@field getPlayerPosition fun(self: ServerGamemodeContext, playerId: string): fixed64vector3|nil
---@field broadcastEvent fun(self: ServerGamemodeContext, type: string, payload: table)
---@field finishRace fun(self: ServerGamemodeContext, standings: RaceStandings)

ServerGamemodeContext = {}


---@class TimeTrial
---@field hasGhost boolean
---@field begin fun(self: TimeTrial, car: BackendCar)
---@field applyGhost fun(self: TimeTrial, ghostCar: BackendCar, tick: integer)
---@field getSplitDiff fun(self: TimeTrial, splitIndex: integer): number|nil
---@field getLastSplitDiff fun(self: TimeTrial): number|nil
---@field getLapDiff fun(self: TimeTrial, lapIndex: integer): number|nil
---@field recordSplit fun(self: TimeTrial, splitTime: number)
---@field getLapTime fun(self: TimeTrial, lapIndex: integer): number|nil
---@field getLastSplitTime fun(self: TimeTrial): number|nil
---@field getBestLastSplitTime fun(self: TimeTrial): number|nil
---@field record fun(self: TimeTrial, car: BackendCar)
---@field save fun(self: TimeTrial)

TimeTrial = {}


---Creates a new TimeTrial
---@param stage BackendStage
---@return TimeTrial
function TimeTrial.new(stage) end

---@class GroundInstruction : EnvironmentInstruction, System.IEquatable_EnvironmentInstruction, System.IEquatable_GroundInstruction
---@field color Color3

GroundInstruction = {}


---@class HierarchyGroup : System.IEquatable_HierarchyGroup
---@field name string
---@field pieces { [integer|number]: PiecePlacement }
---@field coordinateKeys { [integer|number]: string }

HierarchyGroup = {}


---@class Int3 : System.IEquatable_Int3
---@field x integer
---@field y integer
---@field z integer

Int3 = {}


---@class ServerSidePlayerInfo
---@field id string
---@field playerName string
---@field carName string
---@field color Color3

ServerSidePlayerInfo = {}


---@class PiecePlacement : System.IEquatable_PiecePlacement
---@field type PiecePlacementType
---@field object Rad3d
---@field position fixed64vector3
---@field rotation f64euler
---@field nodeKind AiNodeKind|nil
---@field isSpecial boolean
---@field isWall boolean

PiecePlacement = {}


---@class PiecePlacementType : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

PiecePlacementType = {}


---@class PolysInstruction : EnvironmentInstruction, System.IEquatable_EnvironmentInstruction, System.IEquatable_PolysInstruction
---@field color Color3

PolysInstruction = {}


---@class AttachmentLineDirection : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

AttachmentLineDirection = {}


---@class LineType : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

LineType = {}


---@class PolyType : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

PolyType = {}


---@class Rad3d
---@field maxRadius integer
---@field colors { [integer|number]: Color3 }
---@field stats CarStats
---@field wheels { [integer|number]: Rad3dWheelDef }
---@field rims Rad3dRimsDef|nil
---@field boxes { [integer|number]: Rad3dBoxDef }
---@field polys { [integer|number]: Rad3dPoly }
---@field castsShadow boolean
---@field atp { [integer|number]: LuaVector2 }
---@field fileName string
---@field atLines { [integer|number]: Rad3dAttachmentLine }|nil

Rad3d = {}


---@class Rad3dAttachmentLine : System.IEquatable_Rad3dAttachmentLine
---@field direction AttachmentLineDirection
---@field offset fixed64

Rad3dAttachmentLine = {}


---@class Rad3dBoxDef : System.IEquatable_Rad3dBoxDef
---@field xy integer
---@field zy integer
---@field radius fixed64vector3
---@field translation fixed64vector3
---@field surfaceType SurfaceType
---@field damage integer
---@field notWall boolean
---@field color Color3
---@field tractionMultiplier fixed64|nil

Rad3dBoxDef = {}


---@class Rad3dPoly : System.IEquatable_Rad3dPoly
---@field color Color3
---@field colNum integer|nil
---@field polyType PolyType
---@field lineType LineType|nil

Rad3dPoly = {}


---@class Rad3dRimsDef : System.IEquatable_Rad3dRimsDef
---@field color Color3
---@field size number
---@field depth number

Rad3dRimsDef = {}


---@class Rad3dWheelDef : System.IEquatable_Rad3dWheelDef
---@field position fixed64vector3
---@field rotates integer
---@field width fixed64
---@field height fixed64

Rad3dWheelDef = {}


---@class SkyInstruction : EnvironmentInstruction, System.IEquatable_EnvironmentInstruction, System.IEquatable_SkyInstruction
---@field color Color3

SkyInstruction = {}


---@class SnapInstruction : EnvironmentInstruction, System.IEquatable_EnvironmentInstruction, System.IEquatable_SnapInstruction
---@field color Color3

SnapInstruction = {}


---@class StageLoader
---@field path string
---@field nlaps integer
---@field musicPath string
---@field remasteredMusicPath string
---@field musicFreqMul number
---@field musicTempoMul number
---@field name string
---@field indexOffset integer
---@field sx integer
---@field sz integer
---@field ncx integer
---@field ncz integer
---@field cloudCoverage number|nil
---@field fogDensity integer|nil
---@field fadeFrom integer|nil
---@field lightsOn boolean
---@field drawMountains boolean
---@field mountainSeed integer|nil
---@field mountainCoverage number|nil
---@field lightDirection LuaVector3|nil
---@field pieces { [integer|number]: PiecePlacement }
---@field walls { [integer|number]: Rad3dBoxDef }
---@field maxr integer
---@field maxl integer
---@field maxt integer
---@field maxb integer
---@field environmentInstructions { [integer|number]: EnvironmentInstruction }
---@field drawPolys boolean
---@field drawClouds boolean

StageLoader = {}


---@class StageWall : System.IEquatable_StageWall
---@field direction WallDirection
---@field count integer
---@field position integer
---@field offset integer

StageWall = {}


---@class SurfaceType : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

SurfaceType = {}


---@class TextureInstruction : EnvironmentInstruction, System.IEquatable_EnvironmentInstruction, System.IEquatable_TextureInstruction
---@field texture { [integer|number]: integer }

TextureInstruction = {}


---@class Color3 : System.IEquatable_Color3
---@field r integer
---@field g integer
---@field b integer

Color3 = {}


---@class DeterministicRandom
---@field next fun(self: DeterministicRandom): integer
---@field nextBetween fun(self: DeterministicRandom, min: integer, max: integer): integer
---@field nextf64 fun(self: DeterministicRandom): fixed64

DeterministicRandom = {}


---Creates a new DeterministicRandom
---@param value fixed64
---@return DeterministicRandom
function DeterministicRandom.new(value) end

---@class Stopwatch
---@field isRunning boolean
---@field elapsed number
---@field elapsedMilliseconds integer
---@field elapsedMicroseconds integer
---@field stop fun(self: Stopwatch)
---@field start fun(self: Stopwatch)
---@field restart fun(self: Stopwatch)
---@field reset fun(self: Stopwatch)

Stopwatch = {}


---Creates a new Stopwatch
---@return Stopwatch
function Stopwatch.new() end

---@return Stopwatch
function Stopwatch.startNew() end

---@class LuaVector2 : System.IEquatable_LuaVector2
---@field x number
---@field y number

LuaVector2 = {}


---@class LuaVector3 : System.IEquatable_LuaVector3
---@field x number
---@field y number
---@field z number

LuaVector3 = {}


---@class WallDirection : System.Enum, System.IComparable, System.IConvertible, System.ISpanFormattable, System.IFormattable

WallDirection = {}


