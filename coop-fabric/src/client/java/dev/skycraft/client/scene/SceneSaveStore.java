package dev.skycraft.client.scene;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.channels.FileChannel;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.nio.file.StandardOpenOption;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.HexFormat;
import java.util.Properties;
import java.util.UUID;

/** Crash-safe transfer journal. Snapshots are immutable; a single atomic record commits each phase. */
public final class SceneSaveStore {
    public record State(String save, int scene, String snapshot, String pendingSave, int pendingScene) {
        public boolean pending() { return !pendingSave.isEmpty(); }
    }
    private static final int MAX_BYTES = 64 * 1024 * 1024;
    private final Path root, saves, record;
    private final UUID player;

    public SceneSaveStore(Path root, Path saves, UUID player) throws IOException {
        this.root = root.toAbsolutePath().normalize();
        this.saves = saves.toAbsolutePath().normalize();
        this.player = player;
        Files.createDirectories(this.root.resolve("snapshots"));
        Files.createDirectories(this.root.resolve("backups"));
        record = this.root.resolve(player + ".properties");
    }

    public static String saveName(int scene) {
        if (scene == 0) throw new IllegalArgumentException("A live PEAK scene is required");
        return scene == 4 ? "SkyCraft" : "SkyCraft-scene-" + Integer.toUnsignedString(scene);
    }

    public Path playerFile(String save) throws IOException {
        validSave(save);
        // Minecraft 26.3's LevelResource.PLAYER_DATA_DIR is "players/data".
        // "playerdata" was only the legacy layout; preferring it can resurrect
        // an old inventory after the current vanilla save has already advanced.
        Path result = saves.resolve(save).resolve("players").resolve("data").resolve(player + ".dat").normalize();
        if (!result.startsWith(saves)) throw new IOException("World path escaped the save directory");
        Path cursor = saves;
        for (Path part : saves.relativize(result.getParent())) {
            cursor = cursor.resolve(part);
            if (Files.isSymbolicLink(cursor)) throw new IOException("Linked world paths are not transfer targets");
        }
        return result;
    }

    public State read() throws IOException {
        if (!Files.exists(record)) return new State("SkyCraft", 4, "", "", 0);
        Properties values = new Properties();
        values.load(new ByteArrayInputStream(Files.readAllBytes(record)));
        try {
            if (!"1".equals(values.getProperty("version"))) throw new IllegalArgumentException("Unknown journal version");
            State state = new State(values.getProperty("save"), Integer.parseInt(values.getProperty("scene")),
                values.getProperty("snapshot", ""), values.getProperty("pendingSave", ""),
                Integer.parseInt(values.getProperty("pendingScene", "0")));
            validSave(state.save());
            if (!saveName(state.scene()).equals(state.save())) throw new IllegalArgumentException("Scene/save mismatch");
            if (state.pending() && !saveName(state.pendingScene()).equals(state.pendingSave())) throw new IllegalArgumentException("Pending scene/save mismatch");
            if (state.pending() && state.snapshot().isEmpty()) throw new IllegalArgumentException("Pending transfer has no player snapshot");
            if (!state.snapshot().isEmpty()) snapshot(state.snapshot());
            return state;
        } catch (RuntimeException error) { throw new IOException("Invalid scene journal; original worlds were preserved", error); }
    }

    public synchronized State prepare(String source, int sourceScene, String target, int targetScene, byte[] playerData) throws IOException {
        if (!saveName(sourceScene).equals(source) || !saveName(targetScene).equals(target)) throw new IOException("Invalid transfer scenes");
        String hash = keepSnapshot(playerData);
        State state = new State(source, sourceScene, hash, target, targetScene);
        write(state);
        return state;
    }

    public synchronized State initialize(String save, int scene) throws IOException {
        if (!saveName(scene).equals(save)) throw new IOException("Invalid initial scene");
        State state = new State(save, scene, "", "", 0);
        write(state);
        return state;
    }

    public synchronized State complete(String save, int scene, byte[] playerData) throws IOException {
        if (!saveName(scene).equals(save)) throw new IOException("Invalid commit scene");
        requireBytes(playerData); // validate before touching a previously valid player file
        // The target player file is durable before clearing the pending marker.
        Path target = playerFile(save);
        if (Files.exists(target)) keepBackup(Files.readAllBytes(target));
        atomicWrite(target, playerData);
        if (!digest(Files.readAllBytes(target)).equals(digest(playerData))) throw new IOException("Player write verification failed");
        State state = new State(save, scene, keepSnapshot(playerData), "", 0);
        write(state);
        return state;
    }

    public byte[] snapshot(String hash) throws IOException {
        if (hash == null || !hash.matches("[0-9a-f]{64}")) throw new IOException("Invalid snapshot reference");
        byte[] bytes = Files.readAllBytes(root.resolve("snapshots").resolve(hash + ".dat"));
        requireBytes(bytes);
        if (!digest(bytes).equals(hash)) throw new IOException("Scene snapshot checksum failed");
        return bytes;
    }

    public byte[] lastPlayerData(State state) throws IOException {
        Path file = playerFile(state.save());
        if (Files.exists(file)) {
            byte[] bytes = Files.readAllBytes(file);
            requireBytes(bytes);
            return bytes;
        }
        if (!state.snapshot().isEmpty()) return snapshot(state.snapshot());
        return null;
    }

    private String keepSnapshot(byte[] bytes) throws IOException { return keep("snapshots", bytes); }
    private void keepBackup(byte[] bytes) throws IOException { keep("backups", bytes); }
    private String keep(String folder, byte[] bytes) throws IOException {
        requireBytes(bytes);
        String hash = digest(bytes);
        Path file = root.resolve(folder).resolve(hash + ".dat");
        if (!Files.exists(file)) atomicWrite(file, bytes);
        if (!digest(Files.readAllBytes(file)).equals(hash)) throw new IOException("Immutable backup checksum failed");
        return hash;
    }

    private void write(State state) throws IOException {
        Properties values = new Properties();
        values.setProperty("version", "1"); values.setProperty("save", state.save());
        values.setProperty("scene", Integer.toString(state.scene())); values.setProperty("snapshot", state.snapshot());
        values.setProperty("pendingSave", state.pendingSave()); values.setProperty("pendingScene", Integer.toString(state.pendingScene()));
        ByteArrayOutputStream bytes = new ByteArrayOutputStream(); values.store(bytes, "MinePeak scene transfer; preserve with the worlds");
        atomicWrite(record, bytes.toByteArray());
    }

    private static void validSave(String save) throws IOException {
        if (save == null || !(save.equals("SkyCraft") || save.matches("SkyCraft-scene-[0-9]{1,10}"))) throw new IOException("Invalid managed world name");
    }
    private static void requireBytes(byte[] bytes) throws IOException {
        if (bytes == null || bytes.length == 0 || bytes.length > MAX_BYTES) throw new IOException("Missing or oversized player snapshot");
    }
    private static String digest(byte[] bytes) {
        try { return HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(bytes)); }
        catch (NoSuchAlgorithmException impossible) { throw new AssertionError(impossible); }
    }
    private static void atomicWrite(Path target, byte[] bytes) throws IOException {
        Files.createDirectories(target.getParent());
        Path temporary = Files.createTempFile(target.getParent(), ".minepeak-", ".pending");
        try {
            try (FileChannel out = FileChannel.open(temporary, StandardOpenOption.WRITE, StandardOpenOption.TRUNCATE_EXISTING)) {
                ByteBuffer buffer = ByteBuffer.wrap(bytes); while (buffer.hasRemaining()) out.write(buffer); out.force(true);
            }
            // No non-atomic fallback: a failure keeps the previous record and recoverable snapshot.
            Files.move(temporary, target, StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING);
        } finally { Files.deleteIfExists(temporary); }
    }
}
