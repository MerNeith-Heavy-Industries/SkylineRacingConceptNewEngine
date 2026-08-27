-- Sx — fine-grained reactive UI framework for UiLib.
--
-- Public entry point. Exposes the reactive primitives, the hyperscript `x`, the
-- renderer, and the flow components, all under one module. The game / benchmark
-- injects the end-of-frame scheduler via `Sx.setScheduler(UiLib.defer)`.

local signals = require('./signals')
local h = require('./h')
local dom = require('./dom')
local styled = require('./styled')

local Sx = {}

-- reactive primitives
Sx.createSignal = signals.createSignal
Sx.createMemo = signals.createMemo
Sx.createEffect = signals.createEffect
Sx.createRoot = signals.createRoot
Sx.batch = signals.batch
Sx.untrack = signals.untrack
Sx.onCleanup = signals.onCleanup
Sx.onMount = signals.onMount
Sx.getOwner = signals.getOwner
Sx.runWithOwner = signals.runWithOwner
Sx.setScheduler = signals.setScheduler
Sx.flushSync = signals.flushSync

-- hyperscript
Sx.x = h.x

-- renderer + flow components
Sx.render = dom.render
Sx.Fragment = dom.Fragment
Sx.Show = dom.Show
Sx.Switch = dom.Switch
Sx.Match = dom.Match
Sx.For = dom.For
Sx.Index = dom.Index

-- styling
Sx.styled = styled

return Sx
