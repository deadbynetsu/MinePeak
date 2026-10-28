package dev.skycraft.client.mixin;

import dev.skycraft.client.collision.BlockCollisionExporter;
import dev.skycraft.client.render.WorldExporter;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.core.SectionPos;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Add physics export while preserving all existing Co-op renderer and block-event hooks. */
@Mixin(value = WorldExporter.class, remap = false)
public abstract class BlockCollisionExportMixin {
    @Inject(method = "markDirty", at = @At("TAIL"))
    private static void peakcraft$dirty(int sx, int sy, int sz, CallbackInfo ci) {
        BlockCollisionExporter.markDirty(sx, sy, sz, false);
    }

    @Inject(method = "markDirtyNow", at = @At("TAIL"))
    private static void peakcraft$dirtyNow(int sx, int sy, int sz, CallbackInfo ci) {
        BlockCollisionExporter.markDirty(sx, sy, sz, true);
    }

    @Inject(method = "resendEverything", at = @At("HEAD"))
    private static void peakcraft$reload(Minecraft minecraft, ClientLevel level, CallbackInfo ci) {
        BlockCollisionExporter.resendAll();
    }

    @Inject(method = "meshSection", at = @At("RETURN"))
    private static void peakcraft$section(ClientLevel level, long key, CallbackInfoReturnable<Boolean> ci) {
        if (ci.getReturnValueZ())
            // A completed visual update must get its collider before unrelated
            // chunk/light notifications, including when its last block was removed.
            BlockCollisionExporter.markDirty(SectionPos.x(key), SectionPos.y(key), SectionPos.z(key), true);
    }

    @Inject(method = "frame", at = @At("TAIL"))
    private static void peakcraft$frame(Minecraft minecraft, float partialTick, CallbackInfo ci) {
        BlockCollisionExporter.frame(minecraft);
    }
}
