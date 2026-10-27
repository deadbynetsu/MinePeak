package dev.skycraft.client.mixin;

import dev.skycraft.client.vitals.VitalsBridge;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.food.FoodData;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/** Only the game controlling the body advances natural hunger and food-based regeneration. */
@Mixin(FoodData.class)
public abstract class FoodDataVitalsMixin {
    @Inject(method = "tick", at = @At("HEAD"), cancellable = true)
    private void peakcraft$singleMetabolismClock(ServerPlayer player, CallbackInfo callback) {
        if (VitalsBridge.pauseNaturalFoodTick(player)) callback.cancel();
    }
}
