local React = require('./react')

local function render(node: React.VNode)
    local root = UiLib.createRoot()
    React.render(node, root)

    UiLib.setActiveRoot(root)
end

return {render = render}