import { useState, useEffect } from "preact/hooks";
import { callNfmw, onNfmwEvent } from "@shared/bridge";
import { GlassCard, StatBar } from "@shared/components/GlassCard";
import { CarStatsData } from "@shared/memorypack/CarStatsData";
import { CarCollectionsData } from "@shared/memorypack/CarCollectionsData";

// ── Garage ───────────────────────────────────────────────────────
// Functional Preact component: car selection + stat display.

export function Garage() {
  const [currentCar, setCurrentCar] = useState<CarStatsData | null>(null);
  const [collections, setCollections] = useState<CarCollectionsData | null>(null);

  useEffect(() => {
    const u1 = onNfmwEvent<CarStatsData | null>("garage:currentCar", setCurrentCar, CarStatsData.deserialize.bind(CarStatsData));
    const u2 = onNfmwEvent<CarCollectionsData | null>("garage:collections", setCollections, CarCollectionsData.deserialize.bind(CarCollectionsData));
    return () => { u1(); u2(); };
  }, []);

  const handleBack = () => callNfmw("back");
  const handleSelectCar = (collection: string, carName: string) => {
    callNfmw("selectCar", { collection, carName });
  };

  return (
    <div style={{ width: "100%", height: "100%", display: "flex", animation: "nfmw-fadeIn 0.3s ease-out" }}>
      <div style={{
        width: "340px", height: "100%", padding: "32px 24px",
        display: "flex", flexDirection: "column", gap: "16px",
        background: "rgba(0,0,0,0.3)", borderRight: "1px solid rgba(255,255,255,0.06)",
        overflowY: "auto",
      }}>
        <div style={{ fontSize: "28px", fontWeight: 700, letterSpacing: "2px", textTransform: "uppercase" }}>
          Garage
        </div>

        <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", color: "rgba(255,255,255,0.3)", fontSize: "18px", letterSpacing: "2px" }}>
            {currentCar ? (
              <div style={{ width: "280px", padding: "24px" }}>
                  <div style={{ fontSize: "18px", fontWeight: 600, color: "#4fc3f7", marginBottom: "16px" }}>
                  {currentCar.name}
                  </div>
                  <StatBar label="Top Speed" value={currentCar.topSpeed} color="#ff6e40" />
                  <StatBar label="Acceleration" value={currentCar.acceleration} color="#ffd740" />
                  <StatBar label="Handling" value={currentCar.handling} color="#69f0ae" />
                  <StatBar label="Power Save" value={currentCar.powerSave} color="#40c4ff" />
                  <StatBar label="Strength" value={currentCar.strength} color="#ff4081" />
                  <StatBar label="Max Health" value={currentCar.maxHealth} color="#e040fb" />
                  <StatBar label="Stunting" value={currentCar.stunting} color="#ff6e40" />
                  <StatBar label="Hypergliding" value={currentCar.hypergliding} color="#7c4dff" />
                  <StatBar label="AB'ing" value={currentCar.abing} color="#448aff" />
              </div>
            ) : (
              "Select a car to view stats"
            )}
        </div>

        {collections?.collections?.map((col) => (
          col != null &&
            <div key={col.name}>
              <div style={{ fontSize: "12px", color: "rgba(255,255,255,0.4)", marginBottom: "6px", letterSpacing: "1px", textTransform: "uppercase" }}>
                {col.name}
              </div>
              {col.cars?.map((car) => (
                car != null &&
                  <GlassCard
                    key={car.name}
                    color={currentCar?.name === car.name ? "#4fc3f7" : "rgba(255,255,255,0.15)"}
                    style={{
                      marginBottom: "8px", cursor: "pointer",
                      opacity: currentCar?.name === car.name ? 1 : 0.7,
                      transition: "opacity 0.15s ease",
                    }}
                  >
                    <div onClick={() => handleSelectCar(col.name, car.name)}>
                      <div style={{ fontWeight: 600, fontSize: "14px", marginBottom: "4px" }}>{car.name}</div>
                      <div style={{ fontSize: "11px", color: "rgba(255,255,255,0.4)" }}>{col.name}</div>
                    </div>
                  </GlassCard>
              ))}
            </div>
        ))}

        <button
          onClick={handleBack}
          style={{
            padding: "10px 24px", fontSize: "14px", fontWeight: 600,
            color: "rgba(255,255,255,0.6)", background: "rgba(255,255,255,0.06)",
            border: "1px solid rgba(255,255,255,0.1)", borderRadius: "6px",
            cursor: "pointer", marginTop: "auto",
          }}
        >
          ← Back to Menu
        </button>
      </div>
    </div>
  );
}
