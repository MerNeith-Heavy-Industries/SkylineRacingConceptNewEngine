# Sx UI framework — a beginner's guide

Sx is the fine-grained reactive UI framework used by the NFM World Lua UI. If you've never
used React, SolidJS, "signals", or web dev in general, this guide is for you. It builds up
from zero and explains *why* things are the way they are, not just what to type.

If you already know SolidJS/dom-expressions, skim the **Quick reference** at the end and the
**Gotchas** section; the rest will be familiar.

---

## 1. The problem this framework solves

Older UI code in this project (the "preact-luau" engine) worked like this: every time *any*
piece of data changed, it re-ran the whole screen's code and diffed the old vs new result to
figure out what to redraw. That works, but re-running and diffing an entire tree **every
frame** is expensive — and in a racing game the HUD changes every frame (speed, lap times,
damage bars).

Sx flips the model: instead of "recompute everything and diff", it says

> **Only recompute the tiny piece that actually depends on the value that changed.**

So a per-frame speed update ends up touching *one* number on *one* text node, not a whole
tree. That's why this framework exists.

---

## 2. The mental model: boxes with wires

Think of a **signal** as a box that holds a value. The box has two handles:

```lua
local speed, setSpeed = Sx.createSignal(0)
--   ↑ read handle       ↑ write handle
```

- **Read** it by calling the first handle: `speed()` → returns `0`.
- **Write** it by calling the second handle: `setSpeed(50)` → puts `50` in the box.

Now the interesting part: **wires**. When your code *reads* a signal while it is inside a
reactive scope (more on that below), Sx lays a wire from the box to that scope. When
something *writes* the box, Sx yanks the wire and the scope re-runs — just that scope, not
everything.

That's the entire framework, honestly. Everything else is convenience built on top of this
one idea: **reads subscribe, writes notify.**

---

## 3. Reactive scopes: `createEffect`

A reactive scope is a chunk of code that automatically re-runs when any signal it reads
changes:

```lua
Sx.createEffect(function()
    print("speed is now", speed())
end)
```

- The first time it runs, it reads `speed()`, so it subscribes to `speed`.
- Every time `setSpeed(x)` is called with a different value, this exact function runs again.
- If the effect reads two signals, it re-runs when *either* changes.
- If it reads no signals, it runs exactly once and never again.

That's it. `createEffect` is your "when this changes, do that" tool.

---

## 4. Components run ONCE — this is the big mental shift

In the old framework, a "component" was a function that got **re-run** every time its data
changed. In Sx, **a component function runs exactly once**. It describes the screen, and the
dynamic parts of the screen are themselves reactive scopes.

Compare:

```lua
-- ❌ Old mindset: the function re-runs when the value changes.
local function Speed()
    local s = speed()          -- read once, baked in forever
    return x("text") { ("%d"):format(s) }
end

-- ✅ Sx mindset: the function runs once; the changing part is a function.
local function Speed()
    return x("text") { function()
        return ("%d"):format(speed())
    end }
end
```

In the second version, `Speed()` builds the text node once, and the inner `function()` is
the reactive scope that re-runs when `speed` changes. Sx updates just that text.

**Rule of thumb:** if you want a value on screen to react to a signal, wrap the expression
in a function. If it's a static label, just write it.

---

## 5. Building UI: the `x` helper

There's no HTML. You build the UI with a function called `x` (short for "element"). It takes
a tag name, then a table with the element's properties and children:

```lua
x("view") {                         -- a container
    style = { flexDirection = "column", gap = 8 },
    x("text") { "Hello" },          -- a text element
    x("view") {                     -- nested container
        x("text") { "World" },
    },
}
```

Available host elements:

| Tag | What it is |
|---|---|
| `"view"` | A box/container. Use it for layout (flex, spacing, backgrounds). |
| `"text"` | A piece of text. Its children are the text content. |
| `"image"` | An image (`src`, `scale`). |
| `"textinput"` | A text entry box (`value`, `placeholder`, `onchange`, `onsubmit`). |

**How the table is read:**

- **Named entries** (string keys like `style = ...`) are *properties*.
- **Positional entries** (just values in order) are *children*.

```lua
x("view") {
    style = { padding = 8 },        -- a property (named)
    x("text") { "child one" },      -- a child (positional)
    x("text") { "child two" },      -- another child
}
```

You can also define your own reusable "components" — just a function that returns an `x(...)`
tree:

```lua
local function SpeedReadout()
    return x("view") {
        x("text") { function() return ("%d"):format(speed()) end },
        x("text") { "KM/H" },
    }
end

-- use it:
x("view") { x(SpeedReadout) {} }
```

---

## 6. Text that updates

To make text react to a signal, pass a **function** as the text child:

```lua
x("text") { function()
    return ("%d"):format(speed())
end }
```

- The function is a reactive scope. It re-runs when `speed` changes.
- Sx updates the existing text node in place — one tiny change, nothing else redraws.

This is the single most important pattern in the framework.

---

## 7. Lists: `For` and `Index`

To render a list, you use `For`. Its `each` is a function returning the array, and its child
is a function that gets each item:

```lua
local items = { { name = "Skyline" }, { name = "Silvia" } }

x("view") {
    x(Sx.For) {
        each = function() return items end,
        function(item, index)
            local it = item()
            return x("text") { it.name }
        end,
    },
}
```

**Read this carefully:**

- `each` is a function returning the array (so the list re-renders if the array signal changes).
- The child function receives `item` — but `item` is itself a *getter*. Call `item()` to get
  the actual row data. (In Sx, list rows are kept separate so one row can update without
  rebuilding the others.)
- `For` keys rows by the item's identity, so if your list changes, rows that didn't change
  are reused, not rebuilt.

`Index` is the same but matches rows by *position* instead of by identity. Use `For` when
your rows have stable identities (e.g. car objects); use `Index` when you only care about
position.

> If a row's content needs to react to a signal, use the accessor pattern inside the row,
> just like with text: `x("text") { function() return item().name end }`.

---

## 8. Conditionals: `Show`, `Switch`/`Match`

### Show (show this, or show that)

```lua
x(Sx.Show) {
    when = function() return isLoggedIn() end,
    fallback = x("text") { "Sign in" },
    x("text") { "Welcome back" },
}
```

- When `when()` is true, the children mount.
- When it's false, the `fallback` mounts instead (optional).
- The children/fallback mount and unmount automatically as the condition flips.

### Switch / Match (pick the first true case)

```lua
x(Sx.Switch) {
    x(Sx.Match) { when = function() return route() == "menu" end, x(MainMenu) {} },
    x(Sx.Match) { when = function() return route() == "garage" end, x(Garage) {} },
    x(Sx.Match) { when = function() return true end, x(MainMenu) {} }, -- fallback
}
```

`Switch` looks at each `Match` in order and mounts the children of the **first** one whose
`when()` is true. The trailing `when = true` is the "default" case.

### Fragment (group without a wrapper)

If a component needs to return *several* top-level children with no wrapping box, use
`Fragment`:

```lua
return x(Sx.Fragment) {
    x("text") { "first" },
    x("text") { "second" },
}
```

---

## 9. Events: the `on` prefix

Anything that starts with `on` is an **event handler** (a function called when something
happens). They're wired up once and stay put:

```lua
x("view") {
    onmousedown = function(evt)
        UiLib.call("navigate", { page = "garage" })
    end,
    x("text") { "Garage" },
}
```

Available events: `onmousedown`, `onmouseup`, `onmousedrag`, `onmousescroll`, `onmousemove`,
`onmouseenter`, `onmouseleave`, `onkeytype`, `onkeydown`, `onkeyup`, `onfocus`, `onblur`,
`onanimationframebegan`, plus `onsubmit`/`onchange` on text inputs.

> **Gotcha:** an `on`-prefixed property is a handler. Any **other** property whose value is a
> function is treated as a *reactive value* (section 6 applies). So if you want a plain
> callback that isn't an event, name it with `on` anyway (e.g. `onClose`).

---

## 10. Styles: `styled`

Inline styles are just tables:

```lua
x("view") { style = { width = "100%", height = "100%", flexDirection = "column" } }
```

For reusable styled components, use `styled`:

```lua
local MenuItem = styled("view") {
    padding = 14,
    backgroundColor = "rgba(60,60,60,0.12)",
    borderRadius = 8,
    hover = {
        backgroundColor = "rgba(79,195,247,0.24)",
    },
}

-- use it:
x(MenuItem) {
    onmousedown = function() print("clicked") end,
    x("text") { "Label" },
}
```

- `styled(tag)(baseStyle)` returns a component you use with `x(...)`.
- A `hover` block is applied automatically while the cursor is over it (and Sx only re-sets
  that one node's style on hover — it doesn't redraw anything else).
- Caller styles can still override via a `style` prop if you pass one through.

---

## 11. Derived values: `createMemo`

A memo is a cached, derived value that recomputes only when the signals it reads change:

```lua
local speedKmh = Sx.createMemo(function()
    return math.floor(speed() * 1.4 * 21.0 * 60.0 * 60.0 / 100000.0 + 0.5)
end)

x("text") { function() return ("%d"):format(speedKmh()) end }
```

- Like a signal, you read it with `speedKmh()`.
- Unlike `createEffect`, a memo **doesn't run eagerly** — it only computes when something
  reads it, and only re-computes when its inputs changed.
- Use memos to factor out repeated calculations or derived state so you don't recompute the
  same thing in five places.

---

## 12. Lifecycle: `onMount`, `onCleanup`, `createRoot`

These are for when a component or effect starts and stops.

```lua
local function Page()
    -- Subscribe to a game event when mounted; unsubscribe when unmounted.
    Sx.createEffect(function()
        local unsub = UiLib.onEvent("main-menu:account", function(data)
            setAccount(data)
        end)
        Sx.onCleanup(unsub)   -- runs when this scope is torn down
    end)
    ...
end
```

- `Sx.onCleanup(fn)` — registers a teardown for the current reactive scope (runs on unmount
  or when the effect is disposed).
- `Sx.onMount(fn)` — runs `fn` once when the component mounts.
- `Sx.createRoot(fn)` — creates a disposable "root" scope; the dispose function it hands you
  tears down everything inside. You rarely need this directly (components get their own root
  automatically), but it's how disposal works under the hood.

---

## 13. Grouping writes: `batch`

If you write several signals at once, wrap them in `batch` so Sx only does **one** update
pass instead of one per write:

```lua
Sx.batch(function()
    setPosition(2)
    setTotal(6)
    setStateText("Lap 2")
end)
```

On its own this is a small optimization, but it's good practice when updating multiple values
that belong together.

---

## 14. Talking to the game: `UiLib.onEvent` and `UiLib.call`

The Lua UI talks to the C# game through two functions:

- **C# → Lua (incoming data):** `UiLib.onEvent(eventName, handler)` — register a handler
  that's called when the game pushes data. It returns an unregister function (call it on
  cleanup, see section 12).

  ```lua
  UiLib.onEvent("race:hudState", function(data)
      setSpeed(data.speed)
      setPower(data.power)
      setDamage(data.damage)
  end)
  ```

- **Lua → C# (outgoing requests):** `UiLib.call(methodName, payloadTable)` — ask the game to
  do something.

  ```lua
  UiLib.call("navigate", { page = "garage" })
  ```

Event names follow `"phase:thing"` (e.g. `main-menu:account`, `race:hudState`,
`garage:collections`). Your phase's bridge tells you which events exist and what payloads they
carry.

---

## 15. Putting it together: a router and a page

The app is entered through `data/uis/router.luau`, which subscribes to navigation and renders
the current page:

```lua
local Sx = require("../library/sx/index")
local x = Sx.x
local MainMenu = require("./routes/mainmenu")

local function Router()
    local route, setRoute = Sx.createSignal("main-menu")

    Sx.createEffect(function()
        local unsub = UiLib.onEvent("nfmw:navigate", function(page)
            setRoute(page)
        end)
        Sx.onCleanup(unsub)
    end)

    return x(Sx.Switch) {
        x(Sx.Match) { when = function() return route() == "main-menu" end, x(MainMenu) {} },
        x(Sx.Match) { when = function() return true end, x(MainMenu) {} }, -- fallback
    }
end

Sx.render(x(Router) {})
```

A page is just a function returning `x(...)`, wired to its phase's events:

```lua
local Sx = require("../../library/sx/index")
local x = Sx.x

local function MainMenu()
    local account, setAccount = Sx.createSignal(nil)

    Sx.createEffect(function()
        local unsub = UiLib.onEvent("main-menu:account", function(data)
            setAccount(data)
        end)
        Sx.onCleanup(unsub)
    end)

    return x("view") {
        style = { flexDirection = "column", alignItems = "center", justifyContent = "center" },
        x("text") {
            style = { fontSize = 48, fontStyle = "bold" },
            "NFM WORLD",
        },
        x(Sx.Show) {
            when = function()
                local a = account()
                return a ~= nil and a.isLoggedIn
            end,
            x("text") { function()
                return ("Welcome, %s"):format(account().name)
            end },
        },
    }
end

return MainMenu
```

Read the whole flow: `Router` builds a signal for the page; the effect subscribes to
navigation; the `Switch` mounts the matching page. Each page subscribes to its own data and
builds its tree once, with reactive bits inside functions.

---

## 16. Gotchas (worth reading twice)

1. **Reactive text must be a function.** `x("text") { value }` sets it once; to react, use
   `x("text") { function() return value() end }`.

2. **Non-`on` function props are reactive.** `onmousedown = fn` is a handler; `color = fn`
   would be treated as a reactive value. Name plain callbacks with `on`.

3. **`For` rows give you getters.** Inside a `For` child, `item` is a getter — call `item()`.

4. **`and/or` is a trap.** `x and f() or y` returns `y` when `f()` returns `false`/`nil`.
   Write explicit `if`s when the value can be falsy.

5. **We don't re-run components.** Components run once. If something isn't updating, you're
   probably reading a signal outside a function instead of inside one.

6. **`%d` wants exact integers.** `("%d%%"):format(0.8 * 100)` throws; use
   `math.floor(0.8 * 100 + 0.5)` first.

7. **Lists rebuild when the array changes.** If you pass a brand-new array to `each` every
   time, `For` rebuilds. Keep the array reference stable unless the list really changed
   (e.g. keep it in a signal/memo, don't allocate a new one in a reactive scope).

8. **Unsubscribe on unmount.** If you `UiLib.onEvent(...)` inside an effect, pass the returned
   unregister to `Sx.onCleanup` so a navigated-away page doesn't keep receiving events.

---

## 17. Glossary (web terms → plain words)

| Term | Plain meaning |
|---|---|
| Signal | A box that holds a value, with a reader and a writer. Reading subscribes; writing notifies. |
| Reactive scope | A chunk of code (an effect, a memo, a `function()` child) that re-runs when a signal it reads changes. |
| Memo | A cached derived value that recomputes lazily when its inputs change. |
| Component | A function that returns an `x(...)` tree. It runs once; dynamic parts are functions. |
| Hyperscript / `x` | The way you write UI in Lua (like HTML, but as function calls). |
| Prop / property | A named entry in an element's table (`style`, `onmousedown`, ...). |
| Child | A positional entry in an element's table (nested elements or text). |
| Mount / unmount | A node appearing in / being removed from the tree. |
| Cleanup / dispose | Code that runs when a scope is torn down (unsubscribe, remove listeners). |
| Batch | Grouping several writes so Sx does one update pass. |
| HUD | Head-Up Display — the in-race overlay (speed, laps, position). |
| Phase / bridge | A game screen (main menu, garage, race) and the C# code that feeds it data. |

---

## Quick reference

```lua
local Sx = require("../../library/sx/index")
local x = Sx.x
local styled = Sx.styled

-- state
local value, setValue = Sx.createSignal(0)
local doubled = Sx.createMemo(function() return value() * 2 end)

-- side effects / lifecycle
Sx.createEffect(function() ... end)        -- re-runs when deps change
Sx.onCleanup(fn)                            -- teardown for current scope
Sx.onMount(fn)                              -- run once on mount
Sx.batch(function() ... end)                -- group writes

-- tree
x("view")  { style = {...}, x("text") { "hi" } }
x(MyComponent) { someProp = 1, x("text") { "child" } }

-- flow
x(Sx.Show)    { when = fn, fallback = ..., children }
x(Sx.Switch)  { x(Sx.Match) { when = fn, children }, ... }
x(Sx.For)     { each = fn, function(item, index) return ... end }
x(Sx.Fragment){ child1, child2 }

-- events (once) vs reactive props (functions)
onmousedown = function(evt) ... end
color       = function() return selected() and "#4fc3f7" or "#fff" end

-- game bridge
UiLib.onEvent("phase:event", function(data) ... end)  -- returns unregister
UiLib.call("method", { ... })

-- entry point
Sx.render(x(Router) {})
```
