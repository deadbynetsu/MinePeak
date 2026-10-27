package dev.skycraft.mixin;

import dev.skycraft.SkyCraft;
import dev.skycraft.link.SkyLink;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.phys.Vec3;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(Entity.class)
public abstract class VoidGuardMixin {
   private static final Map<UUID, Vec3> LAST_GROUND = new ConcurrentHashMap<>();

   @Inject(method = "checkBelowWorld", at = @At("HEAD"))
   private void skycraft$rememberGround(CallbackInfo var1) {
      Entity var2 = (Entity)this;
      if (var2 instanceof ServerPlayer var3 && SkyLink.active() && var3.onGround() && var3.getY() >= var3.level().getMinY() - 64.0) {
         LAST_GROUND.put(var3.getUUID(), var3.position());
      }
   }

   @Inject(method = "checkBelowWorld", at = @At("HEAD"), cancellable = true)
   private void skycraft$catchFall(CallbackInfo var1) {
      Entity var2 = (Entity)this;
      if (var2 instanceof ServerPlayer var3 && SkyLink.active() && !(var3.getY() >= var3.level().getMinY() - 64.0)) {
         Vec3 var4 = LAST_GROUND.get(var3.getUUID());
         if (var4 == null) {
            var4 = new Vec3(var3.getX(), var3.level().getMinY() + 64.0, var3.getZ());
         }

         var3.teleportTo(var4.x, var4.y + 0.05, var4.z);
         var3.setDeltaMovement(Vec3.ZERO);
         var3.resetFallDistance();
         SkyCraft.LOG.info("SkyCraft: void guard: {} fell below the world; put back at {}", var3.getName().getString(), var4);
         var1.cancel();
      }
   }
}
