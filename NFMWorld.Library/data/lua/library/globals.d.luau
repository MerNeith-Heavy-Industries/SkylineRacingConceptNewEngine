export type integer = typeof(setmetatable(
	{},
	{} :: integerMt
))

export type integerMt = {
	__add: ((integer, integer) -> integer) & ((integer, number) -> number) & ((number, integer) -> number),
	__sub: ((integer, integer) -> integer) & ((integer, number) -> number) & ((number, integer) -> number),
	__mul: ((integer, integer) -> integer) & ((integer, number) -> number) & ((number, integer) -> number),
	__div: ((integer, integer) -> integer) & ((integer, number) -> number) & ((number, integer) -> number),
	__mod: ((integer, integer) -> integer) & ((integer, number) -> number) & ((number, integer) -> number),
	__pow: ((integer, integer) -> integer) & ((integer, number) -> number) & ((number, integer) -> number),
	__unm: ((integer) -> integer),
	__tostring: ((integer) -> string),
	__eq: ((integer, integer) -> boolean) & ((integer, number) -> boolean) & ((number, integer) -> boolean),
	__lt: ((integer, integer) -> boolean) & ((integer, number) -> boolean) & ((number, integer) -> boolean),
	__le: ((integer, integer) -> boolean) & ((integer, number) -> boolean) & ((number, integer) -> boolean),
}

export type Array<T> = { [number]: T }

export type Stream = {
	write: ((Stream, string) -> nil)
}

-- TODO more lua module types!
declare io: {
	stderr: Stream
}

export type fixed64 = typeof(setmetatable(
	{} :: {
		--- The underlying raw long value cast to double
		raw: number
	},
	{} :: fixed64Mt
))

export type fixed64Mt = {
	__add: ((fixed64, fixed64) -> fixed64),
	__sub: ((fixed64, fixed64) -> fixed64),
	__mul: ((fixed64, fixed64) -> fixed64),
	__div: ((fixed64, fixed64) -> fixed64),
	__mod: ((fixed64, fixed64) -> fixed64),
	__unm: ((fixed64) -> fixed64),
	__tostring: ((fixed64) -> string),
	__eq: ((fixed64, fixed64) -> boolean),
	__lt: ((fixed64, fixed64) -> boolean),
	__le: ((fixed64, fixed64) -> boolean),
}

export type fixed64vector3 = typeof(setmetatable(
	{} :: {
		--- The underlying raw long value cast to double
		x: fixed64,
		y: fixed64,
		z: fixed64,
	},
	{} :: fixed64Vector3Mt
))

export type fixed64Vector3Mt = {
	__add: ((fixed64vector3, fixed64vector3) -> fixed64vector3) & ((fixed64vector3, number) -> fixed64vector3) & ((number, fixed64vector3) -> fixed64vector3),
	__sub: ((fixed64vector3, fixed64vector3) -> fixed64vector3) & ((fixed64vector3, number) -> fixed64vector3) & ((number, fixed64vector3) -> fixed64vector3),
	__mul: ((fixed64vector3, fixed64vector3) -> fixed64vector3) & ((fixed64vector3, number) -> fixed64vector3) & ((number, fixed64vector3) -> fixed64vector3),
	__div: ((fixed64vector3, fixed64vector3) -> fixed64vector3) & ((fixed64vector3, number) -> fixed64vector3) & ((number, fixed64vector3) -> fixed64vector3),
	__mod: ((fixed64vector3, fixed64vector3) -> fixed64vector3) & ((fixed64vector3, number) -> fixed64vector3) & ((number, fixed64vector3) -> fixed64vector3),
	__unm: ((fixed64vector3) -> fixed64vector3),
	__tostring: ((fixed64vector3) -> string),
	__eq: ((fixed64vector3, fixed64vector3) -> boolean) & ((fixed64vector3, number) -> boolean) & ((number, fixed64vector3) -> boolean),
	__lt: ((fixed64vector3, fixed64vector3) -> boolean) & ((fixed64vector3, number) -> boolean) & ((number, fixed64vector3) -> boolean),
	__le: ((fixed64vector3, fixed64vector3) -> boolean) & ((fixed64vector3, number) -> boolean) & ((number, fixed64vector3) -> boolean),
}

--- Creates a Fixed64 value.
---
--- Accepts a Lua number (double), a string (parsed via Fixed64.TryParse),
--- or an existing Fixed64 (returned as-is).
---
--- Cross-type arithmetic (Fixed64 + Number) is intentionally not supported —
--- convert both sides to the same type first.
declare function fixed64(value: number|string|fixed64): fixed64

--- Creates a Fixed64Vector3 value.
---
--- Each argument is converted to Fixed64 (accepts both Lua numbers and Fixed64).
declare function fixed64vector3(x: number|fixed64, y: number|fixed64, z: number|fixed64): fixed64vector3

declare fixed64vec3: {
	--- Returns a normalized (unit-length) copy of the vector.
	normalized: ((v: fixed64vector3) -> fixed64vector3),
	--- Returns the cross product of two vectors.
	cross: ((a: fixed64vector3, b: fixed64vector3) -> fixed64vector3),
	--- Returns the dot product of two vectors.
	dot: ((a: fixed64vector3, b: fixed64vector3) -> fixed64),
	--- Returns the Euclidean distance between two points.
	distance: ((a: fixed64vector3, b: fixed64vector3) -> fixed64),
	--- Returns the squared Euclidean distance between two points.
	sqrdistance: ((a: fixed64vector3, b: fixed64vector3) -> fixed64),
	--- Returns the magnitude (length) of the vector.
	magnitude: ((v: fixed64vector3) -> fixed64),
	--- Returns the squared magnitude (length) of the vector.
	sqrmagnitude: ((v: fixed64vector3) -> fixed64),
	--- Returns the component-wise maximum of two vectors.
	max: ((a: fixed64vector3, b: fixed64vector3) -> fixed64vector3),
	--- Returns the component-wise minimum of two vectors.
	min: ((a: fixed64vector3, b: fixed64vector3) -> fixed64vector3),
	--- Linearly interpolates between two vectors.
	lerp: ((a: fixed64vector3, b: fixed64vector3, t: fixed64) -> fixed64vector3),
	--- Returns the component-wise absolute value of the vector.
	abs: ((v: fixed64vector3) -> fixed64vector3),
	--- Returns the component-wise sign of the vector (1, -1, or 0 per component).
	sign: ((v: fixed64vector3) -> fixed64vector3),
}

export type f64angle = typeof(setmetatable(
	{} :: {
		--- Angle in radians
		rad: fixed64,
		--- Angle in degrees
		deg: fixed64,
	},
	{} :: f64angleMt
))

export type f64euler = typeof(setmetatable(
	{} :: {
		yaw: f64angle,
		pitch: f64angle,
		roll: f64angle,
	},
	{} :: f64eulerMt
))

export type f64angleMt = {
	__add: ((f64angle, f64angle) -> f64angle),
	__sub: ((f64angle, f64angle) -> f64angle),
	__mul: ((f64angle, f64angle) -> f64angle),
	__div: ((f64angle, f64angle) -> f64angle),
	__unm: ((f64angle) -> f64angle),
	__eq: ((f64angle, f64angle) -> boolean),
	__lt: ((f64angle, f64angle) -> boolean),
	__le: ((f64angle, f64angle) -> boolean),
}

export type f64eulerMt = {
	__add: ((f64euler, f64euler) -> f64euler),
	__sub: ((f64euler, f64euler) -> f64euler),
	__unm: ((f64euler) -> f64euler),
	__mul: ((f64euler, f64angle) -> f64euler),
	__div: ((f64euler, f64angle) -> f64euler),
	__eq: ((f64euler, f64euler) -> boolean),
	__lt: ((f64euler, f64euler) -> boolean),
	__le: ((f64euler, f64euler) -> boolean),
}

--- Creates an f64AngleSingle from degrees.
---
--- Accepts a Lua number, a Fixed64 (interpreted as degrees), a string,
--- or an existing f64angle (returned as-is).
---
--- Cross-type arithmetic (f64angle + Number) is intentionally not supported —
--- convert both sides to the same type first.
declare function f64angle(value: number|string|fixed64|f64angle): f64angle

--- Creates an f64Euler from three angles or numbers.
---
--- Each argument is converted to f64AngleSingle:
--- numbers/Fixed64 are interpreted as degrees; existing f64angle values pass through.
---
---@param yaw number|fixed64|f64angle
---@param pitch number|fixed64|f64angle
---@param roll number|fixed64|f64angle
---@return f64euler
declare function f64euler(yaw: number|fixed64|f64angle, pitch: number|fixed64|f64angle, roll: number|fixed64|f64angle): f64euler

declare f64anglelib: {
	--- Creates an f64AngleSingle from a radian value.
	from_radians: ((radians: fixed64) -> f64angle),
	--- Creates an f64AngleSingle from a degree value.
	from_degrees: ((degrees: fixed64) -> f64angle),
	--- Wraps the angle to the range [-180°, 180°] ([-π, π] rad).
	wrap: ((a: f64angle) -> f64angle),
	--- Wraps the angle to the range [0°, 360°) ([0, 2π) rad).
	wrap_positive: ((a: f64angle) -> f64angle),
	--- Returns the smaller of two angles.
	min: ((a: f64angle, b: f64angle) -> f64angle),
	--- Returns the larger of two angles.
	max: ((a: f64angle, b: f64angle) -> f64angle),
	--- Returns the angle's value in degrees.
	degrees: ((a: f64angle) -> fixed64),
	--- Returns the angle's value in radians.
	radians: ((a: f64angle) -> fixed64),
}

declare f64eulerlib: {
	--- Wraps each component (yaw, pitch, roll) to [-180°, 180°] ([-π, π] rad).
	wrap: ((e: f64euler) -> f64euler),
	--- Wraps each component (yaw, pitch, roll) to [0°, 360°) ([0, 2π) rad).
	wrap_positive: ((e: f64euler) -> f64euler),
}

declare f64math: {
	--- The largest representable fixed64 value.
	maxValue: fixed64,
	--- The smallest representable fixed64 value.
	minValue: fixed64,
	--- pi (π).
	pi: fixed64,
	--- Half of pi (π/2).
	halfpi: fixed64,
	--- Two times pi (2π).
	twopi: fixed64,
	--- Returns the sine of x (radians).
	sin: ((x: fixed64) -> fixed64),
	--- Returns the cosine of x (radians).
	cos: ((x: fixed64) -> fixed64),
	--- Returns the tangent of x (radians).
	tan: ((x: fixed64) -> fixed64),
	--- Returns the arc-sine of x (radians).
	asin: ((x: fixed64) -> fixed64),
	--- Returns the arc-cosine of x (radians).
	acos: ((x: fixed64) -> fixed64),
	--- Returns the arc-tangent of x (radians).
	atan: ((x: fixed64) -> fixed64),
	--- Returns the angle whose tangent is y / x (radians).
	atan2: ((y: fixed64, x: fixed64) -> fixed64),
	--- Returns the square root of x.
	sqrt: ((x: fixed64) -> fixed64),
	--- Returns b raised to the power e.
	pow: ((b: fixed64, e: fixed64) -> fixed64),
	--- Returns the natural logarithm of x.
	ln: ((x: fixed64) -> fixed64),
	--- Returns the base-2 logarithm of x.
	log2: ((x: fixed64) -> fixed64),
	--- Returns the absolute value of x.
	abs: ((x: fixed64) -> fixed64),
	--- Returns the largest integral value less than or equal to x.
	floor: ((x: fixed64) -> fixed64),
	--- Returns the smallest integral value greater than or equal to x.
	ceil: ((x: fixed64) -> fixed64),
	--- Rounds x to the nearest integral value (banker's rounding).
	round: ((x: fixed64) -> fixed64),
	--- Returns the smaller of two values.
	min: ((a: fixed64, b: fixed64) -> fixed64),
	--- Returns the larger of two values.
	max: ((a: fixed64, b: fixed64) -> fixed64),
	--- Clamps v to the inclusive range [min, max].
	clamp: ((v: fixed64, min: fixed64, max: fixed64) -> fixed64),
	--- Clamps v to the range [0, 1].
	clamp01: ((v: fixed64) -> fixed64),
	--- Returns the sign of x (-1, 0, or 1).
	sign: ((x: fixed64) -> fixed64),
	--- Linearly interpolates between a and b by t (0: a, 1: b).
	lerp: ((a: fixed64, b: fixed64, t: fixed64) -> fixed64),
	--- Returns sqrt(a^2 + b^2).
	hypot: ((a: fixed64, b: fixed64) -> fixed64),
	--- Converts degrees to radians.
	deg2rad: ((x: fixed64) -> fixed64),
	--- Converts radians to degrees.
	rad2deg: ((x: fixed64) -> fixed64),
}

export type Standing = {
	playerId: string,
	position: integer,
	finished: boolean,
}

export type RaceStandings = {
	[integer]: Standing,
}

declare Key: {
    -- Bit masks
    KeyCode  : number,
    Modifiers: number,

    None            : number,
    LButton         : number,
    RButton         : number,
    Cancel          : number,
    MButton         : number,
    XButton1        : number,
    XButton2        : number,
    Back            : number,
    Tab             : number,
    LineFeed        : number,
    Clear           : number,
    Return          : number,
    Enter           : number,
    ShiftKey        : number,
    ControlKey      : number,
    Menu            : number,
    Pause           : number,
    Capital         : number,
    CapsLock        : number,
    KanaMode        : number,
    HanguelMode     : number,
    HangulMode      : number,
    JunjaMode       : number,
    FinalMode       : number,
    HanjaMode       : number,
    KanjiMode       : number,
    Escape          : number,
    IMEConvert      : number,
    IMENonconvert   : number,
    IMEAccept       : number,
    IMEAceept       : number,
    IMEModeChange   : number,
    Space           : number,
    Prior           : number,
    PageUp          : number,
    Next            : number,
    PageDown        : number,
    End             : number,
    Home            : number,
    Left            : number,
    Up              : number,
    Right           : number,
    Down            : number,
    Select          : number,
    Print           : number,
    Execute         : number,
    Snapshot        : number,
    PrintScreen     : number,
    Insert          : number,
    Delete          : number,
    Help            : number,
    D0: number, D1: number, D2: number, D3: number, D4: number,
    D5: number, D6: number, D7: number, D8: number, D9: number,
    A: number, B: number, C: number, D: number, E: number,
    F: number, G: number, H: number, I: number, J: number,
    K: number, L: number, M: number, N: number, O: number,
    P: number, Q: number, R: number, S: number, T: number,
    U: number, V: number, W: number, X: number, Y: number,
    Z: number,
    LWin           : number,
    RWin           : number,
    Apps           : number,
    Sleep          : number,
    NumPad0        : number, NumPad1: number, NumPad2: number,
    NumPad3        : number, NumPad4: number, NumPad5: number,
    NumPad6        : number, NumPad7: number, NumPad8: number,
    NumPad9        : number,
    Multiply       : number,
    Add            : number,
    Separator      : number,
    Subtract       : number,
    Decimal        : number,
    Divide         : number,
    F1 : number, F2 : number, F3 : number, F4 : number,
    F5 : number, F6 : number, F7 : number, F8 : number,
    F9 : number, F10: number, F11: number, F12: number,
    F13: number, F14: number, F15: number, F16: number,
    F17: number, F18: number, F19: number, F20: number,
    F21: number, F22: number, F23: number, F24: number,
    NumLock        : number,
    Scroll         : number,
    LShiftKey      : number,
    RShiftKey      : number,
    LControlKey    : number,
    RControlKey    : number,
    LMenu          : number,
    RMenu          : number,
    BrowserBack    : number,
    BrowserForward : number,
    BrowserRefresh : number,
    BrowserStop    : number,
    BrowserSearch  : number,
    BrowserFavorites: number,
    BrowserHome    : number,
    VolumeMute     : number,
    VolumeDown     : number,
    VolumeUp       : number,
    MediaNextTrack : number,
    MediaPreviousTrack: number,
    MediaStop      : number,
    MediaPlayPause : number,
    LaunchMail     : number,
    SelectMedia    : number,
    LaunchApplication1: number,
    LaunchApplication2: number,
    OemSemicolon   : number,
    Oem1           : number,
    Oemplus        : number,
    Oemcomma       : number,
    OemMinus       : number,
    OemPeriod      : number,
    OemQuestion    : number,
    Oem2           : number,
    Oem3           : number,
    Oemtilde       : number,
    OemOpenBrackets: number,
    Oem4           : number,
    OemPipe        : number,
    Oem5           : number,
    OemCloseBrackets: number,
    Oem6           : number,
    Oem7           : number,
    OemQuotes      : number,
    Oem8           : number,
    Oem102         : number,
    OemBackslash   : number,
    ProcessKey     : number,
    Packet         : number,
    Attn           : number,
    Crsel          : number,
    Exsel          : number,
    EraseEof       : number,
    Play           : number,
    Zoom           : number,
    NoName         : number,
    Pa1            : number,
    OemClear       : number,

    -- Modifier flags
    Shift  : number,
    Control: number,
    Alt    : number,
}

declare UiLib: {
	createInstance: ((vtype: string, props: { [string]: any }) -> any),
	createTextInstance: ((text: string) -> any),
	appendChild: ((parent: any, child: any) -> nil),
	insertBefore: ((parent: any, child: any, before: any) -> nil),
	removeChild: ((parent: any, child: any) -> nil),
	setProperty: ((instance: any, key: string, value: any) -> nil),
	commitTextUpdate: ((textInstance: any, oldText: string, newText: string) -> nil),
}

declare extern type UDim with
	Offset: number
	Scale: number
	__add: ((self: UDim, other: UDim) -> UDim) & ((self: UDim, other: number) -> number)
	function __sub(self, other: UDim): UDim
	function __unm(self): UDim
end

declare extern type UDim2 with
	Height: UDim
	Width: UDim
	X: UDim
	Y: UDim
	function Lerp(self, goal: UDim2, alpha: number): UDim2
	function __add(self, other: UDim2): UDim2
	function __sub(self, other: UDim2): UDim2
	function __unm(self): UDim2
end

declare UDim: {
	new: ((Scale: number?, Offset: number?) -> UDim),
}

declare UDim2: {
	fromScale: ((xScale: number, yScale: number) -> UDim2),
	fromOffset: ((xOffset: number, yOffset: number) -> UDim2),
	new: ((x: UDim, y: UDim) -> UDim2) & ((xScale: number?, xOffset: number?, yScale: number?, yOffset: number?) -> UDim2),
}
