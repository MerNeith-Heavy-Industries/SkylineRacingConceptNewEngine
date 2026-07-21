import { MemoryPackWriter } from "./MemoryPackWriter";
import { MemoryPackReader } from "./MemoryPackReader";

export class AccountData {
    name: string;
    isLoggedIn: boolean;
    avatarUrl: string | null;

    constructor() {
        this.name = "";
        this.isLoggedIn = false;
        this.avatarUrl = null;

    }

    static serialize(value: AccountData | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeCore(writer, value);
        return writer.toArray();
    }

    static serializeCore(writer: MemoryPackWriter, value: AccountData | null): void {
        if (value == null) {
            writer.writeNullObjectHeader();
            return;
        }

        writer.writeObjectHeader(3);
        writer.writeString(value.name);
        writer.writeBoolean(value.isLoggedIn);
        writer.writeString(value.avatarUrl);

    }

    static serializeArray(value: (AccountData | null)[] | null): Uint8Array {
        const writer = MemoryPackWriter.getSharedInstance();
        this.serializeArrayCore(writer, value);
        return writer.toArray();
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (AccountData | null)[] | null): void {
        writer.writeArray(value, (writer, x) => AccountData.serializeCore(writer, x));
    }

    static deserialize(buffer: ArrayBuffer): AccountData | null {
        return this.deserializeCore(new MemoryPackReader(buffer));
    }

    static deserializeCore(reader: MemoryPackReader): AccountData | null {
        const [ok, count] = reader.tryReadObjectHeader();
        if (!ok) {
            return null;
        }

        const value = new AccountData();
        if (count == 3) {
            value.name = reader.readString()!;
            value.isLoggedIn = reader.readBoolean()!;
            value.avatarUrl = reader.readString()!;

        }
        else if (count > 3) {
            throw new Error("Current object's property count is larger than type schema, can't deserialize about versioning.");
        }
        else {
            if (count == 0) return value;
            value.name = reader.readString()!; if (count == 1) return value;
            value.isLoggedIn = reader.readBoolean()!; if (count == 2) return value;
            value.avatarUrl = reader.readString()!; if (count == 3) return value;

        }
        return value;
    }

    static deserializeArray(buffer: ArrayBuffer): (AccountData | null)[] | null {
        return this.deserializeArrayCore(new MemoryPackReader(buffer));
    }

    static deserializeArrayCore(reader: MemoryPackReader): (AccountData | null)[] | null {
        return reader.readArray(reader => AccountData.deserializeCore(reader));
    }
}
