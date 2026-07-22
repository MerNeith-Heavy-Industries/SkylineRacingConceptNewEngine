import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class HudStateData {
    speed: number;
    power: number;
    damage: number;
    lap: number;
    totalLaps: number;
    lapTime: number;
    position: number;
    totalRacers: number;
    stateText: string | null;
    stateTextEndsAt: Date | null;
    lapDiffMs: number | null;
    lastLapDiffMs: number | null;
    chkDiffMs: number | null;
    lastChkDiffMs: number | null;
    countdownTimer: number;

    constructor() {
        this.speed = 0;
        this.power = 0;
        this.damage = 0;
        this.lap = 0;
        this.totalLaps = 0;
        this.lapTime = 0;
        this.position = 0;
        this.totalRacers = 0;
        this.stateText = null;
        this.stateTextEndsAt = null;
        this.lapDiffMs = null;
        this.lastLapDiffMs = null;
        this.chkDiffMs = null;
        this.lastChkDiffMs = null;
        this.countdownTimer = 0;

    }

    static serialize(value: HudStateData | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: HudStateData | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(15);
        writer.writeFloat32(value.speed);
        writer.writeFloat32(value.power);
        writer.writeFloat32(value.damage);
        writer.writeInt32(value.lap);
        writer.writeInt32(value.totalLaps);
        writer.writeInt32(value.lapTime);
        writer.writeInt32(value.position);
        writer.writeInt32(value.totalRacers);
        writer.writeString(value.stateText);
        writer.writeNullableDate(value.stateTextEndsAt);
        writer.writeNullableInt32(value.lapDiffMs);
        writer.writeNullableInt32(value.lastLapDiffMs);
        writer.writeNullableInt32(value.chkDiffMs);
        writer.writeNullableInt32(value.lastChkDiffMs);
        writer.writeInt32(value.countdownTimer);

    }

    static serializeArray(value: (HudStateData | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (HudStateData | null)[] | null): void {
        writer.writeArray(value, (writer, x) => HudStateData.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): HudStateData | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): HudStateData | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new HudStateData();
        if (count == 15) {
            value.speed = reader.readFloat32()!;
            value.power = reader.readFloat32()!;
            value.damage = reader.readFloat32()!;
            value.lap = reader.readInt32()!;
            value.totalLaps = reader.readInt32()!;
            value.lapTime = reader.readInt32()!;
            value.position = reader.readInt32()!;
            value.totalRacers = reader.readInt32()!;
            value.stateText = reader.readString()!;
            value.stateTextEndsAt = reader.readNullableDate();
            value.lapDiffMs = reader.readNullableInt32();
            value.lastLapDiffMs = reader.readNullableInt32();
            value.chkDiffMs = reader.readNullableInt32();
            value.lastChkDiffMs = reader.readNullableInt32();
            value.countdownTimer = reader.readInt32()!;

        }
        else if (count > 15) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.speed = reader.readFloat32()!; if (count == 1) return value;
            value.power = reader.readFloat32()!; if (count == 2) return value;
            value.damage = reader.readFloat32()!; if (count == 3) return value;
            value.lap = reader.readInt32()!; if (count == 4) return value;
            value.totalLaps = reader.readInt32()!; if (count == 5) return value;
            value.lapTime = reader.readInt32()!; if (count == 6) return value;
            value.position = reader.readInt32()!; if (count == 7) return value;
            value.totalRacers = reader.readInt32()!; if (count == 8) return value;
            value.stateText = reader.readString()!; if (count == 9) return value;
            value.stateTextEndsAt = reader.readNullableDate(); if (count == 10) return value;
            value.lapDiffMs = reader.readNullableInt32(); if (count == 11) return value;
            value.lastLapDiffMs = reader.readNullableInt32(); if (count == 12) return value;
            value.chkDiffMs = reader.readNullableInt32(); if (count == 13) return value;
            value.lastChkDiffMs = reader.readNullableInt32(); if (count == 14) return value;
            value.countdownTimer = reader.readInt32()!; if (count == 15) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (HudStateData | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (HudStateData | null)[] | null {
        return reader.readArray(reader => HudStateData.deserializeCore(reader));
    }
}
