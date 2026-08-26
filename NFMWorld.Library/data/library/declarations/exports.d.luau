-- AI
declare AI: AiContext;

-- Gamemode
declare GM: GamemodeContext;

declare function OnServerEvent(type: string, payload: { [any]: any })

export type RaceResults = {
	duration: number,
	gamemodeId: string,
	standings: RaceStandings,
}

declare function OnServerRaceFinished(results: RaceResults)

declare function OnKeyPressed(key: integer)

declare function OnKeyReleased(key: integer)

declare function OnKeyTyped(key: string)

-- Server Gamemode
declare SGM: ServerGamemodeContext;

-- UI
declare UiLib: {
	--- Creates a new view that the root React node can be mounted onto.
	createRoot: (() -> View),
	--- Creates a new Node with the type and properties.
	createInstance: ((vtype: string, props: { [string]: any }) -> Node),
	--- Creates a new TextNode with the given text.
	createTextInstance: ((text: string) -> TextNode),
	--- Appends a child to a parent node.
	appendChild: ((parent: Node, child: Node) -> nil),
	--- Inserts a child before another child.
	insertBefore: ((parent: Node, child: Node, before: Node) -> nil),
	--- Removes a child from its parent.
	removeChild: ((parent: Node, child: Node) -> nil),
	--- Sets a property on a node.
	setProperty: ((instance: Node, key: string, value: any) -> nil),
	--- Commits a text update to a TextNode.
	commitTextUpdate: ((textInstance: TextNode, oldText: string, newText: string) -> nil),
	--- Sets the active root node. This is the root node that gets rendered on screen.
	setActiveRoot: ((root: View) -> nil),
	--- Calls a method registered on the C# bridge of the current phase.
	call: ((method: string, payload: any) -> nil),
	--- Registers a callback for an event from the C# bridge.
	onEvent: (<T>(event: string, callback: ((payload: T) -> ())) -> () -> ()),
	--- Defers a function to be called at the end of the current frame.
	defer: (fn: () -> ()) -> (),
}

-- Gamemode config

---Defines a property.
export type ConfigProperty = {
	name: string,
	type: 'string' | 'number' | 'boolean',
	description: string?,
	options: { [number]: { label: string, value: string | number | boolean } }?,
}

export type Config = {
	name: string,
	description: string,
	properties: {ConfigProperty},
}

---Defines the config for a gamemode
declare function DefineGamemodeConfig(config: Config)