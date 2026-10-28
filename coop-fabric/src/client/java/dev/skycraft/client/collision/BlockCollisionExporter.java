package dev.skycraft.client.collision;

import dev.skycraft.link.SkyLink;
import it.unimi.dsi.fastutil.longs.LongLinkedOpenHashSet;
import it.unimi.dsi.fastutil.longs.LongOpenHashSet;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import net.fabricmc.api.ClientModInitializer;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.core.BlockPos;
import net.minecraft.core.SectionPos;
import net.minecraft.world.level.chunk.LevelChunk;
import net.minecraft.world.level.chunk.LevelChunkSection;
import net.minecraft.world.level.chunk.status.ChunkStatus;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.shapes.VoxelShape;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * An additive extension of the installed Co-op bridge. It exports the actual Minecraft
 * block collision shapes without replacing any upstream rendering or multiplayer code.
 *
 * Render message 12: six little-endian ints (section X/Y/Z, box count, host world ID,
 * collision epoch), then six floats per box (local min XYZ, local max XYZ). A zero
 * count removes that section. The PEAK receiver rejects packets from an old world/epoch.
 */
public final class BlockCollisionExporter implements ClientModInitializer {
    private static final int MESSAGE = 12;
    private static final int MAX_BOXES = 65_536;
    private static final int SECTIONS_PER_FRAME = 4;
    private static final long FRAME_BUDGET_NS = 3_000_000L;
    private static final Logger LOG = LoggerFactory.getLogger("peakcraft-collision");
    private static final LongLinkedOpenHashSet DIRTY = new LongLinkedOpenHashSet();
    private static final LongOpenHashSet SENT = new LongOpenHashSet();
    private static final ByteBuffer BOXES = ByteBuffer.allocate(MAX_BOXES * 6 * Float.BYTES).order(ByteOrder.LITTLE_ENDIAN);
    private static final SkyLink.SkyState HOST = new SkyLink.SkyState();
    private static ClientLevel trackedLevel;
    private static int generation = Integer.MIN_VALUE;
    private static int worldId;
    private static int epoch;
    private static volatile boolean fullScan = true;
    private static long lastError;
    private static int sentSections;

    private enum SendResult { SKIPPED, SENT, BACKPRESSURE }

    @Override
    public void onInitializeClient() {
        LOG.info("PeakCraft: exact block collision extension ready (render 12, world/epoch guarded)");
    }

    public static void markDirty(int sx, int sy, int sz, boolean urgent) {
        long key = SectionPos.asLong(sx, sy, sz);
        synchronized (DIRTY) {
            if (urgent) DIRTY.addAndMoveToFirst(key);
            else DIRTY.add(key);
        }
    }

    /** The original renderer is clearing/reloading: recreate colliders as well. */
    public static void resendAll() {
        fullScan = true;
    }

    /** Called after the original WorldExporter frame, on the render thread. */
    public static void frame(Minecraft minecraft) {
        ClientLevel level = minecraft.level;
        if (level == null || minecraft.player == null || !SkyLink.active() || !SkyLink.readSkyState(HOST) || !HOST.inGame()) return;
        int currentGeneration = SkyLink.generation();
        boolean changedLevel = level != trackedLevel;
        boolean changedWorld = HOST.worldId != worldId;
        if (changedLevel || changedWorld || currentGeneration != generation) {
            // Old queued cells must never be relabelled with the new world's identity.
            synchronized (DIRTY) { DIRTY.clear(); }
            if (changedLevel || changedWorld) SENT.clear();
            trackedLevel = level;
            worldId = HOST.worldId;
            generation = currentGeneration;
            fullScan = true;
        }
        if (HOST.collisionEpoch != epoch) {
            epoch = HOST.collisionEpoch;
            fullScan = true;
        }
        if (fullScan) {
            fullScan = false;
            queueLoaded(minecraft, level);
        }
        long deadline = System.nanoTime() + FRAME_BUDGET_NS;
        int sent = 0;
        while (sent < SECTIONS_PER_FRAME && System.nanoTime() < deadline) {
            long key;
            synchronized (DIRTY) {
                if (DIRTY.isEmpty()) break;
                key = DIRTY.removeFirstLong();
            }
            try {
                SendResult result = sendSection(level, key);
                if (result == SendResult.BACKPRESSURE) {
                    synchronized (DIRTY) { DIRTY.addAndMoveToFirst(key); }
                    break; // a full render ring gets one retry on the next frame
                }
                // Chunk/light notifications dirty thousands of unloaded/all-air cells.
                // Only actual collider snapshots consume the send budget, so those
                // cheap skips cannot delay a placed block or its removal for minutes.
                if (result == SendResult.SENT) sent++;
            } catch (RuntimeException error) {
                // A transient chunk/shape update must not permanently keep an old collider.
                synchronized (DIRTY) { DIRTY.add(key); }
                if (System.currentTimeMillis() - lastError > 10_000) {
                    lastError = System.currentTimeMillis();
                    LOG.warn("PeakCraft: keeping the last complete collision section after an export error", error);
                }
            }
        }
    }

    private static void queueLoaded(Minecraft minecraft, ClientLevel level) {
        synchronized (DIRTY) {
            // This also resends empty sections to remove colliders after a delayed update.
            for (long key : SENT) DIRTY.add(key);
        }
        int radius = minecraft.options.getEffectiveRenderDistance() + 1;
        int pcx = SectionPos.blockToSectionCoord(minecraft.player.getBlockX());
        int pcz = SectionPos.blockToSectionCoord(minecraft.player.getBlockZ());
        for (int cx = pcx - radius; cx <= pcx + radius; cx++) {
            for (int cz = pcz - radius; cz <= pcz + radius; cz++) {
                LevelChunk chunk = level.getChunkSource().getChunk(cx, cz, ChunkStatus.FULL, false);
                if (chunk == null) continue;
                LevelChunkSection[] sections = chunk.getSections();
                for (int i = 0; i < sections.length; i++) {
                    if (!sections[i].hasOnlyAir()) markDirty(cx, chunk.getSectionYFromSectionIndex(i), cz, false);
                }
            }
        }
        LOG.info("PeakCraft: collision rescan for world {} epoch {}", worldId, epoch);
    }

    private static SendResult sendSection(ClientLevel level, long key) {
        int sx = SectionPos.x(key), sy = SectionPos.y(key), sz = SectionPos.z(key);
        LevelChunk chunk = level.getChunkSource().getChunk(sx, sz, ChunkStatus.FULL, false);
        if (chunk == null) return SendResult.SKIPPED; // keep the complete last-known shape for an unloaded section
        int index = level.getSectionIndexFromSectionY(sy);
        LevelChunkSection section = index >= 0 && index < chunk.getSections().length ? chunk.getSections()[index] : null;
        int count = 0;
        BOXES.clear();
        if (section != null && !section.hasOnlyAir()) {
            BlockPos origin = SectionPos.of(sx, sy, sz).origin();
            BlockPos.MutableBlockPos pos = new BlockPos.MutableBlockPos();
            for (int y = 0; y < 16; y++) {
                for (int z = 0; z < 16; z++) {
                    for (int x = 0; x < 16; x++) {
                        pos.set(origin.getX() + x, origin.getY() + y, origin.getZ() + z);
                        var state = chunk.getBlockState(pos);
                        if (state.isAir()) continue;
                        // BlockState's shape excludes the PEAK mountain injected by
                        // BlockCollisionsMixin, preventing a PEAK -> MC -> PEAK feedback loop.
                        VoxelShape shape = state.getCollisionShape(level, pos);
                        for (AABB box : shape.toAabbs()) {
                            if (count >= MAX_BOXES) throw new IllegalStateException("Section exceeds " + MAX_BOXES + " collision boxes");
                            BOXES.putFloat((float) (x + box.minX)).putFloat((float) (y + box.minY)).putFloat((float) (z + box.minZ));
                            BOXES.putFloat((float) (x + box.maxX)).putFloat((float) (y + box.maxY)).putFloat((float) (z + box.maxZ));
                            count++;
                        }
                    }
                }
            }
        }
        if (count == 0 && !SENT.contains(key)) return SendResult.SKIPPED;
        ByteBuffer header = ByteBuffer.allocate(24).order(ByteOrder.LITTLE_ENDIAN)
            .putInt(sx).putInt(sy).putInt(sz).putInt(count).putInt(worldId).putInt(epoch).flip();
        if (!SkyLink.tryWriteRender(MESSAGE, header, BOXES.flip())) return SendResult.BACKPRESSURE;
        if (count > 0) SENT.add(key); else SENT.remove(key);
        if (++sentSections <= 10 || sentSections % 200 == 0)
            LOG.info("PeakCraft: exact collision section {} {} {}: {} boxes ({} live)", sx, sy, sz, count, SENT.size());
        return SendResult.SENT;
    }
}
