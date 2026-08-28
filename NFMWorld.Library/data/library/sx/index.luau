-- Sx — fine-grained reactive UI framework for UiLib.
--
-- Public entry point. Exposes the reactive primitives, the hyperscript `x`, the
-- renderer, and the flow components, all under one module. The game / benchmark
-- injects the end-of-frame scheduler via `Sx.setScheduler(UiLib.defer)`.

local signals = require('./signals')
local h = require('./h')
local dom = require('./dom')
local styled = require('./styled')
local t = require('./types')

export type NodeDescriptor = t.NodeDescriptor
export type Component<P> = t.Component<P>

export type Accessor<T> = signals.Accessor<T>
export type Signal<T> = signals.Signal<T>

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
Sx.View = h.View
Sx.Text = h.Text
Sx.Image = h.Image
Sx.TextInput = h.TextInput
export type ViewProps = h.ViewProps
export type TextProps = h.TextProps
export type ImageProps = h.ImageProps
export type TextInputProps = h.TextInputProps
export type Styles = h.Styles
export type TextStyles = h.TextStyles
export type TextInputStyles = h.TextInputStyles
export type Signalish<T> = h.Signalish<T>

type function Unwrap(t)
    local function unwrapOne(ty)
        if ty:is("function") then
            local params = ty:parameters()
            local rets = ty:returns()

            -- treat nil as "zero-length pack"
            local pcount = params and #params or (params and params.head and #params.head) or 0
            local rhead = rets and (rets.head or rets) or nil

            if pcount == 0 and rhead and #rhead == 1 then
                return rhead[1]
            end
        end
        return ty
    end

    if t:is("union") then
        local results = {}
        for _, part in t:components() do
            table.insert(results, unwrapOne(part))
        end
        return types.unionof(table.unpack(results))
    else
        return unwrapOne(t)
    end
end

function Sx.read<T>(v: Signalish<T>): Unwrap<T>
    if type(v) == "function" then
        return v()
    end
    return v
end

-- renderer + flow components
Sx.render = dom.render
Sx.Fragment = dom.Fragment :: ({}) -> t.NodeDescriptor
Sx.Show = dom.Show
Sx.Switch = dom.Switch
Sx.Match = dom.Match
Sx.For = dom.For
Sx.Index = dom.Index

-- styling
Sx.styled = styled

return Sx
