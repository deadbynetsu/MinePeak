package dev.skycraft.client.mixin;

import dev.skycraft.client.SkyDigClient;
import dev.skycraft.client.scene.SceneWorlds;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.block.state.BlockState;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Terrain edits use the host scene ID too, so never poll/reveal into an old client level. */
@Mixin(value = SkyDigClient.class, remap = false)
abstract class SceneDigMixin {
    @Inject(method="tick", at=@At("HEAD"), cancellable=true)
    private static void peakcraft$digScene(Minecraft minecraft, CallbackInfo ci) {
        // Allow the original no-level branch to discard its transient caches normally.
        if (minecraft.level != null && !SceneWorlds.levelReady(minecraft)) ci.cancel();
    }

    @Inject(method="blockChanged", at=@At("HEAD"), cancellable=true)
    private static void peakcraft$changedScene(ClientLevel level, BlockPos pos, BlockState before, BlockState after, CallbackInfo ci) {
        Minecraft minecraft = Minecraft.getInstance();
        if (level != minecraft.level || !SceneWorlds.levelReady(minecraft)) ci.cancel();
    }
}
