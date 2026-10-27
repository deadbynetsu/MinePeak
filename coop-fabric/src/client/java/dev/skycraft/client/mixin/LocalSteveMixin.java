package dev.skycraft.client.mixin;

import net.minecraft.client.Minecraft;
import net.minecraft.client.player.AbstractClientPlayer;
import net.minecraft.core.ClientAsset;
import net.minecraft.resources.Identifier;
import net.minecraft.world.entity.player.PlayerModelType;
import net.minecraft.world.entity.player.PlayerSkin;
import net.minecraft.client.renderer.texture.DynamicTexture;
import com.mojang.blaze3d.platform.NativeImage;
import java.nio.file.Files;
import java.nio.file.Path;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/** Optional local appearance only; uses Steve assets already shipped with Minecraft. */
@Mixin(AbstractClientPlayer.class)
public abstract class LocalSteveMixin {
    private static boolean peakcraft$attemptedSkin;
    private static PlayerSkin peakcraft$userSkin;
    private static final PlayerSkin PEAKCRAFT_STEVE = new PlayerSkin(
        new ClientAsset.ResourceTexture(Identifier.withDefaultNamespace("entity/player/wide/steve")),
        null, null, PlayerModelType.WIDE, true);

    @Inject(method = "getSkin", at = @At("HEAD"), cancellable = true)
    private void peakcraft$localSteve(CallbackInfoReturnable<PlayerSkin> result) {
        if ((Object)this != Minecraft.getInstance().player) return;
        if (!peakcraft$attemptedSkin) {
            peakcraft$attemptedSkin = true;
            String file = System.getenv("PEAKCRAFT_SKIN_FILE");
            if (file != null && !file.isBlank()) {
                try (var input = Files.newInputStream(Path.of(file))) {
                    NativeImage image = NativeImage.read(input);
                    if (image.getWidth() != 64 || image.getHeight() != 64) {
                        image.close();
                        throw new IllegalArgumentException("A skin deve ter 64x64 pixels");
                    }
                    Identifier id = Identifier.fromNamespaceAndPath("peakcraft", "local_user_skin");
                    Minecraft.getInstance().getTextureManager().register(id, new DynamicTexture(() -> "PeakCraft local skin", image));
                    PlayerModelType model = "slim".equalsIgnoreCase(System.getenv("PEAKCRAFT_SKIN_MODEL")) ? PlayerModelType.SLIM : PlayerModelType.WIDE;
                    peakcraft$userSkin = new PlayerSkin(new ClientAsset.ResourceTexture(id, id), null, null, model, false);
                    dev.skycraft.SkyCraft.LOG.info("PeakCraft: local user skin loaded (64x64, {})", model);
                } catch (Exception error) {
                    dev.skycraft.SkyCraft.LOG.warn("PeakCraft: local skin unavailable; using the profile default ({})", error.getClass().getSimpleName());
                }
            }
        }
        if (peakcraft$userSkin != null) {
            result.setReturnValue(peakcraft$userSkin);
        } else if (Boolean.getBoolean("peakcraft.steve")) {
            result.setReturnValue(PEAKCRAFT_STEVE);
        }
    }
}
