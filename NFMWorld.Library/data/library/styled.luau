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
            if isHover then
                outprops.style = merge(outprops.style, hover)
            end
            if isFocus then
                outprops.style = merge(outprops.style, focus)
            end

            if outprops.onmouseenter ~= nil then
                outprops.onmouseenter = function()
                    onHover()
                    ;(outprops.onmouseenter :: any)()
                end
            else
                outprops.onmouseenter = onHover
            end
            if outprops.onmouseleave ~= nil then
                outprops.onmouseleave = function()
                    onLeave()
                    ;(outprops.onmouseleave :: any)()
                end
            else
                outprops.onmouseleave = onLeave
            end
            if outprops.onfocus ~= nil then
                outprops.onfocus = function()
                    onFocus()
                    ;(outprops.onfocus :: any)()
                end
            else
                outprops.onfocus = onFocus
            end
            if outprops.onblur ~= nil then
                outprops.onblur = function()
                    onBlur()
                    ;(outprops.onblur :: any)()
                end
            else
                outprops.onblur = onBlur
            end

            return h(vtype, (outprops :: any) :: P)
        end
    end
end

return styled
