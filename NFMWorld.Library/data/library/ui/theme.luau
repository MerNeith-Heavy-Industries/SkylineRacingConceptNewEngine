--!strict
-- Shared UI theme / color palette.
--
-- Single source of truth for the colors used across components and routes. Import it
-- wherever you need a color instead of hardcoding hex/rgba strings:
--
--   local theme = require("../../library/ui/theme")
--   ... color = theme.accent ...

local theme = {
	-- Accent (primary)
	accent = "#4fc3f7",
	accentHover = "#7dd8f9",
	accentSoft = "rgba(79,195,247,0.18)",
	accentSoftStrong = "rgba(79,195,247,0.32)",
	accentBorder = "rgba(79,195,247,0.35)",

	-- Danger
	danger = "#ff5252",
	dangerSoft = "rgba(255,82,82,0.15)",
	dangerSoftStrong = "rgba(255,82,82,0.28)",
	dangerBorder = "rgba(255,82,82,0.35)",

	-- Warning (orange)
	warning = "#ffb300",
	warningSoft = "rgba(255,152,0,0.25)",
	warningSoftStrong = "rgba(255,152,0,0.35)",
	warningBorder = "rgba(255,152,0,0.5)",

	-- Text
	textPrimary = "#ffffff",
	textSecondary = "rgba(255,255,255,0.8)",
	textMuted = "rgba(255,255,255,0.5)",
	textFaint = "rgba(255,255,255,0.35)",

	-- Surfaces
	surface = "rgba(255,255,255,0.06)",
	surfaceHover = "rgba(255,255,255,0.12)",
	surfaceBorder = "rgba(255,255,255,0.12)",

	-- Panels / overlays
	panel = "#1a1a2e",
	overlay = "rgba(0,0,0,0.65)",
}

return theme
