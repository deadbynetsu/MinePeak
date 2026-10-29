package dev.skycraft.client.mixin;

import dev.skycraft.client.scene.SceneWorlds;
import net.minecraft.client.gui.screens.worldselection.WorldOpenFlows;
import net.minecraft.world.level.storage.LevelStorageSource;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Continue only an owned experimental scene, after native backup completion and CRC validation. */
@Mixin(WorldOpenFlows.class)
abstract class SceneWorldBackupMixin {
    @Inject(method="askForBackup", at=@At("HEAD"), cancellable=true)
    private void peakcraft$backupScene(LevelStorageSource.LevelStorageAccess level, boolean customized,
                                        Runnable proceed, Runnable cancel, CallbackInfo ci) {
        if (SceneWorlds.backupExperimentalWorld(level, customized, proceed, cancel)) ci.cancel();
    }
}
