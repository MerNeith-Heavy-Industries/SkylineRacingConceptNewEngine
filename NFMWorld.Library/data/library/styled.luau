--!strict

-- Styled components for Luau

local React = require("./react")
local useState = React.useState
local useEffect = React.useEffect
local useCallback = React.useCallback
local h = React.h
local View = React.View

local function merge<T>(a: T, b: T): T
    if a == nil then return b end
    if b == nil then return a end
    if type(a) == "table" and type(b) == "table" then
        local c = {}
        for k, v in pairs(a) do
            c[k] = merge((a :: any)[k], v)
        end
        for k, v in pairs(b) do
            c[k] = merge((b :: any)[k], v)
        end
        return c
    end
    return b
end

type function rawget(tbl, key)
    local function find(t): type?
        if t:is("table") then
            for k, v in t:properties() do
                if k == key then
                    if v.read ~= v.write then
                        error("mismatched read/write types found for the property")
                    end
                    return v.read
                end
            end
            return nil
        elseif t:is("intersection") then
            local results = {}
            for _, part in t:components() do
                local result = find(part)
                if result ~= nil then
                    table.insert(results, result)
                end
            end
            if #results == 0 then
                return nil
            elseif #results == 1 then
                return results[1]
            else
                return types.intersectionof(table.unpack(results))
            end
        elseif t:is("union") then
            local results = {}
            for _, part in t:components() do
                local result = find(part)
                if result == nil then
                    return nil
                end
                table.insert(results, result)
            end
            if #results == 0 then
                return nil
            elseif #results == 1 then
                return results[1]
            else
                return types.unionof(table.unpack(results))
            end
        end
        return nil
    end

    local result = find(tbl)
    if result == nil then
        error("key not found!")
    end
    return result
end

type Style<T> = rawget<T, 'style'>

type function WithVariants(styleT)
    local result = types.newtable()

    local function collect(t)
        if t:is("table") then
            for k, v in t:properties() do
                result:setproperty(k, v.read)
            end
        elseif t:is("union") then
            for _, part in t:components() do
                collect(part)
            end
        elseif t:is("intersection") then
            for _, part in t:components() do
                collect(part)
            end
        end
        -- silently ignore nil / other branches (e.g. from an optional union)
    end

    collect(styleT)

    local optionalStyles = types.optional(result) -- based on the flat table, not raw styleT
    result:setproperty(types.singleton("hover"), optionalStyles)
    result:setproperty(types.singleton("focus"), optionalStyles)

    result:setindexer(types.string, types.never) -- reject unlisted string keys

    return result
end

type StyledInput<P> = WithVariants<Style<P>>

--- Example:
---
---     local StyledView = styled(View){
---         flexDirection = "row",
---         alignItems = "stretch",
---         justifyContent = "center",
---         width = 100,
---         height = 100
---         hover = {
---             backgroundColor = "#00ff00"
---         },
---         focus = {
---             backgroundColor = "#0000ff"
---         }
---     }
local function styled<P>(vtype: React.StatelessFunctionalComponent<P>): ((StyledInput<P>) -> React.StatelessFunctionalComponent<P>)
    return function(input: StyledInput<P>): React.StatelessFunctionalComponent<P>
        local input1 = (input :: any) :: { hover: any, focus: any, [string]: any }

        local hover = input1.hover
        local focus = input1.focus

        local plain: { [string]: any } = {}
        for k, v in pairs((input :: any)) do
            if k ~= "hover" and k ~= "focus" then
                plain[k] = v
            end
        end

        return function(props: P): React.VNode
            local isHover, setIsHover = useState(false)
            local isFocus, setIsFocus = useState(false)

            useEffect(function()
                return function()
                    setIsHover(false)
                    setIsFocus(false)
                end
            end, {})

            local onHover = useCallback(function() setIsHover(true) end, {})
            local onLeave = useCallback(function() setIsHover(false) end, {})
            local onFocus = useCallback(function() setIsFocus(true) end, {})
            local onBlur = useCallback(function() setIsFocus(false) end, {})

            local outprops: { [string]: any } = {}

            for k, v in pairs((props :: any)) do
                outprops[k] = merge(outprops[k], v)
            end

            outprops.style = merge(outprops.style, plain)
            if isHover and hover ~= nil then
                outprops.style = merge(outprops.style, hover)
            end
            if isFocus and focus ~= nil then
                outprops.style = merge(outprops.style, focus)
            end

            -- Only wire hover/focus listeners when the corresponding variant
            -- actually exists. Wiring them unconditionally made EVERY styled
            -- component (even those with no hover/focus style) fire
            -- setIsHover/setIsFocus on cursor enter/leave, forcing a full tree
            -- re-render on every hover transition and steadily degrading the
            -- frame rate the more the user hovers.
            --
            -- Capture the user's own handlers first so the wrappers chain to
            -- the ORIGINAL function instead of recursively calling themselves
            -- (the old code re-read `outprops.onmouseenter` after overwriting
            -- it, which tail-call-looped when a styled component was also given
            -- its own enter/leave/focus/blur handler).
            local userEnter = outprops.onmouseenter
            local userLeave = outprops.onmouseleave
            local userFocus = outprops.onfocus
            local userBlur = outprops.onblur

            if hover ~= nil then
                if userEnter ~= nil then
                    outprops.onmouseenter = function()
                        onHover()
                        userEnter()
                    end
                else
                    outprops.onmouseenter = onHover
                end
                if userLeave ~= nil then
                    outprops.onmouseleave = function()
                        onLeave()
                        userLeave()
                    end
                else
                    outprops.onmouseleave = onLeave
                end
            end
            if focus ~= nil then
                if userFocus ~= nil then
                    outprops.onfocus = function()
                        onFocus()
                        userFocus()
                    end
                else
                    outprops.onfocus = onFocus
                end
                if userBlur ~= nil then
                    outprops.onblur = function()
                        onBlur()
                        userBlur()
                    end
                else
                    outprops.onblur = onBlur
                end
            end

            return h(vtype, (outprops :: any) :: P)
        end
    end
end

return styled
