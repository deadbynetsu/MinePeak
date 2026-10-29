package dev.skycraft.client.scene;

import dev.skycraft.client.MirrorWorld;
import dev.skycraft.client.mixin.MirrorWorldSceneAccess;
import dev.skycraft.client.mixin.SceneExporterAccess;
import dev.skycraft.link.SkyLink;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.List;
import java.util.Optional;
import java.util.Properties;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import net.fabricmc.api.ClientModInitializer;
import net.fabricmc.fabric.api.event.lifecycle.v1.ServerLifecycleEvents;
import net.fabricmc.fabric.api.event.lifecycle.v1.ServerTickEvents;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.screens.TitleScreen;
import net.minecraft.client.gui.screens.worldselection.EditWorldScreen;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.nbt.DoubleTag;
import net.minecraft.nbt.FloatTag;
import net.minecraft.nbt.ListTag;
import net.minecraft.nbt.NbtAccounter;
import net.minecraft.nbt.NbtIo;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.server.players.NameAndId;
import net.minecraft.util.ProblemReporter;
import net.minecraft.world.level.storage.LevelResource;
import net.minecraft.world.level.storage.LevelStorageSource;
import net.minecraft.world.level.storage.TagValueOutput;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/** Isolates constructions per PEAK scene while carrying the complete local player save. */
public final class SceneWorlds implements ClientModInitializer {
    private static final Logger LOG = LoggerFactory.getLogger("minepeak-scenes");
    private static final SkyLink.SkyState HOST = new SkyLink.SkyState();
    private static volatile SceneSaveStore store;
    private static volatile SceneSaveStore.State state;
    private static volatile MinecraftServer initializedServer;
    private static volatile MinecraftServer closingServer;
    private static CompletableFuture<SceneSaveStore.State> capture;
    private static volatile String openingSave = "";
    private static volatile int openingScene;
    private static volatile String failure;
    private static long nextConfigRead;
    private static boolean externalConfig;
    private static ClientLevel exportedLevel;
    private static int exportedGeneration = Integer.MIN_VALUE;
    private static int exportedScene;
    private static int exportedEpoch;

    @Override public void onInitializeClient() {
        ServerTickEvents.END_SERVER_TICK.register(SceneWorlds::serverTick);
        ServerLifecycleEvents.SERVER_STOPPED.register(server -> {
            if (closingServer == server) closingServer = null;
            if (initializedServer == server) initializedServer = null;
        });
        LOG.info("MinePeak: isolated scene saves and transactional player transfer ready");
    }

    /** Called before the base bridge opens worlds, drains inputs or acknowledges teleports. */
    public static boolean beforeWorldFrame(Minecraft minecraft) {
        if (external(minecraft)) return true;
        if (failure != null) return false;
        try {
            if (!SkyLink.active() || !SkyLink.readSkyState(HOST) || !HOST.inGame() || HOST.worldId == 0) return false;
            if (store == null) {
                UUID uuid = minecraft.getGameProfile().id();
                store = new SceneSaveStore(minecraft.gameDirectory.toPath().resolve("data/minepeak-scenes"),
                    minecraft.getLevelSource().getBaseDir(), uuid);
                state = store.read();
            }
            if (capture != null) {
                if (!capture.isDone()) return false;
                state = capture.join(); capture = null;
                MinecraftServer previous = minecraft.getSingleplayerServer();
                closingServer = previous;
                minecraft.disconnectFromWorld(ClientLevel.DEFAULT_QUIT_MESSAGE);
                MirrorWorldSceneAccess.peakcraft$setAttempted(false);
                minecraft.gui.setScreen(new TitleScreen());
                return false;
            }
            if (closingServer != null) {
                if (!closingServer.isStopped()) return false;
                closingServer = null;
            }
            MinecraftServer server = minecraft.getSingleplayerServer();
            if (server != null) {
                String current = managedSave(server);
                if (current == null) return true; // never touch another single-player save
                int currentScene = sceneFor(current);
                if (currentScene == HOST.worldId) return initializedServer == server;
                if (state.pending() && current.equals(state.pendingSave())) return false; // finish its durable commit first
                String target = SceneSaveStore.saveName(HOST.worldId);
                int targetScene = HOST.worldId;
                capture = new CompletableFuture<>();
                CompletableFuture<SceneSaveStore.State> pending = capture;
                server.execute(() -> {
                    try {
                        ServerPlayer player = localPlayer(server);
                        if (player == null) throw new IOException("Local player was unavailable; original world remains open");
                        // Cursor and crafting-grid stacks are not part of player NBT. Close
                        // both an external menu and the default menu using vanilla's return
                        // rules before taking a snapshot (full-inventory overflow stays here).
                        if (player.containerMenu != player.inventoryMenu) player.closeContainer();
                        player.closeContainer();
                        // Flush block/container changes before committing their resulting
                        // inventory. A recovered snapshot must not outrun its source chest.
                        server.saveEverything(true, true, true);
                        pending.complete(store.prepare(current, currentScene, target, targetScene, encode(player)));
                    } catch (Throwable error) { pending.completeExceptionally(error); }
                });
                return false;
            }
            // Startup after a crash can resume a fully persisted prepared transfer.
            String target = SceneSaveStore.saveName(HOST.worldId);
            if (!state.pending() && !state.save().equals(target)) {
                byte[] bytes = store.lastPlayerData(state);
                if (bytes == null) {
                    Path source = minecraft.getLevelSource().getBaseDir().resolve(state.save());
                    if (Files.exists(source)) throw new IOException("Existing scene player data is missing; refusing an empty inventory");
                    state = store.initialize(target, HOST.worldId); // genuinely new installation, no prior player data
                } else {
                    decode(bytes); // do not close/reopen from a corrupt source snapshot
                    state = store.prepare(state.save(), state.scene(), target, HOST.worldId, bytes);
                }
            }
            if (state.pending() && !state.pendingSave().equals(target) && openingSave.isEmpty())
                state = store.prepare(state.save(), state.scene(), target, HOST.worldId, store.snapshot(state.snapshot()));
            openingSave = state.pending() ? state.pendingSave() : target;
            openingScene = state.pending() ? state.pendingScene() : HOST.worldId;
            return true;
        } catch (Throwable error) { fail(error); return false; }
    }

    public static String savePath(String original) { return openingSave.isEmpty() ? original : openingSave; }

    /** The upstream whitelist covers only SkyCraft. Our new scene worlds need a real backup. */
    public static boolean backupExperimentalWorld(LevelStorageSource.LevelStorageAccess level, boolean customized,
                                                   Runnable proceed, Runnable cancel) {
        Minecraft minecraft = Minecraft.getInstance();
        if (customized || store == null || state == null || failure != null || external(minecraft)) return false;
        String name = level.getLevelId();
        if (!name.equals(openingSave) || !name.startsWith("SkyCraft-scene-")) return false;
        Path expected = minecraft.getLevelSource().getBaseDir().resolve(name).toAbsolutePath().normalize();
        if (!expected.equals(level.getLevelPath(LevelResource.ROOT).toAbsolutePath().normalize())) return false;
        try {
            Path directory = level.parent().getBackupPath();
            var previous = SceneBackupVerifier.existing(directory);
            EditWorldScreen.conditionallyMakeBackupAndShowToast(true, level).thenApplyAsync(ok -> {
                if (!Boolean.TRUE.equals(ok)) throw new IllegalStateException("Minecraft could not create the scene backup");
                try { return SceneBackupVerifier.verifyNew(directory, previous); }
                catch (IOException error) { throw new IllegalStateException("Minecraft scene backup verification failed", error); }
            }).whenCompleteAsync((backup, error) -> {
                if (error != null) { fail(error); cancel.run(); return; }
                LOG.info("MinePeak: experimental scene {} backup verified ({}); continuing world load", name, backup.getFileName());
                proceed.run();
            }, minecraft);
            return true;
        } catch (Throwable error) { fail(error); cancel.run(); return true; }
    }

    /** No old level, pose, entities, overlay or geometry may be labelled as the new PEAK scene. */
    public static boolean levelReady(Minecraft minecraft) {
        if (minecraft.level == null || minecraft.player == null || !SkyLink.active()) return false;
        SkyLink.SkyState host = new SkyLink.SkyState();
        if (!SkyLink.readSkyState(host) || !host.inGame() || host.worldId == 0) return false;
        if (external(minecraft)) return true;
        if (failure != null || capture != null || closingServer != null) return false;
        MinecraftServer server = minecraft.getSingleplayerServer();
        String save = managedSave(server);
        if (save == null) return server != null; // preserve unrelated local-world compatibility
        return initializedServer == server && state != null && !state.pending() && sceneFor(save) == host.worldId;
    }

    /** Ordered render-ring fence: discard stale MMF selections before accepting the new scene. */
    public static boolean exportReady(Minecraft minecraft) {
        if (!levelReady(minecraft)) return false;
        SkyLink.SkyState host = new SkyLink.SkyState();
        if (!SkyLink.readSkyState(host) || !host.inGame() || host.worldId == 0) return false;
        MinecraftServer server = minecraft.getSingleplayerServer();
        String save = managedSave(server);
        if (!external(minecraft) && save != null && sceneFor(save) != host.worldId) return false;
        int generation = SkyLink.generation();
        if (minecraft.level == exportedLevel && generation == exportedGeneration &&
            host.worldId == exportedScene && host.collisionEpoch == exportedEpoch) return true;
        // This MMF lane is separate from the ordered ring. Empty it before the receiver's
        // marker opens rendering, otherwise the previous scene's drops/selection could flash.
        SkyLink.writeWorldEntities(List.of(), null);
        ByteBuffer header = ByteBuffer.allocate(8).order(ByteOrder.LITTLE_ENDIAN)
            .putInt(host.worldId).putInt(host.collisionEpoch).flip();
        if (!SkyLink.tryWriteRender(13, header, null)) return false;
        exportedLevel = minecraft.level; exportedGeneration = generation;
        exportedScene = host.worldId; exportedEpoch = host.collisionEpoch;
        SceneExporterAccess.peakcraft$setSentLevel(null);
        LOG.info("MinePeak: ordered render fence ready for scene {} epoch {}", exportedScene, exportedEpoch);
        return true;
    }

    /** Overrides 26.3's singleplayer-UUID/legacy data precedence only for a verified transfer. */
    public static Optional<CompoundTag> transferredPlayer(MinecraftServer server, NameAndId player) {
        if (store == null || state == null || !player.id().equals(Minecraft.getInstance().getGameProfile().id())) return null;
        String save = managedSave(server);
        if (save == null) return null;
        try {
            if (!state.pending() || !save.equals(state.pendingSave())) {
                if (!save.equals(state.save())) return null;
                UUID previousOwner = server.getWorldData().getSinglePlayerUUID();
                if (previousOwner == null || previousOwner.equals(player.id())) return null;
                // Vanilla follows the world's previous owner's UUID. A newly selected
                // SKLauncher profile must use its own file, never the previous account's.
                Path local = store.playerFile(save);
                if (!Files.exists(local)) return Optional.empty();
                if (!Files.isRegularFile(local)) throw new IOException("Local profile player data is not a regular file");
                LOG.info("MinePeak: selected profile loaded from its own scene player data");
                return Optional.of(decode(Files.readAllBytes(local)));
            }
            CompoundTag tag = decode(store.snapshot(state.snapshot()));
            SkyLink.SkyState host = new SkyLink.SkyState();
            if (!SkyLink.readSkyState(host) || !host.inGame()) throw new IOException("PEAK scene unavailable during transfer");
            // World-local vehicles and projectiles must not be duplicated across scenes.
            tag.remove("RootVehicle"); tag.remove("ender_pearls");
            ListTag pos = new ListTag(); pos.add(DoubleTag.valueOf(host.x)); pos.add(DoubleTag.valueOf(host.y)); pos.add(DoubleTag.valueOf(host.z));
            ListTag motion = new ListTag(); motion.add(DoubleTag.valueOf(0)); motion.add(DoubleTag.valueOf(0)); motion.add(DoubleTag.valueOf(0));
            ListTag rotation = new ListTag(); rotation.add(FloatTag.valueOf(host.yaw)); rotation.add(FloatTag.valueOf(host.pitch));
            tag.put("Pos", pos); tag.put("Motion", motion); tag.put("Rotation", rotation); tag.putFloat("fall_distance", 0);
            tag.putString("Dimension", "minecraft:overworld");
            LOG.info("MinePeak: verified player snapshot supplied for scene {}", state.pendingScene());
            return Optional.of(tag);
        } catch (Throwable error) { fail(error); throw new IllegalStateException("MinePeak refused an unsafe player transfer", error); }
    }

    private static void serverTick(MinecraftServer server) {
        if (failure != null || store == null || state == null || initializedServer == server) return;
        String save = managedSave(server);
        if (save == null || !save.equals(openingSave)) return;
        ServerPlayer player = localPlayer(server);
        if (player == null) return;
        try {
            state = store.complete(save, openingScene, encode(player));
            initializedServer = server;
            LOG.info("MinePeak: scene {} ready; player transfer committed with verified backup", openingScene);
        } catch (Throwable error) { fail(error); }
    }

    /** Observe the final vanilla save too, so last-tick pickups are included before a scene closes. */
    public static void playerSaved(net.minecraft.world.entity.player.Player player) {
        if (capture == null && closingServer == null) return;
        if (!(player instanceof ServerPlayer local) || store == null || state == null || !state.pending()) return;
        MinecraftServer server = local.level().getServer();
        String save = managedSave(server);
        if (save == null || !save.equals(state.save()) || !local.getUUID().equals(Minecraft.getInstance().getGameProfile().id())) return;
        try {
            // Do not recurse through saveEverything()/PlayerDataStorage.save here.
            // This final vanilla-save observation must also flush its source world first.
            server.saveAllChunks(true, true, true);
            state = store.prepare(state.save(), state.scene(), state.pendingSave(), state.pendingScene(), encode(local));
        }
        catch (Throwable error) { fail(error); }
    }

    private static byte[] encode(ServerPlayer player) throws IOException {
        TagValueOutput output = TagValueOutput.createWithContext(ProblemReporter.DISCARDING, player.registryAccess());
        player.saveWithoutId(output);
        ByteArrayOutputStream bytes = new ByteArrayOutputStream(); NbtIo.writeCompressed(output.buildResult(), bytes);
        byte[] encoded = bytes.toByteArray(); decode(encoded); return encoded;
    }
    private static CompoundTag decode(byte[] bytes) throws IOException {
        return NbtIo.readCompressed(new ByteArrayInputStream(bytes), NbtAccounter.create(64L * 1024 * 1024));
    }
    private static ServerPlayer localPlayer(MinecraftServer server) {
        var profile = server.getSingleplayerProfile();
        return profile == null ? null : server.getPlayerList().getPlayer(profile.id());
    }
    private static String managedSave(MinecraftServer server) {
        if (server == null || !server.isSingleplayer() || !"SkyCraft".equals(server.getWorldData().getLevelName())) return null;
        Path path = server.getWorldPath(LevelResource.ROOT).toAbsolutePath().normalize();
        Path base = Minecraft.getInstance().getLevelSource().getBaseDir().toAbsolutePath().normalize();
        if (!base.equals(path.getParent())) return null;
        String name = path.getFileName().toString();
        return name.equals("SkyCraft") || name.matches("SkyCraft-scene-[0-9]{1,10}") ? name : null;
    }
    private static int sceneFor(String name) { return name.equals("SkyCraft") ? 4 : Integer.parseUnsignedInt(name.substring("SkyCraft-scene-".length())); }
    private static boolean external(Minecraft minecraft) {
        if (MirrorWorld.friendAddress(minecraft) != null || (minecraft.level != null && minecraft.getSingleplayerServer() == null)) return true;
        long now = System.currentTimeMillis();
        if (now >= nextConfigRead) {
            nextConfigRead = now + 1000;
            Path path = minecraft.gameDirectory.toPath().resolve("config/skycraft.properties");
            try {
                Properties config = new Properties();
                if (Files.exists(path)) try (var reader = Files.newBufferedReader(path)) { config.load(reader); }
                externalConfig = !config.getProperty("join", "").isBlank();
            } catch (IOException error) { externalConfig = false; fail(error); }
        }
        return externalConfig;
    }
    private static void fail(Throwable error) {
        if (failure != null) return;
        failure = error.toString();
        LOG.error("MinePeak: scene transfer stopped safely; original worlds and inventory backups preserved. Restart after resolving the storage error.", error);
    }
}
