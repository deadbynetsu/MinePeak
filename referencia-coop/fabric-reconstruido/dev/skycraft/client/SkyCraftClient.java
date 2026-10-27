package dev.skycraft.client;

import com.mojang.brigadier.arguments.StringArgumentType;
import com.mojang.brigadier.builder.LiteralArgumentBuilder;
import dev.skycraft.SkyCraft;
import dev.skycraft.combat.SkyCombat;
import dev.skycraft.link.SkyLink;
import dev.skycraft.net.SkyNet;
import dev.skycraft.world.SkyCollision;
import net.fabricmc.api.ClientModInitializer;
import net.fabricmc.fabric.api.client.command.v2.ClientCommandRegistrationCallback;
import net.fabricmc.fabric.api.client.command.v2.ClientCommands;
import net.fabricmc.fabric.api.client.command.v2.FabricClientCommandSource;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientTickEvents;
import net.fabricmc.fabric.api.client.networking.v1.ClientPlayConnectionEvents;
import net.fabricmc.fabric.api.client.networking.v1.ClientPlayNetworking;
import net.fabricmc.fabric.api.client.networking.v1.ClientPlayConnectionEvents.Join;
import net.fabricmc.fabric.api.client.rendering.v1.EntityRendererRegistry;
import net.minecraft.client.Minecraft;
import net.minecraft.client.renderer.entity.NoopRenderer;
import net.minecraft.client.server.IntegratedServer;
import net.minecraft.network.chat.Component;
import net.minecraft.server.MinecraftServer.MultiplayerScope;
import net.minecraft.world.entity.player.Player;

public final class SkyCraftClient implements ClientModInitializer {
   public void onInitializeClient() {
      SkyLink.announceRunning();
      DiscordPresence.start();
      DestructionToggle.register();
      ClientCommandRegistrationCallback.EVENT
         .register(
            (ClientCommandRegistrationCallback)(var0, var1) -> {
               var0.register(
                  (LiteralArgumentBuilder)ClientCommands.literal("join")
                     .then(ClientCommands.argument("link", StringArgumentType.greedyString()).executes(var0x -> {
                        String var1x = StringArgumentType.getString(var0x, "link");
                        ((FabricClientCommandSource)var0x.getSource()).sendFeedback(Component.literal("Joining " + var1x.trim() + "..."));
                        Minecraft.getInstance().execute(() -> MirrorWorld.joinFriend(Minecraft.getInstance(), var1x));
                        return 1;
                     }))
               );
               var0.register((LiteralArgumentBuilder)ClientCommands.literal("leave").executes(var0x -> {
                  Minecraft.getInstance().execute(() -> MirrorWorld.leaveFriend(Minecraft.getInstance()));
                  return 1;
               }));
            }
         );
      ClientTickEvents.END_CLIENT_TICK.register(SkyClient::clientTick);
      ClientPlayConnectionEvents.JOIN
         .register(
            (Join)(var0, var1, var2) -> {
               String var3 = System.getenv("SKYCRAFT_LAN_PORT");
               IntegratedServer var4 = var2.getSingleplayerServer();
               if (var3 != null && !var3.isBlank() && var4 != null && !var4.isPublished()) {
                  var2.execute(
                     () -> {
                        if (System.getenv("SKYCRAFT_LAN_OFFLINE") != null) {
                           var4.setUsesAuthentication(false);
                        }

                        boolean var2x = var4.publishServer(MultiplayerScope.LAN, false, Integer.parseInt(var3.trim()));
                        SkyCraft.LOG
                           .info(
                              "SkyCraft: world opened to LAN on port {} ({}{})",
                              new Object[]{
                                 var3.trim(), var2x ? "ok" : "FAILED", System.getenv("SKYCRAFT_LAN_OFFLINE") != null ? ", offline logins allowed" : ""
                              }
                           );
                     }
                  );
               }
            }
         );
      ClientPlayNetworking.registerGlobalReceiver(SkyNet.Died.TYPE, (var0, var1) -> {
         if (SkyLink.active()) {
            SkyLink.pushEvent(2, var0.attackerFormId(), 0.0F, 0.0F, 0.0F, 0.0F, 0);
         }
      });
      EntityRendererRegistry.register(SkyCombat.SKYRIM_ACTOR, NoopRenderer::new);
      SkyCollision.setSmoothCollider(var0 -> var0 instanceof Player && SkyClient.linked());
   }
}
