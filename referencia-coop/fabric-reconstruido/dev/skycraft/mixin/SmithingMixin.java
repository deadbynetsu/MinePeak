package dev.skycraft.mixin;

import dev.skycraft.link.SkyLink;
import dev.skycraft.net.SkyNet;
import net.minecraft.core.component.DataComponents;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Unique;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(ItemStack.class)
public abstract class SmithingMixin {
   @Inject(method = "onCraftedBy", at = @At("HEAD"))
   private void skycraft$trainSmithing(Player var1, int var2, CallbackInfo var3) {
      ItemStack var4 = (ItemStack)this;
      if (var1 instanceof ServerPlayer var5 && SkyNet.isHost(var5) && var2 > 0) {
         if (var4.has(DataComponents.EQUIPPABLE)
            || var4.has(DataComponents.TOOL)
            || var4.has(DataComponents.WEAPON)
            || var4.is(Items.BOW)
            || var4.is(Items.CROSSBOW)) {
            SkyLink.pushEvent(5, 10, skycraft$worth(var4) * var2, 0.0F, 0.0F, 0.0F, 0);
         }
      }
   }

   @Unique
   private static float skycraft$worth(ItemStack var0) {
      String var1 = BuiltInRegistries.ITEM.getKey(var0.getItem()).getPath();
      if (var1.startsWith("netherite_")) {
         return 150.0F;
      } else if (var1.startsWith("diamond_")) {
         return 80.0F;
      } else if (var1.startsWith("iron_")) {
         return 40.0F;
      } else if (var1.startsWith("golden_") || var1.startsWith("chainmail_")) {
         return 30.0F;
      } else if (var1.startsWith("copper_")) {
         return 25.0F;
      } else {
         return var1.startsWith("wooden_") ? 15.0F : 20.0F;
      }
   }
}
