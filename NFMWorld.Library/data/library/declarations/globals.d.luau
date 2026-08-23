export type integer = number;

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
	__eq: ((fixed64vector3, fixed64vector3) -> boolean),
	__lt: ((fixed64vector3, fixed64vector3) -> boolean),
	__le: ((fixed64vector3, fixed64vector3) -> boolean),
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

--declare extern type UDim with
--	Offset: number
--	Scale: number
--	__add: ((self: UDim, other: UDim) -> UDim) & ((self: UDim, other: number) -> number)
--	function __sub(self, other: UDim): UDim
--	function __unm(self): UDim
--end
--
--declare extern type UDim2 with
--	Height: UDim
--	Width: UDim
--	X: UDim
--	Y: UDim
--	function Lerp(self, goal: UDim2, alpha: number): UDim2
--	function __add(self, other: UDim2): UDim2
--	function __sub(self, other: UDim2): UDim2
--	function __unm(self): UDim2
--end
--
--declare UDim: {
--	new: ((Scale: number?, Offset: number?) -> UDim),
--}
--
--declare UDim2: {
--	fromScale: ((xScale: number, yScale: number) -> UDim2),
--	fromOffset: ((xOffset: number, yOffset: number) -> UDim2),
--	new: ((x: UDim, y: UDim) -> UDim2) & ((xScale: number?, xOffset: number?, yScale: number?, yOffset: number?) -> UDim2),
--}
