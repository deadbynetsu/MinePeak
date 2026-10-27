package dev.skycraft.client.mixin;

import net.minecraft.client.Minecraft;
import net.minecraft.client.player.AbstractClientPlayer;
import net.minecraft.core.ClientAsset;
import net.minecraft.resources.Identifier;
import net.minecraft.world.entity.player.PlayerModelType;
import net.minecraft.world.entity.player.PlayerSkin;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Optional local appearance only; uses Steve assets already shipped with Minecraft. */
@Mixin(AbstractClientPlayer.class)
public abstract class LocalSteveMixin {
    private static final PlayerSkin PEAKCRAFT_STEVE = new PlayerSkin(
        new ClientAsset.ResourceTexture(Identifier.withDefaultNamespace("entity/player/wide/steve")),
        null, null, PlayerModelType.WIDE, true);

    @Inject(method = "getSkin", at = @At("HEAD"), cancellable = true)
    private void peakcraft$localSteve(CallbackInfoReturnable<PlayerSkin> result) {
        if (Boolean.getBoolean("peakcraft.steve") && (Object)this == Minecraft.getInstance().player) {
            result.setReturnValue(PEAKCRAFT_STEVE);
        }
    }
}
