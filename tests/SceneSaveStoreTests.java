import dev.skycraft.client.scene.SceneSaveStore;
import dev.skycraft.client.scene.SceneBackupVerifier;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.UUID;
import java.util.Map;
import java.util.zip.CRC32;
import java.util.zip.ZipEntry;
import java.util.zip.ZipOutputStream;

/** Standalone disk/journal regression tests. No Minecraft, game assets or account needed. */
public final class SceneSaveStoreTests {
    private static final UUID PLAYER = UUID.fromString("cf00a42b-f9bc-4f7b-9197-51c69df6a889");
    private static final byte[] OLD = { 1, 4, 9, 16 }, CURRENT = { 2, 5, 10, 17 }, OTHER = { 3, 6, 11, 18 };
    private static Path root;
    private static int passed;

    public static void main(String[] args) throws Exception {
        root = Path.of(args[0]).toAbsolutePath(); Files.createDirectories(root);
        test("legacy airport is the default without modifying the world", () -> {
            var f = fixture(); var s = f.store.read();
            check(s.save().equals("SkyCraft") && s.scene() == 4 && !s.pending());
            check(!Files.exists(f.saves));
        });
        test("scene names support the full unsigned world ID and reject zero", () -> {
            check(SceneSaveStore.saveName(4).equals("SkyCraft"));
            check(SceneSaveStore.saveName(-1).equals("SkyCraft-scene-4294967295"));
            fails(() -> SceneSaveStore.saveName(0));
            var f = fixture(); int addressable = (int) 0x876acafdL;
            check(SceneSaveStore.saveName(addressable).equals("SkyCraft-scene-2271922941"));
            var pending = f.store.prepare("SkyCraft", 4, SceneSaveStore.saveName(addressable), addressable, CURRENT);
            check(f.store.read().equals(pending) && pending.pendingScene() == addressable);
        });
        test("prepared transfer resumes with the same verified immutable snapshot", () -> {
            var f = fixture();
            var prepared = f.store.prepare("SkyCraft", 4, "SkyCraft-scene-9", 9, CURRENT);
            var reopened = new SceneSaveStore(f.journal, f.saves, PLAYER);
            var resumed = reopened.read();
            check(resumed.equals(prepared) && resumed.pending());
            check(Arrays.equals(reopened.snapshot(resumed.snapshot()), CURRENT));
        });
        test("caller mutation cannot change a persisted snapshot", () -> {
            var f = fixture(); byte[] input = CURRENT.clone();
            var s = f.store.prepare("SkyCraft", 4, "SkyCraft-scene-9", 9, input);
            input[0] = 99;
            check(Arrays.equals(f.store.snapshot(s.snapshot()), CURRENT));
        });
        test("commit writes the target before clearing pending and preserves its previous player data", () -> {
            var f = fixture(); Path target = f.store.playerFile("SkyCraft-scene-9");
            Files.createDirectories(target.getParent()); Files.write(target, OLD);
            f.store.prepare("SkyCraft", 4, "SkyCraft-scene-9", 9, CURRENT);
            var committed = f.store.complete("SkyCraft-scene-9", 9, CURRENT);
            check(!committed.pending() && committed.scene() == 9);
            check(Arrays.equals(Files.readAllBytes(target), CURRENT));
            check(f.store.read().equals(committed));
            try (var files = Files.list(f.journal.resolve("backups"))) {
                var backups = files.toList(); check(backups.size() == 1);
                check(Arrays.equals(Files.readAllBytes(backups.get(0)), OLD));
            }
        });
        test("invalid player bytes cannot overwrite an existing target or its journal", () -> {
            var f = fixture(); Path target = f.store.playerFile("SkyCraft-scene-9");
            Files.createDirectories(target.getParent()); Files.write(target, OLD);
            var prepared = f.store.prepare("SkyCraft", 4, "SkyCraft-scene-9", 9, CURRENT);
            fails(() -> f.store.complete("SkyCraft-scene-9", 9, new byte[0]));
            check(Arrays.equals(Files.readAllBytes(target), OLD)); check(f.store.read().equals(prepared));
        });
        test("a newer vanilla save takes precedence over an older committed snapshot", () -> {
            var f = fixture(); var s = f.store.complete("SkyCraft", 4, OLD);
            Files.write(f.store.playerFile("SkyCraft"), CURRENT);
            check(Arrays.equals(f.store.lastPlayerData(s), CURRENT));
        });
        test("a verified snapshot remains recoverable if the player file is missing", () -> {
            var f = fixture(); var s = f.store.complete("SkyCraft", 4, CURRENT);
            Files.delete(f.store.playerFile("SkyCraft"));
            check(Arrays.equals(f.store.lastPlayerData(s), CURRENT));
        });
        test("corrupt snapshot fails closed and preserves the journal", () -> {
            var f = fixture(); var s = f.store.prepare("SkyCraft", 4, "SkyCraft-scene-9", 9, CURRENT);
            Path record = f.journal.resolve(PLAYER + ".properties"); byte[] previous = Files.readAllBytes(record);
            Files.write(f.journal.resolve("snapshots").resolve(s.snapshot() + ".dat"), OTHER);
            fails(f.store::read); check(Arrays.equals(Files.readAllBytes(record), previous));
        });
        test("mismatched scene IDs in a journal cannot silently select another world", () -> {
            var f = fixture();
            Files.writeString(f.journal.resolve(PLAYER + ".properties"), "version=1\nsave=SkyCraft\nscene=9\n");
            fails(f.store::read); check(!Files.exists(f.saves));
        });
        test("unsupported journal version is refused without creating a new inventory", () -> {
            var f = fixture();
            Files.writeString(f.journal.resolve(PLAYER + ".properties"), "version=99\nsave=SkyCraft\nscene=4\n");
            fails(f.store::read); check(!Files.exists(f.saves));
        });
        test("a pending transfer without a player snapshot cannot resume as an empty inventory", () -> {
            var f = fixture();
            Files.writeString(f.journal.resolve(PLAYER + ".properties"), "version=1\nsave=SkyCraft\nscene=4\npendingSave=SkyCraft-scene-9\npendingScene=9\nsnapshot=\n");
            fails(f.store::read); check(!Files.exists(f.saves));
        });
        test("world paths and transfer mappings cannot escape managed scene names", () -> {
            var f = fixture();
            fails(() -> f.store.playerFile("../outside"));
            fails(() -> f.store.playerFile("C:/outside"));
            fails(() -> f.store.prepare("SkyCraft", 4, "SkyCraft-scene-9", 8, CURRENT));
            check(!Files.exists(f.journal.resolve(PLAYER + ".properties")));
        });
        test("different offline player UUIDs keep separate journals and player files", () -> {
            var f = fixture(); var first = f.store.complete("SkyCraft", 4, CURRENT);
            UUID secondId = UUID.fromString("d900a42b-f9bc-4f7b-9197-51c69df6a889");
            var second = new SceneSaveStore(f.journal, f.saves, secondId);
            second.complete("SkyCraft-scene-9", 9, OTHER);
            check(f.store.read().equals(first));
            check(Arrays.equals(f.store.lastPlayerData(first), CURRENT));
            check(second.read().scene() == 9);
        });
        test("retargeting a prepared transfer preserves the immutable player payload", () -> {
            var f = fixture(); var original = f.store.prepare("SkyCraft", 4, "SkyCraft-scene-9", 9, CURRENT);
            var newer = f.store.prepare(original.save(), original.scene(), "SkyCraft-scene-10", 10, f.store.snapshot(original.snapshot()));
            check(newer.snapshot().equals(original.snapshot()) && newer.pendingScene() == 10);
            check(Arrays.equals(f.store.snapshot(original.snapshot()), CURRENT));
        });
        test("a new installation can durably initialize directly on an island", () -> {
            var f = fixture(); var s = f.store.initialize("SkyCraft-scene-9", 9);
            check(f.store.read().equals(s) && s.snapshot().isEmpty() && !s.pending());
            check(f.store.lastPlayerData(s) == null);
        });
        test("player writes use Minecraft 26.3's canonical players/data directory", () -> {
            var f = fixture();
            Path canonical = f.saves.resolve("SkyCraft").resolve("players").resolve("data").resolve(PLAYER + ".dat");
            check(f.store.playerFile("SkyCraft").equals(canonical));
            f.store.complete("SkyCraft", 4, CURRENT);
            check(Arrays.equals(Files.readAllBytes(canonical), CURRENT));
            check(!Files.exists(f.saves.resolve("SkyCraft").resolve("playerdata")));
        });
        test("current vanilla player data outranks stale legacy playerdata without modifying it", () -> {
            var f = fixture(); var s = f.store.complete("SkyCraft", 4, OLD);
            Path legacy = f.saves.resolve("SkyCraft").resolve("playerdata").resolve(PLAYER + ".dat");
            Files.createDirectories(legacy.getParent()); Files.write(legacy, OTHER);
            Files.write(f.store.playerFile("SkyCraft"), CURRENT);
            check(Arrays.equals(f.store.lastPlayerData(s), CURRENT));
            check(Arrays.equals(Files.readAllBytes(legacy), OTHER));
        });
        test("a newly completed native backup with level.dat and player data is verified", () -> {
            Path directory = backupDirectory(); var previous = SceneBackupVerifier.existing(directory);
            Path backup = zip(directory.resolve("native.zip"), Map.of("SkyCraft/level.dat", OLD, "SkyCraft/players/data/test.dat", CURRENT));
            check(SceneBackupVerifier.verifyNew(directory, previous).equals(backup));
        });
        test("backup verification reads every file and rejects a corrupted player entry CRC", () -> {
            Path directory = backupDirectory(); var previous = SceneBackupVerifier.existing(directory);
            byte[] payload = { 91, 73, 63, 29, 17, 7 };
            Path backup = zip(directory.resolve("broken.zip"), Map.of("level.dat", OLD, "players/data/test.dat", payload));
            byte[] bytes = Files.readAllBytes(backup); int found = -1;
            for (int i = 0; i <= bytes.length - payload.length; i++) {
                boolean matches = true;
                for (int j = 0; j < payload.length; j++) matches &= bytes[i + j] == payload[j];
                if (matches) { found = i; break; }
            }
            check(found >= 0); bytes[found] ^= 1; Files.write(backup, bytes);
            fails(() -> SceneBackupVerifier.verifyNew(directory, previous));
        });
        test("a ZIP without level.dat cannot stand in for a completed world backup", () -> {
            Path directory = backupDirectory(); var previous = SceneBackupVerifier.existing(directory);
            zip(directory.resolve("incomplete.zip"), Map.of("players/data/test.dat", CURRENT));
            fails(() -> SceneBackupVerifier.verifyNew(directory, previous));
        });
        test("an existing archive cannot be mistaken for a newly completed backup", () -> {
            Path directory = backupDirectory(); zip(directory.resolve("old.zip"), Map.of("level.dat", OLD));
            var previous = SceneBackupVerifier.existing(directory);
            fails(() -> SceneBackupVerifier.verifyNew(directory, previous));
        });
        test("two new archives are refused instead of guessing which world was backed up", () -> {
            Path directory = backupDirectory(); var previous = SceneBackupVerifier.existing(directory);
            zip(directory.resolve("first.zip"), Map.of("level.dat", OLD));
            zip(directory.resolve("second.zip"), Map.of("level.dat", CURRENT));
            fails(() -> SceneBackupVerifier.verifyNew(directory, previous));
        });
        test("old backup archives stay untouched while only the new backup is verified", () -> {
            Path directory = backupDirectory(); Path old = directory.resolve("old.zip");
            Files.createDirectories(directory); Files.write(old, OTHER); var previous = SceneBackupVerifier.existing(directory);
            Path backup = zip(directory.resolve("new.zip"), Map.of("SkyCraft-scene-2271922941/level.dat", CURRENT));
            check(SceneBackupVerifier.verifyNew(directory, previous).equals(backup));
            check(Arrays.equals(Files.readAllBytes(old), OTHER));
        });
        System.out.println("PASS: " + passed + " scene save store and backup tests");
    }
    private record Fixture(SceneSaveStore store, Path journal, Path saves) { }
    private static Fixture fixture() throws IOException {
        Path directory = root.resolve(String.format("case-%02d", passed + 1));
        Path journal = directory.resolve("journal"), saves = directory.resolve("saves");
        return new Fixture(new SceneSaveStore(journal, saves, PLAYER), journal, saves);
    }
    private static Path backupDirectory() { return root.resolve("backup-case-" + (passed + 1)); }
    private static Path zip(Path path, Map<String, byte[]> entries) throws IOException {
        Files.createDirectories(path.getParent());
        try (var out = new ZipOutputStream(Files.newOutputStream(path))) {
            for (var item : entries.entrySet()) {
                byte[] content = item.getValue(); CRC32 crc = new CRC32(); crc.update(content);
                var entry = new ZipEntry(item.getKey()); entry.setMethod(ZipEntry.STORED);
                entry.setSize(content.length); entry.setCompressedSize(content.length); entry.setCrc(crc.getValue());
                out.putNextEntry(entry); out.write(content); out.closeEntry();
            }
        }
        return path.toAbsolutePath().normalize();
    }
    @FunctionalInterface private interface Case { void run() throws Exception; }
    private static void test(String name, Case body) throws Exception { body.run(); passed++; System.out.println("PASS: " + name); }
    private static void check(boolean okay) { if (!okay) throw new AssertionError("scene save store regression"); }
    private static void fails(Case body) throws Exception {
        try { body.run(); } catch (IOException | IllegalArgumentException expected) { return; }
        throw new AssertionError("unsafe operation was accepted");
    }
}
