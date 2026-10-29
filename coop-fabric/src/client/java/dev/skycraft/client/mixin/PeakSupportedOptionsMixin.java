package dev.skycraft.client.mixin;

import dev.skycraft.client.DestructionToggle;
import dev.skycraft.world.SkyDig;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** PEAK terrain excavation is unsupported; don't advertise the upstream Skyrim toggle. */
@Mixin(value = DestructionToggle.class, remap = false)
abstract class PeakSupportedOptionsMixin {
    @Inject(method="register", at=@At("HEAD"), cancellable=true)
    private static void peakcraft$supportedOptions(CallbackInfo ci) {
        SkyDig.destruction = false;
        ci.cancel();
    }
}
