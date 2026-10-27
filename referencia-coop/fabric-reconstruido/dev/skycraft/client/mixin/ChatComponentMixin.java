package dev.skycraft.client.mixin;

import dev.skycraft.client.DiscordPresence;
import net.minecraft.client.gui.components.ChatComponent;
import net.minecraft.network.chat.Component;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.ModifyVariable;

@Mixin(ChatComponent.class)
public abstract class ChatComponentMixin {
   @ModifyVariable(method = "addMessage", at = @At("HEAD"), argsOnly = true, ordinal = 0)
   private Component skycraft$watchForLink(Component var1) {
      DiscordPresence.onChat(var1);
      return var1;
   }
}
