package dev.skycraft.client.mixin;

import dev.skycraft.link.SkyLink;
import net.minecraft.client.Minecraft;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Capability and creative-state bits for optional native movement handoffs. */
@Mixin(value = SkyLink.class, remap = false)
abstract class NativeMovementStateMixin {
    @Inject(method = "writeMcState", at = @At("HEAD"))
    private static void peakcraft$movementState(SkyLink.McState state, CallbackInfo ci) {
        var player = Minecraft.getInstance().player;
        state.flags |= 0x200;
        if (player != null && (player.getAbilities().instabuild || player.isSpectator()))
            state.flags |= 0x100;
        else
            state.flags &= ~0x100;
    }
}
