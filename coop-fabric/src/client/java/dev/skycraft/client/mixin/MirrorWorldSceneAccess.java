package dev.skycraft.client.mixin;

import dev.skycraft.client.MirrorWorld;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.gen.Accessor;

@Mixin(value = MirrorWorld.class, remap = false)
public interface MirrorWorldSceneAccess {
    @Accessor("attempted") static void peakcraft$setAttempted(boolean value) { throw new AssertionError(); }
}
