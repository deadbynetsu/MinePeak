package dev.skycraft.client.vitals;

import dev.skycraft.link.SkyLink;
import java.lang.foreign.MemorySegment;
import java.lang.foreign.ValueLayout;
import java.lang.invoke.VarHandle;
import net.fabricmc.api.ClientModInitializer;
import net.fabricmc.fabric.api.event.lifecycle.v1.ServerTickEvents;
import net.minecraft.client.Minecraft;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.food.FoodData;
import net.minecraft.world.level.GameType;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/** Local, versioned extension of the existing bridge. All player changes run on the server thread. */
public final class VitalsBridge implements ClientModInitializer {
    public static final int MAGIC = 0x56495431;
    public static final long HOST_OFFSET = 0x900;
    public static final long GUEST_OFFSET = 0xA00;
    private static final int ENABLED = 1;
    private static final int MINECRAFT_OWNS = 2;
    private static final int FORCE_SURVIVAL = 8;
    private static final long TIMEOUT_MS = 3000;
    private static final VarHandle INT = ValueLayout.JAVA_INT.varHandle();
    private static final Logger LOG = LoggerFactory.getLogger("peakcraft-vitals");

    private static MinecraftServer trackedServer;
    private static ServerPlayer trackedPlayer;
    private static int playerEpoch;
    private static int deathCount;
    private static boolean wasAlive;
    private static int session;
    private static int acknowledged;
    private static int survivalSession;
    private static MinecraftServer survivalServer;
    private static int linkGeneration;
    private static float exactFood;
    private static int lastFood;
    private static long lastError;
    private static long lastStateLog;

    @Override
    public void onInitializeClient() {
        ServerTickEvents.END_SERVER_TICK.register(VitalsBridge::tick);
        LOG.info("PeakCraft: shared hunger and health extension ready (protocol VIT1)");
    }

    public static boolean pauseNaturalFoodTick(ServerPlayer player) {
        MinecraftServer server = player.level().getServer();
        if (!isMirrorServer(server) || !isLocalPlayer(server, player)) return false;
        Host host = readHost();
        return host != null && (host.flags & MINECRAFT_OWNS) == 0;
    }

    private static boolean isMirrorServer(MinecraftServer server) {
        return server != null && server == Minecraft.getInstance().getSingleplayerServer()
            && server.isSingleplayer() && "SkyCraft".equals(server.getWorldData().getLevelName());
    }

    private static boolean isLocalPlayer(MinecraftServer server, ServerPlayer player) {
        var profile = server.getSingleplayerProfile();
        return profile != null && profile.id().equals(player.getUUID());
    }

    private static ServerPlayer findPlayer(MinecraftServer server) {
        var profile = server.getSingleplayerProfile();
        return profile == null ? null : server.getPlayerList().getPlayer(profile.id());
    }

    private static void tick(MinecraftServer server) {
        if (!isMirrorServer(server)) return;
        try {
            Host host = readHost();
            if (host == null) return;
            ServerPlayer player = findPlayer(server);
            if (player == null) {
                publish(host, null);
                return;
            }
            boolean playerChanged = server != trackedServer || player != trackedPlayer;
            boolean alive = player.isAlive();
            if (playerChanged) {
                // Immediate respawn can replace the player between two end-tick observations.
                // Health excludes disconnect/removal, which can also make isAlive() false.
                if (trackedPlayer != null && wasAlive && !trackedPlayer.isAlive() && trackedPlayer.getHealth() <= 0) {
                    deathCount++;
                }
                trackedServer = server;
                trackedPlayer = player;
                playerEpoch++;
                exactFood = player.getFoodData().getFoodLevel();
                lastFood = player.getFoodData().getFoodLevel();
                wasAlive = alive;
            } else if (wasAlive && !alive) {
                playerEpoch++;
                deathCount++;
            }
            wasAlive = alive;

            int generation = SkyLink.generation();
            if (host.session != session || generation != linkGeneration) {
                session = host.session;
                linkGeneration = generation;
                acknowledged = 0;
                exactFood = player.getFoodData().getFoodLevel();
                lastFood = player.getFoodData().getFoodLevel();
                LOG.info("PeakCraft: vitals session linked; player epoch {}", playerEpoch);
            }

            // Changes from Minecraft food, exhaustion, commands, and effects are measured before applying PEAK.
            FoodData food = player.getFoodData();
            exactFood = clamp(exactFood + food.getFoodLevel() - lastFood, 0, 20);
            lastFood = food.getFoodLevel();

            if (host.command != 0 && host.command != acknowledged) {
                if (host.kind >= 1 && host.kind <= 3 && Float.isFinite(host.health) && Float.isFinite(host.food)) {
                    apply(host, player);
                } else {
                    LOG.warn("PeakCraft: rejected invalid vitals command {}", host.command);
                }
                acknowledged = host.command;
            }
            // Applying health zero may have killed the player during this tick.
            alive = player.isAlive();
            if (wasAlive && !alive) {
                playerEpoch++;
                deathCount++;
            }
            wasAlive = alive;
            publish(host, player);

            long now = SkyLink.tickCount();
            if (now - lastStateLog > 20000) {
                lastStateLog = now;
                LOG.info("PeakCraft: shared vitals health20={} food20={} saturation={} ack={} epoch={} minecraftOwns={}",
                    normalizedHealth(player), exactFood, food.getSaturationLevel(), acknowledged, playerEpoch,
                    (host.flags & MINECRAFT_OWNS) != 0);
            }
        } catch (RuntimeException error) {
            long now = System.currentTimeMillis();
            if (now - lastError > 10000) {
                lastError = now;
                LOG.warn("PeakCraft: shared vitals paused after an error", error);
            }
        }
    }

    private static void apply(Host host, ServerPlayer player) {
        boolean absolute = host.kind == 1 || host.kind == 3;
        if (absolute && (host.flags & FORCE_SURVIVAL) != 0 &&
            (survivalSession != host.session || survivalServer != player.level().getServer())) {
            survivalSession = host.session;
            survivalServer = player.level().getServer();
            if (player.gameMode() == GameType.CREATIVE) {
                player.setGameMode(GameType.SURVIVAL);
                LOG.info("PeakCraft: shared vitals started in Survival; Creative can still be selected manually");
            }
        }
        float health = absolute ? host.health : normalizedHealth(player) + host.health;
        float oldFood = exactFood;
        exactFood = clamp(absolute ? host.food : exactFood + host.food, 0, 20);
        FoodData food = player.getFoodData();
        int roundedFood = Math.min(20, Math.max(0, (int)Math.floor(exactFood + 0.5f)));
        food.setFoodLevel(roundedFood);
        if (exactFood > oldFood) {
            food.setSaturation(Math.min(roundedFood, food.getSaturationLevel() + exactFood - oldFood));
        } else if (food.getSaturationLevel() > roundedFood) {
            food.setSaturation(roundedFood);
        }
        lastFood = food.getFoodLevel();

        // A late healing packet must not resurrect a dead player; let vanilla respawn create a new epoch.
        if (player.isAlive()) {
            float target = clamp(health, 0, 20) / 20f * player.getMaxHealth();
            if (target <= 0) {
                player.hurtServer(player.level(), player.damageSources().genericKill(), Float.MAX_VALUE);
            } else {
                player.setHealth(Math.min(player.getMaxHealth(), target));
            }
        }
        LOG.info("PeakCraft: applied vitals command {} kind={} health20={} food20={} alive={}",
            host.command, host.kind, normalizedHealth(player), exactFood, player.isAlive());
    }

    private static float normalizedHealth(ServerPlayer player) {
        float maximum = player.getMaxHealth();
        return maximum > 0 ? clamp(player.getHealth() / maximum * 20f, 0, 20) : 0;
    }

    private static Host readHost() {
        if (!SkyLink.active()) return null;
        MemorySegment memory = SkyLink.segment();
        if (memory == null) return null;
        for (int attempt = 0; attempt < 4; attempt++) {
            int before = (int)INT.getAcquire(memory, HOST_OFFSET);
            if (before == 0 || (before & 1) != 0) continue;
            int magic = memory.get(ValueLayout.JAVA_INT, HOST_OFFSET + 4);
            int hostSession = memory.get(ValueLayout.JAVA_INT, HOST_OFFSET + 8);
            int flags = memory.get(ValueLayout.JAVA_INT, HOST_OFFSET + 12);
            int command = memory.get(ValueLayout.JAVA_INT, HOST_OFFSET + 16);
            int kind = memory.get(ValueLayout.JAVA_INT, HOST_OFFSET + 20);
            float health = memory.get(ValueLayout.JAVA_FLOAT, HOST_OFFSET + 24);
            float food = memory.get(ValueLayout.JAVA_FLOAT, HOST_OFFSET + 28);
            int world = memory.get(ValueLayout.JAVA_INT, HOST_OFFSET + 32);
            long heartbeat = memory.get(ValueLayout.JAVA_LONG, HOST_OFFSET + 40);
            VarHandle.loadLoadFence();
            if ((int)INT.getAcquire(memory, HOST_OFFSET) != before) continue;
            long age = SkyLink.tickCount() - heartbeat;
            if (magic != MAGIC || (flags & ENABLED) == 0 || hostSession == 0 || age < 0 || age >= TIMEOUT_MS) return null;
            return new Host(hostSession, flags, command, kind, health, food, world);
        }
        return null;
    }

    private static void publish(Host host, ServerPlayer player) {
        MemorySegment memory = SkyLink.segment();
        if (memory == null) return;
        int previous = (int)INT.getAcquire(memory, GUEST_OFFSET);
        int odd = (previous & ~1) + 1;
        INT.setRelease(memory, GUEST_OFFSET, odd);
        VarHandle.storeStoreFence();
        memory.set(ValueLayout.JAVA_INT, GUEST_OFFSET + 4, MAGIC);
        memory.set(ValueLayout.JAVA_INT, GUEST_OFFSET + 8, host.session);
        int flags = player == null ? 0 : 1 | (player.isAlive() ? 2 : 0) | (player.gameMode() == GameType.SURVIVAL ? 4 : 0);
        memory.set(ValueLayout.JAVA_INT, GUEST_OFFSET + 12, flags);
        memory.set(ValueLayout.JAVA_INT, GUEST_OFFSET + 16, acknowledged);
        memory.set(ValueLayout.JAVA_INT, GUEST_OFFSET + 20, playerEpoch);
        memory.set(ValueLayout.JAVA_FLOAT, GUEST_OFFSET + 24, player == null ? 0 : normalizedHealth(player));
        memory.set(ValueLayout.JAVA_FLOAT, GUEST_OFFSET + 28, player == null ? 0 : exactFood);
        memory.set(ValueLayout.JAVA_FLOAT, GUEST_OFFSET + 32, player == null ? 20 : player.getMaxHealth());
        memory.set(ValueLayout.JAVA_FLOAT, GUEST_OFFSET + 36, player == null ? 0 : player.getFoodData().getSaturationLevel());
        memory.set(ValueLayout.JAVA_LONG, GUEST_OFFSET + 40, SkyLink.tickCount());
        memory.set(ValueLayout.JAVA_INT, GUEST_OFFSET + 48, deathCount);
        memory.set(ValueLayout.JAVA_INT, GUEST_OFFSET + 52, 0);
        memory.set(ValueLayout.JAVA_LONG, GUEST_OFFSET + 56, 0L);
        VarHandle.storeStoreFence();
        INT.setRelease(memory, GUEST_OFFSET, odd + 1);
    }

    private static float clamp(float value, float minimum, float maximum) {
        return Math.max(minimum, Math.min(maximum, value));
    }

    private record Host(int session, int flags, int command, int kind, float health, float food, int world) {}
}
