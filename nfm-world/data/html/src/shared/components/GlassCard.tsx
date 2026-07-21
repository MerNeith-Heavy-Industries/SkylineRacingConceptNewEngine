import type { ComponentChildren } from "preact";
import { styled } from "goober";

interface GlassCardProps {
  color?: string;
  children?: ComponentChildren;
  className?: string;
  style?: Record<string, string | number>;
  onClick?: () => void;
}

const Wrapper = styled("div")`
  background: rgba(255,255,255,0.05);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-radius: 12px;
  border: 1px solid rgba(255,255,255,0.1);
  padding: 16px 20px;
  color: #fff;
  font-family: 'Segoe UI', system-ui, sans-serif;
  position: relative;
  overflow: hidden;
`;

const Bar = styled<{ color: string }>("div")`
  position: absolute; top: 0; left: 0;
  width: 100%; height: 3px;
  background: ${(p) => p.color};
  border-radius: 12px 12px 0 0;
`;

export function GlassCard({ color = "#4fc3f7", children, style, className, onClick }: GlassCardProps) {
  return (
    <Wrapper style={style} className={className} onClick={onClick}>
      <Bar color={color} />
      {children}
    </Wrapper>
  );
}

// ── StatBar ──────────────────────────────────────────────────────
// Horizontal progress bar with label and value. Used in garage and HUD.

interface StatBarProps {
  label: string;
  value: number; // 0..1
  color?: string;
  height?: number;
}

export function StatBar({ label, value, color = "#4fc3f7", height = 8 }: StatBarProps) {
  const pct = Math.round(Math.max(0, Math.min(1, value)) * 100);
  return (
    <div style={{ marginBottom: "8px" }}>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          fontSize: "12px",
          marginBottom: "4px",
          color: "rgba(255,255,255,0.7)",
        }}
      >
        <span>{label}</span>
        <span>{pct}%</span>
      </div>
      <div
        style={{
          width: "100%",
          height: `${height}px`,
          background: "rgba(255,255,255,0.1)",
          borderRadius: `${height / 2}px`,
          overflow: "hidden",
        }}
      >
        <div
          style={{
            width: `${pct}%`,
            height: "100%",
            background: color,
            borderRadius: `${height / 2}px`,
            transition: "width 0.2s ease",
          }}
        />
      </div>
    </div>
  );
}

// ── CenterText ───────────────────────────────────────────────────
// Large center-screen text overlay (for "GO!", "FINISH", etc.)

interface CenterTextProps {
  text: string;
  size?: number;
  color?: string;
}

export function CenterText({
  text,
  size = 64,
  color = "rgba(255,255,255,0.9)",
}: CenterTextProps) {
  return (
    <div
      style={{
        position: "absolute",
        top: "50%",
        left: "50%",
        transform: "translate(-50%, -50%)",
        fontSize: `${size}px`,
        fontWeight: 800,
        color,
        textShadow: "0 2px 12px rgba(0,0,0,0.6)",
        pointerEvents: "none",
        zIndex: 100,
        animation: "nfmw-fadeIn 0.15s ease-out",
      }}
    >
      {text.split("\n").map((line, index) => (
        <div key={index}>{line}</div>
      ))}
    </div>
  );
}