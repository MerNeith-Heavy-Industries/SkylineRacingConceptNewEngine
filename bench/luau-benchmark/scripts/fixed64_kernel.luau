-- Fixed64 kernel benchmark — distilled from el_stupido.luau (data/ais/el_stupido.luau).
--
-- Heavy fixed64 arithmetic + f64math C# interop, representative of the AI's per-frame
-- math (distance scans, angle diffs, trig). VM-agnostic: it only uses the `fixed64(...)`
-- constructor and the `f64math.*` globals, both of which the host registers in either VM.
--
-- Contract: this chunk returns a single `run(iterations, nodeCount)` function that returns
-- (cpuSeconds, checksum). `cpuSeconds` is an os.clock() delta measured entirely in Lua so
-- both VMs are compared on equal footing.

local function pyo(x1, x2, z1, z2)
    local dx = x1 - x2
    local dz = z1 - z2
    return (dx * dx) + (dz * dz)
end

local function angleDiff(a, b)
    local diff = a - b
    diff = ((diff + fixed64(180)) % fixed64(360)) - fixed64(180)
    if diff < fixed64(-180) then
        diff = diff + fixed64(360)
    end
    return diff
end

local function run(iterations, nodeCount)
    iterations = iterations or 2000
    nodeCount = nodeCount or 500

    -- Node positions (distilled from stage.nodes[].position.x/.z).
    local xs = {}
    local zs = {}
    for i = 1, nodeCount do
        xs[i] = fixed64((i * 37) % 1000)
        zs[i] = fixed64((i * 53) % 1000)
    end

    local acc = fixed64(0)
    local t0 = os.clock()
    for n = 1, iterations do
        local targetX = fixed64(n % 997)
        local targetZ = fixed64(n % 991)

        -- getClearanceInDirection-style min-distance scan.
        local minDistSq = f64math.maxValue
        for i = 1, nodeCount do
            local d = pyo(targetX, xs[i], targetZ, zs[i])
            if d < minDistSq then
                minDistSq = d
            end
        end
        local dist = f64math.sqrt(minDistSq)

        -- detectAndAvoidObstacles-style trig math.
        local heading = fixed64(n % 360)
        local left = angleDiff(heading, fixed64(90))
        local rad = left * f64math.pi / fixed64(180)
        acc = acc + f64math.sin(rad) * dist
        acc = acc + f64math.cos(rad) * dist
    end
    local t1 = os.clock()

    -- os.clock delta + a checksum so the work can't be elided by the compiler.
    return t1 - t0, tostring(acc)
end

return run
