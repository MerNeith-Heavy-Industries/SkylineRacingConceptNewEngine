-- Sx — public type declarations (mirrors data/library/declarations/exports.d.luau style).

type Accessor<T> = () -> T

type Signal<T> = { get: Accessor<T>, set: (T) -> () }

type NodeDescriptor = {
	type: any,
	props: { [string]: any },
	children: { any },
}

type Component<P> = (P) -> NodeDescriptor

type HostElementTag = "view" | "text" | "image" | "textinput"

declare Sx: {
	-- reactive primitives
	createSignal: <T>(initial: T?) -> (Accessor<T>, (T) -> ()),
	createMemo: <T>(fn: () -> T, initial: T?) -> Accessor<T>,
	createEffect: (fn: () -> ()) -> () -> (),
	createRoot: <T>(fn: ((() -> ())) -> T) -> T,
	batch: <T>(fn: () -> T) -> T,
	untrack: <T>(fn: () -> T) -> T,
	onCleanup: (fn: () -> ()) -> (),
	onMount: (fn: () -> ()) -> (),
	getOwner: () -> any?,
	runWithOwner: <T>(owner: any, fn: () -> T) -> T,
	setScheduler: (fn: ((() -> ())) -> ()) -> (),
	flushSync: () -> (),

	-- hyperscript
	x: <P>(vtype: string | Component<P> | ((any) -> NodeDescriptor)) -> ((P) -> NodeDescriptor),

	-- renderer + flow
	render: (rootNode: NodeDescriptor) -> any,
	Show: (props: any) -> NodeDescriptor,
	Switch: (props: any) -> NodeDescriptor,
	Match: (props: any) -> NodeDescriptor,
	For: (props: any) -> NodeDescriptor,
	Index: (props: any) -> NodeDescriptor,
}

return nil
